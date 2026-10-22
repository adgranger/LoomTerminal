// Ported from xtermjs/xterm.js: addons/addon-search/src/SearchAddon.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core;
using LoomTerminal.Presentation.Selection;

namespace LoomTerminal.Presentation.Search;

/// <summary>
/// Find next / previous with highlights and selection: the logic of <c>SearchAddon</c> without the decoration timer
/// (the matches are not refreshed when output arrives; search again).
/// </summary>
/// <remarks>Threading: parser thread only: it reads the emulator and drives the selection.</remarks>
internal sealed class SearchAddon : IDisposable
{
    // Maximum number of matches highlighted at once (DEFAULT_HIGHLIGHT_LIMIT).
    private const int HighlightLimit = 1000;

    private readonly TerminalCore _core;
    private readonly SelectionService _selection;
    private readonly SearchLineCache _lineCache;
    private readonly SearchEngine _engine;
    private readonly SearchState _state = new();

    /// <summary>Creates the search over <paramref name="core"/>; matches are selected through <paramref name="selection"/>.</summary>
    /// <param name="core">The emulator.</param>
    /// <param name="selection">The selection logic.</param>
    internal SearchAddon(TerminalCore core, SelectionService selection)
    {
        _core = core;
        _selection = selection;
        var view = new SearchBufferView(core, selection);
        _lineCache = new SearchLineCache(view);
        _engine = new SearchEngine(view, _lineCache);
    }

    /// <summary>The results and the active match.</summary>
    internal SearchResultTracker Tracker { get; } = new();

    /// <summary>Raised after each find, when decorations are on (<c>onDidChangeResults</c>).</summary>
    internal event Action<SearchResultSummary>? ResultsChanged;

    /// <summary>Finds the next match of <paramref name="term"/>, then scrolls to and selects it.</summary>
    /// <param name="term">The search term.</param>
    /// <param name="options">Search options.</param>
    /// <returns>The state of the search afterwards.</returns>
    internal SearchResultSummary FindNext(string term, SearchOptions options) => Find(term, options, next: true);

    /// <summary>Finds the previous match of <paramref name="term"/>, then scrolls to and selects it.</summary>
    /// <param name="term">The search term.</param>
    /// <param name="options">Search options.</param>
    /// <returns>The state of the search afterwards.</returns>
    internal SearchResultSummary FindPrevious(string term, SearchOptions options) => Find(term, options, next: false);

    /// <summary>Removes the highlights and forgets the search term.</summary>
    internal void ClearDecorations() => ClearDecorations(retainCachedSearchTerm: false);

    /// <inheritdoc />
    public void Dispose() => _lineCache.Dispose();

    private SearchResultSummary Find(string term, SearchOptions options, bool next)
    {
        _state.LastSearchOptions = options;
        if (_state.ShouldUpdateHighlighting(term, options))
        {
            HighlightAllMatches(term, options);
        }

        FindAndSelect(term, options, next);
        var summary = Tracker.Summary();
        if (options.Decorations)
        {
            ResultsChanged?.Invoke(summary);
        }

        _state.CachedSearchTerm = term;
        _lineCache.EndSession();
        return summary;
    }

    private void ClearDecorations(bool retainCachedSearchTerm)
    {
        Tracker.ClearSelectedDecoration();
        Tracker.ClearResults();
        if (!retainCachedSearchTerm)
        {
            _state.ClearCachedTerm();
        }
    }

    private void HighlightAllMatches(string term, SearchOptions options)
    {
        if (!SearchState.IsValidSearchTerm(term))
        {
            ClearDecorations();
            return;
        }

        // New search: clear out the old decorations.
        ClearDecorations(retainCachedSearchTerm: true);

        var results = new List<SearchResult>();
        SearchResult? previous = null;
        var result = _engine.Find(term, 0, 0, options);
        while (result is not null && (previous?.Row != result.Row || previous.Col != result.Col))
        {
            if (results.Count >= HighlightLimit)
            {
                break;
            }

            previous = result;
            results.Add(previous);
            var wraps = previous.Col + previous.Term.Length >= _core.Cols;
            result = _engine.Find(term, wraps ? previous.Row + 1 : previous.Row, wraps ? 0 : previous.Col + 1, options);
        }

        Tracker.UpdateResults(results, HighlightLimit);
    }

    private void FindAndSelect(string term, SearchOptions options, bool next)
    {
        if (!SearchState.IsValidSearchTerm(term))
        {
            _selection.ClearSelection();
            ClearDecorations();
            return;
        }

        var result = next
            ? _engine.FindNextWithSelection(term, options, _state.CachedSearchTerm)
            : _engine.FindPreviousWithSelection(term, options, _state.CachedSearchTerm);
        SelectResult(result, options.Decorations);
    }

    private void SelectResult(SearchResult? result, bool decorations)
    {
        Tracker.ClearSelectedDecoration();
        if (result is null)
        {
            _selection.ClearSelection();
            return;
        }

        _selection.SetSelection(result.Col, result.Row, result.Size);
        if (decorations)
        {
            Tracker.SetSelected(result);
        }

        // Scroll only when the match is outside the viewport, and put it in the middle.
        var viewportY = _core.Buffers.Active.YDisp;
        if (result.Row >= viewportY + _core.Rows || result.Row < viewportY)
        {
            _core.ScrollLines(result.Row - viewportY - (_core.Rows / 2));
        }
    }
}
