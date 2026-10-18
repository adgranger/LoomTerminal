using LoomTerminal.Core.InputHandling;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerShellTests : InputHandlerTestBase
{
    [Fact]
    public void Parse_of_text_marks_the_cursor_row_dirty()
    {
        List<RowRange?> refreshes = [];
        InputHandler.OnRequestRefreshRows.Subscribe(refreshes.Add);
        InputHandler.Parse("abc");
        Assert.Equal<RowRange?>([new RowRange(0, 0)], refreshes);
    }

    [Fact]
    public void Parse_of_bytes_marks_the_cursor_row_dirty()
    {
        List<RowRange?> refreshes = [];
        InputHandler.OnRequestRefreshRows.Subscribe(refreshes.Add);
        InputHandler.Parse("abc"u8);
        Assert.Equal<RowRange?>([new RowRange(0, 0)], refreshes);
    }

    [Fact]
    public void MarkRangeDirty_extends_the_refreshed_rows_of_the_next_parse()
    {
        List<RowRange?> refreshes = [];
        InputHandler.OnRequestRefreshRows.Subscribe(refreshes.Add);
        InputHandler.Parse("a");
        InputHandler.MarkRangeDirty(5, 2);
        InputHandler.Parse("b");
        Assert.Equal<RowRange?>([new RowRange(0, 0), new RowRange(0, 0)], refreshes);
    }

    [Fact]
    public void Unknown_CSI_sequences_reach_the_fallback_handler()
    {
        InputHandler.Parse("\x1b[5z");
        Assert.Contains(LogService.DebugMessages, m => m.StartsWith("Unknown CSI code: ", StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_ESC_sequences_reach_the_fallback_handler()
    {
        InputHandler.Parse("\x1b%");
        InputHandler.Parse("\x1bZ");
        Assert.Contains(LogService.DebugMessages, m => m.StartsWith("Unknown ESC code: ", StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_OSC_sequences_reach_the_fallback_handler()
    {
        InputHandler.Parse("\x1b]9999;x\x07");
        Assert.Contains(LogService.DebugMessages, m => m.StartsWith("Unknown OSC code: 9999 ", StringComparison.Ordinal));
    }

    [Fact]
    public void Text_longer_than_the_parse_buffer_is_parsed_in_chunks()
    {
        List<RowRange?> refreshes = [];
        InputHandler.OnRequestRefreshRows.Subscribe(refreshes.Add);
        InputHandler.Parse(new string('a', 300000));
        Assert.Single(refreshes);
    }

    [Fact]
    public void OnCursorMove_is_not_raised_when_the_cursor_did_not_move()
    {
        int moves = 0;
        InputHandler.OnCursorMove.Subscribe(_ => moves++);
        InputHandler.Parse("abc");
        Assert.Equal(0, moves);
    }
}
