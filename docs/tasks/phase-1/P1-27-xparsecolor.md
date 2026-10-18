# P1-27 — `XParseColor`

- **Status:** done
- **Depends on:** P1-01
- **Size:** S

## Goal

Port the X11 color specification parser and formatter used by OSC 4/10/11/12 (`rgb:RR/GG/BB`, `#RGB`…).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/input/XParseColor.ts` | 1–80 | `RGB_REX`, `HASH_REX`, `parseColor`, `pad`, `toRgbString` |
| `src/common/input/XParseColor.test.ts` | 1–93 | all tests |

## Files to create or change

- `src/LoomTerminal.Core/Input/XParseColor.cs` — create
- `tests/LoomTerminal.Core.Tests/Input/XParseColorTests.cs` — create

## Design decisions (already made — do not change)

- Regexes use `[GeneratedRegex]` (AOT, no runtime regex compilation) with the same patterns; JS regexes here are
  case-sensitive and applied to `data.toLowerCase()`: do the same (`ToLowerInvariant`).
- `parseColor` returns `(int R, int G, int B)?`.

## Tests

- [x] Port `XParseColor` › `parseColor`, `toXColorRgb`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (✅ P1-27)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

OSC handlers (P1-51).

## Suggested commit message

`feat(core): port XParseColor`
