using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace LoomTerminal.WinUI.Controls;

/// <summary>The LoomTerminal terminal control.</summary>
/// <example>
/// <code language="xml">
/// &lt;Window xmlns:loom="using:LoomTerminal.WinUI.Controls"&gt;
///     &lt;loom:TerminalControl /&gt;
/// &lt;/Window&gt;
/// </code>
/// </example>
/// <remarks>
/// <para>
/// Skeleton: the control only proves the chain (template, Win2D canvas, Native AOT). The emulator
/// (LoomTerminal.Core) and the renderer come in phases 1 to 4, see docs/ROADMAP.md.
/// </para>
/// <para>Threading: like every WinUI control, use it on the UI thread only.</para>
/// </remarks>
[TemplatePart(Name = CanvasPartName, Type = typeof(CanvasControl))]
public sealed partial class TerminalControl : Control
{
    private const string CanvasPartName = "PART_Canvas";

    private CanvasControl? _canvas;

    /// <summary>Creates the control.</summary>
    public TerminalControl()
    {
        DefaultStyleKey = typeof(TerminalControl);
        Unloaded += OnUnloaded;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachCanvas();
        _canvas = GetTemplateChild(CanvasPartName) as CanvasControl
            ?? throw new InvalidOperationException($"The TerminalControl template must contain a CanvasControl named '{CanvasPartName}'.");
        _canvas.Draw += OnCanvasDraw;
    }

    private void OnCanvasDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        // Brushes set in XAML are SolidColorBrush in practice; anything else falls back to the default foreground.
        Color foreground = Foreground is SolidColorBrush brush ? brush.Color : Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC);
        using var format = new CanvasTextFormat { FontFamily = "Cascadia Mono", FontSize = (float)FontSize };
        args.DrawingSession.DrawText("LoomTerminal — skeleton (the emulator comes in phase 1)", 12, 8, foreground, format);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => DetachCanvas();

    private void DetachCanvas()
    {
        if (_canvas is null)
        {
            return;
        }

        _canvas.Draw -= OnCanvasDraw;
        // Win2D: release the device resources of a control that leaves the tree, or they leak.
        _canvas.RemoveFromVisualTree();
        _canvas = null;
    }
}
