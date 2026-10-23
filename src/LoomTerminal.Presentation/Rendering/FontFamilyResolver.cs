using System.Text.RegularExpressions;

namespace LoomTerminal.Presentation.Rendering;

/// <summary>Turns a requested font family list and the installed families into <see cref="ResolvedFontFamilies"/>.</summary>
/// <remarks>Threading: stateless; safe from any thread.</remarks>
internal static partial class FontFamilyResolver
{
    /// <summary>The default family of Windows Terminal.</summary>
    internal const string DefaultFamily = "Cascadia Mono";

    /// <summary>The family Windows Terminal falls back to when the default is missing.</summary>
    internal const string LastResortFamily = "Consolas";

    /// <summary>Resolves the families to use.</summary>
    /// <param name="requested">The requested families, in order of preference.</param>
    /// <param name="installed">The installed family names.</param>
    /// <returns>The primary family, the requested fallbacks and the Nerd Font symbol fallbacks.</returns>
    internal static ResolvedFontFamilies Resolve(IReadOnlyList<string> requested, IReadOnlyCollection<string> installed)
    {
        var installedByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in installed)
        {
            installedByName.TryAdd(name, name);
        }

        var available = new List<string>();
        foreach (var name in requested)
        {
            if (installedByName.ContainsKey(name) && !available.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                available.Add(name);
            }
        }

        string primary;
        if (available.Count > 0)
        {
            primary = available[0];
            available.RemoveAt(0);
        }
        else
        {
            primary = installedByName.TryGetValue(DefaultFamily, out var cascadia) ? cascadia : LastResortFamily;
        }

        var symbols = installedByName.Values
            .Where(name => SymbolRank(name) >= 0
                && !name.Equals(primary, StringComparison.OrdinalIgnoreCase)
                && !available.Contains(name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(SymbolRank)
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ResolvedFontFamilies(primary, available, symbols);
    }

    /// <summary>Ranks a family as an icon font: lower is preferred, −1 means it is not a Nerd Font.</summary>
    /// <param name="family">The family name.</param>
    /// <returns>The rank; one-cell icon fonts rank first, proportional variants last.</returns>
    internal static int SymbolRank(string family)
    {
        if (family.Equals("Symbols Nerd Font Mono", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (Contains(family, "Nerd Font Mono") || NfmWord().IsMatch(family))
        {
            return 1;
        }

        if (family.Equals("Symbols Nerd Font", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (Contains(family, "Nerd Font Propo") || NfpWord().IsMatch(family))
        {
            return 4;
        }

        if (Contains(family, "Nerd Font") || NfWord().IsMatch(family))
        {
            return 3;
        }

        return -1;
    }

    /// <summary>Whether a code point is in one of the Unicode private use areas, where icon fonts put their glyphs.</summary>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <returns><see langword="true"/> for U+E000–F8FF and the two supplementary private use planes.</returns>
    internal static bool IsPrivateUse(int codePoint) =>
        codePoint is (>= 0xE000 and <= 0xF8FF) or (>= 0xF0000 and <= 0xFFFFD) or (>= 0x100000 and <= 0x10FFFD);

    private static bool Contains(string family, string text) =>
        family.Contains(text, StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"\bNFM\b", RegexOptions.IgnoreCase)]
    private static partial Regex NfmWord();

    [GeneratedRegex(@"\bNFP\b", RegexOptions.IgnoreCase)]
    private static partial Regex NfpWord();

    [GeneratedRegex(@"\bNF\b", RegexOptions.IgnoreCase)]
    private static partial Regex NfWord();
}
