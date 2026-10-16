using System.Runtime.Versioning;
using Microsoft.Win32;

namespace LoomTerminal.Pty.Windows;

/// <summary>
/// Finds the Windows shells: PowerShell 7, Windows PowerShell, Git Bash, Command Prompt and the WSL distributions
/// (read from the registry: no process is started). Only installed shells are listed.
/// </summary>
/// <remarks>Threading: stateless. Ported from VIGO (<c>Vigo.Git/Terminal/WindowsShellLocator.cs</c>), same author, MIT.</remarks>
[SupportedOSPlatform("windows")]
internal static class WindowsShellLocator
{
    private const string LxssKey = @"Software\Microsoft\Windows\CurrentVersion\Lxss";

    public static List<ShellInfo> Detect()
    {
        List<ShellInfo> shells = [];

        string? pwsh = FindOnPath("pwsh.exe")
            ?? FirstExisting(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"));
        if (pwsh is not null)
        {
            shells.Add(new ShellInfo(ShellIds.PowerShell7, "PowerShell 7", $"{Quote(pwsh)} -NoLogo"));
        }

        string windowsPowerShell = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        if (File.Exists(windowsPowerShell))
        {
            shells.Add(new ShellInfo(ShellIds.WindowsPowerShell, "Windows PowerShell", $"{Quote(windowsPowerShell)} -NoLogo"));
        }

        if (FindGitBash() is { } bash)
        {
            shells.Add(new ShellInfo(ShellIds.GitBash, "Git Bash", $"{Quote(bash)} --login -i"));
        }

        string cmd = Environment.GetEnvironmentVariable("ComSpec") is { Length: > 0 } comSpec && File.Exists(comSpec)
            ? comSpec
            : Path.Combine(Environment.SystemDirectory, "cmd.exe");
        if (File.Exists(cmd))
        {
            shells.Add(new ShellInfo(ShellIds.Cmd, "Command Prompt", Quote(cmd)));
        }

        string wsl = Path.Combine(Environment.SystemDirectory, "wsl.exe");
        if (File.Exists(wsl))
        {
            foreach (string distribution in WslDistributions())
            {
                shells.Add(new ShellInfo(ShellIds.WslPrefix + distribution, $"WSL: {distribution}", $"{Quote(wsl)} -d {Quote(distribution)}"));
            }
        }

        return shells;
    }

    /// <summary>The path or argument between double quotes when it holds a space (CreateProcess command line).</summary>
    internal static string Quote(string value) => value.Contains(' ', StringComparison.Ordinal) ? $"\"{value}\"" : value;

    /// <summary>Installed WSL distributions, the default one first; the ones Docker Desktop keeps for itself are left out.</summary>
    private static List<string> WslDistributions()
    {
        List<string> names = [];
        try
        {
            using RegistryKey? root = Registry.CurrentUser.OpenSubKey(LxssKey);
            if (root is null)
            {
                return names;
            }

            string? defaultId = root.GetValue("DefaultDistribution") as string;
            string? defaultName = null;
            foreach (string id in root.GetSubKeyNames())
            {
                using RegistryKey? distribution = root.OpenSubKey(id);
                if (distribution?.GetValue("DistributionName") is not string name
                    || name.StartsWith("docker-desktop", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(id, defaultId, StringComparison.OrdinalIgnoreCase))
                {
                    defaultName = name;
                }
                else
                {
                    names.Add(name);
                }
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            if (defaultName is not null)
            {
                names.Insert(0, defaultName);
            }
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // No WSL information readable: no WSL entry.
        }

        return names;
    }

    /// <summary>The <c>bash.exe</c> of Git for Windows: next to the git.exe on the PATH, else in the usual install folders.</summary>
    private static string? FindGitBash()
    {
        if (FindOnPath("git.exe") is { } git && Path.GetDirectoryName(git) is { } folder)
        {
            // <root>\cmd\git.exe, <root>\bin\git.exe or <root>\mingw64\bin\git.exe
            DirectoryInfo? root = new(folder);
            for (int level = 0; level < 3 && root is not null; level++, root = root.Parent)
            {
                if (FirstExisting(Path.Combine(root.FullName, "bin", "bash.exe")) is { } found)
                {
                    return found;
                }
            }
        }

        return FirstExisting(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "bash.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "bin", "bash.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Git", "bin", "bash.exe"));
    }

    private static string? FirstExisting(params string[] paths) => paths.FirstOrDefault(File.Exists);

    private static string? FindOnPath(string fileName)
    {
        string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (string folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                string candidate = Path.Combine(folder.Trim('"'), fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
                // A malformed PATH entry.
            }
        }

        return null;
    }
}
