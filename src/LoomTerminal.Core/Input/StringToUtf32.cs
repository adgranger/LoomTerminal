// Ported from xtermjs/xterm.js: src/common/input/TextDecoder.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Input;

/// <summary>
/// Decodes UTF-16 strings into UTF-32 code points. To stay in line with .NET strings it handles single surrogates
/// as UCS2.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class StringToUtf32
{
    private uint _interim;

    /// <summary>Clears the interim surrogate and resets the decoder to a clean state.</summary>
    public void Clear() => _interim = 0;

    /// <summary>
    /// Decodes a string to UTF-32 code points. Assumes stream input: a partly transmitted surrogate pair is stored and
    /// decoded with the next chunk. There is no bounds check on <paramref name="target"/>: make sure the input does
    /// not exceed its size.
    /// </summary>
    /// <param name="input">The text to decode.</param>
    /// <param name="target">Receives the code points.</param>
    /// <returns>The number of code points written to <paramref name="target"/>.</returns>
    public int Decode(string input, uint[] target)
    {
        int length = input.Length;

        if (length == 0)
        {
            return 0;
        }

        int size = 0;
        int startPos = 0;

        // handle leftover surrogate high
        if (_interim != 0)
        {
            uint second = input[startPos++];
            if (0xDC00 <= second && second <= 0xDFFF)
            {
                target[size++] = ((_interim - 0xD800) * 0x400) + second - 0xDC00 + 0x10000;
            }
            else
            {
                // illegal codepoint (UCS2 handling)
                target[size++] = _interim;
                target[size++] = second;
            }

            _interim = 0;
        }

        for (int i = startPos; i < length; ++i)
        {
            uint code = input[i];
            // surrogate pair first
            if (0xD800 <= code && code <= 0xDBFF)
            {
                if (++i >= length)
                {
                    _interim = code;
                    return size;
                }

                uint second = input[i];
                if (0xDC00 <= second && second <= 0xDFFF)
                {
                    target[size++] = ((code - 0xD800) * 0x400) + second - 0xDC00 + 0x10000;
                }
                else
                {
                    // illegal codepoint (UCS2 handling)
                    target[size++] = code;
                    target[size++] = second;
                }

                continue;
            }

            if (code == 0xFEFF)
            {
                // BOM
                continue;
            }

            target[size++] = code;
        }

        return size;
    }
}
