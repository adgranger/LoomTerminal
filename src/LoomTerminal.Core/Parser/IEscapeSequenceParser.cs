// Ported from xtermjs/xterm.js: src/common/parser/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Parser;

/// <summary>The escape sequence parser: turns UTF-32 code points into calls to the registered handlers.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface IEscapeSequenceParser : IDisposable
{
    /// <summary>
    /// Preceding grapheme-join-state (a <c>UnicodeJoinProperties</c> value). Used for joining grapheme clusters across
    /// calls to print, and by REP to check whether repeating a character is allowed. The parser resets it for any
    /// valid sequence besides text.
    /// </summary>
    int PrecedingJoinState { get; set; }

    /// <summary>Resets the parser to its initial state (handlers are kept).</summary>
    void Reset();

    /// <summary>Parses the UTF-32 code points in <paramref name="data"/> up to <paramref name="length"/>.</summary>
    /// <param name="data">The data to parse.</param>
    /// <param name="length">The number of code points to parse.</param>
    void Parse(uint[] data, int length);

    /// <summary>
    /// Gets the string of a numerical function identifier. Useful in fallback handlers, which expose the low level
    /// numerical identifier for debugging purposes. A full back translation to <see cref="FunctionIdentifier"/> is
    /// not implemented.
    /// </summary>
    string IdentToString(int ident);

    /// <summary>Sets the print handler.</summary>
    void SetPrintHandler(PrintHandler handler);

    /// <summary>Restores the default print handler.</summary>
    void ClearPrintHandler();

    /// <summary>Registers an ESC handler. Dispose the result to remove it.</summary>
    IDisposable RegisterEscHandler(FunctionIdentifier id, EscHandler handler);

    /// <summary>Removes every ESC handler of an identifier.</summary>
    void ClearEscHandler(FunctionIdentifier id);

    /// <summary>Sets the fallback for ESC sequences without handler.</summary>
    void SetEscHandlerFallback(EscFallbackHandler handler);

    /// <summary>Sets the EXECUTE handler of a control character (a one-character string).</summary>
    void SetExecuteHandler(string flag, ExecuteHandler handler);

    /// <summary>Removes the EXECUTE handler of a control character.</summary>
    void ClearExecuteHandler(string flag);

    /// <summary>Sets the fallback for control characters without handler.</summary>
    void SetExecuteHandlerFallback(ExecuteFallbackHandler handler);

    /// <summary>Registers a CSI handler. Dispose the result to remove it.</summary>
    IDisposable RegisterCsiHandler(FunctionIdentifier id, CsiHandler handler);

    /// <summary>Removes every CSI handler of an identifier.</summary>
    void ClearCsiHandler(FunctionIdentifier id);

    /// <summary>Sets the fallback for CSI sequences without handler.</summary>
    void SetCsiHandlerFallback(CsiFallbackHandler callback);

    /// <summary>Registers a DCS handler. Dispose the result to remove it.</summary>
    IDisposable RegisterDcsHandler(FunctionIdentifier id, IDcsHandler handler);

    /// <summary>Removes every DCS handler of an identifier.</summary>
    void ClearDcsHandler(FunctionIdentifier id);

    /// <summary>Sets the fallback for DCS commands without handler.</summary>
    void SetDcsHandlerFallback(DcsFallbackHandler handler);

    /// <summary>Registers an OSC handler. Dispose the result to remove it.</summary>
    IDisposable RegisterOscHandler(int ident, IOscHandler handler);

    /// <summary>Removes every OSC handler of an identifier.</summary>
    void ClearOscHandler(int ident);

    /// <summary>Sets the fallback for OSC commands without handler.</summary>
    void SetOscHandlerFallback(OscFallbackHandler handler);

    /// <summary>Sets the error handler.</summary>
    void SetErrorHandler(Func<ParsingState, ParsingState> handler);

    /// <summary>Restores the default error handler.</summary>
    void ClearErrorHandler();
}
