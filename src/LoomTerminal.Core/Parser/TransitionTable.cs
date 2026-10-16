// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Parser;

/// <summary>
/// How <see cref="TransitionTable"/> values are packed. Index: <c>currentState &lt;&lt; IndexStateShift | charCode</c>;
/// value: <c>action &lt;&lt; TransitionActionShift | nextState</c>.
/// </summary>
internal static class TableAccess
{
    /// <summary>Bit position of the action in a table value.</summary>
    public const int TransitionActionShift = 4;

    /// <summary>Mask of the next state in a table value.</summary>
    public const int TransitionStateMask = 15;

    /// <summary>Bit position of the state in a table index.</summary>
    public const int IndexStateShift = 8;
}

/// <summary>Transition table for the escape sequence parser.</summary>
/// <remarks>Threading: filled once at creation, then only read; safe to share.</remarks>
internal sealed class TransitionTable
{
    /// <summary>Creates a table of <paramref name="length"/> entries.</summary>
    public TransitionTable(int length) => Table = new byte[length];

    /// <summary>The packed transitions (see <see cref="TableAccess"/>).</summary>
    public byte[] Table { get; }

    /// <summary>Sets the default transition.</summary>
    /// <param name="action">The default action.</param>
    /// <param name="next">The default next state.</param>
    public void SetDefault(ParserAction action, ParserState next) =>
        Array.Fill(Table, (byte)(((int)action << TableAccess.TransitionActionShift) | (int)next));

    /// <summary>Adds a transition.</summary>
    /// <param name="code">The input character code.</param>
    /// <param name="state">The current parser state.</param>
    /// <param name="action">The parser action to be done.</param>
    /// <param name="next">The next parser state.</param>
    public void Add(int code, ParserState state, ParserAction action, ParserState next) =>
        Table[((int)state << TableAccess.IndexStateShift) | code] = (byte)(((int)action << TableAccess.TransitionActionShift) | (int)next);

    /// <summary>Adds transitions for multiple input character codes.</summary>
    /// <param name="codes">The input character codes.</param>
    /// <param name="state">The current parser state.</param>
    /// <param name="action">The parser action to be done.</param>
    /// <param name="next">The next parser state.</param>
    public void AddMany(ReadOnlySpan<int> codes, ParserState state, ParserAction action, ParserState next)
    {
        foreach (int code in codes)
        {
            Add(code, state, action, next);
        }
    }
}
