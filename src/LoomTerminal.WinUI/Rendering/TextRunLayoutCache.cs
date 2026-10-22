// Inspired by LoomEditor: src/LoomEditor.Rendering/LineLayoutCache.cs
using Microsoft.Graphics.Canvas.Text;

namespace LoomTerminal.WinUI.Rendering;

/// <summary>Key of a cached text layout. Colors are not part of it: they are set per draw.</summary>
/// <param name="Text">The laid out text.</param>
/// <param name="Bold">Whether the text is bold.</param>
/// <param name="Italic">Whether the text is italic.</param>
/// <param name="Family">Resolved font family.</param>
/// <param name="Size">Font size in DIPs.</param>
/// <param name="Scale">Rasterization scale.</param>
internal readonly record struct TextRunLayoutKey(string Text, bool Bold, bool Italic, string Family, double Size, double Scale);

/// <summary>LRU cache of <see cref="CanvasTextLayout"/>; it owns and disposes the layouts.</summary>
/// <remarks>Threading: UI thread only. Call <see cref="Clear"/> on a font, DPI or device change.</remarks>
internal sealed class TextRunLayoutCache
{
    private readonly Dictionary<TextRunLayoutKey, LinkedListNode<(TextRunLayoutKey Key, CanvasTextLayout Layout)>> _map = new();
    private readonly LinkedList<(TextRunLayoutKey Key, CanvasTextLayout Layout)> _lru = new();

    /// <summary>Creates a cache.</summary>
    /// <param name="capacity">Maximum number of layouts kept.</param>
    public TextRunLayoutCache(int capacity) => Capacity = Math.Max(1, capacity);

    /// <summary>Maximum number of layouts kept; the owner raises it when the grid grows.</summary>
    public int Capacity { get; set; }

    /// <summary>Gets a layout, creating it with <paramref name="create"/> on a miss.</summary>
    /// <param name="key">The key.</param>
    /// <param name="create">Factory called on a miss.</param>
    /// <returns>The layout; owned by the cache, do not dispose it.</returns>
    public CanvasTextLayout GetOrCreate(in TextRunLayoutKey key, Func<CanvasTextLayout> create)
    {
        if (_map.TryGetValue(key, out var node))
        {
            _lru.Remove(node);
            _lru.AddFirst(node);
            return node.Value.Layout;
        }

        var layout = create();
        _map[key] = _lru.AddFirst((key, layout));
        while (_map.Count > Capacity)
        {
            var last = _lru.Last!;
            _lru.RemoveLast();
            _map.Remove(last.Value.Key);
            last.Value.Layout.Dispose();
        }

        return layout;
    }

    /// <summary>Disposes every layout.</summary>
    public void Clear()
    {
        foreach (var (_, layout) in _lru)
        {
            layout.Dispose();
        }

        _lru.Clear();
        _map.Clear();
    }
}
