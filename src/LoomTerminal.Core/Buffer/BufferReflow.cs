// Ported from xtermjs/xterm.js: src/common/buffer/BufferReflow.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Buffer;

/// <summary>The new layout of the lines after a reflow larger.</summary>
/// <param name="Layout">The indexes of the lines that are kept.</param>
/// <param name="CountRemoved">The number of removed lines.</param>
internal readonly record struct NewLayoutResult(int[] Layout, int CountRemoved);

/// <summary>The pure functions that compute new line layouts when the terminal width changes.</summary>
/// <remarks>Threading: stateless; used from the parser thread only.</remarks>
internal static class BufferReflow
{
    /// <summary>
    /// Evaluates and returns the indexes to be removed after a reflow larger occurs, as pairs of
    /// <c>(index, count)</c>. Lines are removed when a wrapped line unwraps.
    /// </summary>
    /// <param name="lines">The buffer lines.</param>
    /// <param name="oldCols">The columns before resize.</param>
    /// <param name="newCols">The columns after resize.</param>
    /// <param name="bufferAbsoluteY">The absolute y position of the cursor (baseY + cursorY).</param>
    /// <param name="nullCell">The cell data to use when filling in empty cells.</param>
    /// <param name="reflowCursorLine">Whether to reflow the line containing the cursor.</param>
    public static int[] ReflowLargerGetLinesToRemove(
        CircularList<IBufferLine> lines,
        int oldCols,
        int newCols,
        int bufferAbsoluteY,
        CellData nullCell,
        bool reflowCursorLine)
    {
        // Gather all BufferLines that need to be removed from the Buffer here so that they can be batched up and only
        // committed once
        List<int> toRemove = [];

        for (int y = 0; y < lines.Length - 1; y++)
        {
            // Check if this row is wrapped
            int i = y;
            BufferLine nextLine = (BufferLine)lines.Get(++i)!;
            if (!nextLine.IsWrapped)
            {
                continue;
            }

            // Check how many lines it's wrapped for
            List<BufferLine> wrappedLines = [(BufferLine)lines.Get(y)!];
            while (i < lines.Length && nextLine.IsWrapped)
            {
                wrappedLines.Add(nextLine);
                nextLine = (BufferLine)lines.Get(++i)!;
            }

            if (!reflowCursorLine)
            {
                // If these lines contain the cursor don't touch them, the program will handle fixing up wrapped lines
                // with the cursor
                if (bufferAbsoluteY >= y && bufferAbsoluteY < i)
                {
                    y += wrappedLines.Count - 1;
                    continue;
                }
            }

            // Copy buffer data to new locations
            int destLineIndex = 0;
            int destCol = GetWrappedLineTrimmedLength(wrappedLines, destLineIndex, oldCols);
            int srcLineIndex = 1;
            int srcCol = 0;
            while (srcLineIndex < wrappedLines.Count)
            {
                int srcTrimmedTineLength = GetWrappedLineTrimmedLength(wrappedLines, srcLineIndex, oldCols);
                int srcRemainingCells = srcTrimmedTineLength - srcCol;
                int destRemainingCells = newCols - destCol;
                int cellsToCopy = Math.Min(srcRemainingCells, destRemainingCells);

                wrappedLines[destLineIndex].CopyCellsFrom(wrappedLines[srcLineIndex], srcCol, destCol, cellsToCopy, false);

                destCol += cellsToCopy;
                if (destCol == newCols)
                {
                    destLineIndex++;
                    destCol = 0;
                }

                srcCol += cellsToCopy;
                if (srcCol == srcTrimmedTineLength)
                {
                    srcLineIndex++;
                    srcCol = 0;
                }

                // Make sure the last cell isn't wide, if it is copy it to the current dest
                if (destCol == 0 && destLineIndex != 0)
                {
                    if (wrappedLines[destLineIndex - 1].GetWidth(newCols - 1) == 2)
                    {
                        wrappedLines[destLineIndex].CopyCellsFrom(wrappedLines[destLineIndex - 1], newCols - 1, destCol++, 1, false);

                        // Null out the end of the last row
                        wrappedLines[destLineIndex - 1].SetCell(newCols - 1, nullCell);
                    }
                }
            }

            // Clear out remaining cells or fragments could remain;
            wrappedLines[destLineIndex].ReplaceCells(destCol, newCols, nullCell);

            // Work backwards and remove any rows at the end that only contain null cells
            int countToRemove = 0;
            for (int j = wrappedLines.Count - 1; j > 0; j--)
            {
                if (j > destLineIndex || wrappedLines[j].GetTrimmedLength() == 0)
                {
                    countToRemove++;
                }
                else
                {
                    break;
                }
            }

            if (countToRemove > 0)
            {
                toRemove.Add(y + wrappedLines.Count - countToRemove); // index
                toRemove.Add(countToRemove);
            }

            y += wrappedLines.Count - 1;
        }

        return [.. toRemove];
    }

    /// <summary>Creates and returns the new layout for lines given the (index, count) pairs to be removed.</summary>
    /// <param name="lines">The buffer lines.</param>
    /// <param name="toRemove">The indexes to remove, as returned by <see cref="ReflowLargerGetLinesToRemove"/>.</param>
    public static NewLayoutResult ReflowLargerCreateNewLayout(CircularList<IBufferLine> lines, int[] toRemove)
    {
        List<int> layout = [];

        // First iterate through the list and get the actual indexes to use for rows
        int nextToRemoveIndex = 0;

        // JS: toRemove[n] is undefined past the end, which never equals a line index
        int nextToRemoveStart = toRemove.Length > 0 ? toRemove[nextToRemoveIndex] : -1;
        int countRemovedSoFar = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (nextToRemoveStart == i)
            {
                int countToRemove = toRemove[++nextToRemoveIndex];

                // Tell markers that there was a deletion
                lines.OnDeleteEmitter.Fire(new DeleteEvent(i - countRemovedSoFar, countToRemove));

                i += countToRemove - 1;
                countRemovedSoFar += countToRemove;
                nextToRemoveStart = ++nextToRemoveIndex < toRemove.Length ? toRemove[nextToRemoveIndex] : -1;
            }
            else
            {
                layout.Add(i);
            }
        }

        return new NewLayoutResult([.. layout], countRemovedSoFar);
    }

    /// <summary>
    /// Applies a new layout to the buffer. This does the same as many splice calls but all at once in a single
    /// iteration through the list since splice is very expensive.
    /// </summary>
    /// <param name="lines">The buffer lines.</param>
    /// <param name="newLayout">The new layout to apply.</param>
    public static void ReflowLargerApplyNewLayout(CircularList<IBufferLine> lines, int[] newLayout)
    {
        // Record original lines so they don't get overridden when we rearrange the list
        List<BufferLine> newLayoutLines = [];
        for (int i = 0; i < newLayout.Length; i++)
        {
            newLayoutLines.Add((BufferLine)lines.Get(newLayout[i])!);
        }

        // Rearrange the list
        for (int i = 0; i < newLayoutLines.Count; i++)
        {
            lines.Set(i, newLayoutLines[i]);
        }

        lines.Length = newLayout.Length;
    }

    /// <summary>
    /// Gets the new line lengths for a given wrapped line. This pre-computes the wrapping points since wide characters
    /// may need to be wrapped onto the following line. The result only contains <paramref name="newCols"/> (when the
    /// line does not end with a wide character) and <c>newCols - 1</c> (when it does), except for the last value which
    /// is the remaining items to fill the line. Calling this with a <paramref name="newCols"/> of 1 will lock up.
    /// </summary>
    /// <param name="wrappedLines">The wrapped lines to evaluate.</param>
    /// <param name="oldCols">The columns before resize.</param>
    /// <param name="newCols">The columns after resize.</param>
    public static int[] ReflowSmallerGetNewLineLengths(IReadOnlyList<BufferLine> wrappedLines, int oldCols, int newCols)
    {
        List<int> newLineLengths = [];
        int cellsNeeded = 0;
        for (int i = 0; i < wrappedLines.Count; i++)
        {
            cellsNeeded += GetWrappedLineTrimmedLength(wrappedLines, i, oldCols);
        }

        // Use srcCol and srcLine to find the new wrapping point, use that to get the cellsAvailable and linesNeeded
        int srcCol = 0;
        int srcLine = 0;
        int cellsAvailable = 0;
        while (cellsAvailable < cellsNeeded)
        {
            if (cellsNeeded - cellsAvailable < newCols)
            {
                // Add the final line and exit the loop
                newLineLengths.Add(cellsNeeded - cellsAvailable);
                break;
            }

            srcCol += newCols;
            int oldTrimmedLength = GetWrappedLineTrimmedLength(wrappedLines, srcLine, oldCols);
            if (srcCol > oldTrimmedLength)
            {
                srcCol -= oldTrimmedLength;
                srcLine++;
            }

            bool endsWithWide = wrappedLines[srcLine].GetWidth(srcCol - 1) == 2;
            if (endsWithWide)
            {
                srcCol--;
            }

            int lineLength = endsWithWide ? newCols - 1 : newCols;
            newLineLengths.Add(lineLength);
            cellsAvailable += lineLength;
        }

        return [.. newLineLengths];
    }

    /// <summary>The trimmed length of line <paramref name="i"/> of a wrapped line, excluding a null cell before a wide char.</summary>
    public static int GetWrappedLineTrimmedLength(IReadOnlyList<BufferLine> lines, int i, int cols)
    {
        // If this is the last row in the wrapped line, get the actual trimmed length
        if (i == lines.Count - 1)
        {
            return lines[i].GetTrimmedLength();
        }

        // Detect whether the following line starts with a wide character and the end of the current line is null, if
        // so then we can be pretty sure the null character should be excluded from the line length
        bool endsInNull = lines[i].HasContent(cols - 1) == 0 && lines[i].GetWidth(cols - 1) == 1;
        bool followingLineStartsWithWide = lines[i + 1].GetWidth(0) == 2;
        if (endsInNull && followingLineStartsWithWide)
        {
            return cols - 1;
        }

        return cols;
    }
}
