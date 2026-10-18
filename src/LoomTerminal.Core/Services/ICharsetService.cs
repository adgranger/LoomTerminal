// Ported from xtermjs/xterm.js: src/common/services/Services.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Services;

/// <summary>Tracks the G0–G3 character sets and the active one.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface ICharsetService
{
    /// <summary>The active character set; <see langword="null"/> for US ASCII.</summary>
    IReadOnlyDictionary<char, string>? Charset { get; set; }

    /// <summary>The active G level (0–3).</summary>
    int GLevel { get; }

    /// <summary>Resets the service: no charset, G level 0.</summary>
    void Reset();

    /// <summary>Sets the G level of the terminal.</summary>
    void SetgLevel(int g);

    /// <summary>Sets the charset for the given G level of the terminal.</summary>
    void SetgCharset(int g, IReadOnlyDictionary<char, string>? charset);
}
