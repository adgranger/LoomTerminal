// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Input;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.Parser;

// One partial class across the files of P1-11 to P1-14. The xterm.js test file shares one parser and one test
// terminal between all tests; here xUnit creates one of each per test (same setup, no leaking state).
public sealed partial class EscapeSequenceParserTests : IDisposable
{
    private static readonly ParserState[] States =
    [
        ParserState.Ground,
        ParserState.Escape,
        ParserState.EscapeIntermediate,
        ParserState.CsiEntry,
        ParserState.CsiParam,
        ParserState.CsiIntermediate,
        ParserState.CsiIgnore,
        ParserState.SosPmApcString,
        ParserState.OscString,
        ParserState.DcsEntry,
        ParserState.DcsParam,
        ParserState.DcsIgnore,
        ParserState.DcsIntermediate,
        ParserState.DcsPassthrough,
    ];

    private readonly TestTerminal _testTerminal = new();
    private readonly TestEscapeSequenceParser _parser = new();

    public EscapeSequenceParserTests()
    {
        // parser with byte based transition table
        _parser.MockOscParser();
        _parser.SetPrintHandler(_testTerminal.Print);
        _parser.SetCsiHandlerFallback((ident, @params) =>
        {
            string id = _parser.IdentToString(ident);
            _testTerminal.ActionCsi(id[..^1], @params, id[^1..]);
        });
        _parser.SetEscHandlerFallback(ident =>
        {
            string id = _parser.IdentToString(ident);
            _testTerminal.ActionEsc(id[..^1], id[^1..]);
        });
        _parser.SetExecuteHandlerFallback(code => _testTerminal.ActionExecute(((char)code).ToString()));
        _parser.SetOscHandlerFallback((identifier, action, data) =>
        {
            if (identifier == -1)
            {
                _testTerminal.ActionOsc((string)data!); // handle error condition silently
            }
            else if (action == OscFallbackAction.End)
            {
                _testTerminal.ActionOsc($"{identifier};{data}"); // collect only data at END
            }
        });
        _parser.SetDcsHandlerFallback((collectAndFlag, action, payload) =>
        {
            switch (action)
            {
                case DcsFallbackAction.Hook:
                    _testTerminal.ActionDcsHook((IParams)payload!);
                    break;
                case DcsFallbackAction.Put:
                    _testTerminal.ActionDcsPrint((string)payload!);
                    break;
                case DcsFallbackAction.Unhook:
                    _testTerminal.ActionDcsUnhook((bool)payload!);
                    break;
            }
        });
    }

    public void Dispose()
    {
        _parser.Dispose();
        _parser2.Dispose();
        GC.SuppressFinalize(this);
    }

    // range of characters [a, b)
    private static string[] R(int a, int b)
    {
        string[] arr = new string[b - a];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = ((char)(a + i)).ToString();
        }

        return arr;
    }

    // sub-parameters of a parameter in a params array
    private static int[] S(params ReadOnlySpan<int> items) => items.ToArray();

    // one recorded call or parameter list
    private static object[] A(params ReadOnlySpan<object> items) => items.ToArray();

    // translate string based parse calls into typed array based
    private static void Parse(TestEscapeSequenceParser parser, string data)
    {
        uint[] container = new uint[data.Length];
        StringToUtf32 decoder = new();
        parser.Parse(container, decoder.Decode(data, container));
    }

    // xterm.js: 'constructor'
    [Fact]
    public void Constructor_uses_the_given_transition_table_or_the_VT500_one()
    {
        using TestEscapeSequenceParser p = new();
        Assert.Same(Vt500TransitionTable.Instance, p.Transitions);
        using TestEscapeSequenceParser p500 = new(Vt500TransitionTable.Instance);
        Assert.Same(Vt500TransitionTable.Instance, p500.Transitions);
        TransitionTable transitions = new(10);
        using TestEscapeSequenceParser custom = new(transitions);
        Assert.Same(transitions, custom.Transitions);
    }

    // xterm.js: 'inital states'
    [Fact]
    public void Initial_states()
    {
        Assert.Equal(ParserState.Ground, _parser.InitialState);
        Assert.Equal(ParserState.Ground, _parser.CurrentState);
        Assert.Equal(string.Empty, _parser.Osc);
        Assert.Equal(A(0), _parser.ParamsArray);
        Assert.Equal(string.Empty, _parser.Collect);
    }

    // xterm.js: 'reset states'
    [Fact]
    public void Reset_states()
    {
        _parser.CurrentState = (ParserState)124;
        _parser.Osc = "#";
        _parser.ParamsArray = A(123);
        _parser.Collect = "#";

        _parser.Reset();
        Assert.Equal(ParserState.Ground, _parser.CurrentState);
        Assert.Equal(string.Empty, _parser.Osc);
        Assert.Equal(A(0), _parser.ParamsArray);
        Assert.Equal(string.Empty, _parser.Collect);
    }
}
