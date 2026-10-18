// Ported from xtermjs/xterm.js: src/common/TestUtils.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.TestUtils;

/// <summary>A core service that sends nothing, like the xterm.js mock.</summary>
/// <remarks>Threading: not thread-safe; used from a single test thread.</remarks>
internal sealed class MockCoreService : ICoreService, IDisposable
{
    private readonly Emitter<string> _onData = new();
    private readonly Emitter<EventArgs> _onUserInput = new();
    private readonly Emitter<string> _onBinary = new();
    private readonly Emitter<EventArgs> _onRequestScrollToBottom = new();

    public bool IsCursorInitialized { get; set; } = true;

    public bool IsCursorHidden { get; set; }

    public Modes Modes { get; } = Modes.CreateDefault();

    public DecPrivateModes DecPrivateModes { get; } = DecPrivateModes.CreateDefault();

    public IEvent<string> OnData => _onData.Event;

    public IEvent<EventArgs> OnUserInput => _onUserInput.Event;

    public IEvent<string> OnBinary => _onBinary.Event;

    public IEvent<EventArgs> OnRequestScrollToBottom => _onRequestScrollToBottom.Event;

    public void Reset()
    {
    }

    /// <summary>Called by <see cref="TriggerDataEvent"/>; the JS tests replace the method the same way.</summary>
    public Action<string, bool>? DataEventHandler { get; set; }

    /// <summary>Called by <see cref="TriggerBinaryEvent"/>.</summary>
    public Action<string>? BinaryEventHandler { get; set; }

    public void TriggerDataEvent(string data, bool wasUserInput = false) => DataEventHandler?.Invoke(data, wasUserInput);

    public void TriggerBinaryEvent(string data) => BinaryEventHandler?.Invoke(data);

    public void Dispose()
    {
        _onData.Dispose();
        _onUserInput.Dispose();
        _onBinary.Dispose();
        _onRequestScrollToBottom.Dispose();
    }
}
