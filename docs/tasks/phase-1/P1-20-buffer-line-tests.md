# P1-20 — `BufferLine` tests: combining chars, full width, extended attributes

- **Status:** todo
- **Depends on:** P1-19
- **Size:** S (tests only)

## Goal

Port the remaining `BufferLine` tests and fix the port where they fail.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/BufferLine.test.ts` | 474–810 | `addCharToCell`, `correct fullwidth handling`, `extended attributes` |

## Files to create or change

- `tests/LoomTerminal.Core.Tests/Buffer/BufferLineTests.Content.cs` — create (`partial`)

## Design decisions (already made — do not change)

- Emoji and surrogate pairs in test strings: use C# `\U0001F600` escapes (or the literal), keep the same code points.

## Tests

- [ ] Port every `it` from line 474 to 810.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `buffer/BufferLine.ts`, `AttributeData.ts`, `CellData.ts`, `Constants.ts` ✅
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`test(core): port the remaining BufferLine tests`
