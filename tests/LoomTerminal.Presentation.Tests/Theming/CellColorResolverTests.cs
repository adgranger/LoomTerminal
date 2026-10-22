using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Common;
using LoomTerminal.Presentation.Theming;

namespace LoomTerminal.Presentation.Tests.Theming;

public class CellColorResolverTests
{
    private static readonly ThemePalette Palette = new();

    private static CellColorResolver Resolver(bool boldBright = true, double minimumContrastRatio = 1) =>
        new(new ThemePalette(), boldBright, minimumContrastRatio);

    private static AttributeData Cell(uint fg = 0, uint bg = 0) => new() { Fg = fg, Bg = bg };

    private static double Contrast(uint rgbaA, uint rgbaB)
    {
        var a = Rgb.RelativeLuminance(rgbaA >> 8);
        var b = Rgb.RelativeLuminance(rgbaB >> 8);
        return ColorMath.ContrastRatio(Math.Max(a, b), Math.Min(a, b));
    }

    [Fact]
    public void Default_colors_resolve_to_the_theme_colors()
    {
        var colors = Resolver().Resolve(Cell(), 'a', false, true);

        Assert.Equal(Palette.Foreground.Rgba, colors.Foreground);
        Assert.Equal(Palette.Background.Rgba, colors.Background);
        Assert.Null(colors.UnderlineColor);
    }

    [Fact]
    public void Inverse_swaps_the_default_colors()
    {
        var colors = Resolver().Resolve(Cell(fg: FgFlags.Inverse), 'a', false, true);

        Assert.Equal(Palette.Background.Rgba, colors.Foreground);
        Assert.Equal(Palette.Foreground.Rgba, colors.Background);
    }

    [Fact]
    public void Bold_p16_1_becomes_p16_9_when_bold_is_drawn_in_bright_colors()
    {
        var cell = Cell(fg: Attributes.CmP16 | 1 | FgFlags.Bold);

        Assert.Equal(Palette.Ansi[9].Rgba, Resolver(boldBright: true).Resolve(cell, 'a', false, true).Foreground);
        Assert.Equal(Palette.Ansi[1].Rgba, Resolver(boldBright: false).Resolve(cell, 'a', false, true).Foreground);
    }

    // xterm.js (TextureAtlas) multiplies the opacity of the foreground by DIM_OPACITY (0.5): 0xFF becomes 0x80.
    [Fact]
    public void Dim_halves_the_alpha_of_the_foreground()
    {
        var colors = Resolver().Resolve(Cell(bg: BgFlags.Dim), 'a', false, true);

        Assert.Equal(0x80u, colors.Foreground & 0xFF);
        Assert.Equal(Palette.Foreground.Rgba & 0xFFFFFF00, colors.Foreground & 0xFFFFFF00);
        Assert.Equal(0xFFu, colors.Background & 0xFF);
    }

    [Fact]
    public void Invisible_makes_the_foreground_equal_to_the_background()
    {
        var colors = Resolver().Resolve(Cell(fg: FgFlags.Invisible | Attributes.CmP16 | 1), 'a', false, true);

        Assert.Equal(colors.Background, colors.Foreground);
    }

    [Fact]
    public void A_selected_cell_uses_the_selection_background()
    {
        var focused = Resolver().Resolve(Cell(), 'a', true, true);
        var unfocused = Resolver().Resolve(Cell(), 'a', true, false);

        Assert.Equal(Palette.SelectionBackgroundOpaque.Rgba, focused.Background);
        Assert.Equal(Palette.SelectionInactiveBackgroundOpaque.Rgba, unfocused.Background);
        Assert.Equal(Palette.Foreground.Rgba, focused.Foreground);
    }

    [Fact]
    public void A_selected_cell_with_a_background_color_blends_it_with_the_selection()
    {
        var colors = Resolver().Resolve(Cell(bg: Attributes.CmP16 | 4), 'a', true, true);

        Assert.NotEqual(Palette.Ansi[4].Rgba, colors.Background);
        Assert.NotEqual(Palette.SelectionBackgroundOpaque.Rgba, colors.Background);
    }

    [Fact]
    public void An_explicit_selection_foreground_replaces_the_text_color_of_a_selected_cell()
    {
        var palette = new ThemePalette(new TerminalTheme { SelectionForeground = "#123456" });

        var colors = new CellColorResolver(palette, true, 1).Resolve(Cell(), 'a', true, true);

        Assert.Equal(0x123456FFu, colors.Foreground);
    }

    [Fact]
    public void A_minimum_contrast_ratio_raises_a_dark_on_dark_foreground()
    {
        // p16:0 (#2e3436) on the black default background
        var cell = Cell(fg: Attributes.CmP16 | 0);
        Assert.True(Contrast(Palette.Ansi[0].Rgba, Palette.Background.Rgba) < 4.5);

        var colors = Resolver(minimumContrastRatio: 4.5).Resolve(cell, 'a', false, true);

        Assert.NotEqual(Palette.Ansi[0].Rgba, colors.Foreground);
        Assert.True(Contrast(colors.Foreground, colors.Background) >= 4.5);
    }

    [Fact]
    public void Powerline_glyphs_are_excluded_from_the_minimum_contrast_ratio()
    {
        var cell = Cell(fg: Attributes.CmP16 | 0);

        var colors = Resolver(minimumContrastRatio: 4.5).Resolve(cell, 0xE0B0, false, true);

        Assert.Equal(Palette.Ansi[0].Rgba, colors.Foreground);
    }

    [Fact]
    public void An_rgb_underline_color_is_returned_and_a_palette_one_is_looked_up()
    {
        var rgb = Cell(bg: BgFlags.HasExtended);
        rgb.Extended.UnderlineColor = Attributes.CmRgb | 0x112233;
        var palette = Cell(bg: BgFlags.HasExtended);
        palette.Extended.UnderlineColor = Attributes.CmP256 | 100;

        Assert.Equal(0x112233FFu, Resolver().Resolve(rgb, 'a', false, true).UnderlineColor);
        Assert.Equal(Palette.Ansi[100].Rgba, Resolver().Resolve(palette, 'a', false, true).UnderlineColor);
    }
}
