// Ported from xtermjs/xterm.js: src/common/TestUtils.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.TestUtils;

/// <summary>A mouse service that reports nothing, like the xterm.js mock.</summary>
/// <remarks>Threading: not thread-safe; used from a single test thread.</remarks>
internal sealed class MockCoreMouseService : ICoreMouseService, IDisposable
{
    private readonly Emitter<CoreMouseEventType> _onProtocolChange = new();

    public string ActiveProtocol { get; set; } = string.Empty;

    public string ActiveEncoding { get; set; } = string.Empty;

    public bool AreMouseEventsActive { get; set; }

    public IEvent<CoreMouseEventType> OnProtocolChange => _onProtocolChange.Event;

    public void AddProtocol(string name, CoreMouseProtocol protocol)
    {
    }

    public void AddEncoding(string name, CoreMouseEncoding encoding)
    {
    }

    public void Reset()
    {
    }

    public bool TriggerMouseEvent(CoreMouseEvent @event) => false;

    public Dictionary<string, bool> ExplainEvents(CoreMouseEventType events) =>
        throw new NotImplementedException("Method not implemented.");

    public int ConsumeWheelEvent(double deltaY, WheelDeltaMode mode, bool shift, bool alt, bool ctrl, double cellHeight, double dpr) => 1;

    public void Dispose() => _onProtocolChange.Dispose();
}
