// Ported from xtermjs/xterm.js: src/browser/ColorContrastCache.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;
using LoomTerminal.Presentation.Theming;

namespace LoomTerminal.Presentation.Tests.Theming;

public class ColorContrastCacheTests
{
    // should save and get color values
    [Fact]
    public void Color_values_are_saved_and_returned()
    {
        var cache = new ColorContrastCache();

        Assert.False(cache.TryGetColor(0x01, 0x00, out _));
        cache.SetColor(0x01, 0x01, null);
        Assert.True(cache.TryGetColor(0x01, 0x01, out var none));
        Assert.Null(none);
        cache.SetColor(0x01, 0x02, new TerminalColor("#030303", 0x030303ff));
        Assert.True(cache.TryGetColor(0x01, 0x02, out var color));
        Assert.Equal(new TerminalColor("#030303", 0x030303ff), color);
    }

    // should save and get css values
    [Fact]
    public void Css_values_are_saved_and_returned()
    {
        var cache = new ColorContrastCache();

        Assert.False(cache.TryGetCss(0x01, 0x00, out _));
        cache.SetCss(0x01, 0x01, null);
        Assert.True(cache.TryGetCss(0x01, 0x01, out var none));
        Assert.Null(none);
        cache.SetCss(0x01, 0x02, "#030303");
        Assert.True(cache.TryGetCss(0x01, 0x02, out var css));
        Assert.Equal("#030303", css);
    }

    // should clear all values on clear
    [Fact]
    public void Clear_forgets_all_values()
    {
        var cache = new ColorContrastCache();
        cache.SetColor(0x01, 0x01, null);
        cache.SetColor(0x01, 0x02, new TerminalColor("#030303", 0x030303ff));
        cache.SetCss(0x01, 0x01, null);
        cache.SetCss(0x01, 0x02, "#030303");

        cache.Clear();

        Assert.False(cache.TryGetColor(0x01, 0x01, out _));
        Assert.False(cache.TryGetColor(0x01, 0x02, out _));
        Assert.False(cache.TryGetCss(0x01, 0x01, out _));
        Assert.False(cache.TryGetCss(0x01, 0x02, out _));
    }
}
