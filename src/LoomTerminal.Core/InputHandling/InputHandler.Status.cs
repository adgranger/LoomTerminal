// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Data;
using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>DSR (CSI Ps n): reports the status (5) or the cursor position (6).</summary>
    public bool DeviceStatus(IParams p)
    {
        switch (p.ParamValues[0])
        {
            case 5:
                // status report
                _coreService.TriggerDataEvent($"{C0.Esc}[0n");
                break;
            case 6:
                // cursor position
                int y = _activeBuffer.Y + 1;
                int x = _activeBuffer.X + 1;
                _coreService.TriggerDataEvent($"{C0.Esc}[{y};{x}R");
                break;
            default:
                break;
        }

        return true;
    }

    /// <summary>DSR with DEC private modes (CSI ? Ps n): only the cursor position (6) is answered.</summary>
    public bool DeviceStatusPrivate(IParams p)
    {
        // modern xterm doesnt seem to
        // respond to any of these except ?6, 6, and 5
        if (p.ParamValues[0] == 6)
        {
            // cursor position
            int y = _activeBuffer.Y + 1;
            int x = _activeBuffer.X + 1;
            _coreService.TriggerDataEvent($"{C0.Esc}[?{y};{x}R");
        }

        // 15: no printer, 25: no user defined keys, 26: north american keyboard, 53: no dec locator/mouse
        return true;
    }

    /// <summary>DECSTR (CSI ! p): soft terminal reset.</summary>
    public bool SoftReset(IParams p)
    {
        _coreService.IsCursorHidden = false;
        _onRequestSyncScrollBar.Fire(EventArgs.Empty);
        _activeBuffer.ScrollTop = 0;
        _activeBuffer.ScrollBottom = _bufferService.Rows - 1;
        _curAttrData = AttributeData.DefaultAttrData.Clone();
        _coreService.Reset();
        _charsetService.Reset();

        // reset DECSC data
        _activeBuffer.SavedX = 0;
        _activeBuffer.SavedY = _activeBuffer.YBase;
        _activeBuffer.SavedCurAttrData.Fg = _curAttrData.Fg;
        _activeBuffer.SavedCurAttrData.Bg = _curAttrData.Bg;
        _activeBuffer.SavedCharset = _charsetService.Charset;

        // reset DECOM
        _coreService.DecPrivateModes.Origin = false;
        return true;
    }

    /// <summary>DECSCUSR (CSI Ps SP q): sets the cursor style (0: default, 1 to 6: block, underline, bar; odd values blink).</summary>
    public bool SetCursorStyle(IParams p)
    {
        int param = p.Length == 0 ? 1 : p.ParamValues[0];
        if (param == 0)
        {
            _coreService.DecPrivateModes.CursorStyle = null;
            _coreService.DecPrivateModes.CursorBlink = null;
        }
        else
        {
            switch (param)
            {
                case 1:
                case 2:
                    _coreService.DecPrivateModes.CursorStyle = CursorStyle.Block;
                    break;
                case 3:
                case 4:
                    _coreService.DecPrivateModes.CursorStyle = CursorStyle.Underline;
                    break;
                case 5:
                case 6:
                    _coreService.DecPrivateModes.CursorStyle = CursorStyle.Bar;
                    break;
                default:
                    break;
            }

            bool isBlinking = param % 2 == 1;
            _coreService.DecPrivateModes.CursorBlink = isBlinking;
        }

        return true;
    }

    /// <summary>DECSTBM (CSI Pt ; Pb r): sets the top and bottom scroll margins and homes the cursor.</summary>
    public bool SetScrollRegion(IParams p)
    {
        int top = OrOne(p);
        int bottom = p.Length < 2 ? _bufferService.Rows : p.ParamValues[1];
        if (bottom > _bufferService.Rows || bottom == 0)
        {
            bottom = _bufferService.Rows;
        }

        if (bottom > top)
        {
            _activeBuffer.ScrollTop = top - 1;
            _activeBuffer.ScrollBottom = bottom - 1;
            SetCursor(0, 0);
        }

        return true;
    }

    private void RegisterStatusHandlers()
    {
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "n"), DeviceStatus);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Prefix: "?", Final: "n"), DeviceStatusPrivate);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Intermediates: "!", Final: "p"), SoftReset);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Intermediates: " ", Final: "q"), SetCursorStyle);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "r"), SetScrollRegion);
    }
}
