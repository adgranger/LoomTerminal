namespace LoomTerminal.Presentation.Search;

/// <summary>Options of a search (<c>ISearchOptions</c> of addon-search).</summary>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record SearchOptions
{
    /// <summary>Whether the search is a regular expression.</summary>
    public bool Regex { get; init; }

    /// <summary>Whether the search is case sensitive.</summary>
    public bool CaseSensitive { get; init; }

    /// <summary>Whether a match must not touch a word character.</summary>
    public bool WholeWord { get; init; }

    /// <summary>Whether the matches are highlighted.</summary>
    public bool Decorations { get; init; }
}
