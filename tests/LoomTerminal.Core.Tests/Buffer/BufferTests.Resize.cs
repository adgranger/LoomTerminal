// Ported from xtermjs/xterm.js: src/common/buffer/Buffer.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Services;
using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.Buffer;

public sealed partial class BufferTests
{
    // xterm.js: 'Buffer' › 'resize' › 'column size is reduced' › 'should trim the data in the buffer'
    [Fact]
    public void Resize_reducing_the_columns_trims_the_data_in_the_buffer()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(InitCols / 2, InitRows);
        Assert.Equal(InitRows, _buffer.Lines.Length);
        for (int i = 0; i < InitRows; i++)
        {
            Assert.Equal(InitCols / 2, _buffer.Lines.Get(i)!.Length);
        }
    }

    // xterm.js: 'Buffer' › 'resize' › 'column size is increased' › 'should add pad columns'
    [Fact]
    public void Resize_increasing_the_columns_adds_pad_columns()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(InitCols + 10, InitRows);
        Assert.Equal(InitRows, _buffer.Lines.Length);
        for (int i = 0; i < InitRows; i++)
        {
            Assert.Equal(InitCols + 10, _buffer.Lines.Get(i)!.Length);
        }
    }

    // xterm.js: 'Buffer' › 'resize' › 'row size reduced' › 'should trim blank lines from the end'
    [Fact]
    public void Resize_reducing_the_rows_trims_blank_lines_from_the_end()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(InitCols, InitRows - 10);
        Assert.Equal(InitRows - 10, _buffer.Lines.Length);
    }

    // xterm.js: 'Buffer' › 'resize' › 'row size reduced' › 'should move the viewport down when it's at the end'
    [Fact]
    public void Resize_reducing_the_rows_moves_the_viewport_down_when_it_is_at_the_end()
    {
        _buffer.FillViewportRows();

        // Set cursor y to have 5 blank lines below it
        _buffer.Y = InitRows - 5 - 1;
        _buffer.Resize(InitCols, InitRows - 10);

        // Trim 5 rows
        Assert.Equal(InitRows - 5, _buffer.Lines.Length);

        // Shift the viewport down 5 rows
        Assert.Equal(5, _buffer.YDisp);
        Assert.Equal(5, _buffer.YBase);
    }

    // xterm.js: 'Buffer' › 'resize' › 'row size reduced' › 'no scrollback' › 'should trim from the top of the buffer when the cursor reaches the bottom'
    [Fact]
    public void Resize_without_scrollback_trims_from_the_top_when_the_cursor_reaches_the_bottom()
    {
        using MockOptionsService options = new(TerminalOptions.Default with { Scrollback = 0 });
        _buffer = new LoomTerminal.Core.Buffer.Buffer(true, options, _bufferService);
        Assert.Equal(InitRows, _buffer.Lines.MaxLength);
        _buffer.Y = InitRows - 1;
        _buffer.FillViewportRows();
        CharData chData = _buffer.Lines.Get(5)!.LoadCell(0, new CellData()).GetAsCharData();
        _buffer.Lines.Get(5)!.SetCell(0, CellData.FromCharData(chData with { Char = "a" }));
        chData = _buffer.Lines.Get(InitRows - 1)!.LoadCell(0, new CellData()).GetAsCharData();
        _buffer.Lines.Get(InitRows - 1)!.SetCell(0, CellData.FromCharData(chData with { Char = "b" }));
        _buffer.Resize(InitCols, InitRows - 5);
        Assert.Equal("a", _buffer.Lines.Get(0)!.LoadCell(0, new CellData()).GetAsCharData().Char);
        Assert.Equal("b", _buffer.Lines.Get(InitRows - 1 - 5)!.LoadCell(0, new CellData()).GetAsCharData().Char);
    }

    // xterm.js: 'Buffer' › 'resize' › 'row size increased' › 'empty buffer' › 'should add blank lines to end'
    [Fact]
    public void Resize_increasing_the_rows_of_an_empty_buffer_adds_blank_lines_to_the_end()
    {
        _buffer.FillViewportRows();
        Assert.Equal(0, _buffer.YDisp);
        _buffer.Resize(InitCols, InitRows + 10);
        Assert.Equal(0, _buffer.YDisp);
        Assert.Equal(InitRows + 10, _buffer.Lines.Length);
    }

    // xterm.js: 'Buffer' › 'resize' › 'row size increased' › 'filled buffer' › 'should show more of the buffer above'
    [Fact]
    public void Resize_increasing_the_rows_of_a_filled_buffer_shows_more_of_the_buffer_above()
    {
        _buffer.FillViewportRows();

        // Create 10 extra blank lines
        for (int i = 0; i < 10; i++)
        {
            _buffer.Lines.Push(_buffer.GetBlankLine(AttributeData.DefaultAttrData));
        }

        // Set cursor to the bottom of the buffer
        _buffer.Y = InitRows - 1;

        // Scroll down 10 lines
        _buffer.YBase = 10;
        _buffer.YDisp = 10;
        Assert.Equal(InitRows + 10, _buffer.Lines.Length);
        _buffer.Resize(InitCols, InitRows + 5);

        // Should be should 5 more lines
        Assert.Equal(5, _buffer.YDisp);
        Assert.Equal(5, _buffer.YBase);

        // Should not trim the buffer
        Assert.Equal(InitRows + 10, _buffer.Lines.Length);
    }

    // xterm.js: 'Buffer' › 'resize' › 'row size increased' › 'filled buffer' › 'should show more of the buffer below when the viewport is at the top of the buffer'
    [Fact]
    public void Resize_increasing_the_rows_shows_more_of_the_buffer_below_when_the_viewport_is_at_the_top()
    {
        _buffer.FillViewportRows();

        // Create 10 extra blank lines
        for (int i = 0; i < 10; i++)
        {
            _buffer.Lines.Push(_buffer.GetBlankLine(AttributeData.DefaultAttrData));
        }

        // Set cursor to the bottom of the buffer
        _buffer.Y = InitRows - 1;

        // Scroll down 10 lines
        _buffer.YBase = 10;
        _buffer.YDisp = 0;
        Assert.Equal(InitRows + 10, _buffer.Lines.Length);
        _buffer.Resize(InitCols, InitRows + 5);

        // The viewport should remain at the top
        Assert.Equal(0, _buffer.YDisp);

        // The buffer ybase should move up 5 lines
        Assert.Equal(5, _buffer.YBase);

        // Should not trim the buffer
        Assert.Equal(InitRows + 10, _buffer.Lines.Length);
    }

    // xterm.js: 'Buffer' › 'resize' › 'row and column increased' › 'should resize properly'
    [Fact]
    public void Resize_increasing_rows_and_columns_resizes_properly()
    {
        _buffer.FillViewportRows();
        _buffer.Resize(InitCols + 5, InitRows + 5);
        Assert.Equal(InitRows + 5, _buffer.Lines.Length);
        for (int i = 0; i < InitRows + 5; i++)
        {
            Assert.Equal(InitCols + 5, _buffer.Lines.Get(i)!.Length);
        }
    }

    // xterm.js: 'Buffer' › 'buffer marked to have no scrollback' › 'should always have a scrollback of 0'
    [Fact]
    public void A_buffer_marked_to_have_no_scrollback_always_has_a_scrollback_of_0()
    {
        // Test size on initialization
        using MockOptionsService options = new(TerminalOptions.Default with { Scrollback = 1000 });
        _buffer = new LoomTerminal.Core.Buffer.Buffer(false, options, _bufferService);
        _buffer.FillViewportRows();
        Assert.Equal(InitRows, _buffer.Lines.MaxLength);

        // Test size on buffer increase
        _buffer.Resize(InitCols, InitRows * 2);
        Assert.Equal(InitRows * 2, _buffer.Lines.MaxLength);

        // Test size on buffer decrease
        _buffer.Resize(InitCols, InitRows / 2);
        Assert.Equal(InitRows / 2, _buffer.Lines.MaxLength);
    }

    // xterm.js: 'Buffer' › 'memory cleanup after shrinking' › 'should realign memory from idle task execution'
    [Fact]
    public void Memory_is_realigned_by_the_batched_cleanup_after_shrinking()
    {
        _buffer.FillViewportRows();

        // shrink more than 2 times to trigger lazy memory cleanup
        _buffer.Resize((InitCols / 2) - 1, InitRows);
        Assert.True(_buffer.MemoryCleanupPending);

        for (int i = 0; i < InitRows; i++)
        {
            BufferLine line = (BufferLine)_buffer.Lines.Get(i)!;

            // line memory is still at old size from initialization
            Assert.Equal(InitCols, line.Capacity);

            // the length gets immediately adjusted
            Assert.Equal((InitCols / 2) - 1, line.Length);
        }

        // the session calls the cleanup when the parser thread is idle, until it reports it is done
        while (_buffer.BatchedMemoryCleanup())
        {
        }

        Assert.False(_buffer.MemoryCleanupPending);

        // cleanup should have realigned memory with the exact size
        for (int i = 0; i < InitRows; i++)
        {
            Assert.Equal((InitCols / 2) - 1, ((BufferLine)_buffer.Lines.Get(i)!).Capacity);
        }
    }

    [Fact]
    public void Resize_clamps_the_cursor_and_the_saved_cursor_to_the_new_size()
    {
        _buffer.FillViewportRows();
        _buffer.X = 70;
        _buffer.Y = 20;
        _buffer.SavedX = 75;
        _buffer.ScrollTop = 3;
        _buffer.Resize(40, 10);
        Assert.True(_buffer.X <= 39);
        Assert.True(_buffer.Y <= 9);
        Assert.Equal(39, _buffer.SavedX);
        Assert.Equal(0, _buffer.ScrollTop);
        Assert.Equal(9, _buffer.ScrollBottom);
    }

    [Fact]
    public void Resize_on_a_windows_pty_adds_blank_lines_instead_of_scrolling_up()
    {
        using MockOptionsService options = new(TerminalOptions.Default with { WindowsMode = true });
        LoomTerminal.Core.Buffer.Buffer buffer = new(true, options, _bufferService);
        buffer.FillViewportRows();
        for (int i = 0; i < 5; i++)
        {
            buffer.Lines.Push(buffer.GetBlankLine(AttributeData.DefaultAttrData));
        }

        buffer.YBase = 5;
        buffer.YDisp = 5;
        buffer.Y = InitRows - 1;
        buffer.Resize(InitCols, InitRows + 3);
        Assert.Equal(5, buffer.YBase);
        Assert.Equal(InitRows + 8, buffer.Lines.Length);
    }
}
