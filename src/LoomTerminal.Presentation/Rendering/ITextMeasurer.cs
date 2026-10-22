namespace LoomTerminal.Presentation.Rendering;

/// <summary>Measures the cell of a font. Implemented by a UI backend, which also owns font fallback.</summary>
/// <remarks>Threading: called from the UI thread only.</remarks>
public interface ITextMeasurer
{
    /// <summary>Measures the cell of <paramref name="font"/>.</summary>
    /// <param name="font">The font.</param>
    /// <param name="rasterizationScale">Device pixels per DIP; the result is snapped to device pixels.</param>
    /// <returns>The cell metrics, in DIPs.</returns>
    CellMetrics Measure(TerminalFont font, double rasterizationScale);
}
