// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Text;
using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Data;
using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>REP (CSI Ps b): repeats the preceding graphic character <c>Ps</c> times (default 1).</summary>
    public bool RepeatPrecedingCharacter(IParams p)
    {
        int joinState = _parser.PrecedingJoinState;
        if (joinState == 0)
        {
            return true;
        }

        int length = OrOne(p);
        int chWidth = UnicodeService.ExtractWidth(joinState);
        int x = _activeBuffer.X - chWidth;
        IBufferLine bufferRow = _activeBuffer.Lines.Get(_activeBuffer.YBase + _activeBuffer.Y)!;
        string text = bufferRow.GetString(x);
        uint[] data = new uint[text.Length * length];
        int idata = 0;
        foreach (Rune rune in text.EnumerateRunes())
        {
            data[idata++] = (uint)rune.Value;
        }

        int tlength = idata;
        for (int i = 1; i < length; ++i)
        {
            Array.Copy(data, 0, data, tlength, idata);
            tlength += idata;
        }

        Print(data, 0, tlength);
        return true;
    }

    /// <summary>DA1 (CSI Ps c): sends the primary device attributes of the emulated terminal.</summary>
    public bool SendDeviceAttributesPrimary(IParams p)
    {
        if (p.ParamValues[0] > 0)
        {
            return true;
        }

        if (Is("xterm") || Is("rxvt-unicode") || Is("screen"))
        {
            _coreService.TriggerDataEvent(C0.Esc + "[?1;2c");
        }
        else if (Is("linux"))
        {
            _coreService.TriggerDataEvent(C0.Esc + "[?6c");
        }

        return true;
    }

    /// <summary>DA2 (CSI &gt; Ps c): sends the secondary device attributes of the emulated terminal.</summary>
    public bool SendDeviceAttributesSecondary(IParams p)
    {
        if (p.ParamValues[0] > 0)
        {
            return true;
        }

        if (Is("xterm"))
        {
            _coreService.TriggerDataEvent(C0.Esc + "[>0;276;0c");
        }
        else if (Is("rxvt-unicode"))
        {
            _coreService.TriggerDataEvent(C0.Esc + "[>85;95;0c");
        }
        else if (Is("linux"))
        {
            _coreService.TriggerDataEvent(p.ParamValues[0] + "c");
        }
        else if (Is("screen"))
        {
            _coreService.TriggerDataEvent(C0.Esc + "[>83;40003;0c");
        }

        return true;
    }

    private bool Is(string term) => _optionsService.RawOptions.TermName.StartsWith(term, StringComparison.Ordinal);

    private void RegisterDeviceAttributesHandlers()
    {
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "b"), RepeatPrecedingCharacter);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "c"), SendDeviceAttributesPrimary);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Prefix: ">", Final: "c"), SendDeviceAttributesSecondary);
    }
}
