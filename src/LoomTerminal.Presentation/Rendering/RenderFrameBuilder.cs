using LoomTerminal.Core;
using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Common;
using LoomTerminal.Core.InputHandling;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Search;
using LoomTerminal.Presentation.Theming;

namespace LoomTerminal.Presentation.Rendering;

/// <summary>Builds <see cref="RenderFrame"/> snapshots from the emulator.</summary>
/// <remarks>Threading: parser thread only: it reads the emulator and listens to its events.</remarks>
internal sealed class RenderFrameBuilder : IDisposable
{
    private readonly TerminalCore _core;
    private readonly ThemePalette _palette;
    private readonly List<IDisposable> _subscriptions = [];
    private readonly HashSet<int> _dirty = [];
    private readonly CellData _cell = new();
    private CellColorResolver? _resolver;
    private bool _resolverBoldBright;
    private double _resolverContrast;
    private bool _allDirty = true;
    private ulong _version;
    private IBuffer? _lastBuffer;
    private bool _lastFocused = true;
    private BufferRange? _lastHoveredLink;
    private SearchHighlightMap? _lastSearchHighlights;
    private ((int X, int Y)? Start, (int X, int Y)? End, bool Column) _lastSelection;

    /// <summary>Creates a builder over <paramref name="core"/> and <paramref name="palette"/>.</summary>
    /// <param name="core">The emulator.</param>
    /// <param name="palette">The resolved colors.</param>
    internal RenderFrameBuilder(TerminalCore core, ThemePalette palette)
    {
        _core = core;
        _palette = palette;
        _subscriptions.Add(core.OnRequestRefreshRows.Subscribe(OnRefreshRows));
        _subscriptions.Add(core.OnResize.Subscribe(_ => _allDirty = true));
        _subscriptions.Add(core.OnScroll.Subscribe(_ => _allDirty = true));
        palette.Changed += OnPaletteChanged;
    }

    /// <summary>Builds the frame of the current state of the emulator.</summary>
    /// <param name="view">What the UI decides.</param>
    /// <returns>The frame.</returns>
    internal RenderFrame Build(ViewState view)
    {
        var buffer = _core.Buffers.Active;
        var rows = _core.Rows;
        var cols = _core.Cols;
        var resolver = GetResolver();
        if (!ReferenceEquals(buffer, _lastBuffer))
        {
            _lastBuffer = buffer;
            _allDirty = true;
        }

        // The selection and the focus change colors anywhere on the screen.
        var selectionState = (view.SelectionStart, view.SelectionEnd, view.SelectionColumnMode);
        if (selectionState != _lastSelection || view.IsFocused != _lastFocused || view.HoveredLink != _lastHoveredLink
            || !ReferenceEquals(view.SearchHighlights, _lastSearchHighlights))
        {
            _lastSearchHighlights = view.SearchHighlights;
            _lastHoveredLink = view.HoveredLink;
            _lastSelection = selectionState;
            _lastFocused = view.IsFocused;
            _allDirty = true;
        }

        var selection = new SelectionRenderModel();
        selection.Update(buffer.YDisp, rows, view.SelectionStart, view.SelectionEnd, view.SelectionColumnMode);

        var dirty = new HashSet<int>();
        if (_allDirty)
        {
            for (var y = 0; y < rows; y++)
            {
                dirty.Add(y);
            }
        }
        else
        {
            dirty.UnionWith(_dirty.Where(y => y >= 0 && y < rows));
        }

        _dirty.Clear();
        _allDirty = false;

        var lines = new List<RenderRow>(rows);
        for (var y = 0; y < rows; y++)
        {
            lines.Add(BuildRow(buffer.Lines.Get(buffer.YDisp + y), y, cols, resolver, view, selection));
        }

        return new RenderFrame(
            cols,
            rows,
            lines,
            BuildCursor(buffer, rows, cols, view),
            _palette.Background.Rgba,
            buffer.YDisp,
            buffer.YBase,
            ReferenceEquals(buffer, _core.Buffers.Alt),
            ++_version,
            dirty,
            selection.HasSelection
                ? new SelectionRange(selection.StartCol, selection.ViewportStartRow, selection.EndCol, selection.ViewportEndRow, selection.ColumnSelectMode)
                : null,
            view.IncludePlainLines ? BuildPlainLines(buffer, rows) : []);
    }

    /// <summary>Stops listening to the emulator.</summary>
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _palette.Changed -= OnPaletteChanged;
    }

    private static List<string> BuildPlainLines(Core.Buffer.Buffer buffer, int rows)
    {
        var plain = new List<string>(rows);
        for (var y = 0; y < rows; y++)
        {
            plain.Add(buffer.Lines.Get(buffer.YDisp + y)?.TranslateToString(true) ?? string.Empty);
        }

        return plain;
    }

    private void OnPaletteChanged() => _allDirty = true;

    private void OnRefreshRows(RowRange? range)
    {
        if (range is not { } r)
        {
            _allDirty = true;
            return;
        }

        for (var y = r.Start; y <= r.End; y++)
        {
            _dirty.Add(y);
        }
    }

    private CellColorResolver GetResolver()
    {
        var options = _core.Options;
        if (_resolver is null || _resolverBoldBright != options.DrawBoldTextInBrightColors || _resolverContrast != options.MinimumContrastRatio)
        {
            _resolverBoldBright = options.DrawBoldTextInBrightColors;
            _resolverContrast = options.MinimumContrastRatio;
            _palette.ContrastCache.Clear();
            _palette.HalfContrastCache.Clear();
            _resolver = new CellColorResolver(_palette, _resolverBoldBright, _resolverContrast);
            _allDirty = true;
        }

        return _resolver;
    }

    private RenderRow BuildRow(IBufferLine? line, int y, int cols, CellColorResolver resolver, ViewState view, SelectionRenderModel selection)
    {
        if (line is null)
        {
            return new RenderRow([], false, 0);
        }

        var bufferRow = _core.Buffers.Active.YDisp + y + 1;
        var styles = new TextRunStyle[cols];
        var texts = new string[cols];
        var widths = new int[cols];
        var lastContent = -1;
        for (var x = 0; x < cols; x++)
        {
            line.LoadCell(x, _cell);
            var width = _cell.GetWidth();
            widths[x] = width;
            var chars = _cell.GetChars();
            texts[x] = chars.Length == 0 ? " " : chars;
            styles[x] = ResolveStyle(resolver, _cell, view, selection.IsCellSelected(x, y), IsInLink(view.HoveredLink, x + 1, bufferRow, cols));
            ApplySearchHighlight(ref styles[x], view.SearchHighlights?.At(x, bufferRow - 1) ?? RunHighlight.None);
            if (width == 0)
            {
                // The second half of a wide character belongs to the content of the first.
                if (lastContent == x - 1)
                {
                    lastContent = x;
                }
            }
            else if (chars.Length != 0 || !IsPlain(styles[x]))
            {
                lastContent = x;
            }
        }

        var runs = new List<RenderRun>();
        var runStart = 0;
        var runText = new System.Text.StringBuilder();
        for (var x = 0; x <= lastContent; x++)
        {
            if (widths[x] == 0)
            {
                // Second cell of a wide character: no text, the cell joins the current run.
                continue;
            }

            if (runText.Length > 0 && styles[x] != styles[runStart])
            {
                runs.Add(new RenderRun(runStart, x - runStart, runText.ToString(), styles[runStart]));
                runText.Clear();
            }

            if (runText.Length == 0)
            {
                runStart = x;
            }

            runText.Append(texts[x]);
        }

        if (runText.Length > 0)
        {
            runs.Add(new RenderRun(runStart, lastContent + 1 - runStart, runText.ToString(), styles[runStart]));
        }

        var hash = new HashCode();
        foreach (var run in runs)
        {
            hash.Add(run);
        }

        return new RenderRow(runs, line.IsWrapped, hash.ToHashCode());
    }

    // A match tints the background with the translucent match color of the theme.
    private void ApplySearchHighlight(ref TextRunStyle style, RunHighlight highlight)
    {
        if (highlight == RunHighlight.None)
        {
            return;
        }

        var color = highlight == RunHighlight.ActiveSearchMatch ? _palette.SearchActiveMatchBackground : _palette.SearchMatchBackground;
        style = style with { Background = Rgba.Blend(style.Background, color.Rgba), Highlight = highlight };
    }

    private bool IsPlain(TextRunStyle style) =>
        style.Background == _palette.Background.Rgba
        && style.Underline == TerminalUnderlineStyle.None
        && !style.Strikethrough
        && !style.Overline
        && !style.IsLink
        && style.Highlight == RunHighlight.None;

    // The same linear comparison as Linkifier._linkAtPosition; x and row are 1-based.
    private static bool IsInLink(BufferRange? link, int x, int row, int cols) =>
        link is { } l
        && (l.Start.Y * cols) + l.Start.X <= (row * cols) + x
        && (row * cols) + x <= (l.End.Y * cols) + l.End.X;

    private static TextRunStyle ResolveStyle(CellColorResolver resolver, CellData cell, ViewState view, bool isSelected, bool isLink)
    {
        var colors = resolver.Resolve(cell, (int)cell.GetCode(), isSelected, view.IsFocused);
        var underline = cell.IsUnderline() != 0 ? (TerminalUnderlineStyle)(int)cell.GetUnderlineStyle() : TerminalUnderlineStyle.None;
        return new TextRunStyle(
            colors.Foreground,
            colors.Background,
            cell.IsBold() != 0,
            cell.IsItalic() != 0,
            cell.IsDim() != 0,
            underline,
            underline == TerminalUnderlineStyle.None ? null : colors.UnderlineColor,
            cell.IsStrikethrough() != 0,
            cell.IsOverline() != 0,
            isLink,
            RunHighlight.None);
    }

    private RenderCursor? BuildCursor(Core.Buffer.Buffer buffer, int rows, int cols, ViewState view)
    {
        var row = buffer.YBase + buffer.Y - buffer.YDisp;
        if (row < 0 || row >= rows)
        {
            return null;
        }

        var column = Math.Min(buffer.X, cols - 1);
        var modes = _core.CoreService.DecPrivateModes;
        var options = _core.Options;
        var style = modes.CursorStyle ?? options.CursorStyle;
        var blink = modes.CursorBlink ?? options.CursorBlink;
        var shape = style switch
        {
            CursorStyle.Underline => CursorShape.Underline,
            CursorStyle.Bar => CursorShape.Bar,
            _ => CursorShape.Block,
        };
        var visible = !_core.CoreService.IsCursorHidden && (!blink || view.CursorBlinkVisible);
        if (!view.IsFocused)
        {
            switch (options.CursorInactiveStyle)
            {
                case CursorInactiveStyle.Outline:
                    shape = CursorShape.Outline;
                    break;
                case CursorInactiveStyle.Block:
                    shape = CursorShape.Block;
                    break;
                case CursorInactiveStyle.Bar:
                    shape = CursorShape.Bar;
                    break;
                case CursorInactiveStyle.Underline:
                    shape = CursorShape.Underline;
                    break;
                default:
                    visible = false;
                    break;
            }
        }

        var line = buffer.Lines.Get(buffer.YBase + buffer.Y);
        var cellText = line is null ? string.Empty : line.LoadCell(column, _cell).GetChars();
        return new RenderCursor(column, row, shape, visible, _palette.Cursor.Rgba, _palette.CursorAccent.Rgba, cellText);
    }
}
