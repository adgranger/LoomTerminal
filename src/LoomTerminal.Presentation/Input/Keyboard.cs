// Ported from xtermjs/xterm.js: src/common/input/Keyboard.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Data;

namespace LoomTerminal.Presentation.Input;

/// <summary>Turns key events into the bytes a terminal sends.</summary>
/// <remarks>Threading: stateless; any thread.</remarks>
internal static class Keyboard
{
    // reg + shift key mappings for digits and special chars
    private static readonly Dictionary<int, (string Plain, string Shifted)> KeycodeKeyMappings = new()
    {
        // digits 0-9
        [48] = ("0", ")"),
        [49] = ("1", "!"),
        [50] = ("2", "@"),
        [51] = ("3", "#"),
        [52] = ("4", "$"),
        [53] = ("5", "%"),
        [54] = ("6", "^"),
        [55] = ("7", "&"),
        [56] = ("8", "*"),
        [57] = ("9", "("),

        // special chars
        [186] = (";", ":"),
        [187] = ("=", "+"),
        [188] = (",", "<"),
        [189] = ("-", "_"),
        [190] = (".", ">"),
        [191] = ("/", "?"),
        [192] = ("`", "~"),
        [219] = ("[", "{"),
        [220] = ("\\", "|"),
        [221] = ("]", "}"),
        [222] = ("'", "\""),
    };

    /// <summary>Evaluates a key event.</summary>
    /// <param name="ev">The key event.</param>
    /// <param name="applicationCursorMode">Whether DECCKM is set.</param>
    /// <param name="isMac">Whether the host is macOS.</param>
    /// <param name="macOptionIsMeta">Whether Option acts as Meta on macOS.</param>
    /// <returns>What to do with the event.</returns>
    internal static KeyboardResult EvaluateKeyboardEvent(
        TerminalKeyEvent ev,
        bool applicationCursorMode,
        bool isMac,
        bool macOptionIsMeta)
    {
        var result = new KeyboardResult { Type = KeyboardResultType.SendKey, Cancel = false, Key = null };
        var modifiers = (ev.Shift ? 1 : 0) | (ev.Alt ? 2 : 0) | (ev.Ctrl ? 4 : 0) | (ev.Meta ? 8 : 0);
        switch (ev.KeyCode)
        {
            case 0:
                if (ev.Key == "UIKeyInputUpArrow")
                {
                    result.Key = applicationCursorMode ? C0.Esc + "OA" : C0.Esc + "[A";
                }
                else if (ev.Key == "UIKeyInputLeftArrow")
                {
                    result.Key = applicationCursorMode ? C0.Esc + "OD" : C0.Esc + "[D";
                }
                else if (ev.Key == "UIKeyInputRightArrow")
                {
                    result.Key = applicationCursorMode ? C0.Esc + "OC" : C0.Esc + "[C";
                }
                else if (ev.Key == "UIKeyInputDownArrow")
                {
                    result.Key = applicationCursorMode ? C0.Esc + "OB" : C0.Esc + "[B";
                }

                break;
            case 8:
                // backspace
                result.Key = ev.Ctrl ? "\b" : C0.Del; // ^H or ^?
                if (ev.Alt)
                {
                    result.Key = C0.Esc + result.Key;
                }

                break;
            case 9:
                // tab
                if (ev.Shift)
                {
                    result.Key = C0.Esc + "[Z";
                    break;
                }

                result.Key = C0.Ht;
                result.Cancel = true;
                break;
            case 13:
                // return/enter
                result.Key = ev.Alt ? C0.Esc + C0.Cr : C0.Cr;
                result.Cancel = true;
                break;
            case 27:
                // escape
                result.Key = C0.Esc;
                if (ev.Alt)
                {
                    result.Key = C0.Esc + C0.Esc;
                }

                result.Cancel = true;
                break;
            case 37:
                // left-arrow
                if (ev.Meta)
                {
                    break;
                }

                result.Key = modifiers != 0 ? C0.Esc + "[1;" + (modifiers + 1) + "D"
                    : applicationCursorMode ? C0.Esc + "OD" : C0.Esc + "[D";
                break;
            case 39:
                // right-arrow
                if (ev.Meta)
                {
                    break;
                }

                result.Key = modifiers != 0 ? C0.Esc + "[1;" + (modifiers + 1) + "C"
                    : applicationCursorMode ? C0.Esc + "OC" : C0.Esc + "[C";
                break;
            case 38:
                // up-arrow
                if (ev.Meta)
                {
                    break;
                }

                result.Key = modifiers != 0 ? C0.Esc + "[1;" + (modifiers + 1) + "A"
                    : applicationCursorMode ? C0.Esc + "OA" : C0.Esc + "[A";
                break;
            case 40:
                // down-arrow
                if (ev.Meta)
                {
                    break;
                }

                result.Key = modifiers != 0 ? C0.Esc + "[1;" + (modifiers + 1) + "B"
                    : applicationCursorMode ? C0.Esc + "OB" : C0.Esc + "[B";
                break;
            case 45:
                // insert
                if (!ev.Shift && !ev.Ctrl)
                {
                    // <Ctrl> or <Shift> + <Insert> are used to
                    // copy-paste on some systems.
                    result.Key = C0.Esc + "[2~";
                }

                break;
            case 46:
                // delete
                result.Key = modifiers != 0 ? C0.Esc + "[3;" + (modifiers + 1) + "~" : C0.Esc + "[3~";
                break;
            case 36:
                // home
                result.Key = modifiers != 0 ? C0.Esc + "[1;" + (modifiers + 1) + "H"
                    : applicationCursorMode ? C0.Esc + "OH" : C0.Esc + "[H";
                break;
            case 35:
                // end
                result.Key = modifiers != 0 ? C0.Esc + "[1;" + (modifiers + 1) + "F"
                    : applicationCursorMode ? C0.Esc + "OF" : C0.Esc + "[F";
                break;
            case 33:
                // page up
                if (ev.Shift)
                {
                    result.Type = KeyboardResultType.PageUp;
                }
                else if (ev.Ctrl)
                {
                    result.Key = C0.Esc + "[5;" + (modifiers + 1) + "~";
                }
                else
                {
                    result.Key = C0.Esc + "[5~";
                }

                break;
            case 34:
                // page down
                if (ev.Shift)
                {
                    result.Type = KeyboardResultType.PageDown;
                }
                else if (ev.Ctrl)
                {
                    result.Key = C0.Esc + "[6;" + (modifiers + 1) + "~";
                }
                else
                {
                    result.Key = C0.Esc + "[6~";
                }

                break;
            case 112:
                // F1-F12
                result.Key = modifiers != 0 ? C0.Esc + "[1;" + (modifiers + 1) + "P" : C0.Esc + "OP";
                break;
            case 113:
                result.Key = modifiers != 0 ? C0.Esc + "[1;" + (modifiers + 1) + "Q" : C0.Esc + "OQ";
                break;
            case 114:
                result.Key = modifiers != 0 ? C0.Esc + "[1;" + (modifiers + 1) + "R" : C0.Esc + "OR";
                break;
            case 115:
                result.Key = modifiers != 0 ? C0.Esc + "[1;" + (modifiers + 1) + "S" : C0.Esc + "OS";
                break;
            case 116:
                result.Key = modifiers != 0 ? C0.Esc + "[15;" + (modifiers + 1) + "~" : C0.Esc + "[15~";
                break;
            case 117:
                result.Key = modifiers != 0 ? C0.Esc + "[17;" + (modifiers + 1) + "~" : C0.Esc + "[17~";
                break;
            case 118:
                result.Key = modifiers != 0 ? C0.Esc + "[18;" + (modifiers + 1) + "~" : C0.Esc + "[18~";
                break;
            case 119:
                result.Key = modifiers != 0 ? C0.Esc + "[19;" + (modifiers + 1) + "~" : C0.Esc + "[19~";
                break;
            case 120:
                result.Key = modifiers != 0 ? C0.Esc + "[20;" + (modifiers + 1) + "~" : C0.Esc + "[20~";
                break;
            case 121:
                result.Key = modifiers != 0 ? C0.Esc + "[21;" + (modifiers + 1) + "~" : C0.Esc + "[21~";
                break;
            case 122:
                result.Key = modifiers != 0 ? C0.Esc + "[23;" + (modifiers + 1) + "~" : C0.Esc + "[23~";
                break;
            case 123:
                result.Key = modifiers != 0 ? C0.Esc + "[24;" + (modifiers + 1) + "~" : C0.Esc + "[24~";
                break;
            default:
                EvaluateDefault(ev, isMac, macOptionIsMeta, result);
                break;
        }

        return result;
    }

    private static void EvaluateDefault(TerminalKeyEvent ev, bool isMac, bool macOptionIsMeta, KeyboardResult result)
    {
        // a-z and space
        if (ev.Ctrl && !ev.Shift && !ev.Alt && !ev.Meta)
        {
            if (ev.KeyCode >= 65 && ev.KeyCode <= 90)
            {
                result.Key = ((char)(ev.KeyCode - 64)).ToString();
            }
            else if (ev.KeyCode == 32)
            {
                result.Key = C0.Nul;
            }
            else if (ev.KeyCode >= 51 && ev.KeyCode <= 55)
            {
                // escape, file sep, group sep, record sep, unit sep
                result.Key = ((char)(ev.KeyCode - 51 + 27)).ToString();
            }
            else if (ev.KeyCode == 56)
            {
                result.Key = C0.Del;
            }
            else if (ev.KeyCode == 219)
            {
                result.Key = C0.Esc;
            }
            else if (ev.KeyCode == 220)
            {
                result.Key = C0.Fs;
            }
            else if (ev.KeyCode == 221)
            {
                result.Key = C0.Gs;
            }
        }
        else if ((!isMac || macOptionIsMeta) && ev.Alt && !ev.Meta)
        {
            // On macOS this is a third level shift when !macOptionIsMeta. Use <Esc> instead.
            var key = KeycodeKeyMappings.TryGetValue(ev.KeyCode, out var mapping)
                ? (!ev.Shift ? mapping.Plain : mapping.Shifted)
                : null;
            if (!string.IsNullOrEmpty(key))
            {
                result.Key = C0.Esc + key;
            }
            else if (ev.KeyCode >= 65 && ev.KeyCode <= 90)
            {
                var keyCode = ev.Ctrl ? ev.KeyCode - 64 : ev.KeyCode + 32;
                var keyString = ((char)keyCode).ToString();
                if (ev.Shift)
                {
                    keyString = keyString.ToUpperInvariant();
                }

                result.Key = C0.Esc + keyString;
            }
            else if (ev.KeyCode == 32)
            {
                result.Key = C0.Esc + (ev.Ctrl ? C0.Nul : " ");
            }

            // Not ported: the `ev.key === 'Dead'` branch (xterm.js issue 3725). It reads the DOM `code`, which
            // TerminalKeyEvent does not carry (ADR 0004).
        }
        else if (isMac && !ev.Alt && !ev.Ctrl && !ev.Shift && ev.Meta)
        {
            if (ev.KeyCode == 65)
            {
                // cmd + a
                result.Type = KeyboardResultType.SelectAll;
            }
        }
        else if (!string.IsNullOrEmpty(ev.Key) && !ev.Ctrl && !ev.Alt && !ev.Meta && ev.KeyCode >= 48 && ev.Key.Length == 1)
        {
            // Include only keys that that result in a _single_ character; don't include num lock,
            // volume up, etc.
            result.Key = ev.Key;
        }
        else if (!string.IsNullOrEmpty(ev.Key) && ev.Ctrl)
        {
            if (ev.Key == "_")
            {
                // ^_
                result.Key = C0.Us;
            }

            if (ev.Key == "@")
            {
                // ^ + shift + 2 = ^ + @
                result.Key = C0.Nul;
            }
        }
    }
}
