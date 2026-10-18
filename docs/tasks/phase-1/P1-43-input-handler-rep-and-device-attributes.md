# P1-43 — `InputHandler.DeviceAttributes`: REP, DA1, DA2

- **Status:** done
- **Depends on:** P1-37
- **Size:** S

## Goal

Port "repeat preceding character" and the primary / secondary device attribute reports.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 1615–1666 | `repeatPrecedingCharacter` |
| | 1667–1748 | `sendDeviceAttributesPrimary`, `sendDeviceAttributesSecondary`, `_is` |
| | constructor | CSI `b`, `c`, `>c` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.DeviceAttributes.cs` — create (with `RegisterDeviceAttributeHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerDeviceAttributeTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. `_is(term)` checks `termName` (option `TermName`, default `"xterm"`), as in JS. Reports go through
  `ICoreService.TriggerDataEvent`.

## Tests

- [x] New (no JS tests): `a\x1b[3b` prints `aaaa`; `CSI c` with `TermName = "xterm"` sends the exact JS reply
      string (read it from `sendDeviceAttributesPrimary`); `CSI > c` sends the exact JS secondary reply.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

DSR (P1-48).

## Suggested commit message

`feat(core): port REP and the device attribute reports`
