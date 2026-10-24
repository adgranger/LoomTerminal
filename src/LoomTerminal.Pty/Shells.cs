using LoomTerminal.Pty.Unix;
using LoomTerminal.Pty.Windows;

namespace LoomTerminal.Pty;

/// <summary>Finds the shells installed on this machine.</summary>
/// <remarks>Threading: every member can be called from any thread.</remarks>
public static class Shells
{
    /// <summary>
    /// Installed shells, in a sensible menu order (on Windows: PowerShell 7, Windows PowerShell, Git Bash,
    /// Command Prompt, then the WSL distributions, default first; on Linux and macOS: the login shell, then the other
    /// entries of <c>/etc/shells</c>, started as login shells). No process is started.
    /// </summary>
    /// <returns>The shells found; empty on an unsupported operating system.</returns>
    public static IReadOnlyList<ShellInfo> Detect() =>
        OperatingSystem.IsWindows() ? WindowsShellLocator.Detect()
        : OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ? UnixShellLocator.Detect()
        : [];
}
