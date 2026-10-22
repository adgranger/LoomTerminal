namespace LoomTerminal.Presentation.Rendering;

/// <summary>Kind of a <see cref="CustomGlyphCommand"/>.</summary>
public enum CustomGlyphCommandKind
{
    /// <summary>Starts a new sub-path at (<c>A</c>, <c>B</c>).</summary>
    MoveTo,

    /// <summary>Draws a line to (<c>A</c>, <c>B</c>).</summary>
    LineTo,

    /// <summary>Draws a cubic Bezier curve with control points (<c>A</c>, <c>B</c>), (<c>C</c>, <c>D</c>) and end point (<c>E</c>, <c>F</c>).</summary>
    CurveTo,

    /// <summary>Adds the closed rectangle at (<c>A</c>, <c>B</c>) of size <c>C</c> x <c>D</c>.</summary>
    Rect,

    /// <summary>Fills everything added since the last fill or stroke with the non-zero winding rule; <c>A</c> is the opacity factor (1 = opaque).</summary>
    Fill,

    /// <summary>Strokes everything added since the last fill or stroke; <c>A</c> is the line width in DIPs.</summary>
    Stroke,
}

/// <summary>One drawing instruction of a <see cref="CustomGlyphPath"/>; unused arguments are 0.</summary>
/// <param name="Kind">The instruction.</param>
/// <param name="A">First argument.</param>
/// <param name="B">Second argument.</param>
/// <param name="C">Third argument.</param>
/// <param name="D">Fourth argument.</param>
/// <param name="E">Fifth argument.</param>
/// <param name="F">Sixth argument.</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public readonly record struct CustomGlyphCommand(CustomGlyphCommandKind Kind, double A = 0, double B = 0, double C = 0, double D = 0, double E = 0, double F = 0);

/// <summary>
/// The vector drawing of a box-drawing, block, shade or powerline character, ready for a backend: coordinates are in
/// DIPs relative to the top-left corner of the cell and already scaled to the cell size.
/// </summary>
/// <param name="Commands">The instructions, in drawing order.</param>
/// <param name="ClipToCell">Whether the drawing must be clipped to the cell (powerline shapes overshoot to hide line ends).</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record CustomGlyphPath(IReadOnlyList<CustomGlyphCommand> Commands, bool ClipToCell);
