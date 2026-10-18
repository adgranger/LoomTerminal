// Ported from xtermjs/xterm.js: src/common/buffer/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Buffer;

/// <summary>A terminal buffer: its lines, cursor, scroll region, tab stops and markers.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface IBuffer
{
    /// <summary>The lines of the buffer (scrollback and viewport).</summary>
    CircularList<IBufferLine> Lines { get; }

    /// <summary>The line at the top of the viewport.</summary>
    int YDisp { get; set; }

    /// <summary>The line at the top of the active viewport (the first line of the scrollback ends before it).</summary>
    int YBase { get; set; }

    /// <summary>The cursor row, relative to <see cref="YBase"/>.</summary>
    int Y { get; set; }

    /// <summary>The cursor column.</summary>
    int X { get; set; }

    /// <summary>The tab stops by column; a missing key means no tab stop.</summary>
    Dictionary<int, bool> Tabs { get; set; }

    /// <summary>The bottom row of the scroll region.</summary>
    int ScrollBottom { get; set; }

    /// <summary>The top row of the scroll region.</summary>
    int ScrollTop { get; set; }

    /// <summary>Whether the buffer has a scrollback.</summary>
    bool HasScrollback { get; }

    /// <summary>The saved cursor row (DECSC).</summary>
    int SavedY { get; set; }

    /// <summary>The saved cursor column (DECSC).</summary>
    int SavedX { get; set; }

    /// <summary>The saved character set (DECSC); <see langword="null"/> for US ASCII.</summary>
    IReadOnlyDictionary<char, string>? SavedCharset { get; set; }

    /// <summary>The saved attributes (DECSC).</summary>
    AttributeData SavedCurAttrData { get; set; }

    /// <summary>Whether the cursor is in the viewport.</summary>
    bool IsCursorInViewport { get; }

    /// <summary>The markers of the buffer.</summary>
    List<Marker> Markers { get; }

    /// <summary>Translates a line to a string; an <paramref name="endCol"/> of -1 means the line length.</summary>
    string TranslateBufferLineToString(int lineIndex, bool trimRight, int startCol = 0, int endCol = -1);

    /// <summary>The first and last line of the wrapped line containing line <paramref name="y"/>.</summary>
    (int First, int Last) GetWrappedRangeForLine(int y);

    /// <summary>The tab stop after <paramref name="x"/> (default: the cursor column).</summary>
    int NextStop(int? x = null);

    /// <summary>The tab stop before <paramref name="x"/> (default: the cursor column).</summary>
    int PrevStop(int? x = null);

    /// <summary>Creates a blank line filled with the null cell of <paramref name="attr"/>.</summary>
    IBufferLine GetBlankLine(AttributeData attr, bool isWrapped = false);

    /// <summary>The shared null cell, with the given attributes (or none).</summary>
    CellData GetNullCell(AttributeData? attr = null);

    /// <summary>The shared whitespace cell, with the given attributes (or none).</summary>
    CellData GetWhitespaceCell(AttributeData? attr = null);

    /// <summary>Adds a marker on line <paramref name="y"/>.</summary>
    Marker AddMarker(int y);

    /// <summary>Disposes the markers on line <paramref name="y"/>.</summary>
    void ClearMarkers(int y);

    /// <summary>Disposes all the markers.</summary>
    void ClearAllMarkers();
}
