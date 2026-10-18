// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Common;
using LoomTerminal.Core.Input;
using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.InputHandling;

/// <summary>The kind of report requested by an XTWINOPS sequence.</summary>
internal enum WindowsOptionsReportType
{
    /// <summary>Report the window size in pixels.</summary>
    GetWinSizePixels = 0,

    /// <summary>Report the cell size in pixels.</summary>
    GetCellSizePixels = 1,
}

/// <summary>An inclusive range of viewport rows to refresh.</summary>
/// <param name="Start">The first row.</param>
/// <param name="End">The last row.</param>
internal readonly record struct RowRange(int Start, int End);

/// <summary>
/// The terminal's standard handler of the input of the parser. Refer to
/// http://invisible-island.net/xterm/ctlseqs/ctlseqs.html to understand each handler's header comment. The handlers
/// are split by concern into partial files, each with a <c>Register…Handlers</c> method called by the constructor.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal partial class InputHandler : Disposable
{
    /// <summary>Max length of the UTF-32 input buffer. Real memory consumption is 4 times higher.</summary>
    private const int MaxParseBufferLength = 131072;

    private readonly IBufferService _bufferService;
    private readonly ICharsetService _charsetService;
    private readonly ICoreService _coreService;
    private readonly ILogService _logService;
    private readonly IOptionsService _optionsService;
    private readonly IOscLinkService _oscLinkService;
    private readonly ICoreMouseService _coreMouseService;
    private readonly IUnicodeService _unicodeService;
    private readonly IEscapeSequenceParser _parser;
    private readonly StringToUtf32 _stringDecoder = new();
    private readonly Utf8ToUtf32 _utf8Decoder = new();
    private readonly DirtyRowTracker _dirtyRowTracker;
    private readonly AttributeData _eraseAttrDataInternal = AttributeData.DefaultAttrData.Clone();

    private readonly Emitter<EventArgs> _onRequestBell;
    private readonly Emitter<RowRange?> _onRequestRefreshRows;
    private readonly Emitter<EventArgs> _onRequestReset;
    private readonly Emitter<EventArgs> _onRequestSendFocus;
    private readonly Emitter<EventArgs> _onRequestSyncScrollBar;
    private readonly Emitter<WindowsOptionsReportType> _onRequestWindowsOptionsReport;
    private readonly Emitter<string> _onA11yChar;
    private readonly Emitter<int> _onA11yTab;
    private readonly Emitter<EventArgs> _onCursorMove;
    private readonly Emitter<EventArgs> _onLineFeed;
    private readonly Emitter<int> _onScroll;
    private readonly Emitter<string> _onTitleChange;

    private uint[] _parseBuffer = new uint[4096];
    /// <summary>The window title stack (XTWINOPS 22 and 23).</summary>
    protected readonly List<string> _windowTitleStack = [];

    /// <summary>The icon name stack (XTWINOPS 22 and 23).</summary>
    protected readonly List<string> _iconNameStack = [];

    private int _cursorStartX;
    private int _cursorStartY;
    private readonly AttributeData _curAttrData = AttributeData.DefaultAttrData.Clone();
    private Core.Buffer.Buffer _activeBuffer;

    /// <summary>Creates the handler and registers its handlers on <paramref name="parser"/> (a new parser by default).</summary>
    public InputHandler(
        IBufferService bufferService,
        ICharsetService charsetService,
        ICoreService coreService,
        ILogService logService,
        IOptionsService optionsService,
        IOscLinkService oscLinkService,
        ICoreMouseService coreMouseService,
        IUnicodeService unicodeService,
        IEscapeSequenceParser? parser = null)
    {
        _bufferService = bufferService;
        _charsetService = charsetService;
        _coreService = coreService;
        _logService = logService;
        _optionsService = optionsService;
        _oscLinkService = oscLinkService;
        _coreMouseService = coreMouseService;
        _unicodeService = unicodeService;
        _parser = Register(parser ?? new EscapeSequenceParser());
        _dirtyRowTracker = new DirtyRowTracker(_bufferService);

        _onRequestBell = Register(new Emitter<EventArgs>());
        _onRequestRefreshRows = Register(new Emitter<RowRange?>());
        _onRequestReset = Register(new Emitter<EventArgs>());
        _onRequestSendFocus = Register(new Emitter<EventArgs>());
        _onRequestSyncScrollBar = Register(new Emitter<EventArgs>());
        _onRequestWindowsOptionsReport = Register(new Emitter<WindowsOptionsReportType>());
        _onA11yChar = Register(new Emitter<string>());
        _onA11yTab = Register(new Emitter<int>());
        _onCursorMove = Register(new Emitter<EventArgs>());
        _onLineFeed = Register(new Emitter<EventArgs>());
        _onScroll = Register(new Emitter<int>());
        _onTitleChange = Register(new Emitter<string>());

        // Track properties used in performance critical code manually to avoid using slow getters
        _activeBuffer = _bufferService.Buffer;
        Register(_bufferService.Buffers.OnBufferActivate.Subscribe(e => _activeBuffer = e.ActiveBuffer));

        // custom fallback handlers
        _parser.SetCsiHandlerFallback((ident, parameters) =>
            _logService.Debug($"Unknown CSI code: {_parser.IdentToString(ident)} [{string.Join(", ", parameters.ToArray())}]"));
        _parser.SetEscHandlerFallback(ident => _logService.Debug($"Unknown ESC code: {_parser.IdentToString(ident)}"));
        _parser.SetExecuteHandlerFallback(code => _logService.Debug($"Unknown EXECUTE code: {code}"));
        _parser.SetOscHandlerFallback((identifier, action, data) =>
            _logService.Debug($"Unknown OSC code: {identifier} {action} {data}"));
        _parser.SetDcsHandlerFallback((ident, action, payload) =>
        {
            if (action == DcsFallbackAction.Hook && payload is IParams hookParams)
            {
                payload = string.Join(", ", hookParams.ToArray());
            }

            _logService.Debug($"Unknown DCS code: {_parser.IdentToString(ident)} {action} {payload}");
        });

        // print handler
        // _parser.SetPrintHandler(Print); — P1-37

        // Registrations, in the order of the xterm.js constructor.
        // RegisterCursorHandlers(); — P1-39
        // RegisterEraseHandlers(); — P1-41
        // RegisterInsertDeleteHandlers(); — P1-42
        // RegisterTabHandlers(); — P1-40
        // RegisterDeviceAttributesHandlers(); — P1-43
        // RegisterModeHandlers(); — P1-45
        // RegisterSgrHandlers(); — P1-48
        // RegisterStatusHandlers(); — P1-49
        // RegisterControlHandlers(); — P1-38
        // RegisterEscHandlers(); — P1-44
        // RegisterOscHandlers(); — P1-51
        // RegisterDcsHandlers(); — P1-52
    }

    /// <summary>Raised when a bell is requested.</summary>
    public IEvent<EventArgs> OnRequestBell => _onRequestBell.Event;

    /// <summary>Raised with the viewport rows to refresh, or null to refresh everything.</summary>
    public IEvent<RowRange?> OnRequestRefreshRows => _onRequestRefreshRows.Event;

    /// <summary>Raised when a reset (RIS) is requested.</summary>
    public IEvent<EventArgs> OnRequestReset => _onRequestReset.Event;

    /// <summary>Raised when the focus state should be sent to the application.</summary>
    public IEvent<EventArgs> OnRequestSendFocus => _onRequestSendFocus.Event;

    /// <summary>Raised when the scroll bar should be synchronized with the buffer.</summary>
    public IEvent<EventArgs> OnRequestSyncScrollBar => _onRequestSyncScrollBar.Event;

    /// <summary>Raised when the host should answer an XTWINOPS report request.</summary>
    public IEvent<WindowsOptionsReportType> OnRequestWindowsOptionsReport => _onRequestWindowsOptionsReport.Event;

    /// <summary>Raised with each printed character, for accessibility.</summary>
    public IEvent<string> OnA11yChar => _onA11yChar.Event;

    /// <summary>Raised with the number of columns moved by a tab, for accessibility.</summary>
    public IEvent<int> OnA11yTab => _onA11yTab.Event;

    /// <summary>Raised when the cursor moved while parsing a chunk.</summary>
    public IEvent<EventArgs> OnCursorMove => _onCursorMove.Event;

    /// <summary>Raised on each line feed.</summary>
    public IEvent<EventArgs> OnLineFeed => _onLineFeed.Event;

    /// <summary>Raised with the new <c>YDisp</c> when the buffer scrolls.</summary>
    public IEvent<int> OnScroll => _onScroll.Event;

    /// <summary>Raised with the new title when it changes.</summary>
    public IEvent<string> OnTitleChange => _onTitleChange.Event;

    /// <summary>The current attributes used for new cells.</summary>
    public AttributeData GetAttrData() => _curAttrData;

    /// <summary>Marks the rows from <paramref name="y1"/> to <paramref name="y2"/> as needing a refresh.</summary>
    public void MarkRangeDirty(int y1, int y2) => _dirtyRowTracker.MarkRangeDirty(y1, y2);

    /// <summary>Parses a string of UTF-16 text.</summary>
    public void Parse(string data)
    {
        // Log debug data, the log level gate is to prevent extra work in this hot path
        if (_logService.Level <= LogLevel.Debug)
        {
            _logService.Debug($"parsing data \"{data}\"");
        }

        BeginParse(data.Length);
        if (data.Length > MaxParseBufferLength)
        {
            for (int i = 0; i < data.Length; i += MaxParseBufferLength)
            {
                int end = i + MaxParseBufferLength < data.Length ? i + MaxParseBufferLength : data.Length;
                int len = _stringDecoder.Decode(data[i..end], _parseBuffer);
                _parser.Parse(_parseBuffer, len);
            }
        }
        else
        {
            int len = _stringDecoder.Decode(data, _parseBuffer);
            _parser.Parse(_parseBuffer, len);
        }

        EndParse();
    }

    /// <summary>Parses UTF-8 bytes.</summary>
    public void Parse(ReadOnlySpan<byte> data)
    {
        // Log debug data, the log level gate is to prevent extra work in this hot path
        if (_logService.Level <= LogLevel.Debug)
        {
            _logService.Debug($"parsing data \"{string.Concat(data.ToArray().Select(b => (char)b))}\"");
        }

        BeginParse(data.Length);
        if (data.Length > MaxParseBufferLength)
        {
            for (int i = 0; i < data.Length; i += MaxParseBufferLength)
            {
                int end = i + MaxParseBufferLength < data.Length ? i + MaxParseBufferLength : data.Length;
                int len = _utf8Decoder.Decode(data[i..end], _parseBuffer);
                _parser.Parse(_parseBuffer, len);
            }
        }
        else
        {
            int len = _utf8Decoder.Decode(data, _parseBuffer);
            _parser.Parse(_parseBuffer, len);
        }

        EndParse();
    }

    private int GetCurrentLinkId() => _curAttrData.Extended.UrlId;

    private void BeginParse(int length)
    {
        _cursorStartX = _activeBuffer.X;
        _cursorStartY = _activeBuffer.Y;

        // resize input buffer if needed
        if (_parseBuffer.Length < length && _parseBuffer.Length < MaxParseBufferLength)
        {
            _parseBuffer = new uint[Math.Min(length, MaxParseBufferLength)];
        }

        // Clear the dirty row service so we know which lines changed as a result of parsing
        _dirtyRowTracker.ClearRange();
    }

    private void EndParse()
    {
        if (_activeBuffer.X != _cursorStartX || _activeBuffer.Y != _cursorStartY)
        {
            _onCursorMove.Fire(EventArgs.Empty);
        }

        // Refresh any dirty rows accumulated as part of parsing, fire only for rows within the
        // _viewport_ which is relative to ydisp, not relative to ybase.
        Core.Buffer.Buffer buffer = _bufferService.Buffer;
        int viewportEnd = _dirtyRowTracker.End + (buffer.YBase - buffer.YDisp);
        int viewportStart = _dirtyRowTracker.Start + (buffer.YBase - buffer.YDisp);
        if (viewportStart < _bufferService.Rows)
        {
            _onRequestRefreshRows.Fire(new RowRange(
                Math.Min(viewportStart, _bufferService.Rows - 1),
                Math.Min(viewportEnd, _bufferService.Rows - 1)));
        }
    }
}
