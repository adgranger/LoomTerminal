using System.Text.RegularExpressions;

namespace LoomTerminal.Architecture.Tests;

/// <summary>
/// The emulator and the presentation layer must stay portable (Avalonia on macOS and Linux): their source files do not
/// use Windows or WinUI namespaces or APIs. LoomTerminal.Pty may, but only in its per-OS folders (Windows/, Unix/).
/// </summary>
public partial class PlatformNeutralSourceTests
{
    public static TheoryData<string> PortableFolders => new()
    {
        "LoomTerminal.Core",
        "LoomTerminal.Presentation",
        // The public API of Pty is portable; only Windows/ talks to Win32 and Unix/ to the native shim.
        "LoomTerminal.Pty",
    };

    [GeneratedRegex(@"\b(using\s+(static\s+)?(Windows|Microsoft\.UI|Microsoft\.Windows|Microsoft\.Graphics|Microsoft\.Win32|WinRT|System\.Runtime\.InteropServices\.WindowsRuntime)\b|SupportedOSPlatform\s*\(\s*""windows|LibraryImport|DllImport)")]
    private static partial Regex WindowsSpecificApi();

    [Theory]
    [MemberData(nameof(PortableFolders))]
    public void PortableProject_SourceFiles_DoNotUseWindowsApis(string projectName)
    {
        string folder = Path.Combine(RepositoryLayout.Source.FullName, projectName);
        string separator = Path.DirectorySeparatorChar.ToString();

        string[] offenders = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(separator + "obj" + separator, StringComparison.Ordinal)
                && !file.Contains(separator + "bin" + separator, StringComparison.Ordinal)
                && !file.Contains(separator + "Windows" + separator, StringComparison.Ordinal)
                && !file.Contains(separator + "Unix" + separator, StringComparison.Ordinal))
            .Where(file => WindowsSpecificApi().IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(RepositoryLayout.Root.FullName, file))
            .ToArray();

        Assert.True(offenders.Length == 0,
            $"{projectName} must stay platform neutral outside Windows/, but these files use Windows APIs: {string.Join(", ", offenders)}.");
    }
}
