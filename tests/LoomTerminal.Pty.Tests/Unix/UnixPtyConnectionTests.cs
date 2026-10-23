using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace LoomTerminal.Pty.Tests.Unix;

/// <summary>The ConPTY tests, through the public API, with <c>/bin/sh</c>. They run on Linux and macOS only.</summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed partial class UnixPtyConnectionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static void RequireUnix() =>
        Assert.SkipUnless(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "The Unix pty needs Linux or macOS.");

    [Fact]
    public async Task Command_output_comes_back_and_exit_code_is_reported()
    {
        RequireUnix();
        using IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions("/bin/sh -c 'echo loom-hello; exit 3'"));

        string output = await ReadUntilAsync(pty, "loom-hello");

        Assert.Contains("loom-hello", output, StringComparison.Ordinal);
        Assert.Equal(3, await pty.Exited.WaitAsync(TimeSpan.FromSeconds(20), Ct));
    }

    [Fact]
    public async Task Typed_input_reaches_the_shell()
    {
        RequireUnix();
        using IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions("/bin/sh"));

        pty.Write("echo loom-$((6*7))x\n"u8);
        // The typed line is echoed with "$((6*7))"; only the shell prints the evaluated result.
        string output = await ReadUntilAsync(pty, "loom-42x");

        Assert.Contains("loom-42x", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Working_directory_is_the_one_given()
    {
        RequireUnix();
        DirectoryInfo directory = Directory.CreateTempSubdirectory("loom-pty-");
        try
        {
            using IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions("/bin/sh -c pwd") { WorkingDirectory = directory.FullName });

            string output = await ReadUntilAsync(pty, directory.Name);

            Assert.Contains(directory.Name, output, StringComparison.Ordinal);
            await pty.Exited.WaitAsync(TimeSpan.FromSeconds(20), Ct);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Environment_changes_reach_the_shell()
    {
        RequireUnix();
        var options = new PtyStartOptions("/bin/sh -c 'echo [$TERM][$COLORTERM][$LOOM_TEST][$TERM_PROGRAM]'")
        {
            Environment = new Dictionary<string, string?> { ["LOOM_TEST"] = "on", ["TERM_PROGRAM"] = "Loom" },
        };
        using IPtyConnection pty = PseudoTerminal.Start(options);

        string output = await ReadUntilAsync(pty, "[Loom]");

        Assert.Contains("[xterm-256color][truecolor][on][Loom]", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resize_is_seen_by_the_shell()
    {
        RequireUnix();
        using IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions("/bin/sh"));

        pty.Resize(120, 40);
        pty.Write("stty size\n"u8);
        string output = await ReadUntilAsync(pty, "40 120");

        Assert.Contains("40 120", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Disposing_kills_the_shell_and_what_it_started()
    {
        RequireUnix();
        // The shell starts a child that outlives it unless the whole process group is killed.
        IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions("/bin/sh -c 'sleep 120 & echo child=$!; wait'"));
        string output = await ReadUntilAsync(pty, "child=");
        string childText = await ReadDigitsAsync(pty, output);
        int child = int.Parse(childText, System.Globalization.CultureInfo.InvariantCulture);
        using Process sleeper = Process.GetProcessById(child);

        pty.Dispose();

        await sleeper.WaitForExitAsync(Ct).WaitAsync(TimeSpan.FromSeconds(20), Ct);
        Assert.True(sleeper.HasExited);
    }

    [Fact]
    public void A_missing_program_fails_with_IOException()
    {
        RequireUnix();
        Assert.Throws<IOException>(() => PseudoTerminal.Start(new PtyStartOptions("/this/does/not/exist")));
        Assert.Throws<IOException>(() => PseudoTerminal.Start(new PtyStartOptions("loom-no-such-program-anywhere")));
    }

    [GeneratedRegex(@"child=(\d+)")]
    private static partial Regex ChildPid();

    // The pid is printed in one write, so it is already in the text read so far; read on only if it is not.
    private static async Task<string> ReadDigitsAsync(IPtyConnection pty, string soFar)
    {
        string text = soFar;
        for (int attempt = 0; attempt < 50 && !ChildPid().IsMatch(text); attempt++)
        {
            text += await ReadUntilAsync(pty, "\n");
        }

        return ChildPid().Match(text).Groups[1].Value;
    }

    /// <summary>Reads the output until <paramref name="text"/> appears (or the output ends, or 20 s pass).</summary>
    private static async Task<string> ReadUntilAsync(IPtyConnection pty, string text)
    {
        var all = new StringBuilder();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            await foreach (byte[] chunk in pty.Output.ReadAllAsync(timeout.Token))
            {
                all.Append(Encoding.UTF8.GetString(chunk));
                if (all.ToString().Contains(text, StringComparison.Ordinal))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (!Ct.IsCancellationRequested)
        {
            // Reported by the assertion with what was read.
        }

        return all.ToString();
    }
}
