namespace LoomTerminal.Presentation.Rendering;

/// <summary>The font families a renderer uses: the primary one, the requested fallbacks and the icon fonts.</summary>
/// <param name="Primary">The family used for text, and for any glyph no other family has.</param>
/// <param name="Fallbacks">The installed requested families after <paramref name="Primary"/>, in requested order.</param>
/// <param name="SymbolFallbacks">The installed Nerd Font families, one-cell variants first, used for icons only.</param>
/// <remarks>Threading: immutable; safe from any thread.</remarks>
internal sealed record ResolvedFontFamilies(
    string Primary,
    IReadOnlyList<string> Fallbacks,
    IReadOnlyList<string> SymbolFallbacks)
{
    /// <summary>Picks the family that draws a code point.</summary>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <param name="hasCharacter">Whether a family (first argument) has a glyph for a code point (second).</param>
    /// <returns>
    /// The primary family if it has the glyph, else the first requested fallback that has it, else (private use only)
    /// the first symbol fallback that has it, else the primary family (system fallback, then the missing-glyph box).
    /// </returns>
    internal string FamilyFor(int codePoint, Func<string, int, bool> hasCharacter)
    {
        if (hasCharacter(Primary, codePoint))
        {
            return Primary;
        }

        foreach (var family in Fallbacks)
        {
            if (hasCharacter(family, codePoint))
            {
                return family;
            }
        }

        if (FontFamilyResolver.IsPrivateUse(codePoint))
        {
            foreach (var family in SymbolFallbacks)
            {
                if (hasCharacter(family, codePoint))
                {
                    return family;
                }
            }
        }

        return Primary;
    }
}
