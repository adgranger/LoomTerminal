// Ported from xtermjs/xterm.js: src/common/services/Services.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Services;

/// <summary>A provider of the character widths of one Unicode version.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface IUnicodeVersionProvider
{
    /// <summary>The Unicode version this provider implements.</summary>
    string Version { get; }

    /// <summary>The width in columns of a character: 0, 1 or 2.</summary>
    int Wcwidth(int ucs);

    /// <summary>
    /// The width and kind of a character for grapheme clustering, as a packed value (see
    /// <see cref="UnicodeService.CreatePropertyValue"/>). If <paramref name="preceding"/> is not 0, it is the result
    /// for the previous character, and the result tells whether the characters should be joined.
    /// </summary>
    int CharProperties(int codepoint, int preceding);
}
