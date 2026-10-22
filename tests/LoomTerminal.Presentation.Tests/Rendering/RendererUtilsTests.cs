// Ported from xtermjs/xterm.js: src/browser/renderer/shared/RendererUtils.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Presentation.Tests.Rendering;

public class RendererUtilsTests
{
    private static void AssertVariantOffsets(int line, int[] expected)
    {
        const int CellWidth = 11;
        const int DoubleCellWidth = 22;
        int[] cells = [CellWidth, CellWidth, DoubleCellWidth, DoubleCellWidth];
        var variantOffset = 0;
        for (var index = 0; index < cells.Length; index++)
        {
            variantOffset = RendererUtils.ComputeNextVariantOffset(cells[index], line, variantOffset);
            Assert.Equal(expected[index], variantOffset);
        }
    }

    // computeNextVariantOffset: should line 1 (=,_,=_,=_,)
    [Fact]
    public void Variant_offset_for_a_line_width_of_1() => AssertVariantOffsets(1, [1, 0, 0, 0]);

    // computeNextVariantOffset: should line 2
    [Fact]
    public void Variant_offset_for_a_line_width_of_2() => AssertVariantOffsets(2, [3, 2, 0, 2]);

    // computeNextVariantOffset: should line 3
    [Fact]
    public void Variant_offset_for_a_line_width_of_3() => AssertVariantOffsets(3, [5, 4, 2, 0]);

    [Fact]
    public void Powerline_box_and_block_glyphs_are_treated_as_background_color()
    {
        Assert.True(RendererUtils.TreatGlyphAsBackgroundColor(0xE0B0));
        Assert.True(RendererUtils.TreatGlyphAsBackgroundColor(0x2588));
        Assert.False(RendererUtils.TreatGlyphAsBackgroundColor('a'));
    }
}
