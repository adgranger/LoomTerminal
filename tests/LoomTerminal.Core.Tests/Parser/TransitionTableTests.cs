// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.Parser;

// New tests: xterm.js only tests the table through the parser. Expected values are read from the JS table code.
public class TransitionTableTests
{
    private static (ParserAction Action, ParserState Next) Lookup(ParserState state, int code)
    {
        byte value = Vt500TransitionTable.Instance.Table[((int)state << TableAccess.IndexStateShift) | code];
        return ((ParserAction)(value >> TableAccess.TransitionActionShift), (ParserState)(value & TableAccess.TransitionStateMask));
    }

    [Theory]
    [InlineData((int)ParserState.Ground, 0x1b, (int)ParserAction.Clear, (int)ParserState.Escape)]
    [InlineData((int)ParserState.Ground, 0x41, (int)ParserAction.Print, (int)ParserState.Ground)]
    [InlineData((int)ParserState.Ground, 0xA0, (int)ParserAction.Print, (int)ParserState.Ground)]
    [InlineData((int)ParserState.Escape, 0x5b, (int)ParserAction.Clear, (int)ParserState.CsiEntry)]
    [InlineData((int)ParserState.CsiEntry, 0x3f, (int)ParserAction.Collect, (int)ParserState.CsiParam)]
    [InlineData((int)ParserState.CsiParam, 0x6d, (int)ParserAction.CsiDispatch, (int)ParserState.Ground)]
    [InlineData((int)ParserState.OscString, 0x07, (int)ParserAction.OscEnd, (int)ParserState.Ground)]
    [InlineData((int)ParserState.DcsPassthrough, 0x18, (int)ParserAction.DcsUnhook, (int)ParserState.Ground)]
    [InlineData((int)ParserState.Escape, 0x7f, (int)ParserAction.Ignore, (int)ParserState.Escape)]
    [InlineData((int)ParserState.Ground, 0x7f, (int)ParserAction.Error, (int)ParserState.Ground)]
    public void Transition_matches_the_xterm_js_table(int state, int code, int action, int next)
    {
        Assert.Equal(((ParserAction)action, (ParserState)next), Lookup((ParserState)state, code));
    }

    [Fact]
    public void Cancel_byte_0x18_executes_and_returns_to_ground_in_every_state_without_a_dedicated_rule()
    {
        // OSC_STRING (OSC_END) and DCS_PASSTHROUGH (DCS_UNHOOK) override the global rule.
        for (ParserState state = ParserState.Ground; state <= ParserState.DcsPassthrough; state++)
        {
            if (state is ParserState.OscString or ParserState.DcsPassthrough)
            {
                continue;
            }

            Assert.Equal((ParserAction.Execute, ParserState.Ground), Lookup(state, 0x18));
        }
    }

    [Fact]
    public void The_table_has_the_length_given_to_the_constructor()
    {
        Assert.Equal(4095, Vt500TransitionTable.Instance.Table.Length);
    }
}
