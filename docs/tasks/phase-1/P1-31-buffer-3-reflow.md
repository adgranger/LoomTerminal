# P1-31 — `Buffer` (3/3): reflow

- **Status:** done
- **Depends on:** P1-30
- **Size:** M

## Goal

Port the reflow of wrapped lines when the width changes (larger and smaller), with viewport and cursor adjustment and
marker updates.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/Buffer.ts` | 304–534 | `_reflow`, `_reflowLarger`, `_reflowLargerAdjustViewport`, `_reflowSmaller` |
| `src/common/buffer/Buffer.test.ts` | 237–656 | `resize` › `reflow` (all tests before `reflowLarger cases`) |

## Files to create or change

- `src/LoomTerminal.Core/Buffer/Buffer.Reflow.cs` — create (`partial`)
- `tests/LoomTerminal.Core.Tests/Buffer/BufferTests.Reflow.cs` — create (`partial`)

## Design decisions (already made — do not change)

- Port literally; `_reflowSmaller` is long (≈185 lines): keep it as one method, as in JS, with the same local names.
- `CircularList.OnInsert` / `OnDelete` events fired during reflow must fire in the same order as JS (markers depend
  on it).

## Tests

- [x] Port `reflow` tests of lines 237–656.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`reflowLarger cases` / `reflowSmaller cases` (P1-32).

## Suggested commit message

`feat(core): port Buffer reflow`
