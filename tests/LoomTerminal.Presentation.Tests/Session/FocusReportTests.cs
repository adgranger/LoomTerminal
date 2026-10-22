using LoomTerminal.Presentation.Session;

namespace LoomTerminal.Presentation.Tests.Session;

public class FocusReportTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static async Task WaitForInputAsync(FakePtyConnection fake, string expected)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (fake.Input != expected)
        {
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(10, cts.Token);
        }
    }

    [Fact]
    public async Task SetFocus_reports_focus_changes_when_mode_1004_is_on()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        session.SetFocus(false);
        fake.Emit("\u001b[?1004h");
        using var cts = new CancellationTokenSource(Timeout);
        while (!await session.RunOnParserThreadAsync(core => core.CoreService.DecPrivateModes.SendFocus))
        {
            await Task.Delay(10, cts.Token);
        }

        session.SetFocus(true);
        await WaitForInputAsync(fake, "\u001b[I");
        session.SetFocus(false);
        await WaitForInputAsync(fake, "\u001b[I\u001b[O");
    }

    [Fact]
    public async Task SetFocus_sends_nothing_when_mode_1004_is_off()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);

        session.SetFocus(false);
        session.SetFocus(true);
        await session.RunOnParserThreadAsync(_ => 0);

        Assert.Equal(string.Empty, fake.Input);
    }
}
