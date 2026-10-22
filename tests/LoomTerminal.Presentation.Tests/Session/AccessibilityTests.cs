using LoomTerminal.Presentation.Session;

namespace LoomTerminal.Presentation.Tests.Session;

public class AccessibilityTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static async Task WaitForFrameAfterAsync(FakePtyConnection fake, TerminalSession session, string text)
    {
        var before = session.FramesPublished;
        fake.Emit(text);
        using var cts = new CancellationTokenSource(Timeout);
        while (session.FramesPublished <= before)
        {
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(5, cts.Token);
        }
    }

    [Fact]
    public async Task Printed_text_is_announced_once_and_plain_lines_match_the_screen()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 20, 3);
        var announced = new List<string>();
        session.TextAnnounced += announced.Add;
        session.AccessibilityEnabled = true;
        await WaitForFrameAfterAsync(fake, session, string.Empty);

        await WaitForFrameAfterAsync(fake, session, "hi\r\n");

        Assert.Equal(["hi"], announced);
        var frame = session.TryTakeFrame();
        Assert.NotNull(frame);
        Assert.Equal(["hi", "", ""], frame.PlainLines);
    }

    [Fact]
    public async Task Nothing_is_announced_and_plain_lines_are_empty_when_accessibility_is_off()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 20, 3);
        var announced = 0;
        session.TextAnnounced += _ => announced++;

        await WaitForFrameAfterAsync(fake, session, "hi\r\n");

        Assert.Equal(0, announced);
        Assert.Empty(session.TryTakeFrame()!.PlainLines);
    }

    [Fact]
    public async Task A_typed_character_is_not_announced_again_when_the_shell_echoes_it()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 20, 3);
        var announced = new List<string>();
        session.TextAnnounced += announced.Add;
        session.AccessibilityEnabled = true;

        session.SendText("a");
        await WaitForFrameAfterAsync(fake, session, "ab");

        Assert.Equal(["b"], announced);
    }
}
