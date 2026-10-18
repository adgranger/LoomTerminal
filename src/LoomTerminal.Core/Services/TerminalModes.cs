// Ported from xtermjs/xterm.js: src/common/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Services;

/// <summary>The ANSI modes.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class Modes
{
    /// <summary>Insert mode (IRM).</summary>
    public bool InsertMode { get; set; }

    /// <summary>Creates the default modes (xterm.js freezes a default object; each service gets its own copy).</summary>
    public static Modes CreateDefault() => new();

    /// <summary>Creates a copy.</summary>
    public Modes Clone() => new() { InsertMode = InsertMode };
}

/// <summary>The DEC private modes.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class DecPrivateModes
{
    /// <summary>Application cursor keys (DECCKM).</summary>
    public bool ApplicationCursorKeys { get; set; }

    /// <summary>Application keypad (DECNKM).</summary>
    public bool ApplicationKeypad { get; set; }

    /// <summary>Bracketed paste mode.</summary>
    public bool BracketedPasteMode { get; set; }

    /// <summary>Cursor blink (set by DECSCUSR / mode 12); <see langword="null"/> until an application sets it.</summary>
    public bool? CursorBlink { get; set; }

    /// <summary>Cursor style (set by DECSCUSR); <see langword="null"/> until an application sets it.</summary>
    public CursorStyle? CursorStyle { get; set; }

    /// <summary>Origin mode (DECOM).</summary>
    public bool Origin { get; set; }

    /// <summary>Reverse wraparound mode.</summary>
    public bool ReverseWraparound { get; set; }

    /// <summary>Send focus events (mode 1004).</summary>
    public bool SendFocus { get; set; }

    /// <summary>Synchronized output (mode 2026).</summary>
    public bool SynchronizedOutput { get; set; }

    /// <summary>Wraparound mode (DECAWM); defaults: xterm - true, vt100 - false.</summary>
    public bool Wraparound { get; set; } = true;

    /// <summary>Creates the default modes (xterm.js freezes a default object; each service gets its own copy).</summary>
    public static DecPrivateModes CreateDefault() => new();

    /// <summary>Creates a copy.</summary>
    public DecPrivateModes Clone() => new()
    {
        ApplicationCursorKeys = ApplicationCursorKeys,
        ApplicationKeypad = ApplicationKeypad,
        BracketedPasteMode = BracketedPasteMode,
        CursorBlink = CursorBlink,
        CursorStyle = CursorStyle,
        Origin = Origin,
        ReverseWraparound = ReverseWraparound,
        SendFocus = SendFocus,
        SynchronizedOutput = SynchronizedOutput,
        Wraparound = Wraparound,
    };
}
