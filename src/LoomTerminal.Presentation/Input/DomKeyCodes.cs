// Ported from xtermjs/xterm.js: src/common/input/Keyboard.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Presentation.Input;

/// <summary>DOM key codes used by the keyboard evaluation.</summary>
/// <remarks>Threading: constants only; any thread.</remarks>
public static class DomKeyCodes
{
    /// <summary>DOM key code Backspace.</summary>
    public const int Backspace = 8;

    /// <summary>DOM key code Tab.</summary>
    public const int Tab = 9;

    /// <summary>DOM key code Enter.</summary>
    public const int Enter = 13;

    /// <summary>DOM key code Shift.</summary>
    public const int Shift = 16;

    /// <summary>DOM key code Ctrl.</summary>
    public const int Ctrl = 17;

    /// <summary>DOM key code Alt.</summary>
    public const int Alt = 18;

    /// <summary>DOM key code Escape.</summary>
    public const int Escape = 27;

    /// <summary>DOM key code Space.</summary>
    public const int Space = 32;

    /// <summary>DOM key code PageUp.</summary>
    public const int PageUp = 33;

    /// <summary>DOM key code PageDown.</summary>
    public const int PageDown = 34;

    /// <summary>DOM key code End.</summary>
    public const int End = 35;

    /// <summary>DOM key code Home.</summary>
    public const int Home = 36;

    /// <summary>DOM key code ArrowLeft.</summary>
    public const int ArrowLeft = 37;

    /// <summary>DOM key code ArrowUp.</summary>
    public const int ArrowUp = 38;

    /// <summary>DOM key code ArrowRight.</summary>
    public const int ArrowRight = 39;

    /// <summary>DOM key code ArrowDown.</summary>
    public const int ArrowDown = 40;

    /// <summary>DOM key code Insert.</summary>
    public const int Insert = 45;

    /// <summary>DOM key code Delete.</summary>
    public const int Delete = 46;

    /// <summary>DOM key code of digit 0.</summary>
    public const int Digit0 = 48;

    /// <summary>DOM key code of digit 1.</summary>
    public const int Digit1 = 49;

    /// <summary>DOM key code of digit 2.</summary>
    public const int Digit2 = 50;

    /// <summary>DOM key code of digit 3.</summary>
    public const int Digit3 = 51;

    /// <summary>DOM key code of digit 4.</summary>
    public const int Digit4 = 52;

    /// <summary>DOM key code of digit 5.</summary>
    public const int Digit5 = 53;

    /// <summary>DOM key code of digit 6.</summary>
    public const int Digit6 = 54;

    /// <summary>DOM key code of digit 7.</summary>
    public const int Digit7 = 55;

    /// <summary>DOM key code of digit 8.</summary>
    public const int Digit8 = 56;

    /// <summary>DOM key code of digit 9.</summary>
    public const int Digit9 = 57;

    /// <summary>DOM key code of letter A.</summary>
    public const int KeyA = 65;

    /// <summary>DOM key code of letter B.</summary>
    public const int KeyB = 66;

    /// <summary>DOM key code of letter C.</summary>
    public const int KeyC = 67;

    /// <summary>DOM key code of letter D.</summary>
    public const int KeyD = 68;

    /// <summary>DOM key code of letter E.</summary>
    public const int KeyE = 69;

    /// <summary>DOM key code of letter F.</summary>
    public const int KeyF = 70;

    /// <summary>DOM key code of letter G.</summary>
    public const int KeyG = 71;

    /// <summary>DOM key code of letter H.</summary>
    public const int KeyH = 72;

    /// <summary>DOM key code of letter I.</summary>
    public const int KeyI = 73;

    /// <summary>DOM key code of letter J.</summary>
    public const int KeyJ = 74;

    /// <summary>DOM key code of letter K.</summary>
    public const int KeyK = 75;

    /// <summary>DOM key code of letter L.</summary>
    public const int KeyL = 76;

    /// <summary>DOM key code of letter M.</summary>
    public const int KeyM = 77;

    /// <summary>DOM key code of letter N.</summary>
    public const int KeyN = 78;

    /// <summary>DOM key code of letter O.</summary>
    public const int KeyO = 79;

    /// <summary>DOM key code of letter P.</summary>
    public const int KeyP = 80;

    /// <summary>DOM key code of letter Q.</summary>
    public const int KeyQ = 81;

    /// <summary>DOM key code of letter R.</summary>
    public const int KeyR = 82;

    /// <summary>DOM key code of letter S.</summary>
    public const int KeyS = 83;

    /// <summary>DOM key code of letter T.</summary>
    public const int KeyT = 84;

    /// <summary>DOM key code of letter U.</summary>
    public const int KeyU = 85;

    /// <summary>DOM key code of letter V.</summary>
    public const int KeyV = 86;

    /// <summary>DOM key code of letter W.</summary>
    public const int KeyW = 87;

    /// <summary>DOM key code of letter X.</summary>
    public const int KeyX = 88;

    /// <summary>DOM key code of letter Y.</summary>
    public const int KeyY = 89;

    /// <summary>DOM key code of letter Z.</summary>
    public const int KeyZ = 90;

    /// <summary>DOM key code of F1.</summary>
    public const int F1 = 112;

    /// <summary>DOM key code of F2.</summary>
    public const int F2 = 113;

    /// <summary>DOM key code of F3.</summary>
    public const int F3 = 114;

    /// <summary>DOM key code of F4.</summary>
    public const int F4 = 115;

    /// <summary>DOM key code of F5.</summary>
    public const int F5 = 116;

    /// <summary>DOM key code of F6.</summary>
    public const int F6 = 117;

    /// <summary>DOM key code of F7.</summary>
    public const int F7 = 118;

    /// <summary>DOM key code of F8.</summary>
    public const int F8 = 119;

    /// <summary>DOM key code of F9.</summary>
    public const int F9 = 120;

    /// <summary>DOM key code of F10.</summary>
    public const int F10 = 121;

    /// <summary>DOM key code of F11.</summary>
    public const int F11 = 122;

    /// <summary>DOM key code of F12.</summary>
    public const int F12 = 123;

    /// <summary>DOM key code Semicolon.</summary>
    public const int Semicolon = 186;

    /// <summary>DOM key code Equal.</summary>
    public const int Equal = 187;

    /// <summary>DOM key code Comma.</summary>
    public const int Comma = 188;

    /// <summary>DOM key code Minus.</summary>
    public const int Minus = 189;

    /// <summary>DOM key code Period.</summary>
    public const int Period = 190;

    /// <summary>DOM key code Slash.</summary>
    public const int Slash = 191;

    /// <summary>DOM key code Backquote.</summary>
    public const int Backquote = 192;

    /// <summary>DOM key code BracketLeft.</summary>
    public const int BracketLeft = 219;

    /// <summary>DOM key code Backslash.</summary>
    public const int Backslash = 220;

    /// <summary>DOM key code BracketRight.</summary>
    public const int BracketRight = 221;

    /// <summary>DOM key code Quote.</summary>
    public const int Quote = 222;
}
