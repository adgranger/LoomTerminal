// Ported from xtermjs/xterm.js: src/common/buffer/CellData.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Input;

namespace LoomTerminal.Core.Buffer;

/// <summary>A single cell in the terminal buffer: the content word, the attributes and the combined string.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class CellData : AttributeData
{
    /// <summary>The content word: codepoint, combined flag and width (see <see cref="Buffer.Content"/>).</summary>
    public uint Content;

    /// <summary>The string of a combined cell (only meaningful when <see cref="IsCombined"/> is non-zero).</summary>
    public string CombinedData = string.Empty;

    /// <summary>Creates a <see cref="CellData"/> from a <see cref="CharData"/>.</summary>
    public static CellData FromCharData(CharData value)
    {
        CellData obj = new();
        obj.SetFromCharData(value);
        return obj;
    }

    /// <summary>Whether the cell contains a combined string (the masked bit, non-zero means true).</summary>
    public uint IsCombined() => Content & Buffer.Content.IsCombinedMask;

    /// <summary>The width of the cell.</summary>
    public int GetWidth() => unchecked((int)Content) >> Buffer.Content.WidthShift;

    /// <summary>The string of the content.</summary>
    public string GetChars()
    {
        if ((Content & Buffer.Content.IsCombinedMask) != 0)
        {
            return CombinedData;
        }

        if ((Content & Buffer.Content.CodepointMask) != 0)
        {
            return TextDecoder.StringFromCodePoint(Content & Buffer.Content.CodepointMask);
        }

        return string.Empty;
    }

    /// <summary>
    /// The codepoint of the cell. For single chars this is the UTF32 codepoint; for a combined string it is the code of
    /// the last char of the string, to be in line with the code in <see cref="CharData"/>.
    /// </summary>
    public uint GetCode() =>
        IsCombined() != 0
            ? CombinedData[^1]
            : Content & Buffer.Content.CodepointMask;

    /// <summary>Sets the data from a <see cref="CharData"/>.</summary>
    public void SetFromCharData(CharData value)
    {
        Fg = value.Attr;
        Bg = 0;
        bool combined = false;

        // surrogates and combined strings need special treatment
        if (value.Char.Length > 2)
        {
            combined = true;
        }
        else if (value.Char.Length == 2)
        {
            int code = value.Char[0];

            // if the 2-char string is a surrogate create single codepoint, everything else is combined
            if (0xD800 <= code && code <= 0xDBFF)
            {
                int second = value.Char[1];
                if (0xDC00 <= second && second <= 0xDFFF)
                {
                    Content = (uint)(((code - 0xD800) * 0x400) + second - 0xDC00 + 0x10000) | ((uint)value.Width << Buffer.Content.WidthShift);
                }
                else
                {
                    combined = true;
                }
            }
            else
            {
                combined = true;
            }
        }
        else
        {
            // JS: ''.charCodeAt(0) is NaN, which is 0 in a bit operation
            uint code = value.Char.Length == 1 ? value.Char[0] : 0u;
            Content = code | ((uint)value.Width << Buffer.Content.WidthShift);
        }

        if (combined)
        {
            CombinedData = value.Char;
            Content = Buffer.Content.IsCombinedMask | ((uint)value.Width << Buffer.Content.WidthShift);
        }
    }

    /// <summary>Gets the data as <see cref="CharData"/>.</summary>
    public CharData GetAsCharData() => new(Fg, GetChars(), GetWidth(), GetCode());
}
