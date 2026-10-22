// Ported from xtermjs/xterm.js: addons/addon-search/src/SearchResultTracker.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Presentation.Search;

/// <summary>Tracks the search results and the active match, and turns them into highlights.</summary>
/// <remarks>Threading: parser thread only.</remarks>
internal sealed class SearchResultTracker
{
    private SearchResult[] _results = [];
    private SearchHighlightMap? _map;
    private int _mapCols = -1;

    /// <summary>The current results.</summary>
    internal IReadOnlyList<SearchResult> Results => _results;

    /// <summary>The active match (<c>selectedDecoration.match</c>), if any.</summary>
    internal SearchResult? Selected { get; private set; }

    /// <summary>Replaces the results.</summary>
    /// <param name="results">The new results.</param>
    /// <param name="maxResults">The maximum number of results to track.</param>
    internal void UpdateResults(IReadOnlyList<SearchResult> results, int maxResults)
    {
        _results = results.Take(maxResults).ToArray();
        _mapCols = -1;
    }

    /// <summary>Clears all the results.</summary>
    internal void ClearResults()
    {
        _results = [];
        _mapCols = -1;
    }

    /// <summary>Sets the active match.</summary>
    /// <param name="result">The match, or <see langword="null"/> for none.</param>
    internal void SetSelected(SearchResult? result)
    {
        Selected = result;
        _mapCols = -1;
    }

    /// <summary>Clears the active match.</summary>
    internal void ClearSelectedDecoration() => SetSelected(null);

    /// <summary>Finds the index of a result in the current results.</summary>
    /// <param name="result">The result to find.</param>
    /// <returns>The index, or <c>-1</c> when it is not found.</returns>
    internal int FindResultIndex(SearchResult result)
    {
        for (var i = 0; i < _results.Length; i++)
        {
            var match = _results[i];
            if (match.Row == result.Row && match.Col == result.Col && match.Size == result.Size)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>The current state: the active match among the results, and how many there are.</summary>
    /// <returns>The summary.</returns>
    internal SearchResultSummary Summary() =>
        new(Selected is { } selected ? FindResultIndex(selected) : -1, _results.Length);

    /// <summary>Gets the highlights to paint; the same instance until something changes.</summary>
    /// <param name="cols">Number of columns.</param>
    /// <returns>The map, or <see langword="null"/> when nothing is highlighted.</returns>
    internal SearchHighlightMap? Highlights(int cols)
    {
        if (_mapCols != cols)
        {
            _map = SearchHighlightMap.Create(_results, Selected, cols);
            _mapCols = cols;
        }

        return _map;
    }
}
