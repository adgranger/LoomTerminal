// Ported from xtermjs/xterm.js: src/common/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Buffer;

/// <summary>The legacy cell tuple of xterm.js: <c>[attr, char, width, code]</c>.</summary>
/// <param name="Attr">The attributes.</param>
/// <param name="Char">The character(s) of the cell.</param>
/// <param name="Width">The width of the cell.</param>
/// <param name="Code">The code point of the cell.</param>
internal readonly record struct CharData(uint Attr, string Char, int Width, uint Code);
