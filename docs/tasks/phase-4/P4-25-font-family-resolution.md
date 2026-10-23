# P4-25 — Font family resolution and symbol fallback order

- **Status:** todo
- **Depends on:** P4-18
- **Size:** S

## Goal

A UI-independent, tested rule (in Presentation, so Avalonia reuses it) that turns the requested family list and the
installed families into: the primary family (Windows Terminal's default stack), the requested fallbacks, and the
installed Nerd Font families used for icons (Private Use Area) the primary family lacks — and picks the family for a
given code point.

## Verified facts

- Windows Terminal docs (learn.microsoft.com, "Appearance profile settings", read 2026-10-23): default `font.face`
  is `"Cascadia Mono"`; "The terminal will try to fallback to Consolas if this can't be found".
- Installed Nerd Font family names on the author's machine (GDI names): `CaskaydiaCove NF`, `CaskaydiaCove NFM`,
  `CaskaydiaCove NFP`, `CaskaydiaMono NF`, … with weight suffixes (`CaskaydiaCove NFM Light`). Nerd Fonts v3 also
  install long names (`… Nerd Font`, `… Nerd Font Mono`, `… Nerd Font Propo`) and the symbols-only
  `Symbols Nerd Font` / `Symbols Nerd Font Mono`. `NFM`/`Mono` = icons one cell wide; `NFP`/`Propo` = proportional.

## Files to create or change

- `src/LoomTerminal.Presentation/Rendering/FontFamilyResolver.cs` — create (`internal static class`)
- `src/LoomTerminal.Presentation/Rendering/ResolvedFontFamilies.cs` — create (`internal sealed record`)
- `tests/LoomTerminal.Presentation.Tests/Rendering/FontFamilyResolverTests.cs` — create

(Presentation already has `InternalsVisibleTo LoomTerminal.WinUI`: no public API.)

## Design decisions (already made — do not change)

- `internal sealed record ResolvedFontFamilies(string Primary, IReadOnlyList<string> Fallbacks, IReadOnlyList<string> SymbolFallbacks)`
  with method `string FamilyFor(int codePoint, Func<string, int, bool> hasCharacter)`:
  1. `Primary` when `hasCharacter(Primary, codePoint)`;
  2. else the first of `Fallbacks` that has it;
  3. else, **only if** `FontFamilyResolver.IsPrivateUse(codePoint)`, the first of `SymbolFallbacks` that has it;
  4. else `Primary` (DirectWrite system fallback, then the missing-glyph box — same as Windows Terminal).
  Reason: requested fallbacks are the user's choice for any character; automatic Nerd Font fallback only for icons, so
  normal text never changes font behind the user's back.
- `FontFamilyResolver.Resolve(IReadOnlyList<string> requested, IReadOnlyCollection<string> installed)`
  (comparisons `OrdinalIgnoreCase`, names returned with the spelling of `requested` / `installed`):
  - `Primary` = first requested family that is installed, else `"Cascadia Mono"` if installed, else `"Consolas"`.
    Constants `DefaultFamily = "Cascadia Mono"`, `LastResortFamily = "Consolas"`. Reason: Windows Terminal's default
    and its documented fallback.
  - `Fallbacks` = the other installed requested families, in requested order, without `Primary`.
  - `SymbolFallbacks` = installed families with `SymbolRank(name) >= 0`, not already in `Primary`/`Fallbacks`, sorted
    by rank then `StringComparer.OrdinalIgnoreCase`.
- `internal static int SymbolRank(string family)`: the checks run in this order, the first match wins, all
  case-insensitive; "has the word X" = regex `\bX\b` (`[GeneratedRegex]`, `IgnoreCase`):
  1. equals `Symbols Nerd Font Mono` → 0
  2. contains `Nerd Font Mono`, or has the word `NFM` → 1
  3. equals `Symbols Nerd Font` → 2
  4. contains `Nerd Font Propo`, or has the word `NFP` → 4
  5. contains `Nerd Font`, or has the word `NF` → 3
  6. otherwise → −1

  Reason: one-cell icons first, so they stay on the grid; proportional variants last.
- `internal static bool IsPrivateUse(int codePoint)`: `0xE000–0xF8FF`, `0xF0000–0xFFFFD`, `0x100000–0x10FFFD`.

## Tests

- [ ] New: `Primary_is_the_first_installed_requested_family`
- [ ] New: `Primary_falls_back_to_Cascadia_Mono_then_Consolas`
- [ ] New: `Fallbacks_keep_the_requested_order_without_the_primary`
- [ ] New: `Symbol_fallbacks_prefer_one_cell_Nerd_Fonts` (installed: `CaskaydiaCove NFP`, `CaskaydiaCove NF`,
  `CaskaydiaCove NFM`, `Symbols Nerd Font Mono`, `Segoe UI` → `Symbols Nerd Font Mono`, `CaskaydiaCove NFM`,
  `CaskaydiaCove NF`, `CaskaydiaCove NFP`)
- [ ] New: `A_requested_Nerd_Font_is_not_repeated_in_symbol_fallbacks`
- [ ] New: `Theory: SymbolRank` (`"JetBrainsMono Nerd Font Mono"` 1, `"Hack Nerd Font"` 3, `"Hack Nerd Font Propo"` 4,
  `"Consolas"` −1, `"NFL Sans"` −1)
- [ ] New: `FamilyFor_uses_symbol_fallbacks_only_for_private_use_code_points` (fake `hasCharacter`)
- [ ] New: `FamilyFor_returns_the_primary_when_no_family_has_the_glyph`
- [ ] New: `Theory: IsPrivateUse` (0xE0A0 true, 0xF0001 true, 0x41 false, 0x2500 false)

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --project tests/LoomTerminal.Presentation.Tests -c Release` — all green
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

- Using it in the WinUI renderer (P4-26). Reading Windows Terminal's settings (P4-27).
- Bundling a symbols font with the package.

## Suggested commit message

`feat(presentation): resolve font families and the Nerd Font fallback order`
