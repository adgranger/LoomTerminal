// Inspired by LoomEditor: src/LoomEditor.WinUI/Controls/LoomEditor.Caret.cs (DispatcherQueueTimer driven by the blink state)
using Microsoft.UI.Dispatching;

namespace LoomTerminal.WinUI.Controls;

public sealed partial class TerminalControl
{
    private DispatcherQueueTimer? _blinkTimer;

    private void InitializeFocus()
    {
        _blinkTimer = DispatcherQueue.CreateTimer();
        _blinkTimer.IsRepeating = false;
        _blinkTimer.Tick += (_, _) => _session?.RefreshFrame();
        GotFocus += (_, _) =>
        {
            UpdateAccessibility();
            _session?.SetFocus(true);
        };
        LostFocus += (_, _) => _session?.SetFocus(false);
    }

    // The frame bakes the blink phase: one timer, re-armed at the next phase change and stopped when the session
    // reports none (unfocused, or blinking off).
    private void ScheduleBlink()
    {
        if (_blinkTimer is null)
        {
            return;
        }

        _blinkTimer.Stop();
        if (CursorBlink && _session?.CursorBlink.NextChange is { } next)
        {
            _blinkTimer.Interval = next;
            _blinkTimer.Start();
        }
    }
}
