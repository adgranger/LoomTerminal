using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Presentation.Tests.Rendering;

public class CustomGlyphsTests
{
    private static readonly CellMetrics Metrics = new FakeTextMeasurer().Measure(new TerminalFont(["x"], 12), 1);

    private static CustomGlyphPath Path(int codePoint) =>
        CustomGlyphs.TryGetPath(codePoint, Metrics) ?? throw new InvalidOperationException($"U+{codePoint:X4} has no definition");

    [Fact]
    public void Horizontal_light_line_is_one_stroked_segment_across_the_middle_of_the_cell()
    {
        var path = Path('─');

        Assert.Equal(
            [
                new CustomGlyphCommand(CustomGlyphCommandKind.MoveTo, 0, 8.5),
                new CustomGlyphCommand(CustomGlyphCommandKind.LineTo, 8, 8.5),
                new CustomGlyphCommand(CustomGlyphCommandKind.Stroke, 1),
            ],
            path.Commands);
        Assert.False(path.ClipToCell);
    }

    [Fact]
    public void Heavy_line_is_stroked_three_times_wider()
    {
        Assert.Equal(new CustomGlyphCommand(CustomGlyphCommandKind.Stroke, 3), Path('━').Commands[^1]);
    }

    [Fact]
    public void Mixed_weight_character_strokes_the_normal_part_then_the_bold_part()
    {
        // ╼ LIGHT LEFT AND HEAVY RIGHT
        var strokes = Path('╼').Commands.Where(c => c.Kind == CustomGlyphCommandKind.Stroke).Select(c => c.A);

        Assert.Equal([1d, 3d], strokes);
    }

    [Fact]
    public void Full_block_fills_the_whole_cell()
    {
        Assert.Equal(
            [
                new CustomGlyphCommand(CustomGlyphCommandKind.Rect, 0, 0, 8, 16),
                new CustomGlyphCommand(CustomGlyphCommandKind.Fill, 1),
            ],
            Path('█').Commands);
    }

    [Fact]
    public void Lower_half_block_fills_the_bottom_half_and_a_quadrant_character_fills_two_rectangles()
    {
        Assert.Equal(new CustomGlyphCommand(CustomGlyphCommandKind.Rect, 0, 8, 8, 8), Path('▄').Commands[0]);
        Assert.Equal(2, Path('▚').Commands.Count(c => c.Kind == CustomGlyphCommandKind.Rect));
    }

    [Theory]
    [InlineData('░', 0.25)]
    [InlineData('▒', 0.5)]
    [InlineData('▓', 0.75)]
    public void Shades_fill_the_cell_with_reduced_opacity(char shade, double alpha)
    {
        Assert.Equal(
            [
                new CustomGlyphCommand(CustomGlyphCommandKind.Rect, 0, 0, 8, 16),
                new CustomGlyphCommand(CustomGlyphCommandKind.Fill, alpha),
            ],
            Path(shade).Commands);
    }

    [Fact]
    public void Powerline_right_triangle_is_a_filled_triangle_clipped_to_the_cell()
    {
        var path = Path(0xE0B0);

        Assert.True(path.ClipToCell);
        Assert.Equal(
            [CustomGlyphCommandKind.MoveTo, CustomGlyphCommandKind.LineTo, CustomGlyphCommandKind.LineTo, CustomGlyphCommandKind.Fill],
            path.Commands.Select(c => c.Kind));
        Assert.Equal(0, path.Commands[0].A);
        Assert.Equal(0, path.Commands[0].B);
        Assert.Equal(16, path.Commands[2].B);
    }

    [Fact]
    public void Powerline_line_separator_is_stroked()
    {
        Assert.Equal(CustomGlyphCommandKind.Stroke, Path(0xE0B1).Commands[^1].Kind);
    }

    [Fact]
    public void Rounded_corner_uses_a_curve()
    {
        Assert.Contains(Path('╭').Commands, c => c.Kind == CustomGlyphCommandKind.CurveTo);
    }

    [Fact]
    public void Supplementary_plane_block_is_defined()
    {
        // U+1FB70 VERTICAL ONE EIGHTH BLOCK-2
        Assert.Equal(new CustomGlyphCommand(CustomGlyphCommandKind.Rect, 1, 0, 1, 16), Path(0x1FB70).Commands[0]);
    }

    [Theory]
    [InlineData('a')]
    [InlineData(' ')]
    [InlineData('▟' + 1)]
    [InlineData(0xE0A0 - 1)]
    public void Characters_without_a_definition_return_null(int codePoint)
    {
        Assert.Null(CustomGlyphs.TryGetPath(codePoint, Metrics));
    }

    [Fact]
    public void Every_box_drawing_character_has_a_definition()
    {
        for (var codePoint = 0x2500; codePoint <= 0x257F; codePoint++)
        {
            Assert.True(CustomGlyphs.IsCustomGlyph(codePoint), $"U+{codePoint:X4}");
        }
    }
}
