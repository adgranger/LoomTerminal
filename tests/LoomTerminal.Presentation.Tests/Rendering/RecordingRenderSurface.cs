using System.Globalization;
using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Presentation.Tests.Rendering;

/// <summary>Records each draw call as one line of text.</summary>
internal sealed class RecordingRenderSurface : IRenderSurface
{
    private readonly List<string> _calls = [];

    public IReadOnlyList<string> Calls => _calls;

    public string Text => string.Join('\n', _calls);

    public void Clear(uint rgba) => _calls.Add($"Clear {Color(rgba)}");

    public void FillRectangle(double x, double y, double width, double height, uint rgba) =>
        _calls.Add($"FillRect {N(x)},{N(y)} {N(width)}x{N(height)} {Color(rgba)}");

    public void DrawTextRun(int row, int column, string text, int cellCount, in TextRunStyle style, in CellMetrics metrics)
    {
        var flags = (style.Bold ? "b" : "") + (style.Italic ? "i" : "") + (style.Dim ? "d" : "");
        _calls.Add($"Text {row},{column} \"{text}\" cells={cellCount} fg={Color(style.Foreground)}" +
                   (flags.Length > 0 ? $" {flags}" : ""));
    }

    public void DrawUnderline(double x, double y, double width, TerminalUnderlineStyle style, uint rgba, in CellMetrics metrics) =>
        _calls.Add($"Underline {style} {N(x)},{N(y)} w={N(width)} {Color(rgba)}");

    public void DrawLine(double x1, double y1, double x2, double y2, double thickness, uint rgba) =>
        _calls.Add($"Line {N(x1)},{N(y1)} {N(x2)},{N(y2)} t={N(thickness)} {Color(rgba)}");

    public void DrawRectangle(double x, double y, double width, double height, double thickness, uint rgba) =>
        _calls.Add($"Rect {N(x)},{N(y)} {N(width)}x{N(height)} t={N(thickness)} {Color(rgba)}");

    public void DrawCustomGlyph(CustomGlyphPath path, double x, double y, in CellMetrics metrics, uint rgba) =>
        _calls.Add($"Glyph {N(x)},{N(y)} {Color(rgba)} commands={path.Commands.Count}");

    private static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Color(uint rgba) => "#" + rgba.ToString("x8", CultureInfo.InvariantCulture);
}
