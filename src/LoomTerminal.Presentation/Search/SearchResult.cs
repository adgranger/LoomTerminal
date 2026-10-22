namespace LoomTerminal.Presentation.Search;

/// <summary>A match found by a search.</summary>
/// <param name="Term">The text that matched (differs from the search term for a regular expression or another case).</param>
/// <param name="Col">Column of the first cell of the match.</param>
/// <param name="Row">Buffer row of the first cell of the match.</param>
/// <param name="Size">Number of cells of the match, wrapped rows included.</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record SearchResult(string Term, int Col, int Row, int Size);
