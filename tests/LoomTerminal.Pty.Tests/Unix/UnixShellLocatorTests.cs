using System.Runtime.Versioning;
using LoomTerminal.Pty.Unix;

namespace LoomTerminal.Pty.Tests.Unix;

/// <summary>The ordering and naming rules run everywhere; the last test looks at the real machine (Linux and macOS only).</summary>
public sealed class UnixShellLocatorTests
{
    private static readonly string[] EtcShells =
    [
        "# /etc/shells: valid login shells",
        "/bin/sh", "/bin/bash", "/usr/bin/bash", "/usr/bin/zsh", "/usr/sbin/nologin", "/usr/bin/fish", "",
    ];

    [Fact]
    public void The_login_shell_comes_first_and_the_other_listed_shells_follow_without_duplicates()
    {
        List<ShellInfo> shells = UnixShellLocator.Build("/usr/bin/zsh", "/bin/bash", EtcShells, _ => true);

        Assert.Equal(["zsh", "bash", "sh", "fish"], shells.Select(shell => shell.Id));
        Assert.Equal("/usr/bin/zsh", shells[0].ExecutablePath);
    }

    [Fact]
    public void The_passwd_shell_is_used_when_SHELL_is_not_set()
    {
        List<ShellInfo> shells = UnixShellLocator.Build(null, "/usr/bin/fish", EtcShells, _ => true);

        Assert.Equal("fish", shells[0].Id);
    }

    [Fact]
    public void Shells_start_as_login_shells_except_sh_and_paths_are_quoted()
    {
        List<ShellInfo> shells = UnixShellLocator.Build(null, null, ["/bin/sh", "/bin/bash", "/opt/my shells/zsh", "/opt/pwsh"], _ => true);

        Assert.Equal("/bin/sh", shells.Single(s => s.Id == "sh").CommandLine);
        Assert.Equal("/bin/bash -l", shells.Single(s => s.Id == "bash").CommandLine);
        Assert.Equal("'/opt/my shells/zsh' -l", shells.Single(s => s.Id == "zsh").CommandLine);
        Assert.Equal("PowerShell 7", shells.Single(s => s.Id == ShellIds.PowerShell7).Name);
        Assert.Equal(["/opt/my shells/zsh", "-l"], PosixCommandLine.Split(shells.Single(s => s.Id == "zsh").CommandLine));
    }

    [Fact]
    public void Missing_files_and_non_shells_are_left_out()
    {
        List<ShellInfo> shells = UnixShellLocator.Build(null, null, ["/bin/bash", "/usr/bin/false", "/bin/zsh"], path => path == "/bin/bash");

        Assert.Equal(["bash"], shells.Select(shell => shell.Id));
    }

    [Fact]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    public void This_machine_has_shells_with_unique_ids_and_the_login_shell_first()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "Linux and macOS only.");

        IReadOnlyList<ShellInfo> shells = Shells.Detect();

        Assert.NotEmpty(shells);
        Assert.Equal(shells.Count, shells.Select(shell => shell.Id).Distinct().Count());
        string? login = Environment.GetEnvironmentVariable("SHELL");
        if (login is not null && File.Exists(login))
        {
            Assert.Equal(login, shells[0].ExecutablePath);
        }
    }
}
