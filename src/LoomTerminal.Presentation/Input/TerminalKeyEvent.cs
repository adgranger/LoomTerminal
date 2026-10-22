namespace LoomTerminal.Presentation.Input;

/// <summary>Kind of a <see cref="TerminalKeyEvent"/>.</summary>
public enum TerminalKeyEventType
{
    /// <summary>A key was pressed.</summary>
    KeyDown,

    /// <summary>A character-producing key press.</summary>
    KeyPress,
}

/// <summary>
/// Backend-neutral key event (ADR 0004). The physical key (<c>code</c>) is deliberately absent: only
/// <see cref="KeyCode"/> is used.
/// </summary>
/// <param name="KeyCode">DOM-style key code (see <see cref="DomKeyCodes"/>).</param>
/// <param name="Key">DOM-style key value (<c>"a"</c>, <c>"Enter"</c>, …).</param>
/// <param name="Ctrl">Whether Ctrl is held.</param>
/// <param name="Alt">Whether Alt is held.</param>
/// <param name="Shift">Whether Shift is held.</param>
/// <param name="Meta">Whether Meta (Windows/Cmd) is held.</param>
/// <param name="Type">Kind of event.</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record TerminalKeyEvent(
    int KeyCode,
    string Key,
    bool Ctrl,
    bool Alt,
    bool Shift,
    bool Meta,
    TerminalKeyEventType Type)
{
    /// <summary>Set by backends when AltGr is pressed (it arrives as Ctrl+Alt on Windows).</summary>
    public bool AltGraph { get; init; }
}
