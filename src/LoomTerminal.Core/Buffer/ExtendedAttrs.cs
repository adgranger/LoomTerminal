// Ported from xtermjs/xterm.js: src/common/buffer/AttributeData.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Buffer;

/// <summary>Extended attributes of a cell: the underline style and color, the underline variant offset and the hyperlink id.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class ExtendedAttrs
{
    private uint _ext;
    private int _urlId;

    /// <summary>Creates extended attributes.</summary>
    /// <param name="ext">The packed bits.</param>
    /// <param name="urlId">The hyperlink id.</param>
    public ExtendedAttrs(uint ext = 0, int urlId = 0)
    {
        _ext = ext;
        _urlId = urlId;
    }

    /// <summary>The packed bits; with a hyperlink, the underline style bits tell the hyperlink style.</summary>
    public uint Ext
    {
        get
        {
            if (_urlId != 0)
            {
                return (_ext & ~ExtFlags.UnderlineStyle) | ((uint)UnderlineStyle << 26);
            }

            return _ext;
        }

        set => _ext = value;
    }

    /// <summary>The underline style; a hyperlink always reads as <see cref="Buffer.UnderlineStyle.Dashed"/>.</summary>
    public UnderlineStyle UnderlineStyle
    {
        get
        {
            // Always return the URL style if it has one
            if (_urlId != 0)
            {
                return UnderlineStyle.Dashed;
            }

            return (UnderlineStyle)((_ext & ExtFlags.UnderlineStyle) >> 26);
        }

        set
        {
            _ext &= ~ExtFlags.UnderlineStyle;
            _ext |= ((uint)value << 26) & ExtFlags.UnderlineStyle;
        }
    }

    /// <summary>The underline color: color mode and RGB / palette bits.</summary>
    public uint UnderlineColor
    {
        get => _ext & (Attributes.CmMask | Attributes.RgbMask);
        set
        {
            _ext &= ~(Attributes.CmMask | Attributes.RgbMask);
            _ext |= value & (Attributes.CmMask | Attributes.RgbMask);
        }
    }

    /// <summary>The hyperlink id.</summary>
    public int UrlId
    {
        get => _urlId;
        set => _urlId = value;
    }

    /// <summary>The underline variant offset (0 to 7).</summary>
    public int UnderlineVariantOffset
    {
        get => (int)((_ext & ExtFlags.VariantOffset) >> 29);
        set
        {
            _ext &= ~ExtFlags.VariantOffset;
            _ext |= ((uint)value << 29) & ExtFlags.VariantOffset;
        }
    }

    /// <summary>Creates a copy.</summary>
    public ExtendedAttrs Clone() => new(_ext, _urlId);

    /// <summary>Whether the object holds no additional information that needs to be persistent in the buffer.</summary>
    public bool IsEmpty() => UnderlineStyle == UnderlineStyle.None && _urlId == 0;
}
