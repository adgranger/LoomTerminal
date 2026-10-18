// Ported from xtermjs/xterm.js: src/common/services/CharsetService.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Services;

/// <summary>Tracks the G0–G3 character sets and the active one.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class CharsetService : ICharsetService
{
    private IReadOnlyDictionary<char, string>?[] _charsets = new IReadOnlyDictionary<char, string>?[4];

    /// <inheritdoc />
    public IReadOnlyDictionary<char, string>? Charset { get; set; }

    /// <inheritdoc />
    public int GLevel { get; private set; }

    /// <summary>The character sets of G0–G3 (an entry is <see langword="null"/> for US ASCII).</summary>
    public IReadOnlyDictionary<char, string>?[] Charsets => _charsets;

    /// <inheritdoc />
    public void Reset()
    {
        Charset = null;
        _charsets = new IReadOnlyDictionary<char, string>?[4];
        GLevel = 0;
    }

    /// <inheritdoc />
    public void SetgLevel(int g)
    {
        GLevel = g;
        Charset = _charsets[g];
    }

    /// <inheritdoc />
    public void SetgCharset(int g, IReadOnlyDictionary<char, string>? charset)
    {
        _charsets[g] = charset;
        if (GLevel == g)
        {
            Charset = charset;
        }
    }
}
