// Ported from xtermjs/xterm.js: src/common/services/Services.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Services;

/// <summary>The Unicode width service: a registry of version providers and the active one.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface IUnicodeService
{
    /// <summary>The registered Unicode versions.</summary>
    string[] Versions { get; }

    /// <summary>The currently active version. Setting an unknown version throws.</summary>
    string ActiveVersion { get; set; }

    /// <summary>Raised when the active version changed.</summary>
    IEvent<string> OnChange { get; }

    /// <summary>Registers a Unicode version provider.</summary>
    void Register(IUnicodeVersionProvider provider);

    /// <summary>The width in columns of a code point, for the active version: 0, 1 or 2.</summary>
    int Wcwidth(int codepoint);

    /// <summary>The width in columns of a string, for the active version.</summary>
    int GetStringCellWidth(string s);

    /// <summary>
    /// The width and kind of a character for grapheme clustering. If <paramref name="preceding"/> is not 0, it is the
    /// result for the previous character, and the result tells whether the characters should be joined.
    /// </summary>
    int CharProperties(int codepoint, int preceding);
}
