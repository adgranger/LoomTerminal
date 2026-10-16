# P1-55 — Windows PTY heuristics (`windowsPty`, `windowsMode`)

- **Status:** todo
- **Depends on:** P1-54
- **Size:** S

## Goal

Port the ConPTY/winpty wrapping heuristics: on old ConPTY builds (< 21376) or with `windowsMode`, lines are marked as
wrapped from the character before a line feed; and reflow is disabled when the PTY does its own (see `Buffer._isReflowEnabled`).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/WindowsMode.ts` | 1–27 | `updateWindowsModeWrappedState` |
| `src/common/CoreTerminal.ts` | 240–242, 253–283 | `_setup`, `_handleWindowsPtyOptionChange`, `_enableWindowsWrappingHeuristics`, the option change subscription in the constructor |
| `src/common/buffer/Buffer.ts` | 296–303 | `_isReflowEnabled` (already ported in P1-30: re-check it against VIGO's use) |

## Files to create or change

- `src/LoomTerminal.Core/WindowsMode.cs` — create
- `src/LoomTerminal.Core/TerminalCore.cs` — add the heuristics (a `MutableDisposable<IDisposable>` field, P1-01)
- `tests/LoomTerminal.Core.Tests/WindowsModeTests.cs` — create

## Design decisions (already made — do not change)

- Keep the JS condition literally, including its duplicated `buildNumber !== undefined` check (line 256): with
  `WindowsPtyOptions.BuildNumber` an `int?`, write `windowsPty.BuildNumber is not null`.
- VIGO passes `{ Backend = "conpty", BuildNumber = <Windows build> }` (≥ 22000 on supported Windows 11): the
  heuristics are then **off** and reflow is **off** (ConPTY rewraps). Document this in the XML doc of `WindowsPtyOptions`.

## Tests

- [ ] New: with `WindowsPty = new("conpty", 19041)`, writing 80 non-space chars then `\n` marks the next line
      `IsWrapped`; with `BuildNumber = 22631` it does not; with `WindowsMode = true` it does; changing the option at
      run time switches the behavior (the subscription disposes the heuristics).
- [ ] New: with `WindowsPty = new("conpty", 22631)`, resizing narrower does not reflow (lines keep their content).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `WindowsMode.ts` ✅, `CoreTerminal.ts` ✅ (except `write`)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(core): port the Windows PTY wrapping heuristics`
