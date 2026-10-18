// Ported from xtermjs/xterm.js: src/common/TestUtils.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Common;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.TestUtils;

/// <summary>A buffer service with a real <see cref="BufferSet"/>; resizing only changes the size, like the xterm.js mock.</summary>
/// <remarks>Threading: not thread-safe; used from a single test thread.</remarks>
internal sealed class MockBufferService : IBufferService, IDisposable
{
    private readonly Emitter<BufferResizeEvent> _onResize = new();
    private readonly Emitter<int> _onScroll = new();
    private readonly MockOptionsService? _ownedOptions;

    public MockBufferService(int cols, int rows, IOptionsService? optionsService = null)
    {
        Cols = cols;
        Rows = rows;
        if (optionsService is null)
        {
            _ownedOptions = new MockOptionsService();
            optionsService = _ownedOptions;
        }

        Buffers = new BufferSet(optionsService, this);

        // Listen to buffer activation events and automatically fire scroll events
        Buffers.OnBufferActivate.Subscribe(e => _onScroll.Fire(e.ActiveBuffer.YDisp));
    }

    public int Cols { get; private set; }

    public int Rows { get; private set; }

    public Core.Buffer.Buffer Buffer => Buffers.Active;

    public BufferSet Buffers { get; }

    public bool IsUserScrolling { get; set; }

    public IEvent<BufferResizeEvent> OnResize => _onResize.Event;

    public IEvent<int> OnScroll => _onScroll.Event;

    public void Scroll(AttributeData eraseAttr, bool isWrapped = false) => throw new NotImplementedException();

    public void ScrollLines(int disp, bool suppressScrollEvent = false) => throw new NotImplementedException();

    public void Resize(int cols, int rows)
    {
        Cols = cols;
        Rows = rows;
    }

    public void Reset()
    {
    }

    public void Dispose()
    {
        Buffers.Dispose();
        _onResize.Dispose();
        _onScroll.Dispose();
        _ownedOptions?.Dispose();
    }
}
