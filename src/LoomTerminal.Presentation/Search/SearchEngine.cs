// Ported from xtermjs/xterm.js: addons/addon-search/src/SearchEngine.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Globalization;

namespace LoomTerminal.Presentation.Search;

/// <summary>
/// Core search engine that handles finding text within terminal content: the search algorithms and the position
/// calculations. An invalid regular expression, or one that takes more than a second, finds nothing.
/// </summary>
/// <remarks>Threading: parser thread only: it reads the emulator.</remarks>
internal sealed partial class SearchEngine(SearchBufferView terminal, SearchLineCache lineCache)
{
    // Characters that are considered non-word characters for search boundary detection (whole-word searches).
    private const string NonWordCharacters = " ~!@#$%^&*()+`-=[]{}|\\;:\"',./<>?";

    /// <summary>Finds the first occurrence of a term starting from a specific position.</summary>
    /// <param name="term">The search term.</param>
    /// <param name="startRow">The row to start searching from.</param>
    /// <param name="startCol">The column to start searching from.</param>
    /// <param name="searchOptions">Search options.</param>
    /// <returns>The search result if found, <see langword="null"/> otherwise.</returns>
    internal SearchResult? Find(string? term, int startRow, int startCol, SearchOptions? searchOptions = null)
    {
        if (string.IsNullOrEmpty(term))
        {
            terminal.ClearSelection();
            return null;
        }

        if (startCol > terminal.Cols)
        {
            throw new ArgumentException(string.Create(CultureInfo.InvariantCulture, $"Invalid col: {startCol} to search in terminal of {terminal.Cols} cols"));
        }

        lineCache.InitLinesCache();

        var searchPosition = new SearchPosition(startRow, startCol);

        // Search startRow, then from startRow + 1 to end
        var result = FindInLine(term, searchPosition, searchOptions);
        return result ?? FindFromRowToEnd(term, startRow, searchPosition, searchOptions);
    }

    /// <summary>Finds the next occurrence of a term with wrapping and selection management.</summary>
    /// <param name="term">The search term.</param>
    /// <param name="searchOptions">Search options.</param>
    /// <param name="cachedSearchTerm">The cached search term to determine incremental behavior.</param>
    /// <returns>The search result if found, <see langword="null"/> otherwise.</returns>
    internal SearchResult? FindNextWithSelection(string? term, SearchOptions? searchOptions = null, string? cachedSearchTerm = null)
    {
        if (string.IsNullOrEmpty(term))
        {
            terminal.ClearSelection();
            return null;
        }

        var prevSelectedPos = terminal.GetSelectionPosition();
        terminal.ClearSelection();

        var startCol = 0;
        var startRow = 0;
        if (prevSelectedPos is { } prev)
        {
            if (cachedSearchTerm == term)
            {
                startCol = prev.End.X;
                startRow = prev.End.Y;
            }
            else
            {
                startCol = prev.Start.X;
                startRow = prev.Start.Y;
            }
        }

        lineCache.InitLinesCache();

        var searchPosition = new SearchPosition(startRow, startCol);

        // Search startRow, then from startRow + 1 to end
        var result = FindInLine(term, searchPosition, searchOptions) ?? FindFromRowToEnd(term, startRow, searchPosition, searchOptions);

        // If we hit the bottom and didn't search from the very top wrap back up
        if (result is null && startRow != 0)
        {
            for (var y = 0; y < startRow; y++)
            {
                searchPosition.StartRow = y;
                searchPosition.StartCol = 0;
                result = FindInLine(term, searchPosition, searchOptions);
                if (result is not null)
                {
                    break;
                }
            }
        }

        // If there is only one result, wrap back and return selection if it exists.
        if (result is null && prevSelectedPos is { } selected)
        {
            searchPosition.StartRow = selected.Start.Y;
            searchPosition.StartCol = 0;
            result = FindInLine(term, searchPosition, searchOptions);
        }

        return result;
    }

    /// <summary>Finds the previous occurrence of a term with wrapping and selection management.</summary>
    /// <param name="term">The search term.</param>
    /// <param name="searchOptions">Search options.</param>
    /// <param name="cachedSearchTerm">The cached search term to determine if expansion should occur.</param>
    /// <returns>The search result if found, <see langword="null"/> otherwise.</returns>
    internal SearchResult? FindPreviousWithSelection(string? term, SearchOptions? searchOptions = null, string? cachedSearchTerm = null)
    {
        if (string.IsNullOrEmpty(term))
        {
            terminal.ClearSelection();
            return null;
        }

        var prevSelectedPos = terminal.GetSelectionPosition();
        terminal.ClearSelection();

        var lastRow = terminal.BaseY + terminal.Rows - 1;
        var startRow = lastRow;
        var startCol = terminal.Cols;
        const bool isReverseSearch = true;

        lineCache.InitLinesCache();
        var searchPosition = new SearchPosition(startRow, startCol);

        SearchResult? result = null;
        if (prevSelectedPos is { } prev)
        {
            searchPosition.StartRow = startRow = prev.Start.Y;
            searchPosition.StartCol = startCol = prev.Start.X;
            if (cachedSearchTerm != term)
            {
                // Try to expand selection to right first.
                result = FindInLine(term, searchPosition, searchOptions, false);
                if (result is null)
                {
                    // If selection was not able to be expanded to the right, then try reverse search
                    searchPosition.StartRow = startRow = prev.End.Y;
                    searchPosition.StartCol = startCol = prev.End.X;
                }
            }
        }

        result ??= FindInLine(term, searchPosition, searchOptions, isReverseSearch);

        // Search from startRow - 1 to top
        if (result is null)
        {
            searchPosition.StartCol = Math.Max(searchPosition.StartCol, terminal.Cols);
            for (var y = startRow - 1; y >= 0; y--)
            {
                searchPosition.StartRow = y;
                result = FindInLine(term, searchPosition, searchOptions, isReverseSearch);
                if (result is not null)
                {
                    break;
                }
            }
        }

        // If we hit the top and didn't search from the very bottom wrap back down
        if (result is null && startRow != lastRow)
        {
            for (var y = lastRow; y >= startRow; y--)
            {
                searchPosition.StartRow = y;
                result = FindInLine(term, searchPosition, searchOptions, isReverseSearch);
                if (result is not null)
                {
                    break;
                }
            }
        }

        return result;
    }

    // Searches from startRow + 1 to the end of the buffer.
    private SearchResult? FindFromRowToEnd(string term, int startRow, SearchPosition searchPosition, SearchOptions? searchOptions)
    {
        for (var y = startRow + 1; y < terminal.BaseY + terminal.Rows; y++)
        {
            searchPosition.StartRow = y;
            searchPosition.StartCol = 0;
            var result = FindInLine(term, searchPosition, searchOptions);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    // A found substring is a whole word if it doesn't have an alphanumeric character directly adjacent to it.
    private static bool IsWholeWord(int searchIndex, string line, string term) =>
        (searchIndex == 0 || NonWordCharacters.Contains(line[searchIndex - 1], StringComparison.Ordinal))
        && (searchIndex + term.Length == line.Length || NonWordCharacters.Contains(line[searchIndex + term.Length], StringComparison.Ordinal));

    /// <summary>The position to start a search from; <see cref="FindInLine"/> moves it while it skips wrapped rows.</summary>
    private sealed class SearchPosition(int startRow, int startCol)
    {
        internal int StartRow { get; set; } = startRow;

        internal int StartCol { get; set; } = startCol;
    }
}
