// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Data;
using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>DECRQSS (DCS $ q Pt ST): reports the state of the setting named by <paramref name="data"/>.</summary>
    public bool RequestStatusString(string data, IParams p)
    {
        bool F(string s)
        {
            _coreService.TriggerDataEvent($"{C0.Esc}{s}{C0.Esc}\\");
            return true;
        }

        // access helpers
        Core.Buffer.Buffer b = _bufferService.Buffer;
        TerminalOptions opts = _optionsService.RawOptions;
        int style = opts.CursorStyle switch
        {
            CursorStyle.Block => 2,
            CursorStyle.Underline => 4,
            _ => 6,
        };

        if (data == "\"q")
        {
            return F($"P1$r{(_curAttrData.IsProtected() != 0 ? 1 : 0)}\"q");
        }

        if (data == "\"p")
        {
            return F("P1$r61;1\"p");
        }

        if (data == "r")
        {
            return F($"P1$r{b.ScrollTop + 1};{b.ScrollBottom + 1}r");
        }

        // FIXME: report real SGR settings instead of 0m
        if (data == "m")
        {
            return F("P1$r0m");
        }

        if (data == " q")
        {
            return F($"P1$r{style - (opts.CursorBlink ? 1 : 0)} q");
        }

        return F("P0$r");
    }

    private void RegisterDcsHandlers() =>
        _parser.RegisterDcsHandler(new FunctionIdentifier(Intermediates: "$", Final: "q"), new DcsHandler(RequestStatusString));
}
