// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Parser;

internal partial class EscapeSequenceParser
{
    /// <inheritdoc />
    public void Parse(uint[] data, int length) => throw new NotImplementedException("The parse loop is ported by card P1-10.");
}
