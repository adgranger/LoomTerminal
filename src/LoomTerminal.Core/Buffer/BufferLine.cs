// Ported from xtermjs/xterm.js: src/common/buffer/BufferLine.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Input;

namespace LoomTerminal.Core.Buffer;

// buffer memory layout:
//
//   |             uint32_t             |        uint32_t         |        uint32_t         |
//   |             `content`            |          `FG`           |          `BG`           |
//   | wcwidth(2) comb(1) codepoint(21) | flags(8) R(8) G(8) B(8) | flags(8) R(8) G(8) B(8) |

/// <summary>
/// Typed array based buffer line.
/// <para>
/// There are 2 ways to insert data into the cell buffer: <see cref="SetCellFromCodepoint"/> +
/// <see cref="AddCodepointToCell"/> for data that is already UTF32 (used by the input handler), and
/// <see cref="SetCell"/>, which takes a <see cref="CellData"/> (see <see cref="CellData.FromCharData"/>).
/// </para>
/// <para>
/// To retrieve data use either the primitive getters (if only one value is needed) or <see cref="LoadCell"/>, reusing
/// the <see cref="CellData"/> in a loop.
/// </para>
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal partial class BufferLine : IBufferLine
{
    /// <summary>Typed array slots taken by one cell.</summary>
    private const int CellSize = 3;

    // Cell member indices: content = data[column * CellSize + CellContent], same for Fg and Bg.
    private const int CellContent = 0;
    private const int CellFg = 1; // currently simply holds all known attrs
    private const int CellBg = 2; // currently unused

    /// <summary>Factor when to cleanup the underlying array after shrinking.</summary>
    private const int CleanupThreshold = 2;

    /// <summary>The packed cells.</summary>
    protected uint[] _data;

    /// <summary>The strings of the combined cells, by column.</summary>
    protected Dictionary<int, string> _combined = [];

    /// <summary>The extended attributes of the cells, by column.</summary>
    protected Dictionary<int, ExtendedAttrs> _extendedAttrs = [];

    /// <summary>Creates a line of <paramref name="cols"/> cells filled with <paramref name="fillCellData"/>.</summary>
    public BufferLine(int cols, CellData? fillCellData = null, bool isWrapped = false)
    {
        IsWrapped = isWrapped;
        _data = new uint[cols * CellSize];
        CellData cell = fillCellData ?? CellData.FromCharData(
            new CharData(0, BufferConstants.NullCellChar, BufferConstants.NullCellWidth, BufferConstants.NullCellCode));
        for (int i = 0; i < cols; ++i)
        {
            SetCell(i, cell);
        }

        Length = cols;
    }

    /// <summary>
    /// The size of the underlying array in cells (3 items each), which can exceed <see cref="Length"/> after a resize
    /// until <see cref="CleanupMemory"/> runs (JS: the byte length of the buffer of the typed array).
    /// </summary>
    public int Capacity => _data.Length / CellSize;

    /// <inheritdoc />
    public int Length { get; set; }

    /// <inheritdoc />
    public bool IsWrapped { get; set; }

    /// <summary>Gets the cell at <paramref name="index"/> as <see cref="CharData"/> (legacy accessor).</summary>
    public CharData Get(int index)
    {
        uint content = _data[(index * CellSize) + CellContent];
        uint cp = content & Content.CodepointMask;
        bool combined = (content & Content.IsCombinedMask) != 0;
        return new CharData(
            _data[(index * CellSize) + CellFg],
            combined ? _combined[index] : (cp != 0 ? TextDecoder.StringFromCodePoint(cp) : string.Empty),
            unchecked((int)content) >> Content.WidthShift,
            combined ? _combined[index][^1] : cp);
    }

    /// <summary>Sets the cell at <paramref name="index"/> from <see cref="CharData"/> (legacy accessor).</summary>
    public void Set(int index, CharData value)
    {
        _data[(index * CellSize) + CellFg] = value.Attr;
        if (value.Char.Length > 1)
        {
            _combined[index] = value.Char;
            _data[(index * CellSize) + CellContent] =
                (uint)index | Content.IsCombinedMask | ((uint)value.Width << Content.WidthShift);
        }
        else
        {
            // JS: ''.charCodeAt(0) is NaN, which is 0 in a bit operation
            uint code = value.Char.Length == 1 ? value.Char[0] : 0u;
            _data[(index * CellSize) + CellContent] = code | ((uint)value.Width << Content.WidthShift);
        }
    }

    // primitive getters: use these when only one value is needed, otherwise use LoadCell

    /// <inheritdoc />
    public int GetWidth(int index) => unchecked((int)_data[(index * CellSize) + CellContent]) >> Content.WidthShift;

    /// <summary>Tests whether the content has width (non-zero means true).</summary>
    public uint HasWidth(int index) => _data[(index * CellSize) + CellContent] & Content.WidthMask;

    /// <summary>Gets the FG cell component.</summary>
    public uint GetFg(int index) => _data[(index * CellSize) + CellFg];

    /// <summary>Gets the BG cell component.</summary>
    public uint GetBg(int index) => _data[(index * CellSize) + CellBg];

    /// <summary>
    /// Tests whether the cell contains any chars (non-zero means true). An empty cell has no content, but other cells
    /// might differ in FG/BG from real empty cells.
    /// </summary>
    public uint HasContent(int index) => _data[(index * CellSize) + CellContent] & Content.HasContentMask;

    /// <summary>
    /// Gets the codepoint of the cell. To be in line with <c>Code</c> in <see cref="CharData"/> this either returns a
    /// single UTF32 codepoint or the last char of a combined string.
    /// </summary>
    public uint GetCodePoint(int index)
    {
        uint content = _data[(index * CellSize) + CellContent];
        if ((content & Content.IsCombinedMask) != 0)
        {
            return _combined[index][^1];
        }

        return content & Content.CodepointMask;
    }

    /// <summary>Tests whether the cell contains a combined string (non-zero means true).</summary>
    public uint IsCombined(int index) => _data[(index * CellSize) + CellContent] & Content.IsCombinedMask;

    /// <summary>Returns the string content of the cell (empty for empty cells).</summary>
    public string GetString(int index)
    {
        uint content = _data[(index * CellSize) + CellContent];
        if ((content & Content.IsCombinedMask) != 0)
        {
            return _combined[index];
        }

        if ((content & Content.CodepointMask) != 0)
        {
            return TextDecoder.StringFromCodePoint(content & Content.CodepointMask);
        }

        // return empty string for empty cells
        return string.Empty;
    }

    /// <summary>Gets the state of the protected flag (non-zero means true).</summary>
    public uint IsProtected(int index) => _data[(index * CellSize) + CellBg] & BgFlags.Protected;

    /// <summary>Loads the data at <paramref name="index"/> into <paramref name="cell"/>, reusing the object.</summary>
    public CellData LoadCell(int index, CellData cell)
    {
        int startIndex = index * CellSize;
        cell.Content = _data[startIndex + CellContent];
        cell.Fg = _data[startIndex + CellFg];
        cell.Bg = _data[startIndex + CellBg];
        if ((cell.Content & Content.IsCombinedMask) != 0)
        {
            cell.CombinedData = _combined[index];
        }

        if ((cell.Bg & BgFlags.HasExtended) != 0)
        {
            cell.Extended = _extendedAttrs[index];
        }

        return cell;
    }

    /// <summary>Sets the data at <paramref name="index"/> to <paramref name="cell"/>.</summary>
    public void SetCell(int index, CellData cell)
    {
        if ((cell.Content & Content.IsCombinedMask) != 0)
        {
            _combined[index] = cell.CombinedData;
        }

        if ((cell.Bg & BgFlags.HasExtended) != 0)
        {
            _extendedAttrs[index] = cell.Extended;
        }

        _data[(index * CellSize) + CellContent] = cell.Content;
        _data[(index * CellSize) + CellFg] = cell.Fg;
        _data[(index * CellSize) + CellBg] = cell.Bg;
    }

    /// <summary>
    /// Sets the cell data from the input handler. Since the input handler sees the incoming chars as UTF32 codepoints,
    /// it gets an optimized access method.
    /// </summary>
    public void SetCellFromCodepoint(int index, uint codePoint, int width, AttributeData attrs)
    {
        if ((attrs.Bg & BgFlags.HasExtended) != 0)
        {
            _extendedAttrs[index] = attrs.Extended;
        }

        _data[(index * CellSize) + CellContent] = codePoint | ((uint)width << Content.WidthShift);
        _data[(index * CellSize) + CellFg] = attrs.Fg;
        _data[(index * CellSize) + CellBg] = attrs.Bg;
    }

    /// <summary>
    /// Adds a codepoint to a cell from the input handler. During input, combining chars with a width of 0 follow and
    /// stack onto a leading char. Since the attrs were set by the previous <see cref="SetCellFromCodepoint"/> call,
    /// they are omitted here.
    /// </summary>
    public void AddCodepointToCell(int index, uint codePoint, int width)
    {
        uint content = _data[(index * CellSize) + CellContent];
        if ((content & Content.IsCombinedMask) != 0)
        {
            // we already have a combined string, simply add
            _combined[index] += TextDecoder.StringFromCodePoint(codePoint);
        }
        else
        {
            if ((content & Content.CodepointMask) != 0)
            {
                // normal case for combining chars:
                //  - move current leading char + new one into combined string
                //  - set combined flag
                _combined[index] = TextDecoder.StringFromCodePoint(content & Content.CodepointMask)
                    + TextDecoder.StringFromCodePoint(codePoint);
                content &= ~Content.CodepointMask; // set codepoint in buffer to 0
                content |= Content.IsCombinedMask;
            }
            else
            {
                // should not happen - we actually have no data in the cell yet
                // simply set the data in the cell buffer with a width of 1
                content = codePoint | (1u << Content.WidthShift);
            }
        }

        if (width != 0)
        {
            content &= ~Content.WidthMask;
            content |= (uint)width << Content.WidthShift;
        }

        _data[(index * CellSize) + CellContent] = content;
    }
}
