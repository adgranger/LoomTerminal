// Ported from xtermjs/xterm.js: src/common/parser/OscParser.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Input;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.Parser;

/// <summary>Helpers shared by the parser tests (<c>toUtf32</c> and the recording OSC handler).</summary>
internal static class ParserTestHelpers
{
    /// <summary>Converts a string to UTF-32 code points (exactly as many as the string has).</summary>
    public static uint[] ToUtf32(string s)
    {
        uint[] utf32 = new uint[s.Length];
        StringToUtf32 decoder = new();
        int length = decoder.Decode(s, utf32);
        return utf32[..length];
    }

    /// <summary>Turns a function identifier into the integer used as handler key (the <c>identifier</c> helper of the DCS tests).</summary>
    public static int Identifier(FunctionIdentifier id)
    {
        int res = 0;
        if (!string.IsNullOrEmpty(id.Prefix))
        {
            if (id.Prefix.Length > 1)
            {
                throw new ArgumentException("only one byte as prefix supported");
            }

            res = id.Prefix[0];
            if ((res != 0 && 0x3c > res) || res > 0x3f)
            {
                throw new ArgumentException("prefix must be in range 0x3c .. 0x3f");
            }
        }

        if (!string.IsNullOrEmpty(id.Intermediates))
        {
            if (id.Intermediates.Length > 2)
            {
                throw new ArgumentException("only two bytes as intermediates are supported");
            }

            foreach (char intermediate in id.Intermediates)
            {
                if (0x20 > intermediate || intermediate > 0x2f)
                {
                    throw new ArgumentException("intermediate must be in range 0x20 .. 0x2f");
                }

                res <<= 8;
                res |= intermediate;
            }
        }

        if (id.Final.Length != 1)
        {
            throw new ArgumentException("final must be a single byte");
        }

        int finalCode = id.Final[0];
        if (0x40 > finalCode || finalCode > 0x7e)
        {
            throw new ArgumentException("final must be in range 0x40 .. 0x7e");
        }

        res <<= 8;
        res |= finalCode;

        return res;
    }
}

/// <summary>An OSC handler that records its calls in <c>output</c>.</summary>
internal sealed class TestOscHandler(int id, List<object?[]> output, string msg, bool returnFalse = false) : IOscHandler
{
    public void Start() => output.Add([msg, id, "START"]);

    public void Put(uint[] data, int start, int end) => output.Add([msg, id, "PUT", TextDecoder.Utf32ToString(data, start, end)]);

    public bool End(bool success)
    {
        output.Add([msg, id, "END", success]);
        return !returnFalse;
    }
}

/// <summary>A DCS handler that records its calls in <c>output</c>.</summary>
internal sealed class TestDcsHandler(List<object?[]> output, string msg, bool returnFalse = false) : IDcsHandler
{
    public void Hook(IParams p) => output.Add([msg, "HOOK", p.ToArray()]);

    public void Put(uint[] data, int start, int end) => output.Add([msg, "PUT", TextDecoder.Utf32ToString(data, start, end)]);

    public bool Unhook(bool success)
    {
        output.Add([msg, "UNHOOK", success]);
        return !returnFalse;
    }
}
