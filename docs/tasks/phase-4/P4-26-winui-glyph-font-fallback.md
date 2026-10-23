# P4-26 — WinUI: per-glyph font fallback, font weight, cached font

- **Status:** done
- **Depends on:** P4-20, P4-25
- **Size:** M

## Goal

The control draws each non-ASCII grapheme with the family chosen by `ResolvedFontFamilies.FamilyFor` (P4-25): the
other families of `FontFamily` ("Cascadia Mono, Segoe UI Emoji") are real fallbacks, and Nerd Font icons
(oh-my-posh, starship) render from any installed Nerd Font even when the primary family is plain Cascadia Mono.
`FontWeight` is honored. The font is built once per change instead of every frame.

## Verified facts

- Win2D 1.4.0 (`Microsoft.Graphics.Canvas.winmd` in the NuGet cache) exposes `CanvasFontSet.GetSystemFontSet`,
  `CanvasFontSet.GetMatchingFonts`, `CanvasFontFace.HasCharacter`, `CanvasTextFormat.GetSystemFontFamilies`. Win2D has
  no API to install a custom DirectWrite fallback on a text format; hence the per-grapheme family choice below.
- `Win2DRenderSurface.DrawTextRun` already draws every non-ASCII run **one grapheme per layout** at its cell position,
  and `TextRunLayoutKey` already contains the family: choosing a family per grapheme needs no new layout code.
- Bug found while writing this card: `TerminalControl.CurrentFont()` builds a new `string[]` each call, and
  `TerminalFont` is a record whose `Families` compares by reference, so `Win2DTextMeasurer`'s cache never hits and
  `ResolveFamily` enumerates the system fonts **every frame**.

## Files to create or change

- `src/LoomTerminal.WinUI/Rendering/GlyphFamilyMap.cs` — create (`internal sealed class`, `IDisposable`)
- `src/LoomTerminal.WinUI/Rendering/Win2DTextMeasurer.cs` — `ResolveFamily` → `ResolveFamilies`
- `src/LoomTerminal.WinUI/Rendering/Win2DRenderSurface.cs` — take a `GlyphFamilyMap` instead of `string family`
- `src/LoomTerminal.WinUI/Controls/TerminalControl.cs`, `TerminalControl.Layout.cs` — cached font and map
- `samples/LoomTerminal.Gallery/Demos/icons.ps1` — create
- `docs/manual-tests.md`, `src/LoomTerminal.WinUI/README.md` (section "Fonts") — update

## Design decisions (already made — do not change)

- `Win2DTextMeasurer.ResolveFamilies(TerminalFont font)` returns
  `FontFamilyResolver.Resolve(font.Families, InstalledFamilies)`, where `InstalledFamilies` is a
  `static readonly Lazy<HashSet<string>>` (`OrdinalIgnoreCase`) of `CanvasTextFormat.GetSystemFontFamilies()`, with the
  comment `// shortcut: fonts installed while the app runs are seen after a restart; watch font changes if users ask.`
  `Compute` uses `ResolveFamilies(font).Primary`. `ResolveFamily` is deleted (update its `<see cref>` users).
- `GlyphFamilyMap(ResolvedFontFamilies families)`: `Primary` property; `string FamilyFor(int codePoint)` caches the
  answer per code point (`Dictionary<int, string>`) and calls `families.FamilyFor(codePoint, HasCharacter)`.
  `HasCharacter(family, cp)`: one `CanvasFontFace?` per family (cached `Dictionary<string, CanvasFontFace?>`), from
  `CanvasFontSet.GetSystemFontSet().GetMatchingFonts(family, new FontWeight { Weight = 400 }, FontStretch.Normal, FontStyle.Normal)`
  → first of `Fonts`, or null when empty; result `face?.HasCharacter((uint)cp) ?? false`. `Dispose` disposes the faces
  and the font sets. Threading: UI thread only. Reason: font faces are device-independent, so the map survives device
  loss; only a font change replaces it.
- `Win2DRenderSurface`: the constructor parameter `string family` becomes `GlyphFamilyMap families`. The ASCII path
  uses `families.Primary`; the grapheme loop uses
  `families.FamilyFor(Rune.TryGetRuneAt(grapheme, 0, out var rune) ? rune.Value : grapheme[0])`; `DrawLayout` and
  `CreateLayout` take the family as a parameter (it goes into `TextRunLayoutKey` as today).
- `TerminalControl`: field `TerminalFont? _font`; `CurrentFont()` returns `_font ??= new(families, FontSize, LineHeight, Weight: FontWeight.Weight)`.
  Field `GlyphFamilyMap? _glyphFamilies`, created lazily in `OnCanvasDraw` from `Win2DTextMeasurer.ResolveFamilies(font)`.
  The existing property callback loop gets `FontWeightProperty` added, and also sets `_font = null`, disposes and nulls
  `_glyphFamilies`. `OnUnloaded` disposes and nulls `_glyphFamilies`. Reason: one font object per change, so every
  cache keyed on it works.
- Wide icons from non-Mono Nerd Fonts may overflow into the next cell: accepted (Windows Terminal does the same);
  `SymbolFallbacks` already prefer Mono variants.
- No font is bundled: with no Nerd Font installed, icons stay missing-glyph boxes, as in Windows Terminal.
- `Demos/icons.ps1`: prints labelled lines with `[char]::ConvertFromUtf32(...)` for U+E0A0 (branch), U+E0B0 (powerline,
  drawn by the control), U+F07B (folder), U+F489 (terminal), U+E70F (windows), U+F0001 (Material, plane 15), and a
  ruler line to check alignment; first line says "Needs a Nerd Font installed (any family); the primary font may be
  Cascadia Mono".

## Tests

- [ ] Manual (AOT gallery), font family **Cascadia Mono**, a Nerd Font installed (e.g. CaskaydiaCove NF): `icons.ps1`
  shows every icon, aligned on the ruler; oh-my-posh or starship prompt icons render.
- [ ] Manual: `unicode.ps1` unchanged (CJK, emoji, box drawing).
- [ ] Manual: `FontWeight` — not exposed in the gallery yet; checked in P4-27.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `rtk proxy pwsh -NoProfile -File eng/Test-AotApp.ps1 samples/LoomTerminal.Gallery` — exit code 0
- [ ] **Manual (human):** the two checks above, ticked in `docs/manual-tests.md`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

- Reading Windows Terminal's settings (P4-27). Bundling a symbols font. Watching font installation.
- Changing the default `FontFamily` / `FontSize` of the control's style.

## Suggested commit message

`feat(winui): fall back per glyph to the listed fonts and to installed Nerd Fonts`
