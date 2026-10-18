// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerInsertDeleteTests : InputHandlerTestBase
{
    private const string Wide20 = "￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥";

    private void PrepareScrollTests()
    {
        BufferService.Resize(5, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        BufferService.Reset();
    }

    // xterm.js: 'InputHandler' › 'SL/SR/DECIC/DECDC' › 'SL (scrollLeft)'
    [Fact]
    public void SL_scrolls_the_region_left()
    {
        PrepareScrollTests();
        InputHandler.Parse(string.Concat(Enumerable.Repeat("12345", 6)));
        InputHandler.Parse("\x1b[ @");
        Assert.Equal<string>(["12345", "2345", "2345", "2345", "2345", "2345"], GetLines(6));
        InputHandler.Parse("\x1b[0 @");
        Assert.Equal<string>(["12345", "345", "345", "345", "345", "345"], GetLines(6));
        InputHandler.Parse("\x1b[2 @");
        Assert.Equal<string>(["12345", "5", "5", "5", "5", "5"], GetLines(6));
    }

    // xterm.js: 'InputHandler' › 'SL/SR/DECIC/DECDC' › 'SR (scrollRight)'
    [Fact]
    public void SR_scrolls_the_region_right()
    {
        PrepareScrollTests();
        InputHandler.Parse(string.Concat(Enumerable.Repeat("12345", 6)));
        InputHandler.Parse("\x1b[ A");
        Assert.Equal<string>(["12345", " 1234", " 1234", " 1234", " 1234", " 1234"], GetLines(6));
        InputHandler.Parse("\x1b[0 A");
        Assert.Equal<string>(["12345", "  123", "  123", "  123", "  123", "  123"], GetLines(6));
        InputHandler.Parse("\x1b[2 A");
        Assert.Equal<string>(["12345", "    1", "    1", "    1", "    1", "    1"], GetLines(6));
    }

    // xterm.js: 'InputHandler' › 'SL/SR/DECIC/DECDC' › 'insertColumns (DECIC)'
    [Fact]
    public void DECIC_inserts_columns()
    {
        PrepareScrollTests();
        string[] one = ["12345", "12 34", "12 34", "12 34", "12 34", "12 34"];
        string[] two = ["12345", "12  3", "12  3", "12  3", "12  3", "12  3"];
        string thirty = string.Concat(Enumerable.Repeat("12345", 6));
        InputHandler.Parse(thirty);
        InputHandler.Parse("\x1b[3;3H");
        InputHandler.Parse("\x1b['}");
        Assert.Equal<string>(one, GetLines(6));
        BufferService.Reset();
        InputHandler.Parse(thirty);
        InputHandler.Parse("\x1b[3;3H");
        InputHandler.Parse("\x1b[1'}");
        Assert.Equal<string>(one, GetLines(6));
        BufferService.Reset();
        InputHandler.Parse(thirty);
        InputHandler.Parse("\x1b[3;3H");
        InputHandler.Parse("\x1b[2'}");
        Assert.Equal<string>(two, GetLines(6));
    }

    // xterm.js: 'InputHandler' › 'SL/SR/DECIC/DECDC' › 'deleteColumns (DECDC)'
    [Fact]
    public void DECDC_deletes_columns()
    {
        PrepareScrollTests();
        string[] one = ["12345", "1245", "1245", "1245", "1245", "1245"];
        string[] two = ["12345", "125", "125", "125", "125", "125"];
        string thirty = string.Concat(Enumerable.Repeat("12345", 6));
        InputHandler.Parse(thirty);
        InputHandler.Parse("\x1b[3;3H");
        InputHandler.Parse("\x1b['~");
        Assert.Equal<string>(one, GetLines(6));
        BufferService.Reset();
        InputHandler.Parse(thirty);
        InputHandler.Parse("\x1b[3;3H");
        InputHandler.Parse("\x1b[1'~");
        Assert.Equal<string>(one, GetLines(6));
        BufferService.Reset();
        InputHandler.Parse(thirty);
        InputHandler.Parse("\x1b[3;3H");
        InputHandler.Parse("\x1b[2'~");
        Assert.Equal<string>(two, GetLines(6));
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'DCH'
    [Fact]
    public void DCH_clamps_the_cursor_into_the_addressable_range()
    {
        BufferService.Resize(10, 10);
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[P");
        Assert.Equal<int>([9, 9], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[P");
        Assert.Equal<int>([0, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'DCH - should delete last cell'
    [Fact]
    public void DCH_deletes_the_last_cell()
    {
        BufferService.Resize(10, 10);
        InputHandler.Parse("0123456789\x1b[P");
        Assert.Equal("012345678 ", BufferService.Buffer.Lines.Get(0)!.TranslateToString(false));
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'ICH'
    [Fact]
    public void ICH_clamps_the_cursor_into_the_addressable_range()
    {
        BufferService.Resize(10, 10);
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[@");
        Assert.Equal<int>([9, 9], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[@");
        Assert.Equal<int>([0, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'ICH - should delete last cell'
    [Fact]
    public void ICH_deletes_the_last_cell()
    {
        BufferService.Resize(10, 10);
        InputHandler.Parse("0123456789\x1b[@");
        Assert.Equal("012345678 ", BufferService.Buffer.Lines.Get(0)!.TranslateToString(false));
    }

    private void PrepareWideChars()
    {
        BufferService.Resize(10, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        InputHandler.Parse(Wide20);
    }

    // xterm.js: 'InputHandler' › 'should correctly reset cells taken by wide chars' › 'ICH'
    [Fact]
    public void ICH_resets_cells_taken_by_wide_chars()
    {
        PrepareWideChars();
        InputHandler.Parse("\x1b[1;6H\x1b[@");
        Assert.Equal<string>(["￥￥   ￥", "￥￥￥￥￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[2;4H\x1b[2@");
        Assert.Equal<string>(["￥￥   ￥", "￥    ￥￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[3;4H\x1b[3@");
        Assert.Equal<string>(["￥￥   ￥", "￥    ￥￥", "￥     ￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[4;4H\x1b[4@");
        Assert.Equal<string>(["￥￥   ￥", "￥    ￥￥", "￥     ￥", "￥      ￥", ""], GetLines());
    }

    // xterm.js: 'InputHandler' › 'should correctly reset cells taken by wide chars' › 'DCH'
    [Fact]
    public void DCH_resets_cells_taken_by_wide_chars()
    {
        PrepareWideChars();
        InputHandler.Parse("\x1b[1;6H\x1b[P");
        Assert.Equal<string>(["￥￥ ￥￥", "￥￥￥￥￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[2;6H\x1b[2P");
        Assert.Equal<string>(["￥￥ ￥￥", "￥￥  ￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[3;6H\x1b[3P");
        Assert.Equal<string>(["￥￥ ￥￥", "￥￥  ￥", "￥￥ ￥", "￥￥￥￥￥", ""], GetLines());
    }
}
