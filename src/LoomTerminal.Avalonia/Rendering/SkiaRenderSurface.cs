using System.Globalization;
using System.Text;
using LoomTerminal.Presentation.Rendering;
using SkiaSharp;

namespace LoomTerminal.Avalonia.Rendering;

/// <summary><see cref="IRenderSurface"/> on a Skia canvas (the specification is the Win2D surface of P4-03 / P4-04 / P4-05).</summary>
/// <remarks>
/// Threading: create one per frame inside the draw operation; it does not own the canvas and is only valid during that
/// call, on that thread. The canvas is in DIPs (Avalonia applies the DPI scale).
/// </remarks>
internal sealed class SkiaRenderSurface : IRenderSurface, IDisposable
{
    private readonly SKCanvas _canvas;
    private readonly SkiaTextBlobCache _cache;
    private readonly TerminalFont _font;
    private readonly string _family;
    private readonly double _scale;
    private readonly SKPaint _paint = new();

    /// <summary>Wraps a canvas.</summary>
    /// <param name="canvas">The canvas leased from Avalonia.</param>
    /// <param name="cache">The blob cache shared between frames.</param>
    /// <param name="font">The terminal font.</param>
    /// <param name="family">The resolved primary family (<see cref="SkiaTextMeasurer.ResolveFamily"/>).</param>
    /// <param name="scale">Rasterization scale.</param>
    public SkiaRenderSurface(SKCanvas canvas, SkiaTextBlobCache cache, TerminalFont font, string family, double scale)
    {
        _canvas = canvas;
        _cache = cache;
        _font = font;
        _family = family;
        _scale = scale;
    }

    /// <inheritdoc />
    public void Dispose() => _paint.Dispose();

    /// <inheritdoc />
    public void Clear(uint rgba) => _canvas.Clear(ToColor(rgba));

    /// <inheritdoc />
    public void FillRectangle(double x, double y, double width, double height, uint rgba)
    {
        Setup(rgba, SKPaintStyle.Fill, 0, antialias: false);
        _canvas.DrawRect((float)x, (float)y, (float)width, (float)height, _paint);
    }

    /// <inheritdoc />
    public void DrawTextRun(int row, int column, string text, int cellCount, in TextRunStyle style, in CellMetrics metrics)
    {
        Setup(style.Foreground, SKPaintStyle.Fill, 0, antialias: true);
        double top = row * metrics.Height;
        SKTypeface primary = _cache.GetTypeface(_family, _font, style.Bold, style.Italic);
        if (IsPrintableAscii(text))
        {
            DrawBlob(text, column * metrics.Width, top, primary, style, metrics, positioned: true);
            return;
        }

        // Anything else goes one grapheme per cell position so wide and fallback glyphs never drift off the grid.
        int cell = column;
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            var grapheme = (string)enumerator.Current;
            int cp = Rune.TryGetRuneAt(grapheme, 0, out Rune rune) ? rune.Value : grapheme[0];
            DrawBlob(grapheme, cell * metrics.Width, top, _cache.GetTypefaceFor(primary, cp), style, metrics, positioned: false);
            cell += CellWidth(cp);
        }
    }

    /// <inheritdoc />
    public void DrawUnderline(double x, double y, double width, TerminalUnderlineStyle style, uint rgba, in CellMetrics metrics)
    {
        float thickness = (float)metrics.UnderlineThickness;
        float left = (float)x;
        float right = (float)(x + width);
        float lineY = (float)(Math.Floor(y * _scale) / _scale) + (thickness / 2);
        switch (style)
        {
            case TerminalUnderlineStyle.SingleLine:
                DrawLine(left, lineY, right, lineY, thickness, rgba);
                break;
            case TerminalUnderlineStyle.DoubleLine:
                DrawLine(left, lineY, right, lineY, thickness, rgba);
                DrawLine(left, lineY + (2 * thickness), right, lineY + (2 * thickness), thickness, rgba);
                break;
            case TerminalUnderlineStyle.Dotted:
            case TerminalUnderlineStyle.Dashed:
                float on = style == TerminalUnderlineStyle.Dotted ? thickness : 3 * thickness;
                using (SKPathEffect dash = SKPathEffect.CreateDash([on, 2 * thickness], 0))
                {
                    Setup(rgba, SKPaintStyle.Stroke, thickness, antialias: false);
                    _paint.PathEffect = dash;
                    _canvas.DrawLine(left, lineY, right, lineY, _paint);
                    _paint.PathEffect = null;
                }

                break;
            case TerminalUnderlineStyle.Curly:
                DrawCurly(left, right, lineY, thickness, rgba, (float)(metrics.Width / 2));
                break;
        }
    }

    /// <inheritdoc />
    public void DrawLine(double x1, double y1, double x2, double y2, double thickness, uint rgba)
    {
        Setup(rgba, SKPaintStyle.Stroke, (float)thickness, antialias: false);
        _canvas.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, _paint);
    }

    /// <inheritdoc />
    public void DrawRectangle(double x, double y, double width, double height, double thickness, uint rgba)
    {
        Setup(rgba, SKPaintStyle.Stroke, (float)thickness, antialias: false);
        _canvas.DrawRect((float)x, (float)y, (float)width, (float)height, _paint);
    }

    /// <inheritdoc />
    public void DrawCustomGlyph(CustomGlyphPath path, double x, double y, in CellMetrics metrics, uint rgba)
    {
        // The path is relative to the cell: translate by the cell origin; the clip hides powerline overshoot.
        int saved = _canvas.Save();
        _canvas.Translate((float)x, (float)y);
        if (path.ClipToCell)
        {
            _canvas.ClipRect(new SKRect(0, 0, (float)metrics.Width, (float)metrics.Height));
        }

        var skPath = new SKPath { FillType = SKPathFillType.Winding };
        foreach (CustomGlyphCommand c in path.Commands)
        {
            switch (c.Kind)
            {
                case CustomGlyphCommandKind.MoveTo:
                    skPath.MoveTo((float)c.A, (float)c.B);
                    break;
                case CustomGlyphCommandKind.LineTo:
                    skPath.LineTo((float)c.A, (float)c.B);
                    break;
                case CustomGlyphCommandKind.CurveTo:
                    skPath.CubicTo((float)c.A, (float)c.B, (float)c.C, (float)c.D, (float)c.E, (float)c.F);
                    break;
                case CustomGlyphCommandKind.Rect:
                    skPath.AddRect(new SKRect((float)c.A, (float)c.B, (float)(c.A + c.C), (float)(c.B + c.D)));
                    break;
                default:
                    if (c.Kind == CustomGlyphCommandKind.Fill)
                    {
                        Setup((rgba & 0xFFFFFF00) | (uint)Math.Round((rgba & 0xFF) * c.A), SKPaintStyle.Fill, 0, antialias: true);
                    }
                    else
                    {
                        Setup(rgba, SKPaintStyle.Stroke, (float)c.A, antialias: true);
                    }

                    _canvas.DrawPath(skPath, _paint);
                    skPath.Dispose();
                    skPath = new SKPath { FillType = SKPathFillType.Winding };
                    break;
            }
        }

        skPath.Dispose();
        _canvas.RestoreToCount(saved);
    }

    // The period is half a cell and the phase comes from the absolute x, so the wave continues across runs.
    private void DrawCurly(float left, float right, float y, float thickness, uint rgba, float period)
    {
        float amplitude = thickness;
        float start = (float)(Math.Floor(left / period) * period);
        float half = period / 2;
        using var path = new SKPath();
        path.MoveTo(start, y);
        for (float px = start; px < right; px += period)
        {
            // One full sine period = two half waves (down then up), each a cubic curve.
            path.CubicTo(px + (half / 3), y + amplitude, px + (half * 2 / 3), y + amplitude, px + half, y);
            path.CubicTo(px + half + (half / 3), y - amplitude, px + half + (half * 2 / 3), y - amplitude, px + period, y);
        }

        int saved = _canvas.Save();
        _canvas.ClipRect(new SKRect(left, y - amplitude - thickness, right, y + amplitude + thickness));
        Setup(rgba, SKPaintStyle.Stroke, thickness, antialias: true);
        _canvas.DrawPath(path, _paint);
        _canvas.RestoreToCount(saved);
    }

    private void DrawBlob(string text, double x, double top, SKTypeface typeface, in TextRunStyle style, in CellMetrics metrics, bool positioned)
    {
        var key = new SkiaTextBlobKey(text, style.Bold, style.Italic, typeface.FamilyName, _font.Size, metrics.Width);
        double cellWidth = metrics.Width;
        SKTextBlob? blob = _cache.GetOrCreate(key, () => CreateBlob(text, typeface, cellWidth, positioned));
        if (blob is not null)
        {
            // The blob origin is the baseline, which includes the extra line height.
            _canvas.DrawText(blob, (float)x, (float)(top + metrics.Baseline), _paint);
        }
    }

    private SKTextBlob? CreateBlob(string text, SKTypeface typeface, double cellWidth, bool positioned)
    {
        using var skFont = new SKFont(typeface, (float)_font.Size) { Subpixel = true, Edging = SKFontEdging.SubpixelAntialias };
        if (!positioned)
        {
            return SKTextBlob.Create(text, skFont);
        }

        // ASCII fast path: one glyph per cell, positioned on the grid whatever the font advance is.
        using var builder = new SKTextBlobBuilder();
        SKHorizontalRunBuffer run = builder.AllocateHorizontalRun(skFont, text.Length, 0);
        skFont.GetGlyphs(text).CopyTo(run.Glyphs);
        Span<float> positions = run.Positions;
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = (float)(i * cellWidth);
        }

        return builder.Build();
    }

    private void Setup(uint rgba, SKPaintStyle style, float strokeWidth, bool antialias)
    {
        _paint.Color = ToColor(rgba);
        _paint.Style = style;
        _paint.StrokeWidth = strokeWidth;
        _paint.IsAntialias = antialias;
    }

    private static bool IsPrintableAscii(string text)
    {
        foreach (char c in text)
        {
            if (c < 0x20 || c > 0x7E)
            {
                return false;
            }
        }

        return true;
    }

    // shortcut: a coarse East Asian Wide / emoji range check (Core's width tables are internal); refine when a grapheme shows a wrong width.
    private static int CellWidth(int cp)
    {
        bool wide = cp is >= 0x1100 and <= 0x115F or >= 0x2E80 and <= 0xA4CF or >= 0xAC00 and <= 0xD7A3
            or >= 0xF900 and <= 0xFAFF or >= 0xFE30 and <= 0xFE6F or >= 0xFF00 and <= 0xFF60 or >= 0xFFE0 and <= 0xFFE6
            or >= 0x1F300 and <= 0x1F64F or >= 0x1F900 and <= 0x1F9FF or >= 0x20000 and <= 0x3FFFD;
        return wide ? 2 : 1;
    }

    private static SKColor ToColor(uint rgba) => new((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);
}
