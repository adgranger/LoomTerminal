// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerSgrTests : InputHandlerTestBase
{
    // xterm.js: 'InputHandler' › 'text attributes' › 'bold'
    [Fact]
    public void Text_attribute_bold()
    {
        InputHandler.Parse("\x1b[1m");
        Assert.NotEqual(0u, InputHandler.CurAttrData.IsBold());
        InputHandler.Parse("\x1b[22m");
        Assert.Equal(0u, InputHandler.CurAttrData.IsBold());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'dim'
    [Fact]
    public void Text_attribute_dim()
    {
        InputHandler.Parse("\x1b[2m");
        Assert.NotEqual(0u, InputHandler.CurAttrData.IsDim());
        InputHandler.Parse("\x1b[22m");
        Assert.Equal(0u, InputHandler.CurAttrData.IsDim());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'italic'
    [Fact]
    public void Text_attribute_italic()
    {
        InputHandler.Parse("\x1b[3m");
        Assert.NotEqual(0u, InputHandler.CurAttrData.IsItalic());
        InputHandler.Parse("\x1b[23m");
        Assert.Equal(0u, InputHandler.CurAttrData.IsItalic());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'underline'
    [Fact]
    public void Text_attribute_underline()
    {
        InputHandler.Parse("\x1b[4m");
        Assert.NotEqual(0u, InputHandler.CurAttrData.IsUnderline());
        InputHandler.Parse("\x1b[24m");
        Assert.Equal(0u, InputHandler.CurAttrData.IsUnderline());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'blink'
    [Fact]
    public void Text_attribute_blink()
    {
        InputHandler.Parse("\x1b[5m");
        Assert.NotEqual(0u, InputHandler.CurAttrData.IsBlink());
        InputHandler.Parse("\x1b[25m");
        Assert.Equal(0u, InputHandler.CurAttrData.IsBlink());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'inverse'
    [Fact]
    public void Text_attribute_inverse()
    {
        InputHandler.Parse("\x1b[7m");
        Assert.NotEqual(0u, InputHandler.CurAttrData.IsInverse());
        InputHandler.Parse("\x1b[27m");
        Assert.Equal(0u, InputHandler.CurAttrData.IsInverse());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'invisible'
    [Fact]
    public void Text_attribute_invisible()
    {
        InputHandler.Parse("\x1b[8m");
        Assert.NotEqual(0u, InputHandler.CurAttrData.IsInvisible());
        InputHandler.Parse("\x1b[28m");
        Assert.Equal(0u, InputHandler.CurAttrData.IsInvisible());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'strikethrough'
    [Fact]
    public void Text_attribute_strikethrough()
    {
        InputHandler.Parse("\x1b[9m");
        Assert.NotEqual(0u, InputHandler.CurAttrData.IsStrikethrough());
        InputHandler.Parse("\x1b[29m");
        Assert.Equal(0u, InputHandler.CurAttrData.IsStrikethrough());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'colormode palette 16'
    [Fact]
    public void Text_attribute_colormode_palette_16()
    {
        Assert.Equal(0u, InputHandler.CurAttrData.GetFgColorMode());
        Assert.Equal(0u, InputHandler.CurAttrData.GetBgColorMode());
        // lower 8 colors
        for (int i = 0; i < 8; ++i)
        {
            InputHandler.Parse($"\x1b[{i + 30};{i + 40}m");
            Assert.Equal(Attributes.CmP16, InputHandler.CurAttrData.GetFgColorMode());
            Assert.Equal(i, InputHandler.CurAttrData.GetFgColor());
            Assert.Equal(Attributes.CmP16, InputHandler.CurAttrData.GetBgColorMode());
            Assert.Equal(i, InputHandler.CurAttrData.GetBgColor());
        }
        // reset to DEFAULT
        InputHandler.Parse("\x1b[39;49m");
        Assert.Equal(0u, InputHandler.CurAttrData.GetFgColorMode());
        Assert.Equal(0u, InputHandler.CurAttrData.GetBgColorMode());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'colormode palette 256'
    [Fact]
    public void Text_attribute_colormode_palette_256()
    {
        Assert.Equal(0u, InputHandler.CurAttrData.GetFgColorMode());
        Assert.Equal(0u, InputHandler.CurAttrData.GetBgColorMode());
        // lower 8 colors
        for (int i = 0; i < 256; ++i)
        {
            InputHandler.Parse($"\x1b[38;5;{i};48;5;{i}m");
            Assert.Equal(Attributes.CmP256, InputHandler.CurAttrData.GetFgColorMode());
            Assert.Equal(i, InputHandler.CurAttrData.GetFgColor());
            Assert.Equal(Attributes.CmP256, InputHandler.CurAttrData.GetBgColorMode());
            Assert.Equal(i, InputHandler.CurAttrData.GetBgColor());
        }
        // reset to DEFAULT
        InputHandler.Parse("\x1b[39;49m");
        Assert.Equal(0u, InputHandler.CurAttrData.GetFgColorMode());
        Assert.Equal(-1, InputHandler.CurAttrData.GetFgColor());
        Assert.Equal(0u, InputHandler.CurAttrData.GetBgColorMode());
        Assert.Equal(-1, InputHandler.CurAttrData.GetBgColor());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'colormode RGB'
    [Fact]
    public void Text_attribute_colormode_RGB()
    {
        Assert.Equal(0u, InputHandler.CurAttrData.GetFgColorMode());
        Assert.Equal(0u, InputHandler.CurAttrData.GetBgColorMode());
        InputHandler.Parse("\x1b[38;2;1;2;3;48;2;4;5;6m");
        Assert.Equal(Attributes.CmRgb, InputHandler.CurAttrData.GetFgColorMode());
        Assert.Equal((1 << 16) | (2 << 8) | 3, InputHandler.CurAttrData.GetFgColor());
        Assert.Equal((1, 2, 3), AttributeData.ToColorRgb((uint)InputHandler.CurAttrData.GetFgColor()));
        Assert.Equal(Attributes.CmRgb, InputHandler.CurAttrData.GetBgColorMode());
        Assert.Equal((4, 5, 6), AttributeData.ToColorRgb((uint)InputHandler.CurAttrData.GetBgColor()));
        // reset to DEFAULT
        InputHandler.Parse("\x1b[39;49m");
        Assert.Equal(0u, InputHandler.CurAttrData.GetFgColorMode());
        Assert.Equal(-1, InputHandler.CurAttrData.GetFgColor());
        Assert.Equal(0u, InputHandler.CurAttrData.GetBgColorMode());
        Assert.Equal(-1, InputHandler.CurAttrData.GetBgColor());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'colormode transition RGB to 256'
    [Fact]
    public void Text_attribute_colormode_transition_RGB_to_256()
    {
        // enter RGB for FG and BG
        InputHandler.Parse("\x1b[38;2;1;2;3;48;2;4;5;6m");
        // enter 256 for FG and BG
        InputHandler.Parse("\x1b[38;5;255;48;5;255m");
        Assert.Equal(Attributes.CmP256, InputHandler.CurAttrData.GetFgColorMode());
        Assert.Equal(255, InputHandler.CurAttrData.GetFgColor());
        Assert.Equal(Attributes.CmP256, InputHandler.CurAttrData.GetBgColorMode());
        Assert.Equal(255, InputHandler.CurAttrData.GetBgColor());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'colormode transition RGB to 16'
    [Fact]
    public void Text_attribute_colormode_transition_RGB_to_16()
    {
        // enter RGB for FG and BG
        InputHandler.Parse("\x1b[38;2;1;2;3;48;2;4;5;6m");
        // enter 16 for FG and BG
        InputHandler.Parse("\x1b[37;47m");
        Assert.Equal(Attributes.CmP16, InputHandler.CurAttrData.GetFgColorMode());
        Assert.Equal(7, InputHandler.CurAttrData.GetFgColor());
        Assert.Equal(Attributes.CmP16, InputHandler.CurAttrData.GetBgColorMode());
        Assert.Equal(7, InputHandler.CurAttrData.GetBgColor());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'colormode transition 16 to 256'
    [Fact]
    public void Text_attribute_colormode_transition_16_to_256()
    {
        // enter 16 for FG and BG
        InputHandler.Parse("\x1b[37;47m");
        // enter 256 for FG and BG
        InputHandler.Parse("\x1b[38;5;255;48;5;255m");
        Assert.Equal(Attributes.CmP256, InputHandler.CurAttrData.GetFgColorMode());
        Assert.Equal(255, InputHandler.CurAttrData.GetFgColor());
        Assert.Equal(Attributes.CmP256, InputHandler.CurAttrData.GetBgColorMode());
        Assert.Equal(255, InputHandler.CurAttrData.GetBgColor());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'colormode transition 256 to 16'
    [Fact]
    public void Text_attribute_colormode_transition_256_to_16()
    {
        // enter 256 for FG and BG
        InputHandler.Parse("\x1b[38;5;255;48;5;255m");
        // enter 16 for FG and BG
        InputHandler.Parse("\x1b[37;47m");
        Assert.Equal(Attributes.CmP16, InputHandler.CurAttrData.GetFgColorMode());
        Assert.Equal(7, InputHandler.CurAttrData.GetFgColor());
        Assert.Equal(Attributes.CmP16, InputHandler.CurAttrData.GetBgColorMode());
        Assert.Equal(7, InputHandler.CurAttrData.GetBgColor());
    }

    // xterm.js: 'InputHandler' › 'text attributes' › 'should zero missing RGB values'
    [Fact]
    public void Text_attribute_should_zero_missing_RGB_values()
    {
        InputHandler.Parse("\x1b[38;2;1;2;3m");
        InputHandler.Parse("\x1b[38;2;5m");
        Assert.Equal((5, 0, 0), AttributeData.ToColorRgb((uint)InputHandler.CurAttrData.GetFgColor()));
    }

    // xterm.js: 'InputHandler' › 'reset text attributes (SGR 0)' › 'resets all attributes if there is no url'
    [Fact]
    public void SGR_0_resets_all_attributes_if_there_is_no_url()
    {
        InputHandler.Parse("\x1b[30m\x1b[40m\x1b[4m");
        Assert.NotEqual(0u, InputHandler.CurAttrData.Fg);
        Assert.NotEqual(0u, InputHandler.CurAttrData.Bg);
        Assert.False(InputHandler.CurAttrData.Extended.IsEmpty());

        InputHandler.Parse("\x1b[m");
        Assert.Equal(0u, InputHandler.CurAttrData.Fg);
        Assert.Equal(0u, InputHandler.CurAttrData.Bg);
        Assert.True(InputHandler.CurAttrData.Extended.IsEmpty());
    }

    // xterm.js: 'InputHandler' › 'reset text attributes (SGR 0)' › 'resets all attributes except for the url'
    [Fact]
    public void SGR_0_resets_all_attributes_except_for_the_url()
    {
        InputHandler.Parse("\x1b[30m\x1b[40m\x1b[4m");
        InputHandler.Parse("\x1b]8;;http://example.com\x1b\\");
        Assert.NotEqual(0u, InputHandler.CurAttrData.Fg);
        Assert.NotEqual(0u, InputHandler.CurAttrData.Bg);
        Assert.NotEqual(0u, InputHandler.CurAttrData.Extended.Ext);
        int urlId = InputHandler.CurAttrData.Extended.UrlId;
        Assert.NotEqual(0, urlId);

        InputHandler.Parse("\x1b[m");
        Assert.Equal(0u, InputHandler.CurAttrData.Fg);
        Assert.Equal(BgFlags.HasExtended, InputHandler.CurAttrData.Bg);
        ExtendedAttrs expectedExtended = new() { UrlId = urlId };
        Assert.Equal(expectedExtended.Ext, InputHandler.CurAttrData.Extended.Ext);
        Assert.Equal(expectedExtended.UrlId, InputHandler.CurAttrData.Extended.UrlId);
    }
}
