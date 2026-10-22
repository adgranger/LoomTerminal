namespace LoomTerminal.Presentation.Input;

/// <summary>
/// Decides what each key event does: host shortcut, copy, paste, VT sequence, or left to the text input of the
/// backend. Rules from VIGO terminal.js and the key handling order of xterm.js CoreBrowserTerminal.
/// </summary>
/// <remarks>Threading: UI thread only; <see cref="KeyPreview"/> is called on the caller's thread.</remarks>
internal sealed class KeyInputHandler
{
    /// <summary>Host callback offered every key down except AltGr ones; <see langword="true"/> means handled by the host.</summary>
    internal Func<TerminalKeyEvent, bool>? KeyPreview { get; set; }

    /// <summary>Decides what to do with <paramref name="ev"/>.</summary>
    /// <param name="ev">The key event.</param>
    /// <param name="ctx">Terminal and platform state.</param>
    /// <returns>The decision.</returns>
    internal KeyInputResult Handle(TerminalKeyEvent ev, KeyContext ctx)
    {
        if (ev.Type != TerminalKeyEventType.KeyDown)
        {
            return new KeyInputResult(KeyInputOutcome.Ignored);
        }

        // AltGr (AZERTY, QWERTZ) arrives as Ctrl+Alt: it types a character, it is never a shortcut.
        if (!ev.AltGraph && KeyPreview?.Invoke(ev) == true)
        {
            return new KeyInputResult(KeyInputOutcome.HandledByHost);
        }

        var isPrintable = ev.Key.Length == 1;
        if (!ev.AltGraph)
        {
            // Copy: Ctrl+Shift+C, or Ctrl+C while text is selected (otherwise Ctrl+C is the interrupt of the shell).
            if (ev.Ctrl && !ev.Alt && ev.KeyCode == DomKeyCodes.KeyC && (ev.Shift || ctx.HasSelection))
            {
                return new KeyInputResult(KeyInputOutcome.Copy);
            }

            if ((ev.Ctrl && !ev.Alt && ev.KeyCode == DomKeyCodes.KeyV)
                || (ev.Shift && !ev.Ctrl && ev.KeyCode == DomKeyCodes.Insert))
            {
                return new KeyInputResult(KeyInputOutcome.Paste);
            }
        }
        else if (isPrintable)
        {
            return new KeyInputResult(KeyInputOutcome.LeaveToTextInput);
        }

        var result = Keyboard.EvaluateKeyboardEvent(ev, ctx.ApplicationCursorKeys, ctx.IsMac, ctx.MacOptionIsMeta);
        if (result.Key is not null)
        {
            return new KeyInputResult(KeyInputOutcome.Sent, result.Key);
        }

        // shortcut: PageUp/PageDown/SelectAll results are dropped here, scrolling arrives with the viewport cards.
        return isPrintable && !ev.Ctrl && !ev.Alt
            ? new KeyInputResult(KeyInputOutcome.LeaveToTextInput)
            : new KeyInputResult(KeyInputOutcome.Ignored);
    }
}
