// Ported from xtermjs/xterm.js: src/common/Color.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Globalization;
using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Tests.Common;

// The xterm.js tests that exercise the canvas fallback of css.toColor do not exist at this commit: every css test uses
// hex or rgb()/rgba(). Named colors are not parsed (see docs/PORTING.md).
public class ColorTests
{
    // The channel values used by the xterm.js loops: 0x00, 0x10, … 0xf0, 0xff.
    public static IEnumerable<int> Steps { get; } = [.. Enumerable.Range(0, 16).Select(i => i * 0x10), 0xff];

    private static string Hex2(int v) => v.ToString("x2", CultureInfo.InvariantCulture);

    private static uint Rgba4(int v) => (uint)v * 0x01010101;

    private static uint Rgb3(int v) => (uint)v * 0x010101;

    // xterm.js: 'Color' › 'channels' › 'toCss' › 'should convert an rgb array to css hex string'
    [Fact]
    public void ToCss_converts_rgb_channels_to_a_css_hex_string()
    {
        foreach (int v in Steps)
        {
            Assert.Equal($"#{Hex2(v)}{Hex2(v)}{Hex2(v)}", Channels.ToCss(v, v, v));
        }
    }

    // xterm.js: 'Color' › 'channels' › 'toCss' › 'should convert an rgba array to css hex string'
    [Fact]
    public void ToCss_converts_rgba_channels_to_a_css_hex_string()
    {
        foreach (int v in Steps)
        {
            Assert.Equal($"#{Hex2(v)}{Hex2(v)}{Hex2(v)}{Hex2(v)}", Channels.ToCss(v, v, v, v));
        }
    }

    // xterm.js: 'Color' › 'channels' › 'toRgba' › 'should convert an rgb array to an rgba number'
    [Fact]
    public void ToRgba_converts_rgb_channels_to_an_rgba_number()
    {
        foreach (int v in Steps)
        {
            Assert.Equal((Rgb3(v) << 8) | 0xFF, Channels.ToRgba(v, v, v));
        }
    }

    // xterm.js: 'Color' › 'channels' › 'toRgba' › 'should convert an rgba array to an rgba number'
    [Fact]
    public void ToRgba_converts_rgba_channels_to_an_rgba_number()
    {
        foreach (int v in Steps)
        {
            Assert.Equal(Rgba4(v), Channels.ToRgba(v, v, v, v));
        }
    }

    // xterm.js: 'Color' › 'channels' › 'toColor' › 'should convert an rgb array to an IColor'
    [Fact]
    public void ToColor_converts_rgb_channels_to_a_color()
    {
        foreach (int v in Steps)
        {
            Assert.Equal(new TerminalColor($"#{Hex2(v)}{Hex2(v)}{Hex2(v)}", (Rgb3(v) << 8) | 0xFF), Channels.ToColor(v, v, v));
        }
    }

    // xterm.js: 'Color' › 'channels' › 'toColor' › 'should convert an rgba array to an IColor'
    [Fact]
    public void ToColor_converts_rgba_channels_to_a_color()
    {
        foreach (int v in Steps)
        {
            Assert.Equal(new TerminalColor($"#{Hex2(v)}{Hex2(v)}{Hex2(v)}{Hex2(v)}", Rgba4(v)), Channels.ToColor(v, v, v, v));
        }
    }

    // xterm.js: 'Color' › 'color' › 'blend' › 'should blend colors based on the alpha channel'
    [Fact]
    public void ColorOps_Blend_blends_colors_based_on_the_alpha_channel()
    {
        TerminalColor black = new("#000000", 0x000000FF);
        foreach (int v in Steps.Where(v => v != 0xff))
        {
            TerminalColor fg = new($"#FFFFFF{v:X2}", 0xFFFFFF00 | (uint)v);
            Assert.Equal(new TerminalColor($"#{Hex2(v)}{Hex2(v)}{Hex2(v)}", (Rgb3(v) << 8) | 0xFF), ColorOps.Blend(black, fg));
        }

        Assert.Equal(new TerminalColor("#000000", 0x000000FF), ColorOps.Blend(black, new TerminalColor("#FFFFFF00", 0xFFFFFF00)));
        Assert.Equal(new TerminalColor("#FFFFFFFF", 0xFFFFFFFF), ColorOps.Blend(black, new TerminalColor("#FFFFFFFF", 0xFFFFFFFF)));
    }

    // xterm.js: 'Color' › 'color' › 'opaque' › 'should make the color opaque'
    [Fact]
    public void ColorOps_Opaque_makes_the_color_opaque()
    {
        foreach (int v in Steps)
        {
            TerminalColor input = new($"#{Hex2(v)}{Hex2(v)}{Hex2(v)}{Hex2(v)}", Rgba4(v));
            Assert.Equal(new TerminalColor($"#{Hex2(v)}{Hex2(v)}{Hex2(v)}", (Rgb3(v) << 8) | 0xFF), ColorOps.Opaque(input));
        }
    }

    // xterm.js: 'Color' › 'color' › 'isOpaque' › 'should return true for opaque colors'
    [Theory]
    [InlineData("#000000")]
    [InlineData("#000000ff")]
    [InlineData("#808080")]
    [InlineData("#808080ff")]
    [InlineData("#ffffff")]
    [InlineData("#ffffffff")]
    public void IsOpaque_is_true_for_opaque_colors(string css) => Assert.True(ColorOps.IsOpaque(Css.ToColor(css)));

    // xterm.js: 'Color' › 'color' › 'isOpaque' › 'should return false for transparent colors'
    [Theory]
    [InlineData("#00000000")]
    [InlineData("#00000080")]
    [InlineData("#000000fe")]
    [InlineData("#80808000")]
    [InlineData("#80808080")]
    [InlineData("#808080fe")]
    [InlineData("#ffffff00")]
    [InlineData("#ffffff80")]
    [InlineData("#fffffffe")]
    public void IsOpaque_is_false_for_transparent_colors(string css) => Assert.False(ColorOps.IsOpaque(Css.ToColor(css)));

    // xterm.js: 'Color' › 'color' › 'opacity' › 'should make the color transparent'
    [Fact]
    public void ColorOps_Opacity_makes_the_color_transparent()
    {
        TerminalColor black = Css.ToColor("#000000");
        Assert.Equal(new TerminalColor("#00000000", 0x00000000), ColorOps.Opacity(black, 0));
        Assert.Equal(new TerminalColor("#00000040", 0x00000040), ColorOps.Opacity(black, 0.25));
        Assert.Equal(new TerminalColor("#00000080", 0x00000080), ColorOps.Opacity(black, 0.5));
        Assert.Equal(new TerminalColor("#000000bf", 0x000000bf), ColorOps.Opacity(black, 0.75));
        Assert.Equal(new TerminalColor("#000000ff", 0x000000ff), ColorOps.Opacity(black, 1));
    }

    // xterm.js: 'Color' › 'css' › 'toColor' › 'should convert the #rgb format to an IColor'
    [Fact]
    public void ToColor_converts_the_rgb_format()
    {
        foreach (int n in Enumerable.Range(0, 16).Append(15))
        {
            string d = n.ToString("x", CultureInfo.InvariantCulture);
            Assert.Equal(new TerminalColor($"#{d}{d}{d}{d}{d}{d}", ((uint)n * 0x111111 << 8) | 0xFF), Css.ToColor($"#{d}{d}{d}"));
        }
    }

    // xterm.js: 'Color' › 'css' › 'toColor' › 'should convert the #rgb format to an IColor' (the second, #rgba, test)
    [Fact]
    public void ToColor_converts_the_rgba_short_format()
    {
        foreach (int n in Enumerable.Range(0, 16).Append(15))
        {
            string d = n.ToString("x", CultureInfo.InvariantCulture);
            Assert.Equal(new TerminalColor($"#{d}{d}{d}{d}{d}{d}{d}{d}", (uint)n * 0x11111111), Css.ToColor($"#{d}{d}{d}{d}"));
        }
    }

    // xterm.js: 'Color' › 'css' › 'toColor' › 'should convert the #rrggbb format to an IColor'
    [Fact]
    public void ToColor_converts_the_rrggbb_format()
    {
        foreach (int v in Steps)
        {
            string css = $"#{Hex2(v)}{Hex2(v)}{Hex2(v)}";
            Assert.Equal(new TerminalColor(css, (Rgb3(v) << 8) | 0xFF), Css.ToColor(css));
        }
    }

    // xterm.js: 'Color' › 'css' › 'toColor' › 'should convert the #rrggbbaa format to an IColor'
    [Fact]
    public void ToColor_converts_the_rrggbbaa_format()
    {
        foreach (int v in Steps)
        {
            string css = $"#{Hex2(v)}{Hex2(v)}{Hex2(v)}{Hex2(v)}";
            Assert.Equal(new TerminalColor(css, Rgba4(v)), Css.ToColor(css));
        }
    }

    // xterm.js: 'Color' › 'css' › 'toColor' › 'should convert the rgb() format to an IColor'
    [Fact]
    public void ToColor_converts_the_rgb_function_format()
    {
        Assert.Equal(new TerminalColor("#000000ff", 0x000000ff), Css.ToColor("rgb(0, 0, 0)"));
        Assert.Equal(new TerminalColor("#500000ff", 0x500000ff), Css.ToColor("rgb(80, 0, 0)"));
        Assert.Equal(new TerminalColor("#005000ff", 0x005000ff), Css.ToColor("rgb(0, 80, 0)"));
        Assert.Equal(new TerminalColor("#000050ff", 0x000050ff), Css.ToColor("rgb(0, 0, 80)"));
        Assert.Equal(new TerminalColor("#ffffffff", 0xffffffff), Css.ToColor("rgb(255, 255, 255)"));
    }

    // xterm.js: 'Color' › 'css' › 'toColor' › 'should convert the rgba() format to an IColor'
    [Fact]
    public void ToColor_converts_the_rgba_function_format()
    {
        Assert.Equal(new TerminalColor("#00000000", 0x00000000), Css.ToColor("rgba(0, 0, 0, 0)"));
        Assert.Equal(new TerminalColor("#50000080", 0x50000080), Css.ToColor("rgba(80, 0, 0, 0.5)"));
        Assert.Equal(new TerminalColor("#00500080", 0x00500080), Css.ToColor("rgba(0, 80, 0, 0.5)"));
        Assert.Equal(new TerminalColor("#00005080", 0x00005080), Css.ToColor("rgba(0, 0, 80, 0.5)"));
        Assert.Equal(new TerminalColor("#ffffffff", 0xffffffff), Css.ToColor("rgba(255, 255, 255, 1)"));
    }

    [Theory]
    [InlineData("red")]
    [InlineData("hsl(0, 100%, 50%)")]
    [InlineData("#12")]
    [InlineData("")]
    public void ToColor_throws_for_formats_that_need_a_canvas(string css) =>
        Assert.Equal(
            "css.toColor: Unsupported css format",
            Assert.Throws<ArgumentException>(() => Css.ToColor(css)).Message);

    // xterm.js: 'Color' › 'rgb' › 'relativeLuminance' › 'should calculate the relative luminance of the color'
    [Fact]
    public void RelativeLuminance_of_gray_levels()
    {
        string[] expected =
        [
            "0.0052", "0.0144", "0.0296", "0.0513", "0.0802", "0.1170", "0.1620", "0.2159",
            "0.2789", "0.3515", "0.4342", "0.5271", "0.6308", "0.7454", "0.8714",
        ];
        Assert.Equal(0, Rgb.RelativeLuminance(0x000000));
        for (int i = 1; i <= 15; i++)
        {
            Assert.Equal(expected[i - 1], Rgb.RelativeLuminance(Rgb3(i * 0x10)).ToString("F4", CultureInfo.InvariantCulture));
        }

        Assert.Equal(1, Rgb.RelativeLuminance(0xFFFFFF));
    }

    // xterm.js: 'Color' › 'rgba' › 'blend' › 'should blend colors based on the alpha channel'
    [Fact]
    public void Rgba_Blend_blends_colors_based_on_the_alpha_channel()
    {
        foreach (int v in Steps.Where(v => v != 0xff))
        {
            Assert.Equal((Rgb3(v) << 8) | 0xFF, Rgba.Blend(0x000000FF, 0xFFFFFF00 | (uint)v));
        }

        Assert.Equal(0xFFFFFFFFu, Rgba.Blend(0x000000FF, 0xFFFFFFFF));
    }

    // xterm.js: 'Color' › 'rgba' › 'ensureContrastRatio' › 'should return undefined if the color already meets the contrast ratio (black bg)'
    [Fact]
    public void EnsureContrastRatio_returns_null_when_the_ratio_is_met_on_a_black_background()
    {
        foreach (int ratio in new[] { 1, 2, 3 })
        {
            Assert.Null(Rgba.EnsureContrastRatio(0x000000ff, 0x606060ff, ratio));
        }
    }

    // xterm.js: 'Color' › 'rgba' › 'ensureContrastRatio' › 'should return a color that meets the contrast ratio (black bg)'
    [Fact]
    public void EnsureContrastRatio_returns_a_color_that_meets_the_ratio_on_a_black_background()
    {
        uint[] expected =
        [
            0x707070ff, 0x7f7f7fff, 0x8c8c8cff, 0x989898ff, 0xa3a3a3ff, 0xadadadff, 0xb6b6b6ff, 0xbebebeff, 0xc5c5c5ff,
            0xd1d1d1ff, 0xd6d6d6ff, 0xdbdbdbff, 0xe3e3e3ff, 0xe9e9e9ff, 0xeeeeeeff, 0xf4f4f4ff, 0xfafafaff, 0xffffffff,
        ];
        for (int ratio = 4; ratio <= 21; ratio++)
        {
            Assert.Equal(expected[ratio - 4], Rgba.EnsureContrastRatio(0x000000ff, 0x606060ff, ratio));
        }
    }

    // xterm.js: 'Color' › 'rgba' › 'ensureContrastRatio' › 'should return undefined if the color already meets the contrast ratio (white bg)'
    [Fact]
    public void EnsureContrastRatio_returns_null_when_the_ratio_is_met_on_a_white_background()
    {
        for (int ratio = 1; ratio <= 6; ratio++)
        {
            Assert.Null(Rgba.EnsureContrastRatio(0xffffffff, 0x606060ff, ratio));
        }
    }

    // xterm.js: 'Color' › 'rgba' › 'ensureContrastRatio' › 'should return a color that meets the contrast ratio (white bg)'
    [Fact]
    public void EnsureContrastRatio_returns_a_color_that_meets_the_ratio_on_a_white_background()
    {
        uint[] expected =
        [
            0x565656ff, 0x4d4d4dff, 0x454545ff, 0x3e3e3eff, 0x373737ff, 0x313131ff, 0x313131ff, 0x272727ff, 0x232323ff,
            0x1f1f1fff, 0x1b1b1bff, 0x151515ff, 0x101010ff, 0x080808ff, 0x000000ff,
        ];
        for (int ratio = 7; ratio <= 21; ratio++)
        {
            Assert.Equal(expected[ratio - 7], Rgba.EnsureContrastRatio(0xffffffff, 0x606060ff, ratio));
        }
    }

    // xterm.js: 'Color' › 'rgba' › 'toChannels' › 'should convert an rgba number to an rgba array'
    [Fact]
    public void ToChannels_converts_an_rgba_number_to_channels()
    {
        foreach (int v in Steps)
        {
            Assert.Equal((v, v, v, v), Rgba.ToChannels(Rgba4(v)));
        }
    }

    // xterm.js: 'Color' › 'toPaddedHex' › 'should convert numbers to 2-digit hex values'
    [Fact]
    public void ToPaddedHex_converts_numbers_to_two_digit_hex_values()
    {
        foreach (int v in Steps)
        {
            Assert.Equal(Hex2(v), ColorMath.ToPaddedHex(v));
        }

        Assert.Equal("00", ColorMath.ToPaddedHex(0));
        Assert.Equal("0a", ColorMath.ToPaddedHex(10));
    }

    // xterm.js: 'Color' › 'contrastRatio' › 'should calculate the relative luminance of the color'
    [Fact]
    public void ContrastRatio_of_luminance_values()
    {
        Assert.Equal(1, ColorMath.ContrastRatio(0, 0));
        Assert.Equal(11, ColorMath.ContrastRatio(0, 0.5));
        Assert.Equal(21, ColorMath.ContrastRatio(0, 1));
    }

    // xterm.js: 'Color' › 'contrastRatio' › 'should work regardless of the parameter order'
    [Fact]
    public void ContrastRatio_does_not_depend_on_the_parameter_order()
    {
        Assert.Equal(21, ColorMath.ContrastRatio(0, 1));
        Assert.Equal(21, ColorMath.ContrastRatio(1, 0));
    }
}
