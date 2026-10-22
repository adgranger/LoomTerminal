using LoomTerminal.Presentation.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;

namespace LoomTerminal.WinUI.Controls;

public sealed partial class TerminalControl
{
    private const double DoubleClickDistance = 4;
    private const double WheelLinesPerNotch = 3;

    private readonly UISettings _uiSettings = new();
    private PointerButton _heldButton;
    private Point? _lastPosition;
    private Point _lastClickPosition;
    private long _lastClickTicks;
    private int _clickCount = 1;
    private bool _cursorIsArrow;
    private bool _cursorIsHand;
    private bool _cursorSet;

    private void InitializePointer()
    {
        PointerPressed += OnPointerPressed;
        PointerMoved += (_, e) => SendPointer(e.GetCurrentPoint(this).Position, _heldButton, PointerAction.Move, e.KeyModifiers);
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += (_, _) => _heldButton = PointerButton.None;
        PointerWheelChanged += OnPointerWheelChanged;
        PointerExited += (_, _) => _lastPosition = null;
        // Ctrl decides whether the link under a still pointer is underlined.
        KeyDown += OnControlKeyChanged;
        KeyUp += OnControlKeyChanged;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var button = point.Properties.PointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed => PointerButton.Left,
            PointerUpdateKind.MiddleButtonPressed => PointerButton.Middle,
            PointerUpdateKind.RightButtonPressed => PointerButton.Right,
            _ => PointerButton.None,
        };
        if (_session is not { } session || _measurer is null || button == PointerButton.None)
        {
            return;
        }

        Focus(FocusState.Pointer);
        e.Handled = true;
        if (button == PointerButton.Right)
        {
            _ = session.HandleRightClick(ToEvent(point.Position, button, PointerAction.Down, e.KeyModifiers));
            return;
        }

        CapturePointer(e.Pointer);
        _heldButton = button;
        CountClick(point.Position);
        SendPointer(point.Position, button, PointerAction.Down, e.KeyModifiers);
        RequestFrame();
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var button = point.Properties.PointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonReleased => PointerButton.Left,
            PointerUpdateKind.MiddleButtonReleased => PointerButton.Middle,
            _ => PointerButton.None,
        };
        if (button == PointerButton.None || button != _heldButton)
        {
            return;
        }

        SendPointer(point.Position, button, PointerAction.Up, e.KeyModifiers);
        _heldButton = PointerButton.None;
        ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (_session is not { } session || _measurer is null)
        {
            return;
        }

        var point = e.GetCurrentPoint(this);
        // DOM convention: a positive delta scrolls down, whereas MouseWheelDelta is positive away from the user.
        var ev = ToEvent(point.Position, PointerButton.None, PointerAction.Wheel, e.KeyModifiers) with
        {
            DeltaMode = WheelDeltaMode.Line,
            WheelDeltaY = -point.Properties.MouseWheelDelta / 120.0 * WheelLinesPerNotch,
        };
        session.HandleWheel(ev, CellHeightInPixels());
        e.Handled = true;
    }

    private void OnControlKeyChanged(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Control && _lastPosition is { } position)
        {
            SendPointer(position, _heldButton, PointerAction.Move, CurrentModifiers());
        }
    }

    // Two presses close in time and space count up to 3, then restart.
    private void CountClick(Point position)
    {
        var now = Environment.TickCount64;
        var near = Math.Abs(position.X - _lastClickPosition.X) <= DoubleClickDistance
            && Math.Abs(position.Y - _lastClickPosition.Y) <= DoubleClickDistance;
        _clickCount = near && now - _lastClickTicks <= _uiSettings.DoubleClickTime && _clickCount < 3 ? _clickCount + 1 : 1;
        _lastClickTicks = now;
        _lastClickPosition = position;
    }

    private void SendPointer(Point position, PointerButton button, PointerAction action, VirtualKeyModifiers modifiers)
    {
        _lastPosition = position;
        if (_session is { } session && _measurer is not null)
        {
            session.HandlePointer(ToEvent(position, button, action, modifiers), CellHeightInPixels());
        }
    }

    // Cells from the position minus Padding (DIPs); X and Y in device pixels, as mouse reports expect.
    private TerminalPointerEvent ToEvent(Point position, PointerButton button, PointerAction action, VirtualKeyModifiers modifiers)
    {
        var metrics = _measurer!.Measure(CurrentFont(), _scale);
        var x = position.X - Padding.Left;
        var y = position.Y - Padding.Top;
        return new TerminalPointerEvent(
            (int)Math.Floor(x / metrics.Width),
            (int)Math.Floor(y / metrics.Height),
            (int)Math.Round(x * _scale),
            (int)Math.Round(y * _scale),
            button,
            action,
            modifiers.HasFlag(VirtualKeyModifiers.Control),
            modifiers.HasFlag(VirtualKeyModifiers.Menu),
            modifiers.HasFlag(VirtualKeyModifiers.Shift))
        {
            ClickCount = _clickCount,
        };
    }

    private double CellHeightInPixels() => _measurer!.Measure(CurrentFont(), _scale).Height * _scale;

    private static VirtualKeyModifiers CurrentModifiers() =>
        (IsDown(VirtualKey.Control) ? VirtualKeyModifiers.Control : VirtualKeyModifiers.None)
        | (IsDown(VirtualKey.Menu) ? VirtualKeyModifiers.Menu : VirtualKeyModifiers.None)
        | (IsDown(VirtualKey.Shift) ? VirtualKeyModifiers.Shift : VirtualKeyModifiers.None);

    // Called every frame: a button held while the pointer is outside keeps the drag scrolling (P3-15).
    private void PumpPointerFrame()
    {
        if (_heldButton != PointerButton.None && _lastPosition is { } position)
        {
            SendPointer(position, _heldButton, PointerAction.Move, CurrentModifiers());
            RequestFrame();
        }

        UpdateCursorShape();
    }

    // IBeam normally, Hand over a link opened by Ctrl+click, Arrow while an application tracks the mouse.
    private void UpdateCursorShape()
    {
        if (_session is not { } session)
        {
            return;
        }

        var arrow = session.IsMouseTracking;
        var hand = !arrow && session.IsOverLink && IsDown(VirtualKey.Control);
        if (_cursorSet && arrow == _cursorIsArrow && hand == _cursorIsHand)
        {
            return;
        }

        (_cursorIsArrow, _cursorIsHand, _cursorSet) = (arrow, hand, true);
        ProtectedCursor = InputSystemCursor.Create(
            arrow ? InputSystemCursorShape.Arrow : hand ? InputSystemCursorShape.Hand : InputSystemCursorShape.IBeam);
    }
}
