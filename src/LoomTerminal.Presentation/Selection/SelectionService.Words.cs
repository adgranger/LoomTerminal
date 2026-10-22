// Ported from xtermjs/xterm.js: src/browser/services/SelectionService.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Presentation.Selection;

/// <summary>Word and line selection.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed partial class SelectionService
{
    /// <summary>Selects the word at the coordinates specified.</summary>
    /// <param name="coords">The coordinates to get the word at.</param>
    /// <param name="allowWhitespaceOnlySelection">If whitespace should be selected.</param>
    internal void SelectWordAt((int X, int Y) coords, bool allowWhitespaceOnlySelection)
    {
        if (GetWordAt(coords, allowWhitespaceOnlySelection) is { } wordPosition)
        {
            // Adjust negative start value
            var y = coords.Y;
            while (wordPosition.Start < 0)
            {
                wordPosition = wordPosition with { Start = wordPosition.Start + _bufferService.Cols };
                y--;
            }

            Model.SelectionStart = (wordPosition.Start, y);
            Model.SelectionStartLength = wordPosition.Length;
        }
    }

    /// <summary>Sets the selection end to the word at the coordinates specified.</summary>
    /// <param name="coords">The coordinates to get the word at.</param>
    internal void SelectToWordAt((int X, int Y) coords)
    {
        if (GetWordAt(coords, true) is { } wordPosition)
        {
            var endRow = coords.Y;

            // Adjust negative start value
            while (wordPosition.Start < 0)
            {
                wordPosition = wordPosition with { Start = wordPosition.Start + _bufferService.Cols };
                endRow--;
            }

            // Adjust wrapped length value, this only needs to happen when values are reversed as in that case we're
            // interested in the start of the word, not the end
            if (!Model.AreSelectionValuesReversed())
            {
                while (wordPosition.Start + wordPosition.Length > _bufferService.Cols)
                {
                    wordPosition = wordPosition with { Length = wordPosition.Length - _bufferService.Cols };
                    endRow++;
                }
            }

            Model.SelectionEnd = (Model.AreSelectionValuesReversed() ? wordPosition.Start : wordPosition.Start + wordPosition.Length, endRow);
        }
    }

    /// <summary>Selects the line specified (all its wrapped lines).</summary>
    /// <param name="line">The line index.</param>
    internal void SelectLineAt(int line)
    {
        var wrappedRange = _bufferService.Buffer.GetWrappedRangeForLine(line);
        var range = new BufferRange(new BufferCellPosition(0, wrappedRange.First), new BufferCellPosition(_bufferService.Cols - 1, wrappedRange.Last));
        Model.SelectionStart = (0, wrappedRange.First);
        Model.SelectionEnd = null;
        Model.SelectionStartLength = BufferRange.GetRangeLength(range, _bufferService.Cols);
    }

    // Safe versions of the JS charAt and slice: out of range is empty, not an error.
    private static bool IsSpaceAt(string line, int index) => index >= 0 && index < line.Length && line[index] == ' ';

    private static string Slice(string line, int start, int end)
    {
        start = Math.Clamp(start, 0, line.Length);
        end = Math.Clamp(end, start, line.Length);
        return line[start..end];
    }

    /// <summary>
    /// Converts a viewport column (0 to cols - 1) to the character index on the buffer line, the latter takes into
    /// account wide and null characters.
    /// </summary>
    private int ConvertViewportColToCharacterIndex(IBufferLine bufferLine, int x)
    {
        var charIndex = x;
        for (var i = 0; x >= i; i++)
        {
            var length = bufferLine.LoadCell(i, _workCell).GetChars().Length;
            if (_workCell.GetWidth() == 0)
            {
                // Wide characters aren't included in the line string so decrement the index so the index is back on
                // the wide character.
                charIndex--;
            }
            else if (length > 1 && x != i)
            {
                // Emojis take up multiple characters, so adjust accordingly. For these we don't want ot include the
                // character at the column as we're returning the start index in the string, not the end index.
                charIndex += length - 1;
            }
        }

        return charIndex;
    }

    /// <summary>Gets positional information for the word at the coordinates specified.</summary>
    private WordPosition? GetWordAt((int X, int Y) coords, bool allowWhitespaceOnlySelection, bool followWrappedLinesAbove = true, bool followWrappedLinesBelow = true)
    {
        var cols = _bufferService.Cols;

        // Ensure coords are within viewport (eg. not within scroll bar)
        if (coords.X >= cols)
        {
            return null;
        }

        var buffer = _bufferService.Buffer;
        var bufferLine = buffer.Lines.Get(coords.Y);
        if (bufferLine is null)
        {
            return null;
        }

        var line = buffer.TranslateBufferLineToString(coords.Y, false);

        // Get actual index, taking into consideration wide characters
        var startIndex = ConvertViewportColToCharacterIndex(bufferLine, coords.X);
        var endIndex = startIndex;

        // Record offset to be used later
        var charOffset = coords.X - startIndex;
        var leftWideCharCount = 0;
        var rightWideCharCount = 0;
        var leftLongCharOffset = 0;
        var rightLongCharOffset = 0;

        if (IsSpaceAt(line, startIndex))
        {
            // Expand until non-whitespace is hit
            while (startIndex > 0 && IsSpaceAt(line, startIndex - 1))
            {
                startIndex--;
            }

            while (endIndex < line.Length && IsSpaceAt(line, endIndex + 1))
            {
                endIndex++;
            }
        }
        else
        {
            // Expand until whitespace is hit. This algorithm works by scanning left and right from the starting
            // position, keeping both the index format (line) and the column format (bufferLine) in sync. When a wide
            // character is hit, it is recorded and the column index is adjusted.
            var startCol = coords.X;
            var endCol = coords.X;

            // Consider the initial position, skip it and increment the wide char variable
            if (bufferLine.GetWidth(startCol) == 0)
            {
                leftWideCharCount++;
                startCol--;
            }

            if (bufferLine.GetWidth(endCol) == 2)
            {
                rightWideCharCount++;
                endCol++;
            }

            // Adjust the end index for characters whose length are > 1 (emojis)
            var length = bufferLine.GetString(endCol).Length;
            if (length > 1)
            {
                rightLongCharOffset += length - 1;
                endIndex += length - 1;
            }

            // Expand the string in both directions until a space is hit
            while (startCol > 0 && startIndex > 0 && !IsCharWordSeparator(bufferLine.LoadCell(startCol - 1, _workCell)))
            {
                bufferLine.LoadCell(startCol - 1, _workCell);
                var cellLength = _workCell.GetChars().Length;
                if (_workCell.GetWidth() == 0)
                {
                    // If the next character is a wide char, record it and skip the column
                    leftWideCharCount++;
                    startCol--;
                }
                else if (cellLength > 1)
                {
                    // If the next character's string is longer than 1 char (eg. emoji), adjust the index
                    leftLongCharOffset += cellLength - 1;
                    startIndex -= cellLength - 1;
                }

                startIndex--;
                startCol--;
            }

            while (endCol < bufferLine.Length && endIndex + 1 < line.Length && !IsCharWordSeparator(bufferLine.LoadCell(endCol + 1, _workCell)))
            {
                bufferLine.LoadCell(endCol + 1, _workCell);
                var cellLength = _workCell.GetChars().Length;
                if (_workCell.GetWidth() == 2)
                {
                    // If the next character is a wide char, record it and skip the column
                    rightWideCharCount++;
                    endCol++;
                }
                else if (cellLength > 1)
                {
                    // If the next character's string is longer than 1 char (eg. emoji), adjust the index
                    rightLongCharOffset += cellLength - 1;
                    endIndex += cellLength - 1;
                }

                endIndex++;
                endCol++;
            }
        }

        // Increment the end index so it is at the start of the next character
        endIndex++;

        // Calculate the start _column_, converting the the string indexes back to column coordinates.
        var start =
            startIndex // The index of the selection's start char in the line string
            + charOffset // The difference between the initial char's column and index
            - leftWideCharCount // The number of wide chars left of the initial char
            + leftLongCharOffset; // The number of additional chars left of the initial char added by columns with strings longer than 1 (emojis)

        // Calculate the length in _columns_, converting the the string indexes back to column coordinates.
        var wordLength = Math.Min(
            cols, // Disallow lengths larger than the terminal cols
            endIndex // The index of the selection's end char in the line string
            - startIndex // The index of the selection's start char in the line string
            + leftWideCharCount // The number of wide chars left of the initial char
            + rightWideCharCount // The number of wide chars right of the initial char (inclusive)
            - leftLongCharOffset // The number of additional chars left of the initial char added by columns with strings longer than 1 (emojis)
            - rightLongCharOffset); // The number of additional chars right of the initial char (inclusive) added by columns with strings longer than 1 (emojis)

        if (!allowWhitespaceOnlySelection && Slice(line, startIndex, endIndex).Trim().Length == 0)
        {
            return null;
        }

        // Recurse upwards if the line is wrapped and the word wraps to the above line
        if (followWrappedLinesAbove && start == 0 && bufferLine.GetCodePoint(0) != 32)
        {
            var previousBufferLine = buffer.Lines.Get(coords.Y - 1);
            if (previousBufferLine is not null && bufferLine.IsWrapped && previousBufferLine.GetCodePoint(cols - 1) != 32
                && GetWordAt((cols - 1, coords.Y - 1), false, true, false) is { } previousLineWordPosition)
            {
                var offset = cols - previousLineWordPosition.Start;
                start -= offset;
                wordLength += offset;
            }
        }

        // Recurse downwards if the line is wrapped and the word wraps to the next line
        if (followWrappedLinesBelow && start + wordLength == cols && bufferLine.GetCodePoint(cols - 1) != 32)
        {
            var nextBufferLine = buffer.Lines.Get(coords.Y + 1);
            if (nextBufferLine is { IsWrapped: true } && nextBufferLine.GetCodePoint(0) != 32
                && GetWordAt((0, coords.Y + 1), false, false, true) is { } nextLineWordPosition)
            {
                wordLength += nextLineWordPosition.Length;
            }
        }

        return new WordPosition(start, wordLength);
    }

    /// <summary>Whether the character is considered a word separator by the select word logic.</summary>
    private bool IsCharWordSeparator(CellData cell)
    {
        // Zero width characters are never separators as they are always to the right of wide characters
        if (cell.GetWidth() == 0)
        {
            return false;
        }

        return _optionsService.RawOptions.WordSeparator.Contains(cell.GetChars(), StringComparison.Ordinal);
    }

    /// <summary>Represents a position of a word on a line.</summary>
    private readonly record struct WordPosition(int Start, int Length);
}
