using System.Globalization;
using System.Text;
using LoomTerminal.Core.Input;

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

    private static readonly UnicodeV11 Unicode = new();

    /// <summary>
    /// Draws a frame: clear, run backgrounds (selection and search highlights are already blended into them), text,
    /// decorations, then the cursor.
    /// </summary>
    /// <param name="frame">The frame to draw.</param>
    /// <param name="metrics">The cell metrics.</param>
    /// <param name="surface">Where to draw.</param>
    /// <param name="customGlyphs">Whether box-drawing, block, shade and powerline characters are drawn as vectors instead of font glyphs.</param>
    public static void Render(RenderFrame frame, in CellMetrics metrics, IRenderSurface surface, bool customGlyphs = true)
    {
        surface.Clear(frame.Background);
        for (var row = 0; row < frame.Lines.Count; row++)
        {
            DrawBackgrounds(frame, row, metrics, surface);
        }

        for (var row = 0; row < frame.Lines.Count; row++)
        {
            DrawText(frame.Lines[row], row, metrics, surface, customGlyphs);
            DrawDecorations(frame.Lines[row], row, metrics, surface);
        }

        if (frame.Cursor is { Visible: true } cursor)
        {
            DrawCursor(cursor, metrics, surface, customGlyphs);
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

    private static void DrawText(RenderRow line, int row, in CellMetrics metrics, IRenderSurface surface, bool customGlyphs)
    {
        foreach (var run in line.Runs)
        {
            if (string.IsNullOrWhiteSpace(run.Text))
            {
                continue;
            }

            if (customGlyphs && MayContainCustomGlyph(run.Text))
            {
                DrawTextAndGlyphs(run, row, metrics, surface);
            }
            else
            {
                surface.DrawTextRun(row, run.Column, run.Text, run.CellCount, run.Style, metrics);
            }
        }
    }

    // Box drawing and blocks (U+2500..U+259F), powerline (U+E0A0..U+E0BF) and the high surrogate of U+1FB70..U+1FB97.
    private static bool MayContainCustomGlyph(string text)
    {
        foreach (var c in text)
        {
            if (c is >= (char)0x2500 and <= (char)0x259F or >= (char)0xE0A0 and <= (char)0xE0BF or (char)0xD83E)
            {
                return true;
            }
        }

        return false;
    }

    // Splits the run around the custom glyph cells: the text between them stays one DrawTextRun each.
    private static void DrawTextAndGlyphs(RenderRun run, int row, in CellMetrics metrics, IRenderSurface surface)
    {
        var pending = new StringBuilder();
        var pendingColumn = run.Column;
        var pendingCells = 0;
        var column = run.Column;
        var graphemes = StringInfo.GetTextElementEnumerator(run.Text);
        while (graphemes.MoveNext())
        {
            var grapheme = (string)graphemes.Current;
            var path = GlyphOf(grapheme, metrics);
            if (path is not null)
            {
                FlushText(run, row, pending, pendingColumn, pendingCells, metrics, surface);
                surface.DrawCustomGlyph(path, column * metrics.Width, row * metrics.Height, metrics, run.Style.Foreground);
                column++;
                pendingColumn = column;
                pendingCells = 0;
                continue;
            }

            var width = Math.Max(1, Unicode.Wcwidth(char.ConvertToUtf32(grapheme, 0)));
            pending.Append(grapheme);
            pendingCells += width;
            column += width;
        }

        FlushText(run, row, pending, pendingColumn, pendingCells, metrics, surface);
    }

    private static void FlushText(RenderRun run, int row, StringBuilder pending, int column, int cells, in CellMetrics metrics, IRenderSurface surface)
    {
        if (pending.Length > 0 && !string.IsNullOrWhiteSpace(pending.ToString()))
        {
            surface.DrawTextRun(row, column, pending.ToString(), cells, run.Style, metrics);
        }

        pending.Clear();
    }

    // A grapheme made of exactly one character that has a vector definition.
    private static CustomGlyphPath? GlyphOf(string grapheme, in CellMetrics metrics)
    {
        if (grapheme.Length is 0 or > 2)
        {
            return null;
        }

        var codePoint = char.ConvertToUtf32(grapheme, 0);
        return grapheme.Length == (codePoint > 0xFFFF ? 2 : 1) ? CustomGlyphs.TryGetPath(codePoint, metrics) : null;
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

    private static void DrawCursor(RenderCursor cursor, in CellMetrics metrics, IRenderSurface surface, bool customGlyphs)
    {
        var x = cursor.Column * metrics.Width;
        var y = cursor.Row * metrics.Height;
        switch (cursor.Shape)
        {
            case CursorShape.Block:
                surface.FillRectangle(x, y, metrics.Width, metrics.Height, cursor.Color);
                if (customGlyphs && GlyphOf(cursor.CellText, metrics) is { } glyph)
                {
                    surface.DrawCustomGlyph(glyph, x, y, metrics, cursor.AccentColor);
                }
                else if (!string.IsNullOrWhiteSpace(cursor.CellText))
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
