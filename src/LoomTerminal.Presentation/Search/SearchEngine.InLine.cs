// Ported from xtermjs/xterm.js: addons/addon-search/src/SearchEngine.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Text.RegularExpressions;

namespace LoomTerminal.Presentation.Search;

internal sealed partial class SearchEngine
{
    // Patterns come from the user: no [GeneratedRegex], and a time limit against catastrophic backtracking.
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    // Searches a line for a search term. Takes the provided terminal line and searches the text line, which may contain
    // subsequent terminal lines if the text is wrapped. If the provided line number is part of a wrapped text line that
    // started on an earlier line then it is skipped since it will be properly searched when the terminal line that the
    // text starts on is searched.
    private SearchResult? FindInLine(string term, SearchPosition searchPosition, SearchOptions? searchOptions, bool isReverseSearch = false)
    {
        searchOptions ??= new SearchOptions();
        var row = searchPosition.StartRow;
        var col = searchPosition.StartCol;

        // Ignore wrapped lines, only consider on unwrapped line (first row of command string).
        if (terminal.GetLine(row) is { IsWrapped: true })
        {
            if (isReverseSearch)
            {
                searchPosition.StartCol += terminal.Cols;
                return null;
            }

            // This will iterate until we find the line start. When we find it, we will search using the calculated start column.
            searchPosition.StartRow--;
            searchPosition.StartCol += terminal.Cols;
            return FindInLine(term, searchPosition, searchOptions);
        }

        var cache = lineCache.GetLineFromCache(row);
        if (cache is null)
        {
            cache = lineCache.TranslateBufferLineToStringWithWrap(row, true);
            lineCache.SetLineInCache(row, cache.Value);
        }

        var (stringLine, offsets) = cache.Value;

        var offset = BufferColsToStringOffset(row, col);
        var searchTerm = term;
        var searchStringLine = stringLine;
        if (!searchOptions.Regex)
        {
            searchTerm = searchOptions.CaseSensitive ? term : term.ToLowerInvariant();
            searchStringLine = searchOptions.CaseSensitive ? stringLine : stringLine.ToLowerInvariant();
        }

        var resultIndex = -1;
        if (searchOptions.Regex)
        {
            if (!TryFindRegex(searchTerm, searchOptions.CaseSensitive, searchStringLine, offset, isReverseSearch, out resultIndex, out term))
            {
                return null;
            }
        }
        else if (isReverseSearch)
        {
            if (offset - searchTerm.Length >= 0)
            {
                // The last match that starts at or before offset - length, that is, ends at or before offset.
                resultIndex = searchStringLine.AsSpan(0, Math.Min(offset, searchStringLine.Length)).LastIndexOf(searchTerm, StringComparison.Ordinal);
            }
        }
        else if (offset <= searchStringLine.Length)
        {
            resultIndex = searchStringLine.IndexOf(searchTerm, offset, StringComparison.Ordinal);
        }

        if (resultIndex < 0)
        {
            return null;
        }

        if (searchOptions.WholeWord && !IsWholeWord(resultIndex, searchStringLine, term))
        {
            return null;
        }

        // Adjust the row number and search index if needed since a "line" of text can span multiple rows
        var startRowOffset = 0;
        while (startRowOffset < offsets.Length - 1 && resultIndex >= offsets[startRowOffset + 1])
        {
            startRowOffset++;
        }

        var endRowOffset = startRowOffset;
        while (endRowOffset < offsets.Length - 1 && resultIndex + term.Length >= offsets[endRowOffset + 1])
        {
            endRowOffset++;
        }

        var startColOffset = resultIndex - offsets[startRowOffset];
        var endColOffset = resultIndex + term.Length - offsets[endRowOffset];
        var startColIndex = StringLengthToBufferSize(row + startRowOffset, startColOffset);
        var endColIndex = StringLengthToBufferSize(row + endRowOffset, endColOffset);
        var size = endColIndex - startColIndex + (terminal.Cols * (endRowOffset - startRowOffset));

        return new SearchResult(term, startColIndex, row + startRowOffset, size);
    }

    // Runs the regular expression; false when the pattern is invalid or too slow (JS: a SyntaxError). resultIndex stays
    // -1 when there is no match, and term becomes the matched text.
    private static bool TryFindRegex(string pattern, bool caseSensitive, string line, int offset, bool isReverseSearch, out int resultIndex, out string term)
    {
        resultIndex = -1;
        term = pattern;
        Regex searchRegex;
        try
        {
            var options = caseSensitive ? RegexOptions.CultureInvariant : RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
            searchRegex = new Regex(pattern, options, RegexTimeout);
        }
        catch (ArgumentException)
        {
            return false;
        }

        try
        {
            if (isReverseSearch)
            {
                // The index of the _last_ regex match in the range 0..offset; matches may overlap, so the next search
                // starts one character after the start of the previous match.
                var range = line[..Math.Min(offset, line.Length)];
                var position = 0;
                while (position <= range.Length && searchRegex.Match(range, position) is { Success: true } found)
                {
                    resultIndex = found.Index;
                    term = found.Value;
                    position = found.Index + 1;
                }
            }
            else if (offset <= line.Length && searchRegex.Match(line[offset..]) is { Success: true, Length: > 0 } match)
            {
                resultIndex = offset + match.Index;
                term = match.Value;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            resultIndex = -1;
            return false;
        }

        return true;
    }

    private int StringLengthToBufferSize(int row, int offset)
    {
        var line = terminal.GetLine(row);
        if (line is null)
        {
            return 0;
        }

        for (var i = 0; i < offset; i++)
        {
            if (i >= line.Length)
            {
                break;
            }

            // Adjust the searchIndex to normalize emoji into single chars
            var chars = line.GetString(i);
            if (chars.Length > 1)
            {
                offset -= chars.Length - 1;
            }

            // Adjust the searchIndex for empty characters following wide unicode chars (eg. CJK)
            if (i + 1 < line.Length && line.GetWidth(i + 1) == 0)
            {
                offset++;
            }
        }

        return offset;
    }

    private int BufferColsToStringOffset(int startRow, int cols)
    {
        var lineIndex = startRow;
        var offset = 0;
        var line = terminal.GetLine(lineIndex);
        while (cols > 0 && line is not null)
        {
            for (var i = 0; i < cols && i < terminal.Cols; i++)
            {
                if (i >= line.Length)
                {
                    break;
                }

                if (line.GetWidth(i) != 0)
                {
                    // Treat null characters as whitespace to align with the translateToString API
                    offset += line.GetCodePoint(i) == 0 ? 1 : line.GetString(i).Length;
                }
            }

            lineIndex++;
            line = terminal.GetLine(lineIndex);
            if (line is { IsWrapped: false })
            {
                break;
            }

            cols -= terminal.Cols;
        }

        return offset;
    }
}
