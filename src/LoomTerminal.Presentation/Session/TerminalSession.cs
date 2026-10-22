using System.Text;
using System.Threading.Channels;
using LoomTerminal.Core;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Rendering;
using LoomTerminal.Presentation.Selection;
using LoomTerminal.Presentation.Theming;
using LoomTerminal.Presentation.Viewport;
using LoomTerminal.Pty;

namespace LoomTerminal.Presentation.Session;

/// <summary>
/// One terminal: an emulator, the shell behind a pseudo terminal and the parser thread that moves data both ways.
/// Output of the shell and every action on the emulator go through one ordered work queue, so they never interleave.
/// </summary>
/// <remarks>
/// Threading: every public method is thread-safe and returns immediately. The events are raised on the parser thread;
/// backends marshal them to the UI thread. Only the parser thread touches the emulator.
/// </remarks>
public sealed class TerminalSession : IDisposable
{
    private const int QueueCapacity = 64;
    private static readonly TimeSpan IdleWorkInterval = TimeSpan.FromMilliseconds(50);

    private readonly IPtyConnection _connection;
    private readonly TerminalCore _core;
    private readonly Channel<TerminalWorkItem> _queue =
        Channel.CreateBounded<TerminalWorkItem>(new BoundedChannelOptions(QueueCapacity) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
    private readonly CancellationTokenSource _cts = new();
    private readonly Thread _thread;
    private readonly KeyInputHandler _keyHandler = new();
    private readonly RenderFrameBuilder _builder;
    private readonly ViewportController _viewport;
    private readonly SelectionService _selection;
    private readonly Lock _frameLock = new();
    private readonly Lock _postLock = new();
    private RenderFrame? _latestFrame;
    private bool _frameDirty;
    private bool _idleWorkScheduled;
    private Task _postTail = Task.CompletedTask;
    private readonly CursorBlinkState _cursorBlink = new(TimeProvider.System);
    private long _framesPublished;
    private volatile bool _focused = true;
    private int _disposed;
    private volatile ModesSnapshot _modes = new(false, false, false);

    private TerminalSession(IPtyConnection connection, int columns, int rows, TerminalSessionOptions options)
    {
        _connection = connection;
        _core = new TerminalCore(CreateCoreOptions(columns, rows, options));
        _core.OnData.Subscribe(data => _connection.Write(Encoding.UTF8.GetBytes(data)));
        _core.OnBinary.Subscribe(data => _connection.Write(Encoding.Latin1.GetBytes(data)));
        _core.OnTitleChange.Subscribe(title => TitleChanged?.Invoke(title));
        _core.OnBell.Subscribe(_ => Bell?.Invoke());
        Palette = new ThemePalette(options.Theme);
        _builder = new RenderFrameBuilder(_core, Palette);
        _viewport = new ViewportController(_core);
        _selection = new SelectionService(_core.BufferService, _core.CoreService, _core.OptionsService, OperatingSystem.IsWindows(), OperatingSystem.IsMacOS());
        _core.OnColor.Subscribe(requests =>
        {
            foreach (var reply in Palette.Apply(requests))
            {
                _core.CoreService.TriggerDataEvent(reply);
            }
        });

        _thread = new Thread(RunParser) { IsBackground = true, Name = "LoomTerminal parser" };
        _thread.Start();
        _ = PumpOutputAsync();
    }

    /// <summary>The resolved colors, including the OSC overrides; touch it on the parser thread only.</summary>
    internal ThemePalette Palette { get; }

    /// <summary>
    /// Raised on the parser thread when a new frame is available and the previous one was taken: once per transition,
    /// not once per frame. Backends then call <see cref="TryTakeFrame"/> from their per-frame callback.
    /// </summary>
    public event Action? FrameAvailable;

    /// <summary>Number of frames published so far (tests wait on it).</summary>
    internal long FramesPublished => Interlocked.Read(ref _framesPublished);

    /// <summary>The selection logic; touch it on the parser thread only.</summary>
    internal SelectionService Selection => _selection;

    /// <summary>The blink phase of the cursor: backends ask it each frame and schedule a frame at its next change.</summary>
    internal CursorBlinkState CursorBlink => _cursorBlink;

    /// <summary>Raised when the shell exited and all its output was processed (parser thread). The argument is the exit code.</summary>
    public event Action<int>? Exited;

    /// <summary>Raised when the title changes (parser thread).</summary>
    public event Action<string>? TitleChanged;

    /// <summary>Raised when the application rings the bell (parser thread).</summary>
    public event Action? Bell;

    /// <summary>
    /// Lets the host claim a key before the terminal handles it (for example a shortcut table); return
    /// <see langword="true"/> to claim it. Called on the thread that calls <see cref="SendKey"/>, never for AltGr keys.
    /// </summary>
    public Func<TerminalKeyEvent, bool>? KeyPreview
    {
        get => _keyHandler.KeyPreview;
        set => _keyHandler.KeyPreview = value;
    }

    /// <summary>Starts a shell behind a pseudo terminal and the session that drives it.</summary>
    /// <param name="pty">What to start; its size is the initial size of the terminal.</param>
    /// <param name="options">Session options; defaults when <see langword="null"/>.</param>
    /// <returns>The running session.</returns>
    public static TerminalSession Start(PtyStartOptions pty, TerminalSessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(pty);
        options ??= new TerminalSessionOptions();
        var environment = new Dictionary<string, string?>(pty.Environment ?? new Dictionary<string, string?>());
        environment.TryAdd("TERM_PROGRAM", options.TermProgram);
        var connection = PseudoTerminal.Start(pty with { Environment = environment });
        return new TerminalSession(connection, pty.Columns, pty.Rows, options);
    }

    /// <summary>Creates a session over an existing connection (tests, custom transports).</summary>
    /// <param name="connection">The connection; the session disposes it.</param>
    /// <param name="columns">Initial number of columns.</param>
    /// <param name="rows">Initial number of rows.</param>
    /// <param name="options">Session options; defaults when <see langword="null"/>.</param>
    /// <returns>The running session.</returns>
    public static TerminalSession Attach(IPtyConnection connection, int columns, int rows, TerminalSessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return new TerminalSession(connection, columns, rows, options ?? new TerminalSessionOptions());
    }

    /// <summary>Takes the latest frame if it was not taken yet. Call it from the UI thread, after <see cref="FrameAvailable"/>.</summary>
    /// <returns>The frame, or <see langword="null"/> when there is nothing new. A frame is never modified after it is published.</returns>
    public RenderFrame? TryTakeFrame()
    {
        lock (_frameLock)
        {
            if (!_frameDirty)
            {
                return null;
            }

            _frameDirty = false;
            return _latestFrame;
        }
    }

    /// <summary>
    /// Tells the session whether the terminal has the focus: an unfocused terminal draws the inactive cursor and
    /// selection colors, and its cursor stops blinking.
    /// </summary>
    /// <param name="focused">Whether the terminal has the focus.</param>
    public void SetFocus(bool focused)
    {
        _focused = focused;
        if (focused)
        {
            _cursorBlink.Resume();
        }
        else
        {
            _cursorBlink.Pause();
        }

        Post(_ => { });
    }

    /// <summary>Scrolls the viewport by <paramref name="lines"/> (negative scrolls up).</summary>
    /// <param name="lines">Number of lines.</param>
    public void ScrollLines(int lines) => Post(core => core.ScrollLines(lines));

    /// <summary>Scrolls the viewport by <paramref name="pages"/> pages of rows - 1 lines (negative scrolls up).</summary>
    /// <param name="pages">Number of pages.</param>
    public void ScrollPages(int pages) => Post(core => core.ScrollPages(pages));

    /// <summary>Scrolls the viewport to the oldest line of the scrollback.</summary>
    public void ScrollToTop() => Post(core => core.ScrollToTop());

    /// <summary>Scrolls the viewport to the bottom, where it follows the output.</summary>
    public void ScrollToBottom() => Post(core => core.ScrollToBottom());

    /// <summary>Scrolls the viewport so that <paramref name="line"/> of the buffer is its first row.</summary>
    /// <param name="line">Line of the buffer, zero based.</param>
    public void ScrollToLine(int line) => Post(core => core.ScrollToLine(line));

    /// <summary>
    /// Handles a wheel event: reports it to an application that tracks the mouse, turns it into an arrow key in a buffer
    /// without scrollback (alternate screen), or scrolls the viewport.
    /// </summary>
    /// <param name="ev">The wheel event.</param>
    /// <param name="cellHeight">Height of a cell in device pixels.</param>
    public void HandleWheel(TerminalPointerEvent ev, double cellHeight)
    {
        ArgumentNullException.ThrowIfNull(ev);
        Post(_ => _viewport.HandleWheel(ev, cellHeight));
    }

    /// <summary>Selects all the text of the buffer, scrollback included.</summary>
    public void SelectAll() => Post(_ => _selection.SelectAll());

    /// <summary>Clears the selection.</summary>
    public void ClearSelection() => Post(_ => _selection.ClearSelection());

    /// <summary>Gets the selected text (empty when nothing is selected); lines are joined with CRLF on Windows and LF elsewhere.</summary>
    /// <returns>The text.</returns>
    public Task<string> GetSelectionTextAsync() => RunOnParserThreadAsync(_ => _selection.SelectionText);

    /// <summary>Sends typed text (character input, IME commit) to the shell as UTF-8, unchanged.</summary>
    /// <param name="text">The text.</param>
    public void SendText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > 0)
        {
            _cursorBlink.Restart();
            Post(core => core.Input(text));
        }
    }

    /// <summary>Decides what a key event does and, when it is a VT sequence, sends it.</summary>
    /// <param name="ev">The key event.</param>
    /// <returns>The decision; the backend acts on copy, paste and text input itself.</returns>
    public KeyInputOutcome SendKey(TerminalKeyEvent ev)
    {
        ArgumentNullException.ThrowIfNull(ev);
        var modes = _modes;
        var result = _keyHandler.Handle(ev, new KeyContext(modes.ApplicationCursorKeys, HasSelection: false, OperatingSystem.IsMacOS()));
        if (result is { Outcome: KeyInputOutcome.Sent, Data: { } data })
        {
            _cursorBlink.Restart();
            Post(core => core.Input(data));
        }

        return result.Outcome;
    }

    /// <summary>Sends pasted text: line endings normalized, bracketed when the application asked for it.</summary>
    /// <param name="text">The pasted text.</param>
    public void Paste(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _cursorBlink.Restart();
        Post(core => core.Input(
            PasteEncoding.PreparePaste(
                text,
                core.CoreService.DecPrivateModes.BracketedPasteMode,
                core.Options.IgnoreBracketedPasteMode)));
    }

    /// <summary>Resizes the emulator and the pseudo terminal. Sizes under 2 columns or 1 row are ignored.</summary>
    /// <param name="columns">Number of columns.</param>
    /// <param name="rows">Number of rows.</param>
    public void Resize(int columns, int rows)
    {
        if (columns < 2 || rows < 1)
        {
            return;
        }

        Post(core =>
        {
            core.Resize(columns, rows);
            _connection.Resize(columns, rows);
        });
    }

    /// <summary>
    /// Writes VT text to the emulator only, as if the shell had printed it (host messages such as an exit line).
    /// Works after the shell exited.
    /// </summary>
    /// <param name="vtText">The text, which may hold VT sequences.</param>
    public void WriteToDisplay(string vtText)
    {
        ArgumentNullException.ThrowIfNull(vtText);
        Post(core => core.Write(vtText));
    }

    /// <summary>Disposes the pseudo terminal and stops the parser thread. Safe to call more than once.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _cts.Cancel();
        _queue.Writer.TryComplete();
        _connection.Dispose();
        if (Thread.CurrentThread != _thread)
        {
            _thread.Join();
        }

        _selection.Dispose();
        _viewport.Dispose();
        _builder.Dispose();
        _core.Dispose();
        _cts.Dispose();
    }

    /// <summary>Runs <paramref name="func"/> on the parser thread, after everything queued before it.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="func">The function to run.</param>
    /// <returns>A task with the result.</returns>
    internal Task<T> RunOnParserThreadAsync<T>(Func<TerminalCore, T> func)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(core =>
        {
            try
            {
                completion.SetResult(func(core));
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task;
    }

    private static TerminalOptions CreateCoreOptions(int columns, int rows, TerminalSessionOptions options) => new()
    {
        Cols = columns,
        Rows = rows,
        Scrollback = options.Scrollback,
        ScrollOnUserInput = options.ScrollOnUserInput,
        CursorStyle = options.CursorStyle switch
        {
            TerminalCursorStyle.Underline => CursorStyle.Underline,
            TerminalCursorStyle.Bar => CursorStyle.Bar,
            _ => CursorStyle.Block,
        },
        CursorBlink = options.CursorBlink,
        TabStopWidth = options.TabStopWidth,
        WordSeparator = options.WordSeparator,
        WindowsPty = options.WindowsPtyBuildNumber is { } build ? new WindowsPtyOptions("conpty", build) : new WindowsPtyOptions(),
    };

    private void RunParser()
    {
        try
        {
            new TerminalWriter(_core).Run(_queue.Reader, _ => AfterBatch(), _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Dispose cancelled the loop.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("LoomTerminal parser thread failed: " + ex);
        }
    }

    private void AfterBatch()
    {
        PublishModes();
        PublishFrame();
        ScheduleIdleWork();
    }

    private void PublishFrame()
    {
        var view = new ViewState
        {
            IsFocused = _focused,
            CursorBlinkVisible = _cursorBlink.IsVisible(TimeProvider.System.GetTimestamp()),
            SelectionStart = _selection.SelectionStart,
            SelectionEnd = _selection.SelectionEnd,
            SelectionColumnMode = _selection.ActiveSelectionMode == SelectionMode.Column,
        };
        var frame = _builder.Build(view);
        Interlocked.Increment(ref _framesPublished);
        bool raise;
        lock (_frameLock)
        {
            _latestFrame = frame;
            raise = !_frameDirty;
            _frameDirty = true;
        }

        if (raise)
        {
            FrameAvailable?.Invoke();
        }
    }

    // When the queue is empty after a batch, clean up the memory of a resized buffer, at most once per 50 ms.
    private void ScheduleIdleWork()
    {
        if (_idleWorkScheduled || _queue.Reader.Count > 0 || !_core.Buffers.Active.MemoryCleanupPending)
        {
            return;
        }

        _idleWorkScheduled = true;
        _ = RunIdleWorkAsync();
    }

    private async Task RunIdleWorkAsync()
    {
        try
        {
            await Task.Delay(IdleWorkInterval, _cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Post(core =>
        {
            _idleWorkScheduled = false;
            if (core.Buffers.Active.MemoryCleanupPending)
            {
                core.Buffers.Active.BatchedMemoryCleanup();
            }
        });
    }

    // The key decision only needs the current modes: the parser thread publishes them after each batch.
    private void PublishModes() => _modes = new ModesSnapshot(
        _core.CoreService.DecPrivateModes.ApplicationCursorKeys,
        _core.CoreService.DecPrivateModes.BracketedPasteMode,
        _core.CoreMouseService.ActiveProtocol != "NONE");

    private async Task PumpOutputAsync()
    {
        try
        {
            await foreach (var chunk in _connection.Output.ReadAllAsync(_cts.Token).ConfigureAwait(false))
            {
                await _queue.Writer.WriteAsync(TerminalWorkItem.FromOutput(chunk), _cts.Token).ConfigureAwait(false);
            }

            var exitCode = await _connection.Exited.ConfigureAwait(false);
            Post(_ => Exited?.Invoke(exitCode));
        }
        catch (Exception ex) when (ex is OperationCanceledException or ChannelClosedException or ObjectDisposedException)
        {
            // The session was disposed.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("LoomTerminal output pump failed: " + ex);
        }
    }

    // Fire and forget: the queue is bounded, but UI calls must never block.
    private void Post(Action<TerminalCore> action)
    {
        lock (_postLock)
        {
            _postTail = PostAsync(_postTail, action);
        }
    }

    // Chained on the previous post: calls made in order from one thread reach the queue in that order even when it is full.
    private async Task PostAsync(Task previous, Action<TerminalCore> action)
    {
        try
        {
            await previous.ConfigureAwait(false);
            await _queue.Writer.WriteAsync(TerminalWorkItem.FromAction(action), _cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or ChannelClosedException or ObjectDisposedException)
        {
            // The session was disposed.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("LoomTerminal could not queue an action: " + ex);
        }
    }

    private sealed record ModesSnapshot(bool ApplicationCursorKeys, bool BracketedPaste, bool MouseTracking);
}
