// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Services;
using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerRegressionTests : InputHandlerTestBase
{
    private static string A(int count) => new('a', count);

    private static string Spaces(int count) => new(' ', count);

    private List<string> TermContent(bool trim)
    {
        List<string> result = [];
        for (int i = 0; i < BufferService.Rows; ++i)
        {
            result.Add(BufferService.Buffer.Lines.Get(i)!.TranslateToString(trim));
        }

        return result;
    }

    private void SetCursor(int x, int y)
    {
        BufferService.Buffer.Y = y;
        BufferService.Buffer.X = x;
    }

    private void PrepareTwoLines()
    {
        // insert some data in first and second line
        InputHandler.Parse(A(BufferService.Cols - 10));
        InputHandler.Parse("1234567890");
        InputHandler.Parse(A(BufferService.Cols - 10));
        InputHandler.Parse("1234567890");
    }

    // xterm.js: 'InputHandler' › 'regression tests' › 'insertChars'
    [Fact]
    public void InsertChars_inserts_the_requested_number_of_blank_cells()
    {
        PrepareTwoLines();
        IBufferLine line1 = BufferService.Buffer.Lines.Get(0)!;
        string prefix = A(BufferService.Cols - 10);
        Assert.Equal(prefix + "1234567890", line1.TranslateToString(false));

        // insert one char from params = [0]
        SetCursor(70, 0);
        InputHandler.InsertChars(Params.FromArray([0]));
        Assert.Equal(prefix + " 123456789", line1.TranslateToString(false));

        // insert one char from params = [1]
        SetCursor(70, 0);
        InputHandler.InsertChars(Params.FromArray([1]));
        Assert.Equal(prefix + "  12345678", line1.TranslateToString(false));

        // insert two chars from params = [2]
        SetCursor(70, 0);
        InputHandler.InsertChars(Params.FromArray([2]));
        Assert.Equal(prefix + "    123456", line1.TranslateToString(false));

        // insert 10 chars from params = [10]
        SetCursor(70, 0);
        InputHandler.InsertChars(Params.FromArray([10]));
        Assert.Equal(prefix + "          ", line1.TranslateToString(false));
        Assert.Equal(prefix, line1.TranslateToString(true));
    }

    // xterm.js: 'InputHandler' › 'regression tests' › 'deleteChars'
    [Fact]
    public void DeleteChars_deletes_the_requested_number_of_cells()
    {
        PrepareTwoLines();
        IBufferLine line1 = BufferService.Buffer.Lines.Get(0)!;
        string prefix = A(BufferService.Cols - 10);
        Assert.Equal(prefix + "1234567890", line1.TranslateToString(false));

        // delete one char from params = [0]
        SetCursor(70, 0);
        InputHandler.DeleteChars(Params.FromArray([0]));
        Assert.Equal(prefix + "234567890 ", line1.TranslateToString(false));
        Assert.Equal(prefix + "234567890", line1.TranslateToString(true));

        // delete one char from params = [1]
        SetCursor(70, 0);
        InputHandler.DeleteChars(Params.FromArray([1]));
        Assert.Equal(prefix + "34567890  ", line1.TranslateToString(false));
        Assert.Equal(prefix + "34567890", line1.TranslateToString(true));

        // delete two chars from params = [2]
        SetCursor(70, 0);
        InputHandler.DeleteChars(Params.FromArray([2]));
        Assert.Equal(prefix + "567890    ", line1.TranslateToString(false));
        Assert.Equal(prefix + "567890", line1.TranslateToString(true));

        // delete 10 chars from params = [10]
        SetCursor(70, 0);
        InputHandler.DeleteChars(Params.FromArray([10]));
        Assert.Equal(prefix + "          ", line1.TranslateToString(false));
        Assert.Equal(prefix, line1.TranslateToString(true));
    }

    // xterm.js: 'InputHandler' › 'regression tests' › 'eraseInLine'
    [Fact]
    public void EraseInLine_erases_right_left_and_the_whole_line()
    {
        // fill 6 lines to test 3 different states
        InputHandler.Parse(A(BufferService.Cols));
        InputHandler.Parse(A(BufferService.Cols));
        InputHandler.Parse(A(BufferService.Cols));

        // params[0] - right erase
        SetCursor(70, 0);
        InputHandler.EraseInLine(Params.FromArray([0]));
        Assert.Equal(A(70) + "          ", BufferService.Buffer.Lines.Get(0)!.TranslateToString(false));

        // params[1] - left erase
        SetCursor(70, 1);
        InputHandler.EraseInLine(Params.FromArray([1]));
        Assert.Equal(Spaces(70) + " aaaaaaaaa", BufferService.Buffer.Lines.Get(1)!.TranslateToString(false));

        // params[2] - erase the whole line
        SetCursor(70, 2);
        InputHandler.EraseInLine(Params.FromArray([2]));
        Assert.Equal(Spaces(BufferService.Cols), BufferService.Buffer.Lines.Get(2)!.TranslateToString(false));
    }

    private void ResetToBaseState()
    {
        // reset and add a wrapped line
        SetCursor(0, 0);
        InputHandler.Parse(A(BufferService.Cols)); // line 0
        InputHandler.Parse(A(BufferService.Cols + 9)); // line 1 and 2
        for (int i = 3; i < BufferService.Rows; ++i)
        {
            InputHandler.Parse(A(BufferService.Cols));
        }

        // confirm precondition that line 2 is wrapped
        Assert.True(BufferService.Buffer.Lines.Get(2)!.IsWrapped);
    }

    // xterm.js: 'InputHandler' › 'regression tests' › 'eraseInLine reflow'
    [Fact]
    public void EraseInLine_clears_the_wrapped_flag_only_when_the_line_start_is_erased()
    {
        // params[0] - erase from the cursor through the end of the row.
        ResetToBaseState();
        SetCursor(40, 2);
        InputHandler.EraseInLine(Params.FromArray([0]));
        Assert.True(BufferService.Buffer.Lines.Get(2)!.IsWrapped);
        SetCursor(0, 2);
        InputHandler.EraseInLine(Params.FromArray([0]));
        Assert.False(BufferService.Buffer.Lines.Get(2)!.IsWrapped);

        // params[1] - erase from the beginning of the line through the cursor
        ResetToBaseState();
        SetCursor(40, 2);
        InputHandler.EraseInLine(Params.FromArray([1]));
        Assert.True(BufferService.Buffer.Lines.Get(2)!.IsWrapped);

        // params[2] - erase complete line
        ResetToBaseState();
        SetCursor(40, 2);
        InputHandler.EraseInLine(Params.FromArray([2]));
        Assert.False(BufferService.Buffer.Lines.Get(2)!.IsWrapped);
    }

    // xterm.js: 'InputHandler' › 'regression tests' › 'ED2 with scrollOnEraseInDisplay turned on'
    [Fact]
    public void ED2_with_ScrollOnEraseInDisplay_pushes_the_screen_to_the_scrollback()
    {
        OptionsService.RawOptions = OptionsService.RawOptions with { ScrollOnEraseInDisplay = true };
        string aLine = A(BufferService.Cols);

        // add 2 full lines of text.
        InputHandler.Parse(aLine);
        InputHandler.Parse(aLine);

        InputHandler.EraseInDisplay(Params.FromArray([2]));

        // those 2 lines should have been pushed to scrollback.
        Assert.Equal(BufferService.Rows + 2, BufferService.Buffer.Lines.Length);
        Assert.Equal(2, BufferService.Buffer.YBase);
        Assert.Equal(aLine, BufferService.Buffer.Lines.Get(0)?.TranslateToString());
        Assert.Equal(aLine, BufferService.Buffer.Lines.Get(1)?.TranslateToString());

        // Move to last line and add more text.
        SetCursor(0, BufferService.Rows - 1);
        InputHandler.Parse(aLine);
        InputHandler.EraseInDisplay(Params.FromArray([2]));

        // Screen should have been scrolled by a full screen size.
        Assert.Equal((BufferService.Rows * 2) + 2, BufferService.Buffer.Lines.Length);
    }

    private void FillDisplay()
    {
        for (int i = 0; i < BufferService.Rows; ++i)
        {
            InputHandler.Parse(A(BufferService.Cols));
        }
    }

    // xterm.js: 'InputHandler' › 'regression tests' › 'eraseInDisplay'
    [Fact]
    public void EraseInDisplay_erases_below_above_and_everything()
    {
        BufferService.Resize(80, 7);
        int cols = BufferService.Cols;

        // fill display with a's
        FillDisplay();

        // params [0] - right and below erase
        SetCursor(40, 5);
        InputHandler.EraseInDisplay(Params.FromArray([0]));
        Assert.Equal<string>(
            [A(cols), A(cols), A(cols), A(cols), A(cols), A(40) + Spaces(cols - 40), Spaces(cols)],
            TermContent(false));
        Assert.Equal<string>([A(cols), A(cols), A(cols), A(cols), A(cols), A(40), string.Empty], TermContent(true));

        // reset
        SetCursor(0, 0);
        FillDisplay();

        // params [1] - left and above
        SetCursor(40, 5);
        InputHandler.EraseInDisplay(Params.FromArray([1]));
        Assert.Equal<string>(
            [Spaces(cols), Spaces(cols), Spaces(cols), Spaces(cols), Spaces(cols), Spaces(41) + A(cols - 41), A(cols)],
            TermContent(false));
        Assert.Equal<string>(
            [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, Spaces(41) + A(cols - 41), A(cols)],
            TermContent(true));

        // reset
        SetCursor(0, 0);
        FillDisplay();

        // params [2] - whole screen
        SetCursor(40, 5);
        InputHandler.EraseInDisplay(Params.FromArray([2]));
        Assert.Equal<string>(
            [Spaces(cols), Spaces(cols), Spaces(cols), Spaces(cols), Spaces(cols), Spaces(cols), Spaces(cols)],
            TermContent(false));
        Assert.Equal<string>(
            [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty],
            TermContent(true));

        // reset and add a wrapped line
        ResetToBaseState();

        // params[1] left and above with wrap
        SetCursor(40, 2);
        InputHandler.EraseInDisplay(Params.FromArray([1]));
        Assert.False(BufferService.Buffer.Lines.Get(2)!.IsWrapped);

        // reset and add a wrapped line
        ResetToBaseState();

        // params[1] left and above with wrap
        SetCursor(90, 1); // Cursor is beyond last column
        InputHandler.EraseInDisplay(Params.FromArray([1]));
        Assert.False(BufferService.Buffer.Lines.Get(2)!.IsWrapped);
    }

    // xterm.js: 'InputHandler' › 'should parse big chunks in smaller subchunks'
    [Fact]
    public void Big_chunks_are_parsed_in_smaller_subchunks()
    {
        // max single chunk size is hardcoded as 131072
        using var parser = new RecordingEscapeSequenceParser();
        using var bufferService = new MockBufferService(10, 10);
        using var coreService = new MockCoreService();
        using var optionsService = new MockOptionsService();
        using var coreMouseService = new MockCoreMouseService();
        using var handler = new TestInputHandler(
            bufferService,
            new MockCharsetService(),
            coreService,
            new MockLogService(),
            optionsService,
            new MockOscLinkService(),
            coreMouseService,
            new MockUnicodeService(),
            parser);
        handler.Parse("12345");
        handler.Parse(new string('a', 10000));
        handler.Parse(new string('a', 200000));
        handler.Parse(new string('a', 300000));
        Assert.Equal<(int, int)>(
            [
                (4096, 5),
                (10000, 10000),
                (131072, 131072),
                (131072, 200000 - 131072),
                (131072, 131072),
                (131072, 131072),
                (131072, 300000 - 131072 - 131072),
            ],
            parser.Calls);
    }

    // xterm.js: 'InputHandler' › 'EL/ED cursor at buffer.cols' › 'following sequence keeps working' › 'cursor never advances beyond cols'
    [Fact]
    public void The_cursor_never_advances_beyond_cols_after_ED2_and_a_cursor_sequence()
    {
        BufferService.Resize(10, 5);
        string[] sequences =
        [
            "\x1b[10@", // ICH
            "\x1b[10 @", // SL
            "\x1b[10A", // CUU
            "\x1b[10 A", // SR
            "\x1b[10B", // CUD
            "\x1b[10C", // CUF
            "\x1b[10D", // CUB
            "\x1b[10E", // CNL
            "\x1b[10F", // CPL
            "\x1b[10G", // CHA
            "\x1b[10;10H", // CUP
            "\x1b[10I", // CHT
            "\x1b[10L", // IL
            "\x1b[10M", // DL
            "\x1b[10P", // DCH
            "\x1b[10S", // SU
            "\x1b[10T", // SD
            "\x1b[10X", // ECH
            "\x1b[10Z", // CBT
            "\x1b[10`", // HPA
            "\x1b[10a", // HPR
            "\x1b[10b", // REP
            "\x1b[10d", // VPA
            "\x1b[10e", // VPR
            "\x1b[10;10f", // HVP
            "\x1b[0g", // TBC
            "\x1b[s", // SCOSC
            "\x1b[10'}", // DECIC
            "\x1b[10'~", // DECDC
        ];
        foreach (string seq in sequences)
        {
            InputHandler.Parse("##########\x1b[2J" + seq);
            Assert.True(BufferService.Buffer.X <= BufferService.Cols, seq);
            InputHandler.Reset();
            BufferService.Reset();
        }
    }
}

