using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Presentation.Search;

/// <summary>The cells covered by search matches, by buffer row: what the frame builder paints.</summary>
/// <remarks>Threading: immutable once created; any thread may read it.</remarks>
internal sealed class SearchHighlightMap
{
    private readonly Dictionary<int, List<(int Start, int End, RunHighlight Highlight)>> _rows = [];

    private SearchHighlightMap()
    {
    }

    /// <summary>Maps <paramref name="results"/> and the <paramref name="active"/> match to cells.</summary>
    /// <param name="results">All the matches.</param>
    /// <param name="active">The active match, if any.</param>
    /// <param name="cols">Number of columns: a match wraps to the next row after the last one.</param>
    /// <returns>The map, or <see langword="null"/> when nothing is highlighted.</returns>
    internal static SearchHighlightMap? Create(IReadOnlyList<SearchResult> results, SearchResult? active, int cols)
    {
        if (results.Count == 0 && active is null)
        {
            return null;
        }

        var map = new SearchHighlightMap();
        foreach (var result in results)
        {
            map.Add(result, RunHighlight.SearchMatch, cols);
        }

        if (active is not null)
        {
            map.Add(active, RunHighlight.ActiveSearchMatch, cols);
        }

        return map;
    }

    /// <summary>Gets the highlight of a cell.</summary>
    /// <param name="col">Column.</param>
    /// <param name="row">Buffer row.</param>
    /// <returns>The highlight; the active match wins over a plain one.</returns>
    internal RunHighlight At(int col, int row)
    {
        var highlight = RunHighlight.None;
        if (_rows.TryGetValue(row, out var segments))
        {
            foreach (var (start, end, kind) in segments)
            {
                if (col >= start && col < end && kind > highlight)
                {
                    highlight = kind;
                }
            }
        }

        return highlight;
    }

    private void Add(SearchResult result, RunHighlight kind, int cols)
    {
        var row = result.Row;
        var col = result.Col;
        var remaining = result.Size;
        while (remaining > 0 && cols > 0)
        {
            var length = Math.Min(remaining, cols - col);
            if (!_rows.TryGetValue(row, out var segments))
            {
                _rows[row] = segments = [];
            }

            segments.Add((col, col + length, kind));
            remaining -= length;
            row++;
            col = 0;
        }
    }
}
