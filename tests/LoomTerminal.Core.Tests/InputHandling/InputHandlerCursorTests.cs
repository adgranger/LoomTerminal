// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerCursorTests : InputHandlerTestBase
{
    public InputHandlerCursorTests() => BufferService.Resize(10, 10);

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'cursor forward (CUF)'
    [Fact]
    public void CUF_moves_the_cursor_forward()
    {
        InputHandler.Parse("\x1b[C");
        Assert.Equal<int>([1, 0], GetCursor());
        InputHandler.Parse("\x1b[1C");
        Assert.Equal<int>([2, 0], GetCursor());
        InputHandler.Parse("\x1b[4C");
        Assert.Equal<int>([6, 0], GetCursor());
        InputHandler.Parse("\x1b[100C");
        Assert.Equal<int>([9, 0], GetCursor());
        // should not change y
        BufferService.Buffer.X = 8;
        BufferService.Buffer.Y = 4;
        InputHandler.Parse("\x1b[C");
        Assert.Equal<int>([9, 4], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'cursor backward (CUB)'
    [Fact]
    public void CUB_moves_the_cursor_backward()
    {
        InputHandler.Parse("\x1b[D");
        Assert.Equal<int>([0, 0], GetCursor());
        InputHandler.Parse("\x1b[1D");
        Assert.Equal<int>([0, 0], GetCursor());
        // place cursor at end of first line
        InputHandler.Parse("\x1b[100C");
        InputHandler.Parse("\x1b[D");
        Assert.Equal<int>([8, 0], GetCursor());
        InputHandler.Parse("\x1b[1D");
        Assert.Equal<int>([7, 0], GetCursor());
        InputHandler.Parse("\x1b[4D");
        Assert.Equal<int>([3, 0], GetCursor());
        InputHandler.Parse("\x1b[100D");
        Assert.Equal<int>([0, 0], GetCursor());
        // should not change y
        BufferService.Buffer.X = 4;
        BufferService.Buffer.Y = 4;
        InputHandler.Parse("\x1b[D");
        Assert.Equal<int>([3, 4], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'cursor down (CUD)'
    [Fact]
    public void CUD_moves_the_cursor_down()
    {
        InputHandler.Parse("\x1b[B");
        Assert.Equal<int>([0, 1], GetCursor());
        InputHandler.Parse("\x1b[1B");
        Assert.Equal<int>([0, 2], GetCursor());
        InputHandler.Parse("\x1b[4B");
        Assert.Equal<int>([0, 6], GetCursor());
        InputHandler.Parse("\x1b[100B");
        Assert.Equal<int>([0, 9], GetCursor());
        // should not change x
        BufferService.Buffer.X = 8;
        BufferService.Buffer.Y = 0;
        InputHandler.Parse("\x1b[B");
        Assert.Equal<int>([8, 1], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'cursor up (CUU)'
    [Fact]
    public void CUU_moves_the_cursor_up()
    {
        InputHandler.Parse("\x1b[A");
        Assert.Equal<int>([0, 0], GetCursor());
        InputHandler.Parse("\x1b[1A");
        Assert.Equal<int>([0, 0], GetCursor());
        // place cursor at beginning of last row
        InputHandler.Parse("\x1b[100B");
        InputHandler.Parse("\x1b[A");
        Assert.Equal<int>([0, 8], GetCursor());
        InputHandler.Parse("\x1b[1A");
        Assert.Equal<int>([0, 7], GetCursor());
        InputHandler.Parse("\x1b[4A");
        Assert.Equal<int>([0, 3], GetCursor());
        InputHandler.Parse("\x1b[100A");
        Assert.Equal<int>([0, 0], GetCursor());
        // should not change x
        BufferService.Buffer.X = 8;
        BufferService.Buffer.Y = 9;
        InputHandler.Parse("\x1b[A");
        Assert.Equal<int>([8, 8], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'cursor next line (CNL)'
    [Fact]
    public void CNL_moves_to_the_next_line()
    {
        InputHandler.Parse("\x1b[E");
        Assert.Equal<int>([0, 1], GetCursor());
        InputHandler.Parse("\x1b[1E");
        Assert.Equal<int>([0, 2], GetCursor());
        InputHandler.Parse("\x1b[4E");
        Assert.Equal<int>([0, 6], GetCursor());
        InputHandler.Parse("\x1b[100E");
        Assert.Equal<int>([0, 9], GetCursor());
        // should reset x to zero
        BufferService.Buffer.X = 8;
        BufferService.Buffer.Y = 0;
        InputHandler.Parse("\x1b[E");
        Assert.Equal<int>([0, 1], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'cursor previous line (CPL)'
    [Fact]
    public void CPL_moves_to_the_previous_line()
    {
        InputHandler.Parse("\x1b[F");
        Assert.Equal<int>([0, 0], GetCursor());
        InputHandler.Parse("\x1b[1F");
        Assert.Equal<int>([0, 0], GetCursor());
        // place cursor at beginning of last row
        InputHandler.Parse("\x1b[100E");
        InputHandler.Parse("\x1b[F");
        Assert.Equal<int>([0, 8], GetCursor());
        InputHandler.Parse("\x1b[1F");
        Assert.Equal<int>([0, 7], GetCursor());
        InputHandler.Parse("\x1b[4F");
        Assert.Equal<int>([0, 3], GetCursor());
        InputHandler.Parse("\x1b[100F");
        Assert.Equal<int>([0, 0], GetCursor());
        // should reset x to zero
        BufferService.Buffer.X = 8;
        BufferService.Buffer.Y = 9;
        InputHandler.Parse("\x1b[F");
        Assert.Equal<int>([0, 8], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'cursor character absolute (CHA)'
    [Fact]
    public void CHA_moves_to_an_absolute_column()
    {
        InputHandler.Parse("\x1b[G");
        Assert.Equal<int>([0, 0], GetCursor());
        InputHandler.Parse("\x1b[1G");
        Assert.Equal<int>([0, 0], GetCursor());
        InputHandler.Parse("\x1b[2G");
        Assert.Equal<int>([1, 0], GetCursor());
        InputHandler.Parse("\x1b[5G");
        Assert.Equal<int>([4, 0], GetCursor());
        InputHandler.Parse("\x1b[100G");
        Assert.Equal<int>([9, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'cursor position (CUP)'
    [Fact]
    public void CUP_positions_the_cursor()
    {
        BufferService.Buffer.X = 5;
        BufferService.Buffer.Y = 5;
        InputHandler.Parse("\x1b[H");
        Assert.Equal<int>([0, 0], GetCursor());
        BufferService.Buffer.X = 5;
        BufferService.Buffer.Y = 5;
        InputHandler.Parse("\x1b[1H");
        Assert.Equal<int>([0, 0], GetCursor());
        BufferService.Buffer.X = 5;
        BufferService.Buffer.Y = 5;
        InputHandler.Parse("\x1b[1;1H");
        Assert.Equal<int>([0, 0], GetCursor());
        BufferService.Buffer.X = 5;
        BufferService.Buffer.Y = 5;
        InputHandler.Parse("\x1b[8H");
        Assert.Equal<int>([0, 7], GetCursor());
        BufferService.Buffer.X = 5;
        BufferService.Buffer.Y = 5;
        InputHandler.Parse("\x1b[;8H");
        Assert.Equal<int>([7, 0], GetCursor());
        BufferService.Buffer.X = 5;
        BufferService.Buffer.Y = 5;
        InputHandler.Parse("\x1b[100;100H");
        Assert.Equal<int>([9, 9], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'horizontal position absolute (HPA)'
    [Fact]
    public void HPA_moves_to_an_absolute_column()
    {
        InputHandler.Parse("\x1b[`");
        Assert.Equal<int>([0, 0], GetCursor());
        InputHandler.Parse("\x1b[1`");
        Assert.Equal<int>([0, 0], GetCursor());
        InputHandler.Parse("\x1b[2`");
        Assert.Equal<int>([1, 0], GetCursor());
        InputHandler.Parse("\x1b[5`");
        Assert.Equal<int>([4, 0], GetCursor());
        InputHandler.Parse("\x1b[100`");
        Assert.Equal<int>([9, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'horizontal position relative (HPR)'
    [Fact]
    public void HPR_moves_the_cursor_right()
    {
        InputHandler.Parse("\x1b[a");
        Assert.Equal<int>([1, 0], GetCursor());
        InputHandler.Parse("\x1b[1a");
        Assert.Equal<int>([2, 0], GetCursor());
        InputHandler.Parse("\x1b[4a");
        Assert.Equal<int>([6, 0], GetCursor());
        InputHandler.Parse("\x1b[100a");
        Assert.Equal<int>([9, 0], GetCursor());
        // should not change y
        BufferService.Buffer.X = 8;
        BufferService.Buffer.Y = 4;
        InputHandler.Parse("\x1b[a");
        Assert.Equal<int>([9, 4], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'vertical position absolute (VPA)'
    [Fact]
    public void VPA_moves_to_an_absolute_row()
    {
        InputHandler.Parse("\x1b[d");
        Assert.Equal<int>([0, 0], GetCursor());
        InputHandler.Parse("\x1b[1d");
        Assert.Equal<int>([0, 0], GetCursor());
        InputHandler.Parse("\x1b[2d");
        Assert.Equal<int>([0, 1], GetCursor());
        InputHandler.Parse("\x1b[5d");
        Assert.Equal<int>([0, 4], GetCursor());
        InputHandler.Parse("\x1b[100d");
        Assert.Equal<int>([0, 9], GetCursor());
        // should not change x
        BufferService.Buffer.X = 8;
        BufferService.Buffer.Y = 4;
        InputHandler.Parse("\x1b[d");
        Assert.Equal<int>([8, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'vertical position relative (VPR)'
    [Fact]
    public void VPR_moves_the_cursor_down_by_rows()
    {
        InputHandler.Parse("\x1b[e");
        Assert.Equal<int>([0, 1], GetCursor());
        InputHandler.Parse("\x1b[1e");
        Assert.Equal<int>([0, 2], GetCursor());
        InputHandler.Parse("\x1b[4e");
        Assert.Equal<int>([0, 6], GetCursor());
        InputHandler.Parse("\x1b[100e");
        Assert.Equal<int>([0, 9], GetCursor());
        // should not change x
        BufferService.Buffer.X = 8;
        BufferService.Buffer.Y = 4;
        InputHandler.Parse("\x1b[e");
        Assert.Equal<int>([8, 5], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'CUF'
    [Fact]
    public void CUF_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[C");
        Assert.Equal<int>([9, 9], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[C");
        Assert.Equal<int>([1, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'CUB'
    [Fact]
    public void CUB_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[D");
        Assert.Equal<int>([8, 9], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[D");
        Assert.Equal<int>([0, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'CUD'
    [Fact]
    public void CUD_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[B");
        Assert.Equal<int>([9, 9], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[B");
        Assert.Equal<int>([0, 1], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'CUU'
    [Fact]
    public void CUU_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[A");
        Assert.Equal<int>([9, 8], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[A");
        Assert.Equal<int>([0, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'CNL'
    [Fact]
    public void CNL_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[E");
        Assert.Equal<int>([0, 9], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[E");
        Assert.Equal<int>([0, 1], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'CPL'
    [Fact]
    public void CPL_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[F");
        Assert.Equal<int>([0, 8], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[F");
        Assert.Equal<int>([0, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'CHA'
    [Fact]
    public void CHA_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[5G");
        Assert.Equal<int>([4, 9], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[5G");
        Assert.Equal<int>([4, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'CUP'
    [Fact]
    public void CUP_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[5;5H");
        Assert.Equal<int>([4, 4], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[5;5H");
        Assert.Equal<int>([4, 4], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'HPA'
    [Fact]
    public void HPA_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[5`");
        Assert.Equal<int>([4, 9], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[5`");
        Assert.Equal<int>([4, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'HPR'
    [Fact]
    public void HPR_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[a");
        Assert.Equal<int>([9, 9], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[a");
        Assert.Equal<int>([1, 0], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'VPA'
    [Fact]
    public void VPA_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[5d");
        Assert.Equal<int>([9, 4], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[5d");
        Assert.Equal<int>([0, 4], GetCursor());
    }

    // xterm.js: 'InputHandler' › 'cursor positioning' › 'should clamp cursor into addressible range' › 'VPR'
    [Fact]
    public void VPR_is_clamped_into_the_addressable_range()
    {
        BufferService.Buffer.X = 10000;
        BufferService.Buffer.Y = 10000;
        InputHandler.Parse("\x1b[e");
        Assert.Equal<int>([9, 9], GetCursor());
        BufferService.Buffer.X = -10000;
        BufferService.Buffer.Y = -10000;
        InputHandler.Parse("\x1b[e");
        Assert.Equal<int>([0, 1], GetCursor());
    }
}
