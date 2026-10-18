// Ported from xtermjs/xterm.js: src/common/WindowsMode.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core;

/// <summary>The wrapped line heuristics of winpty and old ConPTY builds.</summary>
/// <remarks>Threading: stateless; used from the parser thread only.</remarks>
internal static class WindowsMode
{
    /// <summary>
    /// Marks the cursor line as wrapped when the last character of the previous line is not a space. Winpty does not
    /// support wraparound mode, which means that lines would never be marked as wrapped. The workaround is to listen
    /// to every incoming line feed and mark the line as wrapped if the last character in the previous line is not a
    /// space. This is certainly not without its problems, but generally on Windows when text reaches the end of the
    /// terminal it's likely going to be wrapped.
    /// </summary>
    public static void UpdateWindowsModeWrappedState(IBufferService bufferService)
    {
        Core.Buffer.Buffer buffer = bufferService.Buffer;
        IBufferLine? line = buffer.Lines.Get(buffer.YBase + buffer.Y - 1);
        CharData? lastChar = line?.Get(bufferService.Cols - 1);

        IBufferLine? nextLine = buffer.Lines.Get(buffer.YBase + buffer.Y);
        if (nextLine is not null && lastChar is not null)
        {
            nextLine.IsWrapped = lastChar.Value.Code != BufferConstants.NullCellCode
                && lastChar.Value.Code != BufferConstants.WhitespaceCellCode;
        }
    }
}
