// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    private string _windowTitle = string.Empty;
    private string _iconName = string.Empty;

    /// <summary>OSC 0 and OSC 2 (set window title): sets the title and raises <see cref="OnTitleChange"/>.</summary>
    public bool SetTitle(string data)
    {
        _windowTitle = data;
        _onTitleChange.Fire(data);
        return true;
    }

    /// <summary>OSC 1 (set icon name): the icon name is not exposed.</summary>
    public bool SetIconName(string data)
    {
        _iconName = data;
        return true;
    }

    private void RegisterOscHandlers()
    {
        _parser.RegisterOscHandler(0, new OscHandler(data =>
        {
            SetTitle(data);
            SetIconName(data);
            return true;
        }));
        _parser.RegisterOscHandler(1, new OscHandler(SetIconName));
        _parser.RegisterOscHandler(2, new OscHandler(SetTitle));
    }
}
