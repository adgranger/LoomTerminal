// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Data;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>BEL: rings the bell.</summary>
    public bool Bell()
    {
        _onRequestBell.Fire(EventArgs.Empty);
        return true;
    }

    /// <summary>
    /// LF, VT, FF: moves the cursor one row down, scrolling if needed. Scrolling is restricted to scroll margins and
    /// only happens on the bottom line.
    /// </summary>
    public bool LineFeed()
    {
        _dirtyRowTracker.MarkDirty(_activeBuffer.Y);
        if (_optionsService.RawOptions.ConvertEol)
        {
            _activeBuffer.X = 0;
        }

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
        else
        {
            // There was an explicit line feed (not just a carriage return), so clear the wrapped state of
            // the line. This is particularly important on conpty/Windows where revisiting lines to
            // reprint is common, especially on resize. Note that the windowsMode wrapped line heuristics
            // can mess with this so windowsMode should be disabled, which is recommended on Windows build
            // 21376 and above.
            _activeBuffer.Lines.Get(_activeBuffer.YBase + _activeBuffer.Y)!.IsWrapped = false;
        }

        // If the end of the line is hit, prevent this action from wrapping around to the next line.
        if (_activeBuffer.X >= _bufferService.Cols)
        {
            _activeBuffer.X--;
        }

        _dirtyRowTracker.MarkDirty(_activeBuffer.Y);

        _onLineFeed.Fire(EventArgs.Empty);
        return true;
    }

    /// <summary>CR: moves the cursor to the beginning of the row.</summary>
    public bool CarriageReturn()
    {
        _activeBuffer.X = 0;
        return true;
    }

    /// <summary>
    /// BS: moves the cursor one position to the left. By default it is not possible to move the cursor past the
    /// leftmost position. If reverse wrap-around (<c>CSI ? 45 h</c>) is set, a previous soft line wrap (DECAWM) can
    /// be undone with BS within the scroll margins; the cursor then wraps back to the end of the previous row. It is
    /// not possible to peek back into the scrollback with the cursor, so at the home position this has no effect.
    /// </summary>
    public bool Backspace()
    {
        // reverse wrap-around is disabled
        if (!_coreService.DecPrivateModes.ReverseWraparound)
        {
            RestrictCursor();
            if (_activeBuffer.X > 0)
            {
                _activeBuffer.X--;
            }

            return true;
        }

        // reverse wrap-around is enabled
        // other than for normal operation mode, reverse wrap-around allows the cursor
        // to be at x=cols to be able to address the last cell of a row by BS
        RestrictCursor(_bufferService.Cols);

        if (_activeBuffer.X > 0)
        {
            _activeBuffer.X--;
        }
        else
        {
            // reverse wrap-around handling:
            // Our implementation deviates from xterm on purpose. Details:
            // - only previous soft NLs can be reversed (isWrapped=true)
            // - only works within scrollborders (top/bottom, left/right not yet supported)
            // - cannot peek into scrollbuffer
            // - any cursor movement sequence keeps working as expected
            if (_activeBuffer.X == 0
                && _activeBuffer.Y > _activeBuffer.ScrollTop
                && _activeBuffer.Y <= _activeBuffer.ScrollBottom
                && _activeBuffer.Lines.Get(_activeBuffer.YBase + _activeBuffer.Y)?.IsWrapped == true)
            {
                _activeBuffer.Lines.Get(_activeBuffer.YBase + _activeBuffer.Y)!.IsWrapped = false;
                _activeBuffer.Y--;
                _activeBuffer.X = _bufferService.Cols - 1;

                // find last taken cell - last cell can have 3 different states:
                // - hasContent(true) + hasWidth(1): narrow char - we are done
                // - hasWidth(0): second part of wide char - we are done
                // - hasContent(false) + hasWidth(1): empty cell due to early wrapping wide char, go one
                //   cell further back
                IBufferLine line = _activeBuffer.Lines.Get(_activeBuffer.YBase + _activeBuffer.Y)!;
                if (line.HasWidth(_activeBuffer.X) != 0 && line.HasContent(_activeBuffer.X) == 0)
                {
                    _activeBuffer.X--;

                    // We do this only once, since width=1 + hasContent=false currently happens only once
                    // before early wrapping of a wide char.
                    // This needs to be fixed once we support graphemes taking more than 2 cells.
                }
            }
        }

        RestrictCursor();
        return true;
    }

    /// <summary>HT: moves the cursor to the next character tab stop.</summary>
    public bool Tab()
    {
        if (_activeBuffer.X >= _bufferService.Cols)
        {
            return true;
        }

        int originalX = _activeBuffer.X;
        _activeBuffer.X = _activeBuffer.NextStop();
        if (_optionsService.RawOptions.ScreenReaderMode)
        {
            _onA11yTab.Fire(_activeBuffer.X - originalX);
        }

        return true;
    }

    /// <summary>SO: switches to the alternate character set (invokes G1).</summary>
    public bool ShiftOut()
    {
        _charsetService.SetgLevel(1);
        return true;
    }

    /// <summary>SI: returns to the regular character set after SO (invokes G0).</summary>
    public bool ShiftIn()
    {
        _charsetService.SetgLevel(0);
        return true;
    }

    private void RegisterControlHandlers()
    {
        _parser.SetExecuteHandler(C0.Bel, Bell);
        _parser.SetExecuteHandler(C0.Lf, LineFeed);
        _parser.SetExecuteHandler(C0.Vt, LineFeed);
        _parser.SetExecuteHandler(C0.Ff, LineFeed);
        _parser.SetExecuteHandler(C0.Cr, CarriageReturn);
        _parser.SetExecuteHandler(C0.Bs, Backspace);
        _parser.SetExecuteHandler(C0.Ht, Tab);
        _parser.SetExecuteHandler(C0.So, ShiftOut);
        _parser.SetExecuteHandler(C0.Si, ShiftIn);
    }
}
