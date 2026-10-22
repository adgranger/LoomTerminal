// Inspired by LoomEditor: src/LoomEditor.WinUI/Input/KeyboardInput.cs (InputKeyboardSource key state)
using LoomTerminal.Presentation.Input;
using LoomTerminal.WinUI.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace LoomTerminal.WinUI.Controls;

public sealed partial class TerminalControl
{
    private void InitializeKeyboard()
    {
        // Tab and the arrows go through PreviewKeyDown so that focus navigation never steals them.
        PreviewKeyDown += (_, e) =>
        {
            if (VirtualKeyMap.IsNavigationKey(e.Key))
            {
                OnTerminalKey(e);
            }
        };
        KeyDown += (_, e) =>
        {
            if (!VirtualKeyMap.IsNavigationKey(e.Key))
            {
                OnTerminalKey(e);
            }
        };
    }

    private void OnTerminalKey(KeyRoutedEventArgs e)
    {
        if (_session is not { } session)
        {
            return;
        }

        var ctrl = IsDown(VirtualKey.Control);
        var alt = IsDown(VirtualKey.Menu);
        var altGraph = ctrl && alt && IsDown(VirtualKey.RightMenu);
        var ev = new TerminalKeyEvent(
            VirtualKeyMap.ToKeyCode(e.Key),
            VirtualKeyMap.ToDomKey(e.Key, altGraph),
            ctrl,
            alt,
            IsDown(VirtualKey.Shift),
            IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows),
            TerminalKeyEventType.KeyDown)
        {
            AltGraph = altGraph,
        };

        // LeaveToTextInput and Ignored stay unhandled: CharacterReceived (P4-08) delivers the text.
        e.Handled = session.SendKey(ev) is KeyInputOutcome.Sent or KeyInputOutcome.Copy or KeyInputOutcome.Paste or KeyInputOutcome.HandledByHost;
    }

    private static bool IsDown(VirtualKey key) =>
        InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
}
