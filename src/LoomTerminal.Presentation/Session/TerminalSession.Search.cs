using LoomTerminal.Presentation.Search;

namespace LoomTerminal.Presentation.Session;

public sealed partial class TerminalSession
{
    private SearchAddon? _searchAddon;

    /// <summary>Raised on the parser thread after each find that highlights matches (<see cref="SearchOptions.Decorations"/>).</summary>
    public event Action<SearchResultSummary>? SearchResultsChanged;

    /// <summary>
    /// Finds the next match of <paramref name="term"/>, selects it (so it can be copied) and scrolls to it when it is
    /// out of the viewport. With <see cref="SearchOptions.Decorations"/> all the matches are highlighted, the active one
    /// stronger. The matches are not refreshed when output arrives: search again.
    /// </summary>
    /// <param name="term">The text to find.</param>
    /// <param name="options">Search options.</param>
    /// <returns>The index of the active match (<c>-1</c> if none) and the number of matches, capped at 1000.</returns>
    public Task<SearchResultSummary> FindNextAsync(string term, SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(options);
        return RunOnParserThreadAsync(_ => Search.FindNext(term, options));
    }

    /// <summary>Finds the previous match of <paramref name="term"/>; see <see cref="FindNextAsync"/>.</summary>
    /// <param name="term">The text to find.</param>
    /// <param name="options">Search options.</param>
    /// <returns>The index of the active match (<c>-1</c> if none) and the number of matches, capped at 1000.</returns>
    public Task<SearchResultSummary> FindPreviousAsync(string term, SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(options);
        return RunOnParserThreadAsync(_ => Search.FindPrevious(term, options));
    }

    /// <summary>Removes the match highlights and forgets the search; the selection is left as it is.</summary>
    public void ClearSearch() => Post(_ => Search.ClearDecorations());

    // Created on first use, on the parser thread.
    private SearchAddon Search
    {
        get
        {
            if (_searchAddon is null)
            {
                _searchAddon = new SearchAddon(_core, _selection);
                _searchAddon.ResultsChanged += summary => SearchResultsChanged?.Invoke(summary);
            }

            return _searchAddon;
        }
    }
}
