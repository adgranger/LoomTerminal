// Ported from xtermjs/xterm.js: src/browser/input/MoveToCell.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Input;

namespace LoomTerminal.Presentation.Tests.Input;

public sealed class MoveToCellTests : IDisposable
{
    private readonly TerminalCore _core = new(new TerminalOptions { Cols = 5, Rows = 5 });

    public MoveToCellTests()
    {
        _core.Buffers.Active.X = 3;
        _core.Buffers.Active.Y = 3;
    }

    public void Dispose() => _core.Dispose();

    private string Sequence(int x, int y, bool applicationCursor = false) =>
        MoveToCell.Sequence(x, y, _core.BufferService, applicationCursor);

    private static string Arrows(char direction, int count, bool applicationCursor = false) =>
        string.Concat(Enumerable.Repeat("\u001b" + (applicationCursor ? "O" : "[") + direction, count));

    // normal buffer: should use the right directional escape sequences
    [Fact]
    public void The_right_directional_escape_sequences_are_used()
    {
        Assert.Equal(Arrows('D', 2), Sequence(1, 3));
        Assert.Equal(Arrows('D', 1), Sequence(2, 3));
        Assert.Equal(Arrows('C', 1), Sequence(4, 3));
        Assert.Equal(Arrows('C', 2), Sequence(5, 3));
    }

    // normal buffer: should wrap around entire row instead of doing up and down when the Y value differs
    [Theory]
    [InlineData(1, 1, 'D', 12)]
    [InlineData(2, 1, 'D', 11)]
    [InlineData(3, 1, 'D', 10)]
    [InlineData(4, 1, 'D', 9)]
    [InlineData(5, 1, 'D', 8)]
    [InlineData(1, 2, 'D', 7)]
    [InlineData(2, 2, 'D', 6)]
    [InlineData(3, 2, 'D', 5)]
    [InlineData(4, 2, 'D', 4)]
    [InlineData(5, 2, 'D', 3)]
    [InlineData(1, 4, 'C', 3)]
    [InlineData(2, 4, 'C', 4)]
    [InlineData(3, 4, 'C', 5)]
    [InlineData(4, 4, 'C', 6)]
    [InlineData(5, 4, 'C', 7)]
    [InlineData(1, 5, 'C', 8)]
    [InlineData(2, 5, 'C', 9)]
    [InlineData(3, 5, 'C', 10)]
    [InlineData(4, 5, 'C', 11)]
    [InlineData(5, 5, 'C', 12)]
    public void The_cursor_wraps_around_entire_rows_when_the_Y_value_differs(int x, int y, char direction, int count)
    {
        Assert.Equal(Arrows(direction, count), Sequence(x, y));
    }

    // normal buffer: should use the correct character for application cursor
    [Fact]
    public void The_application_cursor_character_is_used()
    {
        Assert.Equal(Arrows('D', 10, true), Sequence(3, 1, true));
        Assert.Equal(Arrows('D', 5, true), Sequence(3, 2, true));
        Assert.Equal(Arrows('D', 1, true), Sequence(2, 3, true));
        Assert.Equal(Arrows('C', 1, true), Sequence(4, 3, true));
        Assert.Equal(Arrows('C', 5, true), Sequence(3, 4, true));
        Assert.Equal(Arrows('C', 10, true), Sequence(3, 5, true));
    }

    // alt buffer: should move the cursor across rows
    [Fact]
    public void The_alt_buffer_moves_the_cursor_across_rows()
    {
        _core.Write("\u001b[?1049h");
        _core.Buffers.Active.X = 3;
        _core.Buffers.Active.Y = 3;

        Assert.Equal(Arrows('B', 1) + Arrows('C', 1), Sequence(4, 4));
    }
}
