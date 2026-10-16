# P1-38 — `InputHandler.Controls`: BEL, LF/VT/FF, CR, BS, HT, SO, SI

- **Status:** todo
- **Depends on:** P1-37
- **Size:** S

## Goal

Port the C0 control handlers, including reverse wrap-around for backspace.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 699–860 | `bell`, `lineFeed`, `carriageReturn`, `backspace`, `tab`, `shiftOut`, `shiftIn` |
| | constructor | the `setExecuteHandler(C0.BEL … C0.SI, …)` lines (9 lines) |
| `src/common/InputHandler.test.ts` | 134–181 | `BS with reverseWraparound set/unset` (first block) |
| | 1645–1725 | `BS with reverseWraparound set/unset` (second block) |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.Controls.cs` — create (with `RegisterControlHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerControlTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. `tab` uses `Buffer.NextStop` (P1-29). The C1 execute handlers (IND, NEL, HTS) belong to P1-44 / P1-40.
- `lineFeed` honors `convertEol` and the scroll region exactly as JS (calls `IBufferService.Scroll`).

## Tests

- [ ] Port both `BS with reverseWraparound set/unset` blocks. The two describes share a name in JS: name the C#
      tests `Backspace_reverse_wraparound_…` and `Backspace_reverse_wraparound_block2_…`.
- [ ] New: `\n` with `ConvertEol = true` also returns the cursor to column 0; `\x07` fires `OnRequestBell`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

IND, NEL, RI (P1-44), HTS (P1-40).

## Suggested commit message

`feat(core): port the InputHandler C0 control handlers`
