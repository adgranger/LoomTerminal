// Inspired by LoomEditor: src/LoomEditor.WinUI/Controls/LoomEditor.Font.cs (XamlRoot watch)
using LoomTerminal.Presentation.Rendering;
using LoomTerminal.WinUI.Rendering;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace LoomTerminal.WinUI.Controls;

public sealed partial class TerminalControl
{
    // Width of the scrollbar that takes room from the grid; the scrollbar itself comes with P4-12.
    private const double ScrollbarWidth = 0;

    private FrameScheduler? _frames;
    private DispatcherQueueTimer? _blinkTimer;
    private XamlRoot? _observedRoot;
    private double _scale = 1.0;
    private int _columns;
    private int _rows;

    private TerminalFont CurrentFont() =>
        new(FontFamily.Source.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), FontSize, LineHeight);

    private void InitializeLayout()
    {
        _frames = new FrameScheduler(OnFrame);
        _blinkTimer = DispatcherQueue.CreateTimer();
        _blinkTimer.IsRepeating = false;
        _blinkTimer.Tick += (_, _) => _session?.RefreshFrame();
        SizeChanged += (_, _) => RequestFrame();
        Loaded += OnLoaded;
        GotFocus += (_, _) => _session?.SetFocus(true);
        LostFocus += (_, _) => _session?.SetFocus(false);
        // Font, theme and cursor settings change what is drawn.
        foreach (var property in new[] { FontFamilyProperty, FontSizeProperty, ThemeProperty, LineHeightProperty, CursorStyleProperty, CursorBlinkProperty })
        {
            RegisterPropertyChangedCallback(property, (_, _) => RequestFrame());
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (XamlRoot is not { } root || ReferenceEquals(root, _observedRoot))
        {
            return;
        }

        StopObservingRoot();
        _observedRoot = root;
        _scale = root.RasterizationScale;
        root.Changed += OnXamlRootChanged;
        RequestFrame();
    }

    private void StopObservingRoot()
    {
        if (_observedRoot is not null)
        {
            _observedRoot.Changed -= OnXamlRootChanged;
            _observedRoot = null;
        }
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        if (sender.RasterizationScale != _scale)
        {
            _scale = sender.RasterizationScale;
            _measurer?.Invalidate();
            _layoutCache.Clear();
            RequestFrame();
        }
    }

    /// <summary>Asks for one redraw (and a size check) at the next composition frame.</summary>
    private void RequestFrame() => _frames?.Request();

    // Session frames are raised on the parser thread.
    private void OnSessionFrameAvailable() => DispatcherQueue.TryEnqueue(RequestFrame);

    private void OnFrame()
    {
        ApplySize();
        _canvas?.Invalidate();
        ScheduleBlink();
    }

    // cols = floor(usable width / cell width), at least 2x1; Resize only when the grid changed, so at most once per
    // frame, and never for a hidden (zero-sized) control: the shell keeps its last size.
    private void ApplySize()
    {
        if (_session is null || _canvas is null || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var metrics = (_measurer ??= new Win2DTextMeasurer(_canvas)).Measure(CurrentFont(), _scale);
        if (metrics.Width <= 0 || metrics.Height <= 0)
        {
            return;
        }

        var columns = Math.Max(2, (int)Math.Floor((ActualWidth - Padding.Left - Padding.Right - ScrollbarWidth) / metrics.Width));
        var rows = Math.Max(1, (int)Math.Floor((ActualHeight - Padding.Top - Padding.Bottom) / metrics.Height));
        if (columns == _columns && rows == _rows)
        {
            return;
        }

        (_columns, _rows) = (columns, rows);
        _session.Resize(columns, rows);
    }

    // The frame bakes the blink phase: ask the session for a new one when the phase changes.
    private void ScheduleBlink()
    {
        if (_blinkTimer is null)
        {
            return;
        }

        _blinkTimer.Stop();
        if (CursorBlink && _session?.CursorBlink.NextChange is { } next)
        {
            _blinkTimer.Interval = next;
            _blinkTimer.Start();
        }
    }
}
