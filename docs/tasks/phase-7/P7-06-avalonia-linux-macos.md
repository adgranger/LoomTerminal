# P7-06 — Avalonia on Linux and macOS: platform details

- **Status:** in progress
- **Depends on:** P7-04, P7-05
- **Size:** M

## Goal

Make the Avalonia control feel native on Linux and macOS: default fonts, macOS keyboard conventions (Cmd+C/V, Option
as Meta), and the checklist run on both.

## Files to create or change

- `src/LoomTerminal.Presentation/Input/KeyInputHandler.cs` — `KeyContext.IsMac` drives Cmd+C / Cmd+V copy/paste
  (instead of Ctrl+Shift+C/V) and the `MacOptionIsMeta` option; Linux keeps the Windows rules plus Ctrl+Shift+C/V
- `src/LoomTerminal.Avalonia/Controls/TerminalView.Keyboard.cs` — fill `IsMac` from `OperatingSystem.IsMacOS()`
- `src/LoomTerminal.Avalonia/README.md` — platform notes
- `docs/manual-tests.md` — "Linux" and "macOS" columns

## Design decisions (already made — do not change)

- macOS: Cmd+C copies (only with a selection; without one, nothing — Ctrl+C is the interrupt), Cmd+V pastes, Cmd+K
  clears the scrollback (`TerminalSession.ClearScrollback()`, add it), Option is **not** Meta by default
  (`MacOptionIsMeta = false`, xterm.js default) so Option+letters type accented characters.
- Default font list when the host gives none: macOS `Menlo, SF Mono`; Linux `DejaVu Sans Mono, Liberation Mono, monospace`.
- Linux IME depends on Avalonia's IBus/Fcitx support: document what works.

## Tests

- [x] New (Presentation): the macOS key rules above.
- [ ] Manual: the checklist on Ubuntu (X11 and Wayland) and macOS.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Checklist ticked on Linux and macOS
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(avalonia): Linux and macOS platform conventions`
