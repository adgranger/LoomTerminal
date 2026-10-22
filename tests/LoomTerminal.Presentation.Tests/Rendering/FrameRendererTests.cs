using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Presentation.Tests.Rendering;

public class FrameRendererTests
{
    private const uint Bg = 0x000000ff;
    private const uint Fg = 0xffffffff;
    private const uint Red = 0xff0000ff;
    private const uint Blue = 0x0000ffff;

    private static TextRunStyle Style(uint bg = Bg, TerminalUnderlineStyle underline = TerminalUnderlineStyle.None, bool strike = false) =>
        new(Fg, bg, false, false, false, underline, null, strike, false, false, RunHighlight.None);

    private static RenderRun Run(int column, string text, TextRunStyle style, int cells = -1) =>
        new(column, cells < 0 ? text.Length : cells, text, style);

    private static RenderFrame Frame(RenderRun[][] rows, RenderCursor? cursor = null) =>
        new(10, rows.Length, rows.Select(r => new RenderRow(r, false, 0)).ToArray(), cursor, Bg, 0, 0, false, 1, new HashSet<int>(), null, []);

    private static string Draw(RenderFrame frame)
    {
        var metrics = new FakeTextMeasurer().Measure(new TerminalFont(["x"], 12), 1);
        var surface = new RecordingRenderSurface();
        FrameRenderer.Render(frame, metrics, surface);
        return surface.Text;
    }

    [Fact]
    public void Plain_text_is_cleared_then_drawn_without_background_rectangles()
    {
        var text = Draw(Frame([[Run(0, "hi", Style())]]));

        Assert.Equal("Clear #000000ff\nText 0,0 \"hi\" cells=2 fg=#ffffffff", text);
    }

    [Fact]
    public void Custom_glyph_cells_are_drawn_as_vectors_and_split_the_text_run()
    {
        var text = Draw(Frame([[Run(2, "a─█b", Style())]]));

        Assert.Equal(
            "Clear #000000ff\nText 0,2 \"a\" cells=1 fg=#ffffffff\nGlyph 24,0 #ffffffff commands=3\n" +
            "Glyph 32,0 #ffffffff commands=2\nText 0,5 \"b\" cells=1 fg=#ffffffff",
            text);
    }

    [Fact]
    public void Custom_glyphs_can_be_turned_off()
    {
        var metrics = new FakeTextMeasurer().Measure(new TerminalFont(["x"], 12), 1);
        var surface = new RecordingRenderSurface();
        FrameRenderer.Render(Frame([[Run(0, "─", Style())]]), metrics, surface, customGlyphs: false);

        Assert.Equal("Clear #000000ff\nText 0,0 \"─\" cells=1 fg=#ffffffff", surface.Text);
    }

    [Fact]
    public void A_wide_character_before_a_glyph_shifts_it_by_two_cells()
    {
        var text = Draw(Frame([[Run(0, "中─", Style(), cells: 3)]]));

        Assert.Equal("Clear #000000ff\nText 0,0 \"中\" cells=2 fg=#ffffffff\nGlyph 16,0 #ffffffff commands=3", text);
    }

    [Fact]
    public void Adjacent_runs_with_the_same_background_are_merged_and_spaces_are_not_drawn()
    {
        var text = Draw(Frame([[Run(1, "a", Style(Red)), Run(2, "  ", Style(Red)), Run(5, "b", Style(Blue))]]));

        Assert.Equal(
            "Clear #000000ff\nFillRect 8,0 24x16 #ff0000ff\nFillRect 40,0 8x16 #0000ffff\n" +
            "Text 0,1 \"a\" cells=1 fg=#ffffffff\nText 0,5 \"b\" cells=1 fg=#ffffffff",
            text);
    }

    [Fact]
    public void Selection_over_two_lines_is_drawn_as_the_blended_run_backgrounds()
    {
        var selected = Style(0x334455ff);
        var text = Draw(Frame([[Run(3, "ab", selected)], [Run(0, "cd", selected)]]));

        Assert.Equal(
            "Clear #000000ff\nFillRect 24,0 16x16 #334455ff\nFillRect 0,16 16x16 #334455ff\n" +
            "Text 0,3 \"ab\" cells=2 fg=#ffffffff\nText 1,0 \"cd\" cells=2 fg=#ffffffff",
            text);
    }

    [Theory]
    [InlineData(CursorShape.Block, "FillRect 16,16 8x16 #00ff00ff\nText 1,2 \"x\" cells=1 fg=#111111ff")]
    [InlineData(CursorShape.Bar, "FillRect 16,16 1x16 #00ff00ff")]
    [InlineData(CursorShape.Underline, "FillRect 16,30 8x2 #00ff00ff")]
    [InlineData(CursorShape.Outline, "Rect 16,16 8x16 t=1 #00ff00ff")]
    public void Each_cursor_shape_is_drawn_last(CursorShape shape, string expected)
    {
        var cursor = new RenderCursor(2, 1, shape, true, 0x00ff00ff, 0x111111ff, "x");
        var text = Draw(Frame([[], []], cursor));

        Assert.Equal("Clear #000000ff\n" + expected, text);
    }

    [Fact]
    public void A_hidden_cursor_is_not_drawn()
    {
        var cursor = new RenderCursor(0, 0, CursorShape.Block, false, 0x00ff00ff, 0x111111ff, "x");

        Assert.Equal("Clear #000000ff", Draw(Frame([[]], cursor)));
    }

    [Fact]
    public void A_curly_underline_over_two_runs_is_one_continuous_line()
    {
        var curly = Style(underline: TerminalUnderlineStyle.Curly);
        var text = Draw(Frame([[Run(0, "ab", curly), Run(2, "cd", curly)]]));

        Assert.Equal(
            "Clear #000000ff\nText 0,0 \"ab\" cells=2 fg=#ffffffff\nText 0,2 \"cd\" cells=2 fg=#ffffffff\n" +
            "Underline Curly 0,14 w=32 #ffffffff",
            text);
    }

    [Fact]
    public void A_wide_character_run_spans_two_cells_and_strikethrough_follows_the_cells()
    {
        var text = Draw(Frame([[Run(1, "世", Style(strike: true), cells: 2)]]));

        Assert.Equal(
            "Clear #000000ff\nText 0,1 \"世\" cells=2 fg=#ffffffff\nLine 8,8.5 24,8.5 t=1 #ffffffff",
            text);
    }
}
