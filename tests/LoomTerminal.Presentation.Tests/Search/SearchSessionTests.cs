using LoomTerminal.Presentation.Rendering;
using LoomTerminal.Presentation.Search;
using LoomTerminal.Presentation.Session;
using LoomTerminal.Presentation.Tests.Session;

namespace LoomTerminal.Presentation.Tests.Search;

public sealed class SearchSessionTests
{
    private static readonly SearchOptions Decorated = new() { Decorations = true };

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(5, cts.Token);
        }
    }

    private static async Task EmitAsync(FakePtyConnection fake, TerminalSession session, string text)
    {
        var before = session.FramesPublished;
        fake.Emit(text);
        await WaitUntilAsync(() => session.FramesPublished > before);
    }

    // Runs a search action and returns its result together with the frame published after it.
    private static async Task<(T Result, RenderFrame Frame)> WithFrameAsync<T>(TerminalSession session, Func<Task<T>> action)
    {
        var before = session.FramesPublished;
        var result = await action();
        await WaitUntilAsync(() => session.FramesPublished > before);
        return (result, session.TryTakeFrame()!);
    }

    private static List<RenderRun> RunsWith(RenderFrame frame, RunHighlight highlight) =>
        frame.Lines.SelectMany(line => line.Runs).Where(run => run.Style.Highlight == highlight).ToList();

    [Fact]
    public async Task FindNextAsync_walks_the_matches_and_the_frame_highlights_them()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 40, 10);
        await EmitAsync(fake, session, "foo bar\r\nfoo baz");

        var (first, frame) = await WithFrameAsync(session, () => session.FindNextAsync("foo", Decorated));

        Assert.Equal(new SearchResultSummary(0, 2), first);
        Assert.Equal(["foo"], RunsWith(frame, RunHighlight.ActiveSearchMatch).Select(r => r.Text));
        Assert.Equal(0, RunsWith(frame, RunHighlight.ActiveSearchMatch)[0].Column);
        Assert.Single(RunsWith(frame, RunHighlight.SearchMatch));
        Assert.Equal(0, frame.Selection!.StartRow);

        var (second, frame2) = await WithFrameAsync(session, () => session.FindNextAsync("foo", Decorated));

        Assert.Equal(new SearchResultSummary(1, 2), second);
        var active = frame2.Lines[1].Runs.Single(r => r.Style.Highlight == RunHighlight.ActiveSearchMatch);
        Assert.Equal("foo", active.Text);
        Assert.Equal("foo", await session.GetSelectionTextAsync());
    }

    [Fact]
    public async Task FindPreviousAsync_goes_back_and_a_missing_term_finds_nothing()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 40, 10);
        await EmitAsync(fake, session, "foo\r\nfoo");

        var previous = await session.FindPreviousAsync("foo", Decorated);
        var missing = await session.FindNextAsync("nope", Decorated);

        Assert.Equal(new SearchResultSummary(1, 2), previous);
        Assert.Equal(new SearchResultSummary(-1, 0), missing);
    }

    [Fact]
    public async Task SearchResultsChanged_is_raised_with_decorations_only()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 40, 10);
        await EmitAsync(fake, session, "foo");
        var raised = new List<SearchResultSummary>();
        session.SearchResultsChanged += raised.Add;

        await session.FindNextAsync("fo", new SearchOptions());
        await session.FindNextAsync("foo", Decorated);

        Assert.Equal([new SearchResultSummary(0, 1)], raised);
    }

    [Fact]
    public async Task ClearSearch_removes_the_highlights()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 40, 10);
        await EmitAsync(fake, session, "foo foo");
        await WithFrameAsync(session, () => session.FindNextAsync("foo", Decorated));

        var before = session.FramesPublished;
        session.ClearSearch();
        await WaitUntilAsync(() => session.FramesPublished > before);
        var frame = session.TryTakeFrame()!;

        Assert.Empty(RunsWith(frame, RunHighlight.SearchMatch));
        Assert.Empty(RunsWith(frame, RunHighlight.ActiveSearchMatch));
    }

    [Fact]
    public async Task A_match_in_the_scrollback_scrolls_the_viewport_to_it()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 40, 10);
        await EmitAsync(fake, session, "needle\r\n" + string.Concat(Enumerable.Range(0, 40).Select(i => $"line {i}\r\n")));
        var bottom = session.TryTakeFrame()!;
        Assert.Equal(bottom.ScrollbackLength, bottom.ViewportY);

        var (summary, frame) = await WithFrameAsync(session, () => session.FindNextAsync("needle", Decorated));

        Assert.Equal(new SearchResultSummary(0, 1), summary);
        Assert.True(frame.ViewportY < frame.ScrollbackLength);
        Assert.Contains(frame.Lines.SelectMany(l => l.Runs), r => r.Style.Highlight == RunHighlight.ActiveSearchMatch && r.Text == "needle");
    }

    [Fact]
    public async Task The_theme_colors_the_matches()
    {
        var fake = new FakePtyConnection();
        var theme = new LoomTerminal.Presentation.Theming.TerminalTheme { Background = "#000000", SearchMatchBackground = "#ff0000", SearchActiveMatchBackground = "#00ff00" };
        using var session = TerminalSession.Attach(fake, 40, 10, new TerminalSessionOptions { Theme = theme });
        await EmitAsync(fake, session, "ab ab");

        var (_, frame) = await WithFrameAsync(session, () => session.FindNextAsync("ab", Decorated));

        Assert.Equal(0x00FF00FFu, RunsWith(frame, RunHighlight.ActiveSearchMatch)[0].Style.Background);
        Assert.Equal(0xFF0000FFu, RunsWith(frame, RunHighlight.SearchMatch)[0].Style.Background);
    }
}
