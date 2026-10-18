// Ported from xtermjs/xterm.js: src/common/services/CoreMouseService.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;
using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.Services;

public sealed class CoreMouseServiceTests : IDisposable
{
    private readonly MockBufferService _bufferService = new(300, 100);
    private readonly MockCoreService _coreService = new();
    private readonly MockOptionsService _optionsService = new();
    private readonly CoreMouseService _cms;
    private readonly List<string> _reports = [];

    public CoreMouseServiceTests()
    {
        _cms = new CoreMouseService(_bufferService, _coreService, _optionsService);
        _coreService.DataEventHandler = (data, _) => _reports.Add(data);
        _coreService.BinaryEventHandler = _reports.Add;
    }

    public void Dispose()
    {
        _cms.Dispose();
        _optionsService.Dispose();
        _coreService.Dispose();
        _bufferService.Dispose();
    }

    private static int[] ToBytes(string? s) => s is null ? [] : [.. s.Select(c => (int)c)];

    private string? PopReport()
    {
        if (_reports.Count == 0)
        {
            return null;
        }

        string last = _reports[^1];
        _reports.RemoveAt(_reports.Count - 1);
        return last;
    }

    // xterm.js: 'CoreMouseService' › 'init'
    [Fact]
    public void A_new_service_uses_the_DEFAULT_encoding_and_the_NONE_protocol()
    {
        Assert.Equal("DEFAULT", _cms.ActiveEncoding);
        Assert.Equal("NONE", _cms.ActiveProtocol);
    }

    // xterm.js: 'CoreMouseService' › 'default protocols - NONE, X10, VT200, DRAG, ANY'
    [Fact]
    public void The_default_protocols_are_NONE_X10_VT200_DRAG_and_ANY()
    {
        Assert.Equal<string>(["NONE", "X10", "VT200", "DRAG", "ANY"], _cms.ProtocolNames);
    }

    // xterm.js: 'CoreMouseService' › 'default encodings - DEFAULT, SGR'
    [Fact]
    public void The_default_encodings_are_DEFAULT_SGR_and_SGR_PIXELS()
    {
        Assert.Equal<string>(["DEFAULT", "SGR", "SGR_PIXELS"], _cms.EncodingNames);
    }

    // xterm.js: 'CoreMouseService' › 'protocol/encoding setter, reset'
    [Fact]
    public void Protocol_and_encoding_setters_and_Reset_work_and_unknown_names_throw()
    {
        _cms.ActiveEncoding = "SGR";
        _cms.ActiveProtocol = "ANY";
        Assert.Equal("SGR", _cms.ActiveEncoding);
        Assert.Equal("ANY", _cms.ActiveProtocol);
        _cms.Reset();
        Assert.Equal("DEFAULT", _cms.ActiveEncoding);
        Assert.Equal("NONE", _cms.ActiveProtocol);
        Assert.Equal("unknown encoding \"xyz\"", Assert.Throws<InvalidOperationException>(() => _cms.ActiveEncoding = "xyz").Message);
        Assert.Equal("unknown protocol \"xyz\"", Assert.Throws<InvalidOperationException>(() => _cms.ActiveProtocol = "xyz").Message);
    }

    // xterm.js: 'CoreMouseService' › 'addEncoding'
    [Fact]
    public void AddEncoding_registers_an_encoding_that_can_be_activated()
    {
        _cms.AddEncoding("XYZ", _ => string.Empty);
        _cms.ActiveEncoding = "XYZ";
        Assert.Equal("XYZ", _cms.ActiveEncoding);
    }

    // xterm.js: 'CoreMouseService' › 'addProtocol'
    [Fact]
    public void AddProtocol_registers_a_protocol_that_can_be_activated()
    {
        _cms.AddProtocol("XYZ", new CoreMouseProtocol(CoreMouseEventType.None, _ => false));
        _cms.ActiveProtocol = "XYZ";
        Assert.Equal("XYZ", _cms.ActiveProtocol);
    }

    // xterm.js: 'CoreMouseService' › 'onProtocolChange'
    [Fact]
    public void OnProtocolChange_reports_the_events_wanted_by_the_new_protocol()
    {
        List<CoreMouseEventType> wantedEvents = [];
        _cms.OnProtocolChange.Subscribe(wantedEvents.Add);
        _cms.ActiveProtocol = "NONE";
        Assert.Equal<CoreMouseEventType>([CoreMouseEventType.None], wantedEvents);
        _cms.ActiveProtocol = "ANY";
        Assert.Equal<CoreMouseEventType>(
            [
                CoreMouseEventType.None,
                CoreMouseEventType.Down | CoreMouseEventType.Up | CoreMouseEventType.Wheel | CoreMouseEventType.Drag | CoreMouseEventType.Move,
            ],
            wantedEvents);
    }

    // xterm.js: 'CoreMouseService' › 'triggerMouseEvent' › 'NONE'
    [Fact]
    public void NONE_protocol_reports_nothing()
    {
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Up }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Move }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Middle, Action = CoreMouseAction.Down }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Right, Action = CoreMouseAction.Down }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Wheel, Action = CoreMouseAction.Up }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Move }));
    }

    // xterm.js: 'CoreMouseService' › 'triggerMouseEvent' › 'X10'
    [Fact]
    public void X10_protocol_reports_button_presses_only()
    {
        _cms.ActiveProtocol = "X10";
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Up }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Move }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Middle, Action = CoreMouseAction.Down }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Right, Action = CoreMouseAction.Down }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Wheel, Action = CoreMouseAction.Up }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Move }));
    }

    // xterm.js: 'CoreMouseService' › 'triggerMouseEvent' › 'VT200'
    [Fact]
    public void VT200_protocol_reports_presses_releases_and_wheel()
    {
        _cms.ActiveProtocol = "VT200";
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Up }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Move }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Middle, Action = CoreMouseAction.Down }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Right, Action = CoreMouseAction.Down }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Wheel, Action = CoreMouseAction.Up }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Move }));
    }

    // xterm.js: 'CoreMouseService' › 'triggerMouseEvent' › 'DRAG'
    [Fact]
    public void DRAG_protocol_reports_moves_with_a_button()
    {
        _cms.ActiveProtocol = "DRAG";
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Up }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Move }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Middle, Action = CoreMouseAction.Down }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Right, Action = CoreMouseAction.Down }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Wheel, Action = CoreMouseAction.Up }));
    }

    // xterm.js: 'CoreMouseService' › 'triggerMouseEvent' › 'ANY'
    [Fact]
    public void ANY_protocol_reports_valid_events_and_rejects_invalid_ones()
    {
        _cms.ActiveProtocol = "ANY";
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Up }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Move }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Middle, Action = CoreMouseAction.Down }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Right, Action = CoreMouseAction.Down }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Wheel, Action = CoreMouseAction.Up }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Move }));
        // should not report in any case
        // invalid button + action combinations
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Wheel, Action = CoreMouseAction.Move }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Down }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Up }));
        // invalid coords
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = -1, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 500, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = -1, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
        Assert.False(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 500, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
    }

    // xterm.js: 'CoreMouseService' › 'triggerMouseEvent' › 'eventCodes with modifiers (DEFAULT encoding)'
    [Fact]
    public void Event_codes_with_modifiers_in_the_DEFAULT_encoding()
    {
        // TODO: implement AUX button tests
        _cms.ActiveProtocol = "ANY";
        _cms.ActiveEncoding = "DEFAULT";
        // all buttons + down + no modifer
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down, Ctrl = false, Alt = false, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Middle, Action = CoreMouseAction.Down, Ctrl = false, Alt = false, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Right, Action = CoreMouseAction.Down, Ctrl = false, Alt = false, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Wheel, Action = CoreMouseAction.Down, Ctrl = false, Alt = false, Shift = false }));
        Assert.Equal<string>(["\x1b[M !!", "\x1b[M!!!", "\x1b[M\"!!", "\x1b[Ma!!"], _reports);
        _reports.Clear();
        // all buttons + up + no modifier
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Up, Ctrl = false, Alt = false, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Middle, Action = CoreMouseAction.Up, Ctrl = false, Alt = false, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Right, Action = CoreMouseAction.Up, Ctrl = false, Alt = false, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Wheel, Action = CoreMouseAction.Up, Ctrl = false, Alt = false, Shift = false }));
        Assert.Equal<string>(["\x1b[M#!!", "\x1b[M#!!", "\x1b[M#!!", "\x1b[M`!!"], _reports);
        _reports.Clear();
        // all buttons + move + no modifier
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Move, Ctrl = false, Alt = false, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Middle, Action = CoreMouseAction.Move, Ctrl = false, Alt = false, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.Right, Action = CoreMouseAction.Move, Ctrl = false, Alt = false, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Move, Ctrl = false, Alt = false, Shift = false }));
        Assert.Equal<string>(["\x1b[M@!!", "\x1b[MA!!", "\x1b[MB!!", "\x1b[MC!!"], _reports);
        _reports.Clear();
        // button none + move + modifiers
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Move, Ctrl = true, Alt = false, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Move, Ctrl = false, Alt = true, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Move, Ctrl = false, Alt = false, Shift = true }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Move, Ctrl = true, Alt = true, Shift = false }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Move, Ctrl = false, Alt = true, Shift = true }));
        Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, Button = CoreMouseButton.None, Action = CoreMouseAction.Move, Ctrl = true, Alt = true, Shift = true }));
        Assert.Equal<string>(["\x1b[MS!!", "\x1b[MK!!", "\x1b[MG!!", "\x1b[M[!!", "\x1b[MO!!", "\x1b[M_!!"], _reports);
        _reports.Clear();
    }

    // xterm.js: 'CoreMouseService' › 'triggerMouseEvent' › 'coords' › 'DEFAULT encoding'
    [Fact]
    public void DEFAULT_encoding_suppresses_reports_beyond_the_addressable_range()
    {
        _cms.ActiveProtocol = "ANY";
        for (int i = 0; i < _bufferService.Cols; ++i)
        {
            Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = i, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
            if (i > 222)
            {
                // suppress mouse reports if we are out of addressable range (max. 222)
                Assert.Empty(ToBytes(PopReport()));
            }
            else
            {
                Assert.Equal<int>([0x1b, 0x5b, 0x4d, 0x20, i + 33, 0x21], ToBytes(PopReport()));
            }
        }
    }

    // xterm.js: 'CoreMouseService' › 'triggerMouseEvent' › 'coords' › 'SGR encoding'
    [Fact]
    public void SGR_encoding_reports_one_based_cell_coordinates()
    {
        _cms.ActiveProtocol = "ANY";
        _cms.ActiveEncoding = "SGR";
        for (int i = 0; i < _bufferService.Cols; ++i)
        {
            Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = i, Row = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
            Assert.Equal($"\x1b[<0;{i + 1};1M", PopReport());
        }
    }

    // xterm.js: 'CoreMouseService' › 'triggerMouseEvent' › 'coords' › 'SGR_PIXELS encoding'
    [Fact]
    public void SGR_PIXELS_encoding_reports_pixel_coordinates()
    {
        _cms.ActiveProtocol = "ANY";
        _cms.ActiveEncoding = "SGR_PIXELS";
        for (int i = 0; i < 500; ++i)
        {
            Assert.True(_cms.TriggerMouseEvent(new CoreMouseEvent { Col = 0, Row = 0, X = i, Y = 0, Button = CoreMouseButton.Left, Action = CoreMouseAction.Down }));
            Assert.Equal($"\x1b[<0;{i};0M", PopReport());
        }
    }

    [Fact]
    public void ExplainEvents_names_the_events_of_a_mask()
    {
        Dictionary<string, bool> explained = _cms.ExplainEvents(CoreMouseEventType.Down | CoreMouseEventType.Drag);
        Assert.True(explained["down"]);
        Assert.False(explained["up"]);
        Assert.True(explained["drag"]);
        Assert.False(explained["move"]);
        Assert.False(explained["wheel"]);
    }

    [Fact]
    public void Identical_move_events_are_reported_once()
    {
        _cms.ActiveProtocol = "ANY";
        _cms.ActiveEncoding = "SGR";
        CoreMouseEvent Move() => new() { Col = 3, Row = 4, Button = CoreMouseButton.None, Action = CoreMouseAction.Move };
        Assert.True(_cms.TriggerMouseEvent(Move()));
        Assert.False(_cms.TriggerMouseEvent(Move()));
        Assert.Single(_reports);
    }

    [Fact]
    public void Wheel_events_in_pixels_accumulate_partial_scrolls()
    {
        // 20 px cells at dpr 1: a 10 px trackpad delta is 10 / 20 * 0.3 = 0.15 line.
        int total = 0;
        for (int i = 0; i < 7; ++i)
        {
            total += _cms.ConsumeWheelEvent(10, WheelDeltaMode.Pixel, false, false, false, 20, 1);
        }

        Assert.Equal(1, total);
        Assert.Equal(0, _cms.ConsumeWheelEvent(0, WheelDeltaMode.Pixel, false, false, false, 20, 1));
        Assert.Equal(0, _cms.ConsumeWheelEvent(10, WheelDeltaMode.Pixel, true, false, false, 20, 1));
    }

    [Fact]
    public void Wheel_events_in_pages_scroll_by_the_number_of_rows()
    {
        Assert.Equal(-100, _cms.ConsumeWheelEvent(-1, WheelDeltaMode.Page, false, false, false, 20, 1));
    }

    [Fact]
    public void Wheel_events_with_a_modifier_use_the_fast_scroll_sensitivity()
    {
        // default sensitivities: 1 and 5; a 100 px delta over 20 px cells with Ctrl is 100 * 5 / 20 = 25 lines.
        Assert.Equal(25, _cms.ConsumeWheelEvent(100, WheelDeltaMode.Pixel, false, false, true, 20, 1));
    }
}
