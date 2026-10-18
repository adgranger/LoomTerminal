// Ported from xtermjs/xterm.js: src/common/services/CoreService.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Services;

/// <summary>The terminal modes and the events that carry data toward the pty.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class CoreService : Disposable, ICoreService
{
    private readonly IBufferService _bufferService;
    private readonly ILogService _logService;
    private readonly IOptionsService _optionsService;
    private readonly Emitter<string> _onData;
    private readonly Emitter<EventArgs> _onUserInput;
    private readonly Emitter<string> _onBinary;
    private readonly Emitter<EventArgs> _onRequestScrollToBottom;

    /// <summary>Creates the service with the default modes.</summary>
    public CoreService(IBufferService bufferService, ILogService logService, IOptionsService optionsService)
    {
        _bufferService = bufferService;
        _logService = logService;
        _optionsService = optionsService;
        _onData = Register(new Emitter<string>());
        _onUserInput = Register(new Emitter<EventArgs>());
        _onBinary = Register(new Emitter<string>());
        _onRequestScrollToBottom = Register(new Emitter<EventArgs>());
        Modes = Modes.CreateDefault();
        DecPrivateModes = DecPrivateModes.CreateDefault();
    }

    /// <inheritdoc />
    public bool IsCursorInitialized { get; set; }

    /// <inheritdoc />
    public bool IsCursorHidden { get; set; }

    /// <inheritdoc />
    public Modes Modes { get; private set; }

    /// <inheritdoc />
    public DecPrivateModes DecPrivateModes { get; private set; }

    /// <inheritdoc />
    public IEvent<string> OnData => _onData.Event;

    /// <inheritdoc />
    public IEvent<EventArgs> OnUserInput => _onUserInput.Event;

    /// <inheritdoc />
    public IEvent<string> OnBinary => _onBinary.Event;

    /// <inheritdoc />
    public IEvent<EventArgs> OnRequestScrollToBottom => _onRequestScrollToBottom.Event;

    /// <inheritdoc />
    public void Reset()
    {
        Modes = Modes.CreateDefault();
        DecPrivateModes = DecPrivateModes.CreateDefault();
    }

    /// <inheritdoc />
    public void TriggerDataEvent(string data, bool wasUserInput = false)
    {
        // Prevents all events to pty process if stdin is disabled
        if (_optionsService.RawOptions.DisableStdin)
        {
            return;
        }

        // Input is being sent to the terminal, the terminal should focus the prompt.
        Core.Buffer.Buffer buffer = _bufferService.Buffer;
        if (wasUserInput && _optionsService.RawOptions.ScrollOnUserInput && buffer.YBase != buffer.YDisp)
        {
            _onRequestScrollToBottom.Fire(EventArgs.Empty);
        }

        // Fire OnUserInput so listeners can react as well (eg. clear selection)
        if (wasUserInput)
        {
            _onUserInput.Fire(EventArgs.Empty);
        }

        // Fire OnData API
        _logService.Debug($"sending data \"{data}\"");
        if (_logService.Level <= LogLevel.Trace)
        {
            _logService.Trace($"sending data (codes) {Codes(data)}");
        }

        _onData.Fire(data);
    }

    /// <inheritdoc />
    public void TriggerBinaryEvent(string data)
    {
        if (_optionsService.RawOptions.DisableStdin)
        {
            return;
        }

        _logService.Debug($"sending binary \"{data}\"");
        if (_logService.Level <= LogLevel.Trace)
        {
            _logService.Trace($"sending binary (codes) {Codes(data)}");
        }

        _onBinary.Fire(data);
    }

    private static string Codes(string data) => string.Join(",", data.Select(c => (int)c));
}
