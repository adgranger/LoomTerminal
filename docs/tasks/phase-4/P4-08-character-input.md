# P4-08 — Character input (`CharacterReceived`), dead keys, surrogate pairs

- **Status:** done
- **Depends on:** P4-07
- **Size:** S

## Goal

Deliver printable characters (including dead-key compositions and characters outside the BMP) to the session.

## Files to create or change

- `src/LoomTerminal.WinUI/Controls/TerminalControl.Keyboard.cs` — `CharacterReceived` handler

## Design decisions (already made — do not change)

- `CharacterReceived` gives UTF-16 code units: a high surrogate is buffered until its low surrogate arrives, then the
  pair is sent with `Session.SendText`.
- Control characters (`< 0x20`, `0x7F`) arriving here are ignored: `KeyDown` already sent them.
- Alt+character (Meta) is handled by `Keyboard.EvaluateKeyboardEvent` in `KeyDown`; when it was `Sent`, the following
  `CharacterReceived` for the same key is ignored (keep a "last key was handled" flag cleared on `KeyUp`).

## Tests

- [ ] Manual: `ê` via dead key `^` + `e`; emoji from the Windows emoji panel (Win+.) arrives as one character; Alt+b
      moves a word back in bash (sends `ESC b`) without also typing `b`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

IME composition (P4-09).

## Suggested commit message

`feat(winui): deliver typed characters to the session`
