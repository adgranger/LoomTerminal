// Ported from xtermjs/xterm.js: addons/addon-search/src/SearchState.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Presentation.Search;

/// <summary>Manages search state including cached search terms, options tracking, and validation.</summary>
/// <remarks>Threading: parser thread only.</remarks>
internal sealed class SearchState
{
    /// <summary>The currently cached search term.</summary>
    internal string? CachedSearchTerm { get; set; }

    /// <summary>The last search options used.</summary>
    internal SearchOptions? LastSearchOptions { get; set; }

    /// <summary>Validates a search term to ensure it's not empty or invalid.</summary>
    /// <param name="term">The search term to validate.</param>
    /// <returns><see langword="true"/> if the term is valid for searching.</returns>
    internal static bool IsValidSearchTerm(string? term) => !string.IsNullOrEmpty(term);

    /// <summary>Determines if search options have changed compared to the last search.</summary>
    /// <param name="newOptions">The new search options to compare.</param>
    /// <returns><see langword="true"/> if the options have changed.</returns>
    internal bool DidOptionsChange(SearchOptions? newOptions)
    {
        if (LastSearchOptions is null)
        {
            return true;
        }

        if (newOptions is null)
        {
            return false;
        }

        return LastSearchOptions.CaseSensitive != newOptions.CaseSensitive
            || LastSearchOptions.Regex != newOptions.Regex
            || LastSearchOptions.WholeWord != newOptions.WholeWord;
    }

    /// <summary>Determines if a new search should trigger highlighting updates.</summary>
    /// <param name="term">The search term.</param>
    /// <param name="options">The search options.</param>
    /// <returns><see langword="true"/> if highlighting should be updated.</returns>
    internal bool ShouldUpdateHighlighting(string term, SearchOptions? options)
    {
        if (options is not { Decorations: true })
        {
            return false;
        }

        return CachedSearchTerm is null || term != CachedSearchTerm || DidOptionsChange(options);
    }

    /// <summary>Clears the cached search term.</summary>
    internal void ClearCachedTerm() => CachedSearchTerm = null;

    /// <summary>Resets all state.</summary>
    internal void Reset()
    {
        CachedSearchTerm = null;
        LastSearchOptions = null;
    }
}
