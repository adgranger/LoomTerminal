// Ported from xtermjs/xterm.js: src/common/services/CoreMouseService.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Services;

/// <summary>
/// Provides mouse tracking reports with different protocols and encodings. Protocols: NONE (default), X10, VT200,
/// DRAG, ANY. Encodings: DEFAULT, SGR, SGR_PIXELS. Custom ones can be added; setting <see cref="ActiveProtocol"/>
/// raises <see cref="OnProtocolChange"/> with the events to track.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class CoreMouseService : Disposable, ICoreMouseService
{
    private const int ModifierShift = 4;
    private const int ModifierAlt = 8;
    private const int ModifierCtrl = 16;

    private static readonly (string Name, CoreMouseProtocol Protocol)[] DefaultProtocols =
    [
        // Events: none. Modifiers: none.
        ("NONE", new CoreMouseProtocol(CoreMouseEventType.None, _ => false)),

        // Events: mousedown. Modifiers: none.
        ("X10", new CoreMouseProtocol(CoreMouseEventType.Down, e =>
        {
            // no wheel, no move, no up
            if (e.Button == CoreMouseButton.Wheel || e.Action != CoreMouseAction.Down)
            {
                return false;
            }

            // no modifiers
            e.Ctrl = false;
            e.Alt = false;
            e.Shift = false;
            return true;
        })),

        // Events: mousedown / mouseup / wheel. Modifiers: all.
        ("VT200", new CoreMouseProtocol(
            CoreMouseEventType.Down | CoreMouseEventType.Up | CoreMouseEventType.Wheel,
            e => e.Action != CoreMouseAction.Move)),

        // Events: mousedown / mouseup / wheel / mousedrag. Modifiers: all.
        ("DRAG", new CoreMouseProtocol(
            CoreMouseEventType.Down | CoreMouseEventType.Up | CoreMouseEventType.Wheel | CoreMouseEventType.Drag,
            e => !(e.Action == CoreMouseAction.Move && e.Button == CoreMouseButton.None))),

        // Events: all mouse related events. Modifiers: all.
        ("ANY", new CoreMouseProtocol(
            CoreMouseEventType.Down | CoreMouseEventType.Up | CoreMouseEventType.Wheel | CoreMouseEventType.Drag
            | CoreMouseEventType.Move,
            _ => true)),
    ];

    private static readonly (string Name, CoreMouseEncoding Encoding)[] DefaultEncodings =
    [
        // CSI M Pb Px Py. Single byte encoding for coords and event code; can encode values up to 223 (1-based).
        ("DEFAULT", e =>
        {
            int[] parameters = [EventCode(e, false) + 32, e.Col + 32, e.Row + 32];

            // suppress mouse report if we exceed addressable range
            // Note this is handled differently by emulators
            // - xterm:         sends 0;0 coords instead
            // - vte, konsole:  no report
            if (parameters[0] > 255 || parameters[1] > 255 || parameters[2] > 255)
            {
                return string.Empty;
            }

            return $"\x1b[M{(char)parameters[0]}{(char)parameters[1]}{(char)parameters[2]}";
        }),

        // CSI < Pb ; Px ; Py M|m. No encoding limitation; can report the button on release.
        ("SGR", e => $"\x1b[<{EventCode(e, true)};{e.Col};{e.Row}{SgrFinal(e)}"),
        ("SGR_PIXELS", e => $"\x1b[<{EventCode(e, true)};{e.X};{e.Y}{SgrFinal(e)}"),
    ];

    private readonly IBufferService _bufferService;
    private readonly ICoreService _coreService;
    private readonly IOptionsService _optionsService;
    private readonly Dictionary<string, CoreMouseProtocol> _protocols = [];
    private readonly Dictionary<string, CoreMouseEncoding> _encodings = [];
    private readonly List<string> _protocolNames = [];
    private readonly List<string> _encodingNames = [];
    private readonly Emitter<CoreMouseEventType> _onProtocolChange;
    private string _activeProtocol = string.Empty;
    private string _activeEncoding = string.Empty;
    private CoreMouseEvent? _lastEvent;
    private double _wheelPartialScroll;

    /// <summary>Creates the service with the default protocols and encodings; NONE and DEFAULT are active.</summary>
    public CoreMouseService(IBufferService bufferService, ICoreService coreService, IOptionsService optionsService)
    {
        _bufferService = bufferService;
        _coreService = coreService;
        _optionsService = optionsService;
        _onProtocolChange = Register(new Emitter<CoreMouseEventType>());

        // register default protocols and encodings
        foreach ((string name, CoreMouseProtocol protocol) in DefaultProtocols)
        {
            AddProtocol(name, protocol);
        }

        foreach ((string name, CoreMouseEncoding encoding) in DefaultEncodings)
        {
            AddEncoding(name, encoding);
        }

        // call reset to set defaults
        Reset();
    }

    /// <summary>The registered protocol names, in registration order.</summary>
    internal IReadOnlyList<string> ProtocolNames => _protocolNames;

    /// <summary>The registered encoding names, in registration order.</summary>
    internal IReadOnlyList<string> EncodingNames => _encodingNames;

    /// <inheritdoc />
    public IEvent<CoreMouseEventType> OnProtocolChange => _onProtocolChange.Event;

    /// <inheritdoc />
    public string ActiveProtocol
    {
        get => _activeProtocol;
        set
        {
            if (!_protocols.TryGetValue(value, out CoreMouseProtocol? protocol))
            {
                throw new InvalidOperationException($"unknown protocol \"{value}\"");
            }

            _activeProtocol = value;
            _onProtocolChange.Fire(protocol.Events);
        }
    }

    /// <inheritdoc />
    public string ActiveEncoding
    {
        get => _activeEncoding;
        set
        {
            if (!_encodings.ContainsKey(value))
            {
                throw new InvalidOperationException($"unknown encoding \"{value}\"");
            }

            _activeEncoding = value;
        }
    }

    /// <inheritdoc />
    public bool AreMouseEventsActive => _protocols[_activeProtocol].Events != CoreMouseEventType.None;

    /// <inheritdoc />
    public void AddProtocol(string name, CoreMouseProtocol protocol)
    {
        if (_protocols.TryAdd(name, protocol))
        {
            _protocolNames.Add(name);
        }
        else
        {
            _protocols[name] = protocol;
        }
    }

    /// <inheritdoc />
    public void AddEncoding(string name, CoreMouseEncoding encoding)
    {
        if (_encodings.TryAdd(name, encoding))
        {
            _encodingNames.Add(name);
        }
        else
        {
            _encodings[name] = encoding;
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        ActiveProtocol = "NONE";
        ActiveEncoding = "DEFAULT";
        _lastEvent = null;
        _wheelPartialScroll = 0;
    }

    /// <summary>
    /// Processes a wheel event, accounting for partial scrolls for trackpad, mouse scrolls. This prevents
    /// hyper-sensitive scrolling in the alternate buffer.
    /// </summary>
    public int ConsumeWheelEvent(double deltaY, WheelDeltaMode mode, bool shift, bool alt, bool ctrl, double cellHeight, double dpr)
    {
        // Do nothing if it's not a vertical scroll event
        if (deltaY == 0 || shift)
        {
            return 0;
        }

        double targetWheelEventPixels = cellHeight / dpr;
        double amount = ApplyScrollModifier(deltaY, shift, alt, ctrl);

        if (mode == WheelDeltaMode.Pixel)
        {
            amount /= targetWheelEventPixels;

            bool isLikelyTrackpad = Math.Abs(deltaY) < 50;
            if (isLikelyTrackpad)
            {
                amount *= 0.3;
            }

            _wheelPartialScroll += amount;
            amount = Math.Floor(Math.Abs(_wheelPartialScroll)) * (_wheelPartialScroll > 0 ? 1 : -1);

            // JS '%' keeps the sign of the dividend, like C#.
            _wheelPartialScroll %= 1;
        }
        else if (mode == WheelDeltaMode.Page)
        {
            amount *= _bufferService.Rows;
        }

        return (int)amount;
    }

    /// <inheritdoc />
    public bool TriggerMouseEvent(CoreMouseEvent e)
    {
        // range check for col/row
        if (e.Col < 0 || e.Col >= _bufferService.Cols || e.Row < 0 || e.Row >= _bufferService.Rows)
        {
            return false;
        }

        // filter nonsense combinations of button + action
        if (e.Button == CoreMouseButton.Wheel && e.Action == CoreMouseAction.Move)
        {
            return false;
        }

        if (e.Button == CoreMouseButton.None && e.Action != CoreMouseAction.Move)
        {
            return false;
        }

        if (e.Button != CoreMouseButton.Wheel && (e.Action == CoreMouseAction.Left || e.Action == CoreMouseAction.Right))
        {
            return false;
        }

        // report 1-based coords
        e.Col++;
        e.Row++;

        // debounce move events at grid or pixel level
        if (e.Action == CoreMouseAction.Move && _lastEvent is not null
            && EqualEvents(_lastEvent, e, _activeEncoding == "SGR_PIXELS"))
        {
            return false;
        }

        // apply protocol restrictions
        if (!_protocols[_activeProtocol].Restrict(e))
        {
            return false;
        }

        // encode report and send
        string report = _encodings[_activeEncoding](e);
        if (report.Length > 0)
        {
            // always send DEFAULT as binary data
            if (_activeEncoding == "DEFAULT")
            {
                _coreService.TriggerBinaryEvent(report);
            }
            else
            {
                _coreService.TriggerDataEvent(report, true);
            }
        }

        _lastEvent = e;

        return true;
    }

    /// <inheritdoc />
    public Dictionary<string, bool> ExplainEvents(CoreMouseEventType events) => new()
    {
        ["down"] = (events & CoreMouseEventType.Down) != 0,
        ["up"] = (events & CoreMouseEventType.Up) != 0,
        ["drag"] = (events & CoreMouseEventType.Drag) != 0,
        ["move"] = (events & CoreMouseEventType.Move) != 0,
        ["wheel"] = (events & CoreMouseEventType.Wheel) != 0,
    };

    // helper for default encoders to generate the event code.
    private static int EventCode(CoreMouseEvent e, bool isSgr)
    {
        int code = (e.Ctrl ? ModifierCtrl : 0) | (e.Shift ? ModifierShift : 0) | (e.Alt ? ModifierAlt : 0);
        int button = (int)e.Button;
        if (e.Button == CoreMouseButton.Wheel)
        {
            code |= 64;
            code |= (int)e.Action;
        }
        else
        {
            code |= button & 3;
            if ((button & 4) != 0)
            {
                code |= 64;
            }

            if ((button & 8) != 0)
            {
                code |= 128;
            }

            if (e.Action == CoreMouseAction.Move)
            {
                code |= (int)CoreMouseAction.Move;
            }
            else if (e.Action == CoreMouseAction.Up && !isSgr)
            {
                // special case - only SGR can report button on release
                // all others have to go with NONE
                code |= (int)CoreMouseButton.None;
            }
        }

        return code;
    }

    private static char SgrFinal(CoreMouseEvent e) =>
        e.Action == CoreMouseAction.Up && e.Button != CoreMouseButton.Wheel ? 'm' : 'M';

    private static bool EqualEvents(CoreMouseEvent e1, CoreMouseEvent e2, bool pixels)
    {
        if (pixels)
        {
            if (e1.X != e2.X || e1.Y != e2.Y)
            {
                return false;
            }
        }
        else if (e1.Col != e2.Col || e1.Row != e2.Row)
        {
            return false;
        }

        return e1.Button == e2.Button && e1.Action == e2.Action
            && e1.Ctrl == e2.Ctrl && e1.Alt == e2.Alt && e1.Shift == e2.Shift;
    }

    private double ApplyScrollModifier(double amount, bool shift, bool alt, bool ctrl)
    {
        // Multiply the scroll speed when the modifier key is pressed
        TerminalOptions options = _optionsService.RawOptions;
        if (alt || ctrl || shift)
        {
            return amount * options.FastScrollSensitivity * options.ScrollSensitivity;
        }

        return amount * options.ScrollSensitivity;
    }
}
