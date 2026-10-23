using LoomTerminal.Presentation.Rendering;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI.Text;

namespace LoomTerminal.WinUI.Rendering;

/// <summary>Chooses, per code point, the family that draws it, and remembers the answer.</summary>
/// <remarks>
/// Threading: UI thread only. Font faces are device-independent, so the map survives device loss; only a font
/// change replaces it.
/// </remarks>
internal sealed class GlyphFamilyMap : IDisposable
{
    private readonly ResolvedFontFamilies _families;
    private readonly Dictionary<int, string> _byCodePoint = new();
    private readonly Dictionary<string, CanvasFontFace?> _faces = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<CanvasFontSet> _fontSets = new();

    /// <summary>Creates a map for resolved families.</summary>
    /// <param name="families">The primary, fallback and symbol families.</param>
    public GlyphFamilyMap(ResolvedFontFamilies families)
    {
        ArgumentNullException.ThrowIfNull(families);
        _families = families;
    }

    /// <summary>The family of ASCII text and of glyphs no family has.</summary>
    public string Primary => _families.Primary;

    /// <summary>Picks the family that draws a code point.</summary>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <returns>The family name.</returns>
    public string FamilyFor(int codePoint)
    {
        if (!_byCodePoint.TryGetValue(codePoint, out var family))
        {
            family = _families.FamilyFor(codePoint, HasCharacter);
            _byCodePoint[codePoint] = family;
        }

        return family;
    }

    /// <summary>Releases the font faces and font sets.</summary>
    public void Dispose()
    {
        foreach (var face in _faces.Values)
        {
            face?.Dispose();
        }

        foreach (var set in _fontSets)
        {
            set.Dispose();
        }

        _faces.Clear();
        _fontSets.Clear();
    }

    private bool HasCharacter(string family, int codePoint)
    {
        if (!_faces.TryGetValue(family, out var face))
        {
            var set = CanvasFontSet.GetSystemFontSet().GetMatchingFonts(family, new FontWeight { Weight = 400 }, FontStretch.Normal, FontStyle.Normal);
            _fontSets.Add(set);
            face = set.Fonts.Count > 0 ? set.Fonts[0] : null;
            _faces[family] = face;
        }

        return face?.HasCharacter((uint)codePoint) ?? false;
    }
}
