using LoomTerminal.Core;
using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Session;
using LoomTerminal.Presentation.Theming;
using LoomTerminal.Pty;

namespace LoomTerminal.Presentation.Tests.Session;

public class TerminalSessionTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (!await condition())
        {
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(10, cts.Token);
        }
    }

    private static string Screen(TerminalCore core)
    {
        var lines = new List<string>();
        for (var y = 0; y < core.Rows; y++)
        {
            lines.Add(core.Buffers.Active.Lines.Get(y)!.TranslateToString(true));
        }

        return string.Join('\n', lines);
    }

    [Fact]
    public async Task Shell_output_appears_in_the_emulator()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);

        fake.Emit("hello");

        await WaitUntilAsync(async () => (await session.RunOnParserThreadAsync(Screen)).StartsWith("hello", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendText_reaches_the_shell_as_utf8()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);

        session.SendText("ls\r é");

        await WaitUntilAsync(() => Task.FromResult(fake.Input == "ls\r é"));
    }

    [Fact]
    public async Task A_cursor_position_request_is_answered_on_the_shell_input()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);

        fake.Emit("\u001b[6n");

        await WaitUntilAsync(() => Task.FromResult(fake.Input == "\u001b[1;1R"));
    }

    [Fact]
    public async Task Resize_resizes_the_emulator_and_the_pty()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);

        session.Resize(100, 30);

        await WaitUntilAsync(async () => await session.RunOnParserThreadAsync(core => core.Cols == 100 && core.Rows == 30));
        Assert.Equal((100, 30), fake.LastResize);
    }

    [Fact]
    public async Task Resize_ignores_sizes_below_two_columns_or_one_row()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);

        session.Resize(1, 24);
        session.Resize(80, 0);

        Assert.Equal(80, await session.RunOnParserThreadAsync(core => core.Cols));
        Assert.Null(fake.LastResize);
    }

    [Fact]
    public async Task Exited_is_raised_with_the_exit_code_after_the_output_ended()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        var exited = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.Exited += exited.SetResult;

        fake.Exit(7);

        Assert.Equal(7, await exited.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WriteToDisplay_works_after_the_shell_exited()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        var exited = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.Exited += exited.SetResult;
        fake.Exit(0);
        await exited.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        session.WriteToDisplay("[exited 0]");

        Assert.StartsWith("[exited 0]", await session.RunOnParserThreadAsync(Screen), StringComparison.Ordinal);
        Assert.Equal(string.Empty, fake.Input);
    }

    [Fact]
    public async Task SendKey_uses_application_cursor_keys_once_the_application_enabled_them()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        fake.Emit("\u001b[?1h");
        await WaitUntilAsync(async () => await session.RunOnParserThreadAsync(core => core.CoreService.DecPrivateModes.ApplicationCursorKeys));

        // The modes snapshot is published after the batch that applied the mode.
        await WaitUntilAsync(() =>
        {
            session.SendKey(new TerminalKeyEvent(DomKeyCodes.ArrowUp, "ArrowUp", false, false, false, false, TerminalKeyEventType.KeyDown));
            return Task.FromResult(fake.Input.EndsWith("\u001bOA", StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task Paste_is_bracketed_when_the_application_enabled_bracketed_paste()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        fake.Emit("\u001b[?2004h");
        await WaitUntilAsync(async () => await session.RunOnParserThreadAsync(core => core.CoreService.DecPrivateModes.BracketedPasteMode));

        session.Paste("a\r\nb");

        await WaitUntilAsync(() => Task.FromResult(fake.Input == "\u001b[200~a\rb\u001b[201~"));
    }

    [Fact]
    public async Task An_OSC_4_color_query_is_answered_with_the_themed_color()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24, new TerminalSessionOptions { Theme = new TerminalTheme { Red = "#ff0000" } });

        fake.Emit("\u001b]4;1;?\u0007");

        await WaitUntilAsync(() => Task.FromResult(fake.Input == "\u001b]4;1;rgb:ffff/0000/0000\u001b\\"));
    }

    [Fact]
    public void Dispose_twice_is_safe()
    {
        var session = TerminalSession.Attach(new FakePtyConnection(), 80, 24);

        session.Dispose();
        session.Dispose();
    }

    [Fact]
    public async Task A_real_conpty_command_exits_and_its_output_reaches_the_screen()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "ConPTY is Windows only");
        using var session = TerminalSession.Start(new PtyStartOptions("cmd.exe /c echo hi"));
        var exited = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.Exited += exited.SetResult;

        Assert.Equal(0, await exited.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        Assert.Contains("hi", await session.RunOnParserThreadAsync(Screen), StringComparison.Ordinal);
    }
}
