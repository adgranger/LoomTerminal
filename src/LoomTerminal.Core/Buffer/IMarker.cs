// Ported from xtermjs/xterm.js: src/common/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Buffer;

/// <summary>A marker: a line of the buffer that follows the content as it scrolls.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface IMarker : IDisposable
{
    /// <summary>A unique id.</summary>
    int Id { get; }

    /// <summary>Whether the marker was disposed.</summary>
    bool IsDisposed { get; }

    /// <summary>The line of the marker, <c>-1</c> once disposed.</summary>
    int Line { get; }

    /// <summary>Raised when the marker is disposed, before its listeners are removed.</summary>
    IEvent<EventArgs> OnDispose { get; }
}
