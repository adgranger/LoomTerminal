using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace LoomTerminal.Pty.Tests.Windows;

/// <summary>Ported from VIGO (<c>tests/Vigo.Git.Tests/ConPtySessionTests.cs</c>), through the public API.</summary>
[SupportedOSPlatform("windows10.0.17763")]
public sealed class ConPtyConnectionTests
{
    private static readonly string Cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact(Skip = "Windows only", SkipUnless = nameof(Platform.IsWindows), SkipType = typeof(Platform))]
    public async Task Command_output_comes_back_and_exit_code_is_reported()
    {
        using IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions($"\"{Cmd}\" /c echo loom-hello & exit 3"));

        string output = await ReadUntilAsync(pty, "loom-hello");

        Assert.Contains("loom-hello", output, StringComparison.Ordinal);
        Assert.Equal(3, await pty.Exited.WaitAsync(TimeSpan.FromSeconds(20), Ct));
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(Platform.IsWindows), SkipType = typeof(Platform))]
    public async Task Typed_input_reaches_the_shell()
    {
        using IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions($"\"{Cmd}\""));
        await ReadUntilAsync(pty, ">");

        pty.Write("echo loom-$((6*7))x\r"u8);
        // cmd does not evaluate $(( )): the echoed line appears as typed, which proves the input path.
        string output = await ReadUntilAsync(pty, "loom-$((6*7))x");

        Assert.Contains("loom-$((6*7))x", output, StringComparison.Ordinal);
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(Platform.IsWindows), SkipType = typeof(Platform))]
    public async Task Working_directory_is_the_one_given()
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("loom-conpty-");
        try
        {
            using IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions($"\"{Cmd}\" /c cd") { WorkingDirectory = directory.FullName });

            string output = await ReadUntilAsync(pty, directory.Name);

            Assert.Contains(directory.Name, output, StringComparison.Ordinal);
            // The shell holds the folder as its current directory until it is gone.
            await pty.Exited.WaitAsync(TimeSpan.FromSeconds(20), Ct);
        }
        finally
        {
            await DeleteWithRetriesAsync(directory);
        }
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(Platform.IsWindows), SkipType = typeof(Platform))]
    public async Task Environment_changes_reach_the_shell()
    {
        var options = new PtyStartOptions($"\"{Cmd}\" /c echo [%TERM%][%LOOM_TEST%][%TERM_PROGRAM%]")
        {
            Environment = new Dictionary<string, string?> { ["LOOM_TEST"] = "on", ["TERM_PROGRAM"] = "Loom" },
        };
        using IPtyConnection pty = PseudoTerminal.Start(options);

        string output = await ReadUntilAsync(pty, "[Loom]");

        Assert.Contains("[xterm-256color][on][Loom]", output, StringComparison.Ordinal);
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(Platform.IsWindows), SkipType = typeof(Platform))]
    public async Task Resize_is_seen_by_the_shell()
    {
        using IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions($"\"{Cmd}\""));
        await ReadUntilAsync(pty, ">");

        pty.Resize(120, 40);
        pty.Write("mode con\r"u8);
        string output = await ReadUntilAsync(pty, "120");

        Assert.Contains("120", output, StringComparison.Ordinal);
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(Platform.IsWindows), SkipType = typeof(Platform))]
    public async Task Disposing_kills_the_shell_and_what_it_started()
    {
        // The shell starts a child that outlives it unless the job object kills it.
        IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions($"\"{Cmd}\" /c ping -n 120 127.0.0.1 -w 1000 > nul"));
        Process? ping = await WaitForNewPingAsync();
        Assert.NotNull(ping);

        pty.Dispose();

        bool exited = await ping.WaitForExitAsync(Ct).WaitAsync(TimeSpan.FromSeconds(20), Ct).ContinueWith(t => t.IsCompletedSuccessfully, Ct);
        Assert.True(exited);
        ping.Dispose();
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(Platform.IsWindows), SkipType = typeof(Platform))]
    public void A_missing_program_fails_with_IOException()
    {
        Assert.Throws<IOException>(() => PseudoTerminal.Start(new PtyStartOptions(@"C:\this\does\not\exist.exe")));
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

    // The ping started by the test shell: the newest ping.exe, found by polling.
    private static async Task<Process?> WaitForNewPingAsync()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            Process? started = Process.GetProcessesByName("ping").OrderByDescending(p => p.StartTime).FirstOrDefault();
            if (started is not null && DateTime.Now - started.StartTime < TimeSpan.FromSeconds(30))
            {
                return started;
            }

            await Task.Delay(100, Ct);
        }

        return null;
    }

    private static async Task DeleteWithRetriesAsync(DirectoryInfo directory)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                directory.Delete(recursive: true);
                return;
            }
            catch (IOException)
            {
                // Released a moment after the process exit.
                await Task.Delay(100, Ct);
            }
        }
    }
}
