// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Data;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>Limit length of title and icon name stacks.</summary>
    private const int StackLimit = 10;

    /// <summary>XTWINOPS (CSI Ps ; Ps ; Ps t): window manipulation and reports, gated by the <c>WindowOptions</c> option.</summary>
    public bool WindowOptions(IParams p)
    {
        if (!ParamToWindowOption(p.ParamValues[0], _optionsService.RawOptions.WindowOptions))
        {
            return true;
        }

        int second = p.Length > 1 ? p.ParamValues[1] : 0;
        switch (p.ParamValues[0])
        {
            case 14: // GetWinSizePixels, returns CSI 4 ; height ; width t
                if (second != 2)
                {
                    _onRequestWindowsOptionsReport.Fire(WindowsOptionsReportType.GetWinSizePixels);
                }

                break;
            case 16: // GetCellSizePixels, returns CSI 6 ; height ; width t
                _onRequestWindowsOptionsReport.Fire(WindowsOptionsReportType.GetCellSizePixels);
                break;
            case 18: // GetWinSizeChars, returns CSI 8 ; height ; width t
                _coreService.TriggerDataEvent($"{C0.Esc}[8;{_bufferService.Rows};{_bufferService.Cols}t");
                break;
            case 22: // PushTitle
                if (second == 0 || second == 2)
                {
                    _windowTitleStack.Add(_windowTitle);
                    if (_windowTitleStack.Count > StackLimit)
                    {
                        _windowTitleStack.RemoveAt(0);
                    }
                }

                if (second == 0 || second == 1)
                {
                    _iconNameStack.Add(_iconName);
                    if (_iconNameStack.Count > StackLimit)
                    {
                        _iconNameStack.RemoveAt(0);
                    }
                }

                break;
            case 23: // PopTitle
                if ((second == 0 || second == 2) && _windowTitleStack.Count > 0)
                {
                    SetTitle(PopLast(_windowTitleStack));
                }

                if ((second == 0 || second == 1) && _iconNameStack.Count > 0)
                {
                    SetIconName(PopLast(_iconNameStack));
                }

                break;
            default:
                break;
        }

        return true;
    }

    private static string PopLast(List<string> stack)
    {
        string last = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return last;
    }

    // map params to window option
    private static bool ParamToWindowOption(int n, Services.WindowOptions opts)
    {
        if (n > 24)
        {
            return opts.SetWinLines;
        }

        return n switch
        {
            1 => opts.RestoreWin,
            2 => opts.MinimizeWin,
            3 => opts.SetWinPosition,
            4 => opts.SetWinSizePixels,
            5 => opts.RaiseWin,
            6 => opts.LowerWin,
            7 => opts.RefreshWin,
            8 => opts.SetWinSizeChars,
            9 => opts.MaximizeWin,
            10 => opts.FullscreenWin,
            11 => opts.GetWinState,
            13 => opts.GetWinPosition,
            14 => opts.GetWinSizePixels,
            15 => opts.GetScreenSizePixels,
            16 => opts.GetCellSizePixels,
            18 => opts.GetWinSizeChars,
            19 => opts.GetScreenSizeChars,
            20 => opts.GetIconTitle,
            21 => opts.GetWinTitle,
            22 => opts.PushTitle,
            23 => opts.PopTitle,
            24 => opts.SetWinLines,
            _ => false,
        };
    }

    private void RegisterWindowOptionHandlers() =>
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "t"), WindowOptions);
}
