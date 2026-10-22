// Ported from xtermjs/xterm.js: src/browser/renderer/shared/RendererUtils.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Presentation.Rendering;

/// <summary>Glyph classification and dotted underline helpers shared by the renderers.</summary>
/// <remarks>Threading: stateless; any thread.</remarks>
internal static class RendererUtils
{
    /// <summary>
    /// Whether the code point is a Powerline symbol: those need different padding and are excluded from the minimum
    /// contrast ratio.
    /// </summary>
    /// <param name="codepoint">The code point.</param>
    /// <returns>Whether it is a Powerline glyph.</returns>
    internal static bool IsPowerlineGlyph(int codepoint) => codepoint is >= 0xE0A4 and <= 0xE0D6;

    /// <summary>Whether the glyph is a block, box drawing or Powerline glyph that is drawn as a background color.</summary>
    /// <param name="codepoint">The code point.</param>
    /// <returns>Whether to treat the glyph as a background color.</returns>
    internal static bool TreatGlyphAsBackgroundColor(int codepoint) => IsPowerlineGlyph(codepoint) || IsBoxOrBlockGlyph(codepoint);

    /// <summary>The variant offset of the next dotted underline segment.</summary>
    /// <param name="cellWidth">Width of the cell in device pixels.</param>
    /// <param name="lineWidth">Width of the underline in device pixels.</param>
    /// <param name="currentOffset">The current offset.</param>
    /// <returns>The next offset.</returns>
    internal static int ComputeNextVariantOffset(int cellWidth, double lineWidth, int currentOffset = 0)
    {
        var period = (int)Math.Floor(lineWidth + 0.5) * 2;
        return (cellWidth - (period - currentOffset)) % period;
    }

    private static bool IsBoxOrBlockGlyph(int codepoint) => codepoint is >= 0x2500 and <= 0x259F;
}
