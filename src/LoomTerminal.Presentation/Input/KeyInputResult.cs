namespace LoomTerminal.Presentation.Input;

/// <summary>What to do with a key event.</summary>
public enum KeyInputOutcome
{
    /// <summary>The host claimed the key (shortcut).</summary>
    HandledByHost,

    /// <summary>Send <see cref="KeyInputResult.Data"/> to the pty.</summary>
    Sent,

    /// <summary>Copy the selection.</summary>
    Copy,

    /// <summary>Paste the clipboard.</summary>
    Paste,

    /// <summary>The text input of the backend produces the character (printable keys, AltGr, IME).</summary>
    LeaveToTextInput,

    /// <summary>Nothing to do.</summary>
    Ignored,
}

/// <summary>Decision for one key event.</summary>
/// <param name="Outcome">What to do.</param>
/// <param name="Data">The text to send, for <see cref="KeyInputOutcome.Sent"/>.</param>
/// <remarks>Threading: immutable; any thread.</remarks>
internal readonly record struct KeyInputResult(KeyInputOutcome Outcome, string? Data = null);

/// <summary>What <see cref="KeyInputHandler"/> needs to know about the terminal and the platform.</summary>
/// <param name="ApplicationCursorKeys">Whether DECCKM is set.</param>
/// <param name="HasSelection">Whether text is selected.</param>
/// <param name="IsMac">Whether the host is macOS (false on Windows).</param>
/// <param name="MacOptionIsMeta">Whether Option acts as Meta on macOS.</param>
/// <remarks>Threading: immutable; any thread.</remarks>
internal readonly record struct KeyContext(
    bool ApplicationCursorKeys,
    bool HasSelection,
    bool IsMac = false,
    bool MacOptionIsMeta = false);
