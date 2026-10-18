// Ported from xtermjs/xterm.js: src/common/buffer/BufferRange.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.Buffer;

public class BufferRangeTests
{
    private static BufferRange CreateRange(int x1, int y1, int x2, int y2) => new(new BufferCellPosition(x1, y1), new BufferCellPosition(x2, y2));

    // xterm.js: 'BufferRange' › 'getRangeLength' › 'should get range for single line'
    [Fact]
    public void GetRangeLength_for_a_single_line() =>
        Assert.Equal(4, BufferRange.GetRangeLength(CreateRange(1, 1, 4, 1), 0));

    // xterm.js: 'BufferRange' › 'getRangeLength' › 'should throw for invalid range'
    [Fact]
    public void GetRangeLength_throws_for_an_invalid_range() =>
        Assert.Throws<ArgumentException>(() => BufferRange.GetRangeLength(CreateRange(1, 3, 1, 1), 0));

    // xterm.js: 'BufferRange' › 'getRangeLength' › 'should get range multiple lines'
    [Fact]
    public void GetRangeLength_for_multiple_lines() =>
        Assert.Equal(24, BufferRange.GetRangeLength(CreateRange(1, 1, 4, 5), 5));

    // xterm.js: 'BufferRange' › 'getRangeLength' › 'should get range for end line right after start line'
    [Fact]
    public void GetRangeLength_for_an_end_line_right_after_the_start_line() =>
        Assert.Equal(12, BufferRange.GetRangeLength(CreateRange(1, 1, 7, 2), 5));
}
