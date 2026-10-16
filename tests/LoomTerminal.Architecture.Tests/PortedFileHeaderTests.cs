namespace LoomTerminal.Architecture.Tests;

/// <summary>Files ported from (or inspired by) xterm.js must state the upstream commit they follow.</summary>
public class PortedFileHeaderTests
{
    private const string UpstreamCommitLine = "// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04";

    private static readonly string[] SearchedFolders = ["src", "tests"];

    [Fact]
    public void PortedFiles_StateTheUpstreamCommit()
    {
        string separator = Path.DirectorySeparatorChar.ToString();
        string[] offenders = SearchedFolders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(RepositoryLayout.Root.FullName, folder), "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.Contains(separator + "obj" + separator, StringComparison.Ordinal)
                && !file.Contains(separator + "bin" + separator, StringComparison.Ordinal))
            .Select(file => (File: file, Lines: File.ReadLines(file).Take(2).ToArray()))
            .Where(entry => entry.Lines.Length > 0
                && (entry.Lines[0].StartsWith("// Ported from xtermjs/xterm.js:", StringComparison.Ordinal)
                    || entry.Lines[0].StartsWith("// Inspired by xtermjs/xterm.js:", StringComparison.Ordinal))
                && (entry.Lines.Length < 2 || !entry.Lines[1].StartsWith(UpstreamCommitLine, StringComparison.Ordinal)))
            .Select(entry => Path.GetRelativePath(RepositoryLayout.Root.FullName, entry.File))
            .ToArray();

        Assert.True(offenders.Length == 0,
            $"These files must have '{UpstreamCommitLine}' as their second line: {string.Join(", ", offenders)}.");
    }
}
