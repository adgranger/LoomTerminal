// Ported from xtermjs/xterm.js: src/common/Color.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Globalization;
using System.Text.RegularExpressions;

namespace LoomTerminal.Core.Common;

/// <summary>A color: its CSS representation and its 32-bit <c>rgba</c> value (one byte per channel, red first).</summary>
/// <param name="Css">The CSS string (<c>#rrggbb[aa]</c>).</param>
/// <param name="Rgba">The color as <c>r &lt;&lt; 24 | g &lt;&lt; 16 | b &lt;&lt; 8 | a</c>.</param>
internal readonly record struct TerminalColor(string Css, uint Rgba)
{
    /// <summary>The fully transparent black.</summary>
    public static readonly TerminalColor Null = new("#00000000", 0);
}

/// <summary>Helper functions where the source type is "channels" (individual color channels as numbers).</summary>
/// <remarks>Threading: stateless; safe to use from any thread.</remarks>
internal static class Channels
{
    /// <summary>Converts channels to a <c>#rrggbb[aa]</c> string (the alpha is included when given).</summary>
    public static string ToCss(int r, int g, int b, int? a = null) =>
        a is { } alpha
            ? $"#{ColorMath.ToPaddedHex(r)}{ColorMath.ToPaddedHex(g)}{ColorMath.ToPaddedHex(b)}{ColorMath.ToPaddedHex(alpha)}"
            : $"#{ColorMath.ToPaddedHex(r)}{ColorMath.ToPaddedHex(g)}{ColorMath.ToPaddedHex(b)}";

    /// <summary>
    /// Converts channels to an RGBA32 (big endian) number; it must be converted to ABGR32 on little endian systems
    /// before direct 32-bit buffer writes.
    /// </summary>
    public static uint ToRgba(int r, int g, int b, int a = 0xFF) =>
        ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8) | (uint)a;

    /// <summary>Converts channels to a <see cref="TerminalColor"/>.</summary>
    public static TerminalColor ToColor(int r, int g, int b, int? a = null) =>
        new(ToCss(r, g, b, a), ToRgba(r, g, b, a ?? 0xFF));
}

/// <summary>Helper functions where the source type is <see cref="TerminalColor"/> (JS namespace <c>color</c>).</summary>
/// <remarks>Threading: stateless; safe to use from any thread.</remarks>
internal static class ColorOps
{
    /// <summary>Blends <paramref name="fg"/> over <paramref name="bg"/> according to the alpha of <paramref name="fg"/>.</summary>
    public static TerminalColor Blend(TerminalColor bg, TerminalColor fg)
    {
        double a = (fg.Rgba & 0xFF) / 255.0;
        if (a == 1)
        {
            return new TerminalColor(fg.Css, fg.Rgba);
        }

        int fgR = (int)((fg.Rgba >> 24) & 0xFF);
        int fgG = (int)((fg.Rgba >> 16) & 0xFF);
        int fgB = (int)((fg.Rgba >> 8) & 0xFF);
        int bgR = (int)((bg.Rgba >> 24) & 0xFF);
        int bgG = (int)((bg.Rgba >> 16) & 0xFF);
        int bgB = (int)((bg.Rgba >> 8) & 0xFF);
        int r = bgR + (int)ColorMath.Round((fgR - bgR) * a);
        int g = bgG + (int)ColorMath.Round((fgG - bgG) * a);
        int b = bgB + (int)ColorMath.Round((fgB - bgB) * a);
        return new TerminalColor(Channels.ToCss(r, g, b), Channels.ToRgba(r, g, b));
    }

    /// <summary>Whether the color is fully opaque.</summary>
    public static bool IsOpaque(TerminalColor color) => (color.Rgba & 0xFF) == 0xFF;

    /// <summary>
    /// Ensures the contrast ratio between <paramref name="fg"/> and <paramref name="bg"/>; <see langword="null"/> if it
    /// is already met (see <see cref="Rgba.EnsureContrastRatio"/>).
    /// </summary>
    public static TerminalColor? EnsureContrastRatio(TerminalColor bg, TerminalColor fg, double ratio)
    {
        uint? result = Rgba.EnsureContrastRatio(bg.Rgba, fg.Rgba, ratio);
        if (result is not { } value)
        {
            return null;
        }

        return Channels.ToColor((int)((value >> 24) & 0xFF), (int)((value >> 16) & 0xFF), (int)((value >> 8) & 0xFF));
    }

    /// <summary>The color with an alpha of 0xFF.</summary>
    public static TerminalColor Opaque(TerminalColor color)
    {
        uint rgbaColor = color.Rgba | 0xFF;
        (int r, int g, int b, _) = Rgba.ToChannels(rgbaColor);
        return new TerminalColor(Channels.ToCss(r, g, b), rgbaColor);
    }

    /// <summary>The color with the given opacity (0 to 1).</summary>
    public static TerminalColor Opacity(TerminalColor color, double opacity)
    {
        int a = (int)ColorMath.Round(opacity * 0xFF);
        (int r, int g, int b, _) = Rgba.ToChannels(color.Rgba);
        return new TerminalColor(Channels.ToCss(r, g, b, a), Channels.ToRgba(r, g, b, a));
    }

    /// <summary>The color with its opacity multiplied by <paramref name="factor"/>.</summary>
    public static TerminalColor MultiplyOpacity(TerminalColor color, double factor)
    {
        uint a = color.Rgba & 0xFF;
        return Opacity(color, a * factor / 0xFF);
    }

    /// <summary>The red, green and blue channels of the color.</summary>
    public static (int R, int G, int B) ToColorRgb(TerminalColor color) =>
        ((int)((color.Rgba >> 24) & 0xFF), (int)((color.Rgba >> 16) & 0xFF), (int)((color.Rgba >> 8) & 0xFF));
}

/// <summary>Helper functions where the source type is "css" (<c>#rgb[a]</c>, <c>#rrggbb[aa]</c>, <c>rgb()</c>, <c>rgba()</c>).</summary>
/// <remarks>Threading: stateless; safe to use from any thread.</remarks>
internal static partial class Css
{
    /// <summary>
    /// Converts a css string to a <see cref="TerminalColor"/>; throws <see cref="ArgumentException"/> if it is invalid.
    /// The ideal format is <c>#rrggbb[aa]</c>, the fastest to parse. Only <c>#rgb[a]</c>, <c>#rrggbb[aa]</c>,
    /// <c>rgb()</c> and <c>rgba()</c> are supported: xterm.js falls back to a canvas for other syntaxes, which does
    /// not exist here.
    /// </summary>
    public static TerminalColor ToColor(string css)
    {
        // Formats: #rgb[a] and #rrggbb[aa]
        if (HexRegex().IsMatch(css))
        {
            switch (css.Length)
            {
                case 4: // #rgb
                    return Channels.ToColor(Doubled(css, 1), Doubled(css, 2), Doubled(css, 3));
                case 5: // #rgba
                    return Channels.ToColor(Doubled(css, 1), Doubled(css, 2), Doubled(css, 3), Doubled(css, 4));
                case 7: // #rrggbb
                    return new TerminalColor(css, (ParseHex(css.AsSpan(1)) << 8) | 0xFF);
                case 9: // #rrggbbaa
                    return new TerminalColor(css, ParseHex(css.AsSpan(1)));
            }
        }

        // Formats: rgb() or rgba()
        Match rgbaMatch = RgbaRegex().Match(css);
        if (rgbaMatch.Success)
        {
            int r = int.Parse(rgbaMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            int g = int.Parse(rgbaMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            int b = int.Parse(rgbaMatch.Groups[3].Value, CultureInfo.InvariantCulture);
            double alpha = rgbaMatch.Groups[5].Success
                ? double.Parse(rgbaMatch.Groups[5].Value, CultureInfo.InvariantCulture)
                : 1;
            return Channels.ToColor(r, g, b, (int)ColorMath.Round(alpha * 0xFF));
        }

        throw new ArgumentException("css.toColor: Unsupported css format");
    }

    [GeneratedRegex("#[0-9a-fA-F]{3,8}")]
    private static partial Regex HexRegex();

    [GeneratedRegex(@"rgba?\(\s*([0-9]{1,3})\s*,\s*([0-9]{1,3})\s*,\s*([0-9]{1,3})\s*(,\s*(0|1|[0-9]?\.([0-9]+))\s*)?\)")]
    private static partial Regex RgbaRegex();

    // parseInt(css.slice(i, i + 1).repeat(2), 16)
    private static int Doubled(string css, int index) => (int)ParseHex(new string(css[index], 2));

    // parseInt(value, 16): the leading hex digits (a JS NaN is 0 in the bit operations that follow)
    private static uint ParseHex(ReadOnlySpan<char> value)
    {
        uint result = 0;
        foreach (char c in value)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                break;
            }

            result = (result << 4) | (uint)(c <= '9' ? c - '0' : (c | 0x20) - 'a' + 10);
        }

        return result;
    }
}

/// <summary>Helper functions where the source type is "rgb" (number: <c>0xrrggbb</c>).</summary>
/// <remarks>Threading: stateless; safe to use from any thread.</remarks>
internal static class Rgb
{
    /// <summary>
    /// The relative luminance of an RGB color, useful to determine the contrast ratio between two colors.
    /// See https://www.w3.org/TR/WCAG20/#relativeluminancedef.
    /// </summary>
    public static double RelativeLuminance(uint rgb) =>
        RelativeLuminance2((int)((rgb >> 16) & 0xFF), (int)((rgb >> 8) & 0xFF), (int)(rgb & 0xFF));

    /// <summary>The relative luminance of an RGB color given as its three channels (0x00 to 0xFF).</summary>
    public static double RelativeLuminance2(int r, int g, int b)
    {
        double rs = r / 255.0;
        double gs = g / 255.0;
        double bs = b / 255.0;
        double rr = rs <= 0.03928 ? rs / 12.92 : Math.Pow((rs + 0.055) / 1.055, 2.4);
        double rg = gs <= 0.03928 ? gs / 12.92 : Math.Pow((gs + 0.055) / 1.055, 2.4);
        double rb = bs <= 0.03928 ? bs / 12.92 : Math.Pow((bs + 0.055) / 1.055, 2.4);
        return (rr * 0.2126) + (rg * 0.7152) + (rb * 0.0722);
    }
}

/// <summary>Helper functions where the source type is "rgba" (number: <c>0xrrggbbaa</c>).</summary>
/// <remarks>Threading: stateless; safe to use from any thread.</remarks>
internal static class Rgba
{
    /// <summary>Blends <paramref name="fg"/> over <paramref name="bg"/> according to the alpha of <paramref name="fg"/>.</summary>
    public static uint Blend(uint bg, uint fg)
    {
        double a = (fg & 0xFF) / 255.0;
        if (a == 1)
        {
            return fg;
        }

        int fgR = (int)((fg >> 24) & 0xFF);
        int fgG = (int)((fg >> 16) & 0xFF);
        int fgB = (int)((fg >> 8) & 0xFF);
        int bgR = (int)((bg >> 24) & 0xFF);
        int bgG = (int)((bg >> 16) & 0xFF);
        int bgB = (int)((bg >> 8) & 0xFF);
        int r = bgR + (int)ColorMath.Round((fgR - bgR) * a);
        int g = bgG + (int)ColorMath.Round((fgG - bgG) * a);
        int b = bgB + (int)ColorMath.Round((fgB - bgB) * a);
        return Channels.ToRgba(r, g, b);
    }

    /// <summary>
    /// Given a foreground and a background color, either increases or reduces the luminance of the foreground until the
    /// contrast ratio is met. If pure white or black is hit without meeting it, goes the other direction and takes
    /// either the first or second result, whichever has the higher contrast ratio. Returns <see langword="null"/> if
    /// the contrast ratio is already met.
    /// </summary>
    public static uint? EnsureContrastRatio(uint bgRgba, uint fgRgba, double ratio)
    {
        double bgL = Rgb.RelativeLuminance(bgRgba >> 8);
        double fgL = Rgb.RelativeLuminance(fgRgba >> 8);
        double cr = ColorMath.ContrastRatio(bgL, fgL);
        if (cr >= ratio)
        {
            return null;
        }

        if (fgL < bgL)
        {
            uint resultA = ReduceLuminance(bgRgba, fgRgba, ratio);
            double resultARatio = ColorMath.ContrastRatio(bgL, Rgb.RelativeLuminance(resultA >> 8));
            if (resultARatio < ratio)
            {
                uint resultB = IncreaseLuminance(bgRgba, fgRgba, ratio);
                double resultBRatio = ColorMath.ContrastRatio(bgL, Rgb.RelativeLuminance(resultB >> 8));
                return resultARatio > resultBRatio ? resultA : resultB;
            }

            return resultA;
        }

        uint resultA2 = IncreaseLuminance(bgRgba, fgRgba, ratio);
        double resultA2Ratio = ColorMath.ContrastRatio(bgL, Rgb.RelativeLuminance(resultA2 >> 8));
        if (resultA2Ratio < ratio)
        {
            uint resultB = ReduceLuminance(bgRgba, fgRgba, ratio);
            double resultBRatio = ColorMath.ContrastRatio(bgL, Rgb.RelativeLuminance(resultB >> 8));
            return resultA2Ratio > resultBRatio ? resultA2 : resultB;
        }

        return resultA2;
    }

    /// <summary>Reduces the luminance of the foreground by 10% steps until the ratio is hit (naive but fast).</summary>
    public static uint ReduceLuminance(uint bgRgba, uint fgRgba, double ratio)
    {
        int bgR = (int)((bgRgba >> 24) & 0xFF);
        int bgG = (int)((bgRgba >> 16) & 0xFF);
        int bgB = (int)((bgRgba >> 8) & 0xFF);
        int fgR = (int)((fgRgba >> 24) & 0xFF);
        int fgG = (int)((fgRgba >> 16) & 0xFF);
        int fgB = (int)((fgRgba >> 8) & 0xFF);
        double cr = ColorMath.ContrastRatio(Rgb.RelativeLuminance2(fgR, fgG, fgB), Rgb.RelativeLuminance2(bgR, bgG, bgB));
        while (cr < ratio && (fgR > 0 || fgG > 0 || fgB > 0))
        {
            // Reduce by 10% until the ratio is hit
            fgR -= Math.Max(0, (int)Math.Ceiling(fgR * 0.1));
            fgG -= Math.Max(0, (int)Math.Ceiling(fgG * 0.1));
            fgB -= Math.Max(0, (int)Math.Ceiling(fgB * 0.1));
            cr = ColorMath.ContrastRatio(Rgb.RelativeLuminance2(fgR, fgG, fgB), Rgb.RelativeLuminance2(bgR, bgG, bgB));
        }

        return Channels.ToRgba(fgR, fgG, fgB);
    }

    /// <summary>Increases the luminance of the foreground by 10% steps until the ratio is hit (naive but fast).</summary>
    public static uint IncreaseLuminance(uint bgRgba, uint fgRgba, double ratio)
    {
        int bgR = (int)((bgRgba >> 24) & 0xFF);
        int bgG = (int)((bgRgba >> 16) & 0xFF);
        int bgB = (int)((bgRgba >> 8) & 0xFF);
        int fgR = (int)((fgRgba >> 24) & 0xFF);
        int fgG = (int)((fgRgba >> 16) & 0xFF);
        int fgB = (int)((fgRgba >> 8) & 0xFF);
        double cr = ColorMath.ContrastRatio(Rgb.RelativeLuminance2(fgR, fgG, fgB), Rgb.RelativeLuminance2(bgR, bgG, bgB));
        while (cr < ratio && (fgR < 0xFF || fgG < 0xFF || fgB < 0xFF))
        {
            // Increase by 10% until the ratio is hit
            fgR = Math.Min(0xFF, fgR + (int)Math.Ceiling((255 - fgR) * 0.1));
            fgG = Math.Min(0xFF, fgG + (int)Math.Ceiling((255 - fgG) * 0.1));
            fgB = Math.Min(0xFF, fgB + (int)Math.Ceiling((255 - fgB) * 0.1));
            cr = ColorMath.ContrastRatio(Rgb.RelativeLuminance2(fgR, fgG, fgB), Rgb.RelativeLuminance2(bgR, bgG, bgB));
        }

        return Channels.ToRgba(fgR, fgG, fgB);
    }

    /// <summary>Splits an rgba number into its four channels.</summary>
    public static (int R, int G, int B, int A) ToChannels(uint value) =>
        ((int)((value >> 24) & 0xFF), (int)((value >> 16) & 0xFF), (int)((value >> 8) & 0xFF), (int)(value & 0xFF));
}

/// <summary>Number formatting and contrast helpers of <c>Color.ts</c>.</summary>
/// <remarks>Threading: stateless; safe to use from any thread.</remarks>
internal static class ColorMath
{
    /// <summary>Converts a number to a 2-digit (at least) lowercase hex string.</summary>
    public static string ToPaddedHex(int c) => c.ToString("x2", CultureInfo.InvariantCulture);

    /// <summary>
    /// The contrast ratio between two relative luminance values.
    /// See https://www.w3.org/TR/WCAG20/#contrast-ratiodef.
    /// </summary>
    public static double ContrastRatio(double l1, double l2) =>
        l1 < l2 ? (l2 + 0.05) / (l1 + 0.05) : (l1 + 0.05) / (l2 + 0.05);

    /// <summary>JS <c>Math.round</c>: halves round up (towards positive infinity).</summary>
    internal static double Round(double value) => Math.Floor(value + 0.5);
}
