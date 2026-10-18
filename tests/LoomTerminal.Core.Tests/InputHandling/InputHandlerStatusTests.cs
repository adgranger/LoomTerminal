// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerStatusTests : InputHandlerTestBase
{
    // xterm.js: 'InputHandler' › 'setCursorStyle' › 'should call Terminal.setOption with correct params'
    [Fact]
    public void SetCursorStyle_sets_the_style_and_blink_of_the_DEC_private_modes()
    {
        InputHandler.SetCursorStyle(Params.FromArray([0]));
        Assert.Null(CoreService.DecPrivateModes.CursorStyle);
        Assert.Null(CoreService.DecPrivateModes.CursorBlink);

        (int Param, CursorStyle Style, bool Blink)[] cases =
        [
            (1, CursorStyle.Block, true),
            (2, CursorStyle.Block, false),
            (3, CursorStyle.Underline, true),
            (4, CursorStyle.Underline, false),
            (5, CursorStyle.Bar, true),
            (6, CursorStyle.Bar, false),
        ];
        foreach ((int param, CursorStyle style, bool blink) in cases)
        {
            InputHandler.SetCursorStyle(Params.FromArray([param]));
            Assert.Equal(style, CoreService.DecPrivateModes.CursorStyle);
            Assert.Equal(blink, CoreService.DecPrivateModes.CursorBlink);
        }
    }

    // xterm.js: 'InputHandler' › 'BS with reverseWraparound set/unset' › 'reverseWraparound set' › 'should not reverse outside of scroll margins'
    [Fact]
    public void Backspace_reverse_wraparound_set_should_not_reverse_outside_of_scroll_margins()
    {
        // tty ICANON sends <BS SP BS> on pressing BS
        const string TtyBs = "\x08 \x08";
        string Repeat100 = string.Concat(Enumerable.Repeat(TtyBs, 100));
        BufferService.Resize(5, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        BufferService.Reset();

        // prepare buffer content
        InputHandler.Parse("#####abcdefghijklmnopqrstuvwxy");
        Assert.Equal<string>(["#####", "abcde", "fghij", "klmno", "pqrst", "uvwxy"], GetLines(6));
        Assert.Equal(1, BufferService.Buffers.Active.YDisp);
        Assert.Equal(5, BufferService.Buffers.Active.X);
        Assert.Equal(4, BufferService.Buffers.Active.Y);
        InputHandler.Parse(Repeat100);
        Assert.Equal<string>(["#####", "abcde", "fghij", "klmno", "pqrst", "    y"], GetLines(6));

        InputHandler.Parse("\x1b[?45h");
        InputHandler.Parse("uvwxy");

        // set top/bottom to 1/3 (0-based)
        InputHandler.Parse("\x1b[2;4r");

        // place cursor below scroll bottom
        BufferService.Buffers.Active.X = 5;
        BufferService.Buffers.Active.Y = 4;
        InputHandler.Parse(Repeat100);
        Assert.Equal<string>(["#####", "abcde", "fghij", "klmno", "pqrst", "     "], GetLines(6));

        InputHandler.Parse("uvwxy");

        // place cursor within scroll margins
        BufferService.Buffers.Active.X = 5;
        BufferService.Buffers.Active.Y = 3;
        InputHandler.Parse(Repeat100);
        Assert.Equal<string>(["#####", "abcde", "     ", "     ", "     ", "uvwxy"], GetLines(6));
        Assert.Equal(0, BufferService.Buffers.Active.X);
        Assert.Equal(BufferService.Buffers.Active.ScrollTop, BufferService.Buffers.Active.Y); // stops at 0, scrollTop

        InputHandler.Parse("fghijklmnopqrst");

        // place cursor above scroll top
        BufferService.Buffers.Active.X = 5;
        BufferService.Buffers.Active.Y = 0;
        InputHandler.Parse(Repeat100);
        Assert.Equal<string>(["#####", "     ", "fghij", "klmno", "pqrst", "uvwxy"], GetLines(6));
    }

    [Fact]
    public void DSR_6_reports_the_cursor_position()
    {
        List<string> sent = [];
        CoreService.OnData.Subscribe(sent.Add);
        InputHandler.Parse("\x1b[3;5H\x1b[6n");
        Assert.Equal<string>(["\x1b[3;5R"], sent);
    }

    [Fact]
    public void DSR_5_and_private_DSR_6_report_the_status_and_the_cursor_position()
    {
        List<string> sent = [];
        CoreService.OnData.Subscribe(sent.Add);
        InputHandler.Parse("\x1b[5n\x1b[3;5H\x1b[?6n");
        Assert.Equal<string>(["\x1b[0n", "\x1b[?3;5R"], sent);
    }

    private void Resize10X10() => BufferService.Resize(10, 10);

    // xterm.js: 'InputHandler' › 'DECSTBM - scroll margins' › 'should default to whole viewport'
    [Fact]
    public void DECSTBM_should_default_to_whole_viewport()
    {
        Resize10X10();
        InputHandler.Parse("\x1b[r");
        Assert.Equal(0, BufferService.Buffer.ScrollTop);
        Assert.Equal(9, BufferService.Buffer.ScrollBottom);
        InputHandler.Parse("\x1b[3;7r");
        Assert.Equal(2, BufferService.Buffer.ScrollTop);
        Assert.Equal(6, BufferService.Buffer.ScrollBottom);
        InputHandler.Parse("\x1b[0;0r");
        Assert.Equal(0, BufferService.Buffer.ScrollTop);
        Assert.Equal(9, BufferService.Buffer.ScrollBottom);
    }

    // xterm.js: 'InputHandler' › 'DECSTBM - scroll margins' › 'should clamp bottom'
    [Fact]
    public void DECSTBM_should_clamp_bottom()
    {
        Resize10X10();
        InputHandler.Parse("\x1b[3;1000r");
        Assert.Equal(2, BufferService.Buffer.ScrollTop);
        Assert.Equal(9, BufferService.Buffer.ScrollBottom);
    }

    // xterm.js: 'InputHandler' › 'DECSTBM - scroll margins' › 'should only apply for top < bottom'
    [Fact]
    public void DECSTBM_should_only_apply_for_top_bottom()
    {
        Resize10X10();
        InputHandler.Parse("\x1b[7;2r");
        Assert.Equal(0, BufferService.Buffer.ScrollTop);
        Assert.Equal(9, BufferService.Buffer.ScrollBottom);
    }

    // xterm.js: 'InputHandler' › 'DECSTBM - scroll margins' › 'should home cursor'
    [Fact]
    public void DECSTBM_should_home_cursor()
    {
        Resize10X10();
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[2;7r");
        Assert.Equal<int>([0, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'scroll margins' › 'scrollUp'
    [Fact]
    public void Scroll_margins_scrollUp()
    {
        Resize10X10();
        InputHandler.Parse("0\r\n1\r\n2\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\x1b[2;4r\x1b[2Sm");
        Assert.Equal<string>(["m", "3", "", "", "4", "5", "6", "7", "8", "9"], GetLines());
    }

    // xterm.js: 'InputHandler' › 'scroll margins' › 'scrollDown'
    [Fact]
    public void Scroll_margins_scrollDown()
    {
        Resize10X10();
        InputHandler.Parse("0\r\n1\r\n2\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\x1b[2;4r\x1b[2Tm");
        Assert.Equal<string>(["m", "", "", "1", "4", "5", "6", "7", "8", "9"], GetLines());
    }

    // xterm.js: 'InputHandler' › 'scroll margins' › 'insertLines - out of margins'
    [Fact]
    public void Scroll_margins_insertLines_out_of_margins()
    {
        Resize10X10();
        InputHandler.Parse("0\r\n1\r\n2\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\x1b[3;6r");
        Assert.Equal(2, BufferService.Buffer.ScrollTop);
        Assert.Equal(5, BufferService.Buffer.ScrollBottom);
        InputHandler.Parse("\x1b[2Lm");
        Assert.Equal<string>(["m", "1", "2", "3", "4", "5", "6", "7", "8", "9"], GetLines());
        InputHandler.Parse("\x1b[2H\x1b[2Ln");
        Assert.Equal<string>(["m", "n", "2", "3", "4", "5", "6", "7", "8", "9"], GetLines());
        // skip below scrollbottom
        InputHandler.Parse("\x1b[7H\x1b[2Lo");
        Assert.Equal<string>(["m", "n", "2", "3", "4", "5", "o", "7", "8", "9"], GetLines());
        InputHandler.Parse("\x1b[8H\x1b[2Lp");
        Assert.Equal<string>(["m", "n", "2", "3", "4", "5", "o", "p", "8", "9"], GetLines());
        InputHandler.Parse("\x1b[100H\x1b[2Lq");
        Assert.Equal<string>(["m", "n", "2", "3", "4", "5", "o", "p", "8", "q"], GetLines());
    }

    // xterm.js: 'InputHandler' › 'scroll margins' › 'insertLines - within margins'
    [Fact]
    public void Scroll_margins_insertLines_within_margins()
    {
        Resize10X10();
        InputHandler.Parse("0\r\n1\r\n2\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\x1b[3;6r");
        Assert.Equal(2, BufferService.Buffer.ScrollTop);
        Assert.Equal(5, BufferService.Buffer.ScrollBottom);
        InputHandler.Parse("\x1b[3H\x1b[2Lm");
        Assert.Equal<string>(["0", "1", "m", "", "2", "3", "6", "7", "8", "9"], GetLines());
        InputHandler.Parse("\x1b[6H\x1b[2Ln");
        Assert.Equal<string>(["0", "1", "m", "", "2", "n", "6", "7", "8", "9"], GetLines());
    }

    // xterm.js: 'InputHandler' › 'scroll margins' › 'deleteLines - out of margins'
    [Fact]
    public void Scroll_margins_deleteLines_out_of_margins()
    {
        Resize10X10();
        InputHandler.Parse("0\r\n1\r\n2\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\x1b[3;6r");
        Assert.Equal(2, BufferService.Buffer.ScrollTop);
        Assert.Equal(5, BufferService.Buffer.ScrollBottom);
        InputHandler.Parse("\x1b[2Mm");
        Assert.Equal<string>(["m", "1", "2", "3", "4", "5", "6", "7", "8", "9"], GetLines());
        InputHandler.Parse("\x1b[2H\x1b[2Mn");
        Assert.Equal<string>(["m", "n", "2", "3", "4", "5", "6", "7", "8", "9"], GetLines());
        // skip below scrollbottom
        InputHandler.Parse("\x1b[7H\x1b[2Mo");
        Assert.Equal<string>(["m", "n", "2", "3", "4", "5", "o", "7", "8", "9"], GetLines());
        InputHandler.Parse("\x1b[8H\x1b[2Mp");
        Assert.Equal<string>(["m", "n", "2", "3", "4", "5", "o", "p", "8", "9"], GetLines());
        InputHandler.Parse("\x1b[100H\x1b[2Mq");
        Assert.Equal<string>(["m", "n", "2", "3", "4", "5", "o", "p", "8", "q"], GetLines());
    }

    // xterm.js: 'InputHandler' › 'scroll margins' › 'deleteLines - within margins'
    [Fact]
    public void Scroll_margins_deleteLines_within_margins()
    {
        Resize10X10();
        InputHandler.Parse("0\r\n1\r\n2\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\x1b[3;6r");
        Assert.Equal(2, BufferService.Buffer.ScrollTop);
        Assert.Equal(5, BufferService.Buffer.ScrollBottom);
        InputHandler.Parse("\x1b[6H\x1b[2Mm");
        Assert.Equal<string>(["0", "1", "2", "3", "4", "m", "6", "7", "8", "9"], GetLines());
        InputHandler.Parse("\x1b[3H\x1b[2Mn");
        Assert.Equal<string>(["0", "1", "n", "m", "", "", "6", "7", "8", "9"], GetLines());
    }

    // xterm.js: 'InputHandler' › 'DECSTR' › 'should reset IRM'
    [Fact]
    public void DECSTR_should_reset_IRM()
    {
        BufferService.Resize(10, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        InputHandler.Parse("01234567890123");
        InputHandler.Parse("\x1b[4h");
        Assert.True(CoreService.Modes.InsertMode);
        InputHandler.Parse("\x1b[!p");
        Assert.False(CoreService.Modes.InsertMode);
    }

    // xterm.js: 'InputHandler' › 'DECSTR' › 'should reset cursor visibility'
    [Fact]
    public void DECSTR_should_reset_cursor_visibility()
    {
        BufferService.Resize(10, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        InputHandler.Parse("01234567890123");
        InputHandler.Parse("\x1b[?25l");
        Assert.True(CoreService.IsCursorHidden);
        InputHandler.Parse("\x1b[!p");
        Assert.False(CoreService.IsCursorHidden);
    }

    // xterm.js: 'InputHandler' › 'DECSTR' › 'should reset scroll margins'
    [Fact]
    public void DECSTR_should_reset_scroll_margins()
    {
        BufferService.Resize(10, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        InputHandler.Parse("01234567890123");
        InputHandler.Parse("\x1b[2;4r");
        Assert.Equal(1, BufferService.Buffer.ScrollTop);
        Assert.Equal(3, BufferService.Buffer.ScrollBottom);
        InputHandler.Parse("\x1b[!p");
        Assert.Equal(0, BufferService.Buffer.ScrollTop);
        Assert.Equal(BufferService.Rows - 1, BufferService.Buffer.ScrollBottom);
    }

    // xterm.js: 'InputHandler' › 'DECSTR' › 'should reset text attributes'
    [Fact]
    public void DECSTR_should_reset_text_attributes()
    {
        BufferService.Resize(10, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        InputHandler.Parse("01234567890123");
        InputHandler.Parse("\x1b[1;2;32;43m");
        Assert.NotEqual(0u, InputHandler.CurAttrData.IsBold());
        InputHandler.Parse("\x1b[!p");
        Assert.Equal(0u, InputHandler.CurAttrData.IsBold());
        Assert.Equal(0u, InputHandler.CurAttrData.Fg);
        Assert.Equal(0u, InputHandler.CurAttrData.Bg);
    }

    // xterm.js: 'InputHandler' › 'DECSTR' › 'should reset DECSC data'
    [Fact]
    public void DECSTR_should_reset_DECSC_data()
    {
        BufferService.Resize(10, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        InputHandler.Parse("01234567890123");
        InputHandler.Parse("\u001b7");
        Assert.Equal(4, BufferService.Buffer.SavedX);
        Assert.Equal(1, BufferService.Buffer.SavedY);
        InputHandler.Parse("\x1b[!p");
        Assert.Equal(0, BufferService.Buffer.SavedX);
        Assert.Equal(0, BufferService.Buffer.SavedY);
    }

    // xterm.js: 'InputHandler' › 'DECSTR' › 'should reset DECOM'
    [Fact]
    public void DECSTR_should_reset_DECOM()
    {
        BufferService.Resize(10, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        InputHandler.Parse("01234567890123");
        InputHandler.Parse("\x1b[?6h");
        Assert.True(CoreService.DecPrivateModes.Origin);
        InputHandler.Parse("\x1b[!p");
        Assert.False(CoreService.DecPrivateModes.Origin);
    }
}
