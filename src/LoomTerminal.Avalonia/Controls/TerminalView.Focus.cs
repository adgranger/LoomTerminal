using Avalonia.Input;
using Avalonia.Threading;

namespace LoomTerminal.Avalonia.Controls;

public sealed partial class TerminalView
{
    private readonly DispatcherTimer _blinkTimer = new();

    private void InitializeFocus() => _blinkTimer.Tick += (_, _) =>
    {
        _blinkTimer.Stop();
        _session?.RefreshFrame();
    };

    /// <inheritdoc />
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        _session?.SetFocus(true);
    }

    /// <inheritdoc />
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        _session?.SetFocus(false);
        ResetKeyboardState();
    }

    // The frame bakes the blink phase: one timer, re-armed at the next phase change and stopped when the session
    // reports none (unfocused, or blinking off).
    private void ScheduleBlink()
    {
        _blinkTimer.Stop();
        if (CursorBlink && _session?.CursorBlink.NextChange is { } next && next > TimeSpan.Zero)
        {
            _blinkTimer.Interval = next;
            _blinkTimer.Start();
        }
    }
}
