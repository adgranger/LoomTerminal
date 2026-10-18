// Ported from xtermjs/xterm.js: src/common/buffer/BufferLine.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Buffer;

// Line editing, resize, copy and strings: implemented by P1-19.
internal partial class BufferLine
{
    /// <inheritdoc />
    public void InsertCells(int pos, int n, CellData fillCellData) => throw new NotImplementedException();

    /// <inheritdoc />
    public void DeleteCells(int pos, int n, CellData fillCellData) => throw new NotImplementedException();

    /// <inheritdoc />
    public void ReplaceCells(int start, int end, CellData fillCellData, bool respectProtect = false) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public bool Resize(int cols, CellData fillCellData) => throw new NotImplementedException();

    /// <inheritdoc />
    public int CleanupMemory() => throw new NotImplementedException();

    /// <inheritdoc />
    public void Fill(CellData fillCellData, bool respectProtect = false) => throw new NotImplementedException();

    /// <inheritdoc />
    public void CopyFrom(IBufferLine line) => throw new NotImplementedException();

    /// <inheritdoc />
    public IBufferLine Clone() => throw new NotImplementedException();

    /// <inheritdoc />
    public int GetTrimmedLength() => throw new NotImplementedException();

    /// <inheritdoc />
    public int GetNoBgTrimmedLength() => throw new NotImplementedException();

    /// <inheritdoc />
    public string TranslateToString(bool trimRight = false, int startCol = 0, int? endCol = null, List<int>? outColumns = null) =>
        throw new NotImplementedException();
}
