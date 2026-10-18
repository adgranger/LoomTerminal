using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Services;
using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.Services;

// New tests: BufferService has no unit tests at the pinned commit.
public sealed class BufferServiceTests : IDisposable
{
    private readonly MockOptionsService _options;
    private readonly BufferService _service;

    public BufferServiceTests()
    {
        _options = new MockOptionsService(TerminalOptions.Default with { Cols = 10, Rows = 5, Scrollback = 20 });
        _service = new BufferService(_options);
    }

    public void Dispose()
    {
        _service.Dispose();
        _options.Dispose();
    }

    [Fact]
    public void Size_comes_from_the_options_and_is_at_least_the_minimum()
    {
        Assert.Equal((10, 5), (_service.Cols, _service.Rows));
        using MockOptionsService tiny = new(TerminalOptions.Default with { Cols = 0, Rows = 0 });
        using BufferService small = new(tiny);
        Assert.Equal((BufferService.MinimumCols, BufferService.MinimumRows), (small.Cols, small.Rows));
    }

    [Fact]
    public void Resize_updates_the_size_the_buffers_and_raises_OnResize()
    {
        List<BufferResizeEvent> events = [];
        _service.OnResize.Subscribe(events.Add);
        _service.Resize(12, 5);
        Assert.Equal([new BufferResizeEvent(12, 5, true, false)], events);
        Assert.Equal(12, _service.Buffer.Lines.Get(0)!.Length);
    }

    [Fact]
    public void ScrollLines_up_at_the_top_does_not_go_negative_and_raises_no_event()
    {
        int raised = 0;
        _service.OnScroll.Subscribe(_ => raised++);
        _service.ScrollLines(-1);
        Assert.Equal(0, _service.Buffer.YDisp);
        Assert.False(_service.IsUserScrolling);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void Scroll_adds_a_line_moves_the_viewport_and_raises_OnScroll()
    {
        List<int> scrolls = [];
        _service.OnScroll.Subscribe(scrolls.Add);
        _service.Scroll(AttributeData.DefaultAttrData);
        Assert.Equal(6, _service.Buffer.Lines.Length);
        Assert.Equal((1, 1), (_service.Buffer.YBase, _service.Buffer.YDisp));
        Assert.Equal([1], scrolls);
    }

    [Fact]
    public void ScrollLines_down_to_the_bottom_ends_the_user_scrolling()
    {
        for (int i = 0; i < 3; i++)
        {
            _service.Scroll(AttributeData.DefaultAttrData);
        }

        _service.ScrollLines(-2);
        Assert.True(_service.IsUserScrolling);
        Assert.Equal(1, _service.Buffer.YDisp);
        _service.ScrollLines(5);
        Assert.False(_service.IsUserScrolling);
        Assert.Equal(_service.Buffer.YBase, _service.Buffer.YDisp);
    }

    [Fact]
    public void Scroll_with_a_full_scrollback_trims_and_keeps_ybase_stable()
    {
        // 5 rows + 20 scrollback = 25 lines
        for (int i = 0; i < 20; i++)
        {
            _service.Scroll(AttributeData.DefaultAttrData);
        }

        Core.Buffer.Buffer buffer = _service.Buffer;
        Assert.True(buffer.Lines.IsFull);
        Assert.Equal(20, buffer.YBase);

        _service.Scroll(AttributeData.DefaultAttrData);

        Assert.True(buffer.Lines.IsFull);
        Assert.Equal(25, buffer.Lines.Length);
        Assert.Equal(20, buffer.YBase);
        Assert.Equal(20, buffer.YDisp);
    }

    [Fact]
    public void Scroll_with_a_scroll_region_shifts_the_lines_in_place()
    {
        Core.Buffer.Buffer buffer = _service.Buffer;
        buffer.Lines.Get(1)!.Set(0, new CharData(0, "b", 1, 'b'));
        buffer.Lines.Get(2)!.Set(0, new CharData(0, "c", 1, 'c'));
        buffer.ScrollTop = 1;
        buffer.ScrollBottom = 3;
        _service.Scroll(AttributeData.DefaultAttrData);
        Assert.Equal(5, buffer.Lines.Length);
        Assert.Equal(0, buffer.YBase);
        Assert.Equal("c", buffer.Lines.Get(1)!.TranslateToString(true, 0, 1));
        Assert.Equal(string.Empty, buffer.Lines.Get(3)!.TranslateToString(true));
    }

    [Fact]
    public void Reset_recreates_the_buffers_and_ends_the_user_scrolling()
    {
        _service.Scroll(AttributeData.DefaultAttrData);
        _service.ScrollLines(-1);
        Assert.True(_service.IsUserScrolling);
        _service.Reset();
        Assert.False(_service.IsUserScrolling);
        Assert.Equal(0, _service.Buffer.YBase);
        Assert.Equal(5, _service.Buffer.Lines.Length);
    }
}
