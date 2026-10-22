namespace LoomTerminal.Presentation.Rendering;

/// <summary>Underline of a text run.</summary>
public enum TerminalUnderlineStyle
{
    /// <summary>No underline.</summary>
    None,

    /// <summary>A single line.</summary>
    SingleLine,

    /// <summary>A double line.</summary>
    DoubleLine,

    /// <summary>A curly line.</summary>
    Curly,

    /// <summary>A dotted line.</summary>
    Dotted,

    /// <summary>A dashed line.</summary>
    Dashed,
}

/// <summary>What a search overlay marks a run as.</summary>
public enum RunHighlight
{
    /// <summary>Not highlighted.</summary>
    None,

    /// <summary>Part of a search match.</summary>
    SearchMatch,

    /// <summary>Part of the active search match.</summary>
    ActiveSearchMatch,
}

/// <summary>Shape of the cursor to draw.</summary>
public enum CursorShape
{
    /// <summary>A filled block.</summary>
    Block,

    /// <summary>An underline.</summary>
    Underline,

    /// <summary>A vertical bar.</summary>
    Bar,

    /// <summary>The outline of a block (the cursor of an unfocused terminal).</summary>
    Outline,
}

/// <summary>
/// How a run of cells is drawn. Colors are <c>0xRRGGBBAA</c> and final: inverse, dim colors, bold as bright, selection
/// and the minimum contrast ratio are already applied.
/// </summary>
/// <param name="Foreground">Text color.</param>
/// <param name="Background">Background color (opaque).</param>
/// <param name="Bold">Whether the text is bold.</param>
/// <param name="Italic">Whether the text is italic.</param>
/// <param name="Dim">Whether the text is dim (its foreground is already translucent).</param>
/// <param name="Underline">The underline style.</param>
/// <param name="UnderlineColor">Underline color; <see langword="null"/> when it has the text color.</param>
/// <param name="Strikethrough">Whether the text is struck through.</param>
/// <param name="Overline">Whether the text has an overline.</param>
/// <param name="IsLink">Whether the text is a link (OSC 8, or a detected link).</param>
/// <param name="Highlight">Search highlight.</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public readonly record struct TextRunStyle(
    uint Foreground,
    uint Background,
    bool Bold,
    bool Italic,
    bool Dim,
    TerminalUnderlineStyle Underline,
    uint? UnderlineColor,
    bool Strikethrough,
    bool Overline,
    bool IsLink,
    RunHighlight Highlight);

/// <summary>Consecutive cells of a row drawn with the same style.</summary>
/// <param name="Column">First column of the run.</param>
/// <param name="CellCount">Number of cells; a wide character counts 2.</param>
/// <param name="Text">The characters of the cells concatenated; a wide character counts 1 char for 2 cells.</param>
/// <param name="Style">How to draw it.</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record RenderRun(int Column, int CellCount, string Text, TextRunStyle Style);

/// <summary>One visible row.</summary>
/// <param name="Runs">The runs, left to right; empty trailing cells with the default background produce no run.</param>
/// <param name="IsWrapped">Whether the row continues the previous one.</param>
/// <param name="ContentHash">Hash of the runs: equal rows have equal hashes (within one process), so backends can cache text layouts.</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record RenderRow(IReadOnlyList<RenderRun> Runs, bool IsWrapped, int ContentHash);

/// <summary>The cursor to draw.</summary>
/// <param name="Column">Column of the cursor.</param>
/// <param name="Row">Row of the cursor, relative to the viewport.</param>
/// <param name="Shape">Shape to draw.</param>
/// <param name="Visible">Whether to draw it now (after blinking and DECTCEM).</param>
/// <param name="Color">Cursor color, <c>0xRRGGBBAA</c>.</param>
/// <param name="AccentColor">Color of the character under a block cursor, <c>0xRRGGBBAA</c>.</param>
/// <param name="CellText">The characters of the cell under the cursor (empty for an empty cell).</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record RenderCursor(int Column, int Row, CursorShape Shape, bool Visible, uint Color, uint AccentColor, string CellText);

/// <summary>The selection, in viewport coordinates.</summary>
/// <param name="StartColumn">Column of the start (inclusive).</param>
/// <param name="StartRow">Row of the start; negative when the selection starts above the viewport.</param>
/// <param name="EndColumn">Column of the end (exclusive).</param>
/// <param name="EndRow">Row of the end; beyond the last row when the selection ends below the viewport.</param>
/// <param name="IsColumnMode">Whether the selection is a rectangle of columns.</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record SelectionRange(int StartColumn, int StartRow, int EndColumn, int EndRow, bool IsColumnMode);

/// <summary>Immutable snapshot of what a backend draws.</summary>
/// <param name="Columns">Number of columns.</param>
/// <param name="Rows">Number of visible rows.</param>
/// <param name="Lines">The visible rows (viewport only).</param>
/// <param name="Cursor">The cursor; <see langword="null"/> when it is outside the viewport.</param>
/// <param name="Background">Theme background color, <c>0xRRGGBBAA</c>.</param>
/// <param name="ViewportY">Index of the first visible line in the buffer (ydisp).</param>
/// <param name="ScrollbackLength">Number of lines above the screen (ybase).</param>
/// <param name="IsAlternateBuffer">Whether the alternate screen is active.</param>
/// <param name="Version">Incremented for each frame.</param>
/// <param name="DirtyRows">Rows that changed since the previous frame; all rows after a scroll or a resize.</param>
/// <param name="Selection">The selection; <see langword="null"/> when there is none or it is outside the viewport.</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record RenderFrame(
    int Columns,
    int Rows,
    IReadOnlyList<RenderRow> Lines,
    RenderCursor? Cursor,
    uint Background,
    int ViewportY,
    int ScrollbackLength,
    bool IsAlternateBuffer,
    ulong Version,
    IReadOnlySet<int> DirtyRows,
    SelectionRange? Selection);
