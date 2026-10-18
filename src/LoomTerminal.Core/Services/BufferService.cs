// Ported from xtermjs/xterm.js: src/common/services/BufferService.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Services;

/// <summary>Owns the size of the terminal and its buffers, and implements scrolling.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class BufferService : Disposable, IBufferService
{
    /// <summary>The minimum number of columns: less than 2 can mess with wide chars.</summary>
    public const int MinimumCols = 2;

    /// <summary>The minimum number of rows.</summary>
    public const int MinimumRows = 1;

    private readonly Emitter<BufferResizeEvent> _onResize;
    private readonly Emitter<int> _onScroll;

    // A blank line to clone/copy from for new blank lines
    private IBufferLine? _cachedBlankLine;

    /// <summary>Creates the service, sized after the options (at least the minimum size).</summary>
    public BufferService(IOptionsService optionsService)
    {
        _onResize = Register(new Emitter<BufferResizeEvent>());
        _onScroll = Register(new Emitter<int>());
        Cols = Math.Max(optionsService.RawOptions.Cols, MinimumCols);
        Rows = Math.Max(optionsService.RawOptions.Rows, MinimumRows);
        Buffers = Register(new BufferSet(optionsService, this));
        Register(Buffers.OnBufferActivate.Subscribe(e => _onScroll.Fire(e.ActiveBuffer.YDisp)));
    }

    /// <inheritdoc />
    public int Cols { get; private set; }

    /// <inheritdoc />
    public int Rows { get; private set; }

    /// <inheritdoc />
    public BufferSet Buffers { get; }

    /// <inheritdoc />
    public bool IsUserScrolling { get; set; }

    /// <inheritdoc />
    public IEvent<BufferResizeEvent> OnResize => _onResize.Event;

    /// <inheritdoc />
    public IEvent<int> OnScroll => _onScroll.Event;

    /// <inheritdoc />
    public Core.Buffer.Buffer Buffer => Buffers.Active;

    /// <inheritdoc />
    public void Resize(int cols, int rows)
    {
        bool colsChanged = Cols != cols;
        bool rowsChanged = Rows != rows;
        Cols = cols;
        Rows = rows;
        Buffers.Resize(cols, rows);
        _onResize.Fire(new BufferResizeEvent(cols, rows, colsChanged, rowsChanged));
    }

    /// <inheritdoc />
    public void Reset()
    {
        Buffers.Reset();
        IsUserScrolling = false;
    }

    /// <inheritdoc />
    public void Scroll(AttributeData eraseAttr, bool isWrapped = false)
    {
        Core.Buffer.Buffer buffer = Buffer;

        IBufferLine? newLine = _cachedBlankLine;
        if (newLine is null || newLine.Length != Cols || newLine.GetFg(0) != eraseAttr.Fg || newLine.GetBg(0) != eraseAttr.Bg)
        {
            newLine = buffer.GetBlankLine(eraseAttr, isWrapped);
            _cachedBlankLine = newLine;
        }

        newLine.IsWrapped = isWrapped;

        int topRow = buffer.YBase + buffer.ScrollTop;
        int bottomRow = buffer.YBase + buffer.ScrollBottom;

        if (buffer.ScrollTop == 0)
        {
            // Determine whether the buffer is going to be trimmed after insertion.
            bool willBufferBeTrimmed = buffer.Lines.IsFull;

            // Insert the line using the fastest method
            if (bottomRow == buffer.Lines.Length - 1)
            {
                if (willBufferBeTrimmed)
                {
                    buffer.Lines.Recycle().CopyFrom(newLine);
                }
                else
                {
                    buffer.Lines.Push(newLine.Clone());
                }
            }
            else
            {
                buffer.Lines.Splice(bottomRow + 1, 0, newLine.Clone());
            }

            // Only adjust YBase and YDisp when the buffer is not trimmed
            if (!willBufferBeTrimmed)
            {
                buffer.YBase++;

                // Only scroll the YDisp with YBase if the user has not scrolled up
                if (!IsUserScrolling)
                {
                    buffer.YDisp++;
                }
            }
            else if (IsUserScrolling)
            {
                // When the buffer is full and the user has scrolled up, keep the text stable unless YDisp is right at
                // the top
                buffer.YDisp = Math.Max(buffer.YDisp - 1, 0);
            }
        }
        else
        {
            // ScrollTop is non-zero which means no line will be going to the scrollback, instead we can just shift
            // them in-place.
            int scrollRegionHeight = bottomRow - topRow + 1; // as it's zero-based
            buffer.Lines.ShiftElements(topRow + 1, scrollRegionHeight - 1, -1);
            buffer.Lines.Set(bottomRow, newLine.Clone());
        }

        // Move the viewport to the bottom of the buffer unless the user is scrolling.
        if (!IsUserScrolling)
        {
            buffer.YDisp = buffer.YBase;
        }

        _onScroll.Fire(buffer.YDisp);
    }

    /// <inheritdoc />
    public void ScrollLines(int disp, bool suppressScrollEvent = false)
    {
        Core.Buffer.Buffer buffer = Buffer;
        if (disp < 0)
        {
            if (buffer.YDisp == 0)
            {
                return;
            }

            IsUserScrolling = true;
        }
        else if (disp + buffer.YDisp >= buffer.YBase)
        {
            IsUserScrolling = false;
        }

        int oldYdisp = buffer.YDisp;
        buffer.YDisp = Math.Max(Math.Min(buffer.YDisp + disp, buffer.YBase), 0);

        // No change occurred, don't trigger scroll/refresh
        if (oldYdisp == buffer.YDisp)
        {
            return;
        }

        if (!suppressScrollEvent)
        {
            _onScroll.Fire(buffer.YDisp);
        }
    }
}
