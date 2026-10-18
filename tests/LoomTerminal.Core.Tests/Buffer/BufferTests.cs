// Ported from xtermjs/xterm.js: src/common/buffer/Buffer.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Services;
using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.Buffer;

public sealed partial class BufferTests : IDisposable
{
    private const int InitCols = 80;
    private const int InitRows = 24;
    private const int InitScrollback = 1000;

    private readonly MockOptionsService _optionsService;
    private readonly MockBufferService _bufferService;
    private LoomTerminal.Core.Buffer.Buffer _buffer;

    public BufferTests()
    {
        _optionsService = new MockOptionsService(TerminalOptions.Default with { Scrollback = InitScrollback });
        _bufferService = new MockBufferService(InitCols, InitRows);
        _buffer = new LoomTerminal.Core.Buffer.Buffer(true, _optionsService, _bufferService);
    }

    public void Dispose()
    {
        _bufferService.Dispose();
        _optionsService.Dispose();
    }

    private static CharData Cd(string chars, int width, uint code = 0) => new(0, chars, width, code);

    // xterm.js: 'Buffer' › 'constructor' › 'should create a CircularList with max length equal to rows + scrollback, for its lines'
    [Fact]
    public void Constructor_creates_lines_with_a_max_length_of_rows_plus_scrollback() =>
        Assert.Equal(_bufferService.Rows + InitScrollback, _buffer.Lines.MaxLength);

    // xterm.js: 'Buffer' › 'constructor' › 'should set the Buffer's scrollBottom value equal to the terminal's rows -1'
    [Fact]
    public void Constructor_sets_scrollBottom_to_rows_minus_one() =>
        Assert.Equal(_bufferService.Rows - 1, _buffer.ScrollBottom);

    // xterm.js: 'Buffer' › 'fillViewportRows' › 'should fill the buffer with blank lines based on the size of the viewport'
    [Fact]
    public void FillViewportRows_fills_the_buffer_with_blank_lines_based_on_the_size_of_the_viewport()
    {
        CharData blankLineChar = _buffer.GetBlankLine(AttributeData.DefaultAttrData).LoadCell(0, new CellData()).GetAsCharData();
        _buffer.FillViewportRows();
        Assert.Equal(InitRows, _buffer.Lines.Length);
        for (int y = 0; y < InitRows; y++)
        {
            Assert.Equal(InitCols, _buffer.Lines.Get(y)!.Length);
            for (int x = 0; x < InitCols; x++)
            {
                Assert.Equal(blankLineChar, _buffer.Lines.Get(y)!.LoadCell(x, new CellData()).GetAsCharData());
            }
        }
    }

    // xterm.js: 'Buffer' › 'getWrappedRangeForLine' › 'non-wrapped' › 'should return a single row for the first row'
    [Fact]
    public void GetWrappedRangeForLine_returns_a_single_row_for_the_first_row()
    {
        _buffer.FillViewportRows();
        Assert.Equal((0, 0), _buffer.GetWrappedRangeForLine(0));
    }

    // xterm.js: 'Buffer' › 'getWrappedRangeForLine' › 'non-wrapped' › 'should return a single row for a middle row'
    [Fact]
    public void GetWrappedRangeForLine_returns_a_single_row_for_a_middle_row()
    {
        _buffer.FillViewportRows();
        Assert.Equal((12, 12), _buffer.GetWrappedRangeForLine(12));
    }

    // xterm.js: 'Buffer' › 'getWrappedRangeForLine' › 'non-wrapped' › 'should return a single row for the last row'
    [Fact]
    public void GetWrappedRangeForLine_returns_a_single_row_for_the_last_row()
    {
        _buffer.FillViewportRows();
        Assert.Equal((23, 23), _buffer.GetWrappedRangeForLine(_buffer.Lines.Length - 1));
    }

    // xterm.js: 'Buffer' › 'getWrappedRangeForLine' › 'wrapped' › 'should return a range for the first row'
    [Fact]
    public void GetWrappedRangeForLine_returns_a_range_for_the_first_row()
    {
        _buffer.FillViewportRows();
        _buffer.Lines.Get(1)!.IsWrapped = true;
        Assert.Equal((0, 1), _buffer.GetWrappedRangeForLine(0));
    }

    // xterm.js: 'Buffer' › 'getWrappedRangeForLine' › 'wrapped' › 'should return a range for a middle row wrapping upwards'
    [Fact]
    public void GetWrappedRangeForLine_returns_a_range_for_a_middle_row_wrapping_upwards()
    {
        _buffer.FillViewportRows();
        _buffer.Lines.Get(12)!.IsWrapped = true;
        Assert.Equal((11, 12), _buffer.GetWrappedRangeForLine(12));
    }

    // xterm.js: 'Buffer' › 'getWrappedRangeForLine' › 'wrapped' › 'should return a range for a middle row wrapping downwards'
    [Fact]
    public void GetWrappedRangeForLine_returns_a_range_for_a_middle_row_wrapping_downwards()
    {
        _buffer.FillViewportRows();
        _buffer.Lines.Get(13)!.IsWrapped = true;
        Assert.Equal((12, 13), _buffer.GetWrappedRangeForLine(12));
    }

    // xterm.js: 'Buffer' › 'getWrappedRangeForLine' › 'wrapped' › 'should return a range for a middle row wrapping both ways'
    [Fact]
    public void GetWrappedRangeForLine_returns_a_range_for_a_middle_row_wrapping_both_ways()
    {
        _buffer.FillViewportRows();
        _buffer.Lines.Get(11)!.IsWrapped = true;
        _buffer.Lines.Get(12)!.IsWrapped = true;
        _buffer.Lines.Get(13)!.IsWrapped = true;
        _buffer.Lines.Get(14)!.IsWrapped = true;
        Assert.Equal((10, 14), _buffer.GetWrappedRangeForLine(12));
    }

    // xterm.js: 'Buffer' › 'getWrappedRangeForLine' › 'wrapped' › 'should return a range for the last row'
    [Fact]
    public void GetWrappedRangeForLine_returns_a_range_for_the_last_row()
    {
        _buffer.FillViewportRows();
        _buffer.Lines.Get(23)!.IsWrapped = true;
        Assert.Equal((22, 23), _buffer.GetWrappedRangeForLine(_buffer.Lines.Length - 1));
    }

    // xterm.js: 'Buffer' › 'getWrappedRangeForLine' › 'wrapped' › 'should return a range for a row that wraps upward to first row'
    [Fact]
    public void GetWrappedRangeForLine_returns_a_range_for_a_row_that_wraps_upward_to_the_first_row()
    {
        _buffer.FillViewportRows();
        _buffer.Lines.Get(1)!.IsWrapped = true;
        Assert.Equal((0, 1), _buffer.GetWrappedRangeForLine(1));
    }

    // xterm.js: 'Buffer' › 'getWrappedRangeForLine' › 'wrapped' › 'should return a range for a row that wraps downward to last row'
    [Fact]
    public void GetWrappedRangeForLine_returns_a_range_for_a_row_that_wraps_downward_to_the_last_row()
    {
        _buffer.FillViewportRows();
        _buffer.Lines.Get(_buffer.Lines.Length - 1)!.IsWrapped = true;
        Assert.Equal((22, 23), _buffer.GetWrappedRangeForLine(_buffer.Lines.Length - 2));
    }

    private LoomTerminal.Core.Buffer.Buffer NoScrollbackBuffer()
    {
        using MockOptionsService options = new(TerminalOptions.Default with { Scrollback = 0 });
        LoomTerminal.Core.Buffer.Buffer buffer = new(true, options, _bufferService);
        buffer.FillViewportRows();
        return buffer;
    }

    // xterm.js: 'Buffer' › 'addMarker' › 'should adjust a marker line when the buffer is trimmed'
    [Fact]
    public void AddMarker_adjusts_a_marker_line_when_the_buffer_is_trimmed()
    {
        _buffer = NoScrollbackBuffer();
        Marker marker = _buffer.AddMarker(_buffer.Lines.Length - 1);
        Assert.Equal(_buffer.Lines.Length - 1, marker.Line);
        _buffer.Lines.OnTrimEmitter.Fire(1);
        Assert.Equal(_buffer.Lines.Length - 2, marker.Line);
    }

    // xterm.js: 'Buffer' › 'addMarker' › 'should dispose of a marker if it is trimmed off the buffer'
    [Fact]
    public void AddMarker_disposes_a_marker_trimmed_off_the_buffer()
    {
        _buffer = NoScrollbackBuffer();
        Assert.Empty(_buffer.Markers);
        Marker marker = _buffer.AddMarker(0);
        Assert.False(marker.IsDisposed);
        Assert.Single(_buffer.Markers);
        _buffer.Lines.OnTrimEmitter.Fire(1);
        Assert.True(marker.IsDisposed);
        Assert.Empty(_buffer.Markers);
    }

    // xterm.js: 'Buffer' › 'addMarker' › 'should call onDispose'
    [Fact]
    public void AddMarker_calls_OnDispose()
    {
        List<string> eventStack = [];
        _buffer = NoScrollbackBuffer();
        Assert.Empty(_buffer.Markers);
        Marker marker = _buffer.AddMarker(0);
        marker.OnDispose.Subscribe(_ => eventStack.Add("disposed"));
        Assert.False(marker.IsDisposed);
        Assert.Single(_buffer.Markers);
        _buffer.Lines.OnTrimEmitter.Fire(1);
        Assert.True(marker.IsDisposed);
        Assert.Empty(_buffer.Markers);
        Assert.Equal(["disposed"], eventStack);
    }

    // xterm.js: 'Buffer' › 'translateBufferLineToString' › 'should handle selecting a section of ascii text'
    [Fact]
    public void TranslateBufferLineToString_handles_selecting_a_section_of_ascii_text()
    {
        BufferLine line = new(4);
        line.SetCell(0, CellData.FromCharData(Cd("a", 1, 'a')));
        line.SetCell(1, CellData.FromCharData(Cd("b", 1, 'b')));
        line.SetCell(2, CellData.FromCharData(Cd("c", 1, 'c')));
        line.SetCell(3, CellData.FromCharData(Cd("d", 1, 'd')));
        _buffer.Lines.Set(0, line);

        Assert.Equal("ab", _buffer.TranslateBufferLineToString(0, true, 0, 2));
    }

    // xterm.js: 'Buffer' › 'translateBufferLineToString' › 'should handle a cut-off double width character by including it'
    [Fact]
    public void TranslateBufferLineToString_includes_a_cut_off_double_width_character()
    {
        BufferLine line = new(3);
        line.SetCell(0, CellData.FromCharData(Cd("語", 2, 35486)));
        line.SetCell(1, CellData.FromCharData(Cd(string.Empty, 0)));
        line.SetCell(2, CellData.FromCharData(Cd("a", 1, 'a')));
        _buffer.Lines.Set(0, line);

        Assert.Equal("語", _buffer.TranslateBufferLineToString(0, true, 0, 1));
    }

    // xterm.js: 'Buffer' › 'translateBufferLineToString' › 'should handle a zero width character in the middle of the string by not including it'
    [Fact]
    public void TranslateBufferLineToString_does_not_include_a_zero_width_character_in_the_middle()
    {
        BufferLine line = new(3);
        line.SetCell(0, CellData.FromCharData(Cd("語", 2, '語')));
        line.SetCell(1, CellData.FromCharData(Cd(string.Empty, 0)));
        line.SetCell(2, CellData.FromCharData(Cd("a", 1, 'a')));
        _buffer.Lines.Set(0, line);

        Assert.Equal("語", _buffer.TranslateBufferLineToString(0, true, 0, 1));
        Assert.Equal("語", _buffer.TranslateBufferLineToString(0, true, 0, 2));
        Assert.Equal("語a", _buffer.TranslateBufferLineToString(0, true, 0, 3));
    }

    // xterm.js: 'Buffer' › 'translateBufferLineToString' › 'should handle single width emojis'
    [Fact]
    public void TranslateBufferLineToString_handles_single_width_emojis()
    {
        BufferLine line = new(2);
        line.SetCell(0, CellData.FromCharData(Cd("😁", 1, '\uD83D')));
        line.SetCell(1, CellData.FromCharData(Cd("a", 1, 'a')));
        _buffer.Lines.Set(0, line);

        Assert.Equal("😁", _buffer.TranslateBufferLineToString(0, true, 0, 1));
        Assert.Equal("😁a", _buffer.TranslateBufferLineToString(0, true, 0, 2));
    }

    // xterm.js: 'Buffer' › 'translateBufferLineToString' › 'should handle double width emojis'
    [Fact]
    public void TranslateBufferLineToString_handles_double_width_emojis()
    {
        BufferLine line = new(2);
        line.SetCell(0, CellData.FromCharData(Cd("😁", 2, '\uD83D')));
        line.SetCell(1, CellData.FromCharData(Cd(string.Empty, 0)));
        _buffer.Lines.Set(0, line);

        Assert.Equal("😁", _buffer.TranslateBufferLineToString(0, true, 0, 1));
        Assert.Equal("😁", _buffer.TranslateBufferLineToString(0, true, 0, 2));

        BufferLine line2 = new(3);
        line2.SetCell(0, CellData.FromCharData(Cd("😁", 2, '\uD83D')));
        line2.SetCell(1, CellData.FromCharData(Cd(string.Empty, 0)));
        line2.SetCell(2, CellData.FromCharData(Cd("a", 1, 'a')));
        _buffer.Lines.Set(0, line2);

        Assert.Equal("😁a", _buffer.TranslateBufferLineToString(0, true, 0, 3));
    }

    [Fact]
    public void Tab_stops_are_set_every_tab_stop_width_and_navigated_by_NextStop_and_PrevStop()
    {
        Assert.True(_buffer.Tabs[0]);
        Assert.True(_buffer.Tabs[8]);
        Assert.False(_buffer.Tabs.ContainsKey(7));
        Assert.Equal(8, _buffer.NextStop(0));
        Assert.Equal(16, _buffer.NextStop(8));
        Assert.Equal(InitCols - 1, _buffer.NextStop(InitCols - 2));
        Assert.Equal(8, _buffer.PrevStop(15));
        Assert.Equal(0, _buffer.PrevStop(3));
        _buffer.X = 20;
        Assert.Equal(24, _buffer.NextStop());
        Assert.Equal(16, _buffer.PrevStop());
    }

    [Fact]
    public void ClearMarkers_and_ClearAllMarkers_dispose_the_markers()
    {
        Marker a = _buffer.AddMarker(1);
        Marker b = _buffer.AddMarker(2);
        Marker c = _buffer.AddMarker(2);
        _buffer.ClearMarkers(2);
        Assert.False(a.IsDisposed);
        Assert.True(b.IsDisposed);
        Assert.True(c.IsDisposed);
        Assert.Equal([a], _buffer.Markers);
        _buffer.ClearAllMarkers();
        Assert.True(a.IsDisposed);
        Assert.Empty(_buffer.Markers);
    }

    [Fact]
    public void Clear_resets_the_cursor_scroll_region_and_lines()
    {
        _buffer.FillViewportRows();
        _buffer.X = 5;
        _buffer.Y = 3;
        _buffer.YBase = 2;
        _buffer.YDisp = 2;
        _buffer.ScrollTop = 4;
        _buffer.Clear();
        Assert.Equal(0, _buffer.Lines.Length);
        Assert.Equal((0, 0, 0, 0), (_buffer.X, _buffer.Y, _buffer.YBase, _buffer.YDisp));
        Assert.Equal((0, InitRows - 1), (_buffer.ScrollTop, _buffer.ScrollBottom));
    }
}
