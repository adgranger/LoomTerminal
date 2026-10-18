// Ported from xtermjs/xterm.js: src/common/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Buffer;

/// <summary>A line in the terminal buffer.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface IBufferLine
{
    /// <summary>The number of columns of the line.</summary>
    int Length { get; set; }

    /// <summary>Whether the line is a continuation of the previous one.</summary>
    bool IsWrapped { get; set; }

    /// <summary>Gets the cell at <paramref name="index"/> as <see cref="CharData"/>.</summary>
    CharData Get(int index);

    /// <summary>Sets the cell at <paramref name="index"/> from <see cref="CharData"/>.</summary>
    void Set(int index, CharData value);

    /// <summary>Loads the cell at <paramref name="index"/> into <paramref name="cell"/>.</summary>
    CellData LoadCell(int index, CellData cell);

    /// <summary>Stores <paramref name="cell"/> at <paramref name="index"/>.</summary>
    void SetCell(int index, CellData cell);

    /// <summary>Stores a UTF32 code point with its attributes at <paramref name="index"/>.</summary>
    void SetCellFromCodepoint(int index, uint codePoint, int width, AttributeData attrs);

    /// <summary>Adds a combining code point to the cell at <paramref name="index"/>.</summary>
    void AddCodepointToCell(int index, uint codePoint, int width);

    /// <summary>Inserts <paramref name="n"/> cells at <paramref name="pos"/>.</summary>
    void InsertCells(int pos, int n, CellData ch);

    /// <summary>Deletes <paramref name="n"/> cells at <paramref name="pos"/>.</summary>
    void DeleteCells(int pos, int n, CellData fill);

    /// <summary>Replaces the cells in [<paramref name="start"/>, <paramref name="end"/>) with <paramref name="fill"/>.</summary>
    void ReplaceCells(int start, int end, CellData fill, bool respectProtect = false);

    /// <summary>Resizes the line; returns whether a <see cref="CleanupMemory"/> call would free memory.</summary>
    bool Resize(int cols, CellData fill);

    /// <summary>Frees excess memory; returns 1 if a cleanup happened, otherwise 0.</summary>
    int CleanupMemory();

    /// <summary>Fills the line with <paramref name="fillCellData"/>.</summary>
    void Fill(CellData fillCellData, bool respectProtect = false);

    /// <summary>Makes this line a full copy of <paramref name="line"/>.</summary>
    void CopyFrom(IBufferLine line);

    /// <summary>Creates a full copy of the line.</summary>
    IBufferLine Clone();

    /// <summary>The length of the line without trailing empty cells.</summary>
    int GetTrimmedLength();

    /// <summary>The length of the line without trailing empty cells that have no background color.</summary>
    int GetNoBgTrimmedLength();

    /// <summary>Translates the line to a string; an <paramref name="endCol"/> of -1 means the line length.</summary>
    string TranslateToString(bool trimRight = false, int startCol = 0, int endCol = -1, List<int>? outColumns = null);

    /// <summary>The width of the cell.</summary>
    int GetWidth(int index);

    /// <summary>Non-zero if the cell has a width.</summary>
    uint HasWidth(int index);

    /// <summary>The foreground word of the cell.</summary>
    uint GetFg(int index);

    /// <summary>The background word of the cell.</summary>
    uint GetBg(int index);

    /// <summary>Non-zero if the cell has content.</summary>
    uint HasContent(int index);

    /// <summary>The code point of the cell (the last char of a combined string).</summary>
    uint GetCodePoint(int index);

    /// <summary>Non-zero if the cell contains a combined string.</summary>
    uint IsCombined(int index);

    /// <summary>The string content of the cell.</summary>
    string GetString(int index);
}
