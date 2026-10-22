using System.Text.RegularExpressions;
using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Rendering;
using LoomTerminal.Presentation.Session;

namespace LoomTerminal.Presentation.Tests.Session;

public partial class TerminalSessionFrameTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [GeneratedRegex(@"(\d):(\d+);")]
    private static partial Regex TokenRegex();

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (!condition())
        {
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(5, cts.Token);
        }
    }

    // Plays a chunk of output and waits until the session published a frame for it.
    private static async Task EmitAndWaitForFrameAsync(FakePtyConnection fake, TerminalSession session, string text)
    {
        var before = session.FramesPublished;
        fake.Emit(text);
        await WaitUntilAsync(() => session.FramesPublished > before);
    }

    private static string FirstLine(RenderFrame frame) => string.Concat(frame.Lines[0].Runs.Select(r => r.Text));

    [Fact]
    public async Task Ten_batches_raise_FrameAvailable_once_and_the_frame_holds_the_text()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        var raised = 0;
        session.FrameAvailable += () => Interlocked.Increment(ref raised);

        for (var i = 0; i < 10; i++)
        {
            await EmitAndWaitForFrameAsync(fake, session, i.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        Assert.Equal(1, raised);
        var frame = session.TryTakeFrame();
        Assert.NotNull(frame);
        Assert.Equal("0123456789", FirstLine(frame));
        Assert.Null(session.TryTakeFrame());
    }

    [Fact]
    public async Task A_new_frame_after_taking_one_raises_FrameAvailable_again()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        var raised = 0;
        session.FrameAvailable += () => Interlocked.Increment(ref raised);
        await EmitAndWaitForFrameAsync(fake, session, "a");
        Assert.NotNull(session.TryTakeFrame());

        await EmitAndWaitForFrameAsync(fake, session, "b");

        Assert.Equal(2, raised);
    }

    [Fact]
    public async Task A_published_frame_is_not_modified_by_later_output()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        await EmitAndWaitForFrameAsync(fake, session, "first");
        var taken = session.TryTakeFrame()!;

        await EmitAndWaitForFrameAsync(fake, session, " second");
        var latest = session.TryTakeFrame()!;

        Assert.Equal("first", FirstLine(taken));
        Assert.Equal("first second", FirstLine(latest));
        Assert.True(latest.Version > taken.Version);
    }

    [Fact]
    public async Task Concurrent_SendText_calls_while_output_flows_all_arrive_in_order_per_thread()
    {
        const int Threads = 4;
        const int PerThread = 250;
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        using var stop = new CancellationTokenSource();
        var output = Task.Run(
            async () =>
            {
                while (!stop.IsCancellationRequested)
                {
                    fake.Emit("output\r\n");
                    await Task.Delay(1);
                }
            },
            TestContext.Current.CancellationToken);

        var senders = Enumerable.Range(0, Threads).Select(thread => Task.Run(
            () =>
            {
                for (var i = 0; i < PerThread; i++)
                {
                    session.SendText($"{thread}:{i};");
                }
            },
            TestContext.Current.CancellationToken)).ToArray();
        await Task.WhenAll(senders);
        await WaitUntilAsync(() => TokenRegex().Count(fake.Input) == Threads * PerThread);
        await stop.CancelAsync();
        await output;

        var tokens = TokenRegex().Matches(fake.Input).Select(m => (Thread: int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), Index: int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture)));
        foreach (var group in tokens.GroupBy(t => t.Thread))
        {
            Assert.Equal(Enumerable.Range(0, PerThread), group.Select(t => t.Index));
        }
    }

    [Fact]
    public async Task SetFocus_false_publishes_a_frame_with_the_outline_cursor()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        await EmitAndWaitForFrameAsync(fake, session, "a");
        Assert.Equal(CursorShape.Block, session.TryTakeFrame()!.Cursor!.Shape);

        var before = session.FramesPublished;
        session.SetFocus(false);
        await WaitUntilAsync(() => session.FramesPublished > before);

        Assert.Equal(CursorShape.Outline, session.TryTakeFrame()!.Cursor!.Shape);
    }

    [Fact]
    public async Task SelectAll_selects_the_text_and_shows_the_selection_in_the_next_frame()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        await EmitAndWaitForFrameAsync(fake, session, "hello world");
        var before = session.FramesPublished;

        session.SelectAll();
        await WaitUntilAsync(() => session.FramesPublished > before);

        Assert.StartsWith("hello world", await session.GetSelectionTextAsync(), StringComparison.Ordinal);
        Assert.NotNull(session.TryTakeFrame()!.Selection);

        session.ClearSelection();
        Assert.Equal(string.Empty, await session.GetSelectionTextAsync());
    }

    [Fact]
    public async Task Typing_clears_the_selection()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        await EmitAndWaitForFrameAsync(fake, session, "hello");
        session.SelectAll();
        Assert.NotEqual(string.Empty, await session.GetSelectionTextAsync());

        session.SendText("x");

        Assert.Equal(string.Empty, await session.GetSelectionTextAsync());
    }

    [Fact]
    public async Task HandlePointer_selects_text_by_dragging()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        await EmitAndWaitForFrameAsync(fake, session, "hello world");

        session.HandlePointer(new TerminalPointerEvent(0, 0, 0, 0, PointerButton.Left, PointerAction.Down, false, false, false), 20);
        session.HandlePointer(new TerminalPointerEvent(5, 0, 50, 0, PointerButton.Left, PointerAction.Move, false, false, false), 20);
        session.HandlePointer(new TerminalPointerEvent(5, 0, 50, 0, PointerButton.Left, PointerAction.Up, false, false, false), 20);

        Assert.Equal("hello", await session.GetSelectionTextAsync());
    }
}
