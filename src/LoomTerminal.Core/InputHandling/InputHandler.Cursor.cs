// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>Restricts the cursor to the viewport size, or to the scroll margins in origin mode.</summary>
    private void RestrictCursor(int? maxCol = null)
    {
        int max = maxCol ?? _bufferService.Cols - 1;
        _activeBuffer.X = Math.Min(max, Math.Max(0, _activeBuffer.X));
        _activeBuffer.Y = _coreService.DecPrivateModes.Origin
            ? Math.Min(_activeBuffer.ScrollBottom, Math.Max(_activeBuffer.ScrollTop, _activeBuffer.Y))
            : Math.Min(_bufferService.Rows - 1, Math.Max(0, _activeBuffer.Y));
        _dirtyRowTracker.MarkDirty(_activeBuffer.Y);
    }
}
