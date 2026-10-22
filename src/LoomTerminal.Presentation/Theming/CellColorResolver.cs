// Ported from xtermjs/xterm.js: addons/addon-webgl/src/CellColorResolver.ts, addons/addon-webgl/src/TextureAtlas.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Common;
using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Presentation.Theming;

/// <summary>The final colors of a cell, as <c>0xRRGGBBAA</c>.</summary>
/// <param name="Foreground">Text color (translucent when the cell is dim).</param>
/// <param name="Background">Background color, always opaque.</param>
/// <param name="UnderlineColor">Underline color; <see langword="null"/> when the underline has the text color.</param>
internal readonly record struct ResolvedCellColors(uint Foreground, uint Background, uint? UnderlineColor);

/// <summary>
/// Turns the attributes of a cell into its final colors: selection (<c>CellColorResolver.resolve</c>), then inverse,
/// dim, bold as bright, invisible and the minimum contrast ratio (the color functions of <c>TextureAtlas</c>).
/// Search and link decorations are not handled here: they are overlaid in the render model.
/// </summary>
/// <remarks>Threading: not thread-safe (it reuses a work object); used from the parser thread only.</remarks>
internal sealed class CellColorResolver
{
    private const double DimOpacity = 0.5;

    private readonly ThemePalette _palette;
    private readonly bool _drawBoldTextInBrightColors;
    private readonly double _minimumContrastRatio;
    private readonly AttributeData _work = new();

    /// <summary>Creates a resolver. Clear the palette contrast caches when <paramref name="minimumContrastRatio"/> changes.</summary>
    /// <param name="palette">The resolved theme colors.</param>
    /// <param name="drawBoldTextInBrightColors">Whether bold text of the colors 0 to 7 uses the colors 8 to 15.</param>
    /// <param name="minimumContrastRatio">Minimum contrast ratio between text and background, from 1 to 21.</param>
    internal CellColorResolver(ThemePalette palette, bool drawBoldTextInBrightColors, double minimumContrastRatio)
    {
        _palette = palette;
        _drawBoldTextInBrightColors = drawBoldTextInBrightColors;
        _minimumContrastRatio = minimumContrastRatio;
    }

    /// <summary>Resolves the final colors of a cell.</summary>
    /// <param name="cell">The attributes of the cell.</param>
    /// <param name="code">The code point of the cell (0 when empty).</param>
    /// <param name="isSelected">Whether the cell is part of the selection.</param>
    /// <param name="isFocused">Whether the terminal has the focus (selects the selection color).</param>
    /// <returns>The colors.</returns>
    internal ResolvedCellColors Resolve(AttributeData cell, int code, bool isSelected, bool isFocused)
    {
        var (fg, bg, ext) = ResolveAttributes(cell, code, isSelected, isFocused);
        return ResolveColors(fg, bg, ext, code);
    }

    private (uint Fg, uint Bg, uint Ext) ResolveAttributes(AttributeData cell, int code, bool isSelected, bool isFocused)
    {
        var resultBg = cell.Bg;
        var resultFg = cell.Fg;
        var resultExt = (cell.Bg & BgFlags.HasExtended) != 0 ? cell.Extended.Ext : 0;
        uint bg = 0;
        uint fg = 0;
        var hasBg = false;
        var hasFg = false;
        var colors = _palette;

        // Apply the selection color if needed
        if (isSelected)
        {
            var selectionOpaque = (isFocused ? colors.SelectionBackgroundOpaque : colors.SelectionInactiveBackgroundOpaque).Rgba;

            // If the cell has a bg color, retain the color by blending it with the selection color
            if ((resultFg & FgFlags.Inverse) != 0 || (resultBg & Attributes.CmMask) != Attributes.CmDefault)
            {
                // Resolve the standard bg color
                bg = (resultFg & FgFlags.Inverse) != 0
                    ? PaletteRgba(resultFg, colors.Foreground.Rgba)
                    : PaletteRgba(resultBg, 0);

                // Blend with selection bg color
                bg = (Rgba.Blend(bg, (selectionOpaque & 0xFFFFFF00) | 0x80) >> 8) & Attributes.RgbMask;
            }
            else
            {
                bg = (selectionOpaque >> 8) & Attributes.RgbMask;
            }

            hasBg = true;

            // Apply explicit selection foreground if present
            if (colors.SelectionForeground is { } selectionForeground)
            {
                fg = (selectionForeground.Rgba >> 8) & Attributes.RgbMask;
                hasFg = true;
            }

            // Overwrite fg as bg if it's a special decorative glyph (eg. powerline)
            if (RendererUtils.TreatGlyphAsBackgroundColor(code))
            {
                // Inverse default background should be treated as transparent
                if ((resultFg & FgFlags.Inverse) != 0 && (resultBg & Attributes.CmMask) == Attributes.CmDefault)
                {
                    fg = (selectionOpaque >> 8) & Attributes.RgbMask;
                }
                else
                {
                    fg = (resultFg & FgFlags.Inverse) != 0
                        ? PaletteRgba(resultBg, 0)
                        : PaletteRgba(resultFg, colors.Foreground.Rgba);
                    fg = (Rgba.Blend(fg, (selectionOpaque & 0xFFFFFF00) | 0x80) >> 8) & Attributes.RgbMask;
                }

                hasFg = true;
            }
        }

        // Convert any overrides from rgba to the fg/bg packed format. This resolves the inverse flag
        // ahead of time in order to use the correct cache key
        if (hasBg)
        {
            // Non-RGB attributes from model (+ force non-dim when selected) + override + force RGB color mode
            bg = (cell.Bg & ~Attributes.RgbMask & (isSelected ? ~BgFlags.Dim : uint.MaxValue)) | bg | Attributes.CmRgb;
        }

        if (hasFg)
        {
            // Non-RGB attributes from model + force disable inverse + override + force RGB color mode
            fg = (cell.Fg & ~Attributes.RgbMask & ~FgFlags.Inverse) | fg | Attributes.CmRgb;
        }

        // Handle case where inverse was specified by only one of bg override or fg override was set,
        // resolving the other inverse color and setting the inverse flag if needed.
        if ((resultFg & FgFlags.Inverse) != 0)
        {
            if (hasBg && !hasFg)
            {
                // Resolve bg color type (default color has a different meaning in fg vs bg)
                fg = (resultBg & Attributes.CmMask) == Attributes.CmDefault
                    ? (resultFg & ~(Attributes.RgbMask | FgFlags.Inverse | Attributes.CmMask)) | ((colors.Background.Rgba >> 8) & Attributes.RgbMask) | Attributes.CmRgb
                    : (resultFg & ~(Attributes.RgbMask | FgFlags.Inverse | Attributes.CmMask)) | (resultBg & (Attributes.RgbMask | Attributes.CmMask));
                hasFg = true;
            }

            if (!hasBg && hasFg)
            {
                // Resolve bg color type (default color has a different meaning in fg vs bg)
                bg = (resultFg & Attributes.CmMask) == Attributes.CmDefault
                    ? (resultBg & ~(Attributes.RgbMask | Attributes.CmMask)) | ((colors.Foreground.Rgba >> 8) & Attributes.RgbMask) | Attributes.CmRgb
                    : (resultBg & ~(Attributes.RgbMask | Attributes.CmMask)) | (resultFg & (Attributes.RgbMask | Attributes.CmMask));
                hasBg = true;
            }
        }

        // Use the override if it exists. The dotted underline variant offset is not resolved here: it needs the font
        // metrics, which the backends own.
        return (hasFg ? fg : resultFg, hasBg ? bg : resultBg, resultExt);
    }

    // The color of a packed attribute as 0xRRGGBBAA; "fallback" stands for the default color mode.
    private uint PaletteRgba(uint packed, uint fallback) => (packed & Attributes.CmMask) switch
    {
        Attributes.CmP16 or Attributes.CmP256 => _palette.Ansi[packed & Attributes.PcolorMask].Rgba,
        Attributes.CmRgb => ((packed & Attributes.RgbMask) << 8) | 0xFF,
        _ => fallback,
    };

    private ResolvedCellColors ResolveColors(uint fg, uint bg, uint ext, int code)
    {
        _work.Fg = fg;
        _work.Bg = bg;
        _work.Extended.Ext = ext;

        var bold = _work.IsBold() != 0;
        var inverse = _work.IsInverse() != 0;
        var dim = _work.IsDim() != 0;
        var fgColor = _work.GetFgColor();
        var fgColorMode = _work.GetFgColorMode();
        var bgColor = _work.GetBgColor();
        var bgColorMode = _work.GetBgColorMode();
        if (inverse)
        {
            (fgColor, bgColor) = (bgColor, fgColor);
            (fgColorMode, bgColorMode) = (bgColorMode, fgColorMode);
        }

        var background = GetBackgroundColor(bgColorMode, bgColor, inverse);

        // An invisible cell shows only its background.
        var foreground = _work.IsInvisible() != 0
            ? background
            : GetForegroundColor(bg, bgColorMode, bgColor, fg, fgColorMode, fgColor, inverse, dim, bold, RendererUtils.TreatGlyphAsBackgroundColor(code));

        return new ResolvedCellColors(foreground.Rgba, background.Rgba, GetUnderlineRgba());
    }

    private uint? GetUnderlineRgba()
    {
        if (_work.IsUnderlineColorDefault())
        {
            return null;
        }

        if (_work.IsUnderlineColorRgb())
        {
            return ((uint)_work.GetUnderlineColor() << 8) | 0xFF;
        }

        var index = _work.GetUnderlineColor();
        if (_drawBoldTextInBrightColors && _work.IsBold() != 0 && index < 8)
        {
            index += 8;
        }

        return _palette.Ansi[index].Rgba;
    }

    private TerminalColor GetBackgroundColor(uint bgColorMode, int bgColor, bool inverse)
    {
        var result = bgColorMode switch
        {
            Attributes.CmP16 or Attributes.CmP256 => _palette.Ansi[bgColor],
            Attributes.CmRgb => ToColor(bgColor),
            _ => inverse ? _palette.Foreground : _palette.Background,
        };

        // Ignore the alpha channel: there is no transparent window background.
        return ColorOps.Opaque(result);
    }

    private TerminalColor GetForegroundColor(uint bg, uint bgColorMode, int bgColor, uint fg, uint fgColorMode, int fgColor, bool inverse, bool dim, bool bold, bool excludeFromContrastRatioDemands)
    {
        if (GetMinimumContrastColor(bg, bgColorMode, bgColor, fg, fgColorMode, fgColor, inverse, bold, dim, excludeFromContrastRatioDemands) is { } minimumContrastColor)
        {
            return minimumContrastColor;
        }

        TerminalColor result;
        switch (fgColorMode)
        {
            case Attributes.CmP16 or Attributes.CmP256:
                if (_drawBoldTextInBrightColors && bold && fgColor < 8)
                {
                    fgColor += 8;
                }

                result = _palette.Ansi[fgColor];
                break;
            case Attributes.CmRgb:
                result = ToColor(fgColor);
                break;
            default:
                result = inverse ? _palette.Background : _palette.Foreground;
                break;
        }

        // Apply dim to the color, opacity is fine to use for the foreground color
        return dim ? ColorOps.MultiplyOpacity(result, DimOpacity) : result;
    }

    private TerminalColor? GetMinimumContrastColor(uint bg, uint bgColorMode, int bgColor, uint fg, uint fgColorMode, int fgColor, bool inverse, bool bold, bool dim, bool excludeFromContrastRatioDemands)
    {
        if (_minimumContrastRatio == 1 || excludeFromContrastRatioDemands)
        {
            return null;
        }

        // Try get from cache first
        var cache = dim ? _palette.HalfContrastCache : _palette.ContrastCache;
        if (cache.TryGetColor(bg, fg, out var adjustedColor))
        {
            return adjustedColor;
        }

        var bgRgba = ResolveBackgroundRgba(bgColorMode, bgColor, inverse);
        var fgRgba = ResolveForegroundRgba(fgColorMode, fgColor, inverse, bold);

        // Dim cells only require half the contrast, otherwise they wouldn't be distinguishable from non-dim cells
        if (Rgba.EnsureContrastRatio(bgRgba, fgRgba, _minimumContrastRatio / (dim ? 2 : 1)) is not { } result)
        {
            cache.SetColor(bg, fg, null);
            return null;
        }

        var color = Channels.ToColor((int)((result >> 24) & 0xFF), (int)((result >> 16) & 0xFF), (int)((result >> 8) & 0xFF));
        cache.SetColor(bg, fg, color);
        return color;
    }

    private uint ResolveBackgroundRgba(uint bgColorMode, int bgColor, bool inverse) => bgColorMode switch
    {
        Attributes.CmP16 or Attributes.CmP256 => _palette.Ansi[bgColor].Rgba,
        Attributes.CmRgb => (uint)bgColor << 8,
        _ => inverse ? _palette.Foreground.Rgba : _palette.Background.Rgba,
    };

    private uint ResolveForegroundRgba(uint fgColorMode, int fgColor, bool inverse, bool bold)
    {
        switch (fgColorMode)
        {
            case Attributes.CmP16 or Attributes.CmP256:
                if (_drawBoldTextInBrightColors && bold && fgColor < 8)
                {
                    fgColor += 8;
                }

                return _palette.Ansi[fgColor].Rgba;
            case Attributes.CmRgb:
                return (uint)fgColor << 8;
            default:
                return inverse ? _palette.Background.Rgba : _palette.Foreground.Rgba;
        }
    }

    private static TerminalColor ToColor(int rgb)
    {
        var (r, g, b) = AttributeData.ToColorRgb((uint)rgb);
        return Channels.ToColor(r, g, b);
    }
}
