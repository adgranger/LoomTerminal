namespace LoomTerminal.Presentation.Rendering;

/// <summary>What the UI decides and the frame builder needs; later cards add the selection and the search highlights.</summary>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
internal sealed record ViewState
{
    /// <summary>The default state: focused, cursor in the visible phase of its blink.</summary>
    internal static ViewState Default { get; } = new();

    /// <summary>Whether the terminal has the focus (an unfocused terminal draws the inactive cursor and selection colors).</summary>
    internal bool IsFocused { get; init; } = true;

    /// <summary>Whether a blinking cursor is in its visible phase.</summary>
    internal bool CursorBlinkVisible { get; init; } = true;
}
