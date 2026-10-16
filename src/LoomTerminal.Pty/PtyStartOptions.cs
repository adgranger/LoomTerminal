namespace LoomTerminal.Pty;

/// <summary>What to start behind a pseudo terminal, and how.</summary>
/// <param name="CommandLine">
/// Full command line: executable (quoted when its path holds a space) followed by its arguments,
/// for example <c>"C:\Program Files\PowerShell\7\pwsh.exe" -NoLogo</c>. See <see cref="ShellInfo.CommandLine"/>.
/// </param>
/// <remarks>Threading: immutable.</remarks>
public sealed record PtyStartOptions(string CommandLine)
{
    /// <summary>Current directory of the shell; <see langword="null"/> for the current directory of this process.</summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>Initial number of columns.</summary>
    public int Columns { get; init; } = 80;

    /// <summary>Initial number of rows.</summary>
    public int Rows { get; init; } = 24;

    /// <summary>
    /// Changes to the environment the shell inherits from this process, applied after the defaults
    /// (<c>TERM=xterm-256color</c>, <c>COLORTERM=truecolor</c>). A <see langword="null"/> value removes the variable.
    /// Hosts usually set <c>TERM_PROGRAM</c> here.
    /// </summary>
    public IReadOnlyDictionary<string, string?>? Environment { get; init; }
}
