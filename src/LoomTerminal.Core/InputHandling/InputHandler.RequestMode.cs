// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Data;
using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>The mode state values of a DECRPM report.</summary>
    private enum ModeState
    {
        NotRecognized = 0,
        Set = 1,
        Reset = 2,
        PermanentlySet = 3,
        PermanentlyReset = 4,
    }

    /// <summary>DECRQM (CSI Ps $ p and CSI ? Ps $ p): reports the state of an ANSI or DEC private mode.</summary>
    /// <param name="p">The mode number.</param>
    /// <param name="ansi">True for an ANSI mode, false for a DEC private mode.</param>
    public bool RequestMode(IParams p, bool ansi)
    {
        // access helpers
        DecPrivateModes dm = _coreService.DecPrivateModes;
        string mouseProtocol = _coreMouseService.ActiveProtocol;
        string mouseEncoding = _coreMouseService.ActiveEncoding;
        ICoreService cs = _coreService;
        BufferSet buffers = _bufferService.Buffers;
        int cols = _bufferService.Cols;
        bool isAlt = buffers.Active == buffers.Alt;
        TerminalOptions opts = _optionsService.RawOptions;

        // return value as in DECRPM
        int mode = p.ParamValues[0];
        bool F(ModeState state)
        {
            cs.TriggerDataEvent($"{C0.Esc}[{(ansi ? string.Empty : "?")}{mode};{(int)state}$y");
            return true;
        }

        static ModeState B2V(bool value) => value ? ModeState.Set : ModeState.Reset;

        if (ansi)
        {
            return mode switch
            {
                2 => F(ModeState.PermanentlyReset),
                4 => F(B2V(cs.Modes.InsertMode)),
                12 => F(ModeState.PermanentlySet),
                20 => F(B2V(opts.ConvertEol)),
                _ => F(ModeState.NotRecognized),
            };
        }

        return mode switch
        {
            1 => F(B2V(dm.ApplicationCursorKeys)),
            3 => F(opts.WindowOptions.SetWinLines
                ? (cols == 80 ? ModeState.Reset : cols == 132 ? ModeState.Set : ModeState.NotRecognized)
                : ModeState.NotRecognized),
            6 => F(B2V(dm.Origin)),
            7 => F(B2V(dm.Wraparound)),
            8 => F(ModeState.PermanentlySet),
            9 => F(B2V(mouseProtocol == "X10")),
            12 => F(B2V(opts.CursorBlink)),
            25 => F(B2V(!cs.IsCursorHidden)),
            45 => F(B2V(dm.ReverseWraparound)),
            66 => F(B2V(dm.ApplicationKeypad)),
            67 => F(ModeState.PermanentlyReset),
            1000 => F(B2V(mouseProtocol == "VT200")),
            1002 => F(B2V(mouseProtocol == "DRAG")),
            1003 => F(B2V(mouseProtocol == "ANY")),
            1004 => F(B2V(dm.SendFocus)),
            1005 => F(ModeState.PermanentlyReset),
            1006 => F(B2V(mouseEncoding == "SGR")),
            1015 => F(ModeState.PermanentlyReset),
            1016 => F(B2V(mouseEncoding == "SGR_PIXELS")),
            1048 => F(ModeState.Set), // xterm always returns SET here
            47 or 1047 or 1049 => F(B2V(isAlt)),
            2004 => F(B2V(dm.BracketedPasteMode)),
            2026 => F(B2V(dm.SynchronizedOutput)),
            _ => F(ModeState.NotRecognized),
        };
    }

    private void RegisterRequestModeHandlers()
    {
        _parser.RegisterCsiHandler(new FunctionIdentifier(Intermediates: "$", Final: "p"), p => RequestMode(p, true));
        _parser.RegisterCsiHandler(new FunctionIdentifier(Prefix: "?", Intermediates: "$", Final: "p"), p => RequestMode(p, false));
    }
}
