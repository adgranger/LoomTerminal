// Ported from xtermjs/xterm.js: src/common/buffer/BufferRange.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Globalization;

namespace LoomTerminal.Core.Buffer;

/// <summary>A position of a cell in the buffer.</summary>
/// <param name="X">The column.</param>
/// <param name="Y">The row.</param>
internal readonly record struct BufferCellPosition(int X, int Y);

/// <summary>A range of cells in the buffer, both ends included.</summary>
/// <param name="Start">The first cell.</param>
/// <param name="End">The last cell.</param>
internal readonly record struct BufferRange(BufferCellPosition Start, BufferCellPosition End)
{
    /// <summary>The number of cells in <paramref name="range"/>; throws if the end is on a row before the start.</summary>
    public static int GetRangeLength(BufferRange range, int bufferCols)
    {
        if (range.Start.Y > range.End.Y)
        {
            throw new ArgumentException(string.Create(
                CultureInfo.InvariantCulture,
                $"Buffer range end ({range.End.X}, {range.End.Y}) cannot be before start ({range.Start.X}, {range.Start.Y})"));
        }

        return (bufferCols * (range.End.Y - range.Start.Y)) + (range.End.X - range.Start.X + 1);
    }
}
