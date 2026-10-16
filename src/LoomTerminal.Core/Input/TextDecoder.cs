// Ported from xtermjs/xterm.js: src/common/input/TextDecoder.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Text;

namespace LoomTerminal.Core.Input;

/// <summary>Conversions between UTF-32 code points and UTF-16 strings.</summary>
/// <remarks>Threading: stateless and thread-safe.</remarks>
internal static class TextDecoder
{
    /// <summary>
    /// Converts a UTF-32 code point into a string. Skips the sanity checks of <c>char.ConvertFromUtf32</c>: the input
    /// is always legal UTF-32 (granted by the input decoders).
    /// </summary>
    /// <param name="codePoint">The code point.</param>
    public static string StringFromCodePoint(uint codePoint)
    {
        if (codePoint > 0xFFFF)
        {
            codePoint -= 0x10000;
            return new string([(char)((codePoint >> 10) + 0xD800), (char)((codePoint % 0x400) + 0xDC00)]);
        }

        return ((char)codePoint).ToString();
    }

    /// <summary>Converts UTF-32 code points into a string, like <see cref="StringFromCodePoint"/> in a loop.</summary>
    /// <param name="data">The code points.</param>
    /// <param name="start">The index of the first code point.</param>
    /// <param name="end">The index after the last code point; <c>-1</c> means <c>data.Length</c>.</param>
    public static string Utf32ToString(ReadOnlySpan<uint> data, int start = 0, int end = -1)
    {
        if (end == -1)
        {
            end = data.Length;
        }

        StringBuilder result = new();
        for (int i = start; i < end; ++i)
        {
            uint codepoint = data[i];
            if (codepoint > 0xFFFF)
            {
                // A non BMP code point becomes a surrogate pair:
                //  - subtract 0x10000 from the code point, leaving a 20 bit number
                //  - add the high 10 bits to 0xD800  --> first surrogate
                //  - add the low 10 bits to 0xDC00   --> second surrogate
                codepoint -= 0x10000;
                result.Append((char)((codepoint >> 10) + 0xD800)).Append((char)((codepoint % 0x400) + 0xDC00));
            }
            else
            {
                result.Append((char)codepoint);
            }
        }

        return result.ToString();
    }
}
