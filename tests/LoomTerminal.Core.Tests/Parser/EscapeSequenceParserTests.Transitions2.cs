// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.Parser;

// state transitions and actions (2/2): CSI_IGNORE, SOS_PM_APC_STRING, OSC_STRING, DCS_ENTRY, DCS_PARAM, DCS_IGNORE,
// DCS_INTERMEDIATE, DCS_PASSTHROUGH
public sealed partial class EscapeSequenceParserTests
{
    // the C0 controls that DCS states ignore: the executables and 0x7f
    private static string[] DcsIgnoredControls() => [.. Executables(), "\u007f"];

    // xterm.js: 'trans CSI_ENTRY --> CSI_PARAM for ":" (0x3a)'
    [Fact]
    public void Trans_CSI_ENTRY_to_CSI_PARAM_for_colon()
    {
        _parser.Reset();
        _parser.CurrentState = ParserState.CsiEntry;
        Parse(_parser, ":");
        Assert.Equal(ParserState.CsiParam, _parser.CurrentState);
        _parser.Reset();
    }

    // xterm.js: 'trans CSI_PARAM --> CSI_IGNORE' (first of two tests with this name)
    [Fact]
    public void Trans_CSI_PARAM_to_CSI_IGNORE_1()
    {
        _parser.Reset();
        foreach (string c in CollectPrefixes)
        {
            _parser.CurrentState = ParserState.CsiParam;
            Parse(_parser, ";" + c);
            Assert.Equal(ParserState.CsiIgnore, _parser.CurrentState);
            Assert.Equal(A(0, 0), _parser.ParamsArray);
            _parser.Reset();
        }
    }

    // xterm.js: 'trans CSI_PARAM --> CSI_IGNORE' (second of two tests with this name)
    [Fact]
    public void Trans_CSI_PARAM_to_CSI_IGNORE_2()
    {
        _parser.Reset();
        foreach (string c in CollectPrefixes)
        {
            Assert.Equal(A(0), _parser.ParamsArray);
            _parser.CurrentState = ParserState.CsiParam;
            Parse(_parser, ";" + c);
            Assert.Equal(ParserState.CsiIgnore, _parser.CurrentState);
            Assert.Equal(A(0, 0), _parser.ParamsArray);
            _parser.Reset();
        }
    }

    // xterm.js: 'trans CSI_INTERMEDIATE --> CSI_IGNORE'
    [Fact]
    public void Trans_CSI_INTERMEDIATE_to_CSI_IGNORE()
    {
        _parser.Reset();
        string[] chars = R(0x30, 0x40);
        for (int i = 0; i < chars.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiIntermediate;
            Parse(_parser, chars[i]);
            Assert.Equal(ParserState.CsiIgnore, _parser.CurrentState);
            Assert.Equal(A(0), _parser.ParamsArray);
            _parser.Reset();
        }
    }

    // xterm.js: 'state CSI_IGNORE execute rules'
    [Fact]
    public void State_CSI_IGNORE_execute_rules()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] exes = Executables();
        for (int i = 0; i < exes.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiIgnore;
            Parse(_parser, exes[i]);
            Assert.Equal(ParserState.CsiIgnore, _parser.CurrentState);
            _testTerminal.Compare(A("exe", exes[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'state CSI_IGNORE ignore'
    [Fact]
    public void State_CSI_IGNORE_ignore()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] ignored = [.. R(0x20, 0x40), "\u007f"];
        for (int i = 0; i < ignored.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiIgnore;
            Parse(_parser, ignored[i]);
            Assert.Equal(ParserState.CsiIgnore, _parser.CurrentState);
            _testTerminal.Compare();
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'trans CSI_IGNORE --> GROUND'
    [Fact]
    public void Trans_CSI_IGNORE_to_GROUND()
    {
        _parser.Reset();
        string[] dispatches = R(0x40, 0x7f);
        for (int i = 0; i < dispatches.Length; ++i)
        {
            _parser.CurrentState = ParserState.CsiIgnore;
            _parser.ParamsArray = A(0, 1);
            Parse(_parser, dispatches[i]);
            Assert.Equal(ParserState.Ground, _parser.CurrentState);
            _testTerminal.Compare();
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'trans ANYWHERE/ESCAPE --> SOS_PM_APC_STRING'
    [Fact]
    public void Trans_ANYWHERE_and_ESCAPE_to_SOS_PM_APC_STRING()
    {
        _parser.Reset();

        // C0
        string[] initializers = ["X", "^", "_"];
        for (int i = 0; i < initializers.Length; ++i)
        {
            Parse(_parser, "\u001b" + initializers[i]);
            Assert.Equal(ParserState.SosPmApcString, _parser.CurrentState);
            _parser.Reset();
        }

        // C1
        foreach (ParserState state in States)
        {
            _parser.CurrentState = state;
            initializers = ["\u0098", "\u009e", "\u009f"];
            for (int i = 0; i < initializers.Length; ++i)
            {
                Parse(_parser, initializers[i]);
                Assert.Equal(ParserState.SosPmApcString, _parser.CurrentState);
                _parser.Reset();
            }
        }
    }

    // xterm.js: 'state SOS_PM_APC_STRING ignore rules'
    [Fact]
    public void State_SOS_PM_APC_STRING_ignore_rules()
    {
        _parser.Reset();
        string[] ignored = [.. Executables(), .. R(0x20, 0x80)];
        for (int i = 0; i < ignored.Length; ++i)
        {
            _parser.CurrentState = ParserState.SosPmApcString;
            Parse(_parser, ignored[i]);
            Assert.Equal(ParserState.SosPmApcString, _parser.CurrentState);
            _parser.Reset();
        }
    }

    // xterm.js: 'trans ANYWHERE/ESCAPE --> OSC_STRING'
    [Fact]
    public void Trans_ANYWHERE_and_ESCAPE_to_OSC_STRING()
    {
        _parser.Reset();

        // C0
        Parse(_parser, "\u001b]");
        Assert.Equal(ParserState.OscString, _parser.CurrentState);
        _parser.Reset();

        // C1
        foreach (ParserState state in States)
        {
            _parser.CurrentState = state;
            Parse(_parser, "\u009d");
            Assert.Equal(ParserState.OscString, _parser.CurrentState);
            _parser.Reset();
        }
    }

    // xterm.js: 'state OSC_STRING ignore rules'
    [Fact]
    public void State_OSC_STRING_ignore_rules()
    {
        _parser.Reset();
        // all executables but 0x07 (BEL), which ends the OSC string
        string[] ignored = [.. R(0x00, 0x07), .. R(0x08, 0x18), "\u0019", .. R(0x1c, 0x20)];
        for (int i = 0; i < ignored.Length; ++i)
        {
            _parser.CurrentState = ParserState.OscString;
            Parse(_parser, ignored[i]);
            Assert.Equal(ParserState.OscString, _parser.CurrentState);
            Assert.Equal(string.Empty, _parser.Osc);
            _parser.Reset();
        }
    }

    // xterm.js: 'state OSC_STRING put action'
    [Fact]
    public void State_OSC_STRING_put_action()
    {
        _parser.Reset();
        string[] puts = R(0x20, 0x80);
        for (int i = 0; i < puts.Length; ++i)
        {
            _parser.CurrentState = ParserState.OscString;
            Parse(_parser, puts[i]);
            Assert.Equal(ParserState.OscString, _parser.CurrentState);
            Assert.Equal(puts[i], _parser.Osc);
            _parser.Reset();
        }
    }

    // xterm.js: 'state DCS_ENTRY'
    [Fact]
    public void State_DCS_ENTRY()
    {
        _parser.Reset();

        // C0
        Parse(_parser, "\u001bP");
        Assert.Equal(ParserState.DcsEntry, _parser.CurrentState);
        _parser.Reset();

        // C1
        foreach (ParserState state in States)
        {
            _parser.CurrentState = state;
            Parse(_parser, "\u0090");
            Assert.Equal(ParserState.DcsEntry, _parser.CurrentState);
            _parser.Reset();
        }
    }

    // xterm.js: 'state DCS_ENTRY ignore rules'
    [Fact]
    public void State_DCS_ENTRY_ignore_rules()
    {
        _parser.Reset();
        string[] ignored = DcsIgnoredControls();
        for (int i = 0; i < ignored.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsEntry;
            Parse(_parser, ignored[i]);
            Assert.Equal(ParserState.DcsEntry, _parser.CurrentState);
            _parser.Reset();
        }
    }

    // xterm.js: 'state DCS_ENTRY --> DCS_PARAM with param/collect actions'
    [Fact]
    public void State_DCS_ENTRY_to_DCS_PARAM_with_param_and_collect_actions()
    {
        _parser.Reset();
        for (int i = 0; i < ParamDigits.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsEntry;
            Parse(_parser, ParamDigits[i]);
            Assert.Equal(ParserState.DcsParam, _parser.CurrentState);
            Assert.Equal(A(ParamDigits[i][0] - 48), _parser.ParamsArray);
            _parser.Reset();
        }

        _parser.CurrentState = ParserState.DcsEntry;
        Parse(_parser, ";");
        Assert.Equal(ParserState.DcsParam, _parser.CurrentState);
        Assert.Equal(A(0, 0), _parser.ParamsArray);
        _parser.Reset();
        for (int i = 0; i < CollectPrefixes.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsEntry;
            Parse(_parser, CollectPrefixes[i]);
            Assert.Equal(ParserState.DcsParam, _parser.CurrentState);
            Assert.Equal(CollectPrefixes[i], _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'state DCS_PARAM ignore rules'
    [Fact]
    public void State_DCS_PARAM_ignore_rules()
    {
        _parser.Reset();
        string[] ignored = DcsIgnoredControls();
        for (int i = 0; i < ignored.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsParam;
            Parse(_parser, ignored[i]);
            Assert.Equal(ParserState.DcsParam, _parser.CurrentState);
            _parser.Reset();
        }
    }

    // xterm.js: 'state DCS_PARAM param action'
    [Fact]
    public void State_DCS_PARAM_param_action()
    {
        _parser.Reset();
        for (int i = 0; i < ParamDigits.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsParam;
            Parse(_parser, ParamDigits[i]);
            Assert.Equal(ParserState.DcsParam, _parser.CurrentState);
            Assert.Equal(A(ParamDigits[i][0] - 48), _parser.ParamsArray);
            _parser.Reset();
        }

        _parser.CurrentState = ParserState.DcsParam;
        Parse(_parser, ";");
        Assert.Equal(ParserState.DcsParam, _parser.CurrentState);
        Assert.Equal(A(0, 0), _parser.ParamsArray);
        _parser.Reset();
    }

    // xterm.js: 'trans DCS_ENTRY --> DCS_PARAM for ":" (0x3a)'
    [Fact]
    public void Trans_DCS_ENTRY_to_DCS_PARAM_for_colon()
    {
        _parser.Reset();
        _parser.CurrentState = ParserState.DcsEntry;
        Parse(_parser, ":");
        Assert.Equal(ParserState.DcsParam, _parser.CurrentState);
        _parser.Reset();
    }

    // xterm.js: 'trans DCS_PARAM --> DCS_IGNORE'
    [Fact]
    public void Trans_DCS_PARAM_to_DCS_IGNORE()
    {
        _parser.Reset();
        foreach (string c in CollectPrefixes)
        {
            _parser.CurrentState = ParserState.DcsParam;
            Parse(_parser, ";" + c);
            Assert.Equal(ParserState.DcsIgnore, _parser.CurrentState);
            Assert.Equal(A(0, 0), _parser.ParamsArray);
            _parser.Reset();
        }
    }

    // xterm.js: 'trans DCS_INTERMEDIATE --> DCS_IGNORE' (first of two tests with this name)
    [Fact]
    public void Trans_DCS_INTERMEDIATE_to_DCS_IGNORE_1()
    {
        _parser.Reset();
        string[] chars = R(0x30, 0x40);
        for (int i = 0; i < chars.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsIntermediate;
            Parse(_parser, chars[i]);
            Assert.Equal(ParserState.DcsIgnore, _parser.CurrentState);
            _parser.Reset();
        }
    }

    // xterm.js: 'state DCS_IGNORE ignore rules'
    [Fact]
    public void State_DCS_IGNORE_ignore_rules()
    {
        _parser.Reset();
        string[] ignored = [.. DcsIgnoredControls(), .. R(0x20, 0x80)];
        for (int i = 0; i < ignored.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsIgnore;
            Parse(_parser, ignored[i]);
            Assert.Equal(ParserState.DcsIgnore, _parser.CurrentState);
            _parser.Reset();
        }
    }

    // xterm.js: 'trans DCS_ENTRY --> DCS_INTERMEDIATE with collect action'
    [Fact]
    public void Trans_DCS_ENTRY_to_DCS_INTERMEDIATE_with_collect_action()
    {
        _parser.Reset();
        string[] collect = R(0x20, 0x30);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsEntry;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.DcsIntermediate, _parser.CurrentState);
            Assert.Equal(collect[i], _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'trans DCS_PARAM --> DCS_INTERMEDIATE with collect action'
    [Fact]
    public void Trans_DCS_PARAM_to_DCS_INTERMEDIATE_with_collect_action()
    {
        _parser.Reset();
        string[] collect = R(0x20, 0x30);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsParam;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.DcsIntermediate, _parser.CurrentState);
            Assert.Equal(collect[i], _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'state DCS_INTERMEDIATE ignore rules'
    [Fact]
    public void State_DCS_INTERMEDIATE_ignore_rules()
    {
        _parser.Reset();
        string[] ignored = DcsIgnoredControls();
        for (int i = 0; i < ignored.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsIntermediate;
            Parse(_parser, ignored[i]);
            Assert.Equal(ParserState.DcsIntermediate, _parser.CurrentState);
            _parser.Reset();
        }
    }

    // xterm.js: 'state DCS_INTERMEDIATE collect action'
    [Fact]
    public void State_DCS_INTERMEDIATE_collect_action()
    {
        _parser.Reset();
        string[] collect = R(0x20, 0x30);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsIntermediate;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.DcsIntermediate, _parser.CurrentState);
            Assert.Equal(collect[i], _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'trans DCS_INTERMEDIATE --> DCS_IGNORE' (second of two tests with this name)
    [Fact]
    public void Trans_DCS_INTERMEDIATE_to_DCS_IGNORE_2()
    {
        _parser.Reset();
        string[] chars = R(0x30, 0x40);
        for (int i = 0; i < chars.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsIntermediate;
            Parse(_parser, " " + chars[i]);
            Assert.Equal(ParserState.DcsIgnore, _parser.CurrentState);
            Assert.Equal(" ", _parser.Collect);
            _parser.Reset();
        }
    }

    // xterm.js: 'trans DCS_ENTRY --> DCS_PASSTHROUGH with hook'
    [Fact]
    public void Trans_DCS_ENTRY_to_DCS_PASSTHROUGH_with_hook()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] collect = R(0x40, 0x7f);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsEntry;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.DcsPassthrough, _parser.CurrentState);
            _testTerminal.Compare(A("dcs hook", A(0)));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'trans DCS_PARAM --> DCS_PASSTHROUGH with hook'
    [Fact]
    public void Trans_DCS_PARAM_to_DCS_PASSTHROUGH_with_hook()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] collect = R(0x40, 0x7f);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsParam;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.DcsPassthrough, _parser.CurrentState);
            _testTerminal.Compare(A("dcs hook", A(0)));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'trans DCS_INTERMEDIATE --> DCS_PASSTHROUGH with hook'
    [Fact]
    public void Trans_DCS_INTERMEDIATE_to_DCS_PASSTHROUGH_with_hook()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] collect = R(0x40, 0x7f);
        for (int i = 0; i < collect.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsIntermediate;
            Parse(_parser, collect[i]);
            Assert.Equal(ParserState.DcsPassthrough, _parser.CurrentState);
            _testTerminal.Compare(A("dcs hook", A(0)));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'state DCS_PASSTHROUGH put action'
    [Fact]
    public void State_DCS_PASSTHROUGH_put_action()
    {
        _parser.Reset();
        _testTerminal.Clear();
        string[] puts = [.. Executables(), .. R(0x20, 0x7f)];
        for (int i = 0; i < puts.Length; ++i)
        {
            _parser.CurrentState = ParserState.DcsPassthrough;
            Parse(_parser, puts[i]);
            Assert.Equal(ParserState.DcsPassthrough, _parser.CurrentState);
            _testTerminal.Compare(A("dcs put", puts[i]));
            _parser.Reset();
            _testTerminal.Clear();
        }
    }

    // xterm.js: 'state DCS_PASSTHROUGH ignore'
    [Fact]
    public void State_DCS_PASSTHROUGH_ignore()
    {
        _parser.Reset();
        _testTerminal.Clear();
        _parser.CurrentState = ParserState.DcsPassthrough;
        Parse(_parser, "\u007f");
        Assert.Equal(ParserState.DcsPassthrough, _parser.CurrentState);
        _testTerminal.Compare();
        _parser.Reset();
        _testTerminal.Clear();
    }
}
