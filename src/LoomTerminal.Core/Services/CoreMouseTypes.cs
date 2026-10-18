// Ported from xtermjs/xterm.js: src/common/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Services;

/// <summary>The button of a mouse event. Wheel is treated as a button.</summary>
internal enum CoreMouseButton
{
    /// <summary>The left button.</summary>
    Left = 0,

    /// <summary>The middle button.</summary>
    Middle = 1,

    /// <summary>The right button.</summary>
    Right = 2,

    /// <summary>No button (moves without a button).</summary>
    None = 3,

    /// <summary>The wheel.</summary>
    Wheel = 4,

    /// <summary>Additional button 1 (untested upstream).</summary>
    Aux1 = 8,

    /// <summary>Additional button 2 (untested upstream).</summary>
    Aux2 = 9,

    /// <summary>Additional button 3 (untested upstream).</summary>
    Aux3 = 10,

    /// <summary>Additional button 4 (untested upstream).</summary>
    Aux4 = 11,

    /// <summary>Additional button 5 (untested upstream).</summary>
    Aux5 = 12,

    /// <summary>Additional button 6 (untested upstream).</summary>
    Aux6 = 13,

    /// <summary>Additional button 7 (untested upstream).</summary>
    Aux7 = 14,

    /// <summary>Additional button 8 (untested upstream).</summary>
    Aux8 = 15,
}

/// <summary>The action of a mouse event.</summary>
internal enum CoreMouseAction
{
    /// <summary>Button or wheel released.</summary>
    Up = 0,

    /// <summary>Button or wheel pressed.</summary>
    Down = 1,

    /// <summary>Wheel only: scrolled left.</summary>
    Left = 2,

    /// <summary>Wheel only: scrolled right.</summary>
    Right = 3,

    /// <summary>Buttons only: the pointer moved.</summary>
    Move = 32,
}

/// <summary>The kinds of mouse events a protocol wants the UI to forward. A bit mask.</summary>
[Flags]
internal enum CoreMouseEventType
{
    /// <summary>No events.</summary>
    None = 0,

    /// <summary>Any mouse down event.</summary>
    Down = 1,

    /// <summary>Any mouse up event.</summary>
    Up = 2,

    /// <summary>Any mouse move event while a button is held.</summary>
    Drag = 4,

    /// <summary>Any mouse move event without a button.</summary>
    Move = 8,

    /// <summary>Any wheel event.</summary>
    Wheel = 16,
}

/// <summary>The unit of the delta of a wheel event (DOM values).</summary>
internal enum WheelDeltaMode
{
    /// <summary>The delta is in pixels.</summary>
    Pixel = 0,

    /// <summary>The delta is in lines.</summary>
    Line = 1,

    /// <summary>The delta is in pages.</summary>
    Page = 2,
}

/// <summary>
/// A mouse event as reported by a UI backend. <see cref="CoreMouseService.TriggerMouseEvent"/> changes the values of
/// the instance it is given to fulfil protocol and encoding restrictions.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the UI thread only.</remarks>
internal sealed record CoreMouseEvent
{
    /// <summary>Column (zero based).</summary>
    public required int Col { get; set; }

    /// <summary>Row (zero based).</summary>
    public required int Row { get; set; }

    /// <summary>Pixel x position.</summary>
    public int X { get; set; }

    /// <summary>Pixel y position.</summary>
    public int Y { get; set; }

    /// <summary>
    /// The button of the action. Due to restrictions of the tracking protocols it is not possible to report several
    /// buttons at once. Invalid button and action combinations are silently ignored by the service.
    /// </summary>
    public required CoreMouseButton Button { get; set; }

    /// <summary>The action.</summary>
    public required CoreMouseAction Action { get; set; }

    /// <summary>Whether Ctrl is held. Protocols add or ignore it based on their restrictions.</summary>
    public bool Ctrl { get; set; }

    /// <summary>Whether Alt is held. Protocols add or ignore it based on their restrictions.</summary>
    public bool Alt { get; set; }

    /// <summary>Whether Shift is held. Protocols add or ignore it based on their restrictions.</summary>
    public bool Shift { get; set; }
}

/// <summary>
/// A mouse protocol. <see cref="Events"/> is a hint for the UI to install or remove its event handlers;
/// <see cref="Restrict"/> applies protocol specific restrictions such as not allowed modifiers.
/// </summary>
/// <param name="Events">The events the protocol needs.</param>
/// <param name="Restrict">Returns false to filter the event out; may change the event.</param>
internal sealed record CoreMouseProtocol(CoreMouseEventType Events, Func<CoreMouseEvent, bool> Restrict);

/// <summary>
/// Encodes an event that passed the protocol restrictions into the text sent to the application. Returning an empty
/// string suppresses the report.
/// </summary>
/// <param name="event">The event, with 1-based coordinates.</param>
internal delegate string CoreMouseEncoding(CoreMouseEvent @event);
