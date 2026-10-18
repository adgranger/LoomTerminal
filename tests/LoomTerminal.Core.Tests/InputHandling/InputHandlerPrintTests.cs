// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerPrintTests : InputHandlerTestBase
{
    // xterm.js: 'InputHandler' › 'print' › 'should not cause an infinite loop (regression test)'
    [Fact]
    public void Print_of_a_zero_width_space_does_not_loop_forever()
    {
        using var bufferService = new MockBufferService(80, 30);
        using var coreService = new MockCoreService();
        using var optionsService = new MockOptionsService();
        using var coreMouseService = new MockCoreMouseService();
        using var inputHandler = new TestInputHandler(
            bufferService,
            new MockCharsetService(),
            coreService,
            new MockLogService(),
            optionsService,
            new MockOscLinkService(),
            coreMouseService,
            new MockUnicodeService());
        uint[] container = new uint[10];
        container[0] = 0x200B;
        inputHandler.Print(container, 0, 1);
    }

    // xterm.js: 'InputHandler' › 'print' › 'should clear cells to the right on early wrap-around'
    [Fact]
    public void Print_clears_cells_to_the_right_on_early_wrap_around()
    {
        BufferService.Resize(5, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        InputHandler.Parse("12345");
        BufferService.Buffer.X = 0;
        InputHandler.Parse("￥￥￥");
        Assert.Equal<string>(["￥￥", "￥"], GetLines(2));
    }

    // xterm.js: 'InputHandler' › 'should correctly reset cells taken by wide chars' › 'print'
    [Fact]
    public void Print_resets_cells_taken_by_wide_chars()
    {
        BufferService.Resize(10, 5);
        OptionsService.RawOptions = OptionsService.RawOptions with { Scrollback = 1 };
        InputHandler.Parse("￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥");
        InputHandler.Parse("\x1b[H#");
        Assert.Equal<string>(["# ￥￥￥￥", "￥￥￥￥￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[1;6H######");
        Assert.Equal<string>(["# ￥ #####", "# ￥￥￥￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("#");
        Assert.Equal<string>(["# ￥ #####", "##￥￥￥￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("#");
        Assert.Equal<string>(["# ￥ #####", "### ￥￥￥", "￥￥￥￥￥", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[3;9H#");
        Assert.Equal<string>(["# ￥ #####", "### ￥￥￥", "￥￥￥￥#", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("#");
        Assert.Equal<string>(["# ￥ #####", "### ￥￥￥", "￥￥￥￥##", "￥￥￥￥￥", ""], GetLines());
        InputHandler.Parse("#");
        Assert.Equal<string>(["# ￥ #####", "### ￥￥￥", "￥￥￥￥##", "# ￥￥￥￥", ""], GetLines());
        InputHandler.Parse("\x1b[4;10H#");
        Assert.Equal<string>(["# ￥ #####", "### ￥￥￥", "￥￥￥￥##", "# ￥￥￥ #", ""], GetLines());
    }
}
