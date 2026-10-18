// Ported from xtermjs/xterm.js: src/common/buffer/BufferLine.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Input;

namespace LoomTerminal.Core.Buffer;

// Line editing, resize, copy and strings. The JS typed array view over a larger ArrayBuffer is modeled as: the view
// is the first Length * CellSize items of _data, and the buffer is all of _data.
internal partial class BufferLine
{
    /// <inheritdoc />
    public void InsertCells(int pos, int n, CellData fillCellData)
    {
        pos %= Length;

        // handle fullwidth at pos: reset cell one to the left if pos is second cell of a wide char
        if (pos != 0 && GetWidth(pos - 1) == 2)
        {
            SetCellFromCodepoint(pos - 1, 0, 1, fillCellData);
        }

        if (n < Length - pos)
        {
            CellData cell = new();
            for (int i = Length - pos - n - 1; i >= 0; --i)
            {
                SetCell(pos + n + i, LoadCell(pos + i, cell));
            }

            for (int i = 0; i < n; ++i)
            {
                SetCell(pos + i, fillCellData);
            }
        }
        else
        {
            for (int i = pos; i < Length; ++i)
            {
                SetCell(i, fillCellData);
            }
        }

        // handle fullwidth at line end: reset last cell if it is first cell of a wide char
        if (GetWidth(Length - 1) == 2)
        {
            SetCellFromCodepoint(Length - 1, 0, 1, fillCellData);
        }
    }

    /// <inheritdoc />
    public void DeleteCells(int pos, int n, CellData fillCellData)
    {
        pos %= Length;
        if (n < Length - pos)
        {
            CellData cell = new();
            for (int i = 0; i < Length - pos - n; ++i)
            {
                SetCell(pos + i, LoadCell(pos + n + i, cell));
            }

            for (int i = Length - n; i < Length; ++i)
            {
                SetCell(i, fillCellData);
            }
        }
        else
        {
            for (int i = pos; i < Length; ++i)
            {
                SetCell(i, fillCellData);
            }
        }

        // handle fullwidth at pos:
        // - reset pos-1 if wide char
        // - reset pos if width==0 (previous second cell of a wide char)
        if (pos != 0 && GetWidth(pos - 1) == 2)
        {
            SetCellFromCodepoint(pos - 1, 0, 1, fillCellData);
        }

        if (GetWidth(pos) == 0 && HasContent(pos) == 0)
        {
            SetCellFromCodepoint(pos, 0, 1, fillCellData);
        }
    }

    /// <inheritdoc />
    public void ReplaceCells(int start, int end, CellData fillCellData, bool respectProtect = false)
    {
        if (respectProtect)
        {
            if (start != 0 && GetWidth(start - 1) == 2 && IsProtected(start - 1) == 0)
            {
                SetCellFromCodepoint(start - 1, 0, 1, fillCellData);
            }

            if (end < Length && GetWidth(end - 1) == 2 && IsProtected(end) == 0)
            {
                SetCellFromCodepoint(end, 0, 1, fillCellData);
            }

            while (start < end && start < Length)
            {
                if (IsProtected(start) == 0)
                {
                    SetCell(start, fillCellData);
                }

                start++;
            }

            return;
        }

        // handle fullwidth at start: reset cell one to the left if start is second cell of a wide char
        if (start != 0 && GetWidth(start - 1) == 2)
        {
            SetCellFromCodepoint(start - 1, 0, 1, fillCellData);
        }

        // handle fullwidth at last cell + 1: reset to empty cell if it is second part of a wide char
        if (end < Length && GetWidth(end - 1) == 2)
        {
            SetCellFromCodepoint(end, 0, 1, fillCellData);
        }

        while (start < end && start < Length)
        {
            SetCell(start++, fillCellData);
        }
    }

    /// <summary>
    /// Resizes the line to <paramref name="cols"/>, filling excess cells with <paramref name="fillCellData"/>. The
    /// underlying array does not change if there is still enough space to hold the new data. Returns whether a
    /// <see cref="CleanupMemory"/> call would free excess memory (true after shrinking more than the threshold).
    /// </summary>
    public bool Resize(int cols, CellData fillCellData)
    {
        if (cols == Length)
        {
            return Length * CellSize * CleanupThreshold < _data.Length;
        }

        int items = cols * CellSize;
        if (cols > Length)
        {
            if (_data.Length < items)
            {
                // slow path: new alloc and full data copy of the view
                uint[] data = new uint[items];
                Array.Copy(_data, data, Length * CellSize);
                _data = data;
            }

            // else optimization: avoid alloc and data copy if the array has enough room
            for (int i = Length; i < cols; ++i)
            {
                SetCell(i, fillCellData);
            }
        }
        else
        {
            // Remove any cut off combined data and extended attributes
            RemoveKeysFrom(_combined, cols);
            RemoveKeysFrom(_extendedAttrs, cols);
        }

        Length = cols;
        return items * CleanupThreshold < _data.Length;
    }

    /// <summary>
    /// Cleans up the underlying array. A cleanup is triggered if the array exceeds the actual used memory by a factor
    /// of the threshold. Returns 0 or 1 indicating whether a cleanup happened.
    /// </summary>
    public int CleanupMemory()
    {
        int viewLength = Length * CellSize;
        if (viewLength * CleanupThreshold < _data.Length)
        {
            uint[] data = new uint[viewLength];
            Array.Copy(_data, data, viewLength);
            _data = data;
            return 1;
        }

        return 0;
    }

    /// <summary>Fills the line with <paramref name="fillCellData"/>.</summary>
    public void Fill(CellData fillCellData, bool respectProtect = false)
    {
        if (respectProtect)
        {
            for (int i = 0; i < Length; ++i)
            {
                if (IsProtected(i) == 0)
                {
                    SetCell(i, fillCellData);
                }
            }

            return;
        }

        _combined = [];
        _extendedAttrs = [];
        for (int i = 0; i < Length; ++i)
        {
            SetCell(i, fillCellData);
        }
    }

    /// <summary>Alters this line to a full copy of <paramref name="line"/>.</summary>
    public void CopyFrom(IBufferLine line)
    {
        BufferLine source = (BufferLine)line;
        int viewLength = source.Length * CellSize;
        if (Length != source.Length)
        {
            _data = new uint[viewLength];
        }

        Array.Copy(source._data, _data, viewLength);
        Length = source.Length;
        _combined = new Dictionary<int, string>(source._combined);
        _extendedAttrs = new Dictionary<int, ExtendedAttrs>(source._extendedAttrs);
        IsWrapped = source.IsWrapped;
    }

    /// <summary>Creates a new clone.</summary>
    public IBufferLine Clone()
    {
        BufferLine newLine = new(0);
        newLine.CopyFrom(this);
        return newLine;
    }

    /// <summary>The length of the line without trailing empty cells.</summary>
    public int GetTrimmedLength()
    {
        for (int i = Length - 1; i >= 0; --i)
        {
            uint content = _data[(i * CellSize) + CellContent];
            if ((content & Content.HasContentMask) != 0)
            {
                return i + (unchecked((int)content) >> Content.WidthShift);
            }
        }

        return 0;
    }

    /// <summary>The length of the line without trailing empty cells that have no background color.</summary>
    public int GetNoBgTrimmedLength()
    {
        for (int i = Length - 1; i >= 0; --i)
        {
            uint content = _data[(i * CellSize) + CellContent];
            if ((content & Content.HasContentMask) != 0 || (_data[(i * CellSize) + CellBg] & Attributes.CmMask) != 0)
            {
                return i + (unchecked((int)content) >> Content.WidthShift);
            }
        }

        return 0;
    }

    /// <summary>Copies <paramref name="length"/> cells of <paramref name="src"/> into this line.</summary>
    public void CopyCellsFrom(BufferLine src, int srcCol, int destCol, int length, bool applyInReverse)
    {
        uint[] srcData = src._data;
        if (applyInReverse)
        {
            for (int cell = length - 1; cell >= 0; cell--)
            {
                CopyCell(src, srcData, srcCol + cell, destCol + cell);
            }
        }
        else
        {
            for (int cell = 0; cell < length; cell++)
            {
                CopyCell(src, srcData, srcCol + cell, destCol + cell);
            }
        }

        // Move any combined data over as needed, FIXME: repeat for extended attrs
        foreach (KeyValuePair<int, string> pair in src._combined.ToArray())
        {
            if (pair.Key >= srcCol)
            {
                _combined[pair.Key - srcCol + destCol] = pair.Value;
            }
        }
    }

    /// <summary>
    /// Translates the line to a string. <paramref name="endCol"/> is exclusive, -1 means the line length. If
    /// <paramref name="outColumns"/> is given it is filled with column numbers such that <c>result[i]</c> is displayed
    /// at <c>outColumns[i]</c>; <c>outColumns[result.Length]</c> is where the next char would be displayed. When a
    /// single cell is translated to multiple UTF-16 code units, the entries have the same column number.
    /// </summary>
    public string TranslateToString(bool trimRight = false, int startCol = 0, int endCol = -1, List<int>? outColumns = null)
    {
        if (endCol == -1)
        {
            endCol = Length;
        }

        if (trimRight)
        {
            endCol = Math.Min(endCol, GetTrimmedLength());
        }

        outColumns?.Clear();
        System.Text.StringBuilder result = new();
        while (startCol < endCol)
        {
            uint content = _data[(startCol * CellSize) + CellContent];
            uint cp = content & Content.CodepointMask;
            string chars = (content & Content.IsCombinedMask) != 0
                ? _combined[startCol]
                : cp != 0 ? TextDecoder.StringFromCodePoint(cp) : BufferConstants.WhitespaceCellChar;
            result.Append(chars);
            if (outColumns is not null)
            {
                for (int i = 0; i < chars.Length; ++i)
                {
                    outColumns.Add(startCol);
                }
            }

            int width = unchecked((int)content) >> Content.WidthShift;
            startCol += width != 0 ? width : 1; // always advance by at least 1
        }

        outColumns?.Add(startCol);
        return result.ToString();
    }

    private void CopyCell(BufferLine src, uint[] srcData, int srcCell, int destCell)
    {
        for (int i = 0; i < CellSize; i++)
        {
            _data[(destCell * CellSize) + i] = srcData[(srcCell * CellSize) + i];
        }

        if ((srcData[(srcCell * CellSize) + CellBg] & BgFlags.HasExtended) != 0)
        {
            _extendedAttrs[destCell] = src._extendedAttrs[srcCell];
        }
    }

    private static void RemoveKeysFrom<T>(Dictionary<int, T> map, int cols)
    {
        foreach (int key in map.Keys.ToArray())
        {
            if (key >= cols)
            {
                map.Remove(key);
            }
        }
    }
}
