# P1-46 — `InputHandler.RequestMode`: DECRQM (ANSI and DEC private)

- **Status:** done
- **Depends on:** P1-45
- **Size:** S

## Goal

Port the mode report request (`CSI Ps $ p` and `CSI ? Ps $ p`), which applications use to detect features.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 2245–2307 | `requestMode` |
| | constructor | CSI `$p`, `?$p` |
| `src/common/InputHandler.test.ts` | 2277–2352 | `DECRQM` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.RequestMode.cs` — create (with `RegisterRequestModeHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerRequestModeTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. The JS `const enum V { NOT_RECOGNIZED, SET, RESET, PERMANENTLY_SET, PERMANENTLY_RESET }` local to the
  method becomes a private nested enum with the same values.

## Tests

- [x] Port `DECRQM` (all `it`s). The JS tests capture `coreService.onData`: subscribe to `OnData` and collect.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(core): port DECRQM`
