// Ported from xtermjs/xterm.js: src/common/parser/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Parser;

/// <summary>CSI handler. <paramref name="p"/> is borrowed. Returns true when the sequence was handled.</summary>
internal delegate bool CsiHandler(IParams p);

/// <summary>Fallback called when no CSI handler handled the sequence.</summary>
internal delegate void CsiFallbackHandler(int ident, IParams p);

/// <summary>ESC handler. Returns true when the sequence was handled.</summary>
internal delegate bool EscHandler();

/// <summary>Fallback called when no ESC handler handled the sequence.</summary>
internal delegate void EscFallbackHandler(int ident);

/// <summary>EXECUTE (C0/C1 control) handler. Returns true when the control was handled.</summary>
internal delegate bool ExecuteHandler();

/// <summary>Fallback called when no EXECUTE handler handled the control.</summary>
internal delegate void ExecuteFallbackHandler(int ident);

/// <summary>PRINT handler: receives <c>data[start..end)</c>, the data is borrowed.</summary>
internal delegate void PrintHandler(uint[] data, int start, int end);

/// <summary>The phase of an OSC command reported to <see cref="OscFallbackHandler"/>.</summary>
internal enum OscFallbackAction
{
    Start,
    Put,
    End,
}

/// <summary>Fallback called for OSC commands without handler. The payload is a string for Put and End.</summary>
internal delegate void OscFallbackHandler(int ident, OscFallbackAction action, string? payload);

/// <summary>The phase of a DCS command reported to <see cref="DcsFallbackHandler"/>.</summary>
internal enum DcsFallbackAction
{
    Hook,
    Put,
    Unhook,
}

/// <summary>
/// Fallback called for DCS commands without handler. The payload is an <see cref="IParams"/> for Hook, a
/// <see cref="string"/> for Put and a <see cref="bool"/> for Unhook.
/// </summary>
internal delegate void DcsFallbackHandler(int ident, DcsFallbackAction action, object? payload);

/// <summary>
/// Denotes a specific ESC, CSI or DCS handler slot. The values are turned into an integer during handler
/// registration, to allow a faster handler access while parsing.
/// </summary>
/// <param name="Prefix">The prefix character (<c>&lt;</c>, <c>=</c>, <c>&gt;</c> or <c>?</c>), if any.</param>
/// <param name="Intermediates">The intermediate characters, if any.</param>
/// <param name="Final">The final character; it is required, the default only allows named arguments.</param>
internal readonly record struct FunctionIdentifier(string? Prefix = null, string? Intermediates = null, string Final = "");

/// <summary>
/// Internal state of the escape sequence parser, given to the error handler for introspection on parse errors.
/// Set <see cref="Abort"/> to true to abort the current parsing.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class ParsingState
{
    /// <summary>Position in the parsed data.</summary>
    public int Position { get; set; }

    /// <summary>The actual character code.</summary>
    public int Code { get; set; }

    /// <summary>The current parser state.</summary>
    public ParserState CurrentState { get; set; }

    /// <summary>The collect buffer with intermediate characters.</summary>
    public int Collect { get; set; }

    /// <summary>The params buffer.</summary>
    public IParams Params { get; set; } = null!;

    /// <summary>Whether to abort the parsing (default: false).</summary>
    public bool Abort { get; set; }
}
