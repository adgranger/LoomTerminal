// Ported from xtermjs/xterm.js: src/browser/input/MoveToCell.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Text;
using LoomTerminal.Core.Data;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Presentation.Input;

/// <summary>
/// Builds the arrow key sequences that move the cursor of the shell to a cell (Alt+click). Concatenates all the arrow
/// sequences together: resets the starting row to an unwrapped row, moves to the requested row, then moves to the
/// requested col.
/// </summary>
/// <remarks>Threading: stateless; reads the buffer, so parser thread only.</remarks>
internal static class MoveToCell
{
    /// <summary>The arrow sequences that move the cursor to <paramref name="targetX"/>, <paramref name="targetY"/>.</summary>
    /// <param name="targetX">Target column.</param>
    /// <param name="targetY">Target row, relative to the viewport.</param>
    /// <param name="bufferService">The buffers.</param>
    /// <param name="applicationCursor">Whether DECCKM is set.</param>
    /// <returns>The sequences, to send to the shell.</returns>
    internal static string Sequence(int targetX, int targetY, IBufferService bufferService, bool applicationCursor)
    {
        var startX = bufferService.Buffer.X;
        var startY = bufferService.Buffer.Y;

        // The alt buffer should try to navigate between rows
        if (!bufferService.Buffer.HasScrollback)
        {
            return ResetStartingRow(startX, startY, targetX, targetY, bufferService, applicationCursor)
                + MoveToRequestedRow(startY, targetY, bufferService, applicationCursor)
                + MoveToRequestedCol(startX, startY, targetX, targetY, bufferService, applicationCursor);
        }

        // Only move horizontally for the normal buffer
        char direction;
        if (startY == targetY)
        {
            direction = startX > targetX ? 'D' : 'C';
            return Repeat(Math.Abs(startX - targetX), Arrow(direction, applicationCursor));
        }

        direction = startY > targetY ? 'D' : 'C';
        var rowDifference = Math.Abs(startY - targetY);
        var cellsToMove = ColsFromRowEnd(startY > targetY ? targetX : startX, bufferService)
            + ((rowDifference - 1) * bufferService.Cols) + 1 /* wrap around 1 row */
            + ColsFromRowBeginning(startY > targetY ? startX : targetX);
        return Repeat(cellsToMove, Arrow(direction, applicationCursor));
    }

    // Find the number of cols from a row beginning to a col.
    private static int ColsFromRowBeginning(int currX) => currX - 1;

    // Find the number of cols from a col to row end.
    private static int ColsFromRowEnd(int currX, IBufferService bufferService) => bufferService.Cols - currX;

    // If the initial position of the cursor is on a row that is wrapped, move the cursor up to the first row that is
    // not wrapped to have accurate vertical positioning.
    private static string ResetStartingRow(int startX, int startY, int targetX, int targetY, IBufferService bufferService, bool applicationCursor)
    {
        if (MoveToRequestedRow(startY, targetY, bufferService, applicationCursor).Length == 0)
        {
            return string.Empty;
        }

        return Repeat(
            BufferLine(startX, startY, startX, startY - WrappedRowsForRow(startY, bufferService), false, bufferService).Length,
            Arrow('D', applicationCursor));
    }

    // Using the reset starting and ending row, move to the requested row, ignoring wrapped rows
    private static string MoveToRequestedRow(int startY, int targetY, IBufferService bufferService, bool applicationCursor)
    {
        var startRow = startY - WrappedRowsForRow(startY, bufferService);
        var endRow = targetY - WrappedRowsForRow(targetY, bufferService);
        var rowsToMove = Math.Abs(startRow - endRow) - WrappedRowsCount(startY, targetY, bufferService);
        return Repeat(rowsToMove, Arrow(VerticalDirection(startY, targetY), applicationCursor));
    }

    // Move to the requested col on the ending row
    private static string MoveToRequestedCol(int startX, int startY, int targetX, int targetY, IBufferService bufferService, bool applicationCursor)
    {
        var startRow = MoveToRequestedRow(startY, targetY, bufferService, applicationCursor).Length > 0
            ? targetY - WrappedRowsForRow(targetY, bufferService)
            : startY;
        var endRow = targetY;
        var direction = HorizontalDirection(startX, startY, targetX, targetY, bufferService, applicationCursor);
        return Repeat(
            BufferLine(startX, startRow, targetX, endRow, direction == 'C', bufferService).Length,
            Arrow(direction, applicationCursor));
    }

    // Calculates the number of wrapped rows between the unwrapped starting and ending rows. These rows need to ignored
    // since the cursor skips over them.
    private static int WrappedRowsCount(int startY, int targetY, IBufferService bufferService)
    {
        var wrappedRows = 0;
        var startRow = startY - WrappedRowsForRow(startY, bufferService);
        var endRow = targetY - WrappedRowsForRow(targetY, bufferService);

        for (var i = 0; i < Math.Abs(startRow - endRow); i++)
        {
            var direction = VerticalDirection(startY, targetY) == 'A' ? -1 : 1;
            var line = bufferService.Buffer.Lines.Get(startRow + (direction * i));
            if (line?.IsWrapped == true)
            {
                wrappedRows++;
            }
        }

        return wrappedRows;
    }

    // Calculates the number of wrapped rows that make up a given row.
    private static int WrappedRowsForRow(int currentRow, IBufferService bufferService)
    {
        var rowCount = 0;
        var line = bufferService.Buffer.Lines.Get(currentRow);
        var lineWraps = line?.IsWrapped == true;

        while (lineWraps && currentRow >= 0 && currentRow < bufferService.Rows)
        {
            rowCount++;
            line = bufferService.Buffer.Lines.Get(--currentRow);
            lineWraps = line?.IsWrapped == true;
        }

        return rowCount;
    }

    // Determines if the right or left arrow is needed
    private static char HorizontalDirection(int startX, int startY, int targetX, int targetY, IBufferService bufferService, bool applicationCursor)
    {
        var startRow = MoveToRequestedRow(targetX, targetY, bufferService, applicationCursor).Length > 0
            ? targetY - WrappedRowsForRow(targetY, bufferService)
            : startY;

        if ((startX < targetX && startRow <= targetY) // down/right or same y/right
            || (startX >= targetX && startRow < targetY))
        {
            // down/left or same y/left
            return 'C';
        }

        return 'D';
    }

    // Determines if the up or down arrow is needed
    private static char VerticalDirection(int startY, int targetY) => startY > targetY ? 'A' : 'B';

    // Constructs the string of chars in the buffer from a starting row and col to an ending row and col
    private static string BufferLine(int startCol, int startRow, int endCol, int endRow, bool forward, IBufferService bufferService)
    {
        var currentCol = startCol;
        var currentRow = startRow;
        var bufferStr = new StringBuilder();
        var buffer = bufferService.Buffer;

        while ((currentCol != endCol || currentRow != endRow) && currentRow >= 0 && currentRow < buffer.Lines.Length)
        {
            currentCol += forward ? 1 : -1;

            if (forward && currentCol > bufferService.Cols - 1)
            {
                bufferStr.Append(buffer.TranslateBufferLineToString(currentRow, false, startCol, currentCol));
                currentCol = 0;
                startCol = 0;
                currentRow++;
            }
            else if (!forward && currentCol < 0)
            {
                bufferStr.Append(buffer.TranslateBufferLineToString(currentRow, false, 0, startCol + 1));
                currentCol = bufferService.Cols - 1;
                startCol = currentCol;
                currentRow--;
            }
        }

        return bufferStr + buffer.TranslateBufferLineToString(currentRow, false, startCol, currentCol);
    }

    // Constructs the escape sequence for clicking an arrow
    private static string Arrow(char direction, bool applicationCursor) => C0.Esc + (applicationCursor ? 'O' : '[') + direction;

    private static string Repeat(int count, string text) => count <= 0 ? string.Empty : string.Concat(Enumerable.Repeat(text, count));
}
