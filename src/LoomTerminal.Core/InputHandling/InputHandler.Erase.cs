// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>DECSCA (CSI Ps " q): marks the following characters as protected (1) or unprotected (0 or 2).</summary>
    public bool SelectProtected(IParams p)
    {
        int ps = p.ParamValues[0];
        if (ps == 1)
        {
            _curAttrData.Bg |= BgFlags.Protected;
        }

        if (ps == 2 || ps == 0)
        {
            _curAttrData.Bg &= ~BgFlags.Protected;
        }

        return true;
    }

    /// <summary>
    /// ED (CSI Ps J) and DECSED (CSI ? Ps J): erases in the display. 0: below, 1: above, 2: all, 3: scrollback.
    /// With <paramref name="respectProtect"/> protected cells are kept.
    /// </summary>
    public bool EraseInDisplay(IParams p, bool respectProtect = false)
    {
        RestrictCursor(_bufferService.Cols);
        int j;
        switch (p.ParamValues[0])
        {
            case 0:
                j = _activeBuffer.Y;
                _dirtyRowTracker.MarkDirty(j);
                EraseInBufferLine(j++, _activeBuffer.X, _bufferService.Cols, _activeBuffer.X == 0, respectProtect);
                for (; j < _bufferService.Rows; j++)
                {
                    ResetBufferLine(j, respectProtect);
                }

                _dirtyRowTracker.MarkDirty(j);
                break;
            case 1:
                j = _activeBuffer.Y;
                _dirtyRowTracker.MarkDirty(j);

                // Deleted front part of line and everything before. This line will no longer be wrapped.
                EraseInBufferLine(j, 0, _activeBuffer.X + 1, true, respectProtect);
                if (_activeBuffer.X + 1 >= _bufferService.Cols)
                {
                    // Deleted entire previous line. This next line can no longer be wrapped.
                    _activeBuffer.Lines.Get(j + 1)!.IsWrapped = false;
                }

                while (j-- != 0)
                {
                    ResetBufferLine(j, respectProtect);
                }

                _dirtyRowTracker.MarkDirty(0);
                break;
            case 2:
                if (_optionsService.RawOptions.ScrollOnEraseInDisplay)
                {
                    j = _bufferService.Rows;
                    _dirtyRowTracker.MarkRangeDirty(0, j - 1);
                    while (j-- != 0)
                    {
                        IBufferLine? currentLine = _activeBuffer.Lines.Get(_activeBuffer.YBase + j);
                        if (currentLine is not null && currentLine.GetTrimmedLength() != 0)
                        {
                            break;
                        }
                    }

                    for (; j >= 0; j--)
                    {
                        _bufferService.Scroll(EraseAttrData());
                    }
                }
                else
                {
                    j = _bufferService.Rows;
                    _dirtyRowTracker.MarkDirty(j - 1);
                    while (j-- != 0)
                    {
                        ResetBufferLine(j, respectProtect);
                    }

                    _dirtyRowTracker.MarkDirty(0);
                }

                break;
            case 3:
                // Clear scrollback (everything not in viewport)
                int scrollBackSize = _activeBuffer.Lines.Length - _bufferService.Rows;
                if (scrollBackSize > 0)
                {
                    _activeBuffer.Lines.TrimStart(scrollBackSize);
                    _activeBuffer.YBase = Math.Max(_activeBuffer.YBase - scrollBackSize, 0);
                    _activeBuffer.YDisp = Math.Max(_activeBuffer.YDisp - scrollBackSize, 0);

                    // Force a scroll event to refresh viewport
                    _onScroll.Fire(0);
                }

                break;
            default:
                break;
        }

        return true;
    }

    /// <summary>
    /// EL (CSI Ps K) and DECSEL (CSI ? Ps K): erases in the line. 0: to the right, 1: to the left, 2: all.
    /// With <paramref name="respectProtect"/> protected cells are kept.
    /// </summary>
    public bool EraseInLine(IParams p, bool respectProtect = false)
    {
        RestrictCursor(_bufferService.Cols);
        switch (p.ParamValues[0])
        {
            case 0:
                EraseInBufferLine(_activeBuffer.Y, _activeBuffer.X, _bufferService.Cols, _activeBuffer.X == 0, respectProtect);
                break;
            case 1:
                EraseInBufferLine(_activeBuffer.Y, 0, _activeBuffer.X + 1, false, respectProtect);
                break;
            case 2:
                EraseInBufferLine(_activeBuffer.Y, 0, _bufferService.Cols, true, respectProtect);
                break;
            default:
                break;
        }

        _dirtyRowTracker.MarkDirty(_activeBuffer.Y);
        return true;
    }

    /// <summary>ECH (CSI Ps X): erases <c>Ps</c> characters from the cursor (default 1) without moving it.</summary>
    public bool EraseChars(IParams p)
    {
        RestrictCursor();
        IBufferLine? line = _activeBuffer.Lines.Get(_activeBuffer.YBase + _activeBuffer.Y);
        if (line is not null)
        {
            line.ReplaceCells(
                _activeBuffer.X,
                _activeBuffer.X + OrOne(p),
                _activeBuffer.GetNullCell(EraseAttrData()));
            _dirtyRowTracker.MarkDirty(_activeBuffer.Y);
        }

        return true;
    }

    private void EraseInBufferLine(int y, int start, int end, bool clearWrap = false, bool respectProtect = false)
    {
        IBufferLine line = _activeBuffer.Lines.Get(_activeBuffer.YBase + y)!;
        line.ReplaceCells(start, end, _activeBuffer.GetNullCell(EraseAttrData()), respectProtect);
        if (clearWrap)
        {
            line.IsWrapped = false;
        }
    }

    private void ResetBufferLine(int y, bool respectProtect = false)
    {
        IBufferLine? line = _activeBuffer.Lines.Get(_activeBuffer.YBase + y);
        if (line is not null)
        {
            line.Fill(_activeBuffer.GetNullCell(EraseAttrData()), respectProtect);
            _bufferService.Buffer.ClearMarkers(_activeBuffer.YBase + y);
            line.IsWrapped = false;
        }
    }

    private void RegisterEraseHandlers()
    {
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "J"), p => EraseInDisplay(p, false));
        _parser.RegisterCsiHandler(new FunctionIdentifier(Prefix: "?", Final: "J"), p => EraseInDisplay(p, true));
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "K"), p => EraseInLine(p, false));
        _parser.RegisterCsiHandler(new FunctionIdentifier(Prefix: "?", Final: "K"), p => EraseInLine(p, true));
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "X"), EraseChars);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Intermediates: "\"", Final: "q"), SelectProtected);
    }
}
