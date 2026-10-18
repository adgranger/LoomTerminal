// Ported from xtermjs/xterm.js: src/common/buffer/Buffer.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Buffer;

internal sealed partial class Buffer
{
    private void Reflow(int newCols, int newRows)
    {
        if (_cols == newCols)
        {
            return;
        }

        // Iterate through rows, ignore the last one as it cannot be wrapped
        if (newCols > _cols)
        {
            ReflowLarger(newCols, newRows);
        }
        else
        {
            ReflowSmaller(newCols, newRows);
        }
    }

    private void ReflowLarger(int newCols, int newRows)
    {
        bool reflowCursorLine = _optionsService.RawOptions.ReflowCursorLine;
        int[] toRemove = BufferReflow.ReflowLargerGetLinesToRemove(
            Lines,
            _cols,
            newCols,
            YBase + Y,
            GetNullCell(AttributeData.DefaultAttrData),
            reflowCursorLine);
        if (toRemove.Length > 0)
        {
            NewLayoutResult newLayoutResult = BufferReflow.ReflowLargerCreateNewLayout(Lines, toRemove);
            BufferReflow.ReflowLargerApplyNewLayout(Lines, newLayoutResult.Layout);
            ReflowLargerAdjustViewport(newCols, newRows, newLayoutResult.CountRemoved);
        }
    }

    private void ReflowLargerAdjustViewport(int newCols, int newRows, int countRemoved)
    {
        CellData nullCell = GetNullCell(AttributeData.DefaultAttrData);

        // Adjust viewport based on number of items removed
        int viewportAdjustments = countRemoved;
        while (viewportAdjustments-- > 0)
        {
            if (YBase == 0)
            {
                if (Y > 0)
                {
                    Y--;
                }

                if (Lines.Length < newRows)
                {
                    // Add an extra row at the bottom of the viewport
                    Lines.Push(new BufferLine(newCols, nullCell));
                }
            }
            else
            {
                if (YDisp == YBase)
                {
                    YDisp--;
                }

                YBase--;
            }
        }

        SavedY = Math.Max(SavedY - countRemoved, 0);
    }

    private void ReflowSmaller(int newCols, int newRows)
    {
        bool reflowCursorLine = _optionsService.RawOptions.ReflowCursorLine;
        CellData nullCell = GetNullCell(AttributeData.DefaultAttrData);

        // Gather all BufferLines that need to be inserted into the Buffer here so that they can be batched up and only
        // committed once
        List<InsertItem> toInsert = [];
        int countToInsert = 0;

        // Go backwards as many lines may be trimmed and this will avoid considering them
        for (int y = Lines.Length - 1; y >= 0; y--)
        {
            // Check whether this line is a problem
            BufferLine? nextLine = Lines.Get(y) as BufferLine;
            if (nextLine is null || (!nextLine.IsWrapped && nextLine.GetTrimmedLength() <= newCols))
            {
                continue;
            }

            // Gather wrapped lines and adjust y to be the starting line
            List<BufferLine> wrappedLines = [nextLine];
            while (nextLine!.IsWrapped && y > 0)
            {
                nextLine = (BufferLine)Lines.Get(--y)!;
                wrappedLines.Insert(0, nextLine);
            }

            if (!reflowCursorLine)
            {
                // If these lines contain the cursor don't touch them, the program will handle fixing up wrapped lines
                // with the cursor
                int absoluteY = YBase + Y;
                if (absoluteY >= y && absoluteY < y + wrappedLines.Count)
                {
                    continue;
                }
            }

            int lastLineLength = wrappedLines[^1].GetTrimmedLength();
            int[] destLineLengths = BufferReflow.ReflowSmallerGetNewLineLengths(wrappedLines, _cols, newCols);
            int linesToAdd = destLineLengths.Length - wrappedLines.Count;
            int trimmedLines;
            if (YBase == 0 && Y != Lines.Length - 1)
            {
                // If the top section of the buffer is not yet filled
                trimmedLines = Math.Max(0, Y - Lines.MaxLength + linesToAdd);
            }
            else
            {
                trimmedLines = Math.Max(0, Lines.Length - Lines.MaxLength + linesToAdd);
            }

            // Add the new lines
            List<BufferLine> newLines = [];
            for (int i = 0; i < linesToAdd; i++)
            {
                BufferLine newLine = (BufferLine)GetBlankLine(AttributeData.DefaultAttrData, true);
                newLines.Add(newLine);
            }

            if (newLines.Count > 0)
            {
                // countToInsert here gets the actual index, taking into account other inserted items. Using this we
                // can iterate through the list forwards
                toInsert.Add(new InsertItem(y + wrappedLines.Count + countToInsert, newLines));
                countToInsert += newLines.Count;
            }

            wrappedLines.AddRange(newLines);

            // Copy buffer data to new locations, this needs to happen backwards to do in-place
            int destLineIndex = destLineLengths.Length - 1; // Math.floor(cellsNeeded / newCols);
            int destCol = destLineLengths[destLineIndex]; // cellsNeeded % newCols;
            if (destCol == 0)
            {
                destLineIndex--;
                destCol = LengthAt(destLineLengths, destLineIndex);
            }

            int srcLineIndex = wrappedLines.Count - linesToAdd - 1;
            int srcCol = lastLineLength;
            while (srcLineIndex >= 0)
            {
                int cellsToCopy = Math.Min(srcCol, destCol);
                if (destLineIndex < 0 || destLineIndex >= wrappedLines.Count)
                {
                    // Sanity check that the line exists, this has been known to fail for an unknown reason which would
                    // stop the reflow from happening if an exception would throw.
                    break;
                }

                wrappedLines[destLineIndex].CopyCellsFrom(wrappedLines[srcLineIndex], srcCol - cellsToCopy, destCol - cellsToCopy, cellsToCopy, true);
                destCol -= cellsToCopy;
                if (destCol == 0)
                {
                    destLineIndex--;
                    destCol = LengthAt(destLineLengths, destLineIndex);
                }

                srcCol -= cellsToCopy;
                if (srcCol == 0)
                {
                    srcLineIndex--;
                    int wrappedLinesIndex = Math.Max(srcLineIndex, 0);
                    srcCol = BufferReflow.GetWrappedLineTrimmedLength(wrappedLines, wrappedLinesIndex, _cols);
                }
            }

            // Null out the end of the line ends if a wide character wrapped to the following line
            for (int i = 0; i < wrappedLines.Count; i++)
            {
                if (destLineLengths[i] < newCols)
                {
                    wrappedLines[i].SetCell(destLineLengths[i], nullCell);
                }
            }

            // Adjust viewport as needed
            int viewportAdjustments = linesToAdd - trimmedLines;
            while (viewportAdjustments-- > 0)
            {
                if (YBase == 0)
                {
                    if (Y < newRows - 1)
                    {
                        Y++;
                        Lines.Pop();
                    }
                    else
                    {
                        YBase++;
                        YDisp++;
                    }
                }
                else
                {
                    // Ensure YBase does not exceed its maximum value
                    if (YBase < Math.Min(Lines.MaxLength, Lines.Length + countToInsert) - newRows)
                    {
                        if (YBase == YDisp)
                        {
                            YDisp++;
                        }

                        YBase++;
                    }
                }
            }

            SavedY = Math.Min(SavedY + linesToAdd, YBase + newRows - 1);
        }

        // Rearrange lines in the buffer if there are any insertions, this is done at the end rather than earlier so
        // that it's a single O(n) pass through the buffer, instead of O(n^2) from many costly calls to
        // CircularList.Splice.
        if (toInsert.Count > 0)
        {
            // Record buffer insert events and then play them back backwards so that the indexes are correct
            List<InsertEvent> insertEvents = [];

            // Record original lines so they don't get overridden when we rearrange the list
            List<BufferLine> originalLines = [];
            for (int i = 0; i < Lines.Length; i++)
            {
                originalLines.Add((BufferLine)Lines.Get(i)!);
            }

            int originalLinesLength = Lines.Length;

            int originalLineIndex = originalLinesLength - 1;
            int nextToInsertIndex = 0;
            InsertItem? nextToInsert = toInsert[nextToInsertIndex];
            Lines.Length = Math.Min(Lines.MaxLength, Lines.Length + countToInsert);
            int countInsertedSoFar = 0;
            for (int i = Math.Min(Lines.MaxLength - 1, originalLinesLength + countToInsert - 1); i >= 0; i--)
            {
                if (nextToInsert is not null && nextToInsert.Start > originalLineIndex + countInsertedSoFar)
                {
                    // Insert extra lines here, adjusting i as needed
                    for (int nextI = nextToInsert.NewLines.Count - 1; nextI >= 0; nextI--)
                    {
                        Lines.Set(i--, nextToInsert.NewLines[nextI]);
                    }

                    i++;

                    // Create insert events for later
                    insertEvents.Add(new InsertEvent(originalLineIndex + 1, nextToInsert.NewLines.Count));

                    countInsertedSoFar += nextToInsert.NewLines.Count;
                    nextToInsert = ++nextToInsertIndex < toInsert.Count ? toInsert[nextToInsertIndex] : null;
                }
                else
                {
                    Lines.Set(i, originalLines[originalLineIndex--]);
                }
            }

            // Update markers
            int insertCountEmitted = 0;
            for (int i = insertEvents.Count - 1; i >= 0; i--)
            {
                InsertEvent insertEvent = insertEvents[i] with { Index = insertEvents[i].Index + insertCountEmitted };
                Lines.OnInsertEmitter.Fire(insertEvent);
                insertCountEmitted += insertEvent.Amount;
            }

            int amountToTrim = Math.Max(0, originalLinesLength + countToInsert - Lines.MaxLength);
            if (amountToTrim > 0)
            {
                Lines.OnTrimEmitter.Fire(amountToTrim);
            }
        }
    }

    // JS: reading past the ends of an array gives undefined, which only ends up unused (see the sanity check above)
    private static int LengthAt(int[] lengths, int index) => index >= 0 && index < lengths.Length ? lengths[index] : 0;

    private sealed record InsertItem(int Start, List<BufferLine> NewLines);
}
