using LoomTerminal.Core.Buffer;
using LoomTerminal.Presentation.Search;

namespace LoomTerminal.Presentation.Rendering;

/// <summary>What the UI decides and the frame builder needs; later cards add the selection and the search highlights.</summary>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
internal sealed record ViewState
{
    /// <summary>The default state: focused, cursor in the visible phase of its blink.</summary>
    internal static ViewState Default { get; } = new();

    /// <summary>Whether the terminal has the focus (an unfocused terminal draws the inactive cursor and selection colors).</summary>
    internal bool IsFocused { get; init; } = true;

    /// <summary>Start of the selection, in buffer coordinates.</summary>
    internal (int X, int Y)? SelectionStart { get; init; }

    /// <summary>End of the selection, in buffer coordinates.</summary>
    internal (int X, int Y)? SelectionEnd { get; init; }

    /// <summary>Whether the selection is a rectangle of columns.</summary>
    internal bool SelectionColumnMode { get; init; }

    /// <summary>Cells of the link under the pointer (1-based, end inclusive, buffer rows); drawn as a link.</summary>
    internal BufferRange? HoveredLink { get; init; }

    /// <summary>The cells of the search matches, by buffer row.</summary>
    internal SearchHighlightMap? SearchHighlights { get; init; }

    /// <summary>Whether a blinking cursor is in its visible phase.</summary>
    internal bool CursorBlinkVisible { get; init; } = true;
}
