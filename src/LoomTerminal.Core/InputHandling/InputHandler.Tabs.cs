// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Data;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>TBC (CSI Ps g): clears the tab stop at the cursor (0) or all tab stops (3).</summary>
    public bool TabClear(IParams p)
    {
        int param = p.ParamValues[0];
        if (param == 0)
        {
            _activeBuffer.Tabs.Remove(_activeBuffer.X);
        }
        else if (param == 3)
        {
            _activeBuffer.Tabs = [];
        }

        return true;
    }

    /// <summary>CHT (CSI Ps I): moves the cursor <c>Ps</c> tab stops forward (default 1).</summary>
    public bool CursorForwardTab(IParams p)
    {
        if (_activeBuffer.X >= _bufferService.Cols)
        {
            return true;
        }

        int param = OrOne(p);
        while (param-- != 0)
        {
            _activeBuffer.X = _activeBuffer.NextStop();
        }

        return true;
    }

    /// <summary>CBT (CSI Ps Z): moves the cursor <c>Ps</c> tab stops backward (default 1).</summary>
    public bool CursorBackwardTab(IParams p)
    {
        if (_activeBuffer.X >= _bufferService.Cols)
        {
            return true;
        }

        int param = OrOne(p);
        while (param-- != 0)
        {
            _activeBuffer.X = _activeBuffer.PrevStop();
        }

        return true;
    }

    /// <summary>HTS (ESC H, C1 HTS): sets a tab stop at the cursor column.</summary>
    public bool TabSet()
    {
        _activeBuffer.Tabs[_activeBuffer.X] = true;
        return true;
    }

    private void RegisterTabHandlers()
    {
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "I"), CursorForwardTab);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "Z"), CursorBackwardTab);
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "g"), TabClear);
        _parser.SetExecuteHandler(C1.Hts, TabSet);
        _parser.RegisterEscHandler(new FunctionIdentifier(Final: "H"), TabSet);
    }
}
