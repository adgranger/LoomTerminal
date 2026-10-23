using LoomTerminal.Presentation.Rendering;
using LoomTerminal.WinUI.Rendering;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

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
/// In progress (phase 4): the grid is drawn and sized to the control; input and the scrollbar come with the
/// next cards, see docs/ROADMAP.md.
/// </para>
/// <para>Threading: like every WinUI control, use it on the UI thread only.</para>
/// </remarks>
[TemplatePart(Name = CanvasPartName, Type = typeof(CanvasControl))]
[TemplatePart(Name = VerticalScrollBarPartName, Type = typeof(ScrollBar))]
public sealed partial class TerminalControl : Control
{
    private const string CanvasPartName = "PART_Canvas";

    private readonly TextRunLayoutCache _layoutCache = new(256);
    private CanvasControl? _canvas;
    // Where the canvas lives in the template: a canvas released on Unloaded is recreated there on the next Loaded.
    private Panel? _canvasHost;
    private Win2DTextMeasurer? _measurer;
    private TerminalFont? _font;
    private GlyphFamilyMap? _glyphFamilies;
    private RenderFrame? _lastFrame;
    // Win2D throws when text is measured before the canvas has a device (first CreateResources).
    private bool _hasCanvasDevice;

    /// <summary>How long the last frame took to draw (for diagnostics, e.g. a status bar).</summary>
    public TimeSpan LastFrameTime { get; private set; }

    /// <summary>Creates the control.</summary>
    public TerminalControl()
    {
        DefaultStyleKey = typeof(TerminalControl);
        Unloaded += OnUnloaded;
        InitializeLayout();
        InitializeKeyboard();
        InitializePointer();
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachCanvas();
        _canvas = GetTemplateChild(CanvasPartName) as CanvasControl
            ?? throw new InvalidOperationException($"The TerminalControl template must contain a CanvasControl named '{CanvasPartName}'.");
        _canvasHost = _canvas.Parent as Panel;
        _canvas.Draw += OnCanvasDraw;
        _canvas.CreateResources += OnCanvasCreateResources;
        AttachScrollBar();
    }

    // A tab host (TabView, ...) unloads the control when it is not shown and loads it again later: Unloaded released
    // the Win2D canvas, which cannot be reused, so give the template a new one.
    private void RestoreCanvas()
    {
        if (_canvas is not null || _canvasHost is not { } host)
        {
            return;
        }

        _canvas = new CanvasControl { ClearColor = Microsoft.UI.Colors.Transparent };
        _canvas.Draw += OnCanvasDraw;
        _canvas.CreateResources += OnCanvasCreateResources;
        host.Children.Insert(0, _canvas);
        AttachScrollBar();
    }

    private void OnCanvasDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var scale = _scale;
        var font = CurrentFont();
        var metrics = (_measurer ??= new Win2DTextMeasurer(sender)).Measure(font, scale);
        var frame = _session?.TryTakeFrame() ?? _lastFrame;
        if (frame is null)
        {
            return;
        }

        _lastFrame = frame;
        UpdateScrollBar(frame);
        // 4 x rows x an average of 4 runs per row; the cache is cleared on a device change (CreateResources).
        _layoutCache.Capacity = 16 * Math.Max(1, frame.Lines.Count);
        var surface = new Win2DRenderSurface(args.DrawingSession, sender, _layoutCache, font, _glyphFamilies ??= new GlyphFamilyMap(Win2DTextMeasurer.ResolveFamilies(font)), scale);
        FrameRenderer.Render(frame, metrics, surface, _session?.CustomGlyphs ?? true, _composition);
        LastFrameTime = System.Diagnostics.Stopwatch.GetElapsedTime(started);
    }

    // Device (re)creation, or a DPI change: cached metrics and layouts belong to the old device.
    private void OnCanvasCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        _hasCanvasDevice = true;
        _measurer?.Invalidate();
        _measurer = null;
        _layoutCache.Clear();
        RequestFrame();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopIme();
        _glyphFamilies?.Dispose();
        _glyphFamilies = null;
        _frames?.Cancel();
        _blinkTimer?.Stop();
        StopObservingRoot();
        DetachScrollBar();
        DetachCanvas();
    }

    private void DetachCanvas()
    {
        if (_canvas is null)
        {
            return;
        }

        _hasCanvasDevice = false;
        _canvas.Draw -= OnCanvasDraw;
        _canvas.CreateResources -= OnCanvasCreateResources;
        _layoutCache.Clear();
        // Win2D: release the device resources of a control that leaves the tree, or they leak.
        _canvas.RemoveFromVisualTree();
        _canvas = null;
    }
}
