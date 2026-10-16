// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Parser;

/// <summary>
/// The VT500 compatible transition table, taken from https://vt100.net/emu/dec_ansi_parser. It is built once and
/// shared by every parser.
/// </summary>
/// <remarks>Threading: read-only after creation; safe to share.</remarks>
internal static class Vt500TransitionTable
{
    // Pseudo-character placeholder for printable non-ascii characters (unicode).
    private const int NonAsciiPrintable = 0xA0;

    /// <summary>The shared table.</summary>
    public static TransitionTable Instance { get; } = Create();

    // range macro for byte: [start, end)
    private static int[] Range(int start, int end)
    {
        int[] result = new int[end - start];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = start + i;
        }

        return result;
    }

    private static TransitionTable Create()
    {
        TransitionTable table = new(4095);

        // Default definitions.
        int[] printables = Range(0x20, 0x7f); // 0x20 (SP) included, 0x7F (DEL) excluded
        int[] executables = [.. Range(0x00, 0x18), 0x19, .. Range(0x1c, 0x20)];

        int[] states = Range((int)ParserState.Ground, (int)ParserState.DcsPassthrough + 1);

        // set default transition
        table.SetDefault(ParserAction.Error, ParserState.Ground);
        // printables
        table.AddMany(printables, ParserState.Ground, ParserAction.Print, ParserState.Ground);
        // global anywhere rules
        foreach (int stateValue in states)
        {
            ParserState state = (ParserState)stateValue;
            table.AddMany([0x18, 0x1a, 0x99, 0x9a], state, ParserAction.Execute, ParserState.Ground);
            table.AddMany(Range(0x80, 0x90), state, ParserAction.Execute, ParserState.Ground);
            table.AddMany(Range(0x90, 0x98), state, ParserAction.Execute, ParserState.Ground);
            table.Add(0x9c, state, ParserAction.Ignore, ParserState.Ground); // ST as terminator
            table.Add(0x1b, state, ParserAction.Clear, ParserState.Escape); // ESC
            table.Add(0x9d, state, ParserAction.OscStart, ParserState.OscString); // OSC
            table.AddMany([0x98, 0x9e, 0x9f], state, ParserAction.Ignore, ParserState.SosPmApcString);
            table.Add(0x9b, state, ParserAction.Clear, ParserState.CsiEntry); // CSI
            table.Add(0x90, state, ParserAction.Clear, ParserState.DcsEntry); // DCS
        }

        // rules for executables and 7f
        table.AddMany(executables, ParserState.Ground, ParserAction.Execute, ParserState.Ground);
        table.AddMany(executables, ParserState.Escape, ParserAction.Execute, ParserState.Escape);
        table.Add(0x7f, ParserState.Escape, ParserAction.Ignore, ParserState.Escape);
        table.AddMany(executables, ParserState.OscString, ParserAction.Ignore, ParserState.OscString);
        table.AddMany(executables, ParserState.CsiEntry, ParserAction.Execute, ParserState.CsiEntry);
        table.Add(0x7f, ParserState.CsiEntry, ParserAction.Ignore, ParserState.CsiEntry);
        table.AddMany(executables, ParserState.CsiParam, ParserAction.Execute, ParserState.CsiParam);
        table.Add(0x7f, ParserState.CsiParam, ParserAction.Ignore, ParserState.CsiParam);
        table.AddMany(executables, ParserState.CsiIgnore, ParserAction.Execute, ParserState.CsiIgnore);
        table.AddMany(executables, ParserState.CsiIntermediate, ParserAction.Execute, ParserState.CsiIntermediate);
        table.Add(0x7f, ParserState.CsiIntermediate, ParserAction.Ignore, ParserState.CsiIntermediate);
        table.AddMany(executables, ParserState.EscapeIntermediate, ParserAction.Execute, ParserState.EscapeIntermediate);
        table.Add(0x7f, ParserState.EscapeIntermediate, ParserAction.Ignore, ParserState.EscapeIntermediate);
        // osc
        table.Add(0x5d, ParserState.Escape, ParserAction.OscStart, ParserState.OscString);
        table.AddMany(printables, ParserState.OscString, ParserAction.OscPut, ParserState.OscString);
        table.Add(0x7f, ParserState.OscString, ParserAction.OscPut, ParserState.OscString);
        table.AddMany([0x9c, 0x1b, 0x18, 0x1a, 0x07], ParserState.OscString, ParserAction.OscEnd, ParserState.Ground);
        table.AddMany(Range(0x1c, 0x20), ParserState.OscString, ParserAction.Ignore, ParserState.OscString);
        // sos/pm/apc does nothing
        table.AddMany([0x58, 0x5e, 0x5f], ParserState.Escape, ParserAction.Ignore, ParserState.SosPmApcString);
        table.AddMany(printables, ParserState.SosPmApcString, ParserAction.Ignore, ParserState.SosPmApcString);
        table.AddMany(executables, ParserState.SosPmApcString, ParserAction.Ignore, ParserState.SosPmApcString);
        table.Add(0x9c, ParserState.SosPmApcString, ParserAction.Ignore, ParserState.Ground);
        table.Add(0x7f, ParserState.SosPmApcString, ParserAction.Ignore, ParserState.SosPmApcString);
        // csi entries
        table.Add(0x5b, ParserState.Escape, ParserAction.Clear, ParserState.CsiEntry);
        table.AddMany(Range(0x40, 0x7f), ParserState.CsiEntry, ParserAction.CsiDispatch, ParserState.Ground);
        table.AddMany(Range(0x30, 0x3c), ParserState.CsiEntry, ParserAction.Param, ParserState.CsiParam);
        table.AddMany([0x3c, 0x3d, 0x3e, 0x3f], ParserState.CsiEntry, ParserAction.Collect, ParserState.CsiParam);
        table.AddMany(Range(0x30, 0x3c), ParserState.CsiParam, ParserAction.Param, ParserState.CsiParam);
        table.AddMany(Range(0x40, 0x7f), ParserState.CsiParam, ParserAction.CsiDispatch, ParserState.Ground);
        table.AddMany([0x3c, 0x3d, 0x3e, 0x3f], ParserState.CsiParam, ParserAction.Ignore, ParserState.CsiIgnore);
        table.AddMany(Range(0x20, 0x40), ParserState.CsiIgnore, ParserAction.Ignore, ParserState.CsiIgnore);
        table.Add(0x7f, ParserState.CsiIgnore, ParserAction.Ignore, ParserState.CsiIgnore);
        table.AddMany(Range(0x40, 0x7f), ParserState.CsiIgnore, ParserAction.Ignore, ParserState.Ground);
        table.AddMany(Range(0x20, 0x30), ParserState.CsiEntry, ParserAction.Collect, ParserState.CsiIntermediate);
        table.AddMany(Range(0x20, 0x30), ParserState.CsiIntermediate, ParserAction.Collect, ParserState.CsiIntermediate);
        table.AddMany(Range(0x30, 0x40), ParserState.CsiIntermediate, ParserAction.Ignore, ParserState.CsiIgnore);
        table.AddMany(Range(0x40, 0x7f), ParserState.CsiIntermediate, ParserAction.CsiDispatch, ParserState.Ground);
        table.AddMany(Range(0x20, 0x30), ParserState.CsiParam, ParserAction.Collect, ParserState.CsiIntermediate);
        // esc_intermediate
        table.AddMany(Range(0x20, 0x30), ParserState.Escape, ParserAction.Collect, ParserState.EscapeIntermediate);
        table.AddMany(Range(0x20, 0x30), ParserState.EscapeIntermediate, ParserAction.Collect, ParserState.EscapeIntermediate);
        table.AddMany(Range(0x30, 0x7f), ParserState.EscapeIntermediate, ParserAction.EscDispatch, ParserState.Ground);
        table.AddMany(Range(0x30, 0x50), ParserState.Escape, ParserAction.EscDispatch, ParserState.Ground);
        table.AddMany(Range(0x51, 0x58), ParserState.Escape, ParserAction.EscDispatch, ParserState.Ground);
        table.AddMany([0x59, 0x5a, 0x5c], ParserState.Escape, ParserAction.EscDispatch, ParserState.Ground);
        table.AddMany(Range(0x60, 0x7f), ParserState.Escape, ParserAction.EscDispatch, ParserState.Ground);
        // dcs entry
        table.Add(0x50, ParserState.Escape, ParserAction.Clear, ParserState.DcsEntry);
        table.AddMany(executables, ParserState.DcsEntry, ParserAction.Ignore, ParserState.DcsEntry);
        table.Add(0x7f, ParserState.DcsEntry, ParserAction.Ignore, ParserState.DcsEntry);
        table.AddMany(Range(0x1c, 0x20), ParserState.DcsEntry, ParserAction.Ignore, ParserState.DcsEntry);
        table.AddMany(Range(0x20, 0x30), ParserState.DcsEntry, ParserAction.Collect, ParserState.DcsIntermediate);
        table.AddMany(Range(0x30, 0x3c), ParserState.DcsEntry, ParserAction.Param, ParserState.DcsParam);
        table.AddMany([0x3c, 0x3d, 0x3e, 0x3f], ParserState.DcsEntry, ParserAction.Collect, ParserState.DcsParam);
        table.AddMany(executables, ParserState.DcsIgnore, ParserAction.Ignore, ParserState.DcsIgnore);
        table.AddMany(Range(0x20, 0x80), ParserState.DcsIgnore, ParserAction.Ignore, ParserState.DcsIgnore);
        table.AddMany(Range(0x1c, 0x20), ParserState.DcsIgnore, ParserAction.Ignore, ParserState.DcsIgnore);
        table.AddMany(executables, ParserState.DcsParam, ParserAction.Ignore, ParserState.DcsParam);
        table.Add(0x7f, ParserState.DcsParam, ParserAction.Ignore, ParserState.DcsParam);
        table.AddMany(Range(0x1c, 0x20), ParserState.DcsParam, ParserAction.Ignore, ParserState.DcsParam);
        table.AddMany(Range(0x30, 0x3c), ParserState.DcsParam, ParserAction.Param, ParserState.DcsParam);
        table.AddMany([0x3c, 0x3d, 0x3e, 0x3f], ParserState.DcsParam, ParserAction.Ignore, ParserState.DcsIgnore);
        table.AddMany(Range(0x20, 0x30), ParserState.DcsParam, ParserAction.Collect, ParserState.DcsIntermediate);
        table.AddMany(executables, ParserState.DcsIntermediate, ParserAction.Ignore, ParserState.DcsIntermediate);
        table.Add(0x7f, ParserState.DcsIntermediate, ParserAction.Ignore, ParserState.DcsIntermediate);
        table.AddMany(Range(0x1c, 0x20), ParserState.DcsIntermediate, ParserAction.Ignore, ParserState.DcsIntermediate);
        table.AddMany(Range(0x20, 0x30), ParserState.DcsIntermediate, ParserAction.Collect, ParserState.DcsIntermediate);
        table.AddMany(Range(0x30, 0x40), ParserState.DcsIntermediate, ParserAction.Ignore, ParserState.DcsIgnore);
        table.AddMany(Range(0x40, 0x7f), ParserState.DcsIntermediate, ParserAction.DcsHook, ParserState.DcsPassthrough);
        table.AddMany(Range(0x40, 0x7f), ParserState.DcsParam, ParserAction.DcsHook, ParserState.DcsPassthrough);
        table.AddMany(Range(0x40, 0x7f), ParserState.DcsEntry, ParserAction.DcsHook, ParserState.DcsPassthrough);
        table.AddMany(executables, ParserState.DcsPassthrough, ParserAction.DcsPut, ParserState.DcsPassthrough);
        table.AddMany(printables, ParserState.DcsPassthrough, ParserAction.DcsPut, ParserState.DcsPassthrough);
        table.Add(0x7f, ParserState.DcsPassthrough, ParserAction.Ignore, ParserState.DcsPassthrough);
        table.AddMany([0x1b, 0x9c, 0x18, 0x1a], ParserState.DcsPassthrough, ParserAction.DcsUnhook, ParserState.Ground);
        // special handling of unicode chars
        table.Add(NonAsciiPrintable, ParserState.Ground, ParserAction.Print, ParserState.Ground);
        table.Add(NonAsciiPrintable, ParserState.OscString, ParserAction.OscPut, ParserState.OscString);
        table.Add(NonAsciiPrintable, ParserState.CsiIgnore, ParserAction.Ignore, ParserState.CsiIgnore);
        table.Add(NonAsciiPrintable, ParserState.DcsIgnore, ParserAction.Ignore, ParserState.DcsIgnore);
        table.Add(NonAsciiPrintable, ParserState.DcsPassthrough, ParserAction.DcsPut, ParserState.DcsPassthrough);
        return table;
    }
}
