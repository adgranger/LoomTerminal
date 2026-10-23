using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Avalonia.Controls;

[TemplatePart(VerticalScrollBarPartName, typeof(ScrollBar))]
public sealed partial class TerminalView
{
    private const string VerticalScrollBarPartName = "PART_VerticalScrollBar";

    private ScrollBar? _scrollBar;

    // The default template: just the scrollbar over the grid, which the control draws itself.
    private static FuncControlTemplate<TerminalView> DefaultTemplate() => new((_, scope) =>
        new ScrollBar
        {
            Name = VerticalScrollBarPartName,
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsVisible = false,
        }.RegisterInNameScope(scope));

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_scrollBar is not null)
        {
            _scrollBar.Scroll -= OnScrollBarScroll;
        }

        _scrollBar = e.NameScope.Find<ScrollBar>(VerticalScrollBarPartName);
        if (_scrollBar is not null)
        {
            // Scroll is raised by the user only, so updating the value from a frame cannot loop back.
            _scrollBar.Scroll += OnScrollBarScroll;
        }

        RequestFrame();
    }

    private void OnScrollBarScroll(object? sender, ScrollEventArgs e) => _session?.ScrollToLine((int)Math.Round(e.NewValue));

    // Hidden in the alternate buffer (no scrollback there) and when there is nothing to scroll.
    private void UpdateScrollBar(RenderFrame frame)
    {
        if (_scrollBar is not { } bar)
        {
            return;
        }

        bar.IsVisible = !frame.IsAlternateBuffer && frame.ScrollbackLength > 0;
        bar.Minimum = 0;
        bar.Maximum = frame.ScrollbackLength;
        bar.ViewportSize = frame.Lines.Count;
        bar.SmallChange = 1;
        bar.LargeChange = Math.Max(1, frame.Lines.Count - 1);
        bar.Value = frame.ViewportY;
    }
}
