// Ported from xtermjs/xterm.js: src/common/buffer/BufferLine.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.Buffer;

public class BufferLineTests
{
    private sealed class TestBufferLine : BufferLine
    {
        public TestBufferLine(int cols, CellData? fillCellData = null, bool isWrapped = false)
            : base(cols, fillCellData, isWrapped)
        {
        }

        public Dictionary<int, string> Combined => _combined;

        public List<CharData> ToArray()
        {
            List<CharData> result = [];
            for (int i = 0; i < Length; ++i)
            {
                result.Add(LoadCell(i, new CellData()).GetAsCharData());
            }

            return result;
        }
    }

    private static readonly CharData NullCell =
        new(0, BufferConstants.NullCellChar, BufferConstants.NullCellWidth, BufferConstants.NullCellCode);

    // xterm.js: 'BufferLine' › 'ctor'
    [Fact]
    public void Constructor_fills_the_line_and_sets_the_wrapped_flag()
    {
        IBufferLine line = new TestBufferLine(0);
        Assert.Equal(0, line.Length);
        Assert.False(line.IsWrapped);

        line = new TestBufferLine(10);
        Assert.Equal(10, line.Length);
        Assert.Equal(NullCell, line.LoadCell(0, new CellData()).GetAsCharData());
        Assert.False(line.IsWrapped);

        line = new TestBufferLine(10, null, true);
        Assert.Equal(10, line.Length);
        Assert.Equal(NullCell, line.LoadCell(0, new CellData()).GetAsCharData());
        Assert.True(line.IsWrapped);

        line = new TestBufferLine(10, CellData.FromCharData(new CharData(123, "a", 456, 'a')), true);
        Assert.Equal(10, line.Length);
        Assert.Equal(new CharData(123, "a", 456, 'a'), line.LoadCell(0, new CellData()).GetAsCharData());
        Assert.True(line.IsWrapped);
    }

    // The xterm.js tests 'insertCells' … 'should support combining chars' (lines 189–277) exercise insertCells,
    // deleteCells, replaceCells, fill, clone and copyFrom: they are ported by P1-19, with those methods.

    [Fact]
    public void Get_and_Set_round_trip_plain_and_combined_cells()
    {
        TestBufferLine line = new(3);
        line.Set(0, new CharData(7, "a", 1, 'a'));
        line.Set(1, new CharData(8, "é", 1, '́'));
        Assert.Equal(new CharData(7, "a", 1, 'a'), line.Get(0));
        Assert.Equal(new CharData(8, "é", 1, '́'), line.Get(1));
        Assert.Equal(NullCell, line.Get(2));
        Assert.Equal("é", line.Combined[1]);
    }

    [Fact]
    public void Primitive_getters_read_the_packed_cell()
    {
        TestBufferLine line = new(3);
        CellData cell = CellData.FromCharData(new CharData(5, "中", 2, 0x4E2D));
        cell.Bg = BgFlags.Protected;
        line.SetCell(0, cell);
        line.SetCell(1, CellData.FromCharData(new CharData(0, "é", 1, '́')));

        Assert.Equal(2, line.GetWidth(0));
        Assert.NotEqual(0u, line.HasWidth(0));
        Assert.Equal(5u, line.GetFg(0));
        Assert.Equal(BgFlags.Protected, line.GetBg(0));
        Assert.NotEqual(0u, line.IsProtected(0));
        Assert.NotEqual(0u, line.HasContent(0));
        Assert.Equal(0x4E2Du, line.GetCodePoint(0));
        Assert.Equal(0u, line.IsCombined(0));
        Assert.Equal("中", line.GetString(0));

        Assert.NotEqual(0u, line.IsCombined(1));
        Assert.Equal('́', line.GetCodePoint(1));
        Assert.Equal("é", line.GetString(1));

        Assert.Equal(0u, line.HasContent(2));
        Assert.Equal(string.Empty, line.GetString(2));
    }

    [Fact]
    public void SetCellFromCodepoint_and_AddCodepointToCell_build_a_combined_cell()
    {
        TestBufferLine line = new(2);
        line.SetCellFromCodepoint(0, 'e', 1, new AttributeData { Fg = 9 });
        Assert.Equal("e", line.GetString(0));
        Assert.Equal(9u, line.GetFg(0));

        line.AddCodepointToCell(0, 0x301, 0);
        Assert.Equal("é", line.GetString(0));
        Assert.Equal(1, line.GetWidth(0));

        line.AddCodepointToCell(0, 0x302, 0);
        Assert.Equal("é̂", line.GetString(0));

        // an empty cell takes the code point with a width of 1, unless a width is given
        line.AddCodepointToCell(1, 'x', 0);
        Assert.Equal(new CharData(0, "x", 1, 'x'), line.Get(1));
        line.AddCodepointToCell(1, 0x301, 2);
        Assert.Equal(2, line.GetWidth(1));
    }

    [Fact]
    public void LoadCell_returns_the_combined_data_and_the_extended_attributes()
    {
        TestBufferLine line = new(2);
        AttributeData attrs = new() { Bg = BgFlags.HasExtended };
        attrs.Extended.UrlId = 42;
        line.SetCellFromCodepoint(0, 'a', 1, attrs);
        line.SetCell(1, CellData.FromCharData(new CharData(0, "é", 1, '́')));

        CellData cell = line.LoadCell(0, new CellData());
        Assert.Equal(42, cell.Extended.UrlId);
        Assert.Equal("é", line.LoadCell(1, new CellData()).CombinedData);
    }
}
