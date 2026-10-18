// Ported from xtermjs/xterm.js: src/common/buffer/Buffer.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;
using LoomTerminal.Core.Data;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Buffer;

/// <summary>
/// A terminal buffer (an internal state of the terminal), where the following is stored: the text content of this
/// particular buffer, the cursor position and the scroll position.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed partial class Buffer : IBuffer
{
    /// <summary>The maximum size of a buffer: 2^32 - 1.</summary>
    public const long MaxBufferSize = 4294967295;

    private readonly bool _hasScrollback;
    private readonly IOptionsService _optionsService;
    private readonly IBufferService _bufferService;
    private readonly CellData _nullCell = CellData.FromCharData(
        new CharData(0, BufferConstants.NullCellChar, BufferConstants.NullCellWidth, BufferConstants.NullCellCode));
    private readonly CellData _whitespaceCell = CellData.FromCharData(
        new CharData(0, BufferConstants.WhitespaceCellChar, BufferConstants.WhitespaceCellWidth, BufferConstants.WhitespaceCellCode));
    private int _cols;
    private int _rows;
    private bool _isClearing;

    /// <summary>Creates a buffer sized after the buffer service.</summary>
    public Buffer(bool hasScrollback, IOptionsService optionsService, IBufferService bufferService)
    {
        _hasScrollback = hasScrollback;
        _optionsService = optionsService;
        _bufferService = bufferService;
        _cols = _bufferService.Cols;
        _rows = _bufferService.Rows;
        Lines = new CircularList<IBufferLine>(GetCorrectBufferLength(_rows));
        ScrollTop = 0;
        ScrollBottom = _rows - 1;
        Tabs = [];
        SetupTabStops();
    }

    /// <inheritdoc />
    public CircularList<IBufferLine> Lines { get; private set; }

    /// <inheritdoc />
    public int YDisp { get; set; }

    /// <inheritdoc />
    public int YBase { get; set; }

    /// <inheritdoc />
    public int Y { get; set; }

    /// <inheritdoc />
    public int X { get; set; }

    /// <inheritdoc />
    public int ScrollBottom { get; set; }

    /// <inheritdoc />
    public int ScrollTop { get; set; }

    /// <inheritdoc />
    public Dictionary<int, bool> Tabs { get; set; }

    /// <inheritdoc />
    public int SavedY { get; set; }

    /// <inheritdoc />
    public int SavedX { get; set; }

    /// <inheritdoc />
    public AttributeData SavedCurAttrData { get; set; } = AttributeData.DefaultAttrData.Clone();

    /// <inheritdoc />
    public IReadOnlyDictionary<char, string>? SavedCharset { get; set; } = Charsets.DefaultCharset;

    /// <inheritdoc />
    public List<Marker> Markers { get; } = [];

    /// <inheritdoc />
    public bool HasScrollback => _hasScrollback && Lines.MaxLength > _rows;

    /// <inheritdoc />
    public bool IsCursorInViewport
    {
        get
        {
            int absoluteY = YBase + Y;
            int relativeY = absoluteY - YDisp;
            return relativeY >= 0 && relativeY < _rows;
        }
    }

    /// <inheritdoc />
    public CellData GetNullCell(AttributeData? attr = null)
    {
        if (attr is not null)
        {
            _nullCell.Fg = attr.Fg;
            _nullCell.Bg = attr.Bg;
            _nullCell.Extended = attr.Extended;
        }
        else
        {
            _nullCell.Fg = 0;
            _nullCell.Bg = 0;
            _nullCell.Extended = new ExtendedAttrs();
        }

        return _nullCell;
    }

    /// <inheritdoc />
    public CellData GetWhitespaceCell(AttributeData? attr = null)
    {
        if (attr is not null)
        {
            _whitespaceCell.Fg = attr.Fg;
            _whitespaceCell.Bg = attr.Bg;
            _whitespaceCell.Extended = attr.Extended;
        }
        else
        {
            _whitespaceCell.Fg = 0;
            _whitespaceCell.Bg = 0;
            _whitespaceCell.Extended = new ExtendedAttrs();
        }

        return _whitespaceCell;
    }

    /// <inheritdoc />
    public IBufferLine GetBlankLine(AttributeData attr, bool isWrapped = false) =>
        new BufferLine(_bufferService.Cols, GetNullCell(attr), isWrapped);

    /// <summary>Fills the viewport of the buffer with blank lines.</summary>
    public void FillViewportRows(AttributeData? fillAttr = null)
    {
        if (Lines.Length == 0)
        {
            fillAttr ??= AttributeData.DefaultAttrData;
            int i = _rows;
            while (i-- > 0)
            {
                Lines.Push(GetBlankLine(fillAttr));
            }
        }
    }

    /// <summary>Clears the buffer to its initial state, discarding all previous data.</summary>
    public void Clear()
    {
        YDisp = 0;
        YBase = 0;
        Y = 0;
        X = 0;
        Lines = new CircularList<IBufferLine>(GetCorrectBufferLength(_rows));
        ScrollTop = 0;
        ScrollBottom = _rows - 1;
        SetupTabStops();
    }

    /// <inheritdoc />
    public string TranslateBufferLineToString(int lineIndex, bool trimRight, int startCol = 0, int endCol = -1)
    {
        IBufferLine? line = Lines.Get(lineIndex);
        if (line is null)
        {
            return string.Empty;
        }

        return line.TranslateToString(trimRight, startCol, endCol);
    }

    /// <inheritdoc />
    public (int First, int Last) GetWrappedRangeForLine(int y)
    {
        int first = y;
        int last = y;

        // Scan upwards for wrapped lines
        while (first > 0 && Lines.Get(first)!.IsWrapped)
        {
            first--;
        }

        // Scan downwards for wrapped lines
        while (last + 1 < Lines.Length && Lines.Get(last + 1)!.IsWrapped)
        {
            last++;
        }

        return (first, last);
    }

    /// <summary>Sets up the tab stops, starting from <paramref name="start"/> (or from scratch).</summary>
    public void SetupTabStops(int? start = null)
    {
        int i;
        if (start is { } value)
        {
            i = value;
            if (!HasTabStop(i))
            {
                i = PrevStop(i);
            }
        }
        else
        {
            Tabs = [];
            i = 0;
        }

        for (; i < _cols; i += (int)_optionsService.RawOptions.TabStopWidth)
        {
            Tabs[i] = true;
        }
    }

    /// <inheritdoc />
    public int PrevStop(int? x = null)
    {
        int position = x ?? X;
        while (!HasTabStop(--position) && position > 0)
        {
        }

        return position >= _cols ? _cols - 1 : position < 0 ? 0 : position;
    }

    /// <inheritdoc />
    public int NextStop(int? x = null)
    {
        int position = x ?? X;
        while (!HasTabStop(++position) && position < _cols)
        {
        }

        return position >= _cols ? _cols - 1 : position < 0 ? 0 : position;
    }

    /// <inheritdoc />
    public void ClearMarkers(int y)
    {
        _isClearing = true;
        for (int i = 0; i < Markers.Count; i++)
        {
            if (Markers[i].Line == y)
            {
                Markers[i].Dispose();
                Markers.RemoveAt(i--);
            }
        }

        _isClearing = false;
    }

    /// <inheritdoc />
    public void ClearAllMarkers()
    {
        _isClearing = true;
        for (int i = 0; i < Markers.Count; i++)
        {
            Markers[i].Dispose();
        }

        Markers.Clear();
        _isClearing = false;
    }

    /// <inheritdoc />
    public Marker AddMarker(int y)
    {
        Marker marker = new(y);
        Markers.Add(marker);
        marker.Register(Lines.OnTrim.Subscribe(amount =>
        {
            marker.Line -= amount;

            // The marker should be disposed when the line is trimmed from the buffer
            if (marker.Line < 0)
            {
                marker.Dispose();
            }
        }));
        marker.Register(Lines.OnInsert.Subscribe(e =>
        {
            if (marker.Line >= e.Index)
            {
                marker.Line += e.Amount;
            }
        }));
        marker.Register(Lines.OnDelete.Subscribe(e =>
        {
            // Delete the marker if it's within the range
            if (marker.Line >= e.Index && marker.Line < e.Index + e.Amount)
            {
                marker.Dispose();
            }

            // Shift the marker if it's after the deleted range
            if (marker.Line > e.Index)
            {
                marker.Line -= e.Amount;
            }
        }));
        marker.Register(marker.OnDispose.Subscribe(_ => RemoveMarker(marker)));
        return marker;
    }

    // JS: this.tabs[x] is undefined (falsy) for a column without a tab stop
    private bool HasTabStop(int column) => Tabs.TryGetValue(column, out bool value) && value;

    private void RemoveMarker(Marker marker)
    {
        if (!_isClearing)
        {
            Markers.RemoveAt(Markers.IndexOf(marker));
        }
    }

    /// <summary>
    /// The correct buffer length based on the rows provided, the scrollback of the terminal and whether this buffer is
    /// flagged to have a scrollback or not.
    /// </summary>
    private int GetCorrectBufferLength(int rows)
    {
        if (!_hasScrollback)
        {
            return rows;
        }

        long correctBufferLength = rows + _optionsService.RawOptions.Scrollback;

        // CircularList sizes are ints: the 2^32 - 1 cap of xterm.js is lowered to int.MaxValue
        return (int)Math.Min(correctBufferLength > MaxBufferSize ? MaxBufferSize : correctBufferLength, int.MaxValue);
    }
}
