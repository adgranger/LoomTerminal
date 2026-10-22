using LoomTerminal.Core;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Input;
using CoreWheelDeltaMode = LoomTerminal.Core.Services.WheelDeltaMode;

namespace LoomTerminal.Presentation.Viewport;

/// <summary>
/// Decides what a wheel event does (logic of the <c>wheel</c> handlers of xterm.js <c>CoreBrowserTerminal.bindMouse</c>):
/// report it to an application that tracks the mouse, turn it into an arrow key in a buffer without scrollback, or
/// scroll the viewport.
/// </summary>
/// <remarks>Threading: parser thread only: it reads and changes the emulator.</remarks>
internal sealed class ViewportController : IDisposable
{
    private readonly TerminalCore _core;
    private readonly IDisposable _protocolSubscription;
    private CoreMouseEventType _requestedEvents;

    /// <summary>Creates a controller and starts following the mouse protocol of the application.</summary>
    /// <param name="core">The emulator.</param>
    internal ViewportController(TerminalCore core)
    {
        _core = core;
        _protocolSubscription = core.CoreMouseService.OnProtocolChange.Subscribe(events => _requestedEvents = events);
    }

    /// <summary>Handles a wheel event.</summary>
    /// <param name="ev">The wheel event.</param>
    /// <param name="cellHeight">Height of a cell in device pixels, to turn pixel deltas into lines.</param>
    internal void HandleWheel(TerminalPointerEvent ev, double cellHeight)
    {
        if (ev.WheelDeltaY == 0)
        {
            return;
        }

        var mouse = _core.CoreMouseService;
        if (mouse.AreMouseEventsActive && (_requestedEvents & CoreMouseEventType.Wheel) != 0 && !ev.Shift)
        {
            // The application handles the wheel itself.
            ReportWheel(ev, cellHeight);
            return;
        }

        var lines = mouse.ConsumeWheelEvent(ev.WheelDeltaY, (CoreWheelDeltaMode)(int)ev.DeltaMode, ev.Shift, ev.Alt, ev.Ctrl, cellHeight, 1);
        if (lines == 0)
        {
            return;
        }

        if (!_core.Buffers.Active.HasScrollback)
        {
            // Convert wheel events into up/down events when the buffer does not have scrollback, this enables scrolling
            // in apps hosted in the alt buffer such as vim or tmux even when mouse events are not enabled. As in
            // xterm.js, a single up or down sequence is sent, whatever the number of lines.
            var application = _core.CoreService.DecPrivateModes.ApplicationCursorKeys;
            _core.Input("\u001b" + (application ? "O" : "[") + (ev.WheelDeltaY < 0 ? "A" : "B"));
            return;
        }

        _core.ScrollLines(lines);
    }

    /// <summary>Stops following the mouse protocol.</summary>
    public void Dispose() => _protocolSubscription.Dispose();

    private void ReportWheel(TerminalPointerEvent ev, double cellHeight)
    {
        var lines = _core.CoreMouseService.ConsumeWheelEvent(ev.WheelDeltaY, (CoreWheelDeltaMode)(int)ev.DeltaMode, ev.Shift, ev.Alt, ev.Ctrl, cellHeight, 1);
        if (lines == 0)
        {
            return;
        }

        _core.CoreMouseService.TriggerMouseEvent(new CoreMouseEvent
        {
            Col = ev.Column,
            Row = ev.Row,
            X = ev.X,
            Y = ev.Y,
            Button = CoreMouseButton.Wheel,
            Action = ev.WheelDeltaY < 0 ? CoreMouseAction.Up : CoreMouseAction.Down,
            Ctrl = ev.Ctrl,
            Alt = ev.Alt,
            Shift = ev.Shift,
        });
    }
}
