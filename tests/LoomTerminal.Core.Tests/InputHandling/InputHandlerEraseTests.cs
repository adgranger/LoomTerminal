// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerEraseTests : InputHandlerTestBase
{
    private const string Wide20 = "￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥";

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'ECH'
    [Fact]
    public void ECH_clamps_the_cursor_into_the_addressable_range()
    {
        BufferService.Resize(10, 10);
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[X");
        Assert.Equal<int>([9, 9], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[X");
        Assert.Equal<int>([0, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'ECH - should delete last cell'
    [Fact]
    public void ECH_deletes_the_last_cell()
    {
        BufferService.Resize(10, 10);
        InputHandler.Parse("0123456789\x1b[X");
        Assert.Equal("012345678 ", BufferService.Buffer.Lines.Get(0)!.TranslateToString(false));
    }

    private void PrepareWideChars()
    {
        BufferService.Resize(10, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        InputHandler.Parse(Wide20);
    }

    // xterm.js: 'InputHandler' › 'should correctly reset cells taken by wide chars' › 'EL'
    [Fact]
    public void EL_resets_cells_taken_by_wide_chars()
    {
        PrepareWideChars();
        InputHandler.Parse("\x1b[1;6H\x1b[K#");
        Assert.Equal<string>(["￥￥ #", "￥￥￥￥￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[2;5H\x1b[1K");
        Assert.Equal<string>(["￥￥ #", "      ￥￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[3;6H\x1b[1K");
        Assert.Equal<string>(["￥￥ #", "      ￥￥", "      ￥￥", "￥￥￥￥￥", ""], GetLines());
    }

    // xterm.js: 'InputHandler' › 'should correctly reset cells taken by wide chars' › 'ECH'
    [Fact]
    public void ECH_resets_cells_taken_by_wide_chars()
    {
        PrepareWideChars();
        InputHandler.Parse("\x1b[1;6H\x1b[X");
        Assert.Equal<string>(["￥￥  ￥￥", "￥￥￥￥￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[2;6H\x1b[2X");
        Assert.Equal<string>(["￥￥  ￥￥", "￥￥    ￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[3;6H\x1b[3X");
        Assert.Equal<string>(["￥￥  ￥￥", "￥￥    ￥", "￥￥    ￥", "￥￥￥￥￥", ""], GetLines());
    }

    private static readonly string Ten = new('#', 10);

    private void AssertCursorStaysAtCols(string sequence, string[] expectedLines)
    {
        BufferService.Resize(10, 5);
        InputHandler.Parse(Ten + sequence);
        Assert.Equal(10, BufferService.Buffer.X);
        Assert.Equal<string>(expectedLines, GetLines());
    }

    // xterm.js: 'InputHandler' › 'EL/ED cursor at buffer.cols' › 'cursor should stay at cols / does not overflow' › 'EL0'
    [Fact]
    public void EL0_keeps_the_cursor_at_cols() => AssertCursorStaysAtCols("\x1b[0K", [Ten, "", "", "", ""]);

    // xterm.js: ... › 'EL1'
    [Fact]
    public void EL1_keeps_the_cursor_at_cols() => AssertCursorStaysAtCols("\x1b[1K", ["", "", "", "", ""]);

    // xterm.js: ... › 'EL2'
    [Fact]
    public void EL2_keeps_the_cursor_at_cols() => AssertCursorStaysAtCols("\x1b[2K", ["", "", "", "", ""]);

    // xterm.js: ... › 'ED0'
    [Fact]
    public void ED0_keeps_the_cursor_at_cols() => AssertCursorStaysAtCols("\x1b[0J", [Ten, "", "", "", ""]);

    // xterm.js: ... › 'ED1'
    [Fact]
    public void ED1_keeps_the_cursor_at_cols() => AssertCursorStaysAtCols("\x1b[1J", ["", "", "", "", ""]);

    // xterm.js: ... › 'ED2'
    [Fact]
    public void ED2_keeps_the_cursor_at_cols() => AssertCursorStaysAtCols("\x1b[2J", ["", "", "", "", ""]);

    // xterm.js: ... › 'ED3'
    [Fact]
    public void ED3_keeps_the_cursor_at_cols() => AssertCursorStaysAtCols("\x1b[3J", [Ten, "", "", "", ""]);

    // xterm.js: 'InputHandler' › 'DECSCA and DECSED/DECSEL' › 'default is unprotected'
    [Fact]
    public void Text_is_unprotected_by_default()
    {
        InputHandler.Parse("some text");
        InputHandler.Parse("\x1b[?2K");
        Assert.Equal<string>(["", ""], GetLines(2));
        InputHandler.Parse("some text");
        InputHandler.Parse("\x1b[?2J");
        Assert.Equal<string>(["", ""], GetLines(2));
    }

    // xterm.js: 'InputHandler' › 'DECSCA and DECSED/DECSEL' › 'DECSCA 1 with DECSEL'
    [Fact]
    public void DECSCA_1_protects_cells_from_DECSEL()
    {
        InputHandler.Parse("###\x1b[1\"qlineerase\x1b[0\"q***");
        InputHandler.Parse("\x1b[?2K");
        Assert.Equal<string>(["   lineerase", ""], GetLines(2));

        // normal EL works as before
        InputHandler.Parse("\x1b[2K");
        Assert.Equal<string>(["", ""], GetLines(2));
    }

    // xterm.js: 'InputHandler' › 'DECSCA and DECSED/DECSEL' › 'DECSCA 1 with DECSED'
    [Fact]
    public void DECSCA_1_protects_cells_from_DECSED()
    {
        InputHandler.Parse("###\x1b[1\"qdisplayerase\x1b[0\"q***");
        InputHandler.Parse("\x1b[?2J");
        Assert.Equal<string>(["   displayerase", ""], GetLines(2));

        // normal ED works as before
        InputHandler.Parse("\x1b[2J");
        Assert.Equal<string>(["", ""], GetLines(2));
    }
}
