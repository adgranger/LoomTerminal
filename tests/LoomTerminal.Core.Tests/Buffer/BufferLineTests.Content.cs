// Ported from xtermjs/xterm.js: src/common/buffer/BufferLine.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.Buffer;

public partial class BufferLineTests
{
    private static TestBufferLine NullLine(int cols, int width = 1) =>
        new(cols, CellData.FromCharData(new CharData(BufferConstants.DefaultAttr, BufferConstants.NullCellChar, width, BufferConstants.NullCellCode)));

    // xterm.js: 'BufferLine' › 'addCharToCell' › 'should set width to 1 for empty cell'
    [Fact]
    public void AddCodepointToCell_sets_the_width_to_1_for_an_empty_cell()
    {
        TestBufferLine line = NullLine(3);
        line.AddCodepointToCell(0, 0x301, 0);
        CellData cell = line.LoadCell(0, new CellData());
        Assert.Equal(new CharData(BufferConstants.DefaultAttr, "́", 1, 0x0301), cell.GetAsCharData());
        Assert.Equal(0u, cell.IsCombined());
    }

    // xterm.js: 'BufferLine' › 'addCharToCell' › 'should add char to combining string in cell'
    [Fact]
    public void AddCodepointToCell_adds_a_char_to_the_combining_string_in_the_cell()
    {
        TestBufferLine line = NullLine(3);
        CellData cell = line.LoadCell(0, new CellData());
        cell.SetFromCharData(new CharData(123, "é", 1, "é"[1]));
        line.SetCell(0, cell);
        line.AddCodepointToCell(0, 0x301, 0);
        line.LoadCell(0, cell);
        Assert.Equal(new CharData(123, "é́", 1, 0x0301), cell.GetAsCharData());
        Assert.Equal(Content.IsCombinedMask, cell.IsCombined());
    }

    // xterm.js: 'BufferLine' › 'addCharToCell' › 'should create combining string on taken cell'
    [Fact]
    public void AddCodepointToCell_creates_a_combining_string_on_a_taken_cell()
    {
        TestBufferLine line = NullLine(3);
        CellData cell = line.LoadCell(0, new CellData());
        cell.SetFromCharData(new CharData(123, "e", 1, 'e'));
        line.SetCell(0, cell);
        line.AddCodepointToCell(0, 0x301, 0);
        line.LoadCell(0, cell);
        Assert.Equal(new CharData(123, "é", 1, 0x0301), cell.GetAsCharData());
        Assert.Equal(Content.IsCombinedMask, cell.IsCombined());
    }

    private const string Yen = "￥";

    private static TestBufferLine PopulatedWide()
    {
        TestBufferLine line = NullLine(10, 0);
        CellData cell = Cell(1, Yen, 2);
        for (int i = 0; i < line.Length; i += 2)
        {
            line.SetCell(i, cell);
        }

        return line;
    }

    private static CellData A() => Cell(1, "a", 1);

    // xterm.js: 'BufferLine' › 'correct fullwidth handling' › 'insert - wide char at pos'
    [Fact]
    public void Insert_keeps_fullwidth_pairs_consistent_with_a_wide_char_at_pos()
    {
        TestBufferLine line = PopulatedWide();
        line.InsertCells(9, 1, A());
        Assert.Equal($"{Yen}{Yen}{Yen}{Yen} a", line.TranslateToString());
        line.InsertCells(8, 1, A());
        Assert.Equal($"{Yen}{Yen}{Yen}{Yen}a ", line.TranslateToString());
        line.InsertCells(1, 1, A());
        Assert.Equal($" a {Yen}{Yen}{Yen}a", line.TranslateToString());
    }

    // xterm.js: 'BufferLine' › 'correct fullwidth handling' › 'insert - wide char at end'
    [Fact]
    public void Insert_keeps_fullwidth_pairs_consistent_with_a_wide_char_at_the_end()
    {
        TestBufferLine line = PopulatedWide();
        line.InsertCells(0, 3, A());
        Assert.Equal($"aaa{Yen}{Yen}{Yen} ", line.TranslateToString());
        line.InsertCells(4, 1, A());
        Assert.Equal($"aaa a {Yen}{Yen}", line.TranslateToString());
        line.InsertCells(4, 1, A());
        Assert.Equal($"aaa aa {Yen} ", line.TranslateToString());
    }

    // xterm.js: 'BufferLine' › 'correct fullwidth handling' › 'delete'
    [Fact]
    public void Delete_keeps_fullwidth_pairs_consistent()
    {
        TestBufferLine line = PopulatedWide();
        line.DeleteCells(0, 1, A());
        Assert.Equal($" {Yen}{Yen}{Yen}{Yen}a", line.TranslateToString());
        line.DeleteCells(5, 2, A());
        Assert.Equal($" {Yen}{Yen}{Yen}aaa", line.TranslateToString());
        line.DeleteCells(0, 2, A());
        Assert.Equal($" {Yen}{Yen}aaaaa", line.TranslateToString());
    }

    private static string Replaced(int start, int end)
    {
        TestBufferLine line = PopulatedWide();
        line.ReplaceCells(start, end, A());
        return line.TranslateToString();
    }

    // xterm.js: 'BufferLine' › 'correct fullwidth handling' › 'replace - start at 0'
    [Fact]
    public void Replace_keeps_fullwidth_pairs_consistent_when_starting_at_0()
    {
        Assert.Equal($"a {Yen}{Yen}{Yen}{Yen}", Replaced(0, 1));
        Assert.Equal($"aa{Yen}{Yen}{Yen}{Yen}", Replaced(0, 2));
        Assert.Equal($"aaa {Yen}{Yen}{Yen}", Replaced(0, 3));
        Assert.Equal($"aaaaaaaa{Yen}", Replaced(0, 8));
        Assert.Equal("aaaaaaaaa ", Replaced(0, 9));
        Assert.Equal("aaaaaaaaaa", Replaced(0, 10));
    }

    // xterm.js: 'BufferLine' › 'correct fullwidth handling' › 'replace - start at 1'
    [Fact]
    public void Replace_keeps_fullwidth_pairs_consistent_when_starting_at_1()
    {
        Assert.Equal($" a{Yen}{Yen}{Yen}{Yen}", Replaced(1, 2));
        Assert.Equal($" aa {Yen}{Yen}{Yen}", Replaced(1, 3));
        Assert.Equal($" aaa{Yen}{Yen}{Yen}", Replaced(1, 4));
        Assert.Equal($" aaaaaaa{Yen}", Replaced(1, 8));
        Assert.Equal(" aaaaaaaa ", Replaced(1, 9));
        Assert.Equal(" aaaaaaaaa", Replaced(1, 10));
    }

    private static CellData Curly()
    {
        CellData cell = Cell(1, "a", 0);
        cell.Extended.UnderlineStyle = UnderlineStyle.Curly;
        cell.Bg |= BgFlags.HasExtended;
        return cell;
    }

    /// <summary>The line used by the 'setCells', 'loadCell', 'clone' and 'copyFrom' tests of xterm.js.</summary>
    private static TestBufferLine ExtendedLine()
    {
        TestBufferLine line = new(5);
        CellData cell = Cell(1, "a", 0);

        // no eAttrs
        line.SetCell(0, cell);

        // some underline style
        cell.Extended.UnderlineStyle = UnderlineStyle.Curly;
        cell.Bg |= BgFlags.HasExtended;
        line.SetCell(1, cell);

        // same eAttr, different codepoint
        cell.Content = 65; // 'A'
        line.SetCell(2, cell);

        // different eAttr
        cell.Extended = cell.Extended.Clone();
        cell.Extended.UnderlineStyle = UnderlineStyle.Dotted;
        line.SetCell(3, cell);

        // no eAttrs again
        cell.Bg &= ~BgFlags.HasExtended;
        line.SetCell(4, cell);
        return line;
    }

    // xterm.js: 'BufferLine' › 'extended attributes' › 'setCells'
    [Fact]
    public void SetCell_stores_extended_attributes_by_reference()
    {
        TestBufferLine line = ExtendedLine();
        Assert.Equal(
            [Cd(1, "a", 0), Cd(1, "a", 0), Cd(1, "A", 0), Cd(1, "A", 0), Cd(1, "A", 0)],
            line.ToArray());
        Assert.Null(line.Ext(0));
        Assert.Equal(UnderlineStyle.Curly, line.Ext(1)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Curly, line.Ext(2)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Dotted, line.Ext(3)!.UnderlineStyle);
        Assert.Null(line.Ext(4));

        // should be ref to the same object
        Assert.Same(line.Ext(1), line.Ext(2));

        // should be a different obj
        Assert.NotSame(line.Ext(1), line.Ext(3));
    }

    // xterm.js: 'BufferLine' › 'extended attributes' › 'loadCell'
    [Fact]
    public void LoadCell_returns_the_extended_attributes()
    {
        TestBufferLine line = ExtendedLine();
        CellData[] cells = new CellData[5];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = line.LoadCell(i, new CellData());
        }

        Assert.Equal(UnderlineStyle.None, cells[0].Extended.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Curly, cells[1].Extended.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Curly, cells[2].Extended.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Dotted, cells[3].Extended.UnderlineStyle);
        Assert.Equal(UnderlineStyle.None, cells[4].Extended.UnderlineStyle);
        Assert.Same(cells[1].Extended, cells[2].Extended);
        Assert.NotSame(cells[2].Extended, cells[3].Extended);
    }

    // xterm.js: 'BufferLine' › 'extended attributes' › 'fill'
    [Fact]
    public void Fill_stores_the_extended_attributes_of_every_cell()
    {
        TestBufferLine line = new(3);
        line.Fill(Curly());
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(UnderlineStyle.Curly, line.Ext(i)!.UnderlineStyle);
        }
    }

    // xterm.js: 'BufferLine' › 'extended attributes' › 'insertCells'
    [Fact]
    public void InsertCells_stores_the_extended_attributes_of_the_inserted_cells()
    {
        TestBufferLine line = new(5);
        CellData cell = Curly();
        line.InsertCells(1, 3, cell);
        Assert.Equal(UnderlineStyle.Curly, line.Ext(1)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Curly, line.Ext(2)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Curly, line.Ext(3)!.UnderlineStyle);
        Assert.Null(line.Ext(4));
        cell.Extended = cell.Extended.Clone();
        cell.Extended.UnderlineStyle = UnderlineStyle.Dotted;
        line.InsertCells(2, 2, cell);
        Assert.Equal(UnderlineStyle.Curly, line.Ext(1)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Dotted, line.Ext(2)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Dotted, line.Ext(3)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Curly, line.Ext(4)!.UnderlineStyle);
    }

    // xterm.js: 'BufferLine' › 'extended attributes' › 'deleteCells'
    [Fact]
    public void DeleteCells_moves_the_extended_attributes_and_stores_the_fill_ones()
    {
        TestBufferLine line = new(5);
        CellData fillCell = Curly();
        line.Fill(fillCell);
        fillCell.Extended = fillCell.Extended.Clone();
        fillCell.Extended.UnderlineStyle = UnderlineStyle.Double;
        line.DeleteCells(1, 3, fillCell);
        Assert.Equal(UnderlineStyle.Curly, line.Ext(0)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Curly, line.Ext(1)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Double, line.Ext(2)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Double, line.Ext(3)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Double, line.Ext(4)!.UnderlineStyle);
    }

    // xterm.js: 'BufferLine' › 'extended attributes' › 'replaceCells'
    [Fact]
    public void ReplaceCells_stores_the_extended_attributes_of_the_replaced_cells()
    {
        TestBufferLine line = new(5);
        CellData fillCell = Curly();
        line.Fill(fillCell);
        fillCell.Extended = fillCell.Extended.Clone();
        fillCell.Extended.UnderlineStyle = UnderlineStyle.Double;
        line.ReplaceCells(1, 3, fillCell);
        Assert.Equal(UnderlineStyle.Curly, line.Ext(0)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Double, line.Ext(1)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Double, line.Ext(2)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Curly, line.Ext(3)!.UnderlineStyle);
        Assert.Equal(UnderlineStyle.Curly, line.Ext(4)!.UnderlineStyle);
    }

    // xterm.js: 'BufferLine' › 'extended attributes' › 'clone'
    [Fact]
    public void Clone_shares_the_extended_attributes()
    {
        TestBufferLine line = ExtendedLine();

        // Clone returns a plain BufferLine: read its extended attributes through a CopyFrom, which shares them
        TestBufferLine clone = new(0);
        clone.CopyFrom(line.Clone());
        for (int i = 0; i < 5; i++)
        {
            Assert.Same(line.Ext(i), clone.Ext(i));
        }
    }

    // xterm.js: 'BufferLine' › 'extended attributes' › 'copyFrom'
    [Fact]
    public void CopyFrom_shares_the_extended_attributes()
    {
        TestBufferLine initial = ExtendedLine();
        TestBufferLine line = new(5);
        line.Fill(Cell(1, "b", 0));
        line.CopyFrom(initial);
        for (int i = 0; i < 5; i++)
        {
            Assert.Same(initial.Ext(i), line.Ext(i));
        }
    }
}
