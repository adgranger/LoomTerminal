# P4-16 — Gallery: shells in tabs, settings, demo scripts

- **Status:** todo
- **Depends on:** P4-14, P4-15, P4-11, P4-09, P4-04
- **Size:** M

## Goal

Turn the gallery into the manual test bench of the control: one tab per shell, settings pane, scripted demos for each
feature, and the VIGO parity checklist.

## Files to create or change

- `samples/LoomTerminal.Gallery/MainWindow.xaml(.cs)` — `TabView` (one `TerminalControl` + `TerminalSession` per tab,
  "+" menu from `Shells.Detect()`), settings pane (theme light/dark, font family, size, line height, cursor style,
  blink), status bar (cols×rows, frame time)
- `samples/LoomTerminal.Gallery/Demos/*.ps1` — create: `colors.ps1` (16/256/truecolor tables), `styles.ps1` (SGR
  styles, underline variants), `unicode.ps1` (CJK, emoji, combining, box drawing), `stress.ps1` (10 MB of output)
- `docs/manual-tests.md` — create: the checklist (copy the parity list of `docs/ROADMAP.md` plus every manual check of
  the P4 cards), with how to run each demo

## Design decisions (already made — do not change)

- Closing a tab disposes its session (kills the shell); the window closing disposes all.
- Strings in the gallery are English (sample app, not localized).
- Use only `x:Bind`, typed `DataTemplate`s, `ObservableCollection<T>` (AOT rules, phase 4 README).

## Tests

- [ ] Manual: run every item of `docs/manual-tests.md` once, in the **AOT-published** gallery, and tick it in the PR.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `rtk dotnet publish samples/LoomTerminal.Gallery -c Release -r win-x64` — 0 IL/AOT warnings, app starts
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(gallery): shells in tabs, settings and demo scripts`
