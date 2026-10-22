// Inspired by LoomEditor: src/LoomEditor.Rendering/Win2D/Win2DRenderSurface.cs
using System.Globalization;
using LoomTerminal.Presentation.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI;
using Windows.UI.Text;

namespace LoomTerminal.WinUI.Rendering;

/// <summary><see cref="IRenderSurface"/> on a Win2D drawing session (Direct2D + DirectWrite).</summary>
/// <remarks>
/// Threading: create one per frame inside the <c>CanvasControl.Draw</c> handler; it does not own the session and is
/// only valid during that call, on that thread. Underlines come in P4-05.
/// </remarks>
internal sealed class Win2DRenderSurface : IRenderSurface
{
    private readonly CanvasDrawingSession _session;
    private readonly ICanvasResourceCreator _resourceCreator;
    private readonly TextRunLayoutCache _cache;
    private readonly TerminalFont _font;
    private readonly string _family;
    private readonly double _scale;

    /// <summary>Wraps a drawing session.</summary>
    /// <param name="session">The session of the current draw call.</param>
    /// <param name="resourceCreator">The device used to create text layouts.</param>
    /// <param name="cache">The layout cache shared between frames.</param>
    /// <param name="font">The terminal font.</param>
    /// <param name="family">The resolved family (<see cref="Win2DTextMeasurer.ResolveFamily"/>).</param>
    /// <param name="scale">Rasterization scale.</param>
    public Win2DRenderSurface(CanvasDrawingSession session, ICanvasResourceCreator resourceCreator, TextRunLayoutCache cache, TerminalFont font, string family, double scale)
    {
        _session = session;
        _resourceCreator = resourceCreator;
        _cache = cache;
        _font = font;
        _family = family;
        _scale = scale;
    }

    /// <inheritdoc />
    public void Clear(uint rgba) => _session.Clear(ToColor(rgba));

    /// <inheritdoc />
    public void FillRectangle(double x, double y, double width, double height, uint rgba) =>
        _session.FillRectangle((float)x, (float)y, (float)width, (float)height, ToColor(rgba));

    /// <inheritdoc />
    public void DrawTextRun(int row, int column, string text, int cellCount, in TextRunStyle style, in CellMetrics metrics)
    {
        var color = ToColor(style.Foreground);
        var top = row * metrics.Height;
        if (IsPrintableAscii(text))
        {
            DrawLayout(text, column * metrics.Width, top, metrics, style, color);
            return;
        }

        // Anything else goes one grapheme per cell position so wide and fallback glyphs never drift off the grid.
        var cell = column;
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            var grapheme = (string)enumerator.Current;
            DrawLayout(grapheme, cell * metrics.Width, top, metrics, style, color);
            cell += CellWidth(grapheme);
        }
    }

    /// <inheritdoc />
    public void DrawUnderline(double x, double y, double width, TerminalUnderlineStyle style, uint rgba, in CellMetrics metrics)
    {
        // P4-05
    }

    /// <inheritdoc />
    public void DrawLine(double x1, double y1, double x2, double y2, double thickness, uint rgba) =>
        _session.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, ToColor(rgba), (float)thickness);

    /// <inheritdoc />
    public void DrawRectangle(double x, double y, double width, double height, double thickness, uint rgba) =>
        _session.DrawRectangle((float)x, (float)y, (float)width, (float)height, ToColor(rgba), (float)thickness);

    /// <inheritdoc />
    public void DrawCustomGlyph(CustomGlyphPath path, double x, double y, in CellMetrics metrics, uint rgba)
    {
        // The path is relative to the cell: translate by the cell origin; the clip hides powerline overshoot.
        var saved = _session.Transform;
        _session.Transform = System.Numerics.Matrix3x2.CreateTranslation((float)x, (float)y) * saved;
        using var clip = path.ClipToCell ? _session.CreateLayer(1f, CanvasGeometry.CreateRectangle(_resourceCreator, 0, 0, (float)metrics.Width, (float)metrics.Height)) : null;
        var builder = new CanvasPathBuilder(_resourceCreator);
        var open = false;
        foreach (var c in path.Commands)
        {
            switch (c.Kind)
            {
                case CustomGlyphCommandKind.MoveTo:
                    if (open)
                    {
                        builder.EndFigure(CanvasFigureLoop.Open);
                    }

                    builder.BeginFigure((float)c.A, (float)c.B);
                    open = true;
                    break;
                case CustomGlyphCommandKind.LineTo:
                    builder.AddLine((float)c.A, (float)c.B);
                    break;
                case CustomGlyphCommandKind.CurveTo:
                    builder.AddCubicBezier(new((float)c.A, (float)c.B), new((float)c.C, (float)c.D), new((float)c.E, (float)c.F));
                    break;
                case CustomGlyphCommandKind.Rect:
                    if (open)
                    {
                        builder.EndFigure(CanvasFigureLoop.Open);
                        open = false;
                    }

                    builder.BeginFigure((float)c.A, (float)c.B);
                    builder.AddLine((float)(c.A + c.C), (float)c.B);
                    builder.AddLine((float)(c.A + c.C), (float)(c.B + c.D));
                    builder.AddLine((float)c.A, (float)(c.B + c.D));
                    builder.EndFigure(CanvasFigureLoop.Closed);
                    break;
                default:
                    if (open)
                    {
                        builder.EndFigure(CanvasFigureLoop.Open);
                        open = false;
                    }

                    using (var geometry = CanvasGeometry.CreatePath(builder))
                    {
                        if (c.Kind == CustomGlyphCommandKind.Fill)
                        {
                            var color = ToColor(rgba);
                            color.A = (byte)Math.Round(color.A * c.A);
                            _session.FillGeometry(geometry, color);
                        }
                        else
                        {
                            _session.DrawGeometry(geometry, ToColor(rgba), (float)c.A);
                        }
                    }

                    builder = new CanvasPathBuilder(_resourceCreator);
                    break;
            }
        }

        _session.Transform = saved;
    }

    private void DrawLayout(string text, double x, double top, in CellMetrics metrics, in TextRunStyle style, Color color)
    {
        var key = new TextRunLayoutKey(text, style.Bold, style.Italic, _family, _font.Size, _scale);
        var bold = style.Bold;
        var italic = style.Italic;
        var layout = _cache.GetOrCreate(key, () => CreateLayout(text, bold, italic));
        // Align the layout baseline on the cell baseline (which includes the extra line height).
        var y = top + metrics.Baseline - layout.LineMetrics[0].Baseline;
        _session.DrawTextLayout(layout, (float)x, (float)y, color);
    }

    private CanvasTextLayout CreateLayout(string text, bool bold, bool italic)
    {
        using var format = new CanvasTextFormat
        {
            FontFamily = _family,
            FontSize = (float)_font.Size,
            FontWeight = new FontWeight { Weight = (ushort)(bold ? _font.BoldWeight : _font.Weight) },
            FontStyle = italic ? FontStyle.Italic : FontStyle.Normal,
            WordWrapping = CanvasWordWrapping.NoWrap,
            Options = CanvasDrawTextOptions.EnableColorFont,
        };
        return new CanvasTextLayout(_resourceCreator, text, format, float.MaxValue, float.MaxValue);
    }

    private static bool IsPrintableAscii(string text)
    {
        foreach (var c in text)
        {
            if (c < 0x20 || c > 0x7E)
            {
                return false;
            }
        }

        return true;
    }

    // shortcut: a coarse East Asian Wide / emoji range check (Core's width tables are internal); refine when a grapheme shows a wrong width.
    private static int CellWidth(string grapheme)
    {
        var cp = char.ConvertToUtf32(grapheme, 0);
        var wide = cp is >= 0x1100 and <= 0x115F or >= 0x2E80 and <= 0xA4CF or >= 0xAC00 and <= 0xD7A3
            or >= 0xF900 and <= 0xFAFF or >= 0xFE30 and <= 0xFE6F or >= 0xFF00 and <= 0xFF60 or >= 0xFFE0 and <= 0xFFE6
            or >= 0x1F300 and <= 0x1F64F or >= 0x1F900 and <= 0x1F9FF or >= 0x20000 and <= 0x3FFFD;
        return wide ? 2 : 1;
    }

    private static Color ToColor(uint rgba) =>
        Color.FromArgb((byte)(rgba & 0xFF), (byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8));
}
