# P1-47 — `InputHandler.Sgr` (1/2): color and underline helpers

- **Status:** done
- **Depends on:** P1-36
- **Size:** M

## Goal

Port the private helpers of SGR: attribute color updates, extended color extraction (`38;5;n`, `38;2;r;g;b`, colon
forms), underline style processing.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 2308–2323 | `_updateAttrColor` |
| | 2324–2392 | `_extractColor` |
| | 2393–2509 | `_processUnderline` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.Sgr.cs` — create (helpers; `charAttributes` comes in P1-48)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerSgrHelperTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. Make the three helpers `internal` (not `private`) so this card can test them directly.

## Tests

- [x] New: `_extractColor` on `Params.FromArray([38, 2, 50, 100, 150])` at position 0 returns the advance count JS
      returns and sets an RGB foreground `0x326496`; on `[38, new int[] {5, 50}]` (colon form) sets palette 50;
      `_processUnderline(3, attr)` sets `UnderlineStyle.Curly`. Read the expected advance counts from the JS code.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`charAttributes` and the SGR test suites (P1-48).

## Suggested commit message

`feat(core): port the SGR color and underline helpers`
