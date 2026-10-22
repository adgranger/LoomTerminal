namespace LoomTerminal.Presentation.Rendering;

/// <summary>
/// The drawing algorithm shared by all UI backends: turns a <see cref="RenderFrame"/> into calls on an
/// <see cref="IRenderSurface"/>. The whole viewport is redrawn every frame; backends keep it cheap with their own
/// layout cache.
/// </summary>
/// <remarks>Threading: stateless; call it from the UI thread, like the surface.</remarks>
public static class FrameRenderer
{
    // shortcut: the bar cursor is 1 DIP wide because the frame carries no cursorWidth option, add it to RenderCursor when exposed.
    private const double CursorBarWidth = 1;

    /// <summary>
    /// Draws a frame: clear, run backgrounds (selection and search highlights are already blended into them), text,
    /// decorations, then the cursor.
    /// </summary>
    /// <param name="frame">The frame to draw.</param>
    /// <param name="metrics">The cell metrics.</param>
    /// <param name="surface">Where to draw.</param>
    public static void Render(RenderFrame frame, in CellMetrics metrics, IRenderSurface surface)
    {
        surface.Clear(frame.Background);
        for (var row = 0; row < frame.Lines.Count; row++)
        {
            DrawBackgrounds(frame, row, metrics, surface);
        }

        for (var row = 0; row < frame.Lines.Count; row++)
        {
            DrawText(frame.Lines[row], row, metrics, surface);
            DrawDecorations(frame.Lines[row], row, metrics, surface);
        }

        if (frame.Cursor is { Visible: true } cursor)
        {
            DrawCursor(cursor, metrics, surface);
        }
    }

    private static void DrawBackgrounds(RenderFrame frame, int row, in CellMetrics metrics, IRenderSurface surface)
    {
        var runs = frame.Lines[row].Runs;
        var i = 0;
        while (i < runs.Count)
        {
            var first = runs[i];
            var background = first.Style.Background;
            var end = first.Column + first.CellCount;
            i++;
            while (i < runs.Count && runs[i].Column == end && runs[i].Style.Background == background)
            {
                end += runs[i].CellCount;
                i++;
            }

            if (background != frame.Background)
            {
                surface.FillRectangle(first.Column * metrics.Width, row * metrics.Height, (end - first.Column) * metrics.Width, metrics.Height, background);
            }
        }
    }

    private static void DrawText(RenderRow line, int row, in CellMetrics metrics, IRenderSurface surface)
    {
        foreach (var run in line.Runs)
        {
            if (!string.IsNullOrWhiteSpace(run.Text))
            {
                surface.DrawTextRun(row, run.Column, run.Text, run.CellCount, run.Style, metrics);
            }
        }
    }

    private static void DrawDecorations(RenderRow line, int row, in CellMetrics metrics, IRenderSurface surface)
    {
        var top = row * metrics.Height;
        var runs = line.Runs;

        // Adjacent runs with the same underline are drawn as one so curly, dotted and dashed lines stay continuous.
        var i = 0;
        while (i < runs.Count)
        {
            var style = runs[i].Style;
            if (style.Underline == TerminalUnderlineStyle.None)
            {
                i++;
                continue;
            }

            var color = style.UnderlineColor ?? style.Foreground;
            var start = runs[i].Column;
            var end = start + runs[i].CellCount;
            i++;
            while (i < runs.Count && runs[i].Column == end && runs[i].Style.Underline == style.Underline
                   && (runs[i].Style.UnderlineColor ?? runs[i].Style.Foreground) == color)
            {
                end += runs[i].CellCount;
                i++;
            }

            surface.DrawUnderline(start * metrics.Width, top + metrics.UnderlinePosition, (end - start) * metrics.Width, style.Underline, color, metrics);
        }

        foreach (var run in runs)
        {
            var x1 = run.Column * metrics.Width;
            var x2 = x1 + (run.CellCount * metrics.Width);
            if (run.Style.Strikethrough)
            {
                var y = top + metrics.StrikethroughPosition + (metrics.StrikethroughThickness / 2);
                surface.DrawLine(x1, y, x2, y, metrics.StrikethroughThickness, run.Style.Foreground);
            }

            if (run.Style.Overline)
            {
                var y = top + (metrics.UnderlineThickness / 2);
                surface.DrawLine(x1, y, x2, y, metrics.UnderlineThickness, run.Style.Foreground);
            }
        }
    }

    private static void DrawCursor(RenderCursor cursor, in CellMetrics metrics, IRenderSurface surface)
    {
        var x = cursor.Column * metrics.Width;
        var y = cursor.Row * metrics.Height;
        switch (cursor.Shape)
        {
            case CursorShape.Block:
                surface.FillRectangle(x, y, metrics.Width, metrics.Height, cursor.Color);
                if (!string.IsNullOrWhiteSpace(cursor.CellText))
                {
                    var style = new TextRunStyle(cursor.AccentColor, cursor.Color, false, false, false, TerminalUnderlineStyle.None, null, false, false, false, RunHighlight.None);
                    surface.DrawTextRun(cursor.Row, cursor.Column, cursor.CellText, 1, style, metrics);
                }

                break;
            case CursorShape.Bar:
                surface.FillRectangle(x, y, CursorBarWidth, metrics.Height, cursor.Color);
                break;
            case CursorShape.Underline:
                var thickness = metrics.UnderlineThickness * 2;
                surface.FillRectangle(x, y + metrics.Height - thickness, metrics.Width, thickness, cursor.Color);
                break;
            default:
                surface.DrawRectangle(x, y, metrics.Width, metrics.Height, metrics.UnderlineThickness, cursor.Color);
                break;
        }
    }
}
