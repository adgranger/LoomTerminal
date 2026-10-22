using LoomTerminal.Core;
using LoomTerminal.Core.Buffer;
using LoomTerminal.Presentation.Selection;

namespace LoomTerminal.Presentation.Search;

/// <summary>The selection, in buffer coordinates (0-based columns and rows).</summary>
/// <param name="Start">Start of the selection.</param>
/// <param name="End">End of the selection.</param>
/// <remarks>Threading: immutable; any thread.</remarks>
internal readonly record struct SearchSelectionPosition((int X, int Y) Start, (int X, int Y) End);

/// <summary>
/// What the search engine reads of the terminal: the xterm.js public <c>Terminal</c> API it was written against
/// (<c>buffer.active.getLine</c>, <c>cols</c>, <c>rows</c>, selection and the events that invalidate the line cache).
/// </summary>
/// <remarks>Threading: parser thread only: it reads the emulator.</remarks>
internal sealed class SearchBufferView
{
    private readonly TerminalCore _core;

    /// <summary>Creates a view over <paramref name="core"/>.</summary>
    /// <param name="core">The emulator.</param>
    /// <param name="selection">The selection logic; without it there is never a selection.</param>
    internal SearchBufferView(TerminalCore core, SelectionService? selection = null)
    {
        _core = core;
        GetSelectionPosition = () => selection?.SelectionStart is { } start && selection.SelectionEnd is { } end
            ? new SearchSelectionPosition(start, end)
            : null;
        ClearSelection = () => selection?.ClearSelection();
    }

    /// <summary>Number of columns.</summary>
    internal int Cols => _core.Cols;

    /// <summary>Number of rows of the viewport.</summary>
    internal int Rows => _core.Rows;

    /// <summary>Number of the scrollback rows of the active buffer (<c>baseY</c>).</summary>
    internal int BaseY => _core.Buffers.Active.YBase;

    /// <summary>Number of rows of the active buffer.</summary>
    internal int Length => _core.Buffers.Active.Lines.Length;

    /// <summary>Gets the current selection, if any (replaceable by tests, as the JS tests replace <c>getSelectionPosition</c>).</summary>
    internal Func<SearchSelectionPosition?> GetSelectionPosition { get; set; }

    /// <summary>Clears the selection.</summary>
    internal Action ClearSelection { get; set; }

    /// <summary>Gets a row of the active buffer.</summary>
    /// <param name="y">Buffer row.</param>
    /// <returns>The row, or <see langword="null"/> when out of range.</returns>
    internal IBufferLine? GetLine(int y) => y >= 0 && y < Length ? _core.Buffers.Active.Lines.Get(y) : null;

    /// <summary>Calls <paramref name="onChange"/> on line feed, cursor move and resize.</summary>
    /// <param name="onChange">The callback.</param>
    /// <returns>Dispose to stop listening.</returns>
    internal IDisposable SubscribeToChanges(Action onChange)
    {
        var subscriptions = new[]
        {
            _core.OnLineFeed.Subscribe(_ => onChange()),
            _core.OnCursorMove.Subscribe(_ => onChange()),
            _core.OnResize.Subscribe(_ => onChange()),
        };
        return new Unsubscriber(subscriptions);
    }

    private sealed class Unsubscriber(IDisposable[] subscriptions) : IDisposable
    {
        public void Dispose()
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}
