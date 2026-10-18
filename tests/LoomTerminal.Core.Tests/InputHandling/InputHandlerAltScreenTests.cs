// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerAltScreenTests : InputHandlerTestBase
{
    private int FgColorOfCell(int row, int col) => BufferService.Buffer.Lines.Get(row)!.LoadCell(col, new CellData()).GetFgColor();

    // xterm.js: 'InputHandler' › 'alt screen' › 'should handle DECSET/DECRST 47 (alt screen buffer)'
    [Fact]
    public void DECSET_47_switches_to_the_alt_screen_buffer()
    {
        InputHandler.Parse("\x1b[?47h\r\n\x1b[31mJUNK\x1b[?47lTEST");
        Assert.Equal(string.Empty, BufferService.Buffer.TranslateBufferLineToString(0, true));
        Assert.Equal("    TEST", BufferService.Buffer.TranslateBufferLineToString(1, true));

        // Text color of 'TEST' should be red
        Assert.Equal(1, FgColorOfCell(1, 4));
    }

    // xterm.js: 'InputHandler' › 'alt screen' › 'should handle DECSET/DECRST 1047 (alt screen buffer)'
    [Fact]
    public void DECSET_1047_switches_to_the_alt_screen_buffer()
    {
        InputHandler.Parse("\x1b[?1047h\r\n\x1b[31mJUNK\x1b[?1047lTEST");
        Assert.Equal(string.Empty, BufferService.Buffer.TranslateBufferLineToString(0, true));
        Assert.Equal("    TEST", BufferService.Buffer.TranslateBufferLineToString(1, true));

        // Text color of 'TEST' should be red
        Assert.Equal(1, FgColorOfCell(1, 4));
    }

    // xterm.js: 'InputHandler' › 'alt screen' › 'should handle DECSET/DECRST 1048 (alt screen cursor)'
    [Fact]
    public void DECSET_1048_saves_and_restores_the_cursor()
    {
        InputHandler.Parse("\x1b[?1048h\r\n\x1b[31mJUNK\x1b[?1048lTEST");
        Assert.Equal("TEST", BufferService.Buffer.TranslateBufferLineToString(0, true));
        Assert.Equal("JUNK", BufferService.Buffer.TranslateBufferLineToString(1, true));

        // Text color of 'TEST' should be default
        Assert.Equal(AttributeData.DefaultAttrData.Fg, BufferService.Buffer.Lines.Get(0)!.LoadCell(0, new CellData()).Fg);

        // Text color of 'JUNK' should be red
        Assert.Equal(1, FgColorOfCell(1, 0));
    }

    // xterm.js: 'InputHandler' › 'alt screen' › 'should handle DECSET/DECRST 1049 (alt screen buffer+cursor)'
    [Fact]
    public void DECSET_1049_switches_to_the_alt_screen_buffer_and_saves_the_cursor()
    {
        InputHandler.Parse("\x1b[?1049h\r\n\x1b[31mJUNK\x1b[?1049lTEST");
        Assert.Equal("TEST", BufferService.Buffer.TranslateBufferLineToString(0, true));
        Assert.Equal(string.Empty, BufferService.Buffer.TranslateBufferLineToString(1, true));

        // Text color of 'TEST' should be default
        Assert.Equal(AttributeData.DefaultAttrData.Fg, BufferService.Buffer.Lines.Get(0)!.LoadCell(0, new CellData()).Fg);
    }

    // xterm.js: 'InputHandler' › 'alt screen' › 'should handle DECSET/DECRST 1049 - maintains saved cursor for alt buffer'
    [Fact]
    public void DECSET_1049_maintains_the_saved_cursor_for_the_alt_buffer()
    {
        InputHandler.Parse("\x1b[?1049h\r\n\x1b[31m\x1b[s\x1b[?1049lTEST");
        Assert.Equal("TEST", BufferService.Buffer.TranslateBufferLineToString(0, true));

        // Text color of 'TEST' should be default
        Assert.Equal(AttributeData.DefaultAttrData.Fg, BufferService.Buffer.Lines.Get(0)!.LoadCell(0, new CellData()).Fg);
        InputHandler.Parse("\x1b[?1049h\x1b[uTEST");
        Assert.Equal("TEST", BufferService.Buffer.TranslateBufferLineToString(1, true));

        // Text color of 'TEST' should be red
        Assert.Equal(1, FgColorOfCell(1, 0));
    }

    // xterm.js: 'InputHandler' › 'alt screen' › 'should handle DECSET/DECRST 1049 - clears alt buffer with erase attributes'
    [Fact]
    public void DECSET_1049_clears_the_alt_buffer_with_erase_attributes()
    {
        InputHandler.Parse("\x1b[42m\x1b[?1049h");

        // Buffer should be filled with green background
        Assert.Equal(2, BufferService.Buffer.Lines.Get(20)!.LoadCell(10, new CellData()).GetBgColor());
    }
}
