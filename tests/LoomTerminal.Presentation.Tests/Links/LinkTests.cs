using LoomTerminal.Core;
using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Links;

namespace LoomTerminal.Presentation.Tests.Links;

public sealed class LinkTests : IDisposable
{
    private readonly TerminalCore _core = new(new TerminalOptions { Cols = 20, Rows = 5 });

    public void Dispose() => _core.Dispose();

    private sealed class RecordingHandler(Func<Uri, bool> canOpen) : ILinkHandler
    {
        public List<Uri> Opened { get; } = [];

        public bool CanOpen(Uri uri) => canOpen(uri);

        public void Open(Uri uri) => Opened.Add(uri);
    }

    private static TerminalPointerEvent Pointer(int column, int row, PointerAction action, bool ctrl) =>
        new(column, row, column * 8, row * 16, PointerButton.Left, action, ctrl, false, false);

    private static BufferRange Range(int startX, int startY, int endX, int endY) =>
        new(new BufferCellPosition(startX, startY), new BufferCellPosition(endX, endY));

    [Fact]
    public void A_url_is_detected_with_its_exact_range()
    {
        using var wide = new TerminalCore(new TerminalOptions { Cols = 80, Rows = 5 });
        wide.Write("see https://example.com/a?b=c now");

        var link = Assert.Single(WebLinkComputer.ComputeLinks(wide.Buffers.Active, 1));

        Assert.Equal("https://example.com/a?b=c", link.Text);
        Assert.Equal(Range(5, 1, 29, 1), link.Range);
    }

    [Fact]
    public void A_url_wrapped_across_two_lines_is_one_link()
    {
        _core.Write("https://example.com/abcdef");

        var fromFirst = Assert.Single(WebLinkComputer.ComputeLinks(_core.Buffers.Active, 1));
        var fromSecond = Assert.Single(WebLinkComputer.ComputeLinks(_core.Buffers.Active, 2));

        Assert.Equal("https://example.com/abcdef", fromFirst.Text);
        Assert.Equal(Range(1, 1, 6, 2), fromFirst.Range);
        Assert.Equal(fromFirst, fromSecond);
    }

    [Theory]
    [InlineData("(https://example.com/a).", "https://example.com/a")]
    [InlineData("https://example.com/a,", "https://example.com/a")]
    [InlineData("<https://example.com/a>", "https://example.com/a")]
    [InlineData("\"https://example.com/a\"", "https://example.com/a")]
    public void Brackets_quotes_and_final_punctuation_are_not_part_of_the_url(string line, string expected)
    {
        using var wide = new TerminalCore(new TerminalOptions { Cols = 80, Rows = 5 });
        wide.Write(line);

        var link = Assert.Single(WebLinkComputer.ComputeLinks(wide.Buffers.Active, 1));

        Assert.Equal(expected, link.Text);
    }

    [Fact]
    public void A_scheme_without_a_host_is_not_a_link()
    {
        _core.Write("https://");

        Assert.Empty(WebLinkComputer.ComputeLinks(_core.Buffers.Active, 1));
    }

    [Fact]
    public void An_osc_8_link_is_detected()
    {
        _core.Write("\u001b]8;;https://example.com\u0007link\u001b]8;;\u0007 tail");

        var link = Assert.Single(OscLinkProvider.ProvideLinks(_core.Buffers.Active, _core.OscLinkService, 1));

        Assert.Equal("https://example.com", link.Text);
        Assert.Equal(Range(1, 1, 4, 1), link.Range);
    }

    [Fact]
    public void An_osc_8_link_to_a_non_http_scheme_is_ignored()
    {
        _core.Write("\u001b]8;;file:///etc/passwd\u0007link\u001b]8;;\u0007");

        Assert.Empty(OscLinkProvider.ProvideLinks(_core.Buffers.Active, _core.OscLinkService, 1));
    }

    [Fact]
    public void The_default_handler_accepts_https_only()
    {
        var handler = new HttpsOnlyLinkHandler();

        Assert.True(handler.CanOpen(new Uri("https://example.com")));
        Assert.False(handler.CanOpen(new Uri("http://example.com")));
    }

    [Fact]
    public void A_link_the_handler_refuses_is_never_current()
    {
        _core.Write("http://example.com");
        var tracker = new LinkTracker(_core);

        tracker.Handle(Pointer(2, 0, PointerAction.Move, ctrl: true));

        Assert.Null(tracker.Current);
    }

    [Fact]
    public void The_link_under_the_pointer_is_current_only_while_ctrl_is_held()
    {
        _core.Write("https://example.com");
        var tracker = new LinkTracker(_core);

        tracker.Handle(Pointer(2, 0, PointerAction.Move, ctrl: true));
        Assert.Equal("https://example.com", tracker.Current?.Text);

        tracker.Handle(Pointer(2, 0, PointerAction.Move, ctrl: false));
        Assert.Null(tracker.Current);
    }

    [Fact]
    public void Ctrl_click_opens_the_link_and_a_plain_click_does_not()
    {
        _core.Write("go https://example.com");
        var handler = new RecordingHandler(_ => true);
        var tracker = new LinkTracker(_core) { Handler = handler };

        tracker.Handle(Pointer(5, 0, PointerAction.Down, ctrl: false));
        tracker.Handle(Pointer(5, 0, PointerAction.Up, ctrl: false));
        Assert.Empty(handler.Opened);

        tracker.Handle(Pointer(5, 0, PointerAction.Down, ctrl: true));
        tracker.Handle(Pointer(5, 0, PointerAction.Up, ctrl: true));
        Assert.Equal(new Uri("https://example.com"), Assert.Single(handler.Opened));
    }

    [Fact]
    public void A_drag_off_the_link_does_not_open_it()
    {
        _core.Write("go https://example.com");
        var handler = new RecordingHandler(_ => true);
        var tracker = new LinkTracker(_core) { Handler = handler };

        tracker.Handle(Pointer(5, 0, PointerAction.Down, ctrl: true));
        tracker.Handle(Pointer(6, 0, PointerAction.Up, ctrl: true));

        Assert.Empty(handler.Opened);
    }

    [Fact]
    public void Without_a_handler_no_link_is_offered()
    {
        _core.Write("https://example.com");
        var tracker = new LinkTracker(_core) { Handler = null };

        tracker.Handle(Pointer(2, 0, PointerAction.Move, ctrl: true));

        Assert.Null(tracker.Current);
    }
}
