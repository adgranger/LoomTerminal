// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Services;
using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerModeTests : InputHandlerTestBase
{
    // tty ICANON sends <BS SP BS> on pressing BS
    private const string TtyBs = "\x08 \x08";

    public InputHandlerModeTests()
    {
        BufferService.Resize(5, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
    }

    private static string Repeat(string s, int count) => string.Concat(Enumerable.Repeat(s, count));

    // xterm.js: 'InputHandler' › 'setMode' › 'should toggle bracketedPasteMode'
    [Fact]
    public void SetModePrivate_and_ResetModePrivate_toggle_bracketedPasteMode()
    {
        // Set bracketed paste mode
        InputHandler.SetModePrivate(Params.FromArray([2004]));
        Assert.True(CoreService.DecPrivateModes.BracketedPasteMode);

        // Reset bracketed paste mode
        InputHandler.ResetModePrivate(Params.FromArray([2004]));
        Assert.False(CoreService.DecPrivateModes.BracketedPasteMode);
    }

    [Fact]
    public void DECSET_1_sets_the_application_cursor_keys()
    {
        InputHandler.Parse("\x1b[?1h");
        Assert.True(CoreService.DecPrivateModes.ApplicationCursorKeys);
        InputHandler.Parse("\x1b[?1l");
        Assert.False(CoreService.DecPrivateModes.ApplicationCursorKeys);
    }

    [Fact]
    public void DECSET_1000_and_1006_select_the_VT200_protocol_and_the_SGR_encoding()
    {
        using var mouse = new CoreMouseService(BufferService, CoreService, OptionsService);
        using var handler = new TestInputHandler(
            BufferService, new MockCharsetService(), CoreService, LogService, OptionsService, OscLinkService, mouse, new MockUnicodeService());
        handler.Parse("\x1b[?1000h\x1b[?1006h");
        Assert.Equal("VT200", mouse.ActiveProtocol);
        Assert.Equal("SGR", mouse.ActiveEncoding);
        handler.Parse("\x1b[?1000l\x1b[?1006l");
        Assert.Equal("NONE", mouse.ActiveProtocol);
        Assert.Equal("DEFAULT", mouse.ActiveEncoding);
    }

    [Fact]
    public void DECRST_25_hides_the_cursor_and_DECSET_25_shows_it()
    {
        InputHandler.Parse("\x1b[?25l");
        Assert.True(CoreService.IsCursorHidden);
        InputHandler.Parse("\x1b[?25h");
        Assert.False(CoreService.IsCursorHidden);
    }

    [Fact]
    public void SM_and_RM_toggle_insert_mode_and_ConvertEol()
    {
        InputHandler.Parse("\x1b[4h\x1b[20h");
        Assert.True(CoreService.Modes.InsertMode);
        Assert.True(OptionsService.RawOptions.ConvertEol);
        InputHandler.Parse("\x1b[4l\x1b[20l");
        Assert.False(CoreService.Modes.InsertMode);
        Assert.False(OptionsService.RawOptions.ConvertEol);
    }

    [Fact]
    public void DECSET_6_turns_on_origin_mode_and_homes_the_cursor()
    {
        InputHandler.Parse("abc\x1b[?6h");
        Assert.True(CoreService.DecPrivateModes.Origin);
        Assert.Equal<int>([0, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'BS with reverseWraparound set/unset' › 'reverseWraparound set' (second block)
    // xterm.js: 'InputHandler' › 'BS with reverseWraparound set/unset' › 'reverseWraparound set' › 'can delete last cell'
    [Fact]
    public void Backspace_reverse_wraparound_block2_set_can_delete_the_last_cell()
    {
        InputHandler.Parse("\x1b[?45h");
        InputHandler.Parse("12345");
        InputHandler.Parse(TtyBs);
        Assert.Equal<string>(["1234 "], GetLines(1));
        InputHandler.Parse(Repeat(TtyBs, 7));
        Assert.Equal<string>(["     "], GetLines(1));
    }

    // xterm.js: 'InputHandler' › 'BS with reverseWraparound set/unset' › 'reverseWraparound set' › 'can access prev line if wrapped'
    [Fact]
    public void Backspace_reverse_wraparound_block2_set_can_access_the_previous_line_if_wrapped()
    {
        InputHandler.Parse("\x1b[?45h");
        InputHandler.Parse("1234512345");
        InputHandler.Parse(TtyBs);
        Assert.Equal<string>(["12345", "1234 "], GetLines(2));
        InputHandler.Parse(Repeat(TtyBs, 7));
        Assert.Equal<string>(["12   ", "     "], GetLines(2));
    }

    // xterm.js: 'InputHandler' › 'BS with reverseWraparound set/unset' › 'reverseWraparound set' › 'should lift isWrapped'
    [Fact]
    public void Backspace_reverse_wraparound_block2_set_lifts_isWrapped()
    {
        InputHandler.Parse("\x1b[?45h");
        InputHandler.Parse("1234512345");
        Assert.True(BufferService.Buffer.Lines.Get(1)?.IsWrapped);
        InputHandler.Parse(Repeat(TtyBs, 7));
        Assert.False(BufferService.Buffer.Lines.Get(1)?.IsWrapped);
    }

    // xterm.js: 'InputHandler' › 'BS with reverseWraparound set/unset' › 'reverseWraparound set' › 'stops at hard NLs'
    [Fact]
    public void Backspace_reverse_wraparound_block2_set_stops_at_hard_NLs()
    {
        InputHandler.Parse("\x1b[?45h");
        InputHandler.Parse("12345\r\n");
        InputHandler.Parse("1234512345");
        InputHandler.Parse(Repeat(TtyBs, 50));
        Assert.Equal<string>(["12345", "     ", "     "], GetLines(3));
        Assert.Equal(0, BufferService.Buffer.X);
        Assert.Equal(1, BufferService.Buffer.Y);
    }

    // xterm.js: 'InputHandler' › 'BS with reverseWraparound set/unset' › 'reverseWraparound set' › 'handles wide chars correctly'
    [Fact]
    public void Backspace_reverse_wraparound_block2_set_handles_wide_chars_correctly()
    {
        InputHandler.Parse("\x1b[?45h");
        InputHandler.Parse("￥￥￥");
        Assert.Equal<string>(["￥￥", "￥"], GetLines(2));
        InputHandler.Parse(TtyBs);
        Assert.Equal<string>(["￥￥", "  "], GetLines(2));
        Assert.Equal(1, BufferService.Buffer.X);
        InputHandler.Parse(TtyBs);
        Assert.Equal<string>(["￥￥", "  "], GetLines(2));
        Assert.Equal(0, BufferService.Buffer.X);
        InputHandler.Parse(TtyBs);
        Assert.Equal<string>(["￥  ", "  "], GetLines(2));
        Assert.Equal(3, BufferService.Buffer.X);
        InputHandler.Parse(TtyBs);
        Assert.Equal<string>(["￥  ", "  "], GetLines(2));
        Assert.Equal(2, BufferService.Buffer.X);
        InputHandler.Parse(TtyBs);
        Assert.Equal<string>(["    ", "  "], GetLines(2));
        Assert.Equal(1, BufferService.Buffer.X);
        InputHandler.Parse(TtyBs);
        Assert.Equal<string>(["    ", "  "], GetLines(2));
        Assert.Equal(0, BufferService.Buffer.X);
    }
}
