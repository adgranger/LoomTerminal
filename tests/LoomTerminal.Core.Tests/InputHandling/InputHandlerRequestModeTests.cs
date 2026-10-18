// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerRequestModeTests : InputHandlerTestBase
{
    private readonly List<string> _reportStack = [];

    public InputHandlerRequestModeTests() => CoreService.OnData.Subscribe(_reportStack.Add);

    private string? Pop()
    {
        if (_reportStack.Count == 0)
        {
            return null;
        }

        string last = _reportStack[^1];
        _reportStack.RemoveAt(_reportStack.Count - 1);
        return last;
    }

    // xterm.js: 'InputHandler' › 'DECRQM' › 'ANSI 2 (keyboard action mode)'
    [Fact]
    public void ANSI_2_keyboard_action_mode_is_always_reset()
    {
        InputHandler.Parse("\x1b[2$p");
        Assert.Equal("\x1b[2;4$y", Pop());
    }

    // xterm.js: 'InputHandler' › 'DECRQM' › 'ANSI 4 (insert mode)'
    [Fact]
    public void ANSI_4_insert_mode_follows_SM_and_RM()
    {
        InputHandler.Parse("\x1b[4$p");
        Assert.Equal("\x1b[4;2$y", Pop()); // reset by default
        InputHandler.Parse("\x1b[4h");
        InputHandler.Parse("\x1b[4$p");
        Assert.Equal("\x1b[4;1$y", Pop()); // now active
        InputHandler.Parse("\x1b[4l");
        InputHandler.Parse("\x1b[4$p");
        Assert.Equal("\x1b[4;2$y", Pop()); // again reset
    }

    // xterm.js: 'InputHandler' › 'DECRQM' › 'ANSI 12 (send/receive)'
    [Fact]
    public void ANSI_12_send_receive_is_always_set()
    {
        InputHandler.Parse("\x1b[12$p");
        Assert.Equal("\x1b[12;3$y", Pop());
    }

    // xterm.js: 'InputHandler' › 'DECRQM' › 'ANSI 20 (newline mode)'
    [Fact]
    public void ANSI_20_newline_mode_follows_SM_and_RM()
    {
        InputHandler.Parse("\x1b[20$p");
        Assert.Equal("\x1b[20;2$y", Pop()); // reset by default
        InputHandler.Parse("\x1b[20h");
        InputHandler.Parse("\x1b[20$p");
        Assert.Equal("\x1b[20;1$y", Pop()); // now active
        InputHandler.Parse("\x1b[20l");
        InputHandler.Parse("\x1b[20$p");
        Assert.Equal("\x1b[20;2$y", Pop()); // again reset
    }

    // xterm.js: 'InputHandler' › 'DECRQM' › 'ANSI unknown'
    [Fact]
    public void ANSI_unknown_modes_are_not_recognized()
    {
        InputHandler.Parse("\x1b[1234$p");
        Assert.Equal("\x1b[1234;0$y", Pop());
    }

    // xterm.js: 'InputHandler' › 'DECRQM' › 'DEC privates with set/reset semantic'
    [Fact]
    public void DEC_privates_with_set_reset_semantic()
    {
        // initially reset
        int[] reset = [1, 6, 9, 12, 45, 66, 1000, 1002, 1003, 1004, 1006, 1016, 47, 1047, 1049, 2004, 2026];
        foreach (int mode in reset)
        {
            InputHandler.Parse($"\x1b[?{mode}$p");
            Assert.Equal($"\x1b[?{mode};2$y", Pop()); // initial reset
            InputHandler.Parse($"\x1b[?{mode}h");
            InputHandler.Parse($"\x1b[?{mode}$p");
            Assert.Equal($"\x1b[?{mode};1$y", Pop()); // now active
            InputHandler.Parse($"\x1b[?{mode}l");
            InputHandler.Parse($"\x1b[?{mode}$p");
            Assert.Equal($"\x1b[?{mode};2$y", Pop()); // again reset
        }

        // initially set
        int[] set = [7, 25];
        foreach (int mode in set)
        {
            InputHandler.Parse($"\x1b[?{mode}$p");
            Assert.Equal($"\x1b[?{mode};1$y", Pop()); // initial set
            InputHandler.Parse($"\x1b[?{mode}l");
            InputHandler.Parse($"\x1b[?{mode}$p");
            Assert.Equal($"\x1b[?{mode};2$y", Pop()); // now inactive
            InputHandler.Parse($"\x1b[?{mode}h");
            InputHandler.Parse($"\x1b[?{mode}$p");
            Assert.Equal($"\x1b[?{mode};1$y", Pop()); // again set
        }
    }

    // xterm.js: 'InputHandler' › 'DECRQM' › 'DEC privates perma modes'
    [Fact]
    public void DEC_privates_perma_modes()
    {
        // [mode number, state value]
        (int Mode, int Value)[] perma = [(3, 0), (8, 3), (67, 4), (1005, 4), (1015, 4), (1048, 1)];
        foreach ((int mode, int value) in perma)
        {
            InputHandler.Parse($"\x1b[?{mode}$p");
            Assert.Equal($"\x1b[?{mode};{value}$y", Pop());
        }
    }
}
