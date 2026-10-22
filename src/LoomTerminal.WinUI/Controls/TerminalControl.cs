using LoomTerminal.Presentation.Rendering;
using LoomTerminal.WinUI.Rendering;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
/// In progress (phase 4): backgrounds and text runs are drawn; decorations, the cursor and frame scheduling
/// come with the next cards, see docs/ROADMAP.md.
/// </para>
/// <para>Threading: like every WinUI control, use it on the UI thread only.</para>
/// </remarks>
[TemplatePart(Name = CanvasPartName, Type = typeof(CanvasControl))]
public sealed partial class TerminalControl : Control
{
    private const string CanvasPartName = "PART_Canvas";

    private readonly TextRunLayoutCache _layoutCache = new(256);
    private CanvasControl? _canvas;
    private Win2DTextMeasurer? _measurer;
    private RenderFrame? _lastFrame;

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
        _canvas.CreateResources += OnCanvasCreateResources;
    }

    private void OnCanvasDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var scale = XamlRoot?.RasterizationScale ?? 1.0;
        var font = new TerminalFont(FontFamily.Source.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), FontSize, LineHeight);
        var metrics = (_measurer ??= new Win2DTextMeasurer(sender)).Measure(font, scale);
        var frame = _session?.TryTakeFrame() ?? _lastFrame;
        if (frame is null)
        {
            return;
        }

        _lastFrame = frame;
        // 4 x rows x an average of 4 runs per row; the cache is cleared on a device change (CreateResources).
        _layoutCache.Capacity = 16 * Math.Max(1, frame.Lines.Count);
        var surface = new Win2DRenderSurface(args.DrawingSession, sender, _layoutCache, font, Win2DTextMeasurer.ResolveFamily(font), scale);
        FrameRenderer.Render(frame, metrics, surface);
    }

    // Device (re)creation, or a DPI change: cached metrics and layouts belong to the old device.
    private void OnCanvasCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        _measurer?.Invalidate();
        _measurer = null;
        _layoutCache.Clear();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => DetachCanvas();

    private void DetachCanvas()
    {
        if (_canvas is null)
        {
            return;
        }

        _canvas.Draw -= OnCanvasDraw;
        _canvas.CreateResources -= OnCanvasCreateResources;
        _layoutCache.Clear();
        // Win2D: release the device resources of a control that leaves the tree, or they leak.
        _canvas.RemoveFromVisualTree();
        _canvas = null;
    }
}
