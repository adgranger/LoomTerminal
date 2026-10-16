// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.Parser;

// state transitions and actions (1/2): GROUND, ANYWHERE, ESCAPE, ESCAPE_INTERMEDIATE, CSI_ENTRY, CSI_PARAM, CSI_INTERMEDIATE
public sealed partial class EscapeSequenceParserTests
{
    // the C0 controls that are executed: 0x00-0x17, 0x19 and 0x1c-0x1f
    private static string[] Executables() => [.. R(0x00, 0x18), "\u0019", .. R(0x1c, 0x20)];

    private static readonly string[] ParamDigits = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];

    private static readonly string[] CollectPrefixes = ["<", "=", ">", "?"];

    // xterm.js: 'state GROUND execute action'
    [Fact]
    public void State_GROUND_execute_action()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] exes = Executables();
        for (int i = 0; i < exes.Length; ++i)
        {
            _parser.CurrentState = ParserState.Ground;
            Parse(_parser, exes[i]);
            Assert.Equal(ParserState.Ground, _parser.CurrentState);
            _testTerminal.Compare(A("exe", exes[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'state GROUND print action'
    [Fact]
    public void State_GROUND_print_action()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] printables = R(0x20, 0x7f); // NOTE: DEL excluded
        for (int i = 0; i < printables.Length; ++i)
        {
            _parser.CurrentState = ParserState.Ground;
            Parse(_parser, printables[i]);
            Assert.Equal(ParserState.Ground, _parser.CurrentState);
            _testTerminal.Compare(A("print", printables[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'trans ANYWHERE --> GROUND with actions'
    [Fact]
    public void Trans_ANYWHERE_to_GROUND_with_actions()
    {
        string[] exes =
        [
            "\u0018", "\u001a",
            "\u0080", "\u0081", "\u0082", "\u0083", "\u0084", "\u0085", "\u0086", "\u0087", "\u0088",
            "\u0089", "\u008a", "\u008b", "\u008c", "\u008d", "\u008e", "\u008f",
            "\u0091", "\u0092", "\u0093", "\u0094", "\u0095", "\u0096", "\u0097", "\u0099", "\u009a",
        ];
        _parser.Reset();
        _testTerminal.Clear();
        foreach (ParserState state in States)
        {
            for (int i = 0; i < exes.Length; ++i)
            {
                _parser.CurrentState = state;
                Parse(_parser, exes[i]);
                Assert.Equal(ParserState.Ground, _parser.CurrentState);

                bool abort = exes[i] is "\u0018" or "\u001a";
                if (state == ParserState.OscString && abort)
                {
                    // abort OSC_STRING
                    _testTerminal.Compare();
                }
                else if (state == ParserState.DcsPassthrough && abort)
                {
                    // abort DCS_PASSTHROUGH
                    _testTerminal.Compare(A("dcs unhook", false));
                }
                else
                {
                    _testTerminal.Compare(A("exe", exes[i]));
                }

                _parser.Reset();
                _testTerminal.Clear();
            }

            Parse(_parser, "\u009c");
            Assert.Equal(ParserState.Ground, _parser.CurrentState);
            _testTerminal.Compare();
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'trans ANYWHERE --> ESCAPE with clear'
    [Fact]
    public void Trans_ANYWHERE_to_ESCAPE_with_clear()
    {
        _parser.Reset();
        foreach (ParserState state in States)
        {
            _parser.CurrentState = state;
            _parser.ParamsArray = A(23);
            _parser.Collect = "#";
            Parse(_parser, "\u001b");
            Assert.Equal(ParserState.Escape, _parser.CurrentState);
            Assert.Equal(A(0), _parser.ParamsArray);
            Assert.Equal(string.Empty, _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'state ESCAPE execute rules'
    [Fact]
    public void State_ESCAPE_execute_rules()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] exes = Executables();
        for (int i = 0; i < exes.Length; ++i)
        {
            _parser.CurrentState = ParserState.Escape;
            Parse(_parser, exes[i]);
            Assert.Equal(ParserState.Escape, _parser.CurrentState);
            _testTerminal.Compare(A("exe", exes[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'state ESCAPE ignore'
    [Fact]
    public void State_ESCAPE_ignore()
    {
        _parser.Reset();
        _testTerminal.Clear();
        _parser.CurrentState = ParserState.Escape;
        Parse(_parser, "\u007f");
        Assert.Equal(ParserState.Escape, _parser.CurrentState);
        _testTerminal.Compare();
        _parser.Reset();
        _testTerminal.Clear();
    }

    // xterm.js: 'trans ESCAPE --> GROUND with ecs_dispatch action'
    [Fact]
    public void Trans_ESCAPE_to_GROUND_with_esc_dispatch_action()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] dispatches = [.. R(0x30, 0x50), .. R(0x51, 0x58), "Y", "Z", .. R(0x60, 0x7f)]; // excluded \x5c
        for (int i = 0; i < dispatches.Length; ++i)
        {
            _parser.CurrentState = ParserState.Escape;
            Parse(_parser, dispatches[i]);
            Assert.Equal(ParserState.Ground, _parser.CurrentState);
            _testTerminal.Compare(A("esc", string.Empty, dispatches[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'trans ESCAPE --> ESCAPE_INTERMEDIATE with collect action'
    [Fact]
    public void Trans_ESCAPE_to_ESCAPE_INTERMEDIATE_with_collect_action()
    {
        _parser.Reset();
        string[] collect = R(0x20, 0x30);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.Escape;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.EscapeIntermediate, _parser.CurrentState);
            Assert.Equal(collect[i], _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'state ESCAPE_INTERMEDIATE execute rules'
    [Fact]
    public void State_ESCAPE_INTERMEDIATE_execute_rules()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] exes = Executables();
        for (int i = 0; i < exes.Length; ++i)
        {
            _parser.CurrentState = ParserState.EscapeIntermediate;
            Parse(_parser, exes[i]);
            Assert.Equal(ParserState.EscapeIntermediate, _parser.CurrentState);
            _testTerminal.Compare(A("exe", exes[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'state ESCAPE_INTERMEDIATE ignore'
    [Fact]
    public void State_ESCAPE_INTERMEDIATE_ignore()
    {
        _parser.Reset();
        _testTerminal.Clear();
        _parser.CurrentState = ParserState.EscapeIntermediate;
        Parse(_parser, "\u007f");
        Assert.Equal(ParserState.EscapeIntermediate, _parser.CurrentState);
        _testTerminal.Compare();
        _parser.Reset();
        _testTerminal.Clear();
    }

    // xterm.js: 'state ESCAPE_INTERMEDIATE collect action'
    [Fact]
    public void State_ESCAPE_INTERMEDIATE_collect_action()
    {
        _parser.Reset();
        string[] collect = R(0x20, 0x30);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.EscapeIntermediate;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.EscapeIntermediate, _parser.CurrentState);
            Assert.Equal(collect[i], _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'trans ESCAPE_INTERMEDIATE --> GROUND with esc_dispatch action'
    [Fact]
    public void Trans_ESCAPE_INTERMEDIATE_to_GROUND_with_esc_dispatch_action()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] collect = R(0x30, 0x7f);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.EscapeIntermediate;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.Ground, _parser.CurrentState);

            // '\x5c' --> ESC + \ (7bit ST) parser does not expose this as it already got handled
            if (collect[i] == "\\")
            {
                _testTerminal.Compare();
            }
            else
            {
                _testTerminal.Compare(A("esc", string.Empty, collect[i]));
            }

            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'trans ANYWHERE/ESCAPE --> CSI_ENTRY with clear'
    [Fact]
    public void Trans_ANYWHERE_and_ESCAPE_to_CSI_ENTRY_with_clear()
    {
        _parser.Reset();

        // C0
        _parser.CurrentState = ParserState.Escape;
        _parser.ParamsArray = A(123);
        _parser.Collect = "#";
        Parse(_parser, "[");
        Assert.Equal(ParserState.CsiEntry, _parser.CurrentState);
        Assert.Equal(A(0), _parser.ParamsArray);
        Assert.Equal(string.Empty, _parser.Collect);
        _parser.Reset();

        // C1
        foreach (ParserState state in States)
        {
            _parser.CurrentState = state;
            _parser.ParamsArray = A(123);
            _parser.Collect = "#";
            Parse(_parser, "\u009b");
            Assert.Equal(ParserState.CsiEntry, _parser.CurrentState);
            Assert.Equal(A(0), _parser.ParamsArray);
            Assert.Equal(string.Empty, _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'state CSI_ENTRY execute rules'
    [Fact]
    public void State_CSI_ENTRY_execute_rules()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] exes = Executables();
        for (int i = 0; i < exes.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiEntry;
            Parse(_parser, exes[i]);
            Assert.Equal(ParserState.CsiEntry, _parser.CurrentState);
            _testTerminal.Compare(A("exe", exes[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'state CSI_ENTRY ignore'
    [Fact]
    public void State_CSI_ENTRY_ignore()
    {
        _parser.Reset();
        _testTerminal.Clear();
        _parser.CurrentState = ParserState.CsiEntry;
        Parse(_parser, "\u007f");
        Assert.Equal(ParserState.CsiEntry, _parser.CurrentState);
        _testTerminal.Compare();
        _parser.Reset();
        _testTerminal.Clear();
    }

    // xterm.js: 'trans CSI_ENTRY --> GROUND with csi_dispatch action'
    [Fact]
    public void Trans_CSI_ENTRY_to_GROUND_with_csi_dispatch_action()
    {
        _parser.Reset();
        string[] dispatches = R(0x40, 0x7f);
        for (int i = 0; i < dispatches.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiEntry;
            Parse(_parser, dispatches[i]);
            Assert.Equal(ParserState.Ground, _parser.CurrentState);
            _testTerminal.Compare(A("csi", string.Empty, A(0), dispatches[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'trans CSI_ENTRY --> CSI_PARAM with param/collect actions'
    [Fact]
    public void Trans_CSI_ENTRY_to_CSI_PARAM_with_param_and_collect_actions()
    {
        _parser.Reset();
        for (int i = 0; i < ParamDigits.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiEntry;
            Parse(_parser, ParamDigits[i]);
            Assert.Equal(ParserState.CsiParam, _parser.CurrentState);
            Assert.Equal(A(ParamDigits[i][0] - 48), _parser.ParamsArray);
            _parser.Reset();
        }

        _parser.CurrentState = ParserState.CsiEntry;
        Parse(_parser, ";");
        Assert.Equal(ParserState.CsiParam, _parser.CurrentState);
        Assert.Equal(A(0, 0), _parser.ParamsArray);
        _parser.Reset();
        for (int i = 0; i < CollectPrefixes.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiEntry;
            Parse(_parser, CollectPrefixes[i]);
            Assert.Equal(ParserState.CsiParam, _parser.CurrentState);
            Assert.Equal(CollectPrefixes[i], _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'state CSI_PARAM execute rules'
    [Fact]
    public void State_CSI_PARAM_execute_rules()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] exes = Executables();
        for (int i = 0; i < exes.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiParam;
            Parse(_parser, exes[i]);
            Assert.Equal(ParserState.CsiParam, _parser.CurrentState);
            _testTerminal.Compare(A("exe", exes[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'state CSI_PARAM param action'
    [Fact]
    public void State_CSI_PARAM_param_action()
    {
        _parser.Reset();
        for (int i = 0; i < ParamDigits.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiParam;
            Parse(_parser, ParamDigits[i]);
            Assert.Equal(ParserState.CsiParam, _parser.CurrentState);
            Assert.Equal(A(ParamDigits[i][0] - 48), _parser.ParamsArray);
            _parser.Reset();
        }

        _parser.CurrentState = ParserState.CsiParam;
        Parse(_parser, ";");
        Assert.Equal(ParserState.CsiParam, _parser.CurrentState);
        Assert.Equal(A(0, 0), _parser.ParamsArray);
        _parser.Reset();
    }

    // xterm.js: 'state CSI_PARAM ignore'
    [Fact]
    public void State_CSI_PARAM_ignore()
    {
        _parser.Reset();
        _testTerminal.Clear();
        _parser.CurrentState = ParserState.CsiParam;
        Parse(_parser, "\u007f");
        Assert.Equal(ParserState.CsiParam, _parser.CurrentState);
        _testTerminal.Compare();
        _parser.Reset();
        _testTerminal.Clear();
    }

    // xterm.js: 'trans CSI_PARAM --> GROUND with csi_dispatch action'
    [Fact]
    public void Trans_CSI_PARAM_to_GROUND_with_csi_dispatch_action()
    {
        _parser.Reset();
        string[] dispatches = R(0x40, 0x7f);
        for (int i = 0; i < dispatches.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiParam;
            _parser.ParamsArray = A(0, 1);
            Parse(_parser, dispatches[i]);
            Assert.Equal(ParserState.Ground, _parser.CurrentState);
            _testTerminal.Compare(A("csi", string.Empty, A(0, 1), dispatches[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'trans CSI_ENTRY --> CSI_INTERMEDIATE with collect action'
    [Fact]
    public void Trans_CSI_ENTRY_to_CSI_INTERMEDIATE_with_collect_action()
    {
        _parser.Reset();
        string[] collect = R(0x20, 0x30);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiEntry;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.CsiIntermediate, _parser.CurrentState);
            Assert.Equal(collect[i], _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'trans CSI_PARAM --> CSI_INTERMEDIATE with collect action'
    [Fact]
    public void Trans_CSI_PARAM_to_CSI_INTERMEDIATE_with_collect_action()
    {
        _parser.Reset();
        string[] collect = R(0x20, 0x30);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiParam;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.CsiIntermediate, _parser.CurrentState);
            Assert.Equal(collect[i], _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'state CSI_INTERMEDIATE execute rules'
    [Fact]
    public void State_CSI_INTERMEDIATE_execute_rules()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] exes = Executables();
        for (int i = 0; i < exes.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiIntermediate;
            Parse(_parser, exes[i]);
            Assert.Equal(ParserState.CsiIntermediate, _parser.CurrentState);
            _testTerminal.Compare(A("exe", exes[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'state CSI_INTERMEDIATE collect'
    [Fact]
    public void State_CSI_INTERMEDIATE_collect()
    {
        _parser.Reset();
        string[] collect = R(0x20, 0x30);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiIntermediate;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.CsiIntermediate, _parser.CurrentState);
            Assert.Equal(collect[i], _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'state CSI_INTERMEDIATE ignore'
    [Fact]
    public void State_CSI_INTERMEDIATE_ignore()
    {
        _parser.Reset();
        _testTerminal.Clear();
        _parser.CurrentState = ParserState.CsiIntermediate;
        Parse(_parser, "\u007f");
        Assert.Equal(ParserState.CsiIntermediate, _parser.CurrentState);
        _testTerminal.Compare();
        _parser.Reset();
        _testTerminal.Clear();
    }

    // xterm.js: 'trans CSI_INTERMEDIATE --> GROUND with csi_dispatch action'
    [Fact]
    public void Trans_CSI_INTERMEDIATE_to_GROUND_with_csi_dispatch_action()
    {
        _parser.Reset();
        string[] dispatches = R(0x40, 0x7f);
        for (int i = 0; i < dispatches.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiIntermediate;
            _parser.ParamsArray = A(0, 1);
            Parse(_parser, dispatches[i]);
            Assert.Equal(ParserState.Ground, _parser.CurrentState);
            _testTerminal.Compare(A("csi", string.Empty, A(0, 1), dispatches[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }
}
