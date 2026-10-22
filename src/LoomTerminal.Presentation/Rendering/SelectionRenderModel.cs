// Ported from xtermjs/xterm.js: src/browser/renderer/shared/SelectionRenderModel.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Presentation.Rendering;

/// <summary>How the renderer clips the selection to the viewport.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class SelectionRenderModel
{
    private int _viewportCappedStartRow;
    private int _viewportCappedEndRow;

    /// <summary>Creates an empty model.</summary>
    internal SelectionRenderModel() => Clear();

    /// <summary>Whether a selection is visible in the viewport.</summary>
    internal bool HasSelection { get; private set; }

    /// <summary>Whether the selection is a rectangle of columns.</summary>
    internal bool ColumnSelectMode { get; private set; }

    /// <summary>Row of the start, relative to the viewport (may be negative).</summary>
    internal int ViewportStartRow { get; private set; }

    /// <summary>Row of the end, relative to the viewport (may be beyond the last row).</summary>
    internal int ViewportEndRow { get; private set; }

    /// <summary>Column of the start.</summary>
    internal int StartCol { get; private set; }

    /// <summary>Column of the end (exclusive).</summary>
    internal int EndCol { get; private set; }

    /// <summary>Forgets the selection.</summary>
    internal void Clear()
    {
        HasSelection = false;
        ColumnSelectMode = false;
        ViewportStartRow = 0;
        ViewportEndRow = 0;
        _viewportCappedStartRow = 0;
        _viewportCappedEndRow = 0;
        StartCol = 0;
        EndCol = 0;
    }

    /// <summary>Clips the selection to the viewport.</summary>
    /// <param name="viewportY">Index of the first visible line of the buffer (ydisp).</param>
    /// <param name="rows">Number of visible rows.</param>
    /// <param name="start">Start of the selection, in buffer coordinates.</param>
    /// <param name="end">End of the selection, in buffer coordinates.</param>
    /// <param name="columnSelectMode">Whether the selection is a rectangle of columns.</param>
    internal void Update(int viewportY, int rows, (int X, int Y)? start, (int X, int Y)? end, bool columnSelectMode = false)
    {
        // Selection does not exist
        if (start is not { } s || end is not { } e || (s.X == e.X && s.Y == e.Y))
        {
            Clear();
            return;
        }

        // Translate from buffer position to viewport position
        var viewportStartRow = s.Y - viewportY;
        var viewportEndRow = e.Y - viewportY;
        var viewportCappedStartRow = Math.Max(viewportStartRow, 0);
        var viewportCappedEndRow = Math.Min(viewportEndRow, rows - 1);

        // No need to draw the selection
        if (viewportCappedStartRow >= rows || viewportCappedEndRow < 0)
        {
            Clear();
            return;
        }

        HasSelection = true;
        ColumnSelectMode = columnSelectMode;
        ViewportStartRow = viewportStartRow;
        ViewportEndRow = viewportEndRow;
        _viewportCappedStartRow = viewportCappedStartRow;
        _viewportCappedEndRow = viewportCappedEndRow;
        StartCol = s.X;
        EndCol = e.X;
    }

    /// <summary>Whether a cell is selected.</summary>
    /// <param name="x">Column.</param>
    /// <param name="y">Row, relative to the viewport.</param>
    /// <returns><see langword="true"/> when selected.</returns>
    internal bool IsCellSelected(int x, int y)
    {
        if (!HasSelection)
        {
            return false;
        }

        if (ColumnSelectMode)
        {
            if (StartCol <= EndCol)
            {
                return x >= StartCol && y >= _viewportCappedStartRow && x < EndCol && y <= _viewportCappedEndRow;
            }

            return x < StartCol && y >= _viewportCappedStartRow && x >= EndCol && y <= _viewportCappedEndRow;
        }

        return (y > ViewportStartRow && y < ViewportEndRow)
            || (ViewportStartRow == ViewportEndRow && y == ViewportStartRow && x >= StartCol && x < EndCol)
            || (ViewportStartRow < ViewportEndRow && y == ViewportEndRow && x < EndCol)
            || (ViewportStartRow < ViewportEndRow && y == ViewportStartRow && x >= StartCol);
    }
}
