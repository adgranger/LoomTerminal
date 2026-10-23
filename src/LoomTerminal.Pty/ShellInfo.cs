namespace LoomTerminal.Pty;

/// <summary>A shell installed on this machine.</summary>
/// <param name="Id">Stable identifier, suitable for settings (see <see cref="ShellIds"/>).</param>
/// <param name="Name">English display name ("PowerShell 7", "WSL: Ubuntu"); hosts may localize it from <paramref name="Id"/>.</param>
/// <param name="CommandLine">Command line to pass to <see cref="PtyStartOptions"/>.</param>
/// <remarks>Threading: immutable.</remarks>
public sealed record ShellInfo(string Id, string Name, string CommandLine)
{
    /// <summary>
    /// The executable file that runs, with Microsoft Store app execution aliases resolved to the real file;
    /// null when unknown (shells not created by <see cref="Shells.Detect"/>).
    /// </summary>
    public string? ExecutablePath { get; init; }

    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>Identifiers of the shells <see cref="Shells.Detect"/> knows how to find.</summary>
public static class ShellIds
{
    /// <summary>PowerShell 7 (<c>pwsh.exe</c>).</summary>
    public const string PowerShell7 = "pwsh";

    /// <summary>Windows PowerShell 5.1 (<c>powershell.exe</c>).</summary>
    public const string WindowsPowerShell = "powershell";

    /// <summary>The Bash of Git for Windows.</summary>
    public const string GitBash = "git-bash";

    /// <summary>Command Prompt (<c>cmd.exe</c>).</summary>
    public const string Cmd = "cmd";

    /// <summary>Prefix of a WSL distribution: <c>wsl:Ubuntu</c>.</summary>
    public const string WslPrefix = "wsl:";
}
