using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using LoomTerminal.Avalonia.Input;
using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Avalonia.Controls;

public sealed partial class TerminalView
{
    private bool _keyWasSent;
    private bool _rightAltDown;
    private string? _composition;
    private TerminalTextInputClient? _ime;

    private void InitializeKeyboard() => TextInputMethodClientRequested += (_, e) =>
        e.Client = _ime ??= new TerminalTextInputClient(this, GetCursorRectangle, SetComposition);

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        OnControlKeyChanged(e.Key, e.KeyModifiers);
        if (e.Key == Key.RightAlt)
        {
            _rightAltDown = true;
        }

        // ImeProcessed: the key belongs to the IME composition.
        if (_session is not { } session || e.Key is Key.ImeProcessed || _ime is { IsComposing: true })
        {
            return;
        }

        KeyModifiers modifiers = e.KeyModifiers;
        bool ctrl = modifiers.HasFlag(KeyModifiers.Control);
        bool alt = modifiers.HasFlag(KeyModifiers.Alt);
        bool shift = modifiers.HasFlag(KeyModifiers.Shift);
        // AltGr arrives as Ctrl+Alt on Windows: only the right Alt makes it AltGr.
        bool altGraph = ctrl && alt && _rightAltDown;

        var ev = new TerminalKeyEvent(
            AvaloniaKeyMap.ToKeyCode(e.Key),
            AvaloniaKeyMap.ToDomKey(e.Key, e.KeySymbol, shift),
            ctrl,
            alt,
            shift,
            modifiers.HasFlag(KeyModifiers.Meta),
            TerminalKeyEventType.KeyDown)
        {
            AltGraph = altGraph,
        };

        // LeaveToTextInput and Ignored stay unhandled: TextInput delivers the text.
        KeyInputOutcome outcome = session.SendKey(ev);
        _keyWasSent = outcome is KeyInputOutcome.Sent;
        e.Handled = outcome is KeyInputOutcome.Sent or KeyInputOutcome.Copy or KeyInputOutcome.Paste or KeyInputOutcome.ClearScrollback
            or KeyInputOutcome.HandledByHost;
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        OnControlKeyChanged(e.Key, e.KeyModifiers);
        _keyWasSent = false;
        if (e.Key == Key.RightAlt)
        {
            _rightAltDown = false;
        }
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        // Avalonia delivers whole strings (surrogate pairs included); control characters were sent by OnKeyDown.
        if (_session is not { } session || _keyWasSent || _ime is { IsComposing: true }
            || string.IsNullOrEmpty(e.Text) || e.Text[0] < 0x20 || e.Text[0] == 0x7F)
        {
            return;
        }

        e.Handled = true;
        session.SendText(e.Text);
    }

    // A key released while unfocused is never seen: do not keep a stale right Alt, and drop an abandoned composition.
    private void ResetKeyboardState()
    {
        _rightAltDown = false;
        _keyWasSent = false;
        _ime?.Reset();
    }

    private void SetComposition(string? text)
    {
        _composition = text;
        RequestFrame();
    }

    // The cursor cell in control coordinates (DIPs), where the candidate window opens.
    private Rect GetCursorRectangle()
    {
        if (_lastFrame?.Cursor is not { } cursor)
        {
            return default;
        }

        CellMetrics metrics = _measurer.Measure(CurrentFont(), _scale);
        return new Rect(
            Padding.Left + (cursor.Column * metrics.Width), Padding.Top + (cursor.Row * metrics.Height), metrics.Width, metrics.Height);
    }

    // Called after a frame is drawn: the candidate window follows the cursor.
    private void NotifyImeCursorMoved() => Dispatcher.UIThread.Post(() => _ime?.NotifyCursorMoved());
}
