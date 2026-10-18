// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Input;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>Prints the code points <paramref name="data"/>[<paramref name="start"/>..<paramref name="end"/>) at the cursor.</summary>
    public void Print(uint[] data, int start, int end)
    {
        int chWidth;
        IReadOnlyDictionary<char, string>? charset = _charsetService.Charset;
        bool screenReaderMode = _optionsService.RawOptions.ScreenReaderMode;
        int cols = _bufferService.Cols;
        bool wraparoundMode = _coreService.DecPrivateModes.Wraparound;
        bool insertMode = _coreService.Modes.InsertMode;
        AttributeData curAttr = _curAttrData;
        IBufferLine bufferRow = _activeBuffer.Lines.Get(_activeBuffer.YBase + _activeBuffer.Y)!;

        _dirtyRowTracker.MarkDirty(_activeBuffer.Y);

        // handle wide chars: reset start_cell-1 if we would overwrite the second cell of a wide char
        if (_activeBuffer.X != 0 && end - start > 0 && bufferRow.GetWidth(_activeBuffer.X - 1) == 2)
        {
            bufferRow.SetCellFromCodepoint(_activeBuffer.X - 1, 0, 1, curAttr);
        }

        int precedingJoinState = _parser.PrecedingJoinState;
        for (int pos = start; pos < end; ++pos)
        {
            uint code = data[pos];

            // get charset replacement character
            // charset is only defined for ASCII, therefore we only
            // search for an replacement char if code < 127
            if (code < 127 && charset is not null && charset.TryGetValue((char)code, out string? ch) && ch.Length > 0)
            {
                code = ch[0];
            }

            int currentInfo = _unicodeService.CharProperties((int)code, precedingJoinState);
            chWidth = UnicodeService.ExtractWidth(currentInfo);
            bool shouldJoin = UnicodeService.ExtractShouldJoin(currentInfo);
            int oldWidth = shouldJoin ? UnicodeService.ExtractWidth(precedingJoinState) : 0;
            precedingJoinState = currentInfo;

            if (screenReaderMode)
            {
                _onA11yChar.Fire(TextDecoder.StringFromCodePoint(code));
            }

            if (GetCurrentLinkId() != 0)
            {
                _oscLinkService.AddLineToLink(GetCurrentLinkId(), _activeBuffer.YBase + _activeBuffer.Y);
            }

            // goto next line if ch would overflow
            // NOTE: To avoid costly width checks here,
            // the terminal does not allow a cols < 2.
            if (_activeBuffer.X + chWidth - oldWidth > cols)
            {
                // autowrap - DECAWM
                // automatically wraps to the beginning of the next line
                if (wraparoundMode)
                {
                    IBufferLine oldRow = bufferRow;
                    int oldCol = _activeBuffer.X - oldWidth;
                    _activeBuffer.X = oldWidth;
                    _activeBuffer.Y++;
                    if (_activeBuffer.Y == _activeBuffer.ScrollBottom + 1)
                    {
                        _activeBuffer.Y--;
                        _bufferService.Scroll(EraseAttrData(), true);
                    }
                    else
                    {
                        if (_activeBuffer.Y >= _bufferService.Rows)
                        {
                            _activeBuffer.Y = _bufferService.Rows - 1;
                        }

                        // The line already exists (eg. the initial viewport), mark it as a
                        // wrapped line
                        _activeBuffer.Lines.Get(_activeBuffer.YBase + _activeBuffer.Y)!.IsWrapped = true;
                    }

                    // row changed, get it again
                    bufferRow = _activeBuffer.Lines.Get(_activeBuffer.YBase + _activeBuffer.Y)!;
                    if (oldWidth > 0 && bufferRow is BufferLine newLine)
                    {
                        // Combining character widens 1 column to 2.
                        // Move old character to next line.
                        newLine.CopyCellsFrom((BufferLine)oldRow, oldCol, 0, oldWidth, false);
                    }

                    // clear left over cells to the right
                    while (oldCol < cols)
                    {
                        oldRow.SetCellFromCodepoint(oldCol++, 0, 1, curAttr);
                    }
                }
                else
                {
                    _activeBuffer.X = cols - 1;
                    if (chWidth == 2)
                    {
                        // FIXME: check for xterm behavior
                        // What to do here? We got a wide char that does not fit into last cell
                        continue;
                    }
                }
            }

            // insert combining char at last cursor position
            // this._activeBuffer.x should never be 0 for a combining char
            // since they always follow a cell consuming char
            // therefore we can test for this._activeBuffer.x to avoid overflow left
            if (shouldJoin && _activeBuffer.X != 0)
            {
                int offset = bufferRow.GetWidth(_activeBuffer.X - 1) != 0 ? 1 : 2;

                // if empty cell after fullwidth, need to go 2 cells back
                // it is save to step 2 cells back here
                // since an empty cell is only set by fullwidth chars
                bufferRow.AddCodepointToCell(_activeBuffer.X - offset, code, chWidth);
                for (int delta = chWidth - oldWidth; --delta >= 0;)
                {
                    bufferRow.SetCellFromCodepoint(_activeBuffer.X++, 0, 0, curAttr);
                }

                continue;
            }

            // insert mode: move characters to right
            if (insertMode)
            {
                // right shift cells according to the width
                bufferRow.InsertCells(_activeBuffer.X, chWidth - oldWidth, _activeBuffer.GetNullCell(curAttr));

                // test last cell - since the last cell has only room for
                // a halfwidth char any fullwidth shifted there is lost
                // and will be set to empty cell
                if (bufferRow.GetWidth(cols - 1) == 2)
                {
                    bufferRow.SetCellFromCodepoint(cols - 1, BufferConstants.NullCellCode, BufferConstants.NullCellWidth, curAttr);
                }
            }

            // write current char to buffer and advance cursor
            bufferRow.SetCellFromCodepoint(_activeBuffer.X++, code, chWidth, curAttr);

            // fullwidth char - also set next cell to placeholder stub and advance cursor
            // for graphemes bigger than fullwidth we can simply loop to zero
            // we already made sure above, that this._activeBuffer.x + chWidth will not overflow right
            if (chWidth > 0)
            {
                while (--chWidth != 0)
                {
                    // other than a regular empty cell a cell following a wide char has no width
                    bufferRow.SetCellFromCodepoint(_activeBuffer.X++, 0, 0, curAttr);
                }
            }
        }

        _parser.PrecedingJoinState = precedingJoinState;

        // handle wide chars: reset cell to the right if it is second cell of a wide char
        if (_activeBuffer.X < cols && end - start > 0 && bufferRow.GetWidth(_activeBuffer.X) == 0 && bufferRow.HasContent(_activeBuffer.X) == 0)
        {
            bufferRow.SetCellFromCodepoint(_activeBuffer.X, 0, 1, curAttr);
        }

        _dirtyRowTracker.MarkDirty(_activeBuffer.Y);
    }

    private AttributeData EraseAttrData()
    {
        _eraseAttrDataInternal.Bg &= ~(Attributes.CmMask | 0xFFFFFFu);
        _eraseAttrDataInternal.Bg |= _curAttrData.Bg & ~0xFC000000u;
        return _eraseAttrDataInternal;
    }
}
