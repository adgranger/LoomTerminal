// Ported from xtermjs/xterm.js: src/common/services/Services.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Services;

/// <summary>The buffer service: the size of the terminal and its buffers (P1-33 adds the other members).</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface IBufferService
{
    /// <summary>The number of columns.</summary>
    int Cols { get; }

    /// <summary>The number of rows.</summary>
    int Rows { get; }
}
