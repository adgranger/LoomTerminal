// Ported from xtermjs/xterm.js: src/common/buffer/Constants.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Buffer;

/// <summary>Defaults and null/whitespace cells of the buffer.</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class BufferConstants
{
    /// <summary>The default color.</summary>
    public const uint DefaultColor = 0;

    /// <summary>The default attributes: <c>(0 &lt;&lt; 18) | (DefaultColor &lt;&lt; 9) | (256 &lt;&lt; 0)</c>.</summary>
    public const uint DefaultAttr = (0u << 18) | (DefaultColor << 9) | (256u << 0);

    /// <summary>The default extended attributes.</summary>
    public const uint DefaultExt = 0;

    /// <summary>Index of the attributes in a <see cref="CharData"/>.</summary>
    public const int CharDataAttrIndex = 0;

    /// <summary>Index of the character in a <see cref="CharData"/>.</summary>
    public const int CharDataCharIndex = 1;

    /// <summary>Index of the width in a <see cref="CharData"/>.</summary>
    public const int CharDataWidthIndex = 2;

    /// <summary>Index of the code in a <see cref="CharData"/>.</summary>
    public const int CharDataCodeIndex = 3;

    // Null cell - a real empty cell (containing nothing).
    // Note that code should always be 0 for a null cell as several test conditions of the buffer line rely on this.

    /// <summary>The character of a null cell.</summary>
    public const string NullCellChar = "";

    /// <summary>The width of a null cell.</summary>
    public const int NullCellWidth = 1;

    /// <summary>The code of a null cell.</summary>
    public const int NullCellCode = 0;

    // Whitespace cell. A replacement for empty cells when needed during rendering lines to preserve alignment.

    /// <summary>The character of a whitespace cell.</summary>
    public const string WhitespaceCellChar = " ";

    /// <summary>The width of a whitespace cell.</summary>
    public const int WhitespaceCellWidth = 1;

    /// <summary>The code of a whitespace cell.</summary>
    public const int WhitespaceCellCode = 32;
}

/// <summary>Bitmasks for accessing data in <c>content</c>.</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class Content
{
    /// <summary>
    /// Bit 1..21: codepoint, max allowed in UTF32 is 0x10FFFF (21 bits taken).
    /// Read: <c>codepoint = content &amp; CodepointMask</c>; write: <c>content |= codepoint &amp; CodepointMask</c>.
    /// </summary>
    public const uint CodepointMask = 0x1FFFFF;

    /// <summary>Bit 22: flag indicating whether a cell contains combined content.</summary>
    public const uint IsCombinedMask = 0x200000; // 1 << 21

    /// <summary>Bit 1..22: mask to check whether a cell contains any string data (codepoint and combined bits).</summary>
    public const uint HasContentMask = 0x3FFFFF;

    /// <summary>
    /// Bit 23..24: wcwidth value of the cell, takes 2 bits (ranges from 0..2).
    /// Read: <c>width = (content &amp; WidthMask) &gt;&gt; WidthShift</c>.
    /// </summary>
    public const uint WidthMask = 0xC00000; // 3 << 22

    /// <summary>The shift of the width in <c>content</c>.</summary>
    public const int WidthShift = 22;
}

/// <summary>Bitmasks of the colors in the <c>fg</c> and <c>bg</c> attribute words.</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class Attributes
{
    /// <summary>Bit 1..8: blue in RGB, color in P256 and P16.</summary>
    public const uint BlueMask = 0xFF;

    /// <summary>The shift of blue.</summary>
    public const int BlueShift = 0;

    /// <summary>Bit 1..8: color in P256 and P16.</summary>
    public const uint PcolorMask = 0xFF;

    /// <summary>The shift of the palette color.</summary>
    public const int PcolorShift = 0;

    /// <summary>Bit 9..16: green in RGB.</summary>
    public const uint GreenMask = 0xFF00;

    /// <summary>The shift of green.</summary>
    public const int GreenShift = 8;

    /// <summary>Bit 17..24: red in RGB.</summary>
    public const uint RedMask = 0xFF0000;

    /// <summary>The shift of red.</summary>
    public const int RedShift = 16;

    /// <summary>Bit 25..26: color mode mask, DEFAULT (0) | P16 (1) | P256 (2) | RGB (3).</summary>
    public const uint CmMask = 0x3000000;

    /// <summary>Color mode: default.</summary>
    public const uint CmDefault = 0;

    /// <summary>Color mode: 16 colors palette.</summary>
    public const uint CmP16 = 0x1000000;

    /// <summary>Color mode: 256 colors palette.</summary>
    public const uint CmP256 = 0x2000000;

    /// <summary>Color mode: RGB.</summary>
    public const uint CmRgb = 0x3000000;

    /// <summary>Bit 1..24: RGB room.</summary>
    public const uint RgbMask = 0xFFFFFF;
}

/// <summary>Flags in the <c>fg</c> attribute word (bit 27..32).</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class FgFlags
{
    /// <summary>Inverse.</summary>
    public const uint Inverse = 0x4000000;

    /// <summary>Bold.</summary>
    public const uint Bold = 0x8000000;

    /// <summary>Underline.</summary>
    public const uint Underline = 0x10000000;

    /// <summary>Blink.</summary>
    public const uint Blink = 0x20000000;

    /// <summary>Invisible.</summary>
    public const uint Invisible = 0x40000000;

    /// <summary>Strikethrough.</summary>
    public const uint Strikethrough = 0x80000000;
}

/// <summary>Flags in the <c>bg</c> attribute word (bit 27..32, the upper 2 are unused).</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class BgFlags
{
    /// <summary>Italic.</summary>
    public const uint Italic = 0x4000000;

    /// <summary>Dim.</summary>
    public const uint Dim = 0x8000000;

    /// <summary>The cell has extended attributes.</summary>
    public const uint HasExtended = 0x10000000;

    /// <summary>Protected.</summary>
    public const uint Protected = 0x20000000;

    /// <summary>Overline.</summary>
    public const uint Overline = 0x40000000;
}

/// <summary>Flags in the extended attribute word.</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class ExtFlags
{
    /// <summary>Bit 27..29: the underline style.</summary>
    public const uint UnderlineStyle = 0x1C000000;

    /// <summary>
    /// Bit 30..32: an optional variant for the glyph, used for example to offset underlines by a number of pixels to
    /// create a perfect pattern.
    /// </summary>
    public const uint VariantOffset = 0xE0000000;
}

/// <summary>The style of an underline.</summary>
internal enum UnderlineStyle
{
    /// <summary>No underline.</summary>
    None = 0,

    /// <summary>A single line.</summary>
    Single = 1,

    /// <summary>A double line.</summary>
    Double = 2,

    /// <summary>A curly line.</summary>
    Curly = 3,

    /// <summary>A dotted line.</summary>
    Dotted = 4,

    /// <summary>A dashed line.</summary>
    Dashed = 5,
}
