// Ported from xtermjs/xterm.js: src/common/services/OptionsService.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Services;

/// <summary>Identifies a property of <see cref="TerminalOptions"/>, in the order of its declaration.</summary>
internal enum TerminalOption
{
    /// <summary><see cref="TerminalOptions.Cols"/>.</summary>
    Cols,

    /// <summary><see cref="TerminalOptions.Rows"/>.</summary>
    Rows,

    /// <summary><see cref="TerminalOptions.CursorBlink"/>.</summary>
    CursorBlink,

    /// <summary><see cref="TerminalOptions.CursorStyle"/>.</summary>
    CursorStyle,

    /// <summary><see cref="TerminalOptions.CursorInactiveStyle"/>.</summary>
    CursorInactiveStyle,

    /// <summary><see cref="TerminalOptions.CursorWidth"/>.</summary>
    CursorWidth,

    /// <summary><see cref="TerminalOptions.DrawBoldTextInBrightColors"/>.</summary>
    DrawBoldTextInBrightColors,

    /// <summary><see cref="TerminalOptions.FastScrollModifier"/>.</summary>
    FastScrollModifier,

    /// <summary><see cref="TerminalOptions.FastScrollSensitivity"/>.</summary>
    FastScrollSensitivity,

    /// <summary><see cref="TerminalOptions.IgnoreBracketedPasteMode"/>.</summary>
    IgnoreBracketedPasteMode,

    /// <summary><see cref="TerminalOptions.LogLevel"/>.</summary>
    LogLevel,

    /// <summary><see cref="TerminalOptions.MacOptionIsMeta"/>.</summary>
    MacOptionIsMeta,

    /// <summary><see cref="TerminalOptions.MinimumContrastRatio"/>.</summary>
    MinimumContrastRatio,

    /// <summary><see cref="TerminalOptions.DisableStdin"/>.</summary>
    DisableStdin,

    /// <summary><see cref="TerminalOptions.RightClickSelectsWord"/>.</summary>
    RightClickSelectsWord,

    /// <summary><see cref="TerminalOptions.ScreenReaderMode"/>.</summary>
    ScreenReaderMode,

    /// <summary><see cref="TerminalOptions.Scrollback"/>.</summary>
    Scrollback,

    /// <summary><see cref="TerminalOptions.ScrollOnEraseInDisplay"/>.</summary>
    ScrollOnEraseInDisplay,

    /// <summary><see cref="TerminalOptions.ScrollOnUserInput"/>.</summary>
    ScrollOnUserInput,

    /// <summary><see cref="TerminalOptions.ScrollSensitivity"/>.</summary>
    ScrollSensitivity,

    /// <summary><see cref="TerminalOptions.TabStopWidth"/>.</summary>
    TabStopWidth,

    /// <summary><see cref="TerminalOptions.ReflowCursorLine"/>.</summary>
    ReflowCursorLine,

    /// <summary><see cref="TerminalOptions.WindowOptions"/>.</summary>
    WindowOptions,

    /// <summary><see cref="TerminalOptions.WindowsMode"/>.</summary>
    WindowsMode,

    /// <summary><see cref="TerminalOptions.WindowsPty"/>.</summary>
    WindowsPty,

    /// <summary><see cref="TerminalOptions.WordSeparator"/>.</summary>
    WordSeparator,

    /// <summary><see cref="TerminalOptions.AltClickMovesCursor"/>.</summary>
    AltClickMovesCursor,

    /// <summary><see cref="TerminalOptions.ConvertEol"/>.</summary>
    ConvertEol,

    /// <summary><see cref="TerminalOptions.TermName"/>.</summary>
    TermName,
}
