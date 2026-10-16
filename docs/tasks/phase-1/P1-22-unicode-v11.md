# P1-22 — `UnicodeV11` (from `addon-unicode11`), active by default

- **Status:** todo
- **Depends on:** P1-21
- **Size:** S

## Goal

Port the Unicode 11 width provider (VIGO activates it through `@xterm/addon-unicode11`) and make it the default
version of `UnicodeService` in LoomTerminal.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `addons/addon-unicode11/src/UnicodeV11.ts` | 1–235 | `BMP_COMBINING`, `HIGH_COMBINING`, `BMP_WIDE`, `HIGH_WIDE`, `bisearch`, `UnicodeV11` (`version`, `wcwidth`, `charProperties`) |
| `addons/addon-unicode11/src/Unicode11Addon.ts` | 1–17 | what activation does (register + `activeVersion = '11'`) |

## Files to create or change

- `src/LoomTerminal.Core/Input/UnicodeV11.cs` — create (header: `// Ported from xtermjs/xterm.js: addons/addon-unicode11/src/UnicodeV11.ts`)
- `src/LoomTerminal.Core/Services/UnicodeService.cs` — register `UnicodeV11` in the constructor and activate `"11"`
- `tests/LoomTerminal.Core.Tests/Input/UnicodeV11Tests.cs` — create

## Design decisions (already made — do not change)

- **Default version is `"11"`** (ADR 0002, decision 7). `"6"` stays registered and selectable.
- Tests that rely on V6 behavior use `MockUnicodeService` (P1-24), which is V6-based as in xterm.js: nothing to change there.

## Tests

- [ ] New (the addon has no unit tests at this commit): `wcwidth` of `'a'` = 1, `U+0301` (combining acute) = 0,
      `U+4E00` (CJK) = 2, `U+1F600` (😀) = 2 in V11 and 1 in V6, `U+FF01` (fullwidth !) = 2, `U+200B` = 0.
      Check each expected value against the JS tables before writing it.
- [ ] New: a fresh `UnicodeService` has `ActiveVersion == "11"` and `Versions` contains `"6"` and `"11"`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (✅ P1-22) and "Behavior differences": "Unicode 11 widths are the default"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`addon-unicode-graphemes`.

## Suggested commit message

`feat(core): port the Unicode 11 width provider and make it the default`
