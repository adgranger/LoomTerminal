// Ported from xtermjs/xterm.js: src/browser/services/ThemeService.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;
using LoomTerminal.Core.InputHandling;
using LoomTerminal.Presentation.Theming;

namespace LoomTerminal.Presentation.Tests.Theming;

public class ThemePaletteTests
{
    // The expected css of the colors 16 to 255 (ThemeService.test.ts: "should fill 240 colors with expected values").
    private static readonly string[] ExpectedExtendedColors =
    [
        "#000000", "#00005f", "#000087", "#0000af", "#0000d7", "#0000ff",
        "#005f00", "#005f5f", "#005f87", "#005faf", "#005fd7", "#005fff",
        "#008700", "#00875f", "#008787", "#0087af", "#0087d7", "#0087ff",
        "#00af00", "#00af5f", "#00af87", "#00afaf", "#00afd7", "#00afff",
        "#00d700", "#00d75f", "#00d787", "#00d7af", "#00d7d7", "#00d7ff",
        "#00ff00", "#00ff5f", "#00ff87", "#00ffaf", "#00ffd7", "#00ffff",
        "#5f0000", "#5f005f", "#5f0087", "#5f00af", "#5f00d7", "#5f00ff",
        "#5f5f00", "#5f5f5f", "#5f5f87", "#5f5faf", "#5f5fd7", "#5f5fff",
        "#5f8700", "#5f875f", "#5f8787", "#5f87af", "#5f87d7", "#5f87ff",
        "#5faf00", "#5faf5f", "#5faf87", "#5fafaf", "#5fafd7", "#5fafff",
        "#5fd700", "#5fd75f", "#5fd787", "#5fd7af", "#5fd7d7", "#5fd7ff",
        "#5fff00", "#5fff5f", "#5fff87", "#5fffaf", "#5fffd7", "#5fffff",
        "#870000", "#87005f", "#870087", "#8700af", "#8700d7", "#8700ff",
        "#875f00", "#875f5f", "#875f87", "#875faf", "#875fd7", "#875fff",
        "#878700", "#87875f", "#878787", "#8787af", "#8787d7", "#8787ff",
        "#87af00", "#87af5f", "#87af87", "#87afaf", "#87afd7", "#87afff",
        "#87d700", "#87d75f", "#87d787", "#87d7af", "#87d7d7", "#87d7ff",
        "#87ff00", "#87ff5f", "#87ff87", "#87ffaf", "#87ffd7", "#87ffff",
        "#af0000", "#af005f", "#af0087", "#af00af", "#af00d7", "#af00ff",
        "#af5f00", "#af5f5f", "#af5f87", "#af5faf", "#af5fd7", "#af5fff",
        "#af8700", "#af875f", "#af8787", "#af87af", "#af87d7", "#af87ff",
        "#afaf00", "#afaf5f", "#afaf87", "#afafaf", "#afafd7", "#afafff",
        "#afd700", "#afd75f", "#afd787", "#afd7af", "#afd7d7", "#afd7ff",
        "#afff00", "#afff5f", "#afff87", "#afffaf", "#afffd7", "#afffff",
        "#d70000", "#d7005f", "#d70087", "#d700af", "#d700d7", "#d700ff",
        "#d75f00", "#d75f5f", "#d75f87", "#d75faf", "#d75fd7", "#d75fff",
        "#d78700", "#d7875f", "#d78787", "#d787af", "#d787d7", "#d787ff",
        "#d7af00", "#d7af5f", "#d7af87", "#d7afaf", "#d7afd7", "#d7afff",
        "#d7d700", "#d7d75f", "#d7d787", "#d7d7af", "#d7d7d7", "#d7d7ff",
        "#d7ff00", "#d7ff5f", "#d7ff87", "#d7ffaf", "#d7ffd7", "#d7ffff",
        "#ff0000", "#ff005f", "#ff0087", "#ff00af", "#ff00d7", "#ff00ff",
        "#ff5f00", "#ff5f5f", "#ff5f87", "#ff5faf", "#ff5fd7", "#ff5fff",
        "#ff8700", "#ff875f", "#ff8787", "#ff87af", "#ff87d7", "#ff87ff",
        "#ffaf00", "#ffaf5f", "#ffaf87", "#ffafaf", "#ffafd7", "#ffafff",
        "#ffd700", "#ffd75f", "#ffd787", "#ffd7af", "#ffd7d7", "#ffd7ff",
        "#ffff00", "#ffff5f", "#ffff87", "#ffffaf", "#ffffd7", "#ffffff",
        "#080808", "#121212", "#1c1c1c", "#262626", "#303030", "#3a3a3a",
        "#444444", "#4e4e4e", "#585858", "#626262", "#6c6c6c", "#767676",
        "#808080", "#8a8a8a", "#949494", "#9e9e9e", "#a8a8a8", "#b2b2b2",
        "#bcbcbc", "#c6c6c6", "#d0d0d0", "#dadada", "#e4e4e4", "#eeeeee",
    ];

    // constructor: should fill all colors with values
    [Fact]
    public void All_colors_have_values()
    {
        var palette = new ThemePalette();

        TerminalColor[] colors =
        [
            palette.Foreground, palette.Background, palette.Cursor, palette.CursorAccent,
            palette.SelectionBackgroundTransparent, palette.SelectionBackgroundOpaque,
            palette.SelectionInactiveBackgroundTransparent, palette.SelectionInactiveBackgroundOpaque,
        ];
        foreach (var color in colors)
        {
            Assert.True(color.Css.Length >= 7);
        }

        Assert.Equal(256, palette.Ansi.Length);
    }

    // constructor: should fill 240 colors with expected values
    [Fact]
    public void The_240_extended_colors_have_the_expected_values()
    {
        var palette = new ThemePalette();

        Assert.Equal(240, ExpectedExtendedColors.Length);
        for (var i = 0; i < 240; i++)
        {
            Assert.Equal(ExpectedExtendedColors[i], palette.Ansi[i + 16].Css);
        }
    }

    // setTheme: should not throw when not setting all colors
    [Fact]
    public void Setting_an_empty_theme_does_not_throw()
    {
        new ThemePalette().SetTheme(new TerminalTheme());
    }

    // setTheme: should set a partial set of colors, using the default if not present
    [Fact]
    public void A_partial_theme_uses_the_defaults_for_the_other_colors()
    {
        var palette = new ThemePalette();
        Assert.Equal("#000000", palette.Background.Css);
        Assert.Equal("#ffffff", palette.Foreground.Css);

        palette.SetTheme(new TerminalTheme { Background = "#FF0000", Foreground = "#00FF00" });
        Assert.Equal("#FF0000", palette.Background.Css);
        Assert.Equal("#00FF00", palette.Foreground.Css);

        palette.SetTheme(new TerminalTheme { Background = "#0000FF" });
        Assert.Equal("#0000FF", palette.Background.Css);
        Assert.Equal("#ffffff", palette.Foreground.Css);
    }

    // setTheme: should set all extended ansi colors in reverse order
    [Fact]
    public void All_extended_ansi_colors_can_be_set_in_reverse_order()
    {
        var palette = new ThemePalette(new TerminalTheme
        {
            ExtendedAnsi = ThemePalette.DefaultAnsiColors.Select(c => c.Css).Reverse().ToArray(),
        });

        for (var ansiColor = 16; ansiColor <= 255; ansiColor++)
        {
            Assert.Equal(ThemePalette.DefaultAnsiColors[255 + 16 - ansiColor].Css, palette.Ansi[ansiColor].Css);
        }
    }

    // setTheme: should set one extended ansi color and keep the other default
    [Fact]
    public void One_extended_ansi_color_can_be_set_and_the_others_stay_default()
    {
        var palette = new ThemePalette(new TerminalTheme { ExtendedAnsi = ["#ffffff"] });

        Assert.Equal("#ffffff", palette.Ansi[16].Css);
        Assert.Equal(ThemePalette.DefaultAnsiColors[17].Css, palette.Ansi[17].Css);
    }

    // setTheme: should set extended ansi colors to the default when they are unset
    [Fact]
    public void Extended_ansi_colors_return_to_the_default_when_unset()
    {
        var palette = new ThemePalette(new TerminalTheme { ExtendedAnsi = ["#ffffff"] });
        Assert.Equal("#ffffff", palette.Ansi[16].Css);

        palette.SetTheme(new TerminalTheme { ExtendedAnsi = [] });
        Assert.Equal(ThemePalette.DefaultAnsiColors[16].Css, palette.Ansi[16].Css);

        palette.SetTheme(new TerminalTheme { ExtendedAnsi = ["#ffffff"] });
        Assert.Equal("#ffffff", palette.Ansi[16].Css);

        palette.SetTheme(new TerminalTheme());
        Assert.Equal(ThemePalette.DefaultAnsiColors[16].Css, palette.Ansi[16].Css);
    }

    // setTheme: should set extended ansi colors to the default when they are partially unset
    [Fact]
    public void Extended_ansi_colors_return_to_the_default_when_partially_unset()
    {
        var palette = new ThemePalette(new TerminalTheme { ExtendedAnsi = ["#ffffff", "#000000"] });
        Assert.Equal("#ffffff", palette.Ansi[16].Css);
        Assert.Equal("#000000", palette.Ansi[17].Css);

        palette.SetTheme(new TerminalTheme { ExtendedAnsi = ["#ffffff"] });
        Assert.Equal("#ffffff", palette.Ansi[16].Css);
        Assert.Equal(ThemePalette.DefaultAnsiColors[17].Css, palette.Ansi[17].Css);
    }

    [Fact]
    public void An_invalid_css_color_uses_the_default()
    {
        var palette = new ThemePalette(new TerminalTheme { Foreground = "not a color", Red = "#12" });

        Assert.Equal("#ffffff", palette.Foreground.Css);
        Assert.Equal(ThemePalette.DefaultAnsiColors[1].Css, palette.Ansi[1].Css);
    }

    [Fact]
    public void An_opaque_selection_background_is_made_translucent()
    {
        var palette = new ThemePalette(new TerminalTheme { SelectionBackground = "#336699" });

        Assert.False(ColorOps.IsOpaque(palette.SelectionBackgroundTransparent));
        Assert.True(ColorOps.IsOpaque(palette.SelectionBackgroundOpaque));
    }

    [Fact]
    public void OSC_4_set_then_query_returns_the_color_as_rgb()
    {
        var palette = new ThemePalette();

        var replies = palette.Apply([new ColorSetRequest(1, (255, 0, 0)), new ColorReportRequest(1)]);

        Assert.Equal(["\u001b]4;1;rgb:ffff/0000/0000\u001b\\"], replies);
        Assert.Equal("#ff0000", palette.Ansi[1].Css);
    }

    [Fact]
    public void OSC_104_restores_the_theme_value()
    {
        var palette = new ThemePalette(new TerminalTheme { Red = "#aa0000" });
        palette.Apply([new ColorSetRequest(1, (255, 0, 0))]);

        palette.Apply([new ColorRestoreRequest(1)]);

        Assert.Equal("#aa0000", palette.Ansi[1].Css);
    }

    [Fact]
    public void Restoring_without_an_index_restores_the_whole_palette_but_not_the_special_colors()
    {
        var palette = new ThemePalette();
        palette.Apply([new ColorSetRequest(1, (1, 2, 3)), new ColorSetRequest(200, (4, 5, 6)), new ColorSetRequest(SpecialColorIndex.Foreground, (7, 8, 9))]);

        palette.Apply([new ColorRestoreRequest(null)]);

        Assert.Equal(ThemePalette.DefaultAnsiColors[1].Css, palette.Ansi[1].Css);
        Assert.Equal(ThemePalette.DefaultAnsiColors[200].Css, palette.Ansi[200].Css);
        Assert.Equal("#070809", palette.Foreground.Css);
    }

    [Fact]
    public void Special_colors_are_set_queried_and_restored_with_their_own_identifiers()
    {
        var palette = new ThemePalette();
        palette.Apply([new ColorSetRequest(SpecialColorIndex.Background, (0x10, 0x20, 0x30))]);

        var replies = palette.Apply([
            new ColorReportRequest(SpecialColorIndex.Foreground),
            new ColorReportRequest(SpecialColorIndex.Background),
            new ColorReportRequest(SpecialColorIndex.Cursor),
        ]);

        Assert.Equal(
            ["\u001b]10;rgb:ffff/ffff/ffff\u001b\\", "\u001b]11;rgb:1010/2020/3030\u001b\\", "\u001b]12;rgb:ffff/ffff/ffff\u001b\\"],
            replies);

        palette.Apply([new ColorRestoreRequest(SpecialColorIndex.Background)]);
        Assert.Equal("#000000", palette.Background.Css);
    }
}
