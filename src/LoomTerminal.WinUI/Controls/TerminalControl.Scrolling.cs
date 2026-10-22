// Inspired by LoomEditor: src/LoomEditor.WinUI/Controls/LoomEditor.Scrolling.cs (template part, value sync without feedback loops)
using LoomTerminal.Presentation.Rendering;
using LoomTerminal.Presentation.Session;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.System;

namespace LoomTerminal.WinUI.Controls;

public sealed partial class TerminalControl
{
    private const string VerticalScrollBarPartName = "PART_VerticalScrollBar";

    private ScrollBar? _scrollBar;
    private bool _syncingScrollBar;

    private void AttachScrollBar()
    {
        DetachScrollBar();
        _scrollBar = GetTemplateChild(VerticalScrollBarPartName) as ScrollBar;
        if (_scrollBar is not null)
        {
            _scrollBar.ValueChanged += OnScrollBarValueChanged;
        }
    }

    private void DetachScrollBar()
    {
        if (_scrollBar is not null)
        {
            _scrollBar.ValueChanged -= OnScrollBarValueChanged;
            _scrollBar = null;
        }
    }

    private void OnScrollBarValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_syncingScrollBar)
        {
            _session?.ScrollToLine((int)Math.Round(e.NewValue));
        }
    }

    // Hidden in the alternate buffer (no scrollback there) and when there is nothing to scroll.
    private void UpdateScrollBar(RenderFrame frame)
    {
        if (_scrollBar is not { } bar)
        {
            return;
        }

        _syncingScrollBar = true;
        try
        {
            var visible = !frame.IsAlternateBuffer && frame.ScrollbackLength > 0;
            bar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            bar.Minimum = 0;
            bar.Maximum = frame.ScrollbackLength;
            bar.ViewportSize = frame.Lines.Count;
            bar.SmallChange = 1;
            bar.LargeChange = Math.Max(1, frame.Lines.Count - 1);
            bar.Value = frame.ViewportY;
        }
        finally
        {
            _syncingScrollBar = false;
        }
    }

    // Plain PageUp/PageDown belong to the application; only the Shift / Ctrl+Shift variants scroll the scrollback.
    private static bool TryScrollbackKey(TerminalSession session, VirtualKey key, bool ctrl, bool shift)
    {
        if (!shift)
        {
            return false;
        }

        switch (key)
        {
            case VirtualKey.PageUp when !ctrl:
                session.ScrollPages(-1);
                return true;
            case VirtualKey.PageDown when !ctrl:
                session.ScrollPages(1);
                return true;
            case VirtualKey.Home when ctrl:
                session.ScrollToTop();
                return true;
            case VirtualKey.End when ctrl:
                session.ScrollToBottom();
                return true;
            default:
                return false;
        }
    }
}
