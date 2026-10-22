// Inspired by LoomEditor: src/LoomEditor.Rendering/Win2D/Win2DTextMeasurer.cs
using LoomTerminal.Presentation.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI.Text;

namespace LoomTerminal.WinUI.Rendering;

/// <summary>Measures terminal cells with DirectWrite through Win2D (P3-20 <see cref="ITextMeasurer"/>).</summary>
/// <remarks>Threading: UI thread only. Results are cached per (font, scale); call <see cref="Invalidate"/> on a DPI change.</remarks>
internal sealed class Win2DTextMeasurer : ITextMeasurer
{
    private const int SampleLength = 50;
    private readonly ICanvasResourceCreator _resourceCreator;
    private readonly Dictionary<(TerminalFont Font, double Scale), CellMetrics> _cache = new();

    /// <summary>Creates a measurer on a Win2D device or control.</summary>
    /// <param name="resourceCreator">The device used to lay out the sample text.</param>
    public Win2DTextMeasurer(ICanvasResourceCreator resourceCreator)
    {
        ArgumentNullException.ThrowIfNull(resourceCreator);
        _resourceCreator = resourceCreator;
    }

    /// <summary>Resolves the installed family used to draw <paramref name="font"/>.</summary>
    /// <param name="font">The font.</param>
    /// <returns>The first installed family of the list, else Cascadia Mono, else Consolas.</returns>
    public static string ResolveFamily(TerminalFont font)
    {
        ArgumentNullException.ThrowIfNull(font);
        var installed = new HashSet<string>(CanvasTextFormat.GetSystemFontFamilies(), StringComparer.OrdinalIgnoreCase);
        foreach (string family in font.Families)
        {
            if (installed.Contains(family))
            {
                return family;
            }
        }

        return installed.Contains("Cascadia Mono") ? "Cascadia Mono" : "Consolas";
    }

    /// <inheritdoc />
    public CellMetrics Measure(TerminalFont font, double rasterizationScale)
    {
        ArgumentNullException.ThrowIfNull(font);
        if (_cache.TryGetValue((font, rasterizationScale), out CellMetrics cached))
        {
            return cached;
        }

        CellMetrics metrics = Compute(font, rasterizationScale);
        _cache[(font, rasterizationScale)] = metrics;
        return metrics;
    }

    /// <summary>Drops the cache (DPI or device change).</summary>
    public void Invalidate() => _cache.Clear();

    private CellMetrics Compute(TerminalFont font, double scale)
    {
        using var format = new CanvasTextFormat
        {
            FontFamily = ResolveFamily(font),
            FontSize = (float)font.Size,
            FontWeight = new FontWeight { Weight = (ushort)font.Weight },
            WordWrapping = CanvasWordWrapping.NoWrap,
        };

        // xterm.js measures a long string of "W" and divides, to average rounding errors.
        string sample = new('W', SampleLength);
        using var layout = new CanvasTextLayout(_resourceCreator, sample, format, float.MaxValue, float.MaxValue);
        double advance = layout.GetCaretPosition(SampleLength, false).X / SampleLength;

        CanvasLineMetrics line = layout.LineMetrics[0];
        double lineHeight = line.Height;
        double baseline = line.Baseline;

        double width = Snap(advance + font.LetterSpacing, scale);
        double height = Snap(Math.Ceiling(lineHeight * font.LineHeight * scale) / scale, scale);
        // Extra line height is split around the text, like CSS line-height.
        double ascent = Snap(baseline + ((height - lineHeight) / 2), scale);

        // Underline and strikethrough use the usual typographic proportions of the em box
        // (DirectWrite font-face metrics are not exposed by Win2D for a text format).
        double thickness = Math.Max(1 / scale, Snap(font.Size / 14, scale));
        double underline = Snap(ascent + (font.Size * 0.1), scale);
        double strike = Snap(ascent - (font.Size * 0.3), scale);

        return new CellMetrics(width, height, ascent, underline, thickness, strike, thickness);
    }

    private static double Snap(double value, double scale) => Math.Round(value * scale) / scale;
}
