using System.Runtime.Versioning;
using LoomTerminal.Pty.Windows;

namespace LoomTerminal.Pty.Tests.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsShellLocatorTests
{
    [Fact]
    public void Detected_shells_have_unique_ids_and_the_command_prompt_is_there()
    {
        IReadOnlyList<ShellInfo> shells = Shells.Detect();

        Assert.Contains(shells, s => s.Id == ShellIds.Cmd);
        Assert.Equal(shells.Count, shells.Select(s => s.Id).Distinct().Count());
        Assert.All(shells, s => Assert.False(string.IsNullOrWhiteSpace(s.CommandLine)));
    }

    [Theory]
    [InlineData(@"C:\Program Files\Git\bin\bash.exe", "\"C:\\Program Files\\Git\\bin\\bash.exe\"")]
    [InlineData(@"C:\Windows\System32\cmd.exe", @"C:\Windows\System32\cmd.exe")]
    public void Paths_with_spaces_are_quoted(string path, string expected) =>
        Assert.Equal(expected, WindowsShellLocator.Quote(path));
}
