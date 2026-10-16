// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Text;
using LoomTerminal.Core.Input;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.Parser;

// set/clear handler (the xterm.js suite uses a second parser, `parser2`, without the mocked OSC parser)
public sealed partial class EscapeSequenceParserTests
{
    private const string Input = "\u001b[1;31mhello \u001b%Gwor\u001bEld!\u001b[0m\r\n$>\u001b]1;foo=bar\u001b\\";
    private const string DcsInput = "\u001bP1;2;3+pabc\u001b\\";

    private static readonly FunctionIdentifier PercentG = new(Intermediates: "%", Final: "G");
    private static readonly FunctionIdentifier PlusP = new(Intermediates: "+", Final: "p");
    private static readonly FunctionIdentifier FinalE = new(Final: "E");
    private static readonly FunctionIdentifier FinalM = new(Final: "m");

    private readonly TestEscapeSequenceParser _parser2 = new();
    private readonly StringBuilder _print = new();
    private readonly List<string> _esc = [];
    private readonly List<object[]> _csi = [];
    private readonly List<string> _exe = [];
    private readonly List<object[]> _osc = [];
    private readonly List<object[]> _dcs = [];

    private void ClearAccu()
    {
        _print.Clear();
        _esc.Clear();
        _csi.Clear();
        _exe.Clear();
        _osc.Clear();
        _dcs.Clear();
    }

    private static string DecodeToString(uint[] data, int start, int end)
    {
        StringBuilder s = new();
        for (int i = start; i < end; ++i)
        {
            s.Append(TextDecoder.StringFromCodePoint(data[i]));
        }

        return s.ToString();
    }

    private sealed class CallbackDcsHandler(Action<IParams> hook, Action<uint[], int, int> put, Func<bool> unhook) : IDcsHandler
    {
        public void Hook(IParams p) => hook(p);

        public void Put(uint[] data, int start, int end) => put(data, start, end);

        public bool Unhook(bool success) => unhook();
    }

    // xterm.js: 'print handler'
    [Fact]
    public void SetPrintHandler_receives_the_text_until_cleared()
    {
        _parser2.SetPrintHandler((data, start, end) => _print.Append(DecodeToString(data, start, end)));
        Parse(_parser2, Input);
        Assert.Equal("hello world!$>", _print.ToString());
        _parser2.ClearPrintHandler();
        _parser2.ClearPrintHandler(); // should not throw
        ClearAccu();
        Parse(_parser2, Input);
        Assert.Equal(string.Empty, _print.ToString());
    }

    // xterm.js: 'ESC handler'
    [Fact]
    public void RegisterEscHandler_and_ClearEscHandler()
    {
        _parser2.RegisterEscHandler(PercentG, () =>
        {
            _esc.Add("%G");
            return true;
        });
        _parser2.RegisterEscHandler(FinalE, () =>
        {
            _esc.Add("E");
            return true;
        });
        Parse(_parser2, Input);
        Assert.Equal(["%G", "E"], _esc);
        _parser2.ClearEscHandler(PercentG);
        _parser2.ClearEscHandler(PercentG); // should not throw
        ClearAccu();
        Parse(_parser2, Input);
        Assert.Equal(["E"], _esc);
        _parser2.ClearEscHandler(FinalE);
        ClearAccu();
        Parse(_parser2, Input);
        Assert.Empty(_esc);
    }

    // xterm.js: 'ESC custom handlers' › 'prevent fallback'
    [Fact]
    public void Esc_custom_handlers_prevent_fallback()
    {
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("default - %G"); return true; });
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("custom - %G"); return true; });
        Parse(_parser2, Input);
        Assert.Equal(["custom - %G"], _esc);
    }

    // xterm.js: 'ESC custom handlers' › 'allow fallback'
    [Fact]
    public void Esc_custom_handlers_allow_fallback()
    {
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("default - %G"); return true; });
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("custom - %G"); return false; });
        Parse(_parser2, Input);
        Assert.Equal(["custom - %G", "default - %G"], _esc);
    }

    // xterm.js: 'ESC custom handlers' › 'Multiple custom handlers fallback once'
    [Fact]
    public void Esc_custom_handlers_multiple_fallback_once()
    {
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("default - %G"); return true; });
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("custom - %G"); return true; });
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("custom2 - %G"); return false; });
        Parse(_parser2, Input);
        Assert.Equal(["custom2 - %G", "custom - %G"], _esc);
    }

    // xterm.js: 'ESC custom handlers' › 'Multiple custom handlers no fallback'
    [Fact]
    public void Esc_custom_handlers_multiple_no_fallback()
    {
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("default - %G"); return true; });
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("custom - %G"); return true; });
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("custom2 - %G"); return true; });
        Parse(_parser2, Input);
        Assert.Equal(["custom2 - %G"], _esc);
    }

    // xterm.js: 'ESC custom handlers' › 'Execution order should go from latest handler down to the original'
    [Fact]
    public void Esc_custom_handlers_execution_order_goes_from_latest_down_to_the_original()
    {
        List<int> order = [];
        _parser2.RegisterEscHandler(PercentG, () => { order.Add(1); return true; });
        _parser2.RegisterEscHandler(PercentG, () => { order.Add(2); return false; });
        _parser2.RegisterEscHandler(PercentG, () => { order.Add(3); return false; });
        Parse(_parser2, "\u001b%G");
        Assert.Equal([3, 2, 1], order);
    }

    // xterm.js: 'ESC custom handlers' › 'Dispose should work'
    [Fact]
    public void Esc_custom_handlers_dispose_works()
    {
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("default - %G"); return true; });
        IDisposable dispo = _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("custom - %G"); return true; });
        dispo.Dispose();
        Parse(_parser2, Input);
        Assert.Equal(["default - %G"], _esc);
    }

    // xterm.js: 'ESC custom handlers' › 'Should not corrupt the parser when dispose is called twice'
    [Fact]
    public void Esc_custom_handlers_do_not_corrupt_the_parser_when_disposed_twice()
    {
        _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("default - %G"); return true; });
        IDisposable dispo = _parser2.RegisterEscHandler(PercentG, () => { _esc.Add("custom - %G"); return true; });
        dispo.Dispose();
        dispo.Dispose();
        Parse(_parser2, Input);
        Assert.Equal(["default - %G"], _esc);
    }

    // xterm.js: 'CSI handler'
    [Fact]
    public void RegisterCsiHandler_and_ClearCsiHandler()
    {
        _parser2.RegisterCsiHandler(FinalM, p =>
        {
            _csi.Add(A("m", p.ToArray(), string.Empty));
            return true;
        });
        Parse(_parser2, Input);
        Assert.Equal([A("m", A(1, 31), string.Empty), A("m", A(0), string.Empty)], _csi);
        _parser2.ClearCsiHandler(FinalM);
        _parser2.ClearCsiHandler(FinalM); // should not throw
        ClearAccu();
        Parse(_parser2, Input);
        Assert.Empty(_csi);
    }

    // xterm.js: 'CSI custom handlers' › 'Prevent fallback'
    [Fact]
    public void Csi_custom_handlers_prevent_fallback()
    {
        List<object[]> csiCustom = [];
        _parser2.RegisterCsiHandler(FinalM, p => { _csi.Add(A("m", p.ToArray(), string.Empty)); return true; });
        _parser2.RegisterCsiHandler(FinalM, p => { csiCustom.Add(A("m", p.ToArray(), string.Empty)); return true; });
        Parse(_parser2, Input);
        Assert.Empty(_csi); // Should not fallback to original handler
        Assert.Equal([A("m", A(1, 31), string.Empty), A("m", A(0), string.Empty)], csiCustom);
    }

    // xterm.js: 'CSI custom handlers' › 'Allow fallback'
    [Fact]
    public void Csi_custom_handlers_allow_fallback()
    {
        List<object[]> csiCustom = [];
        _parser2.RegisterCsiHandler(FinalM, p => { _csi.Add(A("m", p.ToArray(), string.Empty)); return true; });
        _parser2.RegisterCsiHandler(FinalM, p => { csiCustom.Add(A("m", p.ToArray(), string.Empty)); return false; });
        Parse(_parser2, Input);
        Assert.Equal([A("m", A(1, 31), string.Empty), A("m", A(0), string.Empty)], _csi); // Should fallback to original handler
        Assert.Equal([A("m", A(1, 31), string.Empty), A("m", A(0), string.Empty)], csiCustom);
    }

    // xterm.js: 'CSI custom handlers' › 'Multiple custom handlers fallback once'
    [Fact]
    public void Csi_custom_handlers_multiple_fallback_once()
    {
        List<object[]> csiCustom = [];
        List<object[]> csiCustom2 = [];
        _parser2.RegisterCsiHandler(FinalM, p => { _csi.Add(A("m", p.ToArray(), string.Empty)); return true; });
        _parser2.RegisterCsiHandler(FinalM, p => { csiCustom.Add(A("m", p.ToArray(), string.Empty)); return true; });
        _parser2.RegisterCsiHandler(FinalM, p => { csiCustom2.Add(A("m", p.ToArray(), string.Empty)); return false; });
        Parse(_parser2, Input);
        Assert.Empty(_csi); // Should not fallback to original handler
        Assert.Equal([A("m", A(1, 31), string.Empty), A("m", A(0), string.Empty)], csiCustom);
        Assert.Equal([A("m", A(1, 31), string.Empty), A("m", A(0), string.Empty)], csiCustom2);
    }

    // xterm.js: 'CSI custom handlers' › 'Multiple custom handlers no fallback'
    [Fact]
    public void Csi_custom_handlers_multiple_no_fallback()
    {
        List<object[]> csiCustom = [];
        List<object[]> csiCustom2 = [];
        _parser2.RegisterCsiHandler(FinalM, p => { _csi.Add(A("m", p.ToArray(), string.Empty)); return true; });
        _parser2.RegisterCsiHandler(FinalM, p => { csiCustom.Add(A("m", p.ToArray(), string.Empty)); return true; });
        _parser2.RegisterCsiHandler(FinalM, p => { csiCustom2.Add(A("m", p.ToArray(), string.Empty)); return true; });
        Parse(_parser2, Input);
        Assert.Empty(_csi); // Should not fallback to original handler
        Assert.Empty(csiCustom); // Should not fallback once
        Assert.Equal([A("m", A(1, 31), string.Empty), A("m", A(0), string.Empty)], csiCustom2);
    }

    // xterm.js: 'CSI custom handlers' › 'Execution order should go from latest handler down to the original'
    [Fact]
    public void Csi_custom_handlers_execution_order_goes_from_latest_down_to_the_original()
    {
        List<int> order = [];
        _parser2.RegisterCsiHandler(FinalM, _ => { order.Add(1); return true; });
        _parser2.RegisterCsiHandler(FinalM, _ => { order.Add(2); return false; });
        _parser2.RegisterCsiHandler(FinalM, _ => { order.Add(3); return false; });
        Parse(_parser2, "\u001b[0m");
        Assert.Equal([3, 2, 1], order);
    }

    // xterm.js: 'CSI custom handlers' › 'Dispose should work'
    [Fact]
    public void Csi_custom_handlers_dispose_works()
    {
        List<object[]> csiCustom = [];
        _parser2.RegisterCsiHandler(FinalM, p => { _csi.Add(A("m", p.ToArray(), string.Empty)); return true; });
        IDisposable customHandler = _parser2.RegisterCsiHandler(FinalM, p => { csiCustom.Add(A("m", p.ToArray(), string.Empty)); return true; });
        customHandler.Dispose();
        Parse(_parser2, Input);
        Assert.Equal([A("m", A(1, 31), string.Empty), A("m", A(0), string.Empty)], _csi);
        Assert.Empty(csiCustom); // Should not use custom handler as it was disposed
    }

    // xterm.js: 'CSI custom handlers' › 'Should not corrupt the parser when dispose is called twice'
    [Fact]
    public void Csi_custom_handlers_do_not_corrupt_the_parser_when_disposed_twice()
    {
        List<object[]> csiCustom = [];
        _parser2.RegisterCsiHandler(FinalM, p => { _csi.Add(A("m", p.ToArray(), string.Empty)); return true; });
        IDisposable customHandler = _parser2.RegisterCsiHandler(FinalM, p => { csiCustom.Add(A("m", p.ToArray(), string.Empty)); return true; });
        customHandler.Dispose();
        customHandler.Dispose();
        Parse(_parser2, Input);
        Assert.Equal([A("m", A(1, 31), string.Empty), A("m", A(0), string.Empty)], _csi);
        Assert.Empty(csiCustom); // Should not use custom handler as it was disposed
    }

    // xterm.js: 'EXECUTE handler'
    [Fact]
    public void SetExecuteHandler_and_ClearExecuteHandler()
    {
        _parser2.SetExecuteHandler("\n", () =>
        {
            _exe.Add("\n");
            return true;
        });
        _parser2.SetExecuteHandler("\r", () =>
        {
            _exe.Add("\r");
            return true;
        });
        Parse(_parser2, Input);
        Assert.Equal(["\r", "\n"], _exe);
        _parser2.ClearExecuteHandler("\r");
        _parser2.ClearExecuteHandler("\r"); // should not throw
        ClearAccu();
        Parse(_parser2, Input);
        Assert.Equal(["\n"], _exe);
    }

    // xterm.js: 'OSC handler'
    [Fact]
    public void RegisterOscHandler_and_ClearOscHandler()
    {
        _parser2.RegisterOscHandler(1, new OscHandler(data =>
        {
            _osc.Add(A(1, data));
            return true;
        }));
        Parse(_parser2, Input);
        Assert.Equal([A(1, "foo=bar")], _osc);
        _parser2.ClearOscHandler(1);
        _parser2.ClearOscHandler(1); // should not throw
        ClearAccu();
        Parse(_parser2, Input);
        Assert.Empty(_osc);
    }

    // xterm.js: 'OSC custom handlers' › 'Prevent fallback'
    [Fact]
    public void Osc_custom_handlers_prevent_fallback()
    {
        List<object[]> oscCustom = [];
        _parser2.RegisterOscHandler(1, new OscHandler(data => { _osc.Add(A(1, data)); return true; }));
        _parser2.RegisterOscHandler(1, new OscHandler(data => { oscCustom.Add(A(1, data)); return true; }));
        Parse(_parser2, Input);
        Assert.Empty(_osc); // Should not fallback to original handler
        Assert.Equal([A(1, "foo=bar")], oscCustom);
    }

    // xterm.js: 'OSC custom handlers' › 'Allow fallback'
    [Fact]
    public void Osc_custom_handlers_allow_fallback()
    {
        List<object[]> oscCustom = [];
        _parser2.RegisterOscHandler(1, new OscHandler(data => { _osc.Add(A(1, data)); return true; }));
        _parser2.RegisterOscHandler(1, new OscHandler(data => { oscCustom.Add(A(1, data)); return false; }));
        Parse(_parser2, Input);
        Assert.Equal([A(1, "foo=bar")], _osc); // Should fallback to original handler
        Assert.Equal([A(1, "foo=bar")], oscCustom);
    }

    // xterm.js: 'OSC custom handlers' › 'Multiple custom handlers fallback once'
    [Fact]
    public void Osc_custom_handlers_multiple_fallback_once()
    {
        List<object[]> oscCustom = [];
        List<object[]> oscCustom2 = [];
        _parser2.RegisterOscHandler(1, new OscHandler(data => { _osc.Add(A(1, data)); return true; }));
        _parser2.RegisterOscHandler(1, new OscHandler(data => { oscCustom.Add(A(1, data)); return true; }));
        _parser2.RegisterOscHandler(1, new OscHandler(data => { oscCustom2.Add(A(1, data)); return false; }));
        Parse(_parser2, Input);
        Assert.Empty(_osc); // Should not fallback to original handler
        Assert.Equal([A(1, "foo=bar")], oscCustom);
        Assert.Equal([A(1, "foo=bar")], oscCustom2);
    }

    // xterm.js: 'OSC custom handlers' › 'Multiple custom handlers no fallback'
    [Fact]
    public void Osc_custom_handlers_multiple_no_fallback()
    {
        List<object[]> oscCustom = [];
        List<object[]> oscCustom2 = [];
        _parser2.RegisterOscHandler(1, new OscHandler(data => { _osc.Add(A(1, data)); return true; }));
        _parser2.RegisterOscHandler(1, new OscHandler(data => { oscCustom.Add(A(1, data)); return true; }));
        _parser2.RegisterOscHandler(1, new OscHandler(data => { oscCustom2.Add(A(1, data)); return true; }));
        Parse(_parser2, Input);
        Assert.Empty(_osc); // Should not fallback to original handler
        Assert.Empty(oscCustom); // Should not fallback once
        Assert.Equal([A(1, "foo=bar")], oscCustom2);
    }

    // xterm.js: 'OSC custom handlers' › 'Execution order should go from latest handler down to the original'
    [Fact]
    public void Osc_custom_handlers_execution_order_goes_from_latest_down_to_the_original()
    {
        List<int> order = [];
        _parser2.RegisterOscHandler(1, new OscHandler(_ => { order.Add(1); return true; }));
        _parser2.RegisterOscHandler(1, new OscHandler(_ => { order.Add(2); return false; }));
        _parser2.RegisterOscHandler(1, new OscHandler(_ => { order.Add(3); return false; }));
        Parse(_parser2, "\u001b]1;foo=bar\u001b\\");
        Assert.Equal([3, 2, 1], order);
    }

    // xterm.js: 'OSC custom handlers' › 'Dispose should work'
    [Fact]
    public void Osc_custom_handlers_dispose_works()
    {
        List<object[]> oscCustom = [];
        _parser2.RegisterOscHandler(1, new OscHandler(data => { _osc.Add(A(1, data)); return true; }));
        IDisposable customHandler = _parser2.RegisterOscHandler(1, new OscHandler(data => { oscCustom.Add(A(1, data)); return true; }));
        customHandler.Dispose();
        Parse(_parser2, Input);
        Assert.Equal([A(1, "foo=bar")], _osc);
        Assert.Empty(oscCustom); // Should not use custom handler as it was disposed
    }

    // xterm.js: 'OSC custom handlers' › 'Should not corrupt the parser when dispose is called twice'
    [Fact]
    public void Osc_custom_handlers_do_not_corrupt_the_parser_when_disposed_twice()
    {
        List<object[]> oscCustom = [];
        _parser2.RegisterOscHandler(1, new OscHandler(data => { _osc.Add(A(1, data)); return true; }));
        IDisposable customHandler = _parser2.RegisterOscHandler(1, new OscHandler(data => { oscCustom.Add(A(1, data)); return true; }));
        customHandler.Dispose();
        customHandler.Dispose();
        Parse(_parser2, Input);
        Assert.Equal([A(1, "foo=bar")], _osc);
        Assert.Empty(oscCustom); // Should not use custom handler as it was disposed
    }

    // xterm.js: 'DCS handler'
    [Fact]
    public void RegisterDcsHandler_and_ClearDcsHandler()
    {
        _parser2.RegisterDcsHandler(
            PlusP,
            new CallbackDcsHandler(
                p => _dcs.Add(A("hook", string.Empty, p.ToArray(), 0)),
                (data, start, end) => _dcs.Add(A("put", DecodeToString(data, start, end))),
                () =>
                {
                    _dcs.Add(A("unhook"));
                    return true;
                }));
        Parse(_parser2, "\u001bP1;2;3+pabc");
        Parse(_parser2, ";de\u009c");
        Assert.Equal(
            [
                A("hook", string.Empty, A(1, 2, 3), 0),
                A("put", "abc"),
                A("put", ";de"),
                A("unhook"),
            ],
            _dcs);
        _parser2.ClearDcsHandler(PlusP);
        _parser2.ClearDcsHandler(PlusP); // should not throw
        ClearAccu();
        Parse(_parser2, "\u001bP1;2;3+pabc");
        Parse(_parser2, ";de\u009c");
        Assert.Empty(_dcs);
    }

    // xterm.js: 'DCS custom handlers' › 'Prevent fallback'
    [Fact]
    public void Dcs_custom_handlers_prevent_fallback()
    {
        List<object[]> dcsCustom = [];
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("A", p.ToArray(), data)); return true; }));
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("B", p.ToArray(), data)); return true; }));
        Parse(_parser2, DcsInput);
        Assert.Equal([A("B", A(1, 2, 3), "abc")], dcsCustom);
    }

    // xterm.js: 'DCS custom handlers' › 'Allow fallback'
    [Fact]
    public void Dcs_custom_handlers_allow_fallback()
    {
        List<object[]> dcsCustom = [];
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("A", p.ToArray(), data)); return true; }));
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("B", p.ToArray(), data)); return false; }));
        Parse(_parser2, DcsInput);
        Assert.Equal([A("B", A(1, 2, 3), "abc"), A("A", A(1, 2, 3), "abc")], dcsCustom);
    }

    // xterm.js: 'DCS custom handlers' › 'Multiple custom handlers fallback once'
    [Fact]
    public void Dcs_custom_handlers_multiple_fallback_once()
    {
        List<object[]> dcsCustom = [];
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("A", p.ToArray(), data)); return true; }));
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("B", p.ToArray(), data)); return true; }));
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("C", p.ToArray(), data)); return false; }));
        Parse(_parser2, DcsInput);
        Assert.Equal([A("C", A(1, 2, 3), "abc"), A("B", A(1, 2, 3), "abc")], dcsCustom);
    }

    // xterm.js: 'DCS custom handlers' › 'Multiple custom handlers no fallback'
    [Fact]
    public void Dcs_custom_handlers_multiple_no_fallback()
    {
        List<object[]> dcsCustom = [];
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("A", p.ToArray(), data)); return true; }));
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("B", p.ToArray(), data)); return true; }));
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("C", p.ToArray(), data)); return true; }));
        Parse(_parser2, DcsInput);
        Assert.Equal([A("C", A(1, 2, 3), "abc")], dcsCustom);
    }

    // xterm.js: 'DCS custom handlers' › 'Execution order should go from latest handler down to the original'
    [Fact]
    public void Dcs_custom_handlers_execution_order_goes_from_latest_down_to_the_original()
    {
        List<int> order = [];
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((_, _) => { order.Add(1); return true; }));
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((_, _) => { order.Add(2); return false; }));
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((_, _) => { order.Add(3); return false; }));
        Parse(_parser2, DcsInput);
        Assert.Equal([3, 2, 1], order);
    }

    // xterm.js: 'DCS custom handlers' › 'Dispose should work'
    [Fact]
    public void Dcs_custom_handlers_dispose_works()
    {
        List<object[]> dcsCustom = [];
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("A", p.ToArray(), data)); return true; }));
        IDisposable dispo = _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("B", p.ToArray(), data)); return true; }));
        dispo.Dispose();
        Parse(_parser2, DcsInput);
        Assert.Equal([A("A", A(1, 2, 3), "abc")], dcsCustom);
    }

    // xterm.js: 'DCS custom handlers' › 'Should not corrupt the parser when dispose is called twice'
    [Fact]
    public void Dcs_custom_handlers_do_not_corrupt_the_parser_when_disposed_twice()
    {
        List<object[]> dcsCustom = [];
        _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("A", p.ToArray(), data)); return true; }));
        IDisposable dispo = _parser2.RegisterDcsHandler(PlusP, new DcsHandler((data, p) => { dcsCustom.Add(A("B", p.ToArray(), data)); return true; }));
        dispo.Dispose();
        dispo.Dispose();
        Parse(_parser2, DcsInput);
        Assert.Equal([A("A", A(1, 2, 3), "abc")], dcsCustom);
    }

    // xterm.js: 'ERROR handler'
    [Fact]
    public void SetErrorHandler_and_ClearErrorHandler()
    {
        ParsingState? errorState = null;
        _parser2.SetErrorHandler(state =>
        {
            errorState = state;
            return state;
        });
        Parse(_parser2, "\u001b[1;2;€;3m"); // faulty escape sequence

        // xterm.js compares the whole state object with a deep equal (the params with Params.fromArray([1, 2, 0]),
        // "extra zero here"): the fields are compared one by one here.
        Assert.NotNull(errorState);
        Assert.Equal(6, errorState.Position);
        Assert.Equal((int)'€', errorState.Code);
        Assert.Equal(ParserState.CsiParam, errorState.CurrentState);
        Assert.Equal(0, errorState.Collect);
        Assert.Equal(Params.FromArray(A(1, 2, 0)).ToArray(), errorState.Params.ToArray());
        Assert.False(errorState.Abort);
        _parser2.ClearErrorHandler();
        _parser2.ClearErrorHandler(); // should not throw
        errorState = null;
        Parse(_parser2, "\u001b[1;2;a;3m");
        Assert.Null(errorState);
    }
}
