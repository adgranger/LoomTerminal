// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Globalization;
using System.Text;
using LoomTerminal.Core.Input;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.Parser;

/// <summary>Replaces the OSC parser: stores the put data and reports it at the end (MockOscPutParser).</summary>
internal sealed class MockOscPutParser : IOscParser
{
    private OscFallbackHandler _fallback = static (_, _, _) => { };

    public string Data { get; set; } = string.Empty;

    public void Reset() => Data = string.Empty;

    public void Put(uint[] data, int start, int end) => Data += TextDecoder.Utf32ToString(data, start, end);

    public void Dispose()
    {
    }

    public void Start()
    {
    }

    public void End(bool success)
    {
        Data += $", success: {(success ? "true" : "false")}";
        // JS: parseInt(data.slice(0, data.indexOf(';'))): an index of -1 slices off the last character.
        int separator = Data.IndexOf(';', StringComparison.Ordinal);
        string idText = separator >= 0 ? Data[..separator] : Data[..^1];
        if (TryParseInt(idText, out int id))
        {
            _fallback(id, OscFallbackAction.End, Data[(separator + 1)..]);
        }
    }

    // JS parseInt: skips leading white space, accepts a sign and reads the leading digits.
    private static bool TryParseInt(string text, out int value)
    {
        string trimmed = text.TrimStart();
        int length = 0;
        if (length < trimmed.Length && (trimmed[length] == '-' || trimmed[length] == '+'))
        {
            length++;
        }

        int digitsStart = length;
        while (length < trimmed.Length && char.IsAsciiDigit(trimmed[length]))
        {
            length++;
        }

        if (length == digitsStart)
        {
            value = 0;
            return false;
        }

        return int.TryParse(trimmed.AsSpan(0, length), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    public IDisposable RegisterHandler(int ident, IOscHandler handler) => throw new NotImplementedException("not implemented");

    public void ClearHandler(int ident) => throw new NotImplementedException("not implemented");

    public void SetHandlerFallback(OscFallbackHandler handler) => _fallback = handler;
}

/// <summary>A parser with access to its internal state (TestEscapeSequenceParser).</summary>
internal sealed class TestEscapeSequenceParser : EscapeSequenceParser
{
    public TestEscapeSequenceParser(TransitionTable? transitions = null)
        : base(transitions)
    {
    }

    public TransitionTable Transitions => _transitions;

    public string Osc
    {
        get => ((MockOscPutParser)_oscParser).Data;
        set => ((MockOscPutParser)_oscParser).Data = value;
    }

    public object[] ParamsArray
    {
        get => _params.ToArray();
        set => _params = Params.FromArray(value);
    }

    public IParams RealParams => _params;

    public string Collect
    {
        get => IdentToString(_collect);
        set
        {
            _collect = 0;
            foreach (char c in value)
            {
                _collect <<= 8;
                _collect |= c;
            }
        }
    }

    public void MockOscParser() => _oscParser = new MockOscPutParser();

    public static int IdentifierOf(FunctionIdentifier id) => Identifier(id);

    /// <summary>Whether at least one CSI handler is registered for <paramref name="id"/>.</summary>
    public bool HasCsiHandler(FunctionIdentifier id) => _csiHandlers.ContainsKey(Identifier(id));

    /// <summary>Whether at least one ESC handler is registered for <paramref name="id"/>.</summary>
    public bool HasEscHandler(FunctionIdentifier id) => _escHandlers.ContainsKey(Identifier(id, 0x30, 0x7e));

    /// <summary>Whether an EXECUTE handler is registered for the control character <paramref name="flag"/>.</summary>
    public bool HasExecuteHandler(string flag) => _executeHandlers.ContainsKey(flag[0]);
}

/// <summary>Collects the parser actions to compare them with expected values (testTerminal).</summary>
internal sealed class TestTerminal
{
    public List<object[]> Calls { get; private set; } = [];

    public void Clear() => Calls = [];

    public void Compare(params object[][] value) => Assert.Equal(value, Calls);

    public void Print(uint[] data, int start, int end)
    {
        StringBuilder s = new();
        for (int i = start; i < end; ++i)
        {
            s.Append(TextDecoder.StringFromCodePoint(data[i]));
        }

        Calls.Add(["print", s.ToString()]);
    }

    public void ActionOsc(string s) => Calls.Add(["osc", s]);

    public void ActionExecute(string flag) => Calls.Add(["exe", flag]);

    public void ActionCsi(string collect, IParams @params, string flag) => Calls.Add(["csi", collect, @params.ToArray(), flag]);

    public void ActionEsc(string collect, string flag) => Calls.Add(["esc", collect, flag]);

    public void ActionDcsHook(IParams @params) => Calls.Add(["dcs hook", @params.ToArray()]);

    public void ActionDcsPrint(string s) => Calls.Add(["dcs put", s]);

    public void ActionDcsUnhook(bool success) => Calls.Add(["dcs unhook", success]);
}
