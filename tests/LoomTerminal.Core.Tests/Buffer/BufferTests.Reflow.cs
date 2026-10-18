// Ported from xtermjs/xterm.js: src/common/buffer/Buffer.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.Buffer;

public sealed partial class BufferTests
{
    private static readonly CharData EmptyCell = new(0, string.Empty, 0, 0);

    private static CharData CharAt(char c, int width = 1) => new(0, c.ToString(), width, c);

    private static void FillRun(IBufferLine line, char first, int count)
    {
        for (int i = 0; i < count; i++)
        {
            line.Set(i, CharAt((char)(first + i)));
        }
    }

    private void AssertLines(int firstIndex, params string[] expected)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], _buffer.Lines.Get(firstIndex + i)!.TranslateToString());
        }
    }

    private void FillWideLines()
    {
        // Buffer:
        // 汉语汉语汉语 (wrapped)
        // 汉语汉语汉语
        _buffer.FillViewportRows();
        _buffer.Resize(12, 10);
        _buffer.Y = 2;
        for (int line = 0; line < 2; line++)
        {
            for (int i = 0; i < 12; i += 4)
            {
                _buffer.Lines.Get(line)!.Set(i, CharAt('汉', 2));
            }

            for (int i = 2; i < 12; i += 4)
            {
                _buffer.Lines.Get(line)!.Set(i, CharAt('语', 2));
            }

            for (int i = 1; i < 12; i += 2)
            {
                _buffer.Lines.Get(line)!.Set(i, EmptyCell);
            }
        }

        _buffer.Lines.Get(1)!.IsWrapped = true;
    }

    private string Trimmed(int line) => _buffer.Lines.Get(line)!.TranslateToString(true);

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should not wrap empty lines'
    [Fact]
    public void Reflow_does_not_wrap_empty_lines_when_the_columns_shrink()
    {
        _buffer.FillViewportRows();
        Assert.Equal(InitRows, _buffer.Lines.Length);
        _buffer.Resize(InitCols - 5, InitRows);
        Assert.Equal(InitRows, _buffer.Lines.Length);
    }

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should shrink row length'
    [Fact]
    public void Reflow_shrinks_the_row_length()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(5, 10);
        Assert.Equal(10, _buffer.Lines.Length);
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(5, _buffer.Lines.Get(i)!.Length);
        }
    }

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should wrap and unwrap lines'
    [Fact]
    public void Reflow_wraps_and_unwraps_lines()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(5, 10);
        IBufferLine firstLine = _buffer.Lines.Get(0)!;
        FillRun(firstLine, 'a', 5);
        _buffer.Y = 1;
        Assert.Equal(5, _buffer.Lines.Get(0)!.Length);
        Assert.Equal("abcde", _buffer.Lines.Get(0)!.TranslateToString());
        _buffer.Resize(1, 10);
        Assert.Equal(10, _buffer.Lines.Length);
        AssertLines(0, "a", "b", "c", "d", "e", " ", " ", " ", " ", " ");
        _buffer.Resize(5, 10);
        Assert.Equal(10, _buffer.Lines.Length);
        AssertLines(0, "abcde", "     ", "     ", "     ", "     ", "     ", "     ", "     ", "     ", "     ");
    }

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should discard parts of wrapped lines that go out of the scrollback'
    [Fact]
    public void Reflow_discards_parts_of_wrapped_lines_that_go_out_of_the_scrollback()
    {
        _buffer.FillViewportRows();
        _optionsService.RawOptions = _optionsService.RawOptions with { Scrollback = 1 };
        _buffer.Resize(10, 5);
        IBufferLine lastLine = _buffer.Lines.Get(3)!;
        FillRun(lastLine, 'a', 10);
        Assert.Equal(5, _buffer.Lines.Length);
        _buffer.Y = 4;
        _buffer.Resize(2, 5);
        Assert.Equal(4, _buffer.Y);
        Assert.Equal(1, _buffer.YBase);
        Assert.Equal(6, _buffer.Lines.Length);
        AssertLines(0, "ab", "cd", "ef", "gh", "ij", "  ");
        _buffer.Resize(1, 5);
        Assert.Equal(4, _buffer.Y);
        Assert.Equal(1, _buffer.YBase);
        Assert.Equal(6, _buffer.Lines.Length);
        AssertLines(0, "f", "g", "h", "i", "j", " ");
        _buffer.Resize(10, 5);
        Assert.Equal(1, _buffer.Y);
        Assert.Equal(0, _buffer.YBase);
        Assert.Equal(5, _buffer.Lines.Length);
        AssertLines(0, "fghij     ", "          ", "          ", "          ", "          ");
    }

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should remove the correct amount of rows when reflowing larger'
    [Fact]
    public void Reflow_removes_the_correct_amount_of_rows_when_reflowing_larger()
    {
        // This is a regression test to ensure that successive wrapped lines that are getting 3+ lines removed on a
        // reflow actually remove the right lines
        _buffer.FillViewportRows();
        _buffer.Resize(10, 10);
        _buffer.Y = 2;
        FillRun(_buffer.Lines.Get(0)!, 'a', 10);
        FillRun(_buffer.Lines.Get(1)!, '0', 10);
        Assert.Equal(10, _buffer.Lines.Length);
        Assert.Equal("abcdefghij", _buffer.Lines.Get(0)!.TranslateToString());
        Assert.Equal("0123456789", _buffer.Lines.Get(1)!.TranslateToString());
        for (int i = 2; i < 10; i++)
        {
            Assert.Equal("          ", _buffer.Lines.Get(i)!.TranslateToString());
        }

        _buffer.Resize(2, 10);
        Assert.Equal(1, _buffer.YBase);
        Assert.Equal(11, _buffer.Lines.Length);
        AssertLines(0, "ab", "cd", "ef", "gh", "ij", "01", "23", "45", "67", "89", "  ");
        _buffer.Resize(10, 10);
        Assert.Equal(0, _buffer.YBase);
        Assert.Equal(10, _buffer.Lines.Length);
        Assert.Equal("abcdefghij", _buffer.Lines.Get(0)!.TranslateToString());
        Assert.Equal("0123456789", _buffer.Lines.Get(1)!.TranslateToString());
        for (int i = 2; i < 10; i++)
        {
            Assert.Equal("          ", _buffer.Lines.Get(i)!.TranslateToString());
        }
    }

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should transfer combined char data over to reflowed lines'
    [Fact]
    public void Reflow_transfers_combined_char_data_over_to_reflowed_lines()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(4, 3);
        _buffer.Y = 2;
        IBufferLine firstLine = _buffer.Lines.Get(0)!;
        firstLine.Set(0, CharAt('a'));
        firstLine.Set(1, CharAt('b'));
        firstLine.Set(2, CharAt('c'));
        firstLine.Set(3, new CharData(0, "😁", 1, '\uD83D'));
        Assert.Equal(3, _buffer.Lines.Length);
        Assert.Equal("abc😁", _buffer.Lines.Get(0)!.TranslateToString());
        Assert.Equal("    ", _buffer.Lines.Get(1)!.TranslateToString());
        _buffer.Resize(2, 3);
        Assert.Equal("ab", _buffer.Lines.Get(0)!.TranslateToString());
        Assert.Equal("c😁", _buffer.Lines.Get(1)!.TranslateToString());
    }

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should adjust markers when reflowing'
    [Fact]
    public void Reflow_adjusts_markers()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(10, 16);
        FillRun(_buffer.Lines.Get(0)!, 'a', 10);
        FillRun(_buffer.Lines.Get(1)!, '0', 10);
        FillRun(_buffer.Lines.Get(2)!, 'k', 10);
        _buffer.Y = 3;

        // Buffer:
        // abcdefghij
        // 0123456789
        // abcdefghij
        Marker firstMarker = _buffer.AddMarker(0);
        Marker secondMarker = _buffer.AddMarker(1);
        Marker thirdMarker = _buffer.AddMarker(2);
        AssertLines(0, "abcdefghij", "0123456789", "klmnopqrst");
        Assert.Equal(0, firstMarker.Line);
        Assert.Equal(1, secondMarker.Line);
        Assert.Equal(2, thirdMarker.Line);
        _buffer.Resize(2, 16);
        AssertLines(0, "ab", "cd", "ef", "gh", "ij", "01", "23", "45", "67", "89", "kl", "mn", "op", "qr", "st");
        Assert.Equal(0, firstMarker.Line); // first marker should remain unchanged
        Assert.Equal(5, secondMarker.Line); // second marker should be shifted since the first line wrapped
        Assert.Equal(10, thirdMarker.Line); // third marker should be shifted since the first and second lines wrapped
        _buffer.Resize(10, 16);
        AssertLines(0, "abcdefghij", "0123456789", "klmnopqrst");
        Assert.Equal(0, firstMarker.Line); // first marker should remain unchanged
        Assert.Equal(1, secondMarker.Line); // second marker should be restored to it's original line
        Assert.Equal(2, thirdMarker.Line); // third marker should be restored to it's original line
        Assert.False(firstMarker.IsDisposed);
        Assert.False(secondMarker.IsDisposed);
        Assert.False(thirdMarker.IsDisposed);
    }

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should dispose markers whose rows are trimmed during a reflow'
    [Fact]
    public void Reflow_disposes_markers_whose_rows_are_trimmed()
    {
        _buffer.FillViewportRows();
        _optionsService.RawOptions = _optionsService.RawOptions with { Scrollback = 1 };
        _buffer.Resize(10, 11);
        FillRun(_buffer.Lines.Get(0)!, 'a', 10);
        FillRun(_buffer.Lines.Get(1)!, '0', 10);
        FillRun(_buffer.Lines.Get(2)!, 'k', 10);
        _buffer.Y = 10;

        // Buffer:
        // abcdefghij
        // 0123456789
        // abcdefghij
        Marker firstMarker = _buffer.AddMarker(0);
        Marker secondMarker = _buffer.AddMarker(1);
        Marker thirdMarker = _buffer.AddMarker(2);
        _buffer.Y = 3;
        AssertLines(0, "abcdefghij", "0123456789", "klmnopqrst");
        Assert.Equal(0, firstMarker.Line);
        Assert.Equal(1, secondMarker.Line);
        Assert.Equal(2, thirdMarker.Line);
        _buffer.Resize(2, 11);
        AssertLines(0, "ij", "01", "23", "45", "67", "89", "kl", "mn", "op", "qr", "st");
        Assert.Equal(1, secondMarker.Line); // second marker should remain the same as it was shifted 4 and trimmed 4
        Assert.Equal(6, thirdMarker.Line); // third marker should be shifted since the first and second lines wrapped
        Assert.True(firstMarker.IsDisposed); // first marker was trimmed
        Assert.False(secondMarker.IsDisposed);
        Assert.False(thirdMarker.IsDisposed);
        _buffer.Resize(10, 11);
        AssertLines(0, "ij        ", "0123456789", "klmnopqrst");
        Assert.Equal(1, secondMarker.Line); // second marker should be restored
        Assert.Equal(2, thirdMarker.Line); // third marker should be restored
    }

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should correctly reflow wrapped lines that end in 0 space (via tab char)' (reflowing larger)
    [Fact]
    public void Reflow_larger_handles_wrapped_lines_that_end_in_0_space()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(4, 10);
        _buffer.Y = 2;
        _buffer.Lines.Get(0)!.Set(0, CharAt('a'));
        _buffer.Lines.Get(0)!.Set(1, CharAt('b'));
        _buffer.Lines.Get(1)!.Set(0, CharAt('c'));
        _buffer.Lines.Get(1)!.Set(1, CharAt('d'));
        _buffer.Lines.Get(1)!.IsWrapped = true;

        // Buffer:
        // "ab  " (wrapped)
        // "cd"
        _buffer.Resize(5, 10);
        Assert.Equal(0, _buffer.YBase);
        Assert.Equal(10, _buffer.Lines.Length);
        Assert.Equal("ab  c", _buffer.Lines.Get(0)!.TranslateToString(true));
        Assert.Equal("d    ", _buffer.Lines.Get(1)!.TranslateToString(false));
        _buffer.Resize(6, 10);
        Assert.Equal(0, _buffer.YBase);
        Assert.Equal(10, _buffer.Lines.Length);
        Assert.Equal("ab  cd", _buffer.Lines.Get(0)!.TranslateToString(true));
        Assert.Equal("      ", _buffer.Lines.Get(1)!.TranslateToString(false));
    }

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should wrap wide characters correctly when reflowing larger'
    [Fact]
    public void Reflow_wraps_wide_characters_correctly_when_reflowing_larger()
    {
        FillWideLines();
        Assert.Equal("汉语汉语汉语", Trimmed(0));
        Assert.Equal("汉语汉语汉语", Trimmed(1));
        _buffer.Resize(13, 10);
        Assert.Equal(0, _buffer.YBase);
        Assert.Equal(10, _buffer.Lines.Length);
        Assert.Equal("汉语汉语汉语", Trimmed(0));
        Assert.Equal("汉语汉语汉语 ", _buffer.Lines.Get(0)!.TranslateToString(false));
        Assert.Equal("汉语汉语汉语", Trimmed(1));
        Assert.Equal("汉语汉语汉语 ", _buffer.Lines.Get(1)!.TranslateToString(false));
        _buffer.Resize(14, 10);
        Assert.Equal("汉语汉语汉语汉", Trimmed(0));
        Assert.Equal("汉语汉语汉语汉", _buffer.Lines.Get(0)!.TranslateToString(false));
        Assert.Equal("语汉语汉语", Trimmed(1));
        Assert.Equal("语汉语汉语    ", _buffer.Lines.Get(1)!.TranslateToString(false));
    }

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should correctly reflow wrapped lines that end in 0 space (via tab char)' (reflowing smaller)
    [Fact]
    public void Reflow_smaller_handles_wrapped_lines_that_end_in_0_space()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(4, 10);
        _buffer.Y = 2;
        _buffer.Lines.Get(0)!.Set(0, CharAt('a'));
        _buffer.Lines.Get(0)!.Set(1, CharAt('b'));
        _buffer.Lines.Get(1)!.Set(0, CharAt('c'));
        _buffer.Lines.Get(1)!.Set(1, CharAt('d'));
        _buffer.Lines.Get(1)!.IsWrapped = true;

        // Buffer:
        // "ab  " (wrapped)
        // "cd"
        _buffer.Resize(3, 10);
        Assert.Equal(2, _buffer.Y);
        Assert.Equal(0, _buffer.YBase);
        Assert.Equal(10, _buffer.Lines.Length);
        Assert.Equal("ab ", _buffer.Lines.Get(0)!.TranslateToString(false));
        Assert.Equal(" cd", _buffer.Lines.Get(1)!.TranslateToString(false));
        _buffer.Resize(2, 10);
        Assert.Equal(3, _buffer.Y);
        Assert.Equal(0, _buffer.YBase);
        Assert.Equal(10, _buffer.Lines.Length);
        Assert.Equal("ab", _buffer.Lines.Get(0)!.TranslateToString(false));
        Assert.Equal("  ", _buffer.Lines.Get(1)!.TranslateToString(false));
        Assert.Equal("cd", _buffer.Lines.Get(2)!.TranslateToString(false));
    }

    // xterm.js: 'Buffer' › 'resize' › 'reflow' › 'should wrap wide characters correctly when reflowing smaller'
    [Fact]
    public void Reflow_wraps_wide_characters_correctly_when_reflowing_smaller()
    {
        FillWideLines();
        Assert.Equal("汉语汉语汉语", Trimmed(0));
        Assert.Equal("汉语汉语汉语", Trimmed(1));
        _buffer.Resize(11, 10);
        Assert.Equal(0, _buffer.YBase);
        Assert.Equal(10, _buffer.Lines.Length);
        Assert.Equal("汉语汉语汉", Trimmed(0));
        Assert.Equal("语汉语汉语", Trimmed(1));
        Assert.Equal("汉语", Trimmed(2));
        _buffer.Resize(10, 10);
        Assert.Equal("汉语汉语汉", Trimmed(0));
        Assert.Equal("语汉语汉语", Trimmed(1));
        Assert.Equal("汉语", Trimmed(2));
        _buffer.Resize(9, 10);
        Assert.Equal("汉语汉语", Trimmed(0));
        Assert.Equal("汉语汉语", Trimmed(1));
        Assert.Equal("汉语汉语", Trimmed(2));
        _buffer.Resize(8, 10);
        Assert.Equal("汉语汉语", Trimmed(0));
        Assert.Equal("汉语汉语", Trimmed(1));
        Assert.Equal("汉语汉语", Trimmed(2));
        _buffer.Resize(7, 10);
        Assert.Equal("汉语汉", Trimmed(0));
        Assert.Equal("语汉语", Trimmed(1));
        Assert.Equal("汉语汉", Trimmed(2));
        Assert.Equal("语汉语", Trimmed(3));
        _buffer.Resize(6, 10);
        Assert.Equal("汉语汉", Trimmed(0));
        Assert.Equal("语汉语", Trimmed(1));
        Assert.Equal("汉语汉", Trimmed(2));
        Assert.Equal("语汉语", Trimmed(3));
    }
}
