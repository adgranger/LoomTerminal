// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>Restricts the cursor to the viewport size, or to the scroll margins in origin mode.</summary>
    private void RestrictCursor(int? maxCol = null)
    {
        int max = maxCol ?? _bufferService.Cols - 1;
        _activeBuffer.X = Math.Min(max, Math.Max(0, _activeBuffer.X));
        _activeBuffer.Y = _coreService.DecPrivateModes.Origin
            ? Math.Min(_activeBuffer.ScrollBottom, Math.Max(_activeBuffer.ScrollTop, _activeBuffer.Y))
            : Math.Min(_bufferService.Rows - 1, Math.Max(0, _activeBuffer.Y));
        _dirtyRowTracker.MarkDirty(_activeBuffer.Y);
    }

    /// <summary>CUU (CSI Ps A): moves the cursor up <c>Ps</c> times (default 1), stopping at the top scroll margin.</summary>
    public bool CursorUp(IParams p)
    {
        // stop at scrollTop
        int diffToTop = _activeBuffer.Y - _activeBuffer.ScrollTop;
        if (diffToTop >= 0)
        {
            MoveCursor(0, -Math.Min(diffToTop, OrOne(p)));
        }
        else
        {
            MoveCursor(0, -OrOne(p));
        }

        return true;
    }

    /// <summary>CUD (CSI Ps B): moves the cursor down <c>Ps</c> times (default 1), stopping at the bottom scroll margin.</summary>
    public bool CursorDown(IParams p)
    {
        // stop at scrollBottom
        int diffToBottom = _activeBuffer.ScrollBottom - _activeBuffer.Y;
        if (diffToBottom >= 0)
        {
            MoveCursor(0, Math.Min(diffToBottom, OrOne(p)));
        }
        else
        {
            MoveCursor(0, OrOne(p));
        }

        return true;
    }

    /// <summary>CUF (CSI Ps C): moves the cursor <c>Ps</c> times forward (default 1).</summary>
    public bool CursorForward(IParams p)
    {
        MoveCursor(OrOne(p), 0);
        return true;
    }

    /// <summary>CUB (CSI Ps D): moves the cursor <c>Ps</c> times backward (default 1).</summary>
    public bool CursorBackward(IParams p)
    {
        MoveCursor(-OrOne(p), 0);
        return true;
    }

    /// <summary>CNL (CSI Ps E): moves the cursor down <c>Ps</c> rows and to the first column.</summary>
    public bool CursorNextLine(IParams p)
    {
        CursorDown(p);
        _activeBuffer.X = 0;
        return true;
    }

    /// <summary>CPL (CSI Ps F): moves the cursor up <c>Ps</c> rows and to the first column.</summary>
    public bool CursorPrecedingLine(IParams p)
    {
        CursorUp(p);
        _activeBuffer.X = 0;
        return true;
    }

    /// <summary>CHA (CSI Ps G): moves the cursor to column <c>Ps</c> (default 1).</summary>
    public bool CursorCharAbsolute(IParams p)
    {
        SetCursor(OrOne(p) - 1, _activeBuffer.Y);
        return true;
    }

    /// <summary>CUP (CSI Ps ; Ps H): moves the cursor to row and column (default 1;1).</summary>
    public bool CursorPosition(IParams p)
    {
        SetCursor(
            // col
            p.Length >= 2 ? (p.ParamValues[1] == 0 ? 1 : p.ParamValues[1]) - 1 : 0,
            // row
            OrOne(p) - 1);
        return true;
    }

    /// <summary>HPA (CSI Ps `): moves the cursor to column <c>Ps</c> (default 1).</summary>
    public bool CharPosAbsolute(IParams p)
    {
        SetCursor(OrOne(p) - 1, _activeBuffer.Y);
        return true;
    }

    /// <summary>HPR (CSI Ps a): moves the cursor <c>Ps</c> columns to the right (default 1).</summary>
    public bool HPositionRelative(IParams p)
    {
        MoveCursor(OrOne(p), 0);
        return true;
    }

    /// <summary>VPA (CSI Ps d): moves the cursor to row <c>Ps</c> (default 1).</summary>
    public bool LinePosAbsolute(IParams p)
    {
        SetCursor(_activeBuffer.X, OrOne(p) - 1);
        return true;
    }

    /// <summary>VPR (CSI Ps e): moves the cursor <c>Ps</c> rows down (default 1).</summary>
    public bool VPositionRelative(IParams p)
    {
        MoveCursor(0, OrOne(p));
        return true;
    }

    /// <summary>HVP (CSI Ps ; Ps f): same as CUP.</summary>
    public bool HVPosition(IParams p)
    {
        CursorPosition(p);
        return true;
    }

    // JS: params.params[0] || 1
    private static int OrOne(IParams p) => p.ParamValues[0] == 0 ? 1 : p.ParamValues[0];

    /// <summary>Sets the absolute cursor position.</summary>
    private void SetCursor(int x, int y)
    {
        _dirtyRowTracker.MarkDirty(_activeBuffer.Y);
        if (_coreService.DecPrivateModes.Origin)
        {
            _activeBuffer.X = x;
            _activeBuffer.Y = _activeBuffer.ScrollTop + y;
        }
        else
        {
            _activeBuffer.X = x;
            _activeBuffer.Y = y;
        }

        RestrictCursor();
        _dirtyRowTracker.MarkDirty(_activeBuffer.Y);
    }

    /// <summary>Sets the relative cursor position.</summary>
    private void MoveCursor(int x, int y)
    {
        // for relative changes we have to make sure we are within 0 .. cols/rows - 1
        // before calculating the new position
        RestrictCursor();
        SetCursor(_activeBuffer.X + x, _activeBuffer.Y + y);
    }

    private void RegisterCursorHandlers()
    {
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "A"), CursorUp);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "B"), CursorDown);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "C"), CursorForward);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "D"), CursorBackward);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "E"), CursorNextLine);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "F"), CursorPrecedingLine);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "G"), CursorCharAbsolute);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "H"), CursorPosition);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "`"), CharPosAbsolute);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "a"), HPositionRelative);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "d"), LinePosAbsolute);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "e"), VPositionRelative);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "f"), HVPosition);
    }
}
