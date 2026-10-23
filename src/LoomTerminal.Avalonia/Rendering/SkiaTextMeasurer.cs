using LoomTerminal.Presentation.Rendering;
using SkiaSharp;

namespace LoomTerminal.Avalonia.Rendering;

/// <summary>Measures terminal cells with Skia (P3-20 <see cref="ITextMeasurer"/>), following the rules of the WinUI measurer.</summary>
/// <remarks>Threading: UI thread only. Results are cached per (font, scale); call <see cref="Invalidate"/> on a DPI change.</remarks>
internal sealed class SkiaTextMeasurer : ITextMeasurer
{
    private const int SampleLength = 50;
    private static readonly string[] DefaultFallbacks = ["Cascadia Mono", "Consolas", "Menlo", "DejaVu Sans Mono", "monospace"];
    private readonly Dictionary<(TerminalFont Font, double Scale), CellMetrics> _cache = new();

    /// <summary>Returns the first installed family of <paramref name="font"/>, else the first installed default fallback.</summary>
    /// <param name="font">The font.</param>
    /// <returns>A family name; the last default fallback when nothing is installed (Skia then picks its own default).</returns>
    internal static string ResolveFamily(TerminalFont font)
    {
        ArgumentNullException.ThrowIfNull(font);
        foreach (string name in font.Families.Concat(DefaultFallbacks))
        {
            if (IsInstalled(name))
            {
                return name;
            }
        }

        return DefaultFallbacks[^1];
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

    private static bool IsInstalled(string name) => SKFontManager.Default.MatchFamily(name) is { } face && DisposeAndTrue(face);

    private static bool DisposeAndTrue(SKTypeface face)
    {
        face.Dispose();
        return true;
    }

    private static CellMetrics Compute(TerminalFont font, double scale)
    {
        using var typeface = SKTypeface.FromFamilyName(
            ResolveFamily(font), new SKFontStyle(font.Weight, (int)SKFontStyleWidth.Normal, SKFontStyleSlant.Upright));
        // Measure in device pixels, then go back to DIPs.
        using var skFont = new SKFont(typeface, (float)(font.Size * scale)) { Subpixel = true };

        SKFontMetrics m = skFont.Metrics;
        // xterm.js measures a long string of "W" and divides, to average rounding errors.
        double advance = skFont.MeasureText(new string('W', SampleLength)) / SampleLength / scale;
        double ascentPx = -m.Ascent;
        double lineHeight = (ascentPx + m.Descent + m.Leading) / scale;
        double baseline = ascentPx / scale;

        double width = Snap(advance + font.LetterSpacing, scale);
        double height = Snap(Math.Ceiling(lineHeight * font.LineHeight * scale) / scale, scale);
        // Extra line height is split around the text, like CSS line-height.
        double ascent = Snap(baseline + ((height - lineHeight) / 2), scale);

        double fallbackThickness = Math.Max(1 / scale, Snap(font.Size / 14, scale));
        double underlineThickness = Thickness(m.UnderlineThickness, scale, fallbackThickness);
        double strikeThickness = Thickness(m.StrikeoutThickness, scale, fallbackThickness);
        double underline = Snap(ascent + (m.UnderlinePosition is { } up ? up / scale : font.Size * 0.1), scale);
        double strike = Snap(ascent + (m.StrikeoutPosition is { } sp ? sp / scale : -font.Size * 0.3), scale);

        return new CellMetrics(width, height, ascent, underline, underlineThickness, strike, strikeThickness);
    }

    private static double Thickness(float? reported, double scale, double fallback) =>
        reported is > 0 ? Math.Max(1 / scale, Snap(reported.Value / scale, scale)) : fallback;

    private static double Snap(double value, double scale) => Math.Round(value * scale) / scale;
}
