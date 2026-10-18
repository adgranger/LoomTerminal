// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Data;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>SM (CSI Pm h): sets ANSI modes (4: insert mode, 20: automatic new line).</summary>
    public bool SetMode(IParams p)
    {
        for (int i = 0; i < p.Length; i++)
        {
            switch (p.ParamValues[i])
            {
                case 4:
                    _coreService.Modes.InsertMode = true;
                    break;
                case 20:
                    _optionsService.SetOptions(_optionsService.RawOptions with { ConvertEol = true });
                    break;
                default:
                    break;
            }
        }

        return true;
    }

    /// <summary>DECSET (CSI ? Pm h): sets DEC private modes.</summary>
    public bool SetModePrivate(IParams p)
    {
        for (int i = 0; i < p.Length; i++)
        {
            switch (p.ParamValues[i])
            {
                case 1:
                    _coreService.DecPrivateModes.ApplicationCursorKeys = true;
                    break;
                case 2:
                    _charsetService.SetgCharset(0, Charsets.DefaultCharset);
                    _charsetService.SetgCharset(1, Charsets.DefaultCharset);
                    _charsetService.SetgCharset(2, Charsets.DefaultCharset);
                    _charsetService.SetgCharset(3, Charsets.DefaultCharset);

                    // set VT100 mode here
                    break;
                case 3:
                    if (_optionsService.RawOptions.WindowOptions.SetWinLines)
                    {
                        _bufferService.Resize(132, _bufferService.Rows);
                        _onRequestReset.Fire(EventArgs.Empty);
                    }

                    break;
                case 6:
                    _coreService.DecPrivateModes.Origin = true;
                    SetCursor(0, 0);
                    break;
                case 7:
                    _coreService.DecPrivateModes.Wraparound = true;
                    break;
                case 12:
                    _optionsService.SetOptions(_optionsService.RawOptions with { CursorBlink = true });
                    break;
                case 45:
                    _coreService.DecPrivateModes.ReverseWraparound = true;
                    break;
                case 66:
                    _logService.Debug("Serial port requested application keypad.");
                    _coreService.DecPrivateModes.ApplicationKeypad = true;
                    _onRequestSyncScrollBar.Fire(EventArgs.Empty);
                    break;
                case 9: // X10 Mouse
                    // no release, no motion, no wheel, no modifiers.
                    _coreMouseService.ActiveProtocol = "X10";
                    break;
                case 1000: // vt200 mouse
                    // no motion.
                    _coreMouseService.ActiveProtocol = "VT200";
                    break;
                case 1002: // button event mouse
                    _coreMouseService.ActiveProtocol = "DRAG";
                    break;
                case 1003: // any event mouse
                    // any event - sends motion events,
                    // even if there is no button held down.
                    _coreMouseService.ActiveProtocol = "ANY";
                    break;
                case 1004: // send focusin/focusout events
                    // focusin: ^[[I
                    // focusout: ^[[O
                    _coreService.DecPrivateModes.SendFocus = true;
                    _onRequestSendFocus.Fire(EventArgs.Empty);
                    break;
                case 1005: // utf8 ext mode mouse - removed in #2507
                    _logService.Debug("DECSET 1005 not supported (see #2507)");
                    break;
                case 1006: // sgr ext mode mouse
                    _coreMouseService.ActiveEncoding = "SGR";
                    break;
                case 1015: // urxvt ext mode mouse - removed in #2507
                    _logService.Debug("DECSET 1015 not supported (see #2507)");
                    break;
                case 1016: // sgr pixels mode mouse
                    _coreMouseService.ActiveEncoding = "SGR_PIXELS";
                    break;
                case 25: // show cursor
                    _coreService.IsCursorHidden = false;
                    break;
                case 1048: // alt screen cursor
                    SaveCursor();
                    break;
                case 1049: // alt screen buffer cursor
                    SaveCursor();
                    ActivateAltBuffer();
                    break;
                case 47: // alt screen buffer
                case 1047: // alt screen buffer
                    ActivateAltBuffer();
                    break;
                case 2004: // bracketed paste mode (https://cirw.in/blog/bracketed-paste)
                    _coreService.DecPrivateModes.BracketedPasteMode = true;
                    break;
                case 2026: // synchronized output (https://github.com/contour-terminal/vt-extensions/blob/master/synchronized-output.md)
                    _coreService.DecPrivateModes.SynchronizedOutput = true;
                    break;
                default:
                    break;
            }
        }

        return true;
    }

    /// <summary>RM (CSI Pm l): resets ANSI modes (4: insert mode, 20: automatic new line).</summary>
    public bool ResetMode(IParams p)
    {
        for (int i = 0; i < p.Length; i++)
        {
            switch (p.ParamValues[i])
            {
                case 4:
                    _coreService.Modes.InsertMode = false;
                    break;
                case 20:
                    _optionsService.SetOptions(_optionsService.RawOptions with { ConvertEol = false });
                    break;
                default:
                    break;
            }
        }

        return true;
    }

    /// <summary>DECRST (CSI ? Pm l): resets DEC private modes.</summary>
    public bool ResetModePrivate(IParams p)
    {
        for (int i = 0; i < p.Length; i++)
        {
            switch (p.ParamValues[i])
            {
                case 1:
                    _coreService.DecPrivateModes.ApplicationCursorKeys = false;
                    break;
                case 3:
                    if (_optionsService.RawOptions.WindowOptions.SetWinLines)
                    {
                        _bufferService.Resize(80, _bufferService.Rows);
                        _onRequestReset.Fire(EventArgs.Empty);
                    }

                    break;
                case 6:
                    _coreService.DecPrivateModes.Origin = false;
                    SetCursor(0, 0);
                    break;
                case 7:
                    _coreService.DecPrivateModes.Wraparound = false;
                    break;
                case 12:
                    _optionsService.SetOptions(_optionsService.RawOptions with { CursorBlink = false });
                    break;
                case 45:
                    _coreService.DecPrivateModes.ReverseWraparound = false;
                    break;
                case 66:
                    _logService.Debug("Switching back to normal keypad.");
                    _coreService.DecPrivateModes.ApplicationKeypad = false;
                    _onRequestSyncScrollBar.Fire(EventArgs.Empty);
                    break;
                case 9: // X10 Mouse
                case 1000: // vt200 mouse
                case 1002: // button event mouse
                case 1003: // any event mouse
                    _coreMouseService.ActiveProtocol = "NONE";
                    break;
                case 1004: // send focusin/focusout events
                    _coreService.DecPrivateModes.SendFocus = false;
                    break;
                case 1005: // utf8 ext mode mouse - removed in #2507
                    _logService.Debug("DECRST 1005 not supported (see #2507)");
                    break;
                case 1006: // sgr ext mode mouse
                    _coreMouseService.ActiveEncoding = "DEFAULT";
                    break;
                case 1015: // urxvt ext mode mouse - removed in #2507
                    _logService.Debug("DECRST 1015 not supported (see #2507)");
                    break;
                case 1016: // sgr pixels mode mouse
                    _coreMouseService.ActiveEncoding = "DEFAULT";
                    break;
                case 25: // hide cursor
                    _coreService.IsCursorHidden = true;
                    break;
                case 1048: // alt screen cursor
                    RestoreCursor();
                    break;
                case 1049: // alt screen buffer cursor
                case 47: // normal screen buffer
                case 1047: // normal screen buffer - clearing it first
                    // Ensure the selection manager has the correct buffer
                    _bufferService.Buffers.ActivateNormalBuffer();
                    if (p.ParamValues[i] == 1049)
                    {
                        RestoreCursor();
                    }

                    _coreService.IsCursorInitialized = true;
                    _onRequestRefreshRows.Fire(null);
                    _onRequestSyncScrollBar.Fire(EventArgs.Empty);
                    break;
                case 2004: // bracketed paste mode (https://cirw.in/blog/bracketed-paste)
                    _coreService.DecPrivateModes.BracketedPasteMode = false;
                    break;
                case 2026: // synchronized output (https://github.com/contour-terminal/vt-extensions/blob/master/synchronized-output.md)
                    _coreService.DecPrivateModes.SynchronizedOutput = false;
                    _onRequestRefreshRows.Fire(null);
                    break;
                default:
                    break;
            }
        }

        return true;
    }

    private void ActivateAltBuffer()
    {
        _bufferService.Buffers.ActivateAltBuffer(EraseAttrData());
        _coreService.IsCursorInitialized = true;
        _onRequestRefreshRows.Fire(null);
        _onRequestSyncScrollBar.Fire(EventArgs.Empty);
    }

    private void RegisterModeHandlers()
    {
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "h"), SetMode);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Prefix: "?", Final: "h"), SetModePrivate);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "l"), ResetMode);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Prefix: "?", Final: "l"), ResetModePrivate);
    }
}
