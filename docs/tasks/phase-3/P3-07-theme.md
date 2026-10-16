# P3-07 — `TerminalTheme` and the 256-color palette

- **Status:** todo
- **Depends on:** P3-01, P1-26
- **Size:** M

## Goal

Port the theme model and its resolution into a full 256-color palette plus special colors, and the OSC color
overrides (set / restore / query from P1-51's `OnColor` events).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/browser/Types.ts` | 180–225 | `DEFAULT_ANSI_COLORS` (16 colors + 6×6×6 cube + 24 grays) |
| `src/browser/services/ThemeService.ts` | 1–198 | `ThemeService` (defaults, `_setTheme`, `restoreColor`, `modifyColors`, `_updateRestoreColors`, contrast cache invalidation) |
| `src/browser/services/ThemeService.test.ts` | 1–373 | all tests |
| `src/common/services/Services.ts` | 276–306 | `ITheme` |

## Files to create or change

- `src/LoomTerminal.Presentation/Theming/TerminalTheme.cs` — create (public sealed record: `Foreground`, `Background`,
  `Cursor`, `CursorAccent`, `SelectionBackground`, `SelectionForeground`, `SelectionInactiveBackground`, `Black` …
  `BrightWhite` (16), `ExtendedAnsi` (`IReadOnlyList<string>?` for 16–255); every color is a `string?` CSS value)
- `src/LoomTerminal.Presentation/Theming/ThemePalette.cs` — create (internal: port of `ThemeService` state)
- `tests/LoomTerminal.Presentation.Tests/Theming/ThemePaletteTests.cs` — create

## Design decisions (already made — do not change)

- Public colors are `string?` CSS hex (`"#1e1e1e"`, `"#1e1e1e80"`), like xterm.js `ITheme`: VIGO already builds such
  strings from its design tokens. `null` means "default". Parsing uses `Css.ToColor` (P1-26).
- Internally everything is `TerminalColor` (`Css` + `Rgba`, P1-26). Defaults are xterm.js `ThemeService` defaults.
- OSC overrides: `ThemePalette.Apply(IReadOnlyList<ColorRequest>)` handles set/restore and returns the reply strings
  for queries (built with `XParseColor.ToRgbString`, P1-27); the session sends replies through `CoreService.TriggerDataEvent`
  (wire it in `TerminalSession` here).

## Tests

- [ ] Port `ThemeService.test.ts` (skip tests about DOM-only behavior; list them).
- [ ] New: OSC 4 set then query returns `rgb:rrrr/gggg/bbbb`; OSC 104 restores the theme value.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Public API files updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Resolving a cell's colors (P3-08).

## Suggested commit message

`feat(presentation): add TerminalTheme and port the theme palette`
