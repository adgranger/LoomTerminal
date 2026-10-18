// Ported from xtermjs/xterm.js: src/common/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.InputHandling;

/// <summary>The indexes of the special colors, after the 256 palette entries.</summary>
internal static class SpecialColorIndex
{
    /// <summary>The foreground color (OSC 10).</summary>
    public const int Foreground = 256;

    /// <summary>The background color (OSC 11).</summary>
    public const int Background = 257;

    /// <summary>The cursor color (OSC 12).</summary>
    public const int Cursor = 258;
}

/// <summary>A request to change or query the colors of the terminal, raised by the OSC color handlers.</summary>
internal abstract record ColorRequest;

/// <summary>A request to report a color to the application.</summary>
/// <param name="Index">A palette index (0 to 255) or a <see cref="SpecialColorIndex"/>.</param>
internal sealed record ColorReportRequest(int Index) : ColorRequest;

/// <summary>A request to set a color.</summary>
/// <param name="Index">A palette index (0 to 255) or a <see cref="SpecialColorIndex"/>.</param>
/// <param name="Color">The new color.</param>
internal sealed record ColorSetRequest(int Index, (int R, int G, int B) Color) : ColorRequest;

/// <summary>A request to restore a color to its default.</summary>
/// <param name="Index">A palette index (0 to 255), a <see cref="SpecialColorIndex"/>, or null for the whole palette.</param>
internal sealed record ColorRestoreRequest(int? Index) : ColorRequest;
