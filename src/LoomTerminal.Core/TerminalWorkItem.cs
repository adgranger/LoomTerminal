namespace LoomTerminal.Core;

/// <summary>An item of work for <see cref="TerminalWriter"/>: a chunk of pty output, or an action on the emulator.</summary>
/// <remarks>Threading: immutable; safe to pass between threads.</remarks>
internal readonly struct TerminalWorkItem
{
    private TerminalWorkItem(byte[]? data, Action<TerminalCore>? action)
    {
        Data = data;
        Action = action;
    }

    /// <summary>The UTF-8 output of the pty; null for an action.</summary>
    public byte[]? Data { get; }

    /// <summary>The action to run on the emulator; null for output.</summary>
    public Action<TerminalCore>? Action { get; }

    /// <summary>Creates an item that writes <paramref name="data"/> (UTF-8) to the emulator.</summary>
    public static TerminalWorkItem FromOutput(byte[] data) => new(data, null);

    /// <summary>Creates an item that runs <paramref name="action"/> on the emulator.</summary>
    public static TerminalWorkItem FromAction(Action<TerminalCore> action) => new(null, action);
}
