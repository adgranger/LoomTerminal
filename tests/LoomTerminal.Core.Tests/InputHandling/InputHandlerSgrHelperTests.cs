using LoomTerminal.Core.Buffer;
using Handler = LoomTerminal.Core.InputHandling.InputHandler;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerSgrHelperTests : InputHandlerTestBase
{
    [Fact]
    public void ExtractColor_with_semicolons_sets_an_RGB_foreground()
    {
        AttributeData attr = new();
        int advance = Handler.ExtractColor(Params.FromArray([38, 2, 50, 100, 150]), 0, attr);
        Assert.Equal(4, advance);
        Assert.Equal(Attributes.CmRgb | 0x326496u, attr.Fg & (Attributes.CmMask | Attributes.RgbMask));
    }

    private static readonly int[] ColonPalette = [5, 50];

    [Fact]
    public void ExtractColor_with_colons_sets_a_palette_foreground()
    {
        AttributeData attr = new();
        int advance = Handler.ExtractColor(Params.FromArray([38, ColonPalette]), 0, attr);
        Assert.Equal(0, advance);
        Assert.Equal(Attributes.CmP256 | 50u, attr.Fg & (Attributes.CmMask | Attributes.PcolorMask));
    }

    [Fact]
    public void ExtractColor_with_semicolons_sets_a_palette_background()
    {
        AttributeData attr = new();
        int advance = Handler.ExtractColor(Params.FromArray([48, 5, 7]), 0, attr);
        Assert.Equal(2, advance);
        Assert.Equal(Attributes.CmP256 | 7u, attr.Bg & (Attributes.CmMask | Attributes.PcolorMask));
    }

    [Fact]
    public void ProcessUnderline_3_sets_a_curly_underline()
    {
        AttributeData attr = new();
        Handler.ProcessUnderline(3, attr);
        Assert.Equal(UnderlineStyle.Curly, attr.Extended.UnderlineStyle);
        Assert.NotEqual(0u, attr.Fg & FgFlags.Underline);
    }

    [Fact]
    public void ProcessUnderline_0_removes_the_underline()
    {
        AttributeData attr = new();
        Handler.ProcessUnderline(3, attr);
        Handler.ProcessUnderline(0, attr);
        Assert.Equal(UnderlineStyle.None, attr.Extended.UnderlineStyle);
        Assert.Equal(0u, attr.Fg & FgFlags.Underline);
    }

    [Fact]
    public void ProcessUnderline_with_an_invalid_style_falls_back_to_single()
    {
        AttributeData attr = new();
        Handler.ProcessUnderline(9, attr);
        Assert.Equal(UnderlineStyle.Single, attr.Extended.UnderlineStyle);
    }
}
