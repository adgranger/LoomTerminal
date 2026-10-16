// Ported from xtermjs/xterm.js: src/common/parser/Constants.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Parser;

/// <summary>Internal states of <c>EscapeSequenceParser</c>.</summary>
internal enum ParserState
{
    Ground = 0,
    Escape = 1,
    EscapeIntermediate = 2,
    CsiEntry = 3,
    CsiParam = 4,
    CsiIntermediate = 5,
    CsiIgnore = 6,
    SosPmApcString = 7,
    OscString = 8,
    DcsEntry = 9,
    DcsParam = 10,
    DcsIgnore = 11,
    DcsIntermediate = 12,
    DcsPassthrough = 13,
}

/// <summary>Internal actions of <c>EscapeSequenceParser</c>.</summary>
internal enum ParserAction
{
    Ignore = 0,
    Error = 1,
    Print = 2,
    Execute = 3,
    OscStart = 4,
    OscPut = 5,
    OscEnd = 6,
    CsiDispatch = 7,
    Param = 8,
    Collect = 9,
    EscDispatch = 10,
    Clear = 11,
    DcsHook = 12,
    DcsPut = 13,
    DcsUnhook = 14,
}

/// <summary>Internal states of <c>OscParser</c>.</summary>
internal enum OscState
{
    Start = 0,
    Id = 1,
    Payload = 2,
    Abort = 3,
}

/// <summary>Limits shared by the parsers.</summary>
internal static class ParserLimits
{
    /// <summary>Payload limit for OSC and DCS.</summary>
    public const int PayloadLimit = 10000000;
}
