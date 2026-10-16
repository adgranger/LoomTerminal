# P1-39 — `InputHandler.Cursor`: CUU, CUD, CUF, CUB, CNL, CPL, CHA, CUP, HPA, HPR, VPA, VPR, HVP

- **Status:** todo
- **Depends on:** P1-38
- **Size:** M

## Goal

Port the cursor movement CSI handlers and their helpers (restricting the cursor, origin mode aware positioning).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 861–901 | `_restrictCursor`, `_setCursor`, `_moveCursor` |
| | 902–1080 | `cursorUp`, `cursorDown`, `cursorForward`, `cursorBackward`, `cursorNextLine`, `cursorPrecedingLine`, `cursorCharAbsolute`, `cursorPosition`, `charPosAbsolute`, `hPositionRelative`, `linePosAbsolute`, `vPositionRelative`, `hVPosition` |
| | constructor | CSI `A B C D E F G H \` a d e f` registrations |
| `src/common/InputHandler.test.ts` | 972–1301 | `cursor positioning` (all, including `should clamp cursor into addressible range` up to `VPR`) |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.Cursor.cs` — create (with `RegisterCursorHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerCursorTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. `params.params[0] || 1` in JS becomes `p.ParamValues[0] == 0 ? 1 : p.ParamValues[0]` — keep each JS
  default exactly (some handlers default to 1, others to 0).

## Tests

- [ ] Port `cursor positioning` lines 972–1301 (the DCH/ECH/ICH clamping tests at 1302–1345 come with P1-41/P1-42).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Tabs (P1-40), save/restore cursor (P1-44).

## Suggested commit message

`feat(core): port the InputHandler cursor movement handlers`
