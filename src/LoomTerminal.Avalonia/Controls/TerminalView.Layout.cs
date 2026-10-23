using Avalonia;
using Avalonia.Controls;
using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Avalonia.Controls;

public sealed partial class TerminalView
{
    // Width of the scrollbar that takes room from the grid; the scrollbar itself comes with P6-06.
    private const double ScrollbarWidth = 0;

    private TopLevel? _topLevel;
    private bool _framePending;
    private double _scale = 1.0;
    private int _columns;
    private int _rows;

    /// <summary>Width of the terminal grid in cells (0 until the first layout).</summary>
    public int Columns => _columns;

    /// <summary>Height of the terminal grid in cells (0 until the first layout).</summary>
    public int Rows => _rows;

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null)
        {
            _scale = _topLevel.RenderScaling;
            _topLevel.ScalingChanged += OnScalingChanged;
        }

        RequestFrame();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_topLevel is not null)
        {
            _topLevel.ScalingChanged -= OnScalingChanged;
            _topLevel = null;
        }

        _framePending = false;
        _blinkTimer.Stop();
    }

    private void OnScalingChanged(object? sender, EventArgs e)
    {
        _scale = _topLevel?.RenderScaling ?? _scale;
        _measurer.Invalidate();
        _blobs.Clear();
        RequestFrame();
    }

    // One pending request at a time: an idle terminal costs no CPU.
    private void RequestFrame()
    {
        if (_framePending || _topLevel is null)
        {
            return;
        }

        _framePending = true;
        _topLevel.RequestAnimationFrame(_ =>
        {
            _framePending = false;
            OnFrame();
        });
    }

    private void OnFrame()
    {
        ApplySize();
        PumpPointerFrame();
        InvalidateVisual();
        NotifyImeCursorMoved();
        ScheduleBlink();
    }

    // cols = floor(usable width / cell width), at least 2x1; Resize only when the grid changed, so at most once per
    // frame, and never for a hidden (zero-sized) control: the shell keeps its last size.
    private void ApplySize()
    {
        if (_session is null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        CellMetrics metrics = _measurer.Measure(CurrentFont(), _scale);
        if (metrics.Width <= 0 || metrics.Height <= 0)
        {
            return;
        }

        int columns = Math.Max(2, (int)Math.Floor((Bounds.Width - Padding.Left - Padding.Right - ScrollbarWidth) / metrics.Width));
        int rows = Math.Max(1, (int)Math.Floor((Bounds.Height - Padding.Top - Padding.Bottom) / metrics.Height));
        if (columns == _columns && rows == _rows)
        {
            return;
        }

        (_columns, _rows) = (columns, rows);
        _session.Resize(columns, rows);
    }
}
