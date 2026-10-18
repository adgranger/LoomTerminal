// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerDcsTests : InputHandlerTestBase
{
    private readonly List<string> _sendStack = [];

    public InputHandlerDcsTests() => CoreService.OnData.Subscribe(_sendStack.Add);

    private string Pop()
    {
        string last = _sendStack[^1];
        _sendStack.RemoveAt(_sendStack.Count - 1);
        return last;
    }

    // xterm.js: 'InputHandler' › 'DECSCA and DECSED/DECSEL' › 'DECRQSS reports correct DECSCA state'
    [Fact]
    public void DECRQSS_reports_the_DECSCA_state()
    {
        // DCS $ q " q ST
        InputHandler.Parse("\x1bP$q\"q\x1b\\");

        // default - DECSCA unset (0 or 2)
        Assert.Equal("\x1bP1$r0\"q\x1b\\", Pop());

        // DECSCA 1 - protected set
        InputHandler.Parse("###\x1b[1\"q");
        InputHandler.Parse("\x1bP$q\"q\x1b\\");
        Assert.Equal("\x1bP1$r1\"q\x1b\\", Pop());

        // DECSCA 2 - protected reset (same as 0)
        InputHandler.Parse("###\x1b[2\"q");
        InputHandler.Parse("\x1bP$q\"q\x1b\\");
        Assert.Equal("\x1bP1$r0\"q\x1b\\", Pop()); // reported as DECSCA 0
    }

    [Fact]
    public void DECRQSS_reports_the_SGR_state_as_0m()
    {
        InputHandler.Parse("\x1b[1;31m\x1bP$qm\x1b\\");
        Assert.Equal("\x1bP1$r0m\x1b\\", Pop());
    }

    [Fact]
    public void DECRQSS_reports_the_scroll_region()
    {
        BufferService.Resize(10, 10);
        InputHandler.Parse("\x1b[3;7r\x1bP$qr\x1b\\");
        Assert.Equal("\x1bP1$r3;7r\x1b\\", Pop());
    }

    [Fact]
    public void DECRQSS_reports_the_cursor_style()
    {
        OptionsService.RawOptions = OptionsService.RawOptions with { CursorStyle = CursorStyle.Bar, CursorBlink = true };
        InputHandler.Parse("\x1bP$q q\x1b\\");
        Assert.Equal("\x1bP1$r5 q\x1b\\", Pop());
    }

    [Fact]
    public void DECRQSS_replies_with_the_invalid_form_for_an_unknown_request()
    {
        InputHandler.Parse("\x1bP$qxyz\x1b\\");
        Assert.Equal("\x1bP0$r\x1b\\", Pop());
    }
}
