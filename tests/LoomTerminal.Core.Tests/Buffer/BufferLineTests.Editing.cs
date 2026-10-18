// Ported from xtermjs/xterm.js: src/common/buffer/BufferLine.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.Buffer;

public partial class BufferLineTests
{
    private static CharData Cd(uint attr, string chars, int width) =>
        new(attr, chars, width, chars.Length == 0 ? 0u : chars[^1]);

    private static CellData Cell(uint attr, string chars, int width) => CellData.FromCharData(Cd(attr, chars, width));

    private static CellData NullFill() =>
        CellData.FromCharData(new CharData(BufferConstants.DefaultAttr, BufferConstants.NullCellChar, BufferConstants.NullCellWidth, BufferConstants.NullCellCode));

    private static TestBufferLine Abcde()
    {
        string letters = "abcde";
        TestBufferLine line = new(5);
        for (int i = 0; i < 5; i++)
        {
            line.SetCell(i, Cell((uint)(i + 1), letters[i].ToString(), 0));
        }

        return line;
    }

    // xterm.js: 'BufferLine' › 'insertCells'
    [Fact]
    public void InsertCells_shifts_cells_to_the_right_and_drops_the_overflow()
    {
        TestBufferLine line = new(3);
        line.SetCell(0, Cell(1, "a", 0));
        line.SetCell(1, Cell(2, "b", 0));
        line.SetCell(2, Cell(3, "c", 0));
        line.InsertCells(1, 3, Cell(4, "d", 0));
        Assert.Equal([Cd(1, "a", 0), Cd(4, "d", 0), Cd(4, "d", 0)], line.ToArray());
    }

    // xterm.js: 'BufferLine' › 'deleteCells'
    [Fact]
    public void DeleteCells_shifts_cells_to_the_left_and_fills_the_end()
    {
        TestBufferLine line = Abcde();
        line.DeleteCells(1, 2, Cell(6, "f", 0));
        Assert.Equal([Cd(1, "a", 0), Cd(4, "d", 0), Cd(5, "e", 0), Cd(6, "f", 0), Cd(6, "f", 0)], line.ToArray());
    }

    // xterm.js: 'BufferLine' › 'replaceCells'
    [Fact]
    public void ReplaceCells_replaces_the_given_range()
    {
        TestBufferLine line = Abcde();
        line.ReplaceCells(2, 4, Cell(6, "f", 0));
        Assert.Equal([Cd(1, "a", 0), Cd(2, "b", 0), Cd(6, "f", 0), Cd(6, "f", 0), Cd(5, "e", 0)], line.ToArray());
    }

    // xterm.js: 'BufferLine' › 'fill'
    [Fact]
    public void Fill_sets_every_cell()
    {
        TestBufferLine line = Abcde();
        line.Fill(Cell(123, "z", 0));
        Assert.Equal(Enumerable.Repeat(Cd(123, "z", 0), 5).ToList(), line.ToArray());
    }

    // xterm.js: 'BufferLine' › 'clone'
    [Fact]
    public void Clone_copies_cells_length_and_wrapped_flag()
    {
        TestBufferLine line = Abcde();
        line.IsWrapped = true;
        IBufferLine line2 = line.Clone();
        Assert.Equal(line.ToArray(), CopyToArray(line2));
        Assert.Equal(line.Length, line2.Length);
        Assert.Equal(line.IsWrapped, line2.IsWrapped);
    }

    // xterm.js: 'BufferLine' › 'copyFrom'
    [Fact]
    public void CopyFrom_copies_cells_length_and_wrapped_flag()
    {
        TestBufferLine line = Abcde();
        TestBufferLine line2 = new(5, Cell(1, "a", 0), true);
        line2.CopyFrom(line);
        Assert.Equal(line.ToArray(), line2.ToArray());
        Assert.Equal(line.Length, line2.Length);
        Assert.Equal(line.IsWrapped, line2.IsWrapped);
    }

    // xterm.js: 'BufferLine' › 'should support combining chars'
    [Fact]
    public void Combining_chars_survive_copy_and_clone()
    {
        TestBufferLine line = new(2, Cell(1, "é", 0));
        Assert.Equal([Cd(1, "é", 0), Cd(1, "é", 0)], line.ToArray());
        TestBufferLine line2 = new(5, Cell(1, "a", 0), true);
        line2.CopyFrom(line);
        Assert.Equal(line.ToArray(), line2.ToArray());
        IBufferLine line3 = line.Clone();
        Assert.Equal(line.ToArray(), CopyToArray(line3));
    }

    private static List<CharData> CopyToArray(IBufferLine line)
    {
        List<CharData> result = [];
        for (int i = 0; i < line.Length; ++i)
        {
            result.Add(line.LoadCell(i, new CellData()).GetAsCharData());
        }

        return result;
    }

    // xterm.js: 'BufferLine' › 'resize' › 'enlarge(false)'
    [Fact]
    public void Resize_enlarge_fills_new_cells()
    {
        TestBufferLine line = new(5, Cell(1, "a", 0));
        line.Resize(10, Cell(1, "a", 0));
        Assert.Equal(Enumerable.Repeat(Cd(1, "a", 0), 10).ToList(), line.ToArray());
    }

    // xterm.js: 'BufferLine' › 'resize' › 'enlarge(true)'
    [Fact]
    public void Resize_enlarge_fills_new_cells_when_wrapped_is_irrelevant()
    {
        TestBufferLine line = new(5, Cell(1, "a", 0));
        line.Resize(10, Cell(1, "a", 0));
        Assert.Equal(Enumerable.Repeat(Cd(1, "a", 0), 10).ToList(), line.ToArray());
    }

    // xterm.js: 'BufferLine' › 'resize' › 'shrink(true) - should apply new size'
    [Fact]
    public void Resize_shrink_applies_the_new_size()
    {
        TestBufferLine line = new(10, Cell(1, "a", 0));
        line.Resize(5, Cell(1, "a", 0));
        Assert.Equal(Enumerable.Repeat(Cd(1, "a", 0), 5).ToList(), line.ToArray());
    }

    // xterm.js: 'BufferLine' › 'resize' › 'shrink to 0 length'
    [Fact]
    public void Resize_shrink_to_zero_length_empties_the_line()
    {
        TestBufferLine line = new(10, Cell(1, "a", 0));
        line.Resize(0, Cell(1, "a", 0));
        Assert.Empty(line.ToArray());
    }

    // xterm.js: 'BufferLine' › 'resize' › 'should remove combining data on replaced cells after shrinking then enlarging'
    [Fact]
    public void Resize_removes_combining_data_of_cut_off_cells()
    {
        TestBufferLine line = new(10, Cell(1, "a", 0));
        line.Set(2, new CharData(0, "\U0001F601", 1, '\uD83D'));
        line.Set(9, new CharData(0, "\U0001F601", 1, '\uD83D'));
        Assert.Equal("aa\U0001F601aaaaaa\U0001F601", line.TranslateToString());
        Assert.Equal(2, line.Combined.Count);
        line.Resize(5, Cell(1, "a", 0));
        Assert.Equal("aa\U0001F601aa", line.TranslateToString());
        line.Resize(10, Cell(1, "a", 0));
        Assert.Equal("aa\U0001F601aaaaaaa", line.TranslateToString());
        Assert.Single(line.Combined);
    }

    [Fact]
    public void Resize_reports_when_a_cleanup_would_free_memory_and_CleanupMemory_frees_it()
    {
        TestBufferLine line = new(100, Cell(1, "a", 0));
        Assert.False(line.Resize(100, Cell(1, "a", 0)));
        Assert.True(line.Resize(10, Cell(1, "a", 0)));
        Assert.Equal(1, line.CleanupMemory());
        Assert.Equal(0, line.CleanupMemory());
        Assert.Equal(Enumerable.Repeat(Cd(1, "a", 0), 10).ToList(), line.ToArray());
    }

    private static TestBufferLine Empty10() => new(10, NullFill());

    // xterm.js: 'BufferLine' › 'getTrimLength' › 'empty line'
    [Fact]
    public void GetTrimmedLength_of_an_empty_line_is_zero() => Assert.Equal(0, Empty10().GetTrimmedLength());

    // xterm.js: 'BufferLine' › 'getTrimLength' › 'ASCII'
    [Fact]
    public void GetTrimmedLength_counts_up_to_the_last_ascii_char()
    {
        TestBufferLine line = Empty10();
        line.SetCell(0, Cell(1, "a", 1));
        line.SetCell(2, Cell(1, "a", 1));
        Assert.Equal(3, line.GetTrimmedLength());
    }

    // xterm.js: 'BufferLine' › 'getTrimLength' › 'surrogate'
    [Fact]
    public void GetTrimmedLength_counts_a_surrogate_pair_as_one_cell()
    {
        TestBufferLine line = Empty10();
        line.SetCell(0, Cell(1, "a", 1));
        line.SetCell(2, Cell(1, "\U0001D11E", 1));
        Assert.Equal(3, line.GetTrimmedLength());
    }

    // xterm.js: 'BufferLine' › 'getTrimLength' › 'combining'
    [Fact]
    public void GetTrimmedLength_counts_a_combined_cell_as_one_cell()
    {
        TestBufferLine line = Empty10();
        line.SetCell(0, Cell(1, "a", 1));
        line.SetCell(2, Cell(1, "é", 1));
        Assert.Equal(3, line.GetTrimmedLength());
    }

    // xterm.js: 'BufferLine' › 'getTrimLength' › 'fullwidth'
    [Fact]
    public void GetTrimmedLength_counts_the_null_cell_after_a_fullwidth_char()
    {
        TestBufferLine line = Empty10();
        line.SetCell(0, Cell(1, "a", 1));
        line.SetCell(2, Cell(1, "１", 2));
        line.SetCell(3, Cell(0, string.Empty, 0));
        Assert.Equal(4, line.GetTrimmedLength());
    }

    private static void AssertTranslate(TestBufferLine line, bool trimRight, int startCol, int endCol, string expected, params int[] columns)
    {
        List<int> actual = [];
        Assert.Equal(expected, line.TranslateToString(trimRight, startCol, endCol, actual));
        Assert.Equal(columns, actual);
    }

    // xterm.js: 'BufferLine' › 'translateToString with and w'o trimming' › 'empty line'
    [Fact]
    public void TranslateToString_of_an_empty_line()
    {
        TestBufferLine line = Empty10();
        AssertTranslate(line, false, 0, -1, "          ", 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        AssertTranslate(line, true, 0, -1, string.Empty, 0);
    }

    // xterm.js: 'BufferLine' › 'translateToString with and w'o trimming' › 'ASCII'
    [Fact]
    public void TranslateToString_ascii()
    {
        TestBufferLine line = Empty10();
        foreach (int i in new[] { 0, 2, 4, 5 })
        {
            line.SetCell(i, Cell(1, "a", 1));
        }

        AssertTranslate(line, false, 0, -1, "a a aa    ", 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        AssertTranslate(line, true, 0, -1, "a a aa", 0, 1, 2, 3, 4, 5, 6);
        foreach (bool trimRight in new[] { true, false })
        {
            AssertTranslate(line, trimRight, 0, 5, "a a a", 0, 1, 2, 3, 4, 5);
            AssertTranslate(line, trimRight, 0, 4, "a a ", 0, 1, 2, 3, 4);
            AssertTranslate(line, trimRight, 0, 3, "a a", 0, 1, 2, 3);
        }
    }

    // xterm.js: 'BufferLine' › 'translateToString with and w'o trimming' › 'surrogate'
    [Fact]
    public void TranslateToString_surrogate()
    {
        TestBufferLine line = Empty10();
        line.SetCell(0, Cell(1, "a", 1));
        foreach (int i in new[] { 2, 4, 5 })
        {
            line.SetCell(i, Cell(1, "\U0001D11E", 1));
        }

        AssertTranslate(line, false, 0, -1, "a \U0001D11E \U0001D11E\U0001D11E    ", 0, 1, 2, 2, 3, 4, 4, 5, 5, 6, 7, 8, 9, 10);
        AssertTranslate(line, true, 0, -1, "a \U0001D11E \U0001D11E\U0001D11E", 0, 1, 2, 2, 3, 4, 4, 5, 5, 6);
        foreach (bool trimRight in new[] { true, false })
        {
            AssertTranslate(line, trimRight, 0, 5, "a \U0001D11E \U0001D11E", 0, 1, 2, 2, 3, 4, 4, 5);
            AssertTranslate(line, trimRight, 0, 4, "a \U0001D11E ", 0, 1, 2, 2, 3, 4);
            AssertTranslate(line, trimRight, 0, 3, "a \U0001D11E", 0, 1, 2, 2, 3);
        }
    }

    // xterm.js: 'BufferLine' › 'translateToString with and w'o trimming' › 'combining'
    [Fact]
    public void TranslateToString_combining()
    {
        TestBufferLine line = Empty10();
        line.SetCell(0, Cell(1, "a", 1));
        foreach (int i in new[] { 2, 4, 5 })
        {
            line.SetCell(i, Cell(1, "é", 1));
        }

        AssertTranslate(line, false, 0, -1, "a é éé    ", 0, 1, 2, 2, 3, 4, 4, 5, 5, 6, 7, 8, 9, 10);
        AssertTranslate(line, true, 0, -1, "a é éé", 0, 1, 2, 2, 3, 4, 4, 5, 5, 6);
        foreach (bool trimRight in new[] { true, false })
        {
            AssertTranslate(line, trimRight, 0, 5, "a é é", 0, 1, 2, 2, 3, 4, 4, 5);
            AssertTranslate(line, trimRight, 0, 4, "a é ", 0, 1, 2, 2, 3, 4);
            AssertTranslate(line, trimRight, 0, 3, "a é", 0, 1, 2, 2, 3);
        }
    }

    // xterm.js: 'BufferLine' › 'translateToString with and w'o trimming' › 'fullwidth'
    [Fact]
    public void TranslateToString_fullwidth()
    {
        TestBufferLine line = Empty10();
        line.SetCell(0, Cell(1, "a", 1));
        foreach (int i in new[] { 2, 5, 7 })
        {
            line.SetCell(i, Cell(1, "１", 2));
            line.SetCell(i + 1, Cell(0, string.Empty, 0));
        }

        AssertTranslate(line, false, 0, -1, "a １ １１ ", 0, 1, 2, 4, 5, 7, 9, 10);
        AssertTranslate(line, true, 0, -1, "a １ １１", 0, 1, 2, 4, 5, 7, 9);
        foreach (bool trimRight in new[] { true, false })
        {
            AssertTranslate(line, trimRight, 0, 7, "a １ １", 0, 1, 2, 4, 5, 7);
            AssertTranslate(line, trimRight, 0, 6, "a １ １", 0, 1, 2, 4, 5, 7);
            AssertTranslate(line, trimRight, 0, 5, "a １ ", 0, 1, 2, 4, 5);
            AssertTranslate(line, trimRight, 0, 4, "a １", 0, 1, 2, 4);
            AssertTranslate(line, trimRight, 0, 3, "a １", 0, 1, 2, 4);
            AssertTranslate(line, trimRight, 0, 2, "a ", 0, 1, 2);
        }
    }

    // xterm.js: 'BufferLine' › 'translateToString with and w'o trimming' › 'space at end'
    [Fact]
    public void TranslateToString_keeps_a_real_space_at_the_end_when_trimming()
    {
        TestBufferLine line = Empty10();
        foreach (int i in new[] { 0, 2, 4, 5 })
        {
            line.SetCell(i, Cell(1, "a", 1));
        }

        line.SetCell(6, Cell(1, " ", 1));
        AssertTranslate(line, false, 0, -1, "a a aa    ", 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        AssertTranslate(line, true, 0, -1, "a a aa ", 0, 1, 2, 3, 4, 5, 6, 7);
    }

    // xterm.js: 'BufferLine' › 'translateToString with and w'o trimming' › 'should always return some sane value'
    [Fact]
    public void TranslateToString_returns_a_sane_value_for_zero_width_null_cells()
    {
        TestBufferLine line = new(10, CellData.FromCharData(new CharData(BufferConstants.DefaultAttr, BufferConstants.NullCellChar, 0, BufferConstants.NullCellCode)));
        AssertTranslate(line, false, 0, -1, "          ", 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        AssertTranslate(line, true, 0, -1, string.Empty, 0);
    }

    // xterm.js: 'BufferLine' › 'translateToString with and w'o trimming' › 'should work with endCol=0'
    [Fact]
    public void TranslateToString_works_with_an_end_column_of_zero()
    {
        TestBufferLine line = new(10, CellData.FromCharData(new CharData(BufferConstants.DefaultAttr, BufferConstants.NullCellChar, 0, BufferConstants.NullCellCode)));
        line.SetCell(0, Cell(1, "a", 1));
        AssertTranslate(line, true, 0, 0, string.Empty, 0);
    }
}
