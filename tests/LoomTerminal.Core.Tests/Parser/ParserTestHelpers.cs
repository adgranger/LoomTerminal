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
