using System.Runtime.Versioning;
using System.Text;

namespace LoomTerminal.Pty.Unix;

/// <summary>Finds the shells of a Linux or macOS machine: the login shell first, then the rest of <c>/etc/shells</c>.</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class UnixShellLocator
{
    private const string EtcShells = "/etc/shells";

    /// <summary>Detects the shells of this machine.</summary>
    /// <returns>The shells, login shell first, without duplicates; empty when none is found.</returns>
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    public static IReadOnlyList<ShellInfo> Detect()
    {
        string[] listed = File.Exists(EtcShells) ? File.ReadAllLines(EtcShells) : [];
        return Build(Environment.GetEnvironmentVariable("SHELL"), UnixPtyNative.LoginShell(), listed, File.Exists);
    }

    /// <summary>The detection itself, on explicit inputs.</summary>
    /// <param name="shellVariable">The <c>SHELL</c> environment variable, if set.</param>
    /// <param name="loginShell">The shell of the user in the passwd database, if known.</param>
    /// <param name="etcShellsLines">The lines of <c>/etc/shells</c> (comments and blanks are skipped).</param>
    /// <param name="exists">Whether a file exists.</param>
    /// <returns>The shells; the first one of each id wins, so the login shell shadows the other builds of the same shell.</returns>
    internal static List<ShellInfo> Build(string? shellVariable, string? loginShell, IEnumerable<string> etcShellsLines, Func<string, bool> exists)
    {
        var candidates = new List<string>();
        foreach (string? path in new[] { shellVariable, loginShell }.Concat(etcShellsLines.Select(line => line.Trim()).Where(line => line.StartsWith('/'))))
        {
            if (!string.IsNullOrWhiteSpace(path) && Path.IsPathRooted(path))
            {
                candidates.Add(path);
            }
        }

        var shells = new List<ShellInfo>();
        foreach (string path in candidates)
        {
            // /usr/bin/false, /sbin/nologin and the like are listed in /etc/shells on some systems but are not shells.
            string file = Path.GetFileName(path);
            if (file is "false" or "nologin" or "true" || !exists(path) || shells.Any(shell => shell.Id == file))
            {
                continue;
            }

            string name = file == ShellIds.PowerShell7 ? "PowerShell 7" : file;
            // Login shells, like macOS Terminal and most Linux terminals; sh takes no flag.
            string commandLine = file == ShellIds.Sh ? Quote(path) : $"{Quote(path)} -l";
            shells.Add(new ShellInfo(file, name, commandLine) { ExecutablePath = path });
        }

        return shells;
    }

    // POSIX single quotes: a path with a space or a quote survives PosixCommandLine.Split.
    private static string Quote(string path) =>
        path.All(c => char.IsAsciiLetterOrDigit(c) || c is '/' or '.' or '_' or '-' or '+')
            ? path
            : new StringBuilder("'").Append(path.Replace("'", "'\\''", StringComparison.Ordinal)).Append('\'').ToString();
}
