using System.Runtime.Versioning;
using System.Text;
using LoomTerminal.Pty.Windows;

namespace LoomTerminal.Pty.Tests.Windows;

[SupportedOSPlatform("windows")]
public sealed class AppExecutionAliasTests
{
    private const string Target = @"C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.7.0_x64__8wekyb3d8bbwe\pwsh.exe";

    private static byte[] Buffer(uint tag, params string[] strings)
    {
        List<byte> data = [.. BitConverter.GetBytes(3u)];
        foreach (string s in strings)
        {
            data.AddRange(Encoding.Unicode.GetBytes(s + "\0"));
        }

        return [.. BitConverter.GetBytes(tag), .. BitConverter.GetBytes((ushort)data.Count), 0, 0, .. data];
    }

    private static byte[] Alias() => Buffer(
        0x8000001B, "Microsoft.PowerShell_8wekyb3d8bbwe", "Microsoft.PowerShell_8wekyb3d8bbwe!App", Target, "0");

    [Fact]
    public void Parse_returns_the_target_of_an_app_execution_alias() =>
        Assert.Equal(Target, AppExecutionAlias.ParseTarget(Alias()));

    [Fact]
    public void Parse_returns_null_for_another_reparse_tag() =>
        Assert.Null(AppExecutionAlias.ParseTarget(Buffer(0xA000000C, "a", "b", Target, "0")));

    [Fact]
    public void Parse_returns_null_for_a_truncated_buffer()
    {
        byte[] valid = Alias();
        // Drops the trailing "0" string and the second half of the target (including its terminator).
        int cut = valid.Length - (2 * ("0".Length + 1)) - (2 * (Target.Length / 2 + 1));

        Assert.Null(AppExecutionAlias.ParseTarget(valid.AsSpan(0, cut)));
        Assert.Null(AppExecutionAlias.ParseTarget([]));
    }

    [Fact]
    public void Resolve_returns_a_regular_file_unchanged()
    {
        string path = Environment.ProcessPath!;

        Assert.Equal(path, AppExecutionAlias.Resolve(path));
    }

    [Fact]
    public void An_installed_alias_resolves_to_a_readable_file()
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps");
        // Aliases of system packages can point to files the user cannot see: take the first one that resolves to a file.
        string? alias = Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "*.exe").FirstOrDefault(f =>
                File.GetAttributes(f).HasFlag(FileAttributes.ReparsePoint)
                && AppExecutionAlias.Resolve(f) is var target && target != f && File.Exists(target))
            : null;
        if (alias is null)
        {
            Assert.Skip("No app execution alias installed.");
        }

        string resolved = AppExecutionAlias.Resolve(alias);

        Assert.NotEqual(alias, resolved);
        Assert.True(File.Exists(resolved));
        Assert.True(new FileInfo(resolved).Length > 0);
    }
}
