// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.InputHandling;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerOscTests : InputHandlerTestBase
{
    private readonly List<IReadOnlyList<ColorRequest>> _stack = [];

    public InputHandlerOscTests() => InputHandler.OnColor.Subscribe(_stack.Add);

    private static ColorReportRequest Report(int index) => new(index);

    private static ColorSetRequest Set(int index, int r, int g, int b) => new(index, (r, g, b));

    private static ColorRestoreRequest Restore(int? index = null) => new(index);

    private void AssertStack(params ColorRequest[][] expected)
    {
        Assert.Equal(expected.Length, _stack.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal<ColorRequest>(expected[i], _stack[i]);
        }

        _stack.Clear();
    }

    // xterm.js: 'InputHandler' › 'OSC' › '4: query color events'
    [Fact]
    public void OSC_4_query_color_events()
    {
        // single color query
        InputHandler.Parse("\x1b]4;0;?\x07");
        AssertStack([Report(0)]);
        InputHandler.Parse("\x1b]4;123;?\x07");
        AssertStack([Report(123)]);

        // multiple queries
        InputHandler.Parse("\x1b]4;0;?;123;?\x07");
        AssertStack([Report(0), Report(123)]);
    }

    // xterm.js: 'InputHandler' › 'OSC' › '4: set color events'
    [Fact]
    public void OSC_4_set_color_events()
    {
        InputHandler.Parse("\x1b]4;0;rgb:01/02/03\x07");
        AssertStack([Set(0, 1, 2, 3)]);
        InputHandler.Parse("\x1b]4;123;#aabbcc\x07");
        AssertStack([Set(123, 170, 187, 204)]);

        // multiple queries
        InputHandler.Parse("\x1b]4;0;rgb:aa/bb/cc;123;#001122\x07");
        AssertStack([Set(0, 170, 187, 204), Set(123, 0, 17, 34)]);
    }

    // xterm.js: 'InputHandler' › 'OSC' › '4: should ignore invalid values'
    [Fact]
    public void OSC_4_ignores_invalid_values()
    {
        InputHandler.Parse("\x1b]4;0;rgb:aa/bb/cc;45;rgb:1/22/333;123;#001122\x07");
        AssertStack([Set(0, 170, 187, 204), Set(123, 0, 17, 34)]);
    }

    // xterm.js: 'InputHandler' › 'OSC' › '8: hyperlink with id'
    [Fact]
    public void OSC_8_hyperlink_with_id()
    {
        InputHandler.Parse("\x1b]8;id=100;http://localhost:3000\x07");
        Assert.NotEqual(0, InputHandler.CurAttrData.Extended.UrlId);
        Assert.Equal(new OscLinkData("100", "http://localhost:3000"), OscLinkService.GetLinkData(InputHandler.CurAttrData.Extended.UrlId));
        InputHandler.Parse("\x1b]8;;\x07");
        Assert.Equal(0, InputHandler.CurAttrData.Extended.UrlId);
    }

    // xterm.js: 'InputHandler' › 'OSC' › '8: hyperlink with semi-colon'
    [Fact]
    public void OSC_8_hyperlink_with_semi_colon()
    {
        InputHandler.Parse("\x1b]8;;http://localhost:3000;abc=def\x07");
        Assert.NotEqual(0, InputHandler.CurAttrData.Extended.UrlId);
        Assert.Equal(new OscLinkData(null, "http://localhost:3000;abc=def"), OscLinkService.GetLinkData(InputHandler.CurAttrData.Extended.UrlId));
        InputHandler.Parse("\x1b]8;;\x07");
        Assert.Equal(0, InputHandler.CurAttrData.Extended.UrlId);
    }

    // xterm.js: 'InputHandler' › 'OSC' › '104: restore events'
    [Fact]
    public void OSC_104_restore_events()
    {
        InputHandler.Parse("\x1b]104;0\x07\x1b]104;43\x07");
        AssertStack([Restore(0)], [Restore(43)]);

        // multiple in one command
        InputHandler.Parse("\x1b]104;0;43\x07");
        AssertStack([Restore(0), Restore(43)]);

        // full ANSI table restore
        InputHandler.Parse("\x1b]104\x07");
        AssertStack([Restore()]);
    }

    // xterm.js: 'InputHandler' › 'OSC' › '10: FG set & query events'
    [Fact]
    public void OSC_10_foreground_set_and_query_events()
    {
        // single foreground query --> color undefined
        InputHandler.Parse("\x1b]10;?\x07");
        AssertStack([Report(SpecialColorIndex.Foreground)]);

        // OSC with multiple values maps to OSC 10 & OSC 11 & OSC 12
        InputHandler.Parse("\x1b]10;?;?;?;?\x07");
        AssertStack([Report(SpecialColorIndex.Foreground)], [Report(SpecialColorIndex.Background)], [Report(SpecialColorIndex.Cursor)]);

        // set foreground color events
        InputHandler.Parse("\x1b]10;rgb:01/02/03\x07");
        AssertStack([Set(SpecialColorIndex.Foreground, 1, 2, 3)]);
        InputHandler.Parse("\x1b]10;#aabbcc\x07");
        AssertStack([Set(SpecialColorIndex.Foreground, 170, 187, 204)]);

        // set FG, BG and cursor color at once
        InputHandler.Parse("\x1b]10;rgb:aa/bb/cc;#001122;rgb:12/34/56\x07");
        AssertStack(
            [Set(SpecialColorIndex.Foreground, 170, 187, 204)],
            [Set(SpecialColorIndex.Background, 0, 17, 34)],
            [Set(SpecialColorIndex.Cursor, 18, 52, 86)]);
    }

    // xterm.js: 'InputHandler' › 'OSC' › '110: restore FG color'
    [Fact]
    public void OSC_110_restores_the_foreground_color()
    {
        InputHandler.Parse("\x1b]110\x07");
        AssertStack([Restore(SpecialColorIndex.Foreground)]);
    }

    // xterm.js: 'InputHandler' › 'OSC' › '11: BG set & query events'
    [Fact]
    public void OSC_11_background_set_and_query_events()
    {
        // single background query --> color undefined
        InputHandler.Parse("\x1b]11;?\x07");
        AssertStack([Report(SpecialColorIndex.Background)]);

        // OSC 11 with multiple values creates only BG and cursor event
        InputHandler.Parse("\x1b]11;?;?;?;?\x07");
        AssertStack([Report(SpecialColorIndex.Background)], [Report(SpecialColorIndex.Cursor)]);

        // set background color events
        InputHandler.Parse("\x1b]11;rgb:01/02/03\x07");
        AssertStack([Set(SpecialColorIndex.Background, 1, 2, 3)]);
        InputHandler.Parse("\x1b]11;#aabbcc\x07");
        AssertStack([Set(SpecialColorIndex.Background, 170, 187, 204)]);

        // set BG and cursor color at once
        InputHandler.Parse("\x1b]11;#001122;rgb:12/34/56\x07");
        AssertStack([Set(SpecialColorIndex.Background, 0, 17, 34)], [Set(SpecialColorIndex.Cursor, 18, 52, 86)]);
    }

    // xterm.js: 'InputHandler' › 'OSC' › '111: restore BG color'
    [Fact]
    public void OSC_111_restores_the_background_color()
    {
        InputHandler.Parse("\x1b]111\x07");
        AssertStack([Restore(SpecialColorIndex.Background)]);
    }

    // xterm.js: 'InputHandler' › 'OSC' › '12: cursor color set & query events'
    [Fact]
    public void OSC_12_cursor_color_set_and_query_events()
    {
        // single cursor query --> color undefined
        InputHandler.Parse("\x1b]12;?\x07");
        AssertStack([Report(SpecialColorIndex.Cursor)]);

        // OSC 12 with multiple values creates only cursor event
        InputHandler.Parse("\x1b]12;?;?;?;?\x07");
        AssertStack([Report(SpecialColorIndex.Cursor)]);

        // set cursor color events
        InputHandler.Parse("\x1b]12;rgb:01/02/03\x07");
        AssertStack([Set(SpecialColorIndex.Cursor, 1, 2, 3)]);
        InputHandler.Parse("\x1b]12;#aabbcc\x07");
        AssertStack([Set(SpecialColorIndex.Cursor, 170, 187, 204)]);
    }

    // xterm.js: 'InputHandler' › 'OSC' › '112: restore cursor color'
    [Fact]
    public void OSC_112_restores_the_cursor_color()
    {
        InputHandler.Parse("\x1b]112\x07");
        AssertStack([Restore(SpecialColorIndex.Cursor)]);
    }
}
