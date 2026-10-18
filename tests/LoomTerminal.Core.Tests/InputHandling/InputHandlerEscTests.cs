// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerEscTests() : InputHandlerTestBase(new CharsetService())
{
    // xterm.js: 'InputHandler' › 'save and restore cursor'
    [Fact]
    public void Save_and_restore_cursor()
    {
        BufferService.Buffer.X = 1;
        BufferService.Buffer.Y = 2;
        BufferService.Buffer.YBase = 0;
        InputHandler.CurAttrData.Fg = 3;

        // Save cursor position
        InputHandler.SaveCursor();
        Assert.Equal(1, BufferService.Buffer.X);
        Assert.Equal(2, BufferService.Buffer.Y);
        Assert.Equal(3u, InputHandler.CurAttrData.Fg);

        // Change cursor position
        BufferService.Buffer.X = 10;
        BufferService.Buffer.Y = 20;
        InputHandler.CurAttrData.Fg = 30;

        // Restore cursor position
        InputHandler.RestoreCursor();
        Assert.Equal(1, BufferService.Buffer.X);
        Assert.Equal(2, BufferService.Buffer.Y);
        Assert.Equal(3u, InputHandler.CurAttrData.Fg);
    }

    [Fact]
    public void ESC_7_and_ESC_8_save_and_restore_the_cursor()
    {
        InputHandler.Parse("ab\x1b" + "7cd\x1b" + "8e");
        Assert.Equal<string>(["abed"], GetLines(1));
    }

    [Fact]
    public void ESC_paren_0_selects_the_DEC_line_drawing_set_and_ESC_paren_B_returns_to_ASCII()
    {
        InputHandler.Parse("\x1b(0q\x1b(Bq");
        Assert.Equal<string>(["─q"], GetLines(1));
    }

    [Fact]
    public void ESC_M_at_the_top_of_the_scroll_region_scrolls_down()
    {
        InputHandler.Parse("first\x1bM");
        Assert.Equal<string>(["", "first"], GetLines(2));
    }

    [Fact]
    public void ESC_M_below_the_top_moves_the_cursor_up()
    {
        InputHandler.Parse("\x1b[3;1H\x1bM");
        Assert.Equal<int>([0, 1], GetCursor());
    }

    [Fact]
    public void ESC_D_and_C1_IND_move_down_and_ESC_E_and_C1_NEL_also_go_to_the_first_column()
    {
        InputHandler.Parse("ab\x1b" + "D");
        Assert.Equal<int>([2, 1], GetCursor());
        InputHandler.Parse("\x1b" + "E");
        Assert.Equal<int>([0, 2], GetCursor());
        InputHandler.Parse("ab\u0084");
        Assert.Equal<int>([2, 3], GetCursor());
        InputHandler.Parse("\u0085");
        Assert.Equal<int>([0, 4], GetCursor());
    }

    [Fact]
    public void ESC_hash_8_fills_the_screen_with_E()
    {
        InputHandler.Parse("\x1b#8");
        Assert.Equal(new string('E', 80), GetLines()[0]);
        Assert.Equal(new string('E', 80), GetLines()[29]);
        Assert.Equal<int>([0, 0], GetCursor());
    }

    [Fact]
    public void ESC_equals_and_ESC_greater_than_switch_the_application_keypad_mode()
    {
        InputHandler.Parse("\x1b=");
        Assert.True(CoreService.DecPrivateModes.ApplicationKeypad);
        InputHandler.Parse("\x1b>");
        Assert.False(CoreService.DecPrivateModes.ApplicationKeypad);
    }

    [Fact]
    public void ESC_c_requests_a_reset()
    {
        int resets = 0;
        InputHandler.OnRequestReset.Subscribe(_ => resets++);
        InputHandler.Parse("\x1b" + "c");
        Assert.Equal(1, resets);
    }
}
