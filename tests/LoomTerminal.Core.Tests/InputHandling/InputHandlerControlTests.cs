// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerControlTests : InputHandlerTestBase
{
    // tty ICANON sends <BS SP BS> on pressing BS
    private const string TtyBs = "\x08 \x08";

    public InputHandlerControlTests()
    {
        BufferService.Resize(5, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
    }

    // xterm.js: 'InputHandler' › 'BS with reverseWraparound set/unset' › 'reverseWraparound unset (default)' › 'cannot delete last cell'
    [Fact]
    public void Backspace_reverse_wraparound_block2_unset_cannot_delete_last_cell()
    {
        InputHandler.Parse("12345");
        InputHandler.Parse(TtyBs);
        Assert.Equal<string>(["123 5"], GetLines(1));
        InputHandler.Parse(string.Concat(Enumerable.Repeat(TtyBs, 10)));
        Assert.Equal<string>(["    5"], GetLines(1));
    }

    // xterm.js: 'InputHandler' › 'BS with reverseWraparound set/unset' › 'reverseWraparound unset (default)' › 'cannot access prev line'
    [Fact]
    public void Backspace_reverse_wraparound_block2_unset_cannot_access_prev_line()
    {
        InputHandler.Parse("1234512345");
        InputHandler.Parse(TtyBs);
        Assert.Equal<string>(["12345", "123 5"], GetLines(2));
        InputHandler.Parse(string.Concat(Enumerable.Repeat(TtyBs, 10)));
        Assert.Equal<string>(["12345", "    5"], GetLines(2));
    }

    [Fact]
    public void Line_feed_with_ConvertEol_also_returns_the_cursor_to_column_0()
    {
        OptionsService.RawOptions = OptionsService.RawOptions with { ConvertEol = true };
        InputHandler.Parse("abc\n");
        Assert.Equal<int>([0, 1], GetCursor());
    }

    [Fact]
    public void Line_feed_without_ConvertEol_keeps_the_column()
    {
        InputHandler.Parse("abc\n");
        Assert.Equal<int>([3, 1], GetCursor());
    }

    [Fact]
    public void BEL_fires_OnRequestBell()
    {
        int bells = 0;
        InputHandler.OnRequestBell.Subscribe(_ => bells++);
        InputHandler.Parse("\x07");
        Assert.Equal(1, bells);
    }

    [Fact]
    public void Carriage_return_moves_to_column_0_and_VT_and_FF_act_as_line_feeds()
    {
        InputHandler.Parse("abc\r");
        Assert.Equal<int>([0, 0], GetCursor());
        InputHandler.Parse("\v\f");
        Assert.Equal<int>([0, 2], GetCursor());
    }

    [Fact]
    public void Tab_moves_to_the_next_tab_stop_and_reports_it_in_screen_reader_mode()
    {
        BufferService.Resize(20, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { ScreenReaderMode = true };
        List<int> tabs = [];
        InputHandler.OnA11yTab.Subscribe(tabs.Add);
        InputHandler.Parse("a\t");
        Assert.Equal<int>([8, 0], GetCursor());
        Assert.Equal<int>([7], tabs);
    }
}
