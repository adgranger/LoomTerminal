// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Data;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>Maps the collected intermediate character to a G level. Used by <see cref="SelectCharset"/>.</summary>
    private static readonly Dictionary<char, int> GLevels = new()
    {
        ['('] = 0,
        [')'] = 1,
        ['*'] = 2,
        ['+'] = 3,
        ['-'] = 1,
        ['.'] = 2,
    };

    /// <summary>DECSC (ESC 7) and SCOSC (CSI s): saves the cursor position, attributes and character set.</summary>
    public bool SaveCursor(IParams? p = null)
    {
        _activeBuffer.SavedX = _activeBuffer.X;
        _activeBuffer.SavedY = _activeBuffer.YBase + _activeBuffer.Y;
        _activeBuffer.SavedCurAttrData.Fg = _curAttrData.Fg;
        _activeBuffer.SavedCurAttrData.Bg = _curAttrData.Bg;
        _activeBuffer.SavedCharset = _charsetService.Charset;
        return true;
    }

    /// <summary>DECRC (ESC 8) and SCORC (CSI u): restores what <see cref="SaveCursor"/> saved.</summary>
    public bool RestoreCursor(IParams? p = null)
    {
        _activeBuffer.X = _activeBuffer.SavedX;
        _activeBuffer.Y = Math.Max(_activeBuffer.SavedY - _activeBuffer.YBase, 0);
        _curAttrData.Fg = _activeBuffer.SavedCurAttrData.Fg;
        _curAttrData.Bg = _activeBuffer.SavedCurAttrData.Bg;
        _charsetService.Charset = null;
        if (_activeBuffer.SavedCharset is not null)
        {
            _charsetService.Charset = _activeBuffer.SavedCharset;
        }

        RestrictCursor();
        return true;
    }

    /// <summary>NEL (ESC E, C1 NEL): moves to the first column of the next line.</summary>
    public bool NextLine()
    {
        _activeBuffer.X = 0;
        Index();
        return true;
    }

    /// <summary>DECKPAM (ESC =): application keypad mode.</summary>
    public bool KeypadApplicationMode()
    {
        _logService.Debug("Serial port requested application keypad.");
        _coreService.DecPrivateModes.ApplicationKeypad = true;
        _onRequestSyncScrollBar.Fire(EventArgs.Empty);
        return true;
    }

    /// <summary>DECKPNM (ESC &gt;): numeric keypad mode.</summary>
    public bool KeypadNumericMode()
    {
        _logService.Debug("Switching back to normal keypad.");
        _coreService.DecPrivateModes.ApplicationKeypad = false;
        _onRequestSyncScrollBar.Fire(EventArgs.Empty);
        return true;
    }

    /// <summary>ESC % @ and ESC % G: selects the default character set (ISO 8859-1 / UTF-8).</summary>
    public bool SelectDefaultCharset()
    {
        _charsetService.SetgLevel(0);
        _charsetService.SetgCharset(0, Charsets.DefaultCharset); // US (default)
        return true;
    }

    /// <summary>ESC ( C, ESC ) C, ESC * C, ESC + C, ESC - C, ESC . C: designates a G0 to G3 character set.</summary>
    /// <param name="collectAndFlag">The collected intermediate followed by the final character.</param>
    public bool SelectCharset(string collectAndFlag)
    {
        if (collectAndFlag.Length != 2)
        {
            SelectDefaultCharset();
            return true;
        }

        if (collectAndFlag[0] == '/')
        {
            return true; // TODO: Is this supported?
        }

        Charsets.All.TryGetValue(collectAndFlag[1], out IReadOnlyDictionary<char, string>? charset);
        _charsetService.SetgCharset(GLevels[collectAndFlag[0]], charset ?? Charsets.DefaultCharset);
        return true;
    }

    /// <summary>IND (ESC D, C1 IND): moves the cursor one line down, scrolling if needed.</summary>
    public bool Index()
    {
        RestrictCursor();
        _activeBuffer.Y++;
        if (_activeBuffer.Y == _activeBuffer.ScrollBottom + 1)
        {
            _activeBuffer.Y--;
            _bufferService.Scroll(EraseAttrData());
        }
        else if (_activeBuffer.Y >= _bufferService.Rows)
        {
            _activeBuffer.Y = _bufferService.Rows - 1;
        }

        RestrictCursor();
        return true;
    }

    /// <summary>RI (ESC M): moves the cursor one line up, scrolling down if at the top margin.</summary>
    public bool ReverseIndex()
    {
        RestrictCursor();
        if (_activeBuffer.Y == _activeBuffer.ScrollTop)
        {
            // possibly move the code below to term.reverseScroll();
            // test: echo -ne '\e[1;1H\e[44m\eM\e[0m'
            // blankLine(true) is xterm/linux behavior
            int scrollRegionHeight = _activeBuffer.ScrollBottom - _activeBuffer.ScrollTop;
            _activeBuffer.Lines.ShiftElements(_activeBuffer.YBase + _activeBuffer.Y, scrollRegionHeight, 1);
            _activeBuffer.Lines.Set(_activeBuffer.YBase + _activeBuffer.Y, _activeBuffer.GetBlankLine(EraseAttrData()));
            _dirtyRowTracker.MarkRangeDirty(_activeBuffer.ScrollTop, _activeBuffer.ScrollBottom);
        }
        else
        {
            _activeBuffer.Y--;
            RestrictCursor(); // quickfix to not run out of bounds
        }

        return true;
    }

    /// <summary>RIS (ESC c): resets the parser and requests a full reset of the terminal.</summary>
    public bool FullReset()
    {
        _parser.Reset();
        _onRequestReset.Fire(EventArgs.Empty);
        return true;
    }

    /// <summary>Resets the current attributes.</summary>
    public void Reset()
    {
        _curAttrData = AttributeData.DefaultAttrData.Clone();
        _eraseAttrDataInternal = AttributeData.DefaultAttrData.Clone();
    }

    /// <summary>LS2, LS3, LS1R, LS2R, LS3R: invokes the character set of a G level.</summary>
    public bool SetgLevel(int level)
    {
        _charsetService.SetgLevel(level);
        return true;
    }

    /// <summary>DECALN (ESC # 8): fills the screen with <c>E</c>.</summary>
    public bool ScreenAlignmentPattern()
    {
        // prepare cell data
        CellData cell = new()
        {
            Content = (1u << Content.WidthShift) | 'E',
            Fg = _curAttrData.Fg,
            Bg = _curAttrData.Bg,
        };

        SetCursor(0, 0);
        for (int yOffset = 0; yOffset < _bufferService.Rows; ++yOffset)
        {
            int row = _activeBuffer.YBase + _activeBuffer.Y + yOffset;
            IBufferLine? line = _activeBuffer.Lines.Get(row);
            if (line is not null)
            {
                line.Fill(cell);
                line.IsWrapped = false;
            }
        }

        _dirtyRowTracker.MarkAllDirty();
        SetCursor(0, 0);
        return true;
    }

    private void RegisterEscHandlers()
    {
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "s"), p => SaveCursor(p));
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "u"), p => RestoreCursor(p));

        _parser.SetExecuteHandler(C1.Ind, Index);
        _parser.SetExecuteHandler(C1.Nel, NextLine);

        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "7"), () => SaveCursor());
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "8"), () => RestoreCursor());
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "D"), Index);
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "E"), NextLine);
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "M"), ReverseIndex);
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "="), KeypadApplicationMode);
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: ">"), KeypadNumericMode);
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "c"), FullReset);
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "n"), () => SetgLevel(2));
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "o"), () => SetgLevel(3));
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "|"), () => SetgLevel(3));
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "}"), () => SetgLevel(2));
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "~"), () => SetgLevel(1));
        _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: "%", Final: "@"), SelectDefaultCharset);
        _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: "%", Final: "G"), SelectDefaultCharset);
        foreach (char flag in Charsets.All.Keys)
        {
            string finalChar = flag.ToString();
            _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: "(", Final: finalChar), () => SelectCharset("(" + finalChar));
            _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: ")", Final: finalChar), () => SelectCharset(")" + finalChar));
            _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: "*", Final: finalChar), () => SelectCharset("*" + finalChar));
            _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: "+", Final: finalChar), () => SelectCharset("+" + finalChar));
            _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: "-", Final: finalChar), () => SelectCharset("-" + finalChar));
            _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: ".", Final: finalChar), () => SelectCharset("." + finalChar));
            _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: "/", Final: finalChar), () => SelectCharset("/" + finalChar)); // TODO: supported?
        }

        _parser.RegisterEscHandler(new FunctionIdentifier(Intermediates: "#", Final: "8"), ScreenAlignmentPattern);
    }
}
