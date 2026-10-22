namespace LoomTerminal.Presentation.Rendering;

/// <summary>
/// What a UI backend draws on. Coordinates are in DIPs, origin at the top-left of the terminal area; colors are
/// <c>0xRRGGBBAA</c>. Backends cache text layouts themselves, keyed by <see cref="RenderRow.ContentHash"/>.
/// </summary>
/// <remarks>Threading: called from the UI thread only.</remarks>
public interface IRenderSurface
{
    /// <summary>Fills the whole surface.</summary>
    /// <param name="rgba">Color.</param>
    void Clear(uint rgba);

    /// <summary>Fills a rectangle.</summary>
    /// <param name="x">Left.</param>
    /// <param name="y">Top.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="rgba">Color.</param>
    void FillRectangle(double x, double y, double width, double height, uint rgba);

    /// <summary>Draws text laid out to span exactly <paramref name="cellCount"/> cells.</summary>
    /// <param name="row">Row of the first cell.</param>
    /// <param name="column">Column of the first cell.</param>
    /// <param name="text">The text.</param>
    /// <param name="cellCount">Number of cells to span; a wide character counts 2.</param>
    /// <param name="style">How to draw it.</param>
    /// <param name="metrics">The cell metrics.</param>
    void DrawTextRun(int row, int column, string text, int cellCount, in TextRunStyle style, in CellMetrics metrics);

    /// <summary>Draws an underline.</summary>
    /// <param name="x">Left.</param>
    /// <param name="y">Top of the underline.</param>
    /// <param name="width">Width.</param>
    /// <param name="style">Underline style.</param>
    /// <param name="rgba">Color.</param>
    /// <param name="metrics">The cell metrics.</param>
    void DrawUnderline(double x, double y, double width, TerminalUnderlineStyle style, uint rgba, in CellMetrics metrics);

    /// <summary>Draws a straight line.</summary>
    /// <param name="x1">Start X.</param>
    /// <param name="y1">Start Y.</param>
    /// <param name="x2">End X.</param>
    /// <param name="y2">End Y.</param>
    /// <param name="thickness">Line thickness.</param>
    /// <param name="rgba">Color.</param>
    void DrawLine(double x1, double y1, double x2, double y2, double thickness, uint rgba);

    /// <summary>Draws a rectangle outline (the outline cursor).</summary>
    /// <param name="x">Left.</param>
    /// <param name="y">Top.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="thickness">Outline thickness.</param>
    /// <param name="rgba">Color.</param>
    void DrawRectangle(double x, double y, double width, double height, double thickness, uint rgba);

    /// <summary>Draws a box-drawing, block, shade or powerline character as a vector shape filling its cell.</summary>
    /// <param name="path">The shape, relative to the cell (see <see cref="CustomGlyphPath"/>).</param>
    /// <param name="x">Left of the cell.</param>
    /// <param name="y">Top of the cell.</param>
    /// <param name="metrics">The cell metrics.</param>
    /// <param name="rgba">Color.</param>
    void DrawCustomGlyph(CustomGlyphPath path, double x, double y, in CellMetrics metrics, uint rgba);
}
