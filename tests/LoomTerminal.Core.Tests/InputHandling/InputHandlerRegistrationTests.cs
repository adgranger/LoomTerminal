// Inspired by xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Data;
using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Tests.Parser;
using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.InputHandling;

/// <summary>Checks that every handler registered by the xterm.js constructor is registered here too.</summary>
public sealed class InputHandlerRegistrationTests : InputHandlerTestBase
{
    private static readonly FunctionIdentifier[] ExpectedCsi =
    [
        new(Final: "@"), new(Intermediates: " ", Final: "@"), new(Final: "A"), new(Intermediates: " ", Final: "A"),
        new(Final: "B"), new(Final: "C"), new(Final: "D"), new(Final: "E"), new(Final: "F"), new(Final: "G"),
        new(Final: "H"), new(Final: "I"), new(Final: "J"), new(Prefix: "?", Final: "J"), new(Final: "K"),
        new(Prefix: "?", Final: "K"), new(Final: "L"), new(Final: "M"), new(Final: "P"), new(Final: "S"),
        new(Final: "T"), new(Final: "X"), new(Final: "Z"), new(Final: "`"), new(Final: "a"), new(Final: "b"),
        new(Final: "c"), new(Prefix: ">", Final: "c"), new(Final: "d"), new(Final: "e"), new(Final: "f"),
        new(Final: "g"), new(Final: "h"), new(Prefix: "?", Final: "h"), new(Final: "l"), new(Prefix: "?", Final: "l"),
        new(Final: "m"), new(Final: "n"), new(Prefix: "?", Final: "n"), new(Intermediates: "!", Final: "p"),
        new(Intermediates: " ", Final: "q"), new(Final: "r"), new(Final: "s"), new(Final: "t"), new(Final: "u"),
        new(Intermediates: "'", Final: "}"), new(Intermediates: "'", Final: "~"), new(Intermediates: "\"", Final: "q"),
        new(Intermediates: "$", Final: "p"), new(Prefix: "?", Intermediates: "$", Final: "p"),
    ];

    private static readonly FunctionIdentifier[] ExpectedEsc =
    [
        new(Final: "7"), new(Final: "8"), new(Final: "D"), new(Final: "E"), new(Final: "H"), new(Final: "M"),
        new(Final: "="), new(Final: ">"), new(Final: "c"), new(Final: "n"), new(Final: "o"), new(Final: "|"),
        new(Final: "}"), new(Final: "~"), new(Intermediates: "%", Final: "@"), new(Intermediates: "%", Final: "G"),
        new(Intermediates: "#", Final: "8"),
    ];

    private static readonly string[] ExpectedExecute =
    [
        C0.Bel, C0.Lf, C0.Vt, C0.Ff, C0.Cr, C0.Bs, C0.Ht, C0.So, C0.Si, C1.Ind, C1.Nel, C1.Hts,
    ];

    private static readonly string[] CharsetIntermediates = ["(", ")", "*", "+", "-", ".", "/"];

    private readonly TestEscapeSequenceParser _parser = new();
    private readonly MockBufferService _bufferService = new(80, 30);
    private readonly MockOptionsService _options = new();
    private readonly MockCoreService _coreService = new();
    private readonly MockCoreMouseService _mouse = new();
    private readonly MockLogService _log = new();
    private readonly TestInputHandler _handler;

    public InputHandlerRegistrationTests() => _handler = new TestInputHandler(
        _bufferService, new MockCharsetService(), _coreService, _log, _options, new MockOscLinkService(), _mouse, new MockUnicodeService(), _parser);

    [Fact]
    public void Every_CSI_handler_of_the_xterm_js_constructor_is_registered()
    {
        foreach (FunctionIdentifier id in ExpectedCsi)
        {
            Assert.True(_parser.HasCsiHandler(id), $"CSI {id}");
        }
    }

    [Fact]
    public void Every_ESC_handler_of_the_xterm_js_constructor_is_registered()
    {
        foreach (FunctionIdentifier id in ExpectedEsc)
        {
            Assert.True(_parser.HasEscHandler(id), $"ESC {id}");
        }

        foreach (char flag in Charsets.All.Keys)
        {
            foreach (string intermediate in CharsetIntermediates)
            {
                FunctionIdentifier id = new(Intermediates: intermediate, Final: flag.ToString());
                Assert.True(_parser.HasEscHandler(id), $"ESC {id}");
            }
        }
    }

    [Fact]
    public void Every_EXECUTE_handler_of_the_xterm_js_constructor_is_registered()
    {
        foreach (string flag in ExpectedExecute)
        {
            Assert.True(_parser.HasExecuteHandler(flag), $"EXECUTE {(int)flag[0]:x2}");
        }
    }

    [Fact]
    public void Every_OSC_and_DCS_handler_of_the_xterm_js_constructor_is_registered()
    {
        foreach (int ident in new[] { 0, 1, 2, 4, 8, 10, 11, 12, 104, 110, 111, 112 })
        {
            _handler.Parse($"\x1b]{ident};\x07");
        }

        _handler.Parse("\x1bP$q\x1b\\");
        Assert.DoesNotContain(_log.DebugMessages, m => m.StartsWith("Unknown OSC code", StringComparison.Ordinal));
        Assert.DoesNotContain(_log.DebugMessages, m => m.StartsWith("Unknown DCS code", StringComparison.Ordinal));
    }
}
