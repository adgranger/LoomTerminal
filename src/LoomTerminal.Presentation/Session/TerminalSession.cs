using System.Text;
using System.Threading.Channels;
using LoomTerminal.Core;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Theming;
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

    private readonly IPtyConnection _connection;
    private readonly TerminalCore _core;
    private readonly Channel<TerminalWorkItem> _queue =
        Channel.CreateBounded<TerminalWorkItem>(new BoundedChannelOptions(QueueCapacity) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
    private readonly CancellationTokenSource _cts = new();
    private readonly Thread _thread;
    private readonly KeyInputHandler _keyHandler = new();
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

    /// <summary>Sends typed text (character input, IME commit) to the shell as UTF-8, unchanged.</summary>
    /// <param name="text">The text.</param>
    public void SendText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > 0)
        {
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
            Post(core => core.Input(data));
        }

        return result.Outcome;
    }

    /// <summary>Sends pasted text: line endings normalized, bracketed when the application asked for it.</summary>
    /// <param name="text">The pasted text.</param>
    public void Paste(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
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
            new TerminalWriter(_core).Run(_queue.Reader, _ => PublishModes(), _cts.Token);
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
    private void Post(Action<TerminalCore> action) => _ = PostAsync(action);

    private async Task PostAsync(Action<TerminalCore> action)
    {
        try
        {
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
