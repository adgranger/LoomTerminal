# P5-02 — Replace VIGO's ConPTY code with `LoomTerminal.Pty`

- **Status:** todo
- **Depends on:** P5-01
- **Size:** S

## Goal

Remove the ConPTY session and shell locator that were extracted into LoomTerminal (they are now duplicates), and make
`TerminalService` use `LoomTerminal.Pty` (`Shells.Detect`, `PtyStartOptions`).

## Files to create or change (in `D:\VIGO`)

- `src/Vigo.Git/Terminal/ConPtyNative.cs`, `ConPtySession.cs`, `WindowsShellLocator.cs` — delete
- `src/Vigo.Core/Services/ITerminal.cs` — delete `ITerminalSession`, `ITerminalSessionFactory`; keep or adapt
  `ITerminalShellLocator` (see decisions)
- `src/Vigo.Core/Services/TerminalService.cs`, `src/Vigo.App/App.xaml.cs` (DI registrations, lines 122–124) — adapt
- `tests/Vigo.Git.Tests/ConPtySessionTests.cs` — delete (the tests live in LoomTerminal now)

## Design decisions (already made — do not change)

- `ShellIds` values of LoomTerminal are identical to VIGO's `TerminalShellIds` (`pwsh`, `powershell`, `git-bash`,
  `cmd`, `wsl:`): stored preferences keep working. VIGO maps them to its `TerminalShell` model.
- **Shell names are localized by VIGO**: LoomTerminal's `ShellInfo.Name` is English; VIGO builds the menu label from
  the id (`Loc.Get("Terminal.Shell.Cmd")` for `cmd`, `"WSL : {0}"` for WSL…) — add the keys to both `.resw`.
- `Vigo.Core` must stay free of UI references; referencing `LoomTerminal.Pty` from `Vigo.Core` is allowed only if
  VIGO's dependency rules allow it — otherwise keep an `ITerminalShellLocator` in Core and implement it in `Vigo.Git`
  over `Shells.Detect`. Check `D:\VIGO\docs\DECISIONS.md` and follow it.
- `TERM_PROGRAM=VIGO` is now passed through `PtyStartOptions.Environment`.

## Tests

- [ ] VIGO tests green (the shell detection test moves or is adapted to the new locator).
- [ ] Manual (Debug): the terminal panel still lists the shells and starts each one (still the xterm.js view).

## Acceptance criteria

- [ ] VIGO builds, tests pass, AOT check OK
- [ ] Commit message proposed (VIGO)
- [ ] Card status and `D:\LoomTerminal\docs\tasks\README.md` updated

## Out of scope

The view (P5-03).

## Suggested commit message (VIGO)

`J<n>.<m>: use LoomTerminal.Pty for the integrated terminal`
