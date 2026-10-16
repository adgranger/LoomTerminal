// Ported from xtermjs/xterm.js: src/common/buffer/BufferLine.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.Buffer;

public class CellDataTests
{
    // xterm.js: 'CellData' › 'CharData <--> CellData equality'
    [Fact]
    public void CharData_and_CellData_are_equal_after_a_round_trip()
    {
        CellData cell = new();

        // ASCII
        cell.SetFromCharData(new CharData(123, "a", 1, 'a'));
        Assert.Equal(new CharData(123, "a", 1, 'a'), cell.GetAsCharData());
        Assert.Equal(0u, cell.IsCombined());

        // combining
        cell.SetFromCharData(new CharData(123, "é", 1, '́'));
        Assert.Equal(new CharData(123, "é", 1, '́'), cell.GetAsCharData());
        Assert.Equal(Content.IsCombinedMask, cell.IsCombined());

        // surrogate
        cell.SetFromCharData(new CharData(123, "\U0001D11E", 1, 0x1D11E));
        Assert.Equal(new CharData(123, "\U0001D11E", 1, 0x1D11E), cell.GetAsCharData());
        Assert.Equal(0u, cell.IsCombined());

        // surrogate + combining
        const string surrogateAndCombining = "\U00013080́";
        cell.SetFromCharData(new CharData(123, surrogateAndCombining, 1, surrogateAndCombining[2]));
        Assert.Equal(new CharData(123, surrogateAndCombining, 1, surrogateAndCombining[2]), cell.GetAsCharData());
        Assert.Equal(Content.IsCombinedMask, cell.IsCombined());

        // wide char
        cell.SetFromCharData(new CharData(123, "１", 2, '１'));
        Assert.Equal(new CharData(123, "１", 2, '１'), cell.GetAsCharData());
        Assert.Equal(0u, cell.IsCombined());
    }

    // New test: the empty string is the char of a null cell (JS: charCodeAt(0) of '' is NaN, which is 0 in bit operations).
    [Fact]
    public void SetFromCharData_accepts_the_null_cell_char()
    {
        CellData cell = CellData.FromCharData(new CharData(0, BufferConstants.NullCellChar, BufferConstants.NullCellWidth, BufferConstants.NullCellCode));
        Assert.Equal(new CharData(0, BufferConstants.NullCellChar, BufferConstants.NullCellWidth, BufferConstants.NullCellCode), cell.GetAsCharData());
        Assert.Equal(0u, cell.IsCombined());
    }
}
