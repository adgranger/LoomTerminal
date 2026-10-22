// Ported from xtermjs/xterm.js: addons/addon-search/src/SearchLineCache.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Presentation.Search;

/// <summary>A cached line of text.</summary>
/// <param name="Text">The string representation of a line (as opposed to the buffer cell representation).</param>
/// <param name="Offsets">The offsets where each row starts when the entry describes a wrapped line.</param>
/// <remarks>Threading: parser thread only.</remarks>
internal readonly record struct LineCacheEntry(string Text, int[] Offsets);

/// <summary>
/// Memoizes <see cref="TranslateBufferLineToStringWithWrap"/>, which is expensive. xterm.js drops the cache after a
/// time-to-live; here the cache lives until <see cref="EndSession"/> (called at the end of each search session), and,
/// as in xterm.js, until a line feed, a cursor move or a resize.
/// </summary>
/// <remarks>Threading: parser thread only: it reads the emulator and listens to its events.</remarks>
internal sealed class SearchLineCache : IDisposable
{
    private readonly SearchBufferView _view;
    private LineCacheEntry?[]? _linesCache;
    private IDisposable? _subscription;

    /// <summary>Creates a cache over <paramref name="view"/>.</summary>
    /// <param name="view">The terminal.</param>
    internal SearchLineCache(SearchBufferView view)
    {
        _view = view;
    }

    /// <summary>Sets up the cache if it does not exist.</summary>
    internal void InitLinesCache()
    {
        if (_linesCache is null)
        {
            _linesCache = new LineCacheEntry?[_view.Length];
            _subscription = _view.SubscribeToChanges(DestroyLinesCache);
        }
    }

    /// <summary>Drops the cache: the search session is over.</summary>
    internal void EndSession() => DestroyLinesCache();

    /// <summary>Gets a cached row.</summary>
    /// <param name="row">Buffer row.</param>
    /// <returns>The entry, or <see langword="null"/> when the cache is not set up or the row is not cached.</returns>
    internal LineCacheEntry? GetLineFromCache(int row) =>
        _linesCache is { } cache && row >= 0 && row < cache.Length ? cache[row] : null;

    /// <summary>Caches a row; ignored when the cache is not set up.</summary>
    /// <param name="row">Buffer row.</param>
    /// <param name="entry">The entry.</param>
    internal void SetLineInCache(int row, LineCacheEntry entry)
    {
        if (_linesCache is { } cache && row >= 0 && row < cache.Length)
        {
            cache[row] = entry;
        }
    }

    /// <summary>
    /// Translates a buffer line to a string, including subsequent lines if they are wraps. Wide characters will count
    /// as two columns in the resulting string.
    /// </summary>
    /// <param name="lineIndex">The index of the line being translated.</param>
    /// <param name="trimRight">Whether to trim whitespace to the right.</param>
    /// <returns>The text and the offsets where each row starts.</returns>
    internal LineCacheEntry TranslateBufferLineToStringWithWrap(int lineIndex, bool trimRight)
    {
        var strings = new System.Text.StringBuilder();
        var lineOffsets = new List<int> { 0 };
        var line = _view.GetLine(lineIndex);
        while (line is not null)
        {
            var nextLine = _view.GetLine(lineIndex + 1);
            var lineWrapsToNext = nextLine is { IsWrapped: true };
            var text = line.TranslateToString(!lineWrapsToNext && trimRight);
            if (lineWrapsToNext && nextLine is not null)
            {
                var lastCellIsNull = line.GetCodePoint(line.Length - 1) == 0 && line.GetWidth(line.Length - 1) == 1;

                // a wide character wrapped to the next line
                if (lastCellIsNull && nextLine.GetWidth(0) == 2)
                {
                    text = text[..^1];
                }
            }

            strings.Append(text);
            if (!lineWrapsToNext)
            {
                break;
            }

            lineOffsets.Add(lineOffsets[^1] + text.Length);
            lineIndex++;
            line = nextLine;
        }

        return new LineCacheEntry(strings.ToString(), [.. lineOffsets]);
    }

    /// <summary>Drops the cache and stops listening.</summary>
    public void Dispose() => DestroyLinesCache();

    private void DestroyLinesCache()
    {
        _linesCache = null;
        _subscription?.Dispose();
        _subscription = null;
    }
}
