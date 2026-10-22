namespace LoomTerminal.Presentation.Rendering;

/// <summary>
/// Size and decoration geometry of one terminal cell, in DIPs. <paramref name="Width"/> and <paramref name="Height"/>
/// already include the line height and letter spacing and are snapped to device pixels by the backend.
/// </summary>
/// <param name="Width">Cell width.</param>
/// <param name="Height">Cell height.</param>
/// <param name="Baseline">Distance from the top of the cell to the text baseline.</param>
/// <param name="UnderlinePosition">Distance from the top of the cell to the top of the underline.</param>
/// <param name="UnderlineThickness">Underline thickness.</param>
/// <param name="StrikethroughPosition">Distance from the top of the cell to the top of the strikethrough line.</param>
/// <param name="StrikethroughThickness">Strikethrough thickness.</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public readonly record struct CellMetrics(
    double Width,
    double Height,
    double Baseline,
    double UnderlinePosition,
    double UnderlineThickness,
    double StrikethroughPosition,
    double StrikethroughThickness);
