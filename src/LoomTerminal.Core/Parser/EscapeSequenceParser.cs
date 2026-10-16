// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Parser;

/// <summary>
/// The ANSI/DEC compatible parser described by Paul Williams (https://vt100.net/emu/dec_ansi_parser).
/// </summary>
/// <remarks>
/// <para>Threading: not thread-safe; used from the parser thread only.</para>
/// <para>
/// To implement custom ANSI compliant escape sequences, register a custom handler instead of altering this parser.
/// It operates in ZDM (Zero Default Mode) as suggested by the original parser: empty parameters are set to 0
/// (not in line with the latest ECMA-48, where ZDM was removed). Other than the original parser it supports
/// sub-parameters in digital parameters separated by colons; empty sub-parameters are set to -1.
/// </para>
/// <para>
/// Prefix and intermediate bytes: only one prefix byte is allowed as first parameter byte (range 0x3c .. 0x3f) and
/// at most two intermediates are respected (range 0x20 .. 0x2f). This is not in line with ECMA-48, which limits
/// neither.
/// </para>
/// <para>Handlers are synchronous (ADR 0002): the xterm.js async stack (<c>_parseStack</c>) is not ported.</para>
/// </remarks>
internal partial class EscapeSequenceParser : Disposable, IEscapeSequenceParser
{
    // buffers over several parse calls
    protected readonly Params _params;
    protected int _collect;

    // handler lookup containers
    protected PrintHandler _printHandler;
    protected Dictionary<int, ExecuteHandler> _executeHandlers = [];
    protected Dictionary<int, List<CsiHandler>> _csiHandlers = [];
    protected Dictionary<int, List<EscHandler>> _escHandlers = [];
    protected readonly OscParser _oscParser;
    protected readonly DcsParser _dcsParser;
    protected Func<ParsingState, ParsingState> _errorHandler;

    // fallback handlers
    protected PrintHandler _printHandlerFb;
    protected ExecuteFallbackHandler _executeHandlerFb;
    protected CsiFallbackHandler _csiHandlerFb;
    protected EscFallbackHandler _escHandlerFb;
    protected Func<ParsingState, ParsingState> _errorHandlerFb;

    protected readonly TransitionTable _transitions;

    /// <summary>Creates a parser.</summary>
    /// <param name="transitions">The transition table; defaults to the VT500 table.</param>
    public EscapeSequenceParser(TransitionTable? transitions = null)
    {
        _transitions = transitions ?? Vt500TransitionTable.Instance;

        InitialState = ParserState.Ground;
        CurrentState = InitialState;
        _params = new Params(); // defaults to 32 storable params/subparams
        _params.AddParam(0); // ZDM
        _collect = 0;
        PrecedingJoinState = 0;

        // set default fallback handlers and handler lookup containers
        _printHandlerFb = static (_, _, _) => { };
        _executeHandlerFb = static _ => { };
        _csiHandlerFb = static (_, _) => { };
        _escHandlerFb = static _ => { };
        _errorHandlerFb = static state => state;
        _printHandler = _printHandlerFb;
        Register(Disposables.Create(() =>
        {
            _csiHandlers = [];
            _executeHandlers = [];
            _escHandlers = [];
        }));
        _oscParser = Register(new OscParser());
        _dcsParser = Register(new DcsParser());
        _errorHandler = _errorHandlerFb;

        // swallow 7bit ST (ESC+\)
        RegisterEscHandler(new FunctionIdentifier(Final: "\\"), static () => true);
    }

    /// <summary>The state the parser starts in and returns to on <see cref="Reset"/>.</summary>
    public ParserState InitialState { get; set; }

    /// <summary>The current parser state.</summary>
    public ParserState CurrentState { get; set; }

    /// <inheritdoc />
    public int PrecedingJoinState { get; set; }

    /// <summary>Gets the string of a numerical function identifier (see <see cref="IEscapeSequenceParser.IdentToString"/>).</summary>
    public string IdentToString(int ident)
    {
        List<char> res = [];
        while (ident != 0)
        {
            res.Add((char)(ident & 0xFF));
            ident >>= 8;
        }

        res.Reverse();
        return new string([.. res]);
    }

    /// <inheritdoc />
    public void SetPrintHandler(PrintHandler handler) => _printHandler = handler;

    /// <inheritdoc />
    public void ClearPrintHandler() => _printHandler = _printHandlerFb;

    /// <inheritdoc />
    public IDisposable RegisterEscHandler(FunctionIdentifier id, EscHandler handler)
    {
        int ident = Identifier(id, 0x30, 0x7e);
        if (!_escHandlers.TryGetValue(ident, out List<EscHandler>? handlerList))
        {
            handlerList = [];
            _escHandlers[ident] = handlerList;
        }

        handlerList.Add(handler);
        return Disposables.Create(() =>
        {
            int handlerIndex = handlerList.IndexOf(handler);
            if (handlerIndex != -1)
            {
                handlerList.RemoveAt(handlerIndex);
            }
        });
    }

    /// <inheritdoc />
    public void ClearEscHandler(FunctionIdentifier id) => _escHandlers.Remove(Identifier(id, 0x30, 0x7e));

    /// <inheritdoc />
    public void SetEscHandlerFallback(EscFallbackHandler handler) => _escHandlerFb = handler;

    /// <inheritdoc />
    public void SetExecuteHandler(string flag, ExecuteHandler handler) => _executeHandlers[flag[0]] = handler;

    /// <inheritdoc />
    public void ClearExecuteHandler(string flag) => _executeHandlers.Remove(flag[0]);

    /// <inheritdoc />
    public void SetExecuteHandlerFallback(ExecuteFallbackHandler handler) => _executeHandlerFb = handler;

    /// <inheritdoc />
    public IDisposable RegisterCsiHandler(FunctionIdentifier id, CsiHandler handler)
    {
        int ident = Identifier(id);
        if (!_csiHandlers.TryGetValue(ident, out List<CsiHandler>? handlerList))
        {
            handlerList = [];
            _csiHandlers[ident] = handlerList;
        }

        handlerList.Add(handler);
        return Disposables.Create(() =>
        {
            int handlerIndex = handlerList.IndexOf(handler);
            if (handlerIndex != -1)
            {
                handlerList.RemoveAt(handlerIndex);
            }
        });
    }

    /// <inheritdoc />
    public void ClearCsiHandler(FunctionIdentifier id) => _csiHandlers.Remove(Identifier(id));

    /// <inheritdoc />
    public void SetCsiHandlerFallback(CsiFallbackHandler callback) => _csiHandlerFb = callback;

    /// <inheritdoc />
    public IDisposable RegisterDcsHandler(FunctionIdentifier id, IDcsHandler handler) =>
        _dcsParser.RegisterHandler(Identifier(id), handler);

    /// <inheritdoc />
    public void ClearDcsHandler(FunctionIdentifier id) => _dcsParser.ClearHandler(Identifier(id));

    /// <inheritdoc />
    public void SetDcsHandlerFallback(DcsFallbackHandler handler) => _dcsParser.SetHandlerFallback(handler);

    /// <inheritdoc />
    public IDisposable RegisterOscHandler(int ident, IOscHandler handler) => _oscParser.RegisterHandler(ident, handler);

    /// <inheritdoc />
    public void ClearOscHandler(int ident) => _oscParser.ClearHandler(ident);

    /// <inheritdoc />
    public void SetOscHandlerFallback(OscFallbackHandler handler) => _oscParser.SetHandlerFallback(handler);

    /// <inheritdoc />
    public void SetErrorHandler(Func<ParsingState, ParsingState> handler) => _errorHandler = handler;

    /// <inheritdoc />
    public void ClearErrorHandler() => _errorHandler = _errorHandlerFb;

    /// <summary>Resets the parser to its initial values (handlers are kept).</summary>
    public void Reset()
    {
        CurrentState = InitialState;
        _oscParser.Reset();
        _dcsParser.Reset();
        _params.Reset();
        _params.AddParam(0); // ZDM
        _collect = 0;
        PrecedingJoinState = 0;
    }

    /// <summary>
    /// Turns a function identifier into an integer: prefix, intermediates and final packed in bytes, so the parse loop
    /// can find handlers fast.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="finalStart">The lowest allowed final byte.</param>
    /// <param name="finalEnd">The highest allowed final byte.</param>
    /// <exception cref="ArgumentException">The identifier is not valid.</exception>
    protected static int Identifier(FunctionIdentifier id, int finalStart = 0x40, int finalEnd = 0x7e)
    {
        int res = 0;
        if (!string.IsNullOrEmpty(id.Prefix))
        {
            if (id.Prefix.Length > 1)
            {
                throw new ArgumentException("only one byte as prefix supported");
            }

            res = id.Prefix[0];
            if ((res != 0 && 0x3c > res) || res > 0x3f)
            {
                throw new ArgumentException("prefix must be in range 0x3c .. 0x3f");
            }
        }

        if (!string.IsNullOrEmpty(id.Intermediates))
        {
            if (id.Intermediates.Length > 2)
            {
                throw new ArgumentException("only two bytes as intermediates are supported");
            }

            foreach (char intermediate in id.Intermediates)
            {
                if (0x20 > intermediate || intermediate > 0x2f)
                {
                    throw new ArgumentException("intermediate must be in range 0x20 .. 0x2f");
                }

                res <<= 8;
                res |= intermediate;
            }
        }

        if (id.Final.Length != 1)
        {
            throw new ArgumentException("final must be a single byte");
        }

        int finalCode = id.Final[0];
        if (finalStart > finalCode || finalCode > finalEnd)
        {
            throw new ArgumentException($"final must be in range {finalStart} .. {finalEnd}");
        }

        res <<= 8;
        res |= finalCode;

        return res;
    }
}
