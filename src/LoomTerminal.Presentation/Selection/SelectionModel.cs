// Ported from xtermjs/xterm.js: src/browser/selection/SelectionModel.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;

namespace LoomTerminal.Presentation.Selection;

/// <summary>
/// Represents a selection within the buffer. This model only cares about column and row coordinates, not wide
/// characters.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class SelectionModel(IBufferService bufferService)
{
    /// <summary>Whether select all is currently active.</summary>
    internal bool IsSelectAllActive { get; set; }

    /// <summary>
    /// The minimal length of the selection from the start position. When double clicking on a word, the word will be
    /// selected which makes the selection start at the start of the word and makes this variable the length.
    /// </summary>
    internal int SelectionStartLength { get; set; }

    /// <summary>The position the selection starts at (<see langword="null"/> for none).</summary>
    internal (int X, int Y)? SelectionStart { get; set; }

    /// <summary>The position the selection ends at (<see langword="null"/> for none).</summary>
    internal (int X, int Y)? SelectionEnd { get; set; }

    /// <summary>The final selection start, taking into consideration select all.</summary>
    internal (int X, int Y)? FinalSelectionStart
    {
        get
        {
            if (IsSelectAllActive)
            {
                return (0, 0);
            }

            if (SelectionEnd is not { } end || SelectionStart is not { } start)
            {
                return SelectionStart;
            }

            return AreSelectionValuesReversed() ? end : start;
        }
    }

    /// <summary>
    /// The final selection end, taking into consideration select all, double click word selection and triple click
    /// line selection.
    /// </summary>
    internal (int X, int Y)? FinalSelectionEnd
    {
        get
        {
            var cols = bufferService.Cols;
            if (IsSelectAllActive)
            {
                return (cols, bufferService.Buffer.YBase + bufferService.Rows - 1);
            }

            if (SelectionStart is not { } start)
            {
                return null;
            }

            // Use the selection start + length if the end doesn't exist or they're reversed
            if (SelectionEnd is not { } end || AreSelectionValuesReversed())
            {
                var startPlusLength = start.X + SelectionStartLength;
                if (startPlusLength > cols)
                {
                    // Ensure the trailing EOL isn't included when the selection ends on the right edge
                    if (startPlusLength % cols == 0)
                    {
                        return (cols, start.Y + (startPlusLength / cols) - 1);
                    }

                    return (startPlusLength % cols, start.Y + (startPlusLength / cols));
                }

                return (startPlusLength, start.Y);
            }

            // Ensure the the word/line is selected after a double/triple click
            if (SelectionStartLength != 0 && end.Y == start.Y)
            {
                // Select the larger of the two when start and end are on the same line.
                // Keep the whole wrapped word/line selected if the content wraps multiple lines
                var startPlusLength = start.X + SelectionStartLength;
                if (startPlusLength > cols)
                {
                    return (startPlusLength % cols, start.Y + (startPlusLength / cols));
                }

                return (Math.Max(startPlusLength, end.X), end.Y);
            }

            return end;
        }
    }

    /// <summary>Clears the current selection.</summary>
    internal void ClearSelection()
    {
        SelectionStart = null;
        SelectionEnd = null;
        IsSelectAllActive = false;
        SelectionStartLength = 0;
    }

    /// <summary>Returns whether the selection start and end are reversed.</summary>
    /// <returns><see langword="true"/> when the end is before the start.</returns>
    internal bool AreSelectionValuesReversed()
    {
        if (SelectionStart is not { } start || SelectionEnd is not { } end)
        {
            return false;
        }

        return start.Y > end.Y || (start.Y == end.Y && start.X > end.X);
    }

    /// <summary>Handle the buffer being trimmed, adjust the selection position.</summary>
    /// <param name="amount">The amount the buffer is being trimmed.</param>
    /// <returns>Whether a refresh is necessary.</returns>
    internal bool HandleTrim(int amount)
    {
        // Adjust the selection position based on the trimmed amount.
        if (SelectionStart is { } start)
        {
            SelectionStart = (start.X, start.Y - amount);
        }

        if (SelectionEnd is { } end)
        {
            SelectionEnd = (end.X, end.Y - amount);
        }

        // The selection has moved off the buffer, clear it.
        if (SelectionEnd is { Y: < 0 })
        {
            ClearSelection();
            return true;
        }

        // If the selection start is trimmed, ensure the start column is 0.
        if (SelectionStart is { Y: < 0 } trimmed)
        {
            SelectionStart = (trimmed.X, 0);
        }

        return false;
    }
}
