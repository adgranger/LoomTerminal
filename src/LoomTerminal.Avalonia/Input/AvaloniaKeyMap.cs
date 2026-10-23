using Avalonia.Input;
using LoomTerminal.Presentation.Input;

namespace LoomTerminal.Avalonia.Input;

/// <summary>
/// Maps Avalonia keys to the DOM values <c>Keyboard.ts</c> expects (ADR 0004). Avalonia <see cref="Key"/> values are not
/// Windows virtual keys, so every key has an entry: <c>keyCode</c> is the Windows virtual-key code browsers report
/// (OEM keys follow the US layout, 186-222), <c>key</c> is a DOM name for non-printable keys and the layout's
/// character (<see cref="KeyEventArgs.KeySymbol"/>) otherwise.
/// </summary>
/// <remarks>Threading: stateless; any thread.</remarks>
internal static class AvaloniaKeyMap
{
    /// <summary>DOM <c>keyCode</c> of <paramref name="key"/>, or 0 when the key has no DOM equivalent.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The key code.</returns>
    public static int ToKeyCode(Key key)
    {
        if (key is >= Key.A and <= Key.Z)
        {
            return 'A' + (key - Key.A);
        }

        if (key is >= Key.D0 and <= Key.D9)
        {
            return '0' + (key - Key.D0);
        }

        if (key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            return 96 + (key - Key.NumPad0);
        }

        if (key is >= Key.F1 and <= Key.F24)
        {
            return 112 + (key - Key.F1);
        }

        return key switch
        {
            Key.Back => DomKeyCodes.Backspace,
            Key.Tab => DomKeyCodes.Tab,
            Key.Enter => DomKeyCodes.Enter,
            Key.LeftShift or Key.RightShift => DomKeyCodes.Shift,
            Key.LeftCtrl or Key.RightCtrl => DomKeyCodes.Ctrl,
            Key.LeftAlt or Key.RightAlt => DomKeyCodes.Alt,
            Key.Pause => 19,
            Key.CapsLock => 20,
            Key.Escape => DomKeyCodes.Escape,
            Key.Space => DomKeyCodes.Space,
            Key.PageUp => DomKeyCodes.PageUp,
            Key.PageDown => DomKeyCodes.PageDown,
            Key.End => DomKeyCodes.End,
            Key.Home => DomKeyCodes.Home,
            Key.Left => DomKeyCodes.ArrowLeft,
            Key.Up => DomKeyCodes.ArrowUp,
            Key.Right => DomKeyCodes.ArrowRight,
            Key.Down => DomKeyCodes.ArrowDown,
            Key.PrintScreen => 44,
            Key.Insert => DomKeyCodes.Insert,
            Key.Delete => 46,
            Key.LWin => 91,
            Key.RWin => 92,
            Key.Apps => 93,
            Key.Multiply => 106,
            Key.Add => 107,
            Key.Subtract => 109,
            Key.Decimal => 110,
            Key.Divide => 111,
            Key.NumLock => 144,
            Key.Scroll => 145,
            Key.OemSemicolon => 186,
            Key.OemPlus => 187,
            Key.OemComma => 188,
            Key.OemMinus => 189,
            Key.OemPeriod => 190,
            Key.OemQuestion => 191,
            Key.OemTilde => 192,
            Key.OemOpenBrackets => 219,
            Key.OemPipe => 220,
            Key.OemCloseBrackets => 221,
            Key.OemQuotes => 222,
            Key.OemBackslash => 226,
            _ => 0,
        };
    }

    /// <summary>Whether <paramref name="key"/> is a Tab or an arrow: keys focus navigation could steal.</summary>
    /// <param name="key">The key.</param>
    /// <returns>True for Tab and the four arrows.</returns>
    public static bool IsNavigationKey(Key key) => key is Key.Tab or Key.Up or Key.Down or Key.Left or Key.Right;

    /// <summary>DOM <c>key</c> name of a non-printable key, or <see langword="null"/> when the key prints.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The DOM name.</returns>
    public static string? NonPrintableName(Key key)
    {
        if (key is >= Key.F1 and <= Key.F24)
        {
            return "F" + (key - Key.F1 + 1);
        }

        return key switch
        {
            Key.Back => "Backspace",
            Key.Tab => "Tab",
            Key.Enter => "Enter",
            Key.LeftShift or Key.RightShift => "Shift",
            Key.LeftCtrl or Key.RightCtrl => "Control",
            Key.LeftAlt or Key.RightAlt => "Alt",
            Key.Pause => "Pause",
            Key.CapsLock => "CapsLock",
            Key.Escape => "Escape",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.End => "End",
            Key.Home => "Home",
            Key.Left => "ArrowLeft",
            Key.Up => "ArrowUp",
            Key.Right => "ArrowRight",
            Key.Down => "ArrowDown",
            Key.PrintScreen => "PrintScreen",
            Key.Insert => "Insert",
            Key.Delete => "Delete",
            Key.LWin or Key.RWin => "Meta",
            Key.Apps => "ContextMenu",
            Key.NumLock => "NumLock",
            Key.Scroll => "ScrollLock",
            _ => null,
        };
    }

    /// <summary>The DOM <c>key</c> value of a key press.</summary>
    /// <param name="key">The key.</param>
    /// <param name="keySymbol">The text the key produces in the current layout, if the platform reports it.</param>
    /// <param name="shift">Whether Shift is held (only used by the fallback).</param>
    /// <returns>A DOM name, the produced character, or an empty string (dead key, or no character).</returns>
    public static string ToDomKey(Key key, string? keySymbol, bool shift)
    {
        if (NonPrintableName(key) is { } name)
        {
            return name;
        }

        if (!string.IsNullOrEmpty(keySymbol) && !char.IsControl(keySymbol[0]))
        {
            return keySymbol;
        }

        // shortcut: US-layout letter or digit when the platform gives no usable symbol (Ctrl+C reports U+0003); other
        // keys give "". Upgrade if another layout needs Ctrl combinations on non-letter keys.
        if (key is >= Key.A and <= Key.Z)
        {
            var letter = (char)('a' + (key - Key.A));
            return (shift ? char.ToUpperInvariant(letter) : letter).ToString();
        }

        return key is >= Key.D0 and <= Key.D9 && !shift ? ((char)('0' + (key - Key.D0))).ToString() : string.Empty;
    }
}
