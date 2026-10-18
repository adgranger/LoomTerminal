// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerSgrUnderlineTests : InputHandlerTestBase
{
    public InputHandlerSgrUnderlineTests() => BufferService.Resize(10, 5);

    // xterm.js: 'InputHandler' › 'extended underline style support (SGR 4)' › '4 | 24'
    [Fact]
    public void SGR_4_then_24_switches_a_single_underline_on_and_off()
    {
        InputHandler.Parse("\x1b[4m");
        Assert.Equal(UnderlineStyle.Single, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[24m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
    }

    // xterm.js: 'InputHandler' › 'extended underline style support (SGR 4)' › '21 | 24'
    [Fact]
    public void SGR_21_then_24_switches_a_double_underline_on_and_off()
    {
        InputHandler.Parse("\x1b[21m");
        Assert.Equal(UnderlineStyle.Double, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[24m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
    }

    // xterm.js: 'InputHandler' › 'extended underline style support (SGR 4)' › '4:1 | 4:0'
    [Fact]
    public void SGR_4_colon_1_then_4_colon_0_switches_the_single_underline_on_and_off()
    {
        InputHandler.Parse("\x1b[4:1m");
        Assert.Equal(UnderlineStyle.Single, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[4:0m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[4:1m");
        Assert.Equal(UnderlineStyle.Single, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[24m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
    }

    // xterm.js: 'InputHandler' › 'extended underline style support (SGR 4)' › '4:2 | 4:0'
    [Fact]
    public void SGR_4_colon_2_then_4_colon_0_switches_the_double_underline_on_and_off()
    {
        InputHandler.Parse("\x1b[4:2m");
        Assert.Equal(UnderlineStyle.Double, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[4:0m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[4:2m");
        Assert.Equal(UnderlineStyle.Double, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[24m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
    }

    // xterm.js: 'InputHandler' › 'extended underline style support (SGR 4)' › '4:3 | 4:0'
    [Fact]
    public void SGR_4_colon_3_then_4_colon_0_switches_the_curly_underline_on_and_off()
    {
        InputHandler.Parse("\x1b[4:3m");
        Assert.Equal(UnderlineStyle.Curly, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[4:0m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[4:3m");
        Assert.Equal(UnderlineStyle.Curly, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[24m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
    }

    // xterm.js: 'InputHandler' › 'extended underline style support (SGR 4)' › '4:4 | 4:0'
    [Fact]
    public void SGR_4_colon_4_then_4_colon_0_switches_the_dotted_underline_on_and_off()
    {
        InputHandler.Parse("\x1b[4:4m");
        Assert.Equal(UnderlineStyle.Dotted, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[4:0m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[4:4m");
        Assert.Equal(UnderlineStyle.Dotted, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[24m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
    }

    // xterm.js: 'InputHandler' › 'extended underline style support (SGR 4)' › '4:5 | 4:0'
    [Fact]
    public void SGR_4_colon_5_then_4_colon_0_switches_the_dashed_underline_on_and_off()
    {
        InputHandler.Parse("\x1b[4:5m");
        Assert.Equal(UnderlineStyle.Dashed, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[4:0m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[4:5m");
        Assert.Equal(UnderlineStyle.Dashed, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[24m");
        Assert.Equal(UnderlineStyle.None, InputHandler.CurAttrData.GetUnderlineStyle());
    }

    // xterm.js: 'InputHandler' › 'extended underline style support (SGR 4)' › '4:x --> 4 should revert to single underline'
    [Fact]
    public void SGR_4_reverts_a_styled_underline_to_a_single_underline()
    {
        InputHandler.Parse("\x1b[4:5m");
        Assert.Equal(UnderlineStyle.Dashed, InputHandler.CurAttrData.GetUnderlineStyle());
        InputHandler.Parse("\x1b[4m");
        Assert.Equal(UnderlineStyle.Single, InputHandler.CurAttrData.GetUnderlineStyle());
    }

    private static void AssertUnderlineColorFollowsForeground(AttributeData attr)
    {
        Assert.Equal(attr.GetFgColor(), attr.GetUnderlineColor());
        Assert.Equal(attr.GetFgColorMode(), attr.GetUnderlineColorMode());
        Assert.Equal(attr.IsFgRgb(), attr.IsUnderlineColorRgb());
        Assert.Equal(attr.IsFgPalette(), attr.IsUnderlineColorPalette());
        Assert.Equal(attr.IsFgDefault(), attr.IsUnderlineColorDefault());
    }

    // xterm.js: 'InputHandler' › 'underline colors (SGR 58 & SGR 59)' › 'defaults to FG color'
    [Fact]
    public void Underline_color_defaults_to_the_foreground_color()
    {
        foreach (string s in new[] { string.Empty, "\x1b[30m", "\x1b[38;510m", "\x1b[38;2;1;2;3m" })
        {
            InputHandler.Parse(s);
            AssertUnderlineColorFollowsForeground(InputHandler.CurAttrData);
        }
    }

    // xterm.js: 'InputHandler' › 'underline colors (SGR 58 & SGR 59)' › 'correctly sets P256/RGB colors'
    [Fact]
    public void SGR_58_sets_P256_and_RGB_underline_colors()
    {
        InputHandler.Parse("\x1b[4m");
        InputHandler.Parse("\x1b[58;5;123m");
        Assert.Equal(123, InputHandler.CurAttrData.GetUnderlineColor());
        Assert.Equal(Attributes.CmP256, InputHandler.CurAttrData.GetUnderlineColorMode());
        Assert.False(InputHandler.CurAttrData.IsUnderlineColorRgb());
        Assert.True(InputHandler.CurAttrData.IsUnderlineColorPalette());
        Assert.False(InputHandler.CurAttrData.IsUnderlineColorDefault());
        InputHandler.Parse("\x1b[58;2::1:2:3m");
        Assert.Equal((1 << 16) | (2 << 8) | 3, InputHandler.CurAttrData.GetUnderlineColor());
        Assert.Equal(Attributes.CmRgb, InputHandler.CurAttrData.GetUnderlineColorMode());
        Assert.True(InputHandler.CurAttrData.IsUnderlineColorRgb());
        Assert.False(InputHandler.CurAttrData.IsUnderlineColorPalette());
        Assert.False(InputHandler.CurAttrData.IsUnderlineColorDefault());
    }

    private static void AssertCellUnderlineColorP256(CellData cell, int value)
    {
        Assert.Equal(value, cell.GetUnderlineColor());
        Assert.Equal(Attributes.CmP256, cell.GetUnderlineColorMode());
        Assert.False(cell.IsUnderlineColorRgb());
        Assert.True(cell.IsUnderlineColorPalette());
        Assert.False(cell.IsUnderlineColorDefault());
    }

    // xterm.js: 'InputHandler' › 'underline colors (SGR 58 & SGR 59)' › 'P256/RGB persistence'
    [Fact]
    public void Underline_colors_persist_in_the_cells()
    {
        CellData cell = new();
        InputHandler.Parse("\x1b[4m");
        InputHandler.Parse("\x1b[58;5;123m");
        Assert.Equal(123, InputHandler.CurAttrData.GetUnderlineColor());
        Assert.Equal(Attributes.CmP256, InputHandler.CurAttrData.GetUnderlineColorMode());
        Assert.False(InputHandler.CurAttrData.IsUnderlineColorRgb());
        Assert.True(InputHandler.CurAttrData.IsUnderlineColorPalette());
        Assert.False(InputHandler.CurAttrData.IsUnderlineColorDefault());
        InputHandler.Parse("ab");
        BufferService.Buffer.Lines.Get(0)!.LoadCell(1, cell);
        AssertCellUnderlineColorP256(cell, 123);

        InputHandler.Parse("\x1b[4:0m");
        AssertUnderlineColorFollowsForeground(InputHandler.CurAttrData);
        InputHandler.Parse("a");
        BufferService.Buffer.Lines.Get(0)!.LoadCell(1, cell);
        AssertCellUnderlineColorP256(cell, 123);
        BufferService.Buffer.Lines.Get(0)!.LoadCell(2, cell);
        AssertUnderlineColorFollowsForeground(cell);

        InputHandler.Parse("\x1b[4m");
        InputHandler.Parse("\x1b[58;2::1:2:3m");
        Assert.Equal((1 << 16) | (2 << 8) | 3, InputHandler.CurAttrData.GetUnderlineColor());
        Assert.Equal(Attributes.CmRgb, InputHandler.CurAttrData.GetUnderlineColorMode());
        Assert.True(InputHandler.CurAttrData.IsUnderlineColorRgb());
        Assert.False(InputHandler.CurAttrData.IsUnderlineColorPalette());
        Assert.False(InputHandler.CurAttrData.IsUnderlineColorDefault());
        InputHandler.Parse("a");
        InputHandler.Parse("\x1b[24m");
        BufferService.Buffer.Lines.Get(0)!.LoadCell(1, cell);
        AssertCellUnderlineColorP256(cell, 123);
        BufferService.Buffer.Lines.Get(0)!.LoadCell(3, cell);
        Assert.Equal((1 << 16) | (2 << 8) | 3, cell.GetUnderlineColor());
        Assert.Equal(Attributes.CmRgb, cell.GetUnderlineColorMode());
        Assert.True(cell.IsUnderlineColorRgb());
        Assert.False(cell.IsUnderlineColorPalette());
        Assert.False(cell.IsUnderlineColorDefault());

        // eAttrs in buffer pos 0 and 1 should be the same object
        CellData cell0 = new();
        CellData cell1 = new();
        CellData cell3 = new();
        BufferService.Buffer.Lines.Get(0)!.LoadCell(0, cell0);
        BufferService.Buffer.Lines.Get(0)!.LoadCell(1, cell1);
        BufferService.Buffer.Lines.Get(0)!.LoadCell(3, cell3);
        Assert.Same(cell0.Extended, cell1.Extended);

        // eAttrs in buffer pos 1 and pos 3 must be different objs
        Assert.NotSame(cell1.Extended, cell3.Extended);
    }
}
