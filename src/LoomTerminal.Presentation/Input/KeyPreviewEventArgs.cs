namespace LoomTerminal.Presentation.Input;

/// <summary>Lets the host claim a key event before the terminal handles it.</summary>
/// <remarks>Threading: raised on the thread that forwards the key event (the UI thread); do not share it.</remarks>
public sealed class KeyPreviewEventArgs : EventArgs
{
    /// <summary>Creates the arguments for <paramref name="key"/>.</summary>
    /// <param name="key">The key event about to be handled.</param>
    public KeyPreviewEventArgs(TerminalKeyEvent key)
    {
        Key = key;
    }

    /// <summary>The key event about to be handled.</summary>
    public TerminalKeyEvent Key { get; }

    /// <summary>Set to <see langword="true"/> to claim the key: the terminal then ignores it.</summary>
    public bool Handled { get; set; }
}
