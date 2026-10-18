// Ported from xtermjs/xterm.js: src/common/services/Services.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Services;

/// <summary>Mouse tracking: protocols, encodings and the reports sent to the application.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface ICoreMouseService
{
    /// <summary>The name of the active protocol. Setting an unknown name throws.</summary>
    string ActiveProtocol { get; set; }

    /// <summary>The name of the active encoding. Setting an unknown name throws.</summary>
    string ActiveEncoding { get; set; }

    /// <summary>Whether the active protocol wants any mouse event.</summary>
    bool AreMouseEventsActive { get; }

    /// <summary>Registers (or replaces) a protocol.</summary>
    void AddProtocol(string name, CoreMouseProtocol protocol);

    /// <summary>Registers (or replaces) an encoding.</summary>
    void AddEncoding(string name, CoreMouseEncoding encoding);

    /// <summary>Activates the NONE protocol and the DEFAULT encoding.</summary>
    void Reset();

    /// <summary>
    /// Triggers a mouse event to be sent. Returns true if the event passed all protocol restrictions and a report was
    /// sent, otherwise false; the UI may use it to decide whether to omit its default action. The values of
    /// <paramref name="event"/> are changed to fulfil protocol and encoding restrictions.
    /// </summary>
    bool TriggerMouseEvent(CoreMouseEvent @event);

    /// <summary>Raised with the events the protocol needs when the active protocol is set.</summary>
    IEvent<CoreMouseEventType> OnProtocolChange { get; }

    /// <summary>A human readable version of mouse events: <c>down</c>, <c>up</c>, <c>drag</c>, <c>move</c>, <c>wheel</c>.</summary>
    Dictionary<string, bool> ExplainEvents(CoreMouseEventType events);

    /// <summary>
    /// Processes a wheel event, taking partial scrolls into account. Returns the number of lines to scroll (negative
    /// is up).
    /// </summary>
    int ConsumeWheelEvent(double deltaY, WheelDeltaMode mode, bool shift, bool alt, bool ctrl, double cellHeight, double dpr);
}
