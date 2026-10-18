// Inspired by xtermjs/xterm.js: addons/addon-unicode11/src/UnicodeV11.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Input;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.Input;

// The addon has no unit tests at the pinned commit: the expected values below were checked against its tables.
public class UnicodeV11Tests
{
    private readonly UnicodeV11 _v11 = new();
    private readonly UnicodeV6 _v6 = new();

    [Theory]
    [InlineData('a', 1)]
    [InlineData(0x0301, 0)]
    [InlineData(0x4E00, 2)]
    [InlineData(0x1F600, 2)]
    [InlineData(0xFF01, 2)]
    [InlineData(0x200B, 0)]
    public void Wcwidth_of_V11_matches_the_upstream_tables(int codepoint, int expected) =>
        Assert.Equal(expected, _v11.Wcwidth(codepoint));

    [Fact]
    public void An_emoji_is_narrow_in_V6_and_wide_in_V11()
    {
        Assert.Equal(1, _v6.Wcwidth(0x1F600));
        Assert.Equal(2, _v11.Wcwidth(0x1F600));
    }

    [Fact]
    public void A_fresh_service_is_on_version_11_and_knows_6()
    {
        using UnicodeService service = new();
        Assert.Equal("11", service.ActiveVersion);
        Assert.Contains("6", service.Versions);
        Assert.Contains("11", service.Versions);
        Assert.Equal(2, service.Wcwidth(0x1F600));
    }
}
