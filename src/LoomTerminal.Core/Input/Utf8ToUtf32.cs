// Ported from xtermjs/xterm.js: src/common/input/TextDecoder.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Input;

/// <summary>Decodes UTF-8 byte sequences into UTF-32 code points, keeping incomplete sequences between chunks.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class Utf8ToUtf32
{
    /// <summary>The bytes of an incomplete sequence kept for the next chunk.</summary>
    public byte[] Interim { get; } = new byte[3];

    /// <summary>Clears the interim bytes and resets the decoder to a clean state.</summary>
    public void Clear() => Array.Clear(Interim);

    /// <summary>
    /// Decodes UTF-8 byte sequences to UTF-32 code points. Assumes stream input: partly transmitted bytes are stored
    /// and decoded with the next chunk. There is no bounds check on <paramref name="target"/>: make sure the chunk
    /// does not exceed its size. Invalid sequences are skipped.
    /// </summary>
    /// <param name="input">The bytes to decode.</param>
    /// <param name="target">Receives the code points.</param>
    /// <returns>The number of code points written to <paramref name="target"/>.</returns>
    public int Decode(ReadOnlySpan<byte> input, uint[] target)
    {
        int length = input.Length;

        if (length == 0)
        {
            return 0;
        }

        int size = 0;
        int startPos = 0;

        // handle leftover bytes
        if (Interim[0] != 0)
        {
            if (!DecodeInterim(input, target, ref size, ref startPos))
            {
                return 0;
            }
        }

        // loop through input
        int fourStop = length - 4;
        int i = startPos;
        while (i < length)
        {
            // ASCII shortcut: 4 consecutive ASCII chars at once.
            while (i < fourStop
                && input[i] < 0x80
                && input[i + 1] < 0x80
                && input[i + 2] < 0x80
                && input[i + 3] < 0x80)
            {
                target[size++] = input[i];
                target[size++] = input[i + 1];
                target[size++] = input[i + 2];
                target[size++] = input[i + 3];
                i += 4;
            }

            // reread byte1
            int byte1 = input[i++];
            int byte2;
            int byte3;
            int byte4;
            int codepoint;

            if (byte1 < 0x80)
            {
                // 1 byte
                target[size++] = (uint)byte1;
            }
            else if ((byte1 & 0xE0) == 0xC0)
            {
                // 2 bytes
                if (i >= length)
                {
                    Interim[0] = (byte)byte1;
                    return size;
                }

                byte2 = input[i++];
                if ((byte2 & 0xC0) != 0x80)
                {
                    // wrong continuation
                    i--;
                    continue;
                }

                codepoint = ((byte1 & 0x1F) << 6) | (byte2 & 0x3F);
                if (codepoint < 0x80)
                {
                    // wrong starter byte
                    i--;
                    continue;
                }

                target[size++] = (uint)codepoint;
            }
            else if ((byte1 & 0xF0) == 0xE0)
            {
                // 3 bytes
                if (i >= length)
                {
                    Interim[0] = (byte)byte1;
                    return size;
                }

                byte2 = input[i++];
                if ((byte2 & 0xC0) != 0x80)
                {
                    // wrong continuation
                    i--;
                    continue;
                }

                if (i >= length)
                {
                    Interim[0] = (byte)byte1;
                    Interim[1] = (byte)byte2;
                    return size;
                }

                byte3 = input[i++];
                if ((byte3 & 0xC0) != 0x80)
                {
                    // wrong continuation
                    i--;
                    continue;
                }

                codepoint = ((byte1 & 0x0F) << 12) | ((byte2 & 0x3F) << 6) | (byte3 & 0x3F);
                if (codepoint < 0x0800 || (codepoint >= 0xD800 && codepoint <= 0xDFFF) || codepoint == 0xFEFF)
                {
                    // illegal codepoint or BOM, no i-- here
                    continue;
                }

                target[size++] = (uint)codepoint;
            }
            else if ((byte1 & 0xF8) == 0xF0)
            {
                // 4 bytes
                if (i >= length)
                {
                    Interim[0] = (byte)byte1;
                    return size;
                }

                byte2 = input[i++];
                if ((byte2 & 0xC0) != 0x80)
                {
                    // wrong continuation
                    i--;
                    continue;
                }

                if (i >= length)
                {
                    Interim[0] = (byte)byte1;
                    Interim[1] = (byte)byte2;
                    return size;
                }

                byte3 = input[i++];
                if ((byte3 & 0xC0) != 0x80)
                {
                    // wrong continuation
                    i--;
                    continue;
                }

                if (i >= length)
                {
                    Interim[0] = (byte)byte1;
                    Interim[1] = (byte)byte2;
                    Interim[2] = (byte)byte3;
                    return size;
                }

                byte4 = input[i++];
                if ((byte4 & 0xC0) != 0x80)
                {
                    // wrong continuation
                    i--;
                    continue;
                }

                codepoint = ((byte1 & 0x07) << 18) | ((byte2 & 0x3F) << 12) | ((byte3 & 0x3F) << 6) | (byte4 & 0x3F);
                if (codepoint < 0x010000 || codepoint > 0x10FFFF)
                {
                    // illegal codepoint, no i-- here
                    continue;
                }

                target[size++] = (uint)codepoint;
            }

            // else: illegal byte, just skip
        }

        return size;
    }

    // Completes the sequence stored in Interim with the first bytes of the input.
    // Returns false when the input ends before the sequence is complete (the caller returns 0).
    private bool DecodeInterim(ReadOnlySpan<byte> input, uint[] target, ref int size, ref int startPos)
    {
        int length = input.Length;
        bool discardInterim = false;
        int cp = Interim[0];
        cp &= (cp & 0xE0) == 0xC0 ? 0x1F : (cp & 0xF0) == 0xE0 ? 0x0F : 0x07;
        int pos = 0;
        int tmp;
        // JS reads interim[3] as undefined (0) and silently ignores writes to it; Interim has 3 bytes here.
        while ((tmp = ++pos < Interim.Length ? Interim[pos] & 0x3F : 0) != 0 && pos < 4)
        {
            cp <<= 6;
            cp |= tmp;
        }

        // missing bytes - read ahead from input
        int type = (Interim[0] & 0xE0) == 0xC0 ? 2 : (Interim[0] & 0xF0) == 0xE0 ? 3 : 4;
        int missing = type - pos;
        while (startPos < missing)
        {
            if (startPos >= length)
            {
                return false;
            }

            tmp = input[startPos++];
            if ((tmp & 0xC0) != 0x80)
            {
                // wrong continuation, discard interim bytes completely
                startPos--;
                discardInterim = true;
                break;
            }

            // need to save so we can continue short inputs in next call
            if (pos < Interim.Length)
            {
                Interim[pos] = (byte)tmp;
            }

            pos++;
            cp <<= 6;
            cp |= tmp & 0x3F;
        }

        if (!discardInterim)
        {
            // final test is type dependent
            if (type == 2)
            {
                if (cp < 0x80)
                {
                    // wrong starter byte
                    startPos--;
                }
                else
                {
                    target[size++] = (uint)cp;
                }
            }
            else if (type == 3)
            {
                // an illegal codepoint or a BOM is dropped
                if (!(cp < 0x0800 || (cp >= 0xD800 && cp <= 0xDFFF) || cp == 0xFEFF))
                {
                    target[size++] = (uint)cp;
                }
            }
            else if (!(cp < 0x010000 || cp > 0x10FFFF))
            {
                // an illegal codepoint is dropped
                target[size++] = (uint)cp;
            }
        }

        Array.Clear(Interim);
        return true;
    }
}
