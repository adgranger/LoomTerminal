# P1-41 — `InputHandler.Erase`: ED, EL, ECH, DECSCA, DECSED, DECSEL

- **Status:** todo
- **Depends on:** P1-39
- **Size:** M

## Goal

Port erase in display / line / characters, with protected cells (DECSCA) and the selective variants.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 1130–1146 | `selectProtected` (DECSCA) |
| | 1147–1197 | `_eraseInBufferLine`, `_resetBufferLine` |
| | 1198–1310 | `eraseInDisplay`, `eraseInLine` |
| | 1575–1614 | `eraseChars` |
| | 3338–3353 | `_eraseAttrData` |
| | constructor | CSI `J`, `?J`, `K`, `?K`, `X`, `"q` |
| `src/common/InputHandler.test.ts` | 1316–1329 | clamping: `ECH`, `ECH - should delete last cell` |
| | 1609–1616, 1635–1644 | wide chars: `EL`, `ECH` |
| | 2150–2234 | `EL/ED cursor at buffer.cols` |
| | 2235–2259 | `DECSCA and DECSED/DECSEL` (not the DECRQSS test at 2260: P1-52) |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.Erase.cs` — create (with `RegisterEraseHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerEraseTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. `eraseInDisplay(params, respectProtect = false)`: keep the `bool` parameter; the `?J` registration
  passes `true`. `scrollOnEraseInDisplay` behavior for ED2 is ported here (its regression test comes in P1-53).

## Tests

- [ ] Port the tests listed in the sources table.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The `eraseInDisplay` / `eraseInLine` regression tests (P1-53).

## Suggested commit message

`feat(core): port the InputHandler erase handlers`
