// Ported from xtermjs/xterm.js: src/common/parser/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Parser;

/// <summary>
/// The OSC sub-parser seen by <see cref="EscapeSequenceParser"/> (<c>IOscParser</c> / <c>ISubParser</c> in xterm.js).
/// It lets the parser tests replace the OSC parser by a mock, as the xterm.js tests do.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface IOscParser : IDisposable
{
    /// <summary>Resets the parser.</summary>
    void Reset();

    /// <summary>Registers a handler for an OSC identifier. Dispose the result to remove it.</summary>
    IDisposable RegisterHandler(int ident, IOscHandler handler);

    /// <summary>Removes every handler of an OSC identifier.</summary>
    void ClearHandler(int ident);

    /// <summary>Sets the handler called for OSC commands that no registered handler takes.</summary>
    void SetHandlerFallback(OscFallbackHandler handler);

    /// <summary>Puts data to the current OSC command.</summary>
    void Put(uint[] data, int start, int end);

    /// <summary>Starts a new OSC command.</summary>
    void Start();

    /// <summary>Ends the OSC command; <paramref name="success"/> tells whether it finished normally or got aborted.</summary>
    void End(bool success);
}
