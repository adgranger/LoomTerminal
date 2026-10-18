// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>IL (CSI Ps L): inserts <c>Ps</c> blank lines at the cursor row (default 1), within the scroll margins.</summary>
    public bool InsertLines(IParams p)
    {
        RestrictCursor();
        int param = OrOne(p);

        if (_activeBuffer.Y > _activeBuffer.ScrollBottom || _activeBuffer.Y < _activeBuffer.ScrollTop)
        {
            return true;
        }

        int row = _activeBuffer.YBase + _activeBuffer.Y;

        int scrollBottomRowsOffset = _bufferService.Rows - 1 - _activeBuffer.ScrollBottom;
        int scrollBottomAbsolute = _bufferService.Rows - 1 + _activeBuffer.YBase - scrollBottomRowsOffset + 1;
        while (param-- != 0)
        {
            // test: echo -e '\e[44m\e[1L\e[0m'
            // blankLine(true) - xterm/linux behavior
            _activeBuffer.Lines.Splice(scrollBottomAbsolute - 1, 1);
            _activeBuffer.Lines.Splice(row, 0, _activeBuffer.GetBlankLine(EraseAttrData()));
        }

        _dirtyRowTracker.MarkRangeDirty(_activeBuffer.Y, _activeBuffer.ScrollBottom);
        _activeBuffer.X = 0; // see https://vt100.net/docs/vt220-rm/chapter4.html - vt220 only?
        return true;
    }

    /// <summary>DL (CSI Ps M): deletes <c>Ps</c> lines at the cursor row (default 1), within the scroll margins.</summary>
    public bool DeleteLines(IParams p)
    {
        RestrictCursor();
        int param = OrOne(p);

        if (_activeBuffer.Y > _activeBuffer.ScrollBottom || _activeBuffer.Y < _activeBuffer.ScrollTop)
        {
            return true;
        }

        int row = _activeBuffer.YBase + _activeBuffer.Y;

        int j = _bufferService.Rows - 1 - _activeBuffer.ScrollBottom;
        j = _bufferService.Rows - 1 + _activeBuffer.YBase - j;
        while (param-- != 0)
        {
            // test: echo -e '\e[44m\e[1M\e[0m'
            // blankLine(true) - xterm/linux behavior
            _activeBuffer.Lines.Splice(row, 1);
            _activeBuffer.Lines.Splice(j, 0, _activeBuffer.GetBlankLine(EraseAttrData()));
        }

        _dirtyRowTracker.MarkRangeDirty(_activeBuffer.Y, _activeBuffer.ScrollBottom);
        _activeBuffer.X = 0; // see https://vt100.net/docs/vt220-rm/chapter4.html - vt220 only?
        return true;
    }

    /// <summary>ICH (CSI Ps @): inserts <c>Ps</c> blank characters at the cursor (default 1).</summary>
    public bool InsertChars(IParams p)
    {
        RestrictCursor();
        IBufferLine? line = _activeBuffer.Lines.Get(_activeBuffer.YBase + _activeBuffer.Y);
        if (line is not null)
        {
            line.InsertCells(_activeBuffer.X, OrOne(p), _activeBuffer.GetNullCell(EraseAttrData()));
            _dirtyRowTracker.MarkDirty(_activeBuffer.Y);
        }

        return true;
    }

    /// <summary>DCH (CSI Ps P): deletes <c>Ps</c> characters at the cursor (default 1).</summary>
    public bool DeleteChars(IParams p)
    {
        RestrictCursor();
        IBufferLine? line = _activeBuffer.Lines.Get(_activeBuffer.YBase + _activeBuffer.Y);
        if (line is not null)
        {
            line.DeleteCells(_activeBuffer.X, OrOne(p), _activeBuffer.GetNullCell(EraseAttrData()));
            _dirtyRowTracker.MarkDirty(_activeBuffer.Y);
        }

        return true;
    }

    /// <summary>SU (CSI Ps S): scrolls the scroll region up <c>Ps</c> lines (default 1).</summary>
    public bool ScrollUp(IParams p)
    {
        int param = OrOne(p);

        while (param-- != 0)
        {
            _activeBuffer.Lines.Splice(_activeBuffer.YBase + _activeBuffer.ScrollTop, 1);
            _activeBuffer.Lines.Splice(_activeBuffer.YBase + _activeBuffer.ScrollBottom, 0, _activeBuffer.GetBlankLine(EraseAttrData()));
        }

        _dirtyRowTracker.MarkRangeDirty(_activeBuffer.ScrollTop, _activeBuffer.ScrollBottom);
        return true;
    }

    /// <summary>SD (CSI Ps T): scrolls the scroll region down <c>Ps</c> lines (default 1).</summary>
    public bool ScrollDown(IParams p)
    {
        int param = OrOne(p);

        while (param-- != 0)
        {
            _activeBuffer.Lines.Splice(_activeBuffer.YBase + _activeBuffer.ScrollBottom, 1);
            _activeBuffer.Lines.Splice(_activeBuffer.YBase + _activeBuffer.ScrollTop, 0, _activeBuffer.GetBlankLine(AttributeData.DefaultAttrData));
        }

        _dirtyRowTracker.MarkRangeDirty(_activeBuffer.ScrollTop, _activeBuffer.ScrollBottom);
        return true;
    }

    /// <summary>SL (CSI Ps SP @): scrolls the scroll region left <c>Ps</c> columns (default 1).</summary>
    public bool ScrollLeft(IParams p)
    {
        if (_activeBuffer.Y > _activeBuffer.ScrollBottom || _activeBuffer.Y < _activeBuffer.ScrollTop)
        {
            return true;
        }

        int param = OrOne(p);
        for (int y = _activeBuffer.ScrollTop; y <= _activeBuffer.ScrollBottom; ++y)
        {
            IBufferLine line = _activeBuffer.Lines.Get(_activeBuffer.YBase + y)!;
            line.DeleteCells(0, param, _activeBuffer.GetNullCell(EraseAttrData()));
            line.IsWrapped = false;
        }

        _dirtyRowTracker.MarkRangeDirty(_activeBuffer.ScrollTop, _activeBuffer.ScrollBottom);
        return true;
    }

    /// <summary>SR (CSI Ps SP A): scrolls the scroll region right <c>Ps</c> columns (default 1).</summary>
    public bool ScrollRight(IParams p)
    {
        if (_activeBuffer.Y > _activeBuffer.ScrollBottom || _activeBuffer.Y < _activeBuffer.ScrollTop)
        {
            return true;
        }

        int param = OrOne(p);
        for (int y = _activeBuffer.ScrollTop; y <= _activeBuffer.ScrollBottom; ++y)
        {
            IBufferLine line = _activeBuffer.Lines.Get(_activeBuffer.YBase + y)!;
            line.InsertCells(0, param, _activeBuffer.GetNullCell(EraseAttrData()));
            line.IsWrapped = false;
        }

        _dirtyRowTracker.MarkRangeDirty(_activeBuffer.ScrollTop, _activeBuffer.ScrollBottom);
        return true;
    }

    /// <summary>DECIC (CSI Ps ' }): inserts <c>Ps</c> columns at the cursor column in the scroll region (default 1).</summary>
    public bool InsertColumns(IParams p)
    {
        if (_activeBuffer.Y > _activeBuffer.ScrollBottom || _activeBuffer.Y < _activeBuffer.ScrollTop)
        {
            return true;
        }

        int param = OrOne(p);
        for (int y = _activeBuffer.ScrollTop; y <= _activeBuffer.ScrollBottom; ++y)
        {
            IBufferLine line = _activeBuffer.Lines.Get(_activeBuffer.YBase + y)!;
            line.InsertCells(_activeBuffer.X, param, _activeBuffer.GetNullCell(EraseAttrData()));
            line.IsWrapped = false;
        }

        _dirtyRowTracker.MarkRangeDirty(_activeBuffer.ScrollTop, _activeBuffer.ScrollBottom);
        return true;
    }

    /// <summary>DECDC (CSI Ps ' ~): deletes <c>Ps</c> columns at the cursor column in the scroll region (default 1).</summary>
    public bool DeleteColumns(IParams p)
    {
        if (_activeBuffer.Y > _activeBuffer.ScrollBottom || _activeBuffer.Y < _activeBuffer.ScrollTop)
        {
            return true;
        }

        int param = OrOne(p);
        for (int y = _activeBuffer.ScrollTop; y <= _activeBuffer.ScrollBottom; ++y)
        {
            IBufferLine line = _activeBuffer.Lines.Get(_activeBuffer.YBase + y)!;
            line.DeleteCells(_activeBuffer.X, param, _activeBuffer.GetNullCell(EraseAttrData()));
            line.IsWrapped = false;
        }

        _dirtyRowTracker.MarkRangeDirty(_activeBuffer.ScrollTop, _activeBuffer.ScrollBottom);
        return true;
    }

    private void RegisterInsertDeleteHandlers()
    {
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "@"), InsertChars);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Intermediates: " ", Final: "@"), ScrollLeft);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Intermediates: " ", Final: "A"), ScrollRight);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "L"), InsertLines);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "M"), DeleteLines);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "P"), DeleteChars);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "S"), ScrollUp);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "T"), ScrollDown);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Intermediates: "'", Final: "}"), InsertColumns);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Intermediates: "'", Final: "~"), DeleteColumns);
    }
}
