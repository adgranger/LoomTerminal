// Ported from xtermjs/xterm.js: src/common/buffer/BufferLine.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.Buffer;

public class AttributeDataTests
{
    private static readonly uint[] ColorModes = [Attributes.CmDefault, Attributes.CmP16, Attributes.CmP256, Attributes.CmRgb];

    // xterm.js: 'AttributeData' › 'extended attributes' › 'hasExtendedAttrs'
    [Fact]
    public void HasExtendedAttrs()
    {
        AttributeData attrs = new();
        Assert.False(attrs.HasExtendedAttrs() != 0);
        attrs.Bg |= BgFlags.HasExtended;
        Assert.True(attrs.HasExtendedAttrs() != 0);
    }

    // xterm.js: 'AttributeData' › 'extended attributes' › 'getUnderlineColor - P256'
    [Fact]
    public void GetUnderlineColor_P256()
    {
        AttributeData attrs = new();

        // set a P256 color
        attrs.Extended.UnderlineColor = Attributes.CmP256 | 45;

        // should use FG color if BgFlags.HAS_EXTENDED is not set
        Assert.Equal(-1, attrs.GetUnderlineColor());

        // should use underlineColor if BgFlags.HAS_EXTENDED is set and underlineColor holds a value
        attrs.Bg |= BgFlags.HasExtended;
        Assert.Equal(45, attrs.GetUnderlineColor());

        // should use FG color if underlineColor holds no value
        attrs.Extended.UnderlineColor = 0;
        attrs.Fg |= Attributes.CmP256 | 123;
        Assert.Equal(123, attrs.GetUnderlineColor());
    }

    // xterm.js: 'AttributeData' › 'extended attributes' › 'getUnderlineColor - RGB'
    [Fact]
    public void GetUnderlineColor_RGB()
    {
        AttributeData attrs = new();

        // set an RGB color
        attrs.Extended.UnderlineColor = Attributes.CmRgb | (1 << 16) | (2 << 8) | 3;

        // should use FG color if BgFlags.HAS_EXTENDED is not set
        Assert.Equal(-1, attrs.GetUnderlineColor());

        // should use underlineColor if BgFlags.HAS_EXTENDED is set and underlineColor holds a value
        attrs.Bg |= BgFlags.HasExtended;
        Assert.Equal((1 << 16) | (2 << 8) | 3, attrs.GetUnderlineColor());

        // should use FG color if underlineColor holds no value
        attrs.Extended.UnderlineColor = 0;
        attrs.Fg |= Attributes.CmP256 | 123;
        Assert.Equal(123, attrs.GetUnderlineColor());
    }

    // xterm.js: 'AttributeData' › 'extended attributes' › 'getUnderlineColorMode / isUnderlineColorRGB / isUnderlineColorPalette / isUnderlineColorDefault'
    [Fact]
    public void GetUnderlineColorMode_and_the_underline_color_predicates()
    {
        AttributeData attrs = new();

        // should always return color mode of fg
        foreach (uint mode in ColorModes)
        {
            attrs.Extended.UnderlineColor = mode;
            Assert.Equal(attrs.GetFgColorMode(), attrs.GetUnderlineColorMode());
            Assert.True(attrs.IsUnderlineColorDefault());
        }

        attrs.Fg = Attributes.CmRgb;
        foreach (uint mode in ColorModes)
        {
            attrs.Extended.UnderlineColor = mode;
            Assert.Equal(attrs.GetFgColorMode(), attrs.GetUnderlineColorMode());
            Assert.False(attrs.IsUnderlineColorDefault());
            Assert.True(attrs.IsUnderlineColorRgb());
        }

        // should return own mode
        attrs.Bg |= BgFlags.HasExtended;
        attrs.Extended.UnderlineColor = Attributes.CmDefault;
        Assert.Equal(Attributes.CmDefault, attrs.GetUnderlineColorMode());
        attrs.Extended.UnderlineColor = Attributes.CmP16;
        Assert.Equal(Attributes.CmP16, attrs.GetUnderlineColorMode());
        Assert.True(attrs.IsUnderlineColorPalette());
        attrs.Extended.UnderlineColor = Attributes.CmP256;
        Assert.Equal(Attributes.CmP256, attrs.GetUnderlineColorMode());
        Assert.True(attrs.IsUnderlineColorPalette());
        attrs.Extended.UnderlineColor = Attributes.CmRgb;
        Assert.Equal(Attributes.CmRgb, attrs.GetUnderlineColorMode());
        Assert.True(attrs.IsUnderlineColorRgb());
    }

    // xterm.js: 'AttributeData' › 'extended attributes' › 'getUnderlineStyle'
    [Fact]
    public void GetUnderlineStyle()
    {
        AttributeData attrs = new();

        // defaults to no underline style
        Assert.Equal(UnderlineStyle.None, attrs.GetUnderlineStyle());

        // should return NONE if UNDERLINE is not set
        attrs.Extended.UnderlineStyle = UnderlineStyle.Curly;
        Assert.Equal(UnderlineStyle.None, attrs.GetUnderlineStyle());

        // should return SINGLE style if UNDERLINE is set and HAS_EXTENDED is false
        attrs.Fg |= FgFlags.Underline;
        Assert.Equal(UnderlineStyle.Single, attrs.GetUnderlineStyle());

        // should return correct style if both is set
        attrs.Bg |= BgFlags.HasExtended;
        Assert.Equal(UnderlineStyle.Curly, attrs.GetUnderlineStyle());

        // should return NONE if UNDERLINE is not set, but HAS_EXTENDED is true
        attrs.Fg &= ~FgFlags.Underline;
        Assert.Equal(UnderlineStyle.None, attrs.GetUnderlineStyle());
    }

    // xterm.js: 'AttributeData' › 'extended attributes' › 'getUnderlineVariantOffset'
    [Fact]
    public void GetUnderlineVariantOffset()
    {
        AttributeData attrs = new();

        // defaults to no offset
        Assert.Equal(0, attrs.GetUnderlineVariantOffset());

        // should return 0 - 7
        for (int i = 0; i < 8; ++i)
        {
            attrs.Extended.UnderlineVariantOffset = i;
            Assert.Equal(i, attrs.GetUnderlineVariantOffset());
        }
    }

    // New tests: the rest of AttributeData is covered by xterm.js only through other suites.
    [Fact]
    public void ToColorRgb_and_FromColorRgb_round_trip()
    {
        uint color = AttributeData.FromColorRgb((1, 2, 3));
        Assert.Equal((1u << 16) | (2u << 8) | 3u, color);
        Assert.Equal((1, 2, 3), AttributeData.ToColorRgb(color));
    }

    [Fact]
    public void Clone_copies_the_attributes_and_the_extended_attributes()
    {
        AttributeData attrs = new() { Fg = FgFlags.Bold | Attributes.CmP256 | 7 };
        attrs.Extended.UnderlineStyle = UnderlineStyle.Double;
        attrs.Extended.UrlId = 5;
        attrs.UpdateExtended();

        AttributeData clone = attrs.Clone();
        Assert.Equal(attrs.Fg, clone.Fg);
        Assert.Equal(attrs.Bg, clone.Bg);
        Assert.NotSame(attrs.Extended, clone.Extended);
        Assert.Equal(attrs.Extended.Ext, clone.Extended.Ext);
        Assert.Equal(5, clone.Extended.UrlId);
        Assert.True(clone.HasExtendedAttrs() != 0);
    }

    [Fact]
    public void ExtendedAttrs_with_a_hyperlink_reports_the_dashed_style()
    {
        ExtendedAttrs ext = new() { UrlId = 1 };
        Assert.Equal(UnderlineStyle.Dashed, ext.UnderlineStyle);
        Assert.False(ext.IsEmpty());
        Assert.Equal((uint)UnderlineStyle.Dashed << 26, ext.Ext & ExtFlags.UnderlineStyle);
    }

    [Fact]
    public void ExtendedAttrs_is_empty_without_underline_style_and_hyperlink()
    {
        ExtendedAttrs ext = new();
        Assert.True(ext.IsEmpty());
        ext.UnderlineStyle = UnderlineStyle.Single;
        Assert.False(ext.IsEmpty());
    }

    [Fact]
    public void Flags_report_the_masked_bits()
    {
        AttributeData attrs = new() { Fg = FgFlags.Bold | FgFlags.Inverse, Bg = BgFlags.Italic };
        Assert.Equal(FgFlags.Bold, attrs.IsBold());
        Assert.Equal(FgFlags.Inverse, attrs.IsInverse());
        Assert.Equal(BgFlags.Italic, attrs.IsItalic());
        Assert.Equal(0u, attrs.IsDim());
        Assert.False(attrs.IsAttributeDefault());
        Assert.True(attrs.IsFgDefault());
        Assert.Equal(-1, attrs.GetFgColor());
    }
}
