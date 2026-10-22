using LoomTerminal.Core;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Rendering;
using LoomTerminal.Presentation.Theming;

namespace LoomTerminal.Presentation.Tests.Rendering;

public sealed class RenderFrameBuilderTests : IDisposable
{
    private readonly TerminalCore _core = new(new TerminalOptions { Cols = 20, Rows = 5 });
    private readonly ThemePalette _palette = new();
    private readonly RenderFrameBuilder _builder;

    public RenderFrameBuilderTests()
    {
        _builder = new RenderFrameBuilder(_core, _palette);
    }

    public void Dispose()
    {
        _builder.Dispose();
        _core.Dispose();
    }

    private RenderFrame Build(ViewState? view = null) => _builder.Build(view ?? ViewState.Default);

    [Fact]
    public void Plain_text_is_one_run()
    {
        _core.Write("hello");

        var run = Assert.Single(Build().Lines[0].Runs);

        Assert.Equal(0, run.Column);
        Assert.Equal(5, run.CellCount);
        Assert.Equal("hello", run.Text);
    }

    [Fact]
    public void A_style_change_splits_the_run()
    {
        _core.Write("ab\u001b[31mcd");

        var runs = Build().Lines[0].Runs;

        Assert.Equal(2, runs.Count);
        Assert.Equal(("ab", 0, 2), (runs[0].Text, runs[0].Column, runs[0].CellCount));
        Assert.Equal(("cd", 2, 2), (runs[1].Text, runs[1].Column, runs[1].CellCount));
        Assert.Equal(_palette.Ansi[1].Rgba, runs[1].Style.Foreground);
        Assert.Equal(_palette.Foreground.Rgba, runs[0].Style.Foreground);
    }

    [Fact]
    public void A_wide_character_and_ascii_with_the_same_style_are_one_run_of_three_cells_and_two_chars()
    {
        _core.Write("あb");

        var run = Assert.Single(Build().Lines[0].Runs);

        Assert.Equal(3, run.CellCount);
        Assert.Equal("あb", run.Text);
    }

    [Fact]
    public void Empty_trailing_cells_with_the_default_background_produce_no_run()
    {
        var frame = Build();

        Assert.Empty(frame.Lines[0].Runs);
        Assert.Equal(5, frame.Lines.Count);
        Assert.Equal(_palette.Background.Rgba, frame.Background);
    }

    [Fact]
    public void A_colored_background_on_empty_cells_is_kept()
    {
        _core.Write("\u001b[44m   ");

        var run = Assert.Single(Build().Lines[0].Runs);

        Assert.Equal("   ", run.Text);
        Assert.Equal(_palette.Ansi[4].Rgba, run.Style.Background);
    }

    [Fact]
    public void The_first_frame_is_all_dirty_and_the_version_increases()
    {
        var first = Build();
        var second = Build();

        Assert.Equal(new HashSet<int> { 0, 1, 2, 3, 4 }, first.DirtyRows);
        Assert.Empty(second.DirtyRows);
        Assert.Equal(first.Version + 1, second.Version);
    }

    [Fact]
    public void Writing_to_row_2_only_marks_row_2_dirty()
    {
        _core.Write("\u001b[3;1H");
        Build();

        _core.Write("xyz");

        Assert.Equal(new HashSet<int> { 2 }, Build().DirtyRows);
    }

    [Fact]
    public void A_resize_marks_every_row_dirty()
    {
        Build();

        _core.Resize(30, 8);
        var frame = Build();

        Assert.Equal(Enumerable.Range(0, 8).ToHashSet(), frame.DirtyRows);
        Assert.Equal((30, 8), (frame.Columns, frame.Rows));
    }

    [Fact]
    public void The_cursor_follows_the_text_and_is_hidden_by_DECTCEM()
    {
        _core.Write("ab");
        var shown = Build().Cursor!;
        _core.Write("\u001b[?25l");
        var hidden = Build().Cursor!;

        Assert.Equal((2, 0, true), (shown.Column, shown.Row, shown.Visible));
        Assert.False(hidden.Visible);
        Assert.Equal(CursorShape.Block, shown.Shape);
        Assert.Equal(_palette.Cursor.Rgba, shown.Color);
    }

    [Fact]
    public void An_unfocused_terminal_draws_the_outline_cursor()
    {
        var cursor = Build(new ViewState { IsFocused = false }).Cursor!;

        Assert.Equal(CursorShape.Outline, cursor.Shape);
    }

    [Fact]
    public void A_blinking_cursor_is_hidden_in_its_off_phase()
    {
        _core.Write("\u001b[1 q");

        Assert.True(Build(new ViewState { CursorBlinkVisible = true }).Cursor!.Visible);
        Assert.False(Build(new ViewState { CursorBlinkVisible = false }).Cursor!.Visible);
    }

    [Fact]
    public void The_cursor_reports_the_text_of_its_cell()
    {
        _core.Write("abc\u001b[2D");

        Assert.Equal("b", Build().Cursor!.CellText);
    }

    [Fact]
    public void Equal_rows_have_equal_content_hashes()
    {
        _core.Write("same\r\nsame\r\ndiff");

        var lines = Build().Lines;

        Assert.Equal(lines[0].ContentHash, lines[1].ContentHash);
        Assert.NotEqual(lines[0].ContentHash, lines[2].ContentHash);
    }

    [Fact]
    public void Wrapped_rows_and_the_alternate_buffer_are_reported()
    {
        _core.Write(new string('x', 25));
        var normal = Build();
        _core.Write("\u001b[?1049h");
        var alternate = Build();

        Assert.True(normal.Lines[1].IsWrapped);
        Assert.False(normal.IsAlternateBuffer);
        Assert.True(alternate.IsAlternateBuffer);
        Assert.Equal(Enumerable.Range(0, 5).ToHashSet(), alternate.DirtyRows);
    }

    [Fact]
    public void Underline_and_links_are_part_of_the_style()
    {
        _core.Write("\u001b[4mu\u001b[0m\u001b]8;;http://example.com\u0007l\u001b]8;;\u0007");

        var runs = Build().Lines[0].Runs;

        Assert.Equal(TerminalUnderlineStyle.SingleLine, runs[0].Style.Underline);
        Assert.False(runs[0].Style.IsLink);
        Assert.True(runs[1].Style.IsLink);
    }

    [Fact]
    public void An_inactive_cursor_style_of_none_hides_the_cursor()
    {
        using var core = new TerminalCore(new TerminalOptions { Cols = 20, Rows = 5, CursorInactiveStyle = CursorInactiveStyle.None });
        using var builder = new RenderFrameBuilder(core, _palette);

        Assert.True(builder.Build(ViewState.Default).Cursor!.Visible);
        Assert.False(builder.Build(new ViewState { IsFocused = false }).Cursor!.Visible);
    }

    [Fact]
    public void The_cursor_shape_follows_the_option_and_DECSCUSR()
    {
        using var core = new TerminalCore(new TerminalOptions { Cols = 20, Rows = 5, CursorStyle = CursorStyle.Underline });
        using var builder = new RenderFrameBuilder(core, _palette);
        Assert.Equal(CursorShape.Underline, builder.Build(ViewState.Default).Cursor!.Shape);

        core.Write("[6 q");

        Assert.Equal(CursorShape.Bar, builder.Build(ViewState.Default).Cursor!.Shape);
    }

    [Fact]
    public void A_selection_is_reported_and_colors_its_cells()
    {
        _core.Write("abcdef");

        var frame = Build(new ViewState { SelectionStart = (0, 0), SelectionEnd = (3, 0) });

        Assert.Equal(new SelectionRange(0, 0, 3, 0, false), frame.Selection);
        var runs = frame.Lines[0].Runs;
        Assert.Equal(("abc", 0), (runs[0].Text, runs[0].Column));
        Assert.Equal(_palette.SelectionBackgroundOpaque.Rgba, runs[0].Style.Background);
        Assert.Equal(("def", 3), (runs[1].Text, runs[1].Column));
        Assert.Equal(_palette.Background.Rgba, runs[1].Style.Background);
    }

    [Fact]
    public void A_selection_outside_the_viewport_is_not_reported_and_a_change_marks_every_row_dirty()
    {
        Build();

        var frame = Build(new ViewState { SelectionStart = (0, 100), SelectionEnd = (3, 100) });

        Assert.Null(frame.Selection);
        Assert.Equal(Enumerable.Range(0, 5).ToHashSet(), frame.DirtyRows);
    }
}
