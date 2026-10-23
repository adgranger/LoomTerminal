using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Presentation.Tests.Rendering;

public class FontFamilyResolverTests
{
    [Fact]
    public void Primary_is_the_first_installed_requested_family()
    {
        var result = FontFamilyResolver.Resolve(["Missing", "fira code", "Consolas"], ["Consolas", "Fira Code"]);

        Assert.Equal("fira code", result.Primary);
    }

    [Fact]
    public void Primary_falls_back_to_Cascadia_Mono_then_Consolas()
    {
        Assert.Equal("Cascadia Mono", FontFamilyResolver.Resolve(["Missing"], ["Consolas", "Cascadia Mono"]).Primary);
        Assert.Equal("Consolas", FontFamilyResolver.Resolve(["Missing"], ["Segoe UI"]).Primary);
    }

    [Fact]
    public void Fallbacks_keep_the_requested_order_without_the_primary()
    {
        var result = FontFamilyResolver.Resolve(["B", "Missing", "A", "C"], ["A", "B", "C"]);

        Assert.Equal("B", result.Primary);
        Assert.Equal(["A", "C"], result.Fallbacks);
    }

    [Fact]
    public void Symbol_fallbacks_prefer_one_cell_Nerd_Fonts()
    {
        var result = FontFamilyResolver.Resolve(
            [],
            ["CaskaydiaCove NFP", "CaskaydiaCove NF", "CaskaydiaCove NFM", "Symbols Nerd Font Mono", "Segoe UI"]);

        Assert.Equal(
            ["Symbols Nerd Font Mono", "CaskaydiaCove NFM", "CaskaydiaCove NF", "CaskaydiaCove NFP"],
            result.SymbolFallbacks);
    }

    [Fact]
    public void A_requested_Nerd_Font_is_not_repeated_in_symbol_fallbacks()
    {
        var result = FontFamilyResolver.Resolve(["Hack Nerd Font"], ["Hack Nerd Font", "Symbols Nerd Font"]);

        Assert.Equal("Hack Nerd Font", result.Primary);
        Assert.Equal(["Symbols Nerd Font"], result.SymbolFallbacks);
    }

    [Theory]
    [InlineData("JetBrainsMono Nerd Font Mono", 1)]
    [InlineData("Hack Nerd Font", 3)]
    [InlineData("Hack Nerd Font Propo", 4)]
    [InlineData("Consolas", -1)]
    [InlineData("NFL Sans", -1)]
    public void SymbolRank_orders_Nerd_Font_variants(string family, int rank)
    {
        Assert.Equal(rank, FontFamilyResolver.SymbolRank(family));
    }

    [Fact]
    public void FamilyFor_uses_symbol_fallbacks_only_for_private_use_code_points()
    {
        var resolved = new ResolvedFontFamilies("Primary", ["Requested"], ["Symbols"]);
        var has = new Dictionary<string, HashSet<int>>
        {
            ["Requested"] = [0xE0A0, 0x4E00],
            ["Symbols"] = [0xE0A0, 0x4E00, 0xE0B0],
        };
        bool HasCharacter(string family, int cp) => has.TryGetValue(family, out var set) && set.Contains(cp);

        Assert.Equal("Requested", resolved.FamilyFor(0xE0A0, HasCharacter));
        Assert.Equal("Symbols", resolved.FamilyFor(0xE0B0, HasCharacter));
        Assert.Equal("Requested", resolved.FamilyFor(0x4E00, HasCharacter));
        Assert.Equal("Primary", resolved.FamilyFor(0x4E01, HasCharacter));
        Assert.Equal("Primary", resolved.FamilyFor(0x2500, (family, _) => family == "Symbols"));
    }

    [Fact]
    public void FamilyFor_returns_the_primary_when_no_family_has_the_glyph()
    {
        var resolved = new ResolvedFontFamilies("Primary", ["Requested"], ["Symbols"]);

        Assert.Equal("Primary", resolved.FamilyFor(0xE0A0, (_, _) => false));
    }

    [Theory]
    [InlineData(0xE0A0, true)]
    [InlineData(0xF0001, true)]
    [InlineData(0x41, false)]
    [InlineData(0x2500, false)]
    public void IsPrivateUse_matches_the_private_use_areas(int codePoint, bool expected)
    {
        Assert.Equal(expected, FontFamilyResolver.IsPrivateUse(codePoint));
    }
}
