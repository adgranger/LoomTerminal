using LoomTerminal.Presentation.Rendering;
using SkiaSharp;

namespace LoomTerminal.Avalonia.Rendering;

/// <summary>Key of a cached text blob (same shape as the WinUI layout key). Colors are not part of it: they are set per draw.</summary>
/// <param name="Text">The text.</param>
/// <param name="Bold">Whether the text is bold.</param>
/// <param name="Italic">Whether the text is italic.</param>
/// <param name="Family">Resolved font family.</param>
/// <param name="Size">Font size in DIPs.</param>
/// <param name="CellWidth">Cell width in DIPs (the ASCII fast path positions glyphs on the grid).</param>
internal readonly record struct SkiaTextBlobKey(string Text, bool Bold, bool Italic, string Family, double Size, double CellWidth);

/// <summary>LRU cache of <see cref="SKTextBlob"/>, plus the typefaces they are built from; it owns and disposes both.</summary>
/// <remarks>Threading: locked, because the draw operation runs on the render thread. Call <see cref="Clear"/> on a font change.</remarks>
internal sealed class SkiaTextBlobCache
{
    private readonly object _lock = new();
    private readonly Dictionary<SkiaTextBlobKey, LinkedListNode<(SkiaTextBlobKey Key, SKTextBlob Blob)>> _map = new();
    private readonly LinkedList<(SkiaTextBlobKey Key, SKTextBlob Blob)> _lru = new();
    private readonly Dictionary<(string Family, bool Bold, bool Italic), SKTypeface> _typefaces = new();
    private readonly Dictionary<int, SKTypeface?> _fallbacks = new();

    /// <summary>Creates a cache.</summary>
    /// <param name="capacity">Maximum number of blobs kept.</param>
    public SkiaTextBlobCache(int capacity) => Capacity = Math.Max(1, capacity);

    /// <summary>Maximum number of blobs kept; the owner raises it when the grid grows.</summary>
    public int Capacity { get; set; }

    /// <summary>Gets the typeface of a family and style (owned by the cache).</summary>
    /// <param name="family">Family name.</param>
    /// <param name="font">The terminal font (weights).</param>
    /// <param name="bold">Bold.</param>
    /// <param name="italic">Italic.</param>
    /// <returns>The typeface.</returns>
    public SKTypeface GetTypeface(string family, TerminalFont font, bool bold, bool italic)
    {
        lock (_lock)
        {
            if (!_typefaces.TryGetValue((family, bold, italic), out SKTypeface? face))
            {
                face = SKTypeface.FromFamilyName(
                    family,
                    new SKFontStyle(bold ? font.BoldWeight : font.Weight, (int)SKFontStyleWidth.Normal, italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright));
                _typefaces[(family, bold, italic)] = face;
            }

            return face;
        }
    }

    /// <summary>Gets a system typeface that has the character, when <paramref name="primary"/> lacks it.</summary>
    /// <param name="primary">The typeface tried first.</param>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <returns>A typeface with the glyph (owned by the cache), or <paramref name="primary"/> when nothing better exists.</returns>
    public SKTypeface GetTypefaceFor(SKTypeface primary, int codePoint)
    {
        if (primary.ContainsGlyph(codePoint))
        {
            return primary;
        }

        lock (_lock)
        {
            if (!_fallbacks.TryGetValue(codePoint, out SKTypeface? fallback))
            {
                fallback = SKFontManager.Default.MatchCharacter(codePoint);
                _fallbacks[codePoint] = fallback;
            }

            return fallback ?? primary;
        }
    }

    /// <summary>Gets a blob, creating it with <paramref name="create"/> on a miss.</summary>
    /// <param name="key">The key.</param>
    /// <param name="create">Factory called on a miss.</param>
    /// <returns>The blob (owned by the cache, do not dispose it), or null when the text has no drawable glyph.</returns>
    public SKTextBlob? GetOrCreate(in SkiaTextBlobKey key, Func<SKTextBlob?> create)
    {
        lock (_lock)
        {
            if (_map.TryGetValue(key, out var node))
            {
                _lru.Remove(node);
                _lru.AddFirst(node);
                return node.Value.Blob;
            }

            SKTextBlob? blob = create();
            if (blob is null)
            {
                return null;
            }

            _map[key] = _lru.AddFirst((key, blob));
            while (_map.Count > Capacity)
            {
                var last = _lru.Last!;
                _lru.RemoveLast();
                _map.Remove(last.Value.Key);
                last.Value.Blob.Dispose();
            }

            return blob;
        }
    }

    /// <summary>Disposes every blob and typeface.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            foreach (var (_, blob) in _lru)
            {
                blob.Dispose();
            }

            _lru.Clear();
            _map.Clear();
            foreach (SKTypeface face in _typefaces.Values)
            {
                face.Dispose();
            }

            _typefaces.Clear();
            foreach (SKTypeface? face in _fallbacks.Values)
            {
                face?.Dispose();
            }

            _fallbacks.Clear();
        }
    }
}
