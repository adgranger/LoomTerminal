// Ported from xtermjs/xterm.js: src/common/services/Services.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Services;

/// <summary>The payload of <see cref="IBufferService.OnResize"/>.</summary>
/// <param name="Cols">The new number of columns.</param>
/// <param name="Rows">The new number of rows.</param>
/// <param name="ColsChanged">Whether the number of columns changed.</param>
/// <param name="RowsChanged">Whether the number of rows changed.</param>
internal readonly record struct BufferResizeEvent(int Cols, int Rows, bool ColsChanged, bool RowsChanged);

/// <summary>The buffer service: the size of the terminal, its buffers, and scrolling.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface IBufferService
{
    /// <summary>The number of columns.</summary>
    int Cols { get; }

    /// <summary>The number of rows.</summary>
    int Rows { get; }

    /// <summary>The active buffer.</summary>
    Core.Buffer.Buffer Buffer { get; }

    /// <summary>The normal and alt buffers.</summary>
    BufferSet Buffers { get; }

    /// <summary>Whether the user is scrolling (locks the scroll position).</summary>
    bool IsUserScrolling { get; set; }

    /// <summary>Raised after a resize.</summary>
    IEvent<BufferResizeEvent> OnResize { get; }

    /// <summary>Raised when the display scrolls; the payload is the new <c>YDisp</c>.</summary>
    IEvent<int> OnScroll { get; }

    /// <summary>Scrolls the terminal down 1 row, creating a blank line.</summary>
    void Scroll(AttributeData eraseAttr, bool isWrapped = false);

    /// <summary>Scrolls the display of the terminal by <paramref name="disp"/> lines (negative scrolls up).</summary>
    void ScrollLines(int disp, bool suppressScrollEvent = false);

    /// <summary>Resizes the terminal.</summary>
    void Resize(int cols, int rows);

    /// <summary>Resets the buffers.</summary>
    void Reset();
}
