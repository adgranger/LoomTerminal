namespace LoomTerminal.Presentation.Search;

/// <summary>Where a search stands: <c>ISearchResultChangeEvent</c> of addon-search.</summary>
/// <param name="ResultIndex">Index of the active match among the results, or <c>-1</c> when there is none.</param>
/// <param name="ResultCount">Number of matches found, capped at 1000.</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record SearchResultSummary(int ResultIndex, int ResultCount);
