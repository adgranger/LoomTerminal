// Ported from xtermjs/xterm.js: src/common/CoreTerminal.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Common;
using LoomTerminal.Core.InputHandling;
using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core;

/// <summary>
/// The composition root of the emulator: it creates and wires the services and the <see cref="InputHandler"/>, and
/// exposes the API used by Presentation (write, resize, scroll, reset, events, buffers, options).
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only (ADR 0002).</remarks>
internal sealed class TerminalCore : Disposable
{
    private readonly InputHandler _inputHandler;
    private readonly MutableDisposable<IDisposable> _windowsWrappingHeuristics;
    private readonly Emitter<string> _onBinary;
    private readonly Emitter<string> _onData;
    private readonly Emitter<EventArgs> _onLineFeed;
    private readonly Emitter<BufferResizeEvent> _onResize;
    private readonly Emitter<int> _onScroll;
    private readonly Emitter<string> _onTitleChange;
    private readonly Emitter<EventArgs> _onBell;
    private readonly Emitter<EventArgs> _onCursorMove;
    private readonly Emitter<RowRange?> _onRequestRefreshRows;
    private readonly Emitter<EventArgs> _onRequestSendFocus;
    private readonly Emitter<EventArgs> _onRequestSyncScrollBar;
    private readonly Emitter<WindowsOptionsReportType> _onRequestWindowsOptionsReport;
    private readonly Emitter<string> _onA11yChar;
    private readonly Emitter<int> _onA11yTab;
    private readonly Emitter<IReadOnlyList<ColorRequest>> _onColor;

    /// <summary>Creates a terminal with the given options (the defaults if null).</summary>
    public TerminalCore(TerminalOptions? options = null)
    {
        // Setup and initialize services
        OptionsService = Register(new OptionsService(options));
        BufferService = Register(new BufferService(OptionsService));
        LogService = NullLogService.Instance;
        CoreService = Register(new CoreService(BufferService, LogService, OptionsService));
        CoreMouseService = Register(new CoreMouseService(BufferService, CoreService, OptionsService));
        UnicodeService = new UnicodeService();
        CharsetService = new CharsetService();
        OscLinkService = new OscLinkService(BufferService);

        // Register input handler and handle/forward events
        _inputHandler = Register(new InputHandler(
            BufferService, CharsetService, CoreService, LogService, OptionsService, OscLinkService, CoreMouseService, UnicodeService));

        _onBinary = Register(new Emitter<string>());
        _onData = Register(new Emitter<string>());
        _onLineFeed = Register(new Emitter<EventArgs>());
        _onResize = Register(new Emitter<BufferResizeEvent>());
        _onScroll = Register(new Emitter<int>());
        _onTitleChange = Register(new Emitter<string>());
        _onBell = Register(new Emitter<EventArgs>());
        _onCursorMove = Register(new Emitter<EventArgs>());
        _onRequestRefreshRows = Register(new Emitter<RowRange?>());
        _onRequestSendFocus = Register(new Emitter<EventArgs>());
        _onRequestSyncScrollBar = Register(new Emitter<EventArgs>());
        _onRequestWindowsOptionsReport = Register(new Emitter<WindowsOptionsReportType>());
        _onA11yChar = Register(new Emitter<string>());
        _onA11yTab = Register(new Emitter<int>());
        _onColor = Register(new Emitter<IReadOnlyList<ColorRequest>>());

        Forward(_inputHandler.OnLineFeed, _onLineFeed);
        Forward(_inputHandler.OnTitleChange, _onTitleChange);
        Forward(_inputHandler.OnRequestBell, _onBell);
        Forward(_inputHandler.OnCursorMove, _onCursorMove);
        Forward(_inputHandler.OnRequestRefreshRows, _onRequestRefreshRows);
        Forward(_inputHandler.OnRequestSendFocus, _onRequestSendFocus);
        Forward(_inputHandler.OnRequestSyncScrollBar, _onRequestSyncScrollBar);
        Forward(_inputHandler.OnRequestWindowsOptionsReport, _onRequestWindowsOptionsReport);
        Forward(_inputHandler.OnA11yChar, _onA11yChar);
        Forward(_inputHandler.OnA11yTab, _onA11yTab);
        Forward(_inputHandler.OnColor, _onColor);

        // RIS: the input handler only asks for a reset, the terminal does it.
        Register(_inputHandler.OnRequestReset.Subscribe(_ => Reset()));

        _windowsWrappingHeuristics = Register(new MutableDisposable<IDisposable>());

        // Setup listeners
        Forward(BufferService.OnResize, _onResize);
        Forward(CoreService.OnData, _onData);
        Forward(CoreService.OnBinary, _onBinary);
        Register(CoreService.OnRequestScrollToBottom.Subscribe(_ => ScrollToBottom()));
        Register(OptionsService.OnMultipleOptionChange([TerminalOption.WindowsMode, TerminalOption.WindowsPty], HandleWindowsPtyOptionChange));
        Register(BufferService.OnScroll.Subscribe(_ =>
        {
            _onScroll.Fire(BufferService.Buffer.YDisp);
            _inputHandler.MarkRangeDirty(BufferService.Buffer.ScrollTop, BufferService.Buffer.ScrollBottom);
        }));

        // JS _setup()
        HandleWindowsPtyOptionChange();
    }

    /// <summary>The options service.</summary>
    internal OptionsService OptionsService { get; }

    /// <summary>The buffer service.</summary>
    internal BufferService BufferService { get; }

    /// <summary>The log service (a null log service for now).</summary>
    internal ILogService LogService { get; }

    /// <summary>The core service (modes and data events).</summary>
    internal CoreService CoreService { get; }

    /// <summary>The mouse service.</summary>
    internal CoreMouseService CoreMouseService { get; }

    /// <summary>The Unicode service.</summary>
    internal UnicodeService UnicodeService { get; }

    /// <summary>The character set service.</summary>
    internal CharsetService CharsetService { get; }

    /// <summary>The hyperlink service.</summary>
    internal OscLinkService OscLinkService { get; }

    /// <summary>The number of columns.</summary>
    public int Cols => BufferService.Cols;

    /// <summary>The number of rows.</summary>
    public int Rows => BufferService.Rows;

    /// <summary>The normal and alternate buffers.</summary>
    public BufferSet Buffers => BufferService.Buffers;

    /// <summary>The current options.</summary>
    public TerminalOptions Options => OptionsService.RawOptions;

    /// <summary>Raised with the binary data (a latin-1 string) to send to the pty.</summary>
    public IEvent<string> OnBinary => _onBinary.Event;

    /// <summary>Raised with the data to send to the pty.</summary>
    public IEvent<string> OnData => _onData.Event;

    /// <summary>Raised on each line feed.</summary>
    public IEvent<EventArgs> OnLineFeed => _onLineFeed.Event;

    /// <summary>Raised when the terminal is resized.</summary>
    public IEvent<BufferResizeEvent> OnResize => _onResize.Event;

    /// <summary>Raised with the new <c>YDisp</c> when the buffer scrolls.</summary>
    public IEvent<int> OnScroll => _onScroll.Event;

    /// <summary>Raised with the new title when it changes.</summary>
    public IEvent<string> OnTitleChange => _onTitleChange.Event;

    /// <summary>Raised when a bell is requested.</summary>
    public IEvent<EventArgs> OnBell => _onBell.Event;

    /// <summary>Raised when the cursor moved while parsing.</summary>
    public IEvent<EventArgs> OnCursorMove => _onCursorMove.Event;

    /// <summary>Raised with the viewport rows to refresh, or null to refresh everything.</summary>
    public IEvent<RowRange?> OnRequestRefreshRows => _onRequestRefreshRows.Event;

    /// <summary>Raised when the focus state should be sent to the application.</summary>
    public IEvent<EventArgs> OnRequestSendFocus => _onRequestSendFocus.Event;

    /// <summary>Raised when the scroll bar should be synchronized with the buffer.</summary>
    public IEvent<EventArgs> OnRequestSyncScrollBar => _onRequestSyncScrollBar.Event;

    /// <summary>Raised when the host should answer an XTWINOPS report request.</summary>
    public IEvent<WindowsOptionsReportType> OnRequestWindowsOptionsReport => _onRequestWindowsOptionsReport.Event;

    /// <summary>Raised with each printed character in screen reader mode.</summary>
    public IEvent<string> OnA11yChar => _onA11yChar.Event;

    /// <summary>Raised with the number of columns moved by a tab in screen reader mode.</summary>
    public IEvent<int> OnA11yTab => _onA11yTab.Event;

    /// <summary>Raised with the color requests of the OSC color sequences.</summary>
    public IEvent<IReadOnlyList<ColorRequest>> OnColor => _onColor.Event;

    /// <summary>Parses <paramref name="data"/> synchronously (JS <c>writeSync</c> without the sub-call limit).</summary>
    public void Write(string data) => _inputHandler.Parse(data);

    /// <summary>
    /// Parses the UTF-8 bytes <paramref name="data"/> synchronously. A multi-byte character may be split across two
    /// calls.
    /// </summary>
    public void Write(ReadOnlySpan<byte> data) => _inputHandler.Parse(data);

    /// <summary>Sends <paramref name="data"/> to the pty, as typed by the user unless <paramref name="wasUserInput"/> is false.</summary>
    public void Input(string data, bool wasUserInput = true) => CoreService.TriggerDataEvent(data, wasUserInput);

    /// <summary>Resizes the terminal (the size is at least 2 columns and 1 row).</summary>
    public void Resize(int x, int y)
    {
        x = Math.Max(x, BufferService.MinimumCols);
        y = Math.Max(y, BufferService.MinimumRows);
        BufferService.Resize(x, y);
    }

    /// <summary>Scrolls the buffer by one line, adding a blank line filled with <paramref name="eraseAttr"/>.</summary>
    public void Scroll(AttributeData eraseAttr, bool isWrapped = false) => BufferService.Scroll(eraseAttr, isWrapped);

    /// <summary>Scrolls the viewport by <paramref name="disp"/> lines.</summary>
    public void ScrollLines(int disp, bool suppressScrollEvent = false) => BufferService.ScrollLines(disp, suppressScrollEvent);

    /// <summary>Scrolls the viewport by pages (a page is one row less than the screen).</summary>
    public void ScrollPages(int pageCount) => ScrollLines(pageCount * (Rows - 1));

    /// <summary>Scrolls the viewport to the top of the scrollback.</summary>
    public void ScrollToTop() => ScrollLines(-BufferService.Buffer.YDisp);

    /// <summary>Scrolls the viewport to the bottom.</summary>
    public void ScrollToBottom() => ScrollLines(BufferService.Buffer.YBase - BufferService.Buffer.YDisp);

    /// <summary>Scrolls the viewport to the given buffer line.</summary>
    public void ScrollToLine(int line)
    {
        int scrollAmount = line - BufferService.Buffer.YDisp;
        if (scrollAmount != 0)
        {
            ScrollLines(scrollAmount);
        }
    }

    /// <summary>Registers an ESC handler. Dispose the result to remove it.</summary>
    public IDisposable RegisterEscHandler(FunctionIdentifier id, EscHandler callback) =>
        _inputHandler.RegisterEscHandler(id, callback);

    /// <summary>Registers a DCS handler. Dispose the result to remove it.</summary>
    public IDisposable RegisterDcsHandler(FunctionIdentifier id, Func<string, IParams, bool> callback) =>
        _inputHandler.RegisterDcsHandler(id, callback);

    /// <summary>Registers a CSI handler. Dispose the result to remove it.</summary>
    public IDisposable RegisterCsiHandler(FunctionIdentifier id, CsiHandler callback) =>
        _inputHandler.RegisterCsiHandler(id, callback);

    /// <summary>Registers an OSC handler. Dispose the result to remove it.</summary>
    public IDisposable RegisterOscHandler(int ident, Func<string, bool> callback) =>
        _inputHandler.RegisterOscHandler(ident, callback);

    /// <summary>Resets the terminal: attributes, buffers, character sets, modes and mouse reporting.</summary>
    public void Reset()
    {
        _inputHandler.Reset();
        BufferService.Reset();
        CharsetService.Reset();
        CoreService.Reset();
        CoreMouseService.Reset();
    }

    private void Forward<T>(IEvent<T> source, Emitter<T> target) => Register(source.Subscribe(target.Fire));

    private void HandleWindowsPtyOptionChange()
    {
        bool value = false;
        WindowsPtyOptions? windowsPty = OptionsService.RawOptions.WindowsPty;
        if (windowsPty is not null && windowsPty.BuildNumber is not null && windowsPty.BuildNumber is not null)
        {
            value = windowsPty.Backend == "conpty" && windowsPty.BuildNumber < 21376;
        }
        else if (OptionsService.RawOptions.WindowsMode)
        {
            value = true;
        }

        if (value)
        {
            EnableWindowsWrappingHeuristics();
        }
        else
        {
            _windowsWrappingHeuristics.Clear();
        }
    }

    private void EnableWindowsWrappingHeuristics()
    {
        if (_windowsWrappingHeuristics.Value is null)
        {
            List<IDisposable> disposables =
            [
                OnLineFeed.Subscribe(_ => WindowsMode.UpdateWindowsModeWrappedState(BufferService)),
                RegisterCsiHandler(new FunctionIdentifier(Final: "H"), _ =>
                {
                    WindowsMode.UpdateWindowsModeWrappedState(BufferService);
                    return false;
                }),
            ];
            _windowsWrappingHeuristics.Value = Disposables.Create(() =>
            {
                foreach (IDisposable d in disposables)
                {
                    d.Dispose();
                }
            });
        }
    }
}
