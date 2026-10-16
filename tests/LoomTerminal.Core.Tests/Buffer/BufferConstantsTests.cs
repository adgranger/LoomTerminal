using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Data;

namespace LoomTerminal.Core.Tests.Buffer;

// New tests: the expected values are read from src/common/buffer/Constants.ts and data/EscapeSequences.ts.
public class BufferConstantsTests
{
    [Fact]
    public void Attributes_have_the_xterm_js_values()
    {
        Assert.Equal(0x3000000u, Attributes.CmRgb);
        Assert.Equal(0x3000000u, Attributes.CmMask);
        Assert.Equal(0xFFFFFFu, Attributes.RgbMask);
        Assert.Equal(16, Attributes.RedShift);
    }

    [Fact]
    public void Flags_have_the_xterm_js_values()
    {
        Assert.Equal(0x4000000u, FgFlags.Inverse);
        Assert.Equal(0x80000000u, FgFlags.Strikethrough);
        Assert.Equal(0x4000000u, BgFlags.Italic);
        Assert.Equal(0x10000000u, BgFlags.HasExtended);
        Assert.Equal(0x1C000000u, ExtFlags.UnderlineStyle);
        Assert.Equal(0xE0000000u, ExtFlags.VariantOffset);
    }

    [Fact]
    public void Content_masks_have_the_xterm_js_values()
    {
        Assert.Equal(0x1FFFFFu, Content.CodepointMask);
        Assert.Equal(0x3FFFFFu, Content.HasContentMask);
        Assert.Equal(0xC00000u, Content.WidthMask);
        Assert.Equal(22, Content.WidthShift);
    }

    [Fact]
    public void Defaults_and_special_cells_have_the_xterm_js_values()
    {
        Assert.Equal(256u | (0u << 9), BufferConstants.DefaultAttr);
        Assert.Equal(0u, BufferConstants.DefaultExt);
        Assert.Equal(BufferConstants.NullCellChar, string.Empty);
        Assert.Equal(1, BufferConstants.NullCellWidth);
        Assert.Equal(0, BufferConstants.NullCellCode);
        Assert.Equal(" ", BufferConstants.WhitespaceCellChar);
        Assert.Equal(32, BufferConstants.WhitespaceCellCode);
    }

    [Fact]
    public void UnderlineStyle_has_the_xterm_js_values()
    {
        Assert.Equal(0, (int)UnderlineStyle.None);
        Assert.Equal(3, (int)UnderlineStyle.Curly);
        Assert.Equal(5, (int)UnderlineStyle.Dashed);
    }

    [Fact]
    public void CharData_holds_the_cell_tuple()
    {
        CharData cell = new(BufferConstants.DefaultAttr, "a", 1, 97);
        Assert.Equal(256u, cell.Attr);
        Assert.Equal("a", cell.Char);
        Assert.Equal(1, cell.Width);
        Assert.Equal(97u, cell.Code);
    }

    [Fact]
    public void Control_codes_have_the_xterm_js_values()
    {
        Assert.Equal("\u001b", C0.Esc);
        Assert.Equal("\u0000", C0.Nul);
        Assert.Equal("\u007f", C0.Del);
        Assert.Equal("\u009b", C1.Csi);
        Assert.Equal("\u009c", C1.St);
        Assert.Equal("\u001b\\", C1Escaped.St);
        Assert.Equal(0x1b, C0Code.Esc);
        Assert.Equal(0x9b, C1Code.Csi);
    }

    [Fact]
    public void Every_control_code_string_matches_its_code()
    {
        Assert.Equal(C0.Bel, ((char)C0Code.Bel).ToString());
        Assert.Equal(C0.Sp, ((char)C0Code.Sp).ToString());
        Assert.Equal(C1.Osc, ((char)C1Code.Osc).ToString());
        Assert.Equal(C1.Apc, ((char)C1Code.Apc).ToString());
    }
}
