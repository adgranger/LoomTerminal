// Ported from xtermjs/xterm.js: src/browser/services/SelectionService.ts (mouse handling), src/browser/CoreBrowserTerminal.ts (bindMouse)
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Selection;

namespace LoomTerminal.Presentation.Input;

/// <summary>
/// Turns pointer events into selection gestures (click, drag, double click for a word, triple click for a line,
/// Shift+click to extend, Alt for a column block, drag beyond the edges to scroll) or into mouse reports for
/// applications that requested them. The right button is not handled here.
/// </summary>
/// <remarks>Threading: parser thread only: it reads and changes the emulator.</remarks>
internal sealed class PointerInputHandler : IDisposable
{
    // The number of pixels the mouse needs to be above or below the viewport in order to scroll at the maximum speed.
    private const int DragScrollMaxThreshold = 50;

    // The maximum scrolling speed
    private const int DragScrollMaxSpeed = 15;

    private static readonly TimeSpan DragScrollInterval = TimeSpan.FromMilliseconds(50);

    // The maximum amount of time that can have elapsed for an alt click to move the cursor.
    private static readonly TimeSpan AltClickMoveCursorTime = TimeSpan.FromMilliseconds(500);

    private readonly TerminalCore _core;
    private readonly SelectionService _selection;
    private readonly TimeProvider _time;
    private readonly IDisposable _protocolSubscription;
    private CoreMouseEventType _requestedEvents;
    private bool _selecting;
    private bool _reporting;
    private int _dragScrollAmount;
    private long _mouseDownTimestamp;
    private long _lastDragScroll;

    /// <summary>Creates a handler.</summary>
    /// <param name="core">The emulator.</param>
    /// <param name="selection">The selection logic.</param>
    /// <param name="time">The clock, to limit drag scrolling and to time Alt+click.</param>
    internal PointerInputHandler(TerminalCore core, SelectionService selection, TimeProvider time)
    {
        _core = core;
        _selection = selection;
        _time = time;

        // The selection is disabled while the application tracks the mouse (Shift still forces it).
        _protocolSubscription = core.CoreMouseService.OnProtocolChange.Subscribe(events =>
        {
            _requestedEvents = events;
            if (core.CoreMouseService.AreMouseEventsActive)
            {
                selection.Disable();
            }
            else
            {
                selection.Enable();
            }
        });
    }

    /// <summary>Handles a pointer event (wheel events are handled by the viewport controller).</summary>
    /// <param name="e">The event.</param>
    /// <param name="cellHeight">Height of a cell in device pixels, to measure how far a drag left the area.</param>
    internal void Handle(TerminalPointerEvent e, double cellHeight)
    {
        switch (e.Action)
        {
            case PointerAction.Down:
                HandleDown(e);
                break;
            case PointerAction.Up:
                HandleUp(e);
                break;
            case PointerAction.Move:
                HandleMove(e, cellHeight);
                break;
        }
    }

    /// <summary>Stops following the mouse protocol.</summary>
    public void Dispose() => _protocolSubscription.Dispose();

    private void HandleDown(TerminalPointerEvent e)
    {
        // The application gets the mouse unless selection is forced (Shift).
        if (_core.CoreMouseService.AreMouseEventsActive && !_selection.ShouldForceSelection(e))
        {
            if (Report(e, Map(e.Button), CoreMouseAction.Down))
            {
                _reporting = true;
            }

            return;
        }

        _mouseDownTimestamp = _time.GetTimestamp();

        // Only action the primary button
        if (e.Button != PointerButton.Left)
        {
            return;
        }

        _dragScrollAmount = 0;
        if (_selection.IsEnabled && e.Shift)
        {
            HandleIncrementalClick(e);
        }
        else if (e.ClickCount == 1)
        {
            HandleSingleClick(e);
        }
        else if (e.ClickCount == 2)
        {
            HandleDoubleClick(e);
        }
        else if (e.ClickCount == 3)
        {
            HandleTripleClick(e);
        }

        _selecting = true;
    }

    private void HandleUp(TerminalPointerEvent e)
    {
        if (_selecting)
        {
            _selecting = false;
            HandleSelectionUp(e);
        }
        else if (_reporting)
        {
            _reporting = false;
            if ((_requestedEvents & CoreMouseEventType.Up) != 0)
            {
                Report(e, Map(e.Button), CoreMouseAction.Up);
            }
        }
    }

    private void HandleMove(TerminalPointerEvent e, double cellHeight)
    {
        if (_selecting)
        {
            HandleSelectionMove(e, cellHeight);
            return;
        }

        if (!_core.CoreMouseService.AreMouseEventsActive)
        {
            return;
        }

        if (_reporting && (_requestedEvents & CoreMouseEventType.Drag) != 0)
        {
            Report(e, Map(e.Button), CoreMouseAction.Move);
        }
        else if (!_reporting && (_requestedEvents & CoreMouseEventType.Move) != 0)
        {
            Report(e, Map(e.Button), CoreMouseAction.Move);
        }
    }

    private bool Report(TerminalPointerEvent e, CoreMouseButton button, CoreMouseAction action) =>
        _core.CoreMouseService.TriggerMouseEvent(new CoreMouseEvent
        {
            Col = e.Column,
            Row = e.Row,
            X = e.X,
            Y = e.Y,
            Button = button,
            Action = action,
            Ctrl = e.Ctrl,
            Alt = e.Alt,
            Shift = e.Shift,
        });

    private static CoreMouseButton Map(PointerButton button) => button switch
    {
        PointerButton.Left => CoreMouseButton.Left,
        PointerButton.Middle => CoreMouseButton.Middle,
        PointerButton.Right => CoreMouseButton.Right,
        _ => CoreMouseButton.None,
    };

    // The 0-based buffer coordinates of the pointer: the column may be one past the last one, the row is clamped to the
    // viewport.
    private (int X, int Y) GetMouseBufferCoords(TerminalPointerEvent e) =>
        (Math.Clamp(e.Column, 0, _core.Cols), Math.Clamp(e.Row, 0, _core.Rows - 1) + _core.Buffers.Active.YDisp);

    // Gets the amount the viewport should be scrolled based on how far out of the terminal the pointer is.
    private int GetMouseEventScrollAmount(TerminalPointerEvent e, double cellHeight)
    {
        double offset = e.Y;
        var terminalHeight = _core.Rows * cellHeight;
        if (offset >= 0 && offset <= terminalHeight)
        {
            return 0;
        }

        if (offset > terminalHeight)
        {
            offset -= terminalHeight;
        }

        offset = Math.Min(Math.Max(offset, -DragScrollMaxThreshold), DragScrollMaxThreshold);
        offset /= DragScrollMaxThreshold;
        return (int)((offset / Math.Abs(offset)) + Math.Floor((offset * (DragScrollMaxSpeed - 1)) + 0.5));
    }

    // Performs an incremental click, setting the selection end position to the pointer position.
    private void HandleIncrementalClick(TerminalPointerEvent e)
    {
        if (_selection.Model.SelectionStart is not null)
        {
            _selection.Model.SelectionEnd = GetMouseBufferCoords(e);
        }
    }

    // Performs a single click, resetting relevant state and setting the selection start position.
    private void HandleSingleClick(TerminalPointerEvent e)
    {
        var model = _selection.Model;
        model.SelectionStartLength = 0;
        model.IsSelectAllActive = false;
        _selection.ActiveSelectionMode = SelectionService.ShouldColumnSelect(e) ? SelectionMode.Column : SelectionMode.Normal;

        // Initialize the new selection
        var start = GetMouseBufferCoords(e);
        model.SelectionStart = start;
        model.SelectionEnd = null;

        // Ensure the line exists
        var line = _core.Buffers.Active.Lines.Get(start.Y);
        if (line is null)
        {
            return;
        }

        // Return early if the click event is not in the buffer (eg. in scroll bar)
        if (line.Length == start.X)
        {
            return;
        }

        // If the pointer is over the second half of a wide character, adjust the selection to cover the whole character
        if (line.GetWidth(start.X) == 0)
        {
            model.SelectionStart = (start.X + 1, start.Y);
        }
    }

    // Performs a double click, selecting the current word.
    private void HandleDoubleClick(TerminalPointerEvent e)
    {
        _selection.SelectWordAt(GetMouseBufferCoords(e), true);
        _selection.Model.SelectionEnd = null;
        _selection.ActiveSelectionMode = SelectionMode.Word;
    }

    // Performs a triple click, selecting the current line and activating line select mode.
    private void HandleTripleClick(TerminalPointerEvent e)
    {
        _selection.ActiveSelectionMode = SelectionMode.Line;
        _selection.SelectLineAt(GetMouseBufferCoords(e).Y);
    }

    // Records the end of the selection while the button is down.
    private void HandleSelectionMove(TerminalPointerEvent e, double cellHeight)
    {
        var model = _selection.Model;

        // Do nothing if there is no selection start, this can happen if the first click in the terminal is an
        // incremental click
        if (model.SelectionStart is not { } start)
        {
            return;
        }

        // Set the initial selection end based on the pointer coordinates
        var end = GetMouseBufferCoords(e);
        model.SelectionEnd = end;

        // Select the entire line if line select mode is active.
        if (_selection.ActiveSelectionMode == SelectionMode.Line)
        {
            model.SelectionEnd = (end.Y < start.Y ? 0 : _core.Cols, end.Y);
        }
        else if (_selection.ActiveSelectionMode == SelectionMode.Word)
        {
            _selection.SelectToWordAt(end);
            end = model.SelectionEnd ?? end;
        }

        end = model.SelectionEnd ?? end;

        // Determine the amount of scrolling that will happen.
        _dragScrollAmount = GetMouseEventScrollAmount(e, cellHeight);

        // If the cursor was above or below the viewport, make sure it's at the start or end of the viewport
        // respectively. This should only happen when NOT in column select mode.
        if (_selection.ActiveSelectionMode != SelectionMode.Column)
        {
            if (_dragScrollAmount > 0)
            {
                end = (_core.Cols, end.Y);
            }
            else if (_dragScrollAmount < 0)
            {
                end = (0, end.Y);
            }
        }

        // If the character is a wide character include the cell to the right in the selection. Note that selections at
        // the very end of the line will never have a character.
        var buffer = _core.Buffers.Active;
        if (end.Y < buffer.Lines.Length && buffer.Lines.Get(end.Y) is { } line && line.GetWidth(end.X) == 0 && end.X < _core.Cols)
        {
            end = (end.X + 1, end.Y);
        }

        model.SelectionEnd = end;

        if (_dragScrollAmount != 0 && _time.GetElapsedTime(_lastDragScroll) >= DragScrollInterval)
        {
            DragScroll();
        }
    }

    // Scrolls the viewport while the pointer is beyond the area, at most once per drag scroll interval.
    private void DragScroll()
    {
        var model = _selection.Model;
        if (model.SelectionEnd is not { } end || model.SelectionStart is null)
        {
            return;
        }

        _lastDragScroll = _time.GetTimestamp();
        _core.ScrollLines(_dragScrollAmount);

        // Re-evaluate selection
        // If the cursor was above or below the viewport, make sure it's at the start or end of the viewport
        // respectively. This should only happen when NOT in column select mode.
        var buffer = _core.Buffers.Active;
        if (_dragScrollAmount > 0)
        {
            model.SelectionEnd = (
                _selection.ActiveSelectionMode != SelectionMode.Column ? _core.Cols : end.X,
                Math.Min(buffer.YDisp + _core.Rows, buffer.Lines.Length - 1));
        }
        else
        {
            model.SelectionEnd = (_selection.ActiveSelectionMode != SelectionMode.Column ? 0 : end.X, buffer.YDisp);
        }
    }

    // Ends the selection gesture; a quick Alt+click without selection moves the cursor of the shell.
    private void HandleSelectionUp(TerminalPointerEvent e)
    {
        var timeElapsed = _time.GetElapsedTime(_mouseDownTimestamp);
        if (_selection.SelectionText.Length <= 1 && timeElapsed < AltClickMoveCursorTime && e.Alt && _core.Options.AltClickMovesCursor)
        {
            var buffer = _core.Buffers.Active;
            if (buffer.YBase == buffer.YDisp)
            {
                var sequence = MoveToCell.Sequence(
                    Math.Clamp(e.Column, 0, _core.Cols - 1),
                    Math.Clamp(e.Row, 0, _core.Rows - 1),
                    _core.BufferService,
                    _core.CoreService.DecPrivateModes.ApplicationCursorKeys);
                _core.Input(sequence);
            }
        }
        else
        {
            _selection.FireEventIfSelectionChanged();
        }
    }
}
