// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerSgrColonTests : InputHandlerTestBase
{
    private readonly MockCoreMouseService _mouse = new();
    private readonly TestInputHandler _handler2;

    public InputHandlerSgrColonTests() => _handler2 = new TestInputHandler(
        BufferService, new MockCharsetService(), CoreService, new MockLogService(), OptionsService, OscLinkService, _mouse, new MockUnicodeService());

    // xterm.js: 'InputHandler' › 'colon notation' › 'should equal to semicolon' › 'CSI 38:2::50:100:150 m'
    [Fact]
    public void Csi_38_colon_2_colon_colon_50_colon_100_colon_150()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;2;50;100;150m");
        InputHandler.Parse("\x1b[38:2::50:100:150m");
        Assert.Equal((uint)((50 << 16) | (100 << 8) | 150), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should equal to semicolon' › 'CSI 38:2::50:100: m'
    [Fact]
    public void Csi_38_colon_2_colon_colon_50_colon_100_colon()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;2;50;100;m");
        InputHandler.Parse("\x1b[38:2::50:100:m");
        Assert.Equal((uint)((50 << 16) | (100 << 8) | 0), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should equal to semicolon' › 'CSI 38:2::50:: m'
    [Fact]
    public void Csi_38_colon_2_colon_colon_50_colon_colon()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;2;50;;m");
        InputHandler.Parse("\x1b[38:2::50::m");
        Assert.Equal((uint)((50 << 16) | (0 << 8) | 0), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should equal to semicolon' › 'CSI 38:2:::: m'
    [Fact]
    public void Csi_38_colon_2_colon_colon_colon_colon()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;2;;;m");
        InputHandler.Parse("\x1b[38:2::::m");
        Assert.Equal((uint)((0 << 16) | (0 << 8) | 0), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should equal to semicolon' › 'CSI 38;2::50:100:150 m'
    [Fact]
    public void Csi_38_semi_2_colon_colon_50_colon_100_colon_150()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;2;50;100;150m");
        InputHandler.Parse("\x1b[38;2::50:100:150m");
        Assert.Equal((uint)((50 << 16) | (100 << 8) | 150), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should equal to semicolon' › 'CSI 38;2;50:100:150 m'
    [Fact]
    public void Csi_38_semi_2_semi_50_colon_100_colon_150()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;2;50;100;150m");
        InputHandler.Parse("\x1b[38;2;50:100:150m");
        Assert.Equal((uint)((50 << 16) | (100 << 8) | 150), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should equal to semicolon' › 'CSI 38;2;50;100:150 m'
    [Fact]
    public void Csi_38_semi_2_semi_50_semi_100_colon_150()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;2;50;100;150m");
        InputHandler.Parse("\x1b[38;2;50;100:150m");
        Assert.Equal((uint)((50 << 16) | (100 << 8) | 150), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should equal to semicolon' › 'CSI 38:5:50 m'
    [Fact]
    public void Csi_38_colon_5_colon_50()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;5;50m");
        InputHandler.Parse("\x1b[38:5:50m");
        Assert.Equal((uint)(50), _handler2.CurAttrData.Fg & 0xFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should equal to semicolon' › 'CSI 38:5: m'
    [Fact]
    public void Csi_38_colon_5_colon()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;5;m");
        InputHandler.Parse("\x1b[38:5:m");
        Assert.Equal((uint)(0), _handler2.CurAttrData.Fg & 0xFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should equal to semicolon' › 'CSI 38;5:50 m'
    [Fact]
    public void Csi_38_semi_5_colon_50()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;5;50m");
        InputHandler.Parse("\x1b[38;5:50m");
        Assert.Equal((uint)(50), _handler2.CurAttrData.Fg & 0xFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should fill early sequence end with default of 0' › 'CSI 38:2 m'
    [Fact]
    public void Csi_38_colon_2()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;2m");
        InputHandler.Parse("\x1b[38:2m");
        Assert.Equal((uint)((0 << 16) | (0 << 8) | 0), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should fill early sequence end with default of 0' › 'CSI 38:5 m'
    [Fact]
    public void Csi_38_colon_5()
    {
        InputHandler.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.CurAttrData.Fg = 0xFFFFFFFF;
        _handler2.Parse("\x1b[38;5m");
        InputHandler.Parse("\x1b[38:5m");
        Assert.Equal((uint)(0), _handler2.CurAttrData.Fg & 0xFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should not interfere with leading/following SGR attrs' › 'CSI 1 ; 38:2::50:100:150 ; 4 m'
    [Fact]
    public void Csi_1_38_colon_2_colon_colon_50_colon_100_colon_150_4()
    {
        _handler2.Parse("\x1b[1;38;2;50;100;150;4m");
        InputHandler.Parse("\x1b[1;38:2::50:100:150;4m");
        Assert.NotEqual(0u, _handler2.CurAttrData.IsBold());
        Assert.NotEqual(0u, _handler2.CurAttrData.IsUnderline());
        Assert.Equal((uint)((50 << 16) | (100 << 8) | 150), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should not interfere with leading/following SGR attrs' › 'CSI 1 ; 38:2::50:100: ; 4 m'
    [Fact]
    public void Csi_1_38_colon_2_colon_colon_50_colon_100_colon_4()
    {
        _handler2.Parse("\x1b[1;38;2;50;100;;4m");
        InputHandler.Parse("\x1b[1;38:2::50:100:;4m");
        Assert.NotEqual(0u, _handler2.CurAttrData.IsBold());
        Assert.NotEqual(0u, _handler2.CurAttrData.IsUnderline());
        Assert.Equal((uint)((50 << 16) | (100 << 8) | 0), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should not interfere with leading/following SGR attrs' › 'CSI 1 ; 38:2::50:100 ; 4 m'
    [Fact]
    public void Csi_1_38_colon_2_colon_colon_50_colon_100_4()
    {
        _handler2.Parse("\x1b[1;38;2;50;100;;4m");
        InputHandler.Parse("\x1b[1;38:2::50:100;4m");
        Assert.NotEqual(0u, _handler2.CurAttrData.IsBold());
        Assert.NotEqual(0u, _handler2.CurAttrData.IsUnderline());
        Assert.Equal((uint)((50 << 16) | (100 << 8) | 0), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should not interfere with leading/following SGR attrs' › 'CSI 1 ; 38:2:: ; 4 m'
    [Fact]
    public void Csi_1_38_colon_2_colon_colon_4()
    {
        _handler2.Parse("\x1b[1;38;2;;;;4m");
        InputHandler.Parse("\x1b[1;38:2::;4m");
        Assert.NotEqual(0u, _handler2.CurAttrData.IsBold());
        Assert.NotEqual(0u, _handler2.CurAttrData.IsUnderline());
        Assert.Equal((uint)(0), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

    // xterm.js: 'InputHandler' › 'colon notation' › 'should not interfere with leading/following SGR attrs' › 'CSI 1 ; 38;2:: ; 4 m'
    [Fact]
    public void Csi_1_38_semi_2_colon_colon_4()
    {
        _handler2.Parse("\x1b[1;38;2;;;;4m");
        InputHandler.Parse("\x1b[1;38;2::;4m");
        Assert.NotEqual(0u, _handler2.CurAttrData.IsBold());
        Assert.NotEqual(0u, _handler2.CurAttrData.IsUnderline());
        Assert.Equal((uint)(0), _handler2.CurAttrData.Fg & 0xFFFFFFu);
        Assert.Equal(_handler2.CurAttrData.Fg, InputHandler.CurAttrData.Fg);
    }

}
