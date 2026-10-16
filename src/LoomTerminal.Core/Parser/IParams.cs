// Ported from xtermjs/xterm.js: src/common/parser/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Parser;

/// <summary>The sequence parameters handed to the handlers of the parser.</summary>
/// <remarks>
/// Threading: not thread-safe; used from the parser thread only. The object given to a handler is borrowed:
/// use <see cref="ToArray"/> or <see cref="Clone"/> to keep a copy.
/// </remarks>
internal interface IParams
{
    /// <summary>The maximum number of storable parameters.</summary>
    int MaxLength { get; }

    /// <summary>The maximum number of storable sub-parameters.</summary>
    int MaxSubParamsLength { get; }

    /// <summary>The parameter values (xterm.js: <c>params</c>). Never read beyond <see cref="Length"/> - 1.</summary>
    int[] ParamValues { get; }

    /// <summary>The number of parameters.</summary>
    int Length { get; }

    /// <summary>Clones the object.</summary>
    IParams Clone();

    /// <summary>
    /// Gets an array representation: <c>object[]</c> whose items are <c>int</c> (a parameter) or <c>int[]</c> (the
    /// sub-parameters of the parameter before it). Sequence <c>1;2:3:4;5::6</c> gives <c>[1, 2, [3, 4], 5, [-1, 6]]</c>.
    /// </summary>
    object[] ToArray();

    /// <summary>Resets to the initial empty state.</summary>
    void Reset();

    /// <summary>Adds a parameter value (ignored once <see cref="MaxLength"/> parameters are stored).</summary>
    void AddParam(int value);

    /// <summary>Adds a sub-parameter value to the last parameter.</summary>
    void AddSubParam(int value);

    /// <summary>Whether the parameter at <paramref name="idx"/> has sub-parameters.</summary>
    bool HasSubParams(int idx);

    /// <summary>The sub-parameters of the parameter at <paramref name="idx"/>, or null if it has none.</summary>
    int[]? GetSubParams(int idx);

    /// <summary>All sub-parameters, by index of their parameter.</summary>
    Dictionary<int, int[]> GetSubParamsAll();
}
