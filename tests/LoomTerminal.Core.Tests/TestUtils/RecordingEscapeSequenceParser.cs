using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.TestUtils;

/// <summary>
/// A parser that records the calls to <see cref="Parse"/> (data buffer size and length) instead of parsing, and
/// delegates everything else to a real parser. Replaces the JS tests that overwrite <c>_parser.parse</c>.
/// </summary>
/// <remarks>Threading: not thread-safe; used from a single test thread.</remarks>
internal sealed class RecordingEscapeSequenceParser : IEscapeSequenceParser
{
    private readonly EscapeSequenceParser _inner = new();

    public List<(int BufferLength, int Length)> Calls { get; } = [];

    public int PrecedingJoinState
    {
        get => _inner.PrecedingJoinState;
        set => _inner.PrecedingJoinState = value;
    }

    public void Dispose() => _inner.Dispose();

    public void Reset() => _inner.Reset();

    public void Parse(uint[] data, int length) => Calls.Add((data.Length, length));

    public string IdentToString(int ident) => _inner.IdentToString(ident);

    public void SetPrintHandler(PrintHandler handler) => _inner.SetPrintHandler(handler);

    public void ClearPrintHandler() => _inner.ClearPrintHandler();

    public IDisposable RegisterEscHandler(FunctionIdentifier id, EscHandler handler) => _inner.RegisterEscHandler(id, handler);

    public void ClearEscHandler(FunctionIdentifier id) => _inner.ClearEscHandler(id);

    public void SetEscHandlerFallback(EscFallbackHandler handler) => _inner.SetEscHandlerFallback(handler);

    public void SetExecuteHandler(string flag, ExecuteHandler handler) => _inner.SetExecuteHandler(flag, handler);

    public void ClearExecuteHandler(string flag) => _inner.ClearExecuteHandler(flag);

    public void SetExecuteHandlerFallback(ExecuteFallbackHandler handler) => _inner.SetExecuteHandlerFallback(handler);

    public IDisposable RegisterCsiHandler(FunctionIdentifier id, CsiHandler handler) => _inner.RegisterCsiHandler(id, handler);

    public void ClearCsiHandler(FunctionIdentifier id) => _inner.ClearCsiHandler(id);

    public void SetCsiHandlerFallback(CsiFallbackHandler callback) => _inner.SetCsiHandlerFallback(callback);

    public IDisposable RegisterDcsHandler(FunctionIdentifier id, IDcsHandler handler) => _inner.RegisterDcsHandler(id, handler);

    public void ClearDcsHandler(FunctionIdentifier id) => _inner.ClearDcsHandler(id);

    public void SetDcsHandlerFallback(DcsFallbackHandler handler) => _inner.SetDcsHandlerFallback(handler);

    public IDisposable RegisterOscHandler(int ident, IOscHandler handler) => _inner.RegisterOscHandler(ident, handler);

    public void ClearOscHandler(int ident) => _inner.ClearOscHandler(ident);

    public void SetOscHandlerFallback(OscFallbackHandler handler) => _inner.SetOscHandlerFallback(handler);

    public void SetErrorHandler(Func<ParsingState, ParsingState> handler) => _inner.SetErrorHandler(handler);

    public void ClearErrorHandler() => _inner.ClearErrorHandler();
}
