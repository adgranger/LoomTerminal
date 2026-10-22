using LoomTerminal.Presentation.Rendering;
using LoomTerminal.WinUI.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Windows.Foundation;

namespace LoomTerminal.WinUI.Accessibility;

/// <summary>
/// <see cref="ITextProvider"/> over the visible rows of the last frame (<see cref="RenderFrame.PlainLines"/>), joined
/// by <c>\r\n</c>. Read only; no scrollback.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class TerminalTextProvider(TerminalControl owner, Func<IRawElementProviderSimple> element) : ITextProvider
{
    public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.Single;

    public ITextRangeProvider DocumentRange
    {
        get
        {
            var text = Snapshot(out _);
            return new TerminalTextRange(text, element, 0, text.Length);
        }
    }

    public ITextRangeProvider[] GetSelection()
    {
        var text = Snapshot(out var frame);
        // Without selection UIA expects one degenerate range at the caret.
        var (start, end) = frame is null ? (0, 0) : SelectionOffsets(frame, text);
        return [new TerminalTextRange(text, element, start, end)];
    }

    public ITextRangeProvider[] GetVisibleRanges() => [DocumentRange];

    public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement) => DocumentRange;

    // shortcut: no cell geometry in the provider, a point maps to the start of the text; upgrade with hit testing.
    public ITextRangeProvider RangeFromPoint(Point screenLocation) => new TerminalTextRange(Snapshot(out _), element, 0, 0);

    private string Snapshot(out RenderFrame? frame)
    {
        frame = owner.LastFrame;
        return frame is null ? string.Empty : string.Join("\r\n", frame.PlainLines);
    }

    // Offset of a frame position in the joined text; rows outside the viewport clamp to the text ends.
    private static int Offset(RenderFrame frame, string text, int column, int row)
    {
        if (row < 0)
        {
            return 0;
        }

        if (row >= frame.PlainLines.Count)
        {
            return text.Length;
        }

        var offset = 0;
        for (var i = 0; i < row; i++)
        {
            offset += frame.PlainLines[i].Length + 2;
        }

        return offset + Math.Clamp(column, 0, frame.PlainLines[row].Length);
    }

    private static (int Start, int End) SelectionOffsets(RenderFrame frame, string text)
    {
        // shortcut: a column-mode selection is read as a stream of text; upgrade if screen readers need rectangles.
        if (frame.Selection is { } s)
        {
            return (Offset(frame, text, s.StartColumn, s.StartRow), Offset(frame, text, s.EndColumn, s.EndRow));
        }

        var caret = frame.Cursor is { } c ? Offset(frame, text, c.Column, c.Row) : 0;
        return (caret, caret);
    }
}
