using LoomTerminal.Core;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Selection;
using Microsoft.Extensions.Time.Testing;

namespace LoomTerminal.Presentation.Tests.Input;

public sealed class PointerInputHandlerTests : IDisposable
{
    private const double CellHeight = 20;

    private readonly TerminalCore _core = new(new TerminalOptions { Cols = 20, Rows = 5, Scrollback = 100 });
    private readonly FakeTimeProvider _time = new();
    private readonly SelectionService _selection;
    private readonly PointerInputHandler _handler;
    private readonly List<string> _sent = [];

    public PointerInputHandlerTests()
    {
        _selection = new SelectionService(_core.BufferService, _core.CoreService, _core.OptionsService, isWindows: false);
        _handler = new PointerInputHandler(_core, _selection, _time);
        _core.OnData.Subscribe(_sent.Add);
    }

    public void Dispose()
    {
        _handler.Dispose();
        _selection.Dispose();
        _core.Dispose();
    }

    private static TerminalPointerEvent Pointer(
        int column,
        int row,
        PointerAction action,
        PointerButton button = PointerButton.Left,
        int clickCount = 1,
        bool shift = false,
        bool alt = false,
        int y = 0) =>
        new(column, row, column * 10, y, button, action, false, alt, shift) { ClickCount = clickCount };

    private void Send(TerminalPointerEvent e) => _handler.Handle(e, CellHeight);

    private void Drag(int fromColumn, int fromRow, int toColumn, int toRow, bool alt = false)
    {
        Send(Pointer(fromColumn, fromRow, PointerAction.Down, alt: alt));
        Send(Pointer(toColumn, toRow, PointerAction.Move, alt: alt));
        Send(Pointer(toColumn, toRow, PointerAction.Up, alt: alt));
    }

    [Fact]
    public void Dragging_selects_the_text_between_the_pointer_positions()
    {
        _core.Write("hello world");

        Drag(0, 0, 4, 0);

        Assert.Equal("hell", _selection.SelectionText);
        Assert.Empty(_sent);
    }

    [Fact]
    public void A_double_click_selects_a_word()
    {
        _core.Write("hello world");

        Send(Pointer(7, 0, PointerAction.Down, clickCount: 2));
        Send(Pointer(7, 0, PointerAction.Up));

        Assert.Equal("world", _selection.SelectionText);
    }

    [Fact]
    public void A_triple_click_selects_a_line()
    {
        _core.Write("hello world");

        Send(Pointer(3, 0, PointerAction.Down, clickCount: 3));
        Send(Pointer(3, 0, PointerAction.Up));

        Assert.Equal("hello world", _selection.SelectionText);
    }

    [Fact]
    public void Dragging_after_a_double_click_extends_by_whole_words()
    {
        _core.Write("one two three");

        Send(Pointer(4, 0, PointerAction.Down, clickCount: 2));
        Send(Pointer(9, 0, PointerAction.Move));
        Send(Pointer(9, 0, PointerAction.Up));

        Assert.Equal("two three", _selection.SelectionText);
    }

    [Fact]
    public void Shift_click_extends_the_selection()
    {
        _core.Write("hello world");
        Drag(0, 0, 2, 0);

        Send(Pointer(5, 0, PointerAction.Down, shift: true));
        Send(Pointer(5, 0, PointerAction.Up));

        Assert.Equal("hello", _selection.SelectionText);
    }

    [Fact]
    public void Alt_drag_selects_a_column_block()
    {
        _core.Write("abcdef\r\nghijkl\r\n");

        Drag(1, 0, 3, 1, alt: true);

        Assert.Equal(SelectionMode.Column, _selection.ActiveSelectionMode);
        Assert.Equal("bc\nhi", _selection.SelectionText);
    }

    [Fact]
    public void With_mouse_tracking_a_click_is_reported_and_does_not_select()
    {
        _core.Write("hello\u001b[?1000h\u001b[?1006h");

        Send(Pointer(2, 0, PointerAction.Down));
        Send(Pointer(2, 0, PointerAction.Up));

        Assert.Equal(["\u001b[<0;3;1M", "\u001b[<0;3;1m"], _sent);
        Assert.False(_selection.HasSelection);
    }

    [Fact]
    public void With_mouse_tracking_shift_selects_instead_of_reporting()
    {
        _core.Write("hello world\u001b[?1000h\u001b[?1006h");

        Send(Pointer(0, 0, PointerAction.Down, shift: true));
        Send(Pointer(4, 0, PointerAction.Move, shift: true));
        Send(Pointer(4, 0, PointerAction.Up, shift: true));

        Assert.Equal("hell", _selection.SelectionText);
        Assert.Empty(_sent);
    }

    [Fact]
    public void With_drag_tracking_moves_with_the_button_held_are_reported()
    {
        _core.Write("\u001b[?1002h\u001b[?1006h");

        Send(Pointer(1, 0, PointerAction.Down));
        Send(Pointer(2, 0, PointerAction.Move));
        Send(Pointer(2, 0, PointerAction.Up));

        Assert.Equal(["\u001b[<0;2;1M", "\u001b[<32;3;1M", "\u001b[<0;3;1m"], _sent);
    }

    [Fact]
    public void A_quick_alt_click_moves_the_cursor_of_the_shell()
    {
        _core.Write("abcdef");

        Send(Pointer(2, 0, PointerAction.Down, alt: true));
        _time.Advance(TimeSpan.FromMilliseconds(100));
        Send(Pointer(2, 0, PointerAction.Up, alt: true));

        Assert.Equal(["\u001b[D\u001b[D\u001b[D\u001b[D"], _sent);
    }

    [Fact]
    public void A_slow_alt_click_does_not_move_the_cursor()
    {
        _core.Write("abcdef");

        Send(Pointer(2, 0, PointerAction.Down, alt: true));
        _time.Advance(TimeSpan.FromMilliseconds(600));
        Send(Pointer(2, 0, PointerAction.Up, alt: true));

        Assert.Empty(_sent);
    }

    [Fact]
    public void Alt_click_does_not_move_the_cursor_when_the_option_is_off()
    {
        using var core = new TerminalCore(new TerminalOptions { Cols = 20, Rows = 5, AltClickMovesCursor = false });
        using var selection = new SelectionService(core.BufferService, core.CoreService, core.OptionsService, isWindows: false);
        using var handler = new PointerInputHandler(core, selection, _time);
        var sent = new List<string>();
        core.OnData.Subscribe(sent.Add);
        core.Write("abcdef");

        handler.Handle(Pointer(2, 0, PointerAction.Down, alt: true), CellHeight);
        handler.Handle(Pointer(2, 0, PointerAction.Up, alt: true), CellHeight);

        Assert.Empty(sent);
    }

    [Fact]
    public void Dragging_above_the_area_scrolls_the_viewport_at_most_once_per_50_ms()
    {
        _core.Write(string.Join("\r\n", Enumerable.Range(0, 30)));
        var bottom = _core.Buffers.Active.YDisp;
        Send(Pointer(0, 2, PointerAction.Down));

        // 25 px above the area: half the maximum threshold
        Send(Pointer(0, -1, PointerAction.Move, y: -25));
        var afterFirst = _core.Buffers.Active.YDisp;
        Send(Pointer(0, -1, PointerAction.Move, y: -25));
        var afterSecond = _core.Buffers.Active.YDisp;
        _time.Advance(TimeSpan.FromMilliseconds(60));
        Send(Pointer(0, -1, PointerAction.Move, y: -25));
        var afterThird = _core.Buffers.Active.YDisp;

        Assert.True(afterFirst < bottom);
        Assert.Equal(afterFirst, afterSecond);
        Assert.True(afterThird < afterSecond);
        Assert.Equal(0, _selection.SelectionStart!.Value.X);
    }
}
