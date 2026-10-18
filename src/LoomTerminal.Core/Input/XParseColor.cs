// Ported from xtermjs/xterm.js: src/common/input/XParseColor.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Globalization;
using System.Text.RegularExpressions;

namespace LoomTerminal.Core.Input;

/// <summary>Parses and formats X11 color specifications (<c>rgb:RR/GG/BB</c>, <c>#RGB</c>…), as used by OSC 4/10/11/12.</summary>
/// <remarks>Threading: stateless; safe to use from any thread.</remarks>
internal static partial class XParseColor
{
    // 'rgb:' rule - matching: r/g/b | rr/gg/bb | rrr/ggg/bbb | rrrr/gggg/bbbb (hex digits)
    // JS `$` matches only at the very end of the input, like `\z` here.
    [GeneratedRegex(
        @"^([0-9a-f])/([0-9a-f])/([0-9a-f])\z|^([0-9a-f]{2})/([0-9a-f]{2})/([0-9a-f]{2})\z|^([0-9a-f]{3})/([0-9a-f]{3})/([0-9a-f]{3})\z|^([0-9a-f]{4})/([0-9a-f]{4})/([0-9a-f]{4})\z")]
    private static partial Regex RgbRex();

    // '#...' rule - matching any hex digits
    [GeneratedRegex(@"^[0-9a-f]+\z")]
    private static partial Regex HashRex();

    /// <summary>
    /// Parses a color spec to RGB values (8 bit per channel); <see langword="null"/> if it is not supported. See
    /// <c>man xparsecolor</c> for the formats. Supported: <c>rgb:&lt;red&gt;/&lt;green&gt;/&lt;blue&gt;</c> with each
    /// part in h | hh | hhh | hhhh, and <c>#RGB</c>, <c>#RRGGBB</c>, <c>#RRRGGGBBB</c>, <c>#RRRRGGGGBBBB</c>. Other
    /// formats (<c>rgbi:</c>, float numbering, named colors) are not supported.
    /// </summary>
    public static (int R, int G, int B)? ParseColor(string? data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return null;
        }

        // also handle uppercases
        string low = data.ToLowerInvariant();
        if (low.StartsWith("rgb:", StringComparison.Ordinal))
        {
            // 'rgb:' specifier
            low = low[4..];
            Match m = RgbRex().Match(low);
            if (m.Success)
            {
                int offset = m.Groups[1].Success ? 1 : m.Groups[4].Success ? 4 : m.Groups[7].Success ? 7 : 10;
                int baseValue = offset == 1 ? 15 : offset == 4 ? 255 : offset == 7 ? 4095 : 65535;
                return (
                    Scale(m.Groups[offset].Value, baseValue),
                    Scale(m.Groups[offset + 1].Value, baseValue),
                    Scale(m.Groups[offset + 2].Value, baseValue));
            }
        }
        else if (low.StartsWith('#'))
        {
            // '#' specifier
            low = low[1..];
            if (HashRex().IsMatch(low) && low.Length is 3 or 6 or 9 or 12)
            {
                int adv = low.Length / 3;
                int[] result = new int[3];
                for (int i = 0; i < 3; ++i)
                {
                    int c = int.Parse(low.AsSpan(adv * i, adv), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                    result[i] = adv == 1 ? c << 4 : adv == 2 ? c : adv == 3 ? c >> 4 : c >> 8;
                }

                return (result[0], result[1], result[2]);
            }
        }

        // Named colors are currently not supported (as in xterm.js).
        return null;
    }

    /// <summary>Converts a color to a <c>rgb:../../..</c> string of <paramref name="bits"/> depth (4, 8, 12 or 16).</summary>
    public static string ToRgbString((int R, int G, int B) color, int bits = 16)
    {
        (int r, int g, int b) = color;
        return $"rgb:{Pad(r, bits)}/{Pad(g, bits)}/{Pad(b, bits)}";
    }

    private static int Scale(string hex, int baseValue) =>
        (int)Math.Floor(
            (int.Parse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture) / (double)baseValue * 255) + 0.5);

    // pad hex output to requested bit width
    private static string Pad(int n, int bits)
    {
        string s = n.ToString("x", CultureInfo.InvariantCulture);
        string s2 = s.Length < 2 ? "0" + s : s;
        return bits switch
        {
            4 => s[..1],
            8 => s2,
            12 => (s2 + s2)[..3],
            _ => s2 + s2,
        };
    }
}
