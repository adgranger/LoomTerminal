// Ported from xtermjs/xterm.js: src/common/buffer/Buffer.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.Buffer;

// The nested describes become name prefixes; their beforeEach blocks are the Setup* and Add* methods below.
public sealed partial class BufferTests
{
    // Setup buffer state:
    // 'ab'
    // 'cd' (wrapped)
    // 'ef'
    // 'gh' (wrapped)
    // 'ij'
    // 'kl' (wrapped)
    // '  ' x 4
    private void SetupReflowLargerCases()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(2, 10);
        string letters = "abcdefghijkl";
        for (int line = 0; line < 6; line++)
        {
            _buffer.Lines.Get(line)!.Set(0, CharAt(letters[line * 2]));
            _buffer.Lines.Get(line)!.Set(1, CharAt(letters[(line * 2) + 1]));
        }

        _buffer.Lines.Get(1)!.IsWrapped = true;
        _buffer.Lines.Get(3)!.IsWrapped = true;
        _buffer.Lines.Get(5)!.IsWrapped = true;
    }

    // Setup buffer state:
    // 'abcd'
    // 'efgh'
    // 'ijkl'
    // '    ' x 7
    private void SetupReflowSmallerCases()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(4, 10);
        string letters = "abcdefghijkl";
        for (int line = 0; line < 3; line++)
        {
            for (int col = 0; col < 4; col++)
            {
                _buffer.Lines.Get(line)!.Set(col, CharAt(letters[(line * 4) + col]));
            }
        }
    }

    private void AddTenEmptyRowsToStart()
    {
        for (int i = 0; i < 10; i++)
        {
            _buffer.Lines.Splice(0, 0, _buffer.GetBlankLine(AttributeData.DefaultAttrData));
        }
    }

    private void AssertSameText(int from, int toExclusive, string text)
    {
        for (int i = from; i < toExclusive; i++)
        {
            Assert.Equal(text, _buffer.Lines.Get(i)!.TranslateToString());
        }
    }

    private void AssertWrappedLines(params int[] wrappedLines)
    {
        for (int i = 0; i < _buffer.Lines.Length; i++)
        {
            Assert.True(
                _buffer.Lines.Get(i)!.IsWrapped == wrappedLines.Contains(i),
                $"line {i} isWrapped must equal {wrappedLines.Contains(i)}");
        }
    }

    private void AssertCursor(int y, int ydisp, int ybase, int length)
    {
        Assert.Equal(y, _buffer.Y);
        Assert.Equal(ydisp, _buffer.YDisp);
        Assert.Equal(ybase, _buffer.YBase);
        Assert.Equal(length, _buffer.Lines.Length);
    }

    private void AssertLargerResultAfterResize()
    {
        AssertLines(0, "abcd", "efgh", "ijkl");
    }

    // xterm.js: 'reflowLarger cases' › 'viewport not yet filled' › 'should move the cursor up and add empty lines'
    [Fact]
    public void ReflowLarger_viewport_not_yet_filled_moves_the_cursor_up_and_adds_empty_lines()
    {
        SetupReflowLargerCases();
        _buffer.Y = 6;
        _buffer.Resize(4, 10);
        AssertCursor(3, 0, 0, 10);
        AssertLargerResultAfterResize();
        AssertSameText(3, 10, "    ");
        AssertWrappedLines();
    }

    // xterm.js: 'reflowLarger cases' › 'viewport filled, scrollback remaining' › 'ybase === 0' › 'should move the cursor up and add empty lines'
    [Fact]
    public void ReflowLarger_viewport_filled_scrollback_remaining_ybase_0_moves_the_cursor_up_and_adds_empty_lines()
    {
        SetupReflowLargerCases();
        _buffer.Y = 9;
        _buffer.Resize(4, 10);
        AssertCursor(6, 0, 0, 10);
        AssertLargerResultAfterResize();
        AssertSameText(3, 10, "    ");
        AssertWrappedLines();
    }

    // xterm.js: 'reflowLarger cases' › 'viewport filled, scrollback remaining' › 'ybase !== 0' › '&& ydisp === ybase' › 'should adjust the viewport and keep ydisp = ybase'
    [Fact]
    public void ReflowLarger_viewport_filled_scrollback_remaining_ybase_not_0_ydisp_equals_ybase_adjusts_the_viewport()
    {
        SetupReflowLargerCases();
        _buffer.Y = 9;
        AddTenEmptyRowsToStart();
        _buffer.YBase = 10;
        _buffer.YDisp = 10;
        _buffer.Resize(4, 10);
        AssertCursor(9, 7, 7, 17);
        AssertSameText(0, 10, "    ");
        AssertLines(10, "abcd", "efgh", "ijkl");
        AssertSameText(13, 17, "    ");
        AssertWrappedLines();
    }

    // xterm.js: 'reflowLarger cases' › 'viewport filled, scrollback remaining' › 'ybase !== 0' › '&& ydisp !== ybase' › 'should keep ydisp at the same value'
    [Fact]
    public void ReflowLarger_viewport_filled_scrollback_remaining_ybase_not_0_ydisp_not_ybase_keeps_ydisp()
    {
        SetupReflowLargerCases();
        _buffer.Y = 9;
        AddTenEmptyRowsToStart();
        _buffer.YBase = 10;
        _buffer.YDisp = 5;
        _buffer.Resize(4, 10);
        AssertCursor(9, 5, 7, 17);
        AssertSameText(0, 10, "    ");
        AssertLines(10, "abcd", "efgh", "ijkl");
        AssertSameText(13, 17, "    ");
        AssertWrappedLines();
    }

    private void SetupNoScrollbackRemaining()
    {
        _optionsService.RawOptions = _optionsService.RawOptions with { Scrollback = 10 };
        AddTenEmptyRowsToStart();
        _buffer.Y = 9;
        _buffer.YBase = 10;
    }

    // xterm.js: 'reflowLarger cases' › 'viewport filled, no scrollback remaining' › 'ybase !== 0' › '&& ydisp === ybase' › 'should trim lines and keep ydisp = ybase'
    [Fact]
    public void ReflowLarger_viewport_filled_no_scrollback_remaining_ydisp_equals_ybase_trims_lines()
    {
        SetupReflowLargerCases();
        SetupNoScrollbackRemaining();
        _buffer.YDisp = 10;
        _buffer.Resize(4, 10);
        AssertCursor(9, 7, 7, 17);
        AssertSameText(0, 10, "    ");
        AssertLines(10, "abcd", "efgh", "ijkl");
        AssertSameText(13, 17, "    ");
        AssertWrappedLines();
    }

    // xterm.js: 'reflowLarger cases' › 'viewport filled, no scrollback remaining' › 'ybase !== 0' › '&& ydisp !== ybase' › 'should trim lines and not change ydisp'
    [Fact]
    public void ReflowLarger_viewport_filled_no_scrollback_remaining_ydisp_not_ybase_does_not_change_ydisp()
    {
        SetupReflowLargerCases();
        SetupNoScrollbackRemaining();
        _buffer.YDisp = 5;
        _buffer.Resize(4, 10);
        AssertCursor(9, 5, 7, 17);
        AssertSameText(0, 10, "    ");
        AssertLines(10, "abcd", "efgh", "ijkl");
        AssertSameText(13, 17, "    ");
        AssertWrappedLines();
    }

    // xterm.js: 'reflowSmaller cases' › 'viewport not yet filled' › 'should move the cursor down'
    [Fact]
    public void ReflowSmaller_viewport_not_yet_filled_moves_the_cursor_down()
    {
        SetupReflowSmallerCases();
        _buffer.Y = 3;
        _buffer.Resize(2, 10);
        AssertCursor(6, 0, 0, 10);
        AssertLines(0, "ab", "cd", "ef", "gh", "ij", "kl");
        AssertSameText(6, 10, "  ");
        AssertWrappedLines(1, 3, 5);
    }

    // xterm.js: 'reflowSmaller cases' › 'viewport filled, scrollback remaining' › 'ybase === 0' › 'should trim the top'
    [Fact]
    public void ReflowSmaller_viewport_filled_scrollback_remaining_ybase_0_trims_the_top()
    {
        SetupReflowSmallerCases();
        _buffer.Y = 9;
        _buffer.Resize(2, 10);
        Assert.Equal(9, _buffer.Y);
        Assert.Equal(3, _buffer.YDisp);
        Assert.Equal(3, _buffer.YBase);
        Assert.Equal(13, _buffer.Lines.Length);
        AssertLines(0, "ab", "cd", "ef", "gh", "ij", "kl");
        AssertSameText(6, 13, "  ");
        AssertWrappedLines(1, 3, 5);
    }

    // xterm.js: 'reflowSmaller cases' › 'viewport filled, scrollback remaining' › 'ybase !== 0' › '&& ydisp === ybase' › 'should adjust the viewport and keep ydisp = ybase'
    [Fact]
    public void ReflowSmaller_viewport_filled_scrollback_remaining_ybase_not_0_ydisp_equals_ybase_adjusts_the_viewport()
    {
        SetupReflowSmallerCases();
        _buffer.Y = 9;
        AddTenEmptyRowsToStart();
        _buffer.YBase = 10;
        _buffer.YDisp = 10;
        _buffer.Resize(2, 10);
        Assert.Equal(13, _buffer.YDisp);
        Assert.Equal(13, _buffer.YBase);
        Assert.Equal(23, _buffer.Lines.Length);
        AssertSameText(0, 10, "  ");
        AssertLines(10, "ab", "cd", "ef", "gh", "ij", "kl");
        AssertSameText(16, 23, "  ");
        AssertWrappedLines(11, 13, 15);
    }

    // xterm.js: 'reflowSmaller cases' › 'viewport filled, scrollback remaining' › 'ybase !== 0' › '&& ydisp !== ybase' › 'should keep ydisp at the same value'
    [Fact]
    public void ReflowSmaller_viewport_filled_scrollback_remaining_ybase_not_0_ydisp_not_ybase_keeps_ydisp()
    {
        SetupReflowSmallerCases();
        _buffer.Y = 9;
        AddTenEmptyRowsToStart();
        _buffer.YBase = 10;
        _buffer.YDisp = 5;
        _buffer.Resize(2, 10);
        Assert.Equal(5, _buffer.YDisp);
        Assert.Equal(13, _buffer.YBase);
        Assert.Equal(23, _buffer.Lines.Length);
        AssertSameText(0, 10, "  ");
        AssertLines(10, "ab", "cd", "ef", "gh", "ij", "kl");
        AssertSameText(16, 23, "  ");
        AssertWrappedLines(11, 13, 15);
    }

    // xterm.js: 'reflowSmaller cases' › 'viewport filled, no scrollback remaining' › 'ybase !== 0' › '&& ydisp === ybase' › 'should trim lines and keep ydisp = ybase'
    [Fact]
    public void ReflowSmaller_viewport_filled_no_scrollback_remaining_ydisp_equals_ybase_trims_lines()
    {
        SetupReflowSmallerCases();
        _optionsService.RawOptions = _optionsService.RawOptions with { Scrollback = 10 };
        AddTenEmptyRowsToStart();
        _buffer.YBase = 10;
        _buffer.YDisp = 10;
        _buffer.Y = 13;
        _buffer.Resize(2, 10);
        Assert.Equal(10, _buffer.YDisp);
        Assert.Equal(10, _buffer.YBase);
        Assert.Equal(20, _buffer.Lines.Length);
        AssertSameText(0, 7, "  ");
        AssertLines(7, "ab", "cd", "ef", "gh", "ij", "kl");
        AssertSameText(13, 20, "  ");
        AssertWrappedLines(8, 10, 12);
    }

    // xterm.js: 'reflowSmaller cases' › 'viewport filled, no scrollback remaining' › 'ybase !== 0' › '&& ydisp !== ybase' › 'should trim lines and not change ydisp'
    [Fact]
    public void ReflowSmaller_viewport_filled_no_scrollback_remaining_ydisp_not_ybase_does_not_change_ydisp()
    {
        SetupReflowSmallerCases();
        _optionsService.RawOptions = _optionsService.RawOptions with { Scrollback = 10 };
        AddTenEmptyRowsToStart();
        _buffer.YBase = 10;
        _buffer.YDisp = 5;
        _buffer.Y = 13;
        _buffer.Resize(2, 10);
        Assert.Equal(5, _buffer.YDisp);
        Assert.Equal(10, _buffer.YBase);
        Assert.Equal(20, _buffer.Lines.Length);
        AssertSameText(0, 7, "  ");
        AssertLines(7, "ab", "cd", "ef", "gh", "ij", "kl");
        AssertSameText(13, 20, "  ");
        AssertWrappedLines(8, 10, 12);
    }
}
