# P1-32 — `Buffer` tests: `reflowLarger` and `reflowSmaller` cases

- **Status:** todo
- **Depends on:** P1-31
- **Size:** M (tests only)

## Goal

Port the viewport / scrollback matrix of reflow tests and fix the port where they fail.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/Buffer.test.ts` | 657–847 | `reflowLarger cases` (viewport not yet filled; viewport filled, scrollback remaining — `ybase === 0`, `ybase !== 0` with `ydisp === ybase` / `ydisp !== ybase`; viewport filled, no scrollback remaining) |
| | 848–1053 | `reflowSmaller cases` (same matrix) |

## Files to create or change

- `tests/LoomTerminal.Core.Tests/Buffer/BufferTests.ReflowCases.cs` — create (`partial`; nested JS `describe`s
  become test name prefixes: `ReflowLarger_viewport_filled_scrollback_remaining_ybase_not_0_ydisp_equals_ybase_…`)

## Design decisions (already made — do not change)

- The `beforeEach` blocks of the nested describes become private setup methods called at the start of each test
  (xUnit has no nested fixtures).

## Tests

- [ ] Port every `it` of lines 657–1053.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `buffer/Buffer.ts`, `BufferReflow.ts`, `Marker.ts`, `BufferRange.ts` ✅
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`test(core): port the reflow case matrix`
