// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    internal static uint UpdateAttrColor(uint color, int mode, int c1, int c2, int c3)
    {
        if (mode == 2)
        {
            color |= Attributes.CmRgb;
            color &= ~Attributes.RgbMask;
            color |= AttributeData.FromColorRgb((c1, c2, c3));
        }
        else if (mode == 5)
        {
            color &= ~(Attributes.CmMask | Attributes.PcolorMask);
            color |= Attributes.CmP256 | (uint)(c1 & 0xff);
        }

        return color;
    }

    /// <summary>
    /// Extracts the extended color (38, 48 or 58) at <paramref name="pos"/> and applies it to <paramref name="attr"/>.
    /// Returns the number of additional parameters consumed.
    /// </summary>
    internal static int ExtractColor(IParams p, int pos, AttributeData attr)
    {
        // normalize params
        // meaning: [target, CM, ign, val, val, val]
        // RGB    : [ 38/48,  2, ign,   r,   g,   b]
        // P256   : [ 38/48,  5, ign,   v, ign, ign]
        int[] accu = [0, 0, -1, 0, 0, 0];

        // alignment placeholder for non color space sequences
        int cSpace = 0;

        // return advance we took in params
        int advance = 0;
        do
        {
            accu[advance + cSpace] = p.ParamValues[pos + advance];
            if (p.HasSubParams(pos + advance))
            {
                int[] subparams = p.GetSubParams(pos + advance)!;
                int i = 0;
                do
                {
                    if (accu[1] == 5)
                    {
                        cSpace = 1;
                    }

                    accu[advance + i + 1 + cSpace] = subparams[i];
                }
                while (++i < subparams.Length && i + advance + 1 + cSpace < accu.Length);

                break;
            }

            // exit early if can decide color mode with semicolons
            if ((accu[1] == 5 && advance + cSpace >= 2) || (accu[1] == 2 && advance + cSpace >= 5))
            {
                break;
            }

            // offset colorSpace slot for semicolon mode
            if (accu[1] != 0)
            {
                cSpace = 1;
            }
        }
        while (++advance + pos < p.Length && advance + cSpace < accu.Length);

        // set default values to 0
        for (int i = 2; i < accu.Length; ++i)
        {
            if (accu[i] == -1)
            {
                accu[i] = 0;
            }
        }

        // apply colors
        switch (accu[0])
        {
            case 38:
                attr.Fg = UpdateAttrColor(attr.Fg, accu[1], accu[3], accu[4], accu[5]);
                break;
            case 48:
                attr.Bg = UpdateAttrColor(attr.Bg, accu[1], accu[3], accu[4], accu[5]);
                break;
            case 58:
                attr.Extended = attr.Extended.Clone();
                attr.Extended.UnderlineColor = UpdateAttrColor(attr.Extended.UnderlineColor, accu[1], accu[3], accu[4], accu[5]);
                break;
            default:
                break;
        }

        return advance;
    }

    /// <summary>Sets the underline style (0 removes the underline; an invalid style means a single underline).</summary>
    internal static void ProcessUnderline(int style, AttributeData attr)
    {
        // treat extended attrs as immutable, thus always clone from old one
        // this is needed since the buffer only holds references to it
        attr.Extended = attr.Extended.Clone();

        // default to 1 == single underline
        if (style == -1 || style > 5)
        {
            style = 1;
        }

        attr.Extended.UnderlineStyle = (UnderlineStyle)style;
        attr.Fg |= FgFlags.Underline;

        // 0 deactivates underline
        if (style == 0)
        {
            attr.Fg &= ~FgFlags.Underline;
        }

        // update HAS_EXTENDED in BG
        attr.UpdateExtended();
    }

    private static void ProcessSgr0(AttributeData attr)
    {
        attr.Fg = AttributeData.DefaultAttrData.Fg;
        attr.Bg = AttributeData.DefaultAttrData.Bg;
        attr.Extended = attr.Extended.Clone();

        // Reset underline style and color. Note that we don't want to reset other
        // fields such as the url id.
        attr.Extended.UnderlineStyle = UnderlineStyle.None;
        attr.Extended.UnderlineColor &= ~(Attributes.CmMask | Attributes.RgbMask);
        attr.UpdateExtended();
    }

    /// <summary>SGR (CSI Pm m): sets the character attributes (bold, colors, underline styles and colors…).</summary>
    public bool CharAttributes(IParams p)
    {
        // Optimize a single SGR0.
        if (p.Length == 1 && p.ParamValues[0] == 0)
        {
            ProcessSgr0(_curAttrData);
            return true;
        }

        int l = p.Length;
        AttributeData attr = _curAttrData;
        for (int i = 0; i < l; i++)
        {
            int ps = p.ParamValues[i];
            if (ps >= 30 && ps <= 37)
            {
                // fg color 8
                attr.Fg &= ~(Attributes.CmMask | Attributes.PcolorMask);
                attr.Fg |= Attributes.CmP16 | (uint)(ps - 30);
            }
            else if (ps >= 40 && ps <= 47)
            {
                // bg color 8
                attr.Bg &= ~(Attributes.CmMask | Attributes.PcolorMask);
                attr.Bg |= Attributes.CmP16 | (uint)(ps - 40);
            }
            else if (ps >= 90 && ps <= 97)
            {
                // fg color 16
                attr.Fg &= ~(Attributes.CmMask | Attributes.PcolorMask);
                attr.Fg |= Attributes.CmP16 | (uint)(ps - 90) | 8;
            }
            else if (ps >= 100 && ps <= 107)
            {
                // bg color 16
                attr.Bg &= ~(Attributes.CmMask | Attributes.PcolorMask);
                attr.Bg |= Attributes.CmP16 | (uint)(ps - 100) | 8;
            }
            else if (ps == 0)
            {
                // default
                ProcessSgr0(attr);
            }
            else if (ps == 1)
            {
                // bold text
                attr.Fg |= FgFlags.Bold;
            }
            else if (ps == 3)
            {
                // italic text
                attr.Bg |= BgFlags.Italic;
            }
            else if (ps == 4)
            {
                // underlined text
                attr.Fg |= FgFlags.Underline;
                ProcessUnderline(p.HasSubParams(i) ? p.GetSubParams(i)![0] : (int)UnderlineStyle.Single, attr);
            }
            else if (ps == 5)
            {
                // blink
                attr.Fg |= FgFlags.Blink;
            }
            else if (ps == 7)
            {
                // inverse and positive
                // test with: echo -e '\e[31m\e[42mhello\e[7mworld\e[27mhi\e[m'
                attr.Fg |= FgFlags.Inverse;
            }
            else if (ps == 8)
            {
                // invisible
                attr.Fg |= FgFlags.Invisible;
            }
            else if (ps == 9)
            {
                // strikethrough
                attr.Fg |= FgFlags.Strikethrough;
            }
            else if (ps == 2)
            {
                // dimmed text
                attr.Bg |= BgFlags.Dim;
            }
            else if (ps == 21)
            {
                // double underline
                ProcessUnderline((int)UnderlineStyle.Double, attr);
            }
            else if (ps == 22)
            {
                // not bold nor faint
                attr.Fg &= ~FgFlags.Bold;
                attr.Bg &= ~BgFlags.Dim;
            }
            else if (ps == 23)
            {
                // not italic
                attr.Bg &= ~BgFlags.Italic;
            }
            else if (ps == 24)
            {
                // not underlined
                attr.Fg &= ~FgFlags.Underline;
                ProcessUnderline((int)UnderlineStyle.None, attr);
            }
            else if (ps == 25)
            {
                // not blink
                attr.Fg &= ~FgFlags.Blink;
            }
            else if (ps == 27)
            {
                // not inverse
                attr.Fg &= ~FgFlags.Inverse;
            }
            else if (ps == 28)
            {
                // not invisible
                attr.Fg &= ~FgFlags.Invisible;
            }
            else if (ps == 29)
            {
                // not strikethrough
                attr.Fg &= ~FgFlags.Strikethrough;
            }
            else if (ps == 39)
            {
                // reset fg
                attr.Fg &= ~(Attributes.CmMask | Attributes.RgbMask);
                attr.Fg |= AttributeData.DefaultAttrData.Fg & (Attributes.PcolorMask | Attributes.RgbMask);
            }
            else if (ps == 49)
            {
                // reset bg
                attr.Bg &= ~(Attributes.CmMask | Attributes.RgbMask);
                attr.Bg |= AttributeData.DefaultAttrData.Bg & (Attributes.PcolorMask | Attributes.RgbMask);
            }
            else if (ps == 38 || ps == 48 || ps == 58)
            {
                // fg color 256 and RGB
                i += ExtractColor(p, i, attr);
            }
            else if (ps == 53)
            {
                // overline
                attr.Bg |= BgFlags.Overline;
            }
            else if (ps == 55)
            {
                // not overline
                attr.Bg &= ~BgFlags.Overline;
            }
            else if (ps == 59)
            {
                attr.Extended = attr.Extended.Clone();
                attr.Extended.UnderlineColor = unchecked((uint)-1);
                attr.UpdateExtended();
            }
            else
            {
                _logService.Debug($"Unknown SGR attribute: {ps}.");
            }
        }

        return true;
    }

    private void RegisterSgrHandlers() =>
        _parser.RegisterCsiHandler(new FunctionIdentifier(Final: "m"), CharAttributes);
}
