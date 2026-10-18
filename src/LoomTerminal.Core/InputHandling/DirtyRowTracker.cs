// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.InputHandling;

/// <summary>Tracks the range of buffer rows that changed while parsing, relative to the top of the active buffer.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class DirtyRowTracker
{
    private readonly IBufferService _bufferService;

    /// <summary>Creates a tracker whose range is the cursor row.</summary>
    public DirtyRowTracker(IBufferService bufferService)
    {
        _bufferService = bufferService;
        ClearRange();
    }

    /// <summary>The first dirty row.</summary>
    public int Start { get; private set; }

    /// <summary>The last dirty row.</summary>
    public int End { get; private set; }

    /// <summary>Resets the range to the cursor row.</summary>
    public void ClearRange()
    {
        Start = _bufferService.Buffer.Y;
        End = _bufferService.Buffer.Y;
    }

    /// <summary>Extends the range to include row <paramref name="y"/>.</summary>
    public void MarkDirty(int y)
    {
        if (y < Start)
        {
            Start = y;
        }
        else if (y > End)
        {
            End = y;
        }
    }

    /// <summary>Extends the range to include the rows from <paramref name="y1"/> to <paramref name="y2"/> (in any order).</summary>
    public void MarkRangeDirty(int y1, int y2)
    {
        if (y1 > y2)
        {
            (y1, y2) = (y2, y1);
        }

        if (y1 < Start)
        {
            Start = y1;
        }

        if (y2 > End)
        {
            End = y2;
        }
    }

    /// <summary>Marks every row of the viewport dirty.</summary>
    public void MarkAllDirty() => MarkRangeDirty(0, _bufferService.Rows - 1);
}
