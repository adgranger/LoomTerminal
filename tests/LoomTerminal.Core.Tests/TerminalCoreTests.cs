using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests;

public sealed class TerminalCoreTests : IDisposable
{
    private readonly TerminalCore _terminal = new(TerminalOptions.Default with { Cols = 80, Rows = 24 });

    public void Dispose() => _terminal.Dispose();

    private string Line(int row) => _terminal.Buffers.Active.Lines.Get(row)!.TranslateToString(true);

    [Fact]
    public void Write_prints_text_on_the_active_buffer()
    {
        _terminal.Write("hello\r\nworld");
        Assert.Equal("hello", Line(0));
        Assert.Equal("world", Line(1));
    }

    [Fact]
    public void Resize_raises_OnResize()
    {
        List<BufferResizeEvent> resizes = [];
        _terminal.OnResize.Subscribe(resizes.Add);
        _terminal.Resize(10, 5);
        Assert.Equal(10, _terminal.Cols);
        Assert.Equal(5, _terminal.Rows);
        Assert.Single(resizes);
        Assert.Equal(10, resizes[0].Cols);
        Assert.Equal(5, resizes[0].Rows);
    }

    [Fact]
    public void Resize_enforces_the_minimum_size()
    {
        _terminal.Resize(0, 0);
        Assert.Equal(BufferService.MinimumCols, _terminal.Cols);
        Assert.Equal(BufferService.MinimumRows, _terminal.Rows);
    }

    [Fact]
    public void OSC_0_raises_OnTitleChange()
    {
        List<string> titles = [];
        _terminal.OnTitleChange.Subscribe(titles.Add);
        _terminal.Write("\x1b]0;title\x07");
        Assert.Equal<string>(["title"], titles);
    }

    [Fact]
    public void DSR_6_raises_OnData_with_a_cursor_position_report()
    {
        List<string> data = [];
        _terminal.OnData.Subscribe(data.Add);
        _terminal.Write("ab\x1b[6n");
        Assert.Equal<string>(["\x1b[1;3R"], data);
    }

    [Fact]
    public void Reset_clears_the_screen_and_the_attributes()
    {
        _terminal.Write("\x1b[1;31mtext");
        _terminal.Reset();
        Assert.Equal(string.Empty, Line(0));
        Assert.Equal(0, _terminal.Buffers.Active.X);
        Assert.Equal(0, _terminal.Buffers.Active.Y);
    }

    [Fact]
    public void ESC_c_resets_the_terminal()
    {
        _terminal.Write("text\x1b" + "c");
        Assert.Equal(string.Empty, Line(0));
    }

    [Fact]
    public void A_UTF8_character_split_across_two_writes_is_printed_once()
    {
        byte[] bytes = "é"u8.ToArray();
        _terminal.Write(bytes.AsSpan(0, 1));
        _terminal.Write(bytes.AsSpan(1));
        Assert.Equal("é", Line(0));
        Assert.Equal(1, _terminal.Buffers.Active.X);
    }

    [Fact]
    public void Bell_and_line_feed_events_are_forwarded()
    {
        int bells = 0;
        int lineFeeds = 0;
        _terminal.OnBell.Subscribe(_ => bells++);
        _terminal.OnLineFeed.Subscribe(_ => lineFeeds++);
        _terminal.Write("\x07\n\n");
        Assert.Equal(1, bells);
        Assert.Equal(2, lineFeeds);
    }

    [Fact]
    public void Scrolling_raises_OnScroll_and_ScrollToTop_and_ScrollToBottom_move_the_viewport()
    {
        List<int> positions = [];
        _terminal.OnScroll.Subscribe(positions.Add);
        _terminal.Write(string.Concat(Enumerable.Repeat("line\r\n", 40)));
        int bottom = _terminal.Buffers.Active.YBase;
        Assert.True(bottom > 0);
        Assert.Equal(bottom, _terminal.Buffers.Active.YDisp);
        _terminal.ScrollToTop();
        Assert.Equal(0, _terminal.Buffers.Active.YDisp);
        _terminal.ScrollToBottom();
        Assert.Equal(bottom, _terminal.Buffers.Active.YDisp);
        _terminal.ScrollToLine(3);
        Assert.Equal(3, _terminal.Buffers.Active.YDisp);
        Assert.NotEmpty(positions);
    }

    [Fact]
    public void Input_sends_data_to_the_pty()
    {
        List<string> data = [];
        _terminal.OnData.Subscribe(data.Add);
        _terminal.Input("a");
        Assert.Equal<string>(["a"], data);
    }

    [Fact]
    public void Registered_CSI_handlers_are_called()
    {
        int calls = 0;
        _terminal.RegisterCsiHandler(new LoomTerminal.Core.Parser.FunctionIdentifier(Final: "z"), _ =>
        {
            calls++;
            return true;
        });
        _terminal.Write("\x1b[1z");
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Registered_XTWINOPS_handlers_are_only_called_when_the_window_option_is_enabled()
    {
        int calls = 0;
        _terminal.RegisterCsiHandler(new LoomTerminal.Core.Parser.FunctionIdentifier(Final: "t"), _ =>
        {
            calls++;
            return true;
        });
        _terminal.Write("\x1b[18t");
        Assert.Equal(0, calls);
        _terminal.OptionsService.SetOptions(_terminal.Options with
        {
            WindowOptions = _terminal.Options.WindowOptions with { GetWinSizeChars = true },
        });
        _terminal.Write("\x1b[18t");
        Assert.Equal(1, calls);
    }
}
