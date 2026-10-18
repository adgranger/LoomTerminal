// Ported from xtermjs/xterm.js: src/common/input/XParseColor.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Input;

namespace LoomTerminal.Core.Tests.Input;

public class XParseColorTests
{
    private static (int R, int G, int B)? Parse(string data) => XParseColor.ParseColor(data);

    private static string RgbString(string data, int bits) => XParseColor.ToRgbString(Parse(data)!.Value, bits);

    // xterm.js: 'XParseColor' › 'parseColor' › 'rgb:<r>/<g>/<b> scheme in 4/8/12/16 bit'
    [Fact]
    public void ParseColor_reads_the_rgb_scheme_in_4_8_12_16_bit()
    {
        // 4 bit
        Assert.Equal((0, 0, 0), Parse("rgb:0/0/0"));
        Assert.Equal((255, 255, 255), Parse("rgb:f/f/f"));
        Assert.Equal((17, 34, 51), Parse("rgb:1/2/3"));

        // 8 bit
        Assert.Equal((0, 0, 0), Parse("rgb:00/00/00"));
        Assert.Equal((255, 255, 255), Parse("rgb:ff/ff/ff"));
        Assert.Equal((17, 34, 51), Parse("rgb:11/22/33"));

        // 12 bit
        Assert.Equal((0, 0, 0), Parse("rgb:000/000/000"));
        Assert.Equal((255, 255, 255), Parse("rgb:fff/fff/fff"));
        Assert.Equal((17, 34, 51), Parse("rgb:111/222/333"));

        // 16 bit
        Assert.Equal((0, 0, 0), Parse("rgb:0000/0000/0000"));
        Assert.Equal((255, 255, 255), Parse("rgb:ffff/ffff/ffff"));
        Assert.Equal((17, 34, 51), Parse("rgb:1111/2222/3333"));
    }

    // xterm.js: 'XParseColor' › 'parseColor' › '#RGB scheme in 4/8/12/16 bit'
    [Fact]
    public void ParseColor_reads_the_hash_scheme_in_4_8_12_16_bit()
    {
        // 4 bit
        Assert.Equal((0, 0, 0), Parse("#000"));
        Assert.Equal((240, 240, 240), Parse("#fff"));
        Assert.Equal((16, 32, 48), Parse("#123"));

        // 8 bit
        Assert.Equal((0, 0, 0), Parse("#000000"));
        Assert.Equal((255, 255, 255), Parse("#ffffff"));
        Assert.Equal((17, 34, 51), Parse("#112233"));

        // 12 bit
        Assert.Equal((0, 0, 0), Parse("#000000000"));
        Assert.Equal((255, 255, 255), Parse("#fffffffff"));
        Assert.Equal((17, 34, 51), Parse("#111222333"));

        // 16 bit
        Assert.Equal((0, 0, 0), Parse("#000000000000"));
        Assert.Equal((255, 255, 255), Parse("#ffffffffffff"));
        Assert.Equal((17, 34, 51), Parse("#111122223333"));
    }

    // xterm.js: 'XParseColor' › 'parseColor' › 'supports upper case'
    [Fact]
    public void ParseColor_supports_upper_case()
    {
        Assert.Equal((0, 170, 255), Parse("RGB:0/A/F"));
        Assert.Equal((240, 240, 240), Parse("#FFF"));
    }

    // xterm.js: 'XParseColor' › 'parseColor' › 'does not parse illegal combinations'
    [Fact]
    public void ParseColor_does_not_parse_illegal_combinations()
    {
        // shifting bit width
        Assert.Null(Parse("rgb:0/11/222"));

        // unsupported scheme
        Assert.Null(Parse("rgbi:00/11/22"));

        // broken # specifier
        Assert.Null(Parse("#aabbbcc"));

        // out of range
        Assert.Null(Parse("#aabbgg"));
        Assert.Null(Parse("rgb:aa/bb/gg"));
    }

    // xterm.js: 'XParseColor' › 'toXColorRgb' › 'rgb:<r>/<g>/<b> scheme in 4/8/12/16 bit'
    [Fact]
    public void ToRgbString_formats_in_4_8_12_16_bit()
    {
        // 4 bit
        Assert.Equal("rgb:0/0/0", RgbString("rgb:0/0/0", 4));
        Assert.Equal("rgb:f/f/f", RgbString("rgb:f/f/f", 4));
        Assert.Equal("rgb:1/2/3", RgbString("rgb:1/2/3", 4));

        // 8 bit
        Assert.Equal("rgb:00/00/00", RgbString("rgb:00/00/00", 8));
        Assert.Equal("rgb:ff/ff/ff", RgbString("rgb:ff/ff/ff", 8));
        Assert.Equal("rgb:11/22/33", RgbString("rgb:11/22/33", 8));

        // 12 bit
        Assert.Equal("rgb:000/000/000", RgbString("rgb:000/000/000", 12));
        Assert.Equal("rgb:fff/fff/fff", RgbString("rgb:fff/fff/fff", 12));
        Assert.Equal("rgb:111/222/333", RgbString("rgb:111/222/333", 12));

        // 16 bit
        Assert.Equal("rgb:0000/0000/0000", RgbString("rgb:0000/0000/0000", 16));
        Assert.Equal("rgb:ffff/ffff/ffff", RgbString("rgb:ffff/ffff/ffff", 16));
        Assert.Equal("rgb:1111/2222/3333", RgbString("rgb:1111/2222/3333", 16));
    }

    // xterm.js: 'XParseColor' › 'toXColorRgb' › 'defaults to 16 bit output'
    [Fact]
    public void ToRgbString_defaults_to_16_bit_output()
    {
        Assert.Equal("rgb:1111/2222/3333", XParseColor.ToRgbString(Parse("rgb:1/2/3")!.Value));
        Assert.Equal("rgb:1111/2222/3333", XParseColor.ToRgbString(Parse("rgb:11/22/33")!.Value));
        Assert.Equal("rgb:1111/2222/3333", XParseColor.ToRgbString(Parse("rgb:111/222/333")!.Value));
        Assert.Equal("rgb:1212/1212/1212", XParseColor.ToRgbString(Parse("rgb:123/123/123")!.Value));
    }

    // xterm.js: 'XParseColor' › 'toXColorRgb' › 'reduces colors to 8 bit resolution'
    [Fact]
    public void ToRgbString_reduces_colors_to_8_bit_resolution()
    {
        Assert.Equal("rgb:121/121/121", RgbString("rgb:123/123/123", 12));
        Assert.Equal("rgb:1212/1212/1212", RgbString("rgb:1234/1234/1234", 16));
    }
}
