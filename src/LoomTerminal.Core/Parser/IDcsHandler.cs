// Ported from xtermjs/xterm.js: src/common/parser/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Parser;

/// <summary>Handler of one DCS command.</summary>
/// <remarks>Threading: not thread-safe; called from the parser thread only.</remarks>
internal interface IDcsHandler
{
    /// <summary>Called when a DCS command starts. Prepare the needed data structures here. <paramref name="p"/> is borrowed.</summary>
    void Hook(IParams p);

    /// <summary>Incoming payload chunk. The data is borrowed.</summary>
    void Put(uint[] data, int start, int end);

    /// <summary>
    /// End of the DCS command. <paramref name="success"/> tells whether the command finished normally or got aborted,
    /// so the final execution should depend on it. Also clean up the data structures here.
    /// </summary>
    bool Unhook(bool success);
}
