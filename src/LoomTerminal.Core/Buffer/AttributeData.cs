// Ported from xtermjs/xterm.js: src/common/buffer/AttributeData.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Buffer;

/// <summary>
/// The attributes of a cell: foreground and background colors with flags, and the extended attributes (underline
/// style and color, hyperlink id).
/// </summary>
/// <remarks>
/// <para>Threading: not thread-safe; used from the parser thread only.</para>
/// <para>
/// The <c>Is*</c> flag methods return the masked bits, like the JS numbers of xterm.js: non-zero means true.
/// </para>
/// </remarks>
internal class AttributeData
{
    /// <summary>
    /// The shared default attributes (<c>DEFAULT_ATTR_DATA</c> in <c>BufferLine.ts</c>, frozen in xterm.js).
    /// C# cannot freeze an object: never mutate it, clone it first.
    /// </summary>
    public static readonly AttributeData DefaultAttrData = new();

    /// <summary>
    /// The foreground color in the 24 least significant bits, and additional flags in the remaining 8 bits.
    /// </summary>
    public uint Fg;

    /// <summary>
    /// The background color in the 24 least significant bits, and additional flags in the remaining 8 bits.
    /// </summary>
    public uint Bg;

    /// <summary>The extended attributes beyond those available in <see cref="Fg"/> and <see cref="Bg"/>.</summary>
    public ExtendedAttrs Extended = new();

    /// <summary>Splits a color into its red, green and blue bytes.</summary>
    public static (int R, int G, int B) ToColorRgb(uint value) =>
        ((int)((value >> Attributes.RedShift) & 255), (int)((value >> Attributes.GreenShift) & 255), (int)(value & 255));

    /// <summary>Joins red, green and blue bytes into a color.</summary>
    public static uint FromColorRgb((int R, int G, int B) value) =>
        (uint)(((value.R & 255) << Attributes.RedShift) | ((value.G & 255) << Attributes.GreenShift) | (value.B & 255));

    /// <summary>Creates a copy (with a copy of the extended attributes).</summary>
    public AttributeData Clone()
    {
        AttributeData newObj = new()
        {
            Fg = Fg,
            Bg = Bg,
            Extended = Extended.Clone(),
        };
        return newObj;
    }

    // flags
    public uint IsInverse() => Fg & FgFlags.Inverse;

    public uint IsBold() => Fg & FgFlags.Bold;

    public uint IsUnderline()
    {
        if (HasExtendedAttrs() != 0 && Extended.UnderlineStyle != UnderlineStyle.None)
        {
            return 1;
        }

        return Fg & FgFlags.Underline;
    }

    public uint IsBlink() => Fg & FgFlags.Blink;

    public uint IsInvisible() => Fg & FgFlags.Invisible;

    public uint IsItalic() => Bg & BgFlags.Italic;

    public uint IsDim() => Bg & BgFlags.Dim;

    public uint IsStrikethrough() => Fg & FgFlags.Strikethrough;

    public uint IsProtected() => Bg & BgFlags.Protected;

    public uint IsOverline() => Bg & BgFlags.Overline;

    // color modes
    public uint GetFgColorMode() => Fg & Attributes.CmMask;

    public uint GetBgColorMode() => Bg & Attributes.CmMask;

    public bool IsFgRgb() => (Fg & Attributes.CmMask) == Attributes.CmRgb;

    public bool IsBgRgb() => (Bg & Attributes.CmMask) == Attributes.CmRgb;

    public bool IsFgPalette() => (Fg & Attributes.CmMask) == Attributes.CmP16 || (Fg & Attributes.CmMask) == Attributes.CmP256;

    public bool IsBgPalette() => (Bg & Attributes.CmMask) == Attributes.CmP16 || (Bg & Attributes.CmMask) == Attributes.CmP256;

    public bool IsFgDefault() => (Fg & Attributes.CmMask) == 0;

    public bool IsBgDefault() => (Bg & Attributes.CmMask) == 0;

    public bool IsAttributeDefault() => Fg == 0 && Bg == 0;

    // colors

    /// <summary>The foreground color; how to decode it depends on <see cref="GetFgColorMode"/>. -1 for the default color.</summary>
    public int GetFgColor() => (Fg & Attributes.CmMask) switch
    {
        Attributes.CmP16 or Attributes.CmP256 => (int)(Fg & Attributes.PcolorMask),
        Attributes.CmRgb => (int)(Fg & Attributes.RgbMask),
        _ => -1, // CM_DEFAULT defaults to -1
    };

    /// <summary>The background color; how to decode it depends on <see cref="GetBgColorMode"/>. -1 for the default color.</summary>
    public int GetBgColor() => (Bg & Attributes.CmMask) switch
    {
        Attributes.CmP16 or Attributes.CmP256 => (int)(Bg & Attributes.PcolorMask),
        Attributes.CmRgb => (int)(Bg & Attributes.RgbMask),
        _ => -1, // CM_DEFAULT defaults to -1
    };

    // extended attrs
    public uint HasExtendedAttrs() => Bg & BgFlags.HasExtended;

    public void UpdateExtended()
    {
        if (Extended.IsEmpty())
        {
            Bg &= ~BgFlags.HasExtended;
        }
        else
        {
            Bg |= BgFlags.HasExtended;
        }
    }

    public int GetUnderlineColor()
    {
        if ((Bg & BgFlags.HasExtended) != 0 && ~Extended.UnderlineColor != 0)
        {
            return (Extended.UnderlineColor & Attributes.CmMask) switch
            {
                Attributes.CmP16 or Attributes.CmP256 => (int)(Extended.UnderlineColor & Attributes.PcolorMask),
                Attributes.CmRgb => (int)(Extended.UnderlineColor & Attributes.RgbMask),
                _ => GetFgColor(),
            };
        }

        return GetFgColor();
    }

    public uint GetUnderlineColorMode() =>
        (Bg & BgFlags.HasExtended) != 0 && ~Extended.UnderlineColor != 0
            ? Extended.UnderlineColor & Attributes.CmMask
            : GetFgColorMode();

    public bool IsUnderlineColorRgb() =>
        (Bg & BgFlags.HasExtended) != 0 && ~Extended.UnderlineColor != 0
            ? (Extended.UnderlineColor & Attributes.CmMask) == Attributes.CmRgb
            : IsFgRgb();

    public bool IsUnderlineColorPalette() =>
        (Bg & BgFlags.HasExtended) != 0 && ~Extended.UnderlineColor != 0
            ? (Extended.UnderlineColor & Attributes.CmMask) == Attributes.CmP16
                || (Extended.UnderlineColor & Attributes.CmMask) == Attributes.CmP256
            : IsFgPalette();

    public bool IsUnderlineColorDefault() =>
        (Bg & BgFlags.HasExtended) != 0 && ~Extended.UnderlineColor != 0
            ? (Extended.UnderlineColor & Attributes.CmMask) == 0
            : IsFgDefault();

    public UnderlineStyle GetUnderlineStyle() =>
        (Fg & FgFlags.Underline) != 0
            ? ((Bg & BgFlags.HasExtended) != 0 ? Extended.UnderlineStyle : UnderlineStyle.Single)
            : UnderlineStyle.None;

    public int GetUnderlineVariantOffset() => Extended.UnderlineVariantOffset;
}
