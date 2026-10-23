using Avalonia;
using Avalonia.Input;
using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Avalonia.Controls;

public sealed partial class TerminalView
{
    private const double WheelLinesPerNotch = 3;

    private PointerButton _heldButton;
    private Point? _lastPosition;
    private KeyModifiers _lastModifiers;
    private int _clickCount = 1;
    private bool _cursorIsHand;
    private bool _cursorSet;

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        PointerPoint point = e.GetCurrentPoint(this);
        PointerButton button = point.Properties.PointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed => PointerButton.Left,
            PointerUpdateKind.MiddleButtonPressed => PointerButton.Middle,
            PointerUpdateKind.RightButtonPressed => PointerButton.Right,
            _ => PointerButton.None,
        };
        if (_session is not { } session || button == PointerButton.None)
        {
            return;
        }

        Focus(NavigationMethod.Pointer);
        e.Handled = true;
        _clickCount = Math.Clamp(e.ClickCount, 1, 3);
        if (button == PointerButton.Right)
        {
            _ = session.HandleRightClick(ToEvent(point.Position, button, PointerAction.Down, e.KeyModifiers));
            return;
        }

        e.Pointer.Capture(this);
        _heldButton = button;
        SendPointer(point.Position, button, PointerAction.Down, e.KeyModifiers);
        RequestFrame();
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        SendPointer(e.GetPosition(this), _heldButton, PointerAction.Move, e.KeyModifiers);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        PointerButton button = e.InitialPressMouseButton switch
        {
            MouseButton.Left => PointerButton.Left,
            MouseButton.Middle => PointerButton.Middle,
            _ => PointerButton.None,
        };
        if (button == PointerButton.None || button != _heldButton)
        {
            return;
        }

        SendPointer(e.GetPosition(this), button, PointerAction.Up, e.KeyModifiers);
        _heldButton = PointerButton.None;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _heldButton = PointerButton.None;
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _lastPosition = null;
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_session is not { } session)
        {
            return;
        }

        // DOM convention: a positive delta scrolls down, whereas Delta.Y is positive up.
        TerminalPointerEvent ev = ToEvent(e.GetPosition(this), PointerButton.None, PointerAction.Wheel, e.KeyModifiers) with
        {
            DeltaMode = WheelDeltaMode.Line,
            WheelDeltaY = -e.Delta.Y * WheelLinesPerNotch,
        };
        session.HandleWheel(ev, CellHeightInPixels());
        e.Handled = true;
    }

    // Ctrl decides whether the link under a still pointer is underlined.
    private void OnControlKeyChanged(Key key, KeyModifiers modifiers)
    {
        if (key is Key.LeftCtrl or Key.RightCtrl && _lastPosition is { } position)
        {
            SendPointer(position, _heldButton, PointerAction.Move, modifiers);
        }
    }

    private void SendPointer(Point position, PointerButton button, PointerAction action, KeyModifiers modifiers)
    {
        _lastPosition = position;
        _lastModifiers = modifiers;
        _session?.HandlePointer(ToEvent(position, button, action, modifiers), CellHeightInPixels());
    }

    // Cells from the position minus Padding (DIPs); X and Y in device pixels, as mouse reports expect.
    private TerminalPointerEvent ToEvent(Point position, PointerButton button, PointerAction action, KeyModifiers modifiers)
    {
        CellMetrics metrics = _measurer.Measure(CurrentFont(), _scale);
        double x = position.X - Padding.Left;
        double y = position.Y - Padding.Top;
        return new TerminalPointerEvent(
            (int)Math.Floor(x / metrics.Width),
            (int)Math.Floor(y / metrics.Height),
            (int)Math.Round(x * _scale),
            (int)Math.Round(y * _scale),
            button,
            action,
            modifiers.HasFlag(KeyModifiers.Control),
            modifiers.HasFlag(KeyModifiers.Alt),
            modifiers.HasFlag(KeyModifiers.Shift))
        {
            ClickCount = _clickCount,
        };
    }

    private double CellHeightInPixels() => _measurer.Measure(CurrentFont(), _scale).Height * _scale;

    // Called every frame: a button held while the pointer is outside keeps the drag scrolling (P3-15).
    private void PumpPointerFrame()
    {
        if (_heldButton != PointerButton.None && _lastPosition is { } position)
        {
            SendPointer(position, _heldButton, PointerAction.Move, _lastModifiers);
            RequestFrame();
        }

        UpdateCursorShape();
    }

    // The standard Arrow (also in mouse tracking mode), Hand over a link opened by Ctrl+click.
    private void UpdateCursorShape()
    {
        if (_session is not { } session)
        {
            return;
        }

        bool hand = !session.IsMouseTracking && session.IsOverLink && _lastModifiers.HasFlag(KeyModifiers.Control);
        if (_cursorSet && hand == _cursorIsHand)
        {
            return;
        }

        (_cursorIsHand, _cursorSet) = (hand, true);
        Cursor = new Cursor(hand ? StandardCursorType.Hand : StandardCursorType.Arrow);
    }
}
