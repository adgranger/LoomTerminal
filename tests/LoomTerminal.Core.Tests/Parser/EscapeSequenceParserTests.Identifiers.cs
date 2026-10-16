// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.Parser;

// function identifiers: registration limits and identifier invocation
public sealed partial class EscapeSequenceParserTests
{
    private string IdentToStringOf(FunctionIdentifier id) => _parser.IdentToString(TestEscapeSequenceParser.IdentifierOf(id));

    private static void AssertInvalid(FunctionIdentifier id, string message)
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() => TestEscapeSequenceParser.IdentifierOf(id));
        Assert.Equal(message, error.Message);
    }

    // xterm.js: 'prefix range 0x3c .. 0x3f, one byte'
    [Fact]
    public void Registration_limits_prefix_range_0x3c_to_0x3f_one_byte()
    {
        for (int i = 0x3c; i <= 0x3f; ++i)
        {
            string c = ((char)i).ToString();
            Assert.Equal(c + "z", IdentToStringOf(new FunctionIdentifier(Prefix: c, Final: "z")));
        }

        AssertInvalid(new FunctionIdentifier(Prefix: ";", Final: "z"), "prefix must be in range 0x3c .. 0x3f");
        AssertInvalid(new FunctionIdentifier(Prefix: "@", Final: "z"), "prefix must be in range 0x3c .. 0x3f");
        AssertInvalid(new FunctionIdentifier(Prefix: "??", Final: "z"), "only one byte as prefix supported");
    }

    // xterm.js: 'intermediates range 0x20 .. 0x2f, up to two bytes'
    [Fact]
    public void Registration_limits_intermediates_range_0x20_to_0x2f_up_to_two_bytes()
    {
        for (int i = 0x20; i <= 0x2f; ++i)
        {
            string c = ((char)i).ToString();
            Assert.Equal(c + c + "z", IdentToStringOf(new FunctionIdentifier(Intermediates: c + c, Final: "z")));
        }

        AssertInvalid(new FunctionIdentifier(Intermediates: "\u001f", Final: "z"), "intermediate must be in range 0x20 .. 0x2f");
        AssertInvalid(new FunctionIdentifier(Intermediates: "0", Final: "z"), "intermediate must be in range 0x20 .. 0x2f");
        AssertInvalid(new FunctionIdentifier(Intermediates: "!!!", Final: "z"), "only two bytes as intermediates are supported");
    }

    // xterm.js: 'final CSI/DCS range 0x40 .. 0x7e (default), one byte'
    [Fact]
    public void Registration_limits_final_CSI_DCS_range_0x40_to_0x7e_one_byte()
    {
        for (int i = 0x40; i <= 0x7e; ++i)
        {
            string c = ((char)i).ToString();
            Assert.Equal(c, IdentToStringOf(new FunctionIdentifier(Final: c)));
        }

        AssertInvalid(new FunctionIdentifier(Final: "?"), "final must be in range 64 .. 126");
        AssertInvalid(new FunctionIdentifier(Final: "\u007f"), "final must be in range 64 .. 126");
        AssertInvalid(new FunctionIdentifier(Final: "zz"), "final must be a single byte");
    }

    // xterm.js: 'final ESC range 0x30 .. 0x7e, one byte'
    [Fact]
    public void Registration_limits_final_ESC_range_0x30_to_0x7e_one_byte()
    {
        for (int i = 0x30; i <= 0x7e; ++i)
        {
            string final = ((char)i).ToString();
            IDisposable handler = _parser.RegisterEscHandler(new FunctionIdentifier(Final: final), () => true);
            handler.Dispose();
        }

        ArgumentException below = Assert.Throws<ArgumentException>(() => _parser.RegisterEscHandler(new FunctionIdentifier(Final: "/"), () => true));
        Assert.Equal("final must be in range 48 .. 126", below.Message);
        ArgumentException above = Assert.Throws<ArgumentException>(() => _parser.RegisterEscHandler(new FunctionIdentifier(Final: "\u007f"), () => true));
        Assert.Equal("final must be in range 48 .. 126", above.Message);
    }

    // xterm.js: 'id calculation - should stacking prefix -> intermediate -> final'
    [Fact]
    public void Registration_limits_id_calculation_stacks_prefix_intermediates_final()
    {
        Assert.Equal("z", IdentToStringOf(new FunctionIdentifier(Final: "z")));
        Assert.Equal("?z", IdentToStringOf(new FunctionIdentifier(Prefix: "?", Final: "z")));
        Assert.Equal("!z", IdentToStringOf(new FunctionIdentifier(Intermediates: "!", Final: "z")));
        Assert.Equal("?!z", IdentToStringOf(new FunctionIdentifier(Prefix: "?", Intermediates: "!", Final: "z")));
        Assert.Equal("?!!z", IdentToStringOf(new FunctionIdentifier(Prefix: "?", Intermediates: "!!", Final: "z")));
    }

    // xterm.js: 'identifier invocation' › 'ESC'
    [Fact]
    public void Identifier_invocation_ESC()
    {
        List<string> callstack = [];
        IDisposable h1 = _parser.RegisterEscHandler(new FunctionIdentifier(Final: "z"), () => { callstack.Add("z"); return true; });
        IDisposable h2 = _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: "!", Final: "z"), () => { callstack.Add("!z"); return true; });
        IDisposable h3 = _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: "!!", Final: "z"), () => { callstack.Add("!!z"); return true; });
        Parse(_parser, "\u001bz\u001b!z\u001b!!z");
        h1.Dispose();
        h2.Dispose();
        h3.Dispose();
        Parse(_parser, "\u001bz\u001b!z\u001b!!z");
        Assert.Equal(["z", "!z", "!!z"], callstack);
    }

    // xterm.js: 'identifier invocation' › 'CSI'
    [Fact]
    public void Identifier_invocation_CSI()
    {
        List<object[]> callstack = [];
        IDisposable[] handlers =
        [
            _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "z"), p => { callstack.Add(A("z", p.ToArray())); return true; }),
            _parser.RegisterCsiHandler(new FunctionIdentifier(Intermediates: "!", Final: "z"), p => { callstack.Add(A("!z", p.ToArray())); return true; }),
            _parser.RegisterCsiHandler(new FunctionIdentifier(Intermediates: "!!", Final: "z"), p => { callstack.Add(A("!!z", p.ToArray())); return true; }),
            _parser.RegisterCsiHandler(new FunctionIdentifier(Prefix: "?", Final: "z"), p => { callstack.Add(A("?z", p.ToArray())); return true; }),
            _parser.RegisterCsiHandler(new FunctionIdentifier(Prefix: "?", Intermediates: "!", Final: "z"), p => { callstack.Add(A("?!z", p.ToArray())); return true; }),
            _parser.RegisterCsiHandler(new FunctionIdentifier(Prefix: "?", Intermediates: "!!", Final: "z"), p => { callstack.Add(A("?!!z", p.ToArray())); return true; }),
        ];
        Parse(_parser, "\u001b[1;z\u001b[1;!z\u001b[1;!!z\u001b[?1;z\u001b[?1;!z\u001b[?1;!!z");
        foreach (IDisposable handler in handlers)
        {
            handler.Dispose();
        }

        Parse(_parser, "\u001b[1;z\u001b[1;!z\u001b[1;!!z\u001b[?1;z\u001b[?1;!z\u001b[?1;!!z");
        Assert.Equal(
            [
                A("z", A(1, 0)),
                A("!z", A(1, 0)),
                A("!!z", A(1, 0)),
                A("?z", A(1, 0)),
                A("?!z", A(1, 0)),
                A("?!!z", A(1, 0)),
            ],
            callstack);
    }

    // xterm.js: 'identifier invocation' › 'DCS'
    [Fact]
    public void Identifier_invocation_DCS()
    {
        List<object[]> callstack = [];
        IDisposable[] handlers =
        [
            _parser.RegisterDcsHandler(new FunctionIdentifier(Final: "z"), new DcsHandler((data, p) => { callstack.Add(A("z", p.ToArray(), data)); return true; })),
            _parser.RegisterDcsHandler(new FunctionIdentifier(Intermediates: "!", Final: "z"), new DcsHandler((data, p) => { callstack.Add(A("!z", p.ToArray(), data)); return true; })),
            _parser.RegisterDcsHandler(new FunctionIdentifier(Intermediates: "!!", Final: "z"), new DcsHandler((data, p) => { callstack.Add(A("!!z", p.ToArray(), data)); return true; })),
            _parser.RegisterDcsHandler(new FunctionIdentifier(Prefix: "?", Final: "z"), new DcsHandler((data, p) => { callstack.Add(A("?z", p.ToArray(), data)); return true; })),
            _parser.RegisterDcsHandler(new FunctionIdentifier(Prefix: "?", Intermediates: "!", Final: "z"), new DcsHandler((data, p) => { callstack.Add(A("?!z", p.ToArray(), data)); return true; })),
            _parser.RegisterDcsHandler(new FunctionIdentifier(Prefix: "?", Intermediates: "!!", Final: "z"), new DcsHandler((data, p) => { callstack.Add(A("?!!z", p.ToArray(), data)); return true; })),
        ];
        const string input = "\u001bP1;zAB\u001b\\\u001bP1;!zAB\u001b\\\u001bP1;!!zAB\u001b\\\u001bP?1;zAB\u001b\\\u001bP?1;!zAB\u001b\\\u001bP?1;!!zAB\u001b\\";
        Parse(_parser, input);
        foreach (IDisposable handler in handlers)
        {
            handler.Dispose();
        }

        Parse(_parser, input);
        Assert.Equal(
            [
                A("z", A(1, 0), "AB"),
                A("!z", A(1, 0), "AB"),
                A("!!z", A(1, 0), "AB"),
                A("?z", A(1, 0), "AB"),
                A("?!z", A(1, 0), "AB"),
                A("?!!z", A(1, 0), "AB"),
            ],
            callstack);
    }
}
