using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Presentation.Tests.Rendering;

public class RecordingRenderSurfaceTests
{
    [Fact]
    public void Each_call_is_recorded_as_one_deterministic_line()
    {
        var surface = new RecordingRenderSurface();
        var metrics = new FakeTextMeasurer().Measure(new TerminalFont(["Test"], 12), 1);

        surface.Clear(0x1e1e1eff);
        surface.FillRectangle(0, 0, 7.2, 16, 0x1e1e1eff);
        surface.DrawUnderline(0, 14, 16, TerminalUnderlineStyle.Curly, 0xff0000ff, metrics);
        surface.DrawLine(0, 1, 2, 3, 1, 0x000000ff);
        surface.DrawRectangle(0, 0, 8, 16, 1, 0xffffffff);

        Assert.Equal(
            "Clear #1e1e1eff\n" +
            "FillRect 0,0 7.2x16 #1e1e1eff\n" +
            "Underline Curly 0,14 w=16 #ff0000ff\n" +
            "Line 0,1 2,3 t=1 #000000ff\n" +
            "Rect 0,0 8x16 t=1 #ffffffff",
            surface.Text);
    }
}
