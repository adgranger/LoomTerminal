using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Presentation.Tests.Rendering;

/// <summary>Fixed 8×16 cells, whatever the font.</summary>
internal sealed class FakeTextMeasurer : ITextMeasurer
{
    public CellMetrics Measure(TerminalFont font, double rasterizationScale) => new(8, 16, 12, 14, 1, 8, 1);
}
