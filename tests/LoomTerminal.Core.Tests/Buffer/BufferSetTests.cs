// Ported from xtermjs/xterm.js: src/common/buffer/BufferSet.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Services;
using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.Buffer;

public sealed class BufferSetTests : IDisposable
{
    private readonly MockOptionsService _options = new(TerminalOptions.Default with { Scrollback = 1000 });
    private readonly MockBufferService _bufferService;
    private readonly BufferSet _bufferSet;

    public BufferSetTests()
    {
        _bufferService = new MockBufferService(80, 24);
        _bufferSet = new BufferSet(_options, _bufferService);
    }

    public void Dispose()
    {
        _bufferSet.Dispose();
        _bufferService.Dispose();
        _options.Dispose();
    }

    // xterm.js: 'BufferSet' › 'constructor' › 'should create two different buffers: alt and normal'
    [Fact]
    public void Constructor_creates_two_different_buffers_alt_and_normal()
    {
        Assert.NotNull(_bufferSet.Normal);
        Assert.NotNull(_bufferSet.Alt);
        Assert.NotSame(_bufferSet.Normal, _bufferSet.Alt);
    }

    // xterm.js: 'BufferSet' › 'activateNormalBuffer' › 'should set the normal buffer as the currently active buffer'
    [Fact]
    public void ActivateNormalBuffer_sets_the_normal_buffer_as_the_active_buffer()
    {
        _bufferSet.ActivateNormalBuffer();
        Assert.Same(_bufferSet.Normal, _bufferSet.Active);
    }

    // xterm.js: 'BufferSet' › 'activateAltBuffer' › 'should set the alt buffer as the currently active buffer'
    [Fact]
    public void ActivateAltBuffer_sets_the_alt_buffer_as_the_active_buffer()
    {
        _bufferSet.ActivateAltBuffer();
        Assert.Same(_bufferSet.Alt, _bufferSet.Active);
    }

    private void ResetCursors()
    {
        _bufferSet.Normal.X = 0;
        _bufferSet.Normal.Y = 0;
        _bufferSet.Alt.X = 0;
        _bufferSet.Alt.Y = 0;
    }

    // xterm.js: 'BufferSet' › 'cursor handling when swapping buffers' › 'should keep the cursor stationary when activating alt buffer'
    [Fact]
    public void Activating_the_alt_buffer_keeps_the_cursor_stationary()
    {
        ResetCursors();
        _bufferSet.ActivateNormalBuffer();
        _bufferSet.Active.X = 30;
        _bufferSet.Active.Y = 10;
        _bufferSet.ActivateAltBuffer();
        Assert.Equal(30, _bufferSet.Active.X);
        Assert.Equal(10, _bufferSet.Active.Y);
    }

    // xterm.js: 'BufferSet' › 'cursor handling when swapping buffers' › 'should keep the cursor stationary when activating normal buffer'
    [Fact]
    public void Activating_the_normal_buffer_keeps_the_cursor_stationary()
    {
        ResetCursors();
        _bufferSet.ActivateAltBuffer();
        _bufferSet.Active.X = 30;
        _bufferSet.Active.Y = 10;
        _bufferSet.ActivateNormalBuffer();
        Assert.Equal(30, _bufferSet.Active.X);
        Assert.Equal(10, _bufferSet.Active.Y);
    }

    // xterm.js: 'BufferSet' › 'markers' › 'should clear the markers when the buffer is switched'
    [Fact]
    public void Switching_the_buffer_clears_the_markers_of_the_alt_buffer()
    {
        _bufferSet.ActivateAltBuffer();
        _bufferSet.Alt.AddMarker(1);
        Assert.Single(_bufferSet.Alt.Markers);
        _bufferSet.ActivateNormalBuffer();
        Assert.Empty(_bufferSet.Alt.Markers);
    }

    [Fact]
    public void OnBufferActivate_announces_the_active_and_the_inactive_buffer()
    {
        List<BufferActivateEvent> events = [];
        _bufferSet.OnBufferActivate.Subscribe(events.Add);
        _bufferSet.ActivateAltBuffer();
        _bufferSet.ActivateAltBuffer();
        _bufferSet.ActivateNormalBuffer();
        Assert.Equal(2, events.Count);
        Assert.Same(_bufferSet.Alt, events[0].ActiveBuffer);
        Assert.Same(_bufferSet.Normal, events[0].InactiveBuffer);
        Assert.Same(_bufferSet.Normal, events[1].ActiveBuffer);
    }

    [Fact]
    public void Changing_the_scrollback_or_tab_stop_width_option_updates_both_buffers()
    {
        _options.RawOptions = _options.RawOptions with { TabStopWidth = 4 };
        Assert.True(_bufferSet.Normal.Tabs[4]);
        Assert.True(_bufferSet.Alt.Tabs[4]);

        _options.RawOptions = _options.RawOptions with { Scrollback = 10 };
        Assert.Equal(24 + 10, _bufferSet.Normal.Lines.MaxLength);
        Assert.Equal(24, _bufferSet.Alt.Lines.MaxLength);
    }
}
