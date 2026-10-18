// Ported from xtermjs/xterm.js: src/common/services/OptionsService.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Services;

/// <summary>The shape of the cursor.</summary>
internal enum CursorStyle
{
    /// <summary>A filled block (the default).</summary>
    Block = 0,

    /// <summary>An underline.</summary>
    Underline = 1,

    /// <summary>A vertical bar.</summary>
    Bar = 2,
}

/// <summary>The shape of the cursor when the terminal is not focused.</summary>
internal enum CursorInactiveStyle
{
    /// <summary>An outlined block (the default).</summary>
    Outline = 0,

    /// <summary>A filled block.</summary>
    Block = 1,

    /// <summary>A vertical bar.</summary>
    Bar = 2,

    /// <summary>An underline.</summary>
    Underline = 3,

    /// <summary>No cursor.</summary>
    None = 4,
}

/// <summary>The log level of the terminal.</summary>
internal enum LogLevel
{
    /// <summary>Trace.</summary>
    Trace = 0,

    /// <summary>Debug.</summary>
    Debug = 1,

    /// <summary>Info (the default).</summary>
    Info = 2,

    /// <summary>Warn.</summary>
    Warn = 3,

    /// <summary>Error.</summary>
    Error = 4,

    /// <summary>Off.</summary>
    Off = 5,
}

/// <summary>The modifier key that speeds up scrolling.</summary>
internal enum FastScrollModifier
{
    /// <summary>No modifier.</summary>
    None = 0,

    /// <summary>Alt (the default).</summary>
    Alt = 1,

    /// <summary>Ctrl.</summary>
    Ctrl = 2,

    /// <summary>Shift.</summary>
    Shift = 3,
}

/// <summary>The window manipulation sequences (CSI t) the terminal accepts. All disabled by default.</summary>
internal sealed record WindowOptions
{
    /// <summary>Restore the window (CSI 1 t).</summary>
    public bool RestoreWin { get; init; }

    /// <summary>Minimize the window (CSI 2 t).</summary>
    public bool MinimizeWin { get; init; }

    /// <summary>Move the window (CSI 3 ; x ; y t).</summary>
    public bool SetWinPosition { get; init; }

    /// <summary>Resize the window in pixels (CSI 4 ; height ; width t).</summary>
    public bool SetWinSizePixels { get; init; }

    /// <summary>Raise the window (CSI 5 t).</summary>
    public bool RaiseWin { get; init; }

    /// <summary>Lower the window (CSI 6 t).</summary>
    public bool LowerWin { get; init; }

    /// <summary>Refresh the window (CSI 7 t).</summary>
    public bool RefreshWin { get; init; }

    /// <summary>Resize the text area in characters (CSI 8 ; rows ; cols t).</summary>
    public bool SetWinSizeChars { get; init; }

    /// <summary>Maximize the window (CSI 9 t).</summary>
    public bool MaximizeWin { get; init; }

    /// <summary>Toggle full screen (CSI 10 t).</summary>
    public bool FullscreenWin { get; init; }

    /// <summary>Report the window state (CSI 11 t).</summary>
    public bool GetWinState { get; init; }

    /// <summary>Report the window position (CSI 13 t).</summary>
    public bool GetWinPosition { get; init; }

    /// <summary>Report the window size in pixels (CSI 14 t).</summary>
    public bool GetWinSizePixels { get; init; }

    /// <summary>Report the screen size in pixels (CSI 15 t).</summary>
    public bool GetScreenSizePixels { get; init; }

    /// <summary>Report the cell size in pixels (CSI 16 t).</summary>
    public bool GetCellSizePixels { get; init; }

    /// <summary>Report the text area size in characters (CSI 18 t).</summary>
    public bool GetWinSizeChars { get; init; }

    /// <summary>Report the screen size in characters (CSI 19 t).</summary>
    public bool GetScreenSizeChars { get; init; }

    /// <summary>Report the icon label (CSI 20 t).</summary>
    public bool GetIconTitle { get; init; }

    /// <summary>Report the window title (CSI 21 t).</summary>
    public bool GetWinTitle { get; init; }

    /// <summary>Push the title on the stack (CSI 22 t).</summary>
    public bool PushTitle { get; init; }

    /// <summary>Pop the title from the stack (CSI 23 t).</summary>
    public bool PopTitle { get; init; }

    /// <summary>Resize the window to a number of lines (CSI Ps t, Ps >= 24).</summary>
    public bool SetWinLines { get; init; }
}

/// <summary>
/// Describes the pseudoterminal behind the terminal, so that the emulator can mimic its quirks. With
/// <c>{ Backend = "conpty", BuildNumber = 21376 or more }</c> (every supported Windows 11) the wrapping heuristics are
/// off and reflow is on, exactly as in xterm.js: ConPTY marks wrapped lines itself and the emulator reflows with it.
/// </summary>
/// <param name="Backend">The backend: <c>"conpty"</c> or <c>"winpty"</c>; <see langword="null"/> when unknown.</param>
/// <param name="BuildNumber">The Windows build number; <see langword="null"/> when unknown.</param>
internal sealed record WindowsPtyOptions(string? Backend = null, int? BuildNumber = null);

/// <summary>The options the emulator reads (the Core subset of the xterm.js terminal options). Immutable.</summary>
/// <remarks>Threading: immutable; safe to share between threads.</remarks>
internal sealed record TerminalOptions
{
    /// <summary>The options with their default values.</summary>
    public static TerminalOptions Default { get; } = new();

    /// <summary>The number of columns.</summary>
    public int Cols { get; init; } = 80;

    /// <summary>The number of rows.</summary>
    public int Rows { get; init; } = 24;

    /// <summary>Whether the cursor blinks.</summary>
    public bool CursorBlink { get; init; }

    /// <summary>The cursor style.</summary>
    public CursorStyle CursorStyle { get; init; } = CursorStyle.Block;

    /// <summary>The cursor style when the terminal is not focused.</summary>
    public CursorInactiveStyle CursorInactiveStyle { get; init; } = CursorInactiveStyle.Outline;

    /// <summary>The width of the cursor in CSS pixels when the style is <see cref="CursorStyle.Bar"/>.</summary>
    public double CursorWidth { get; init; } = 1;

    /// <summary>Whether bold text is drawn in bright colors.</summary>
    public bool DrawBoldTextInBrightColors { get; init; } = true;

    /// <summary>The modifier that speeds up scrolling.</summary>
    public FastScrollModifier FastScrollModifier { get; init; } = FastScrollModifier.Alt;

    /// <summary>The scroll speed multiplier used while the fast scroll modifier is held.</summary>
    public double FastScrollSensitivity { get; init; } = 5;

    /// <summary>Whether bracketed paste mode is ignored.</summary>
    public bool IgnoreBracketedPasteMode { get; init; }

    /// <summary>The log level.</summary>
    public LogLevel LogLevel { get; init; } = LogLevel.Info;

    /// <summary>Whether the Option key acts as Meta.</summary>
    public bool MacOptionIsMeta { get; init; }

    /// <summary>The minimum contrast ratio between foreground and background, from 1 to 21.</summary>
    public double MinimumContrastRatio { get; init; } = 1;

    /// <summary>Whether input is disabled.</summary>
    public bool DisableStdin { get; init; }

    /// <summary>Whether a right click selects the word under the cursor.</summary>
    public bool RightClickSelectsWord { get; init; }

    /// <summary>Whether the screen reader mode is on.</summary>
    public bool ScreenReaderMode { get; init; }

    /// <summary>The number of rows kept in the scrollback buffer.</summary>
    public long Scrollback { get; init; } = 1000;

    /// <summary>Whether erasing the display scrolls its content into the scrollback.</summary>
    public bool ScrollOnEraseInDisplay { get; init; }

    /// <summary>Whether user input scrolls to the bottom.</summary>
    public bool ScrollOnUserInput { get; init; } = true;

    /// <summary>The scroll speed multiplier.</summary>
    public double ScrollSensitivity { get; init; } = 1;

    /// <summary>The distance between tab stops.</summary>
    public int TabStopWidth { get; init; } = 8;

    /// <summary>Whether the cursor line is reflowed on resize.</summary>
    public bool ReflowCursorLine { get; init; }

    /// <summary>The window manipulation sequences the terminal accepts.</summary>
    public WindowOptions WindowOptions { get; init; } = new();

    /// <summary>Whether Windows mode (reflow heuristics for ConPTY) is on.</summary>
    public bool WindowsMode { get; init; }

    /// <summary>The pseudoterminal description; never <see langword="null"/> once validated.</summary>
    public WindowsPtyOptions? WindowsPty { get; init; } = new();

    /// <summary>The characters that delimit words for word selection.</summary>
    public string WordSeparator { get; init; } = " ()[]{}',\"`";

    /// <summary>Whether Alt+click moves the cursor.</summary>
    public bool AltClickMovesCursor { get; init; } = true;

    /// <summary>Whether a line feed also performs a carriage return.</summary>
    public bool ConvertEol { get; init; }

    /// <summary>The terminal name reported to applications.</summary>
    public string TermName { get; init; } = "xterm";
}
