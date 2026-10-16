// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.Parser;

// escape sequence examples and coverage tests
public sealed partial class EscapeSequenceParserTests
{
    // the `test` helper of the xterm.js test file
    private void RunExample(string s, bool noReset, params object[][] value)
    {
        if (!noReset)
        {
            _parser.Reset();
            _testTerminal.Clear();
        }

        Parse(_parser, s);
        _testTerminal.Compare(value);
    }

    // xterm.js: 'CSI with print and execute'
    [Fact]
    public void Example_CSI_with_print_and_execute()
    {
        RunExample(
            "\u001b[<31;5mHello World! öäü€\nabc",
            false,
            A("csi", "<", A(31, 5), "m"),
            A("print", "Hello World! öäü€"),
            A("exe", "\n"),
            A("print", "abc"));
    }

    // xterm.js: 'OSC'
    [Fact]
    public void Example_OSC()
    {
        RunExample("\u001b]0;abc123€öäü\u0007", false, A("osc", "0;abc123€öäü, success: true"));
    }

    // xterm.js: 'single DCS'
    [Fact]
    public void Example_single_DCS()
    {
        RunExample(
            "\u001bP1;2;3+$aäbc;däe\u009c",
            false,
            A("dcs hook", A(1, 2, 3)),
            A("dcs put", "äbc;däe"),
            A("dcs unhook", true));
    }

    // xterm.js: 'multi DCS'
    [Fact]
    public void Example_multi_DCS()
    {
        RunExample("\u001bP1;2;3+$abc;de", false, A("dcs hook", A(1, 2, 3)), A("dcs put", "bc;de"));
        _testTerminal.Clear();
        RunExample("abc\u009c", true, A("dcs put", "abc"), A("dcs unhook", true));
    }

    // xterm.js: 'print + DCS(C1)'
    [Fact]
    public void Example_print_and_DCS_C1()
    {
        RunExample(
            "abc\u00901;2;3+$abc;de\u009c",
            false,
            A("print", "abc"),
            A("dcs hook", A(1, 2, 3)),
            A("dcs put", "bc;de"),
            A("dcs unhook", true));
    }

    // xterm.js: 'print + PM(C1) + print'
    [Fact]
    public void Example_print_PM_C1_print()
    {
        RunExample("abc\u0098123tzf\u009cdefg", false, A("print", "abc"), A("print", "defg"));
    }

    // xterm.js: 'print + OSC(C1) + print'
    [Fact]
    public void Example_print_OSC_C1_print()
    {
        RunExample(
            "abc\u009d123;tzf\u009cdefg",
            false,
            A("print", "abc"),
            A("osc", "123;tzf, success: true"),
            A("print", "defg"));
    }

    // xterm.js: 'error recovery'
    [Fact]
    public void Example_error_recovery()
    {
        RunExample(
            "\u001b[1€abcdefg\u009b<;c",
            false,
            A("print", "abcdefg"),
            A("csi", "<", A(0, 0), "c"));
    }

    // xterm.js: '7bit ST should be swallowed'
    [Fact]
    public void Example_7bit_ST_should_be_swallowed()
    {
        RunExample(
            "abc\u009d123;tzf\u001b\\defg",
            false,
            A("print", "abc"),
            A("osc", "123;tzf, success: true"),
            A("print", "defg"));
    }

    // xterm.js: 'colon notation in CSI params'
    [Fact]
    public void Example_colon_notation_in_CSI_params()
    {
        RunExample(
            "\u001b[<31;5::123:;8mHello World! öäü€\nabc",
            false,
            A("csi", "<", A(31, 5, S(-1, 123, -1), 8), "m"),
            A("print", "Hello World! öäü€"),
            A("exe", "\n"),
            A("print", "abc"));
    }

    // xterm.js: 'colon notation in DCS params'
    [Fact]
    public void Example_colon_notation_in_DCS_params()
    {
        RunExample(
            "abc\u00901;2::55;3+$abc;de\u009c",
            false,
            A("print", "abc"),
            A("dcs hook", A(1, 2, S(-1, 55), 3)),
            A("dcs put", "bc;de"),
            A("dcs unhook", true));
    }

    // xterm.js: 'CAN should abort DCS'
    [Fact]
    public void Example_CAN_should_abort_DCS()
    {
        RunExample(
            "abc\u00901;2::55;3+$abc;de\u0018",
            false,
            A("print", "abc"),
            A("dcs hook", A(1, 2, S(-1, 55), 3)),
            A("dcs put", "bc;de"),
            A("dcs unhook", false)); // false for abort
    }

    // xterm.js: 'SUB should abort DCS'
    [Fact]
    public void Example_SUB_should_abort_DCS()
    {
        RunExample(
            "abc\u00901;2::55;3+$abc;de\u001a",
            false,
            A("print", "abc"),
            A("dcs hook", A(1, 2, S(-1, 55), 3)),
            A("dcs put", "bc;de"),
            A("dcs unhook", false)); // false for abort
    }

    // xterm.js: 'CAN should abort OSC'
    [Fact]
    public void Example_CAN_should_abort_OSC()
    {
        RunExample("\u001b]0;abc123€öäü\u0018", false, A("osc", "0;abc123€öäü, success: false"));
    }

    // xterm.js: 'SUB should abort OSC'
    [Fact]
    public void Example_SUB_should_abort_OSC()
    {
        RunExample("\u001b]0;abc123€öäü\u001a", false, A("osc", "0;abc123€öäü, success: false"));
    }

    // xterm.js: 'CSI_IGNORE error'
    [Fact]
    public void Coverage_CSI_IGNORE_error()
    {
        _parser.Reset();
        _testTerminal.Clear();
        _parser.CurrentState = ParserState.CsiIgnore;
        Parse(_parser, "€öäü");
        Assert.Equal(ParserState.CsiIgnore, _parser.CurrentState);
        _testTerminal.Compare();
        _parser.Reset();
        _testTerminal.Clear();
    }

    // xterm.js: 'DCS_IGNORE error'
    [Fact]
    public void Coverage_DCS_IGNORE_error()
    {
        _parser.Reset();
        _testTerminal.Clear();
        _parser.CurrentState = ParserState.DcsIgnore;
        Parse(_parser, "€öäü");
        Assert.Equal(ParserState.DcsIgnore, _parser.CurrentState);
        _testTerminal.Compare();
        _parser.Reset();
        _testTerminal.Clear();
    }

    // xterm.js: 'DCS_PASSTHROUGH error'
    [Fact]
    public void Coverage_DCS_PASSTHROUGH_error()
    {
        _parser.Reset();
        _testTerminal.Clear();
        _parser.CurrentState = ParserState.DcsPassthrough;
        Parse(_parser, "\u00901;2;3+$a€öäü");
        Assert.Equal(ParserState.DcsPassthrough, _parser.CurrentState);
        _testTerminal.Compare(A("dcs hook", A(1, 2, 3)), A("dcs put", "€öäü"));
        _parser.Reset();
        _testTerminal.Clear();
    }

    // xterm.js: 'error else of if (code > 159)'
    [Fact]
    public void Coverage_error_else_of_if_code_greater_than_159()
    {
        _parser.Reset();
        _testTerminal.Clear();
        _parser.CurrentState = ParserState.Ground;
        Parse(_parser, "\u009c");
        Assert.Equal(ParserState.Ground, _parser.CurrentState);
        _testTerminal.Compare();
        _parser.Reset();
        _testTerminal.Clear();
    }
}
