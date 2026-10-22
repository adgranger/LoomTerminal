// Ported from xtermjs/xterm.js: src/browser/ColorContrastCache.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Presentation.Theming;

/// <summary>
/// Caches the foreground adjusted to the minimum contrast ratio, per (background, foreground) attribute pair. A cached
/// <see langword="null"/> means "no adjustment needed", which differs from "not cached yet".
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class ColorContrastCache
{
    private readonly Dictionary<(uint Bg, uint Fg), TerminalColor?> _color = [];
    private readonly Dictionary<(uint Bg, uint Fg), string?> _css = [];

    /// <summary>Caches a css value (xterm.js <c>setCss</c>).</summary>
    /// <param name="bg">Packed background attribute.</param>
    /// <param name="fg">Packed foreground attribute.</param>
    /// <param name="value">The value, or <see langword="null"/> for "no adjustment".</param>
    internal void SetCss(uint bg, uint fg, string? value) => _css[(bg, fg)] = value;

    /// <summary>Gets a cached css value (xterm.js <c>getCss</c>).</summary>
    /// <param name="bg">Packed background attribute.</param>
    /// <param name="fg">Packed foreground attribute.</param>
    /// <param name="value">The cached value, possibly <see langword="null"/>.</param>
    /// <returns><see langword="false"/> when nothing is cached for the pair.</returns>
    internal bool TryGetCss(uint bg, uint fg, out string? value) => _css.TryGetValue((bg, fg), out value);

    /// <summary>Caches a color (xterm.js <c>setColor</c>).</summary>
    /// <param name="bg">Packed background attribute.</param>
    /// <param name="fg">Packed foreground attribute.</param>
    /// <param name="value">The color, or <see langword="null"/> for "no adjustment".</param>
    internal void SetColor(uint bg, uint fg, TerminalColor? value) => _color[(bg, fg)] = value;

    /// <summary>Gets a cached color (xterm.js <c>getColor</c>).</summary>
    /// <param name="bg">Packed background attribute.</param>
    /// <param name="fg">Packed foreground attribute.</param>
    /// <param name="value">The cached color, possibly <see langword="null"/>.</param>
    /// <returns><see langword="false"/> when nothing is cached for the pair.</returns>
    internal bool TryGetColor(uint bg, uint fg, out TerminalColor? value) => _color.TryGetValue((bg, fg), out value);

    /// <summary>Forgets everything.</summary>
    internal void Clear()
    {
        _color.Clear();
        _css.Clear();
    }
}
