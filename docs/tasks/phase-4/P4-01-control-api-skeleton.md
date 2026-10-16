# P4-01 — `TerminalControl` public API skeleton

- **Status:** todo
- **Depends on:** P3-23
- **Size:** S

## Goal

Give `TerminalControl` its public surface (session, theme, font, padding, events) without behavior yet, and connect the
gallery to a real session so later cards can be checked by hand.

## Files to create or change

- `src/LoomTerminal.WinUI/Controls/TerminalControl.cs` — properties and events below; drawing still the placeholder
- `src/LoomTerminal.WinUI/LoomTerminal.WinUI.csproj` — remove the direct `LoomTerminal.Pty` reference if nothing uses
  it (Presentation brings it transitively)
- `tests/LoomTerminal.Architecture.Tests/DependencyRulesTests.cs` — update the WinUI row accordingly
- `samples/LoomTerminal.Gallery/MainWindow.xaml(.cs)` — start `cmd.exe` or the first detected shell in a session and
  assign it to the control

## Design decisions (already made — do not change)

- Properties (dependency properties unless noted): `Session` (`TerminalSession?`, plain CLR property — not a DP: it is
  not bindable data and must not be set from XAML), `Theme` (`TerminalTheme`, default dark VS Code-like theme of VIGO:
  copy the colors of `TerminalView.cs` 379–426), `FontFamily` / `FontSize` (inherited `Control` properties, used as
  `TerminalFont.Families` split on commas and `Size`), `LineHeight` (`double`, default 1.15 as VIGO),
  `Padding` (inherited, default `12,6,2,0` as VIGO's CSS `6px 2px 0 12px`), `CursorStyle`, `CursorBlink`.
- Events (raised on the UI thread): `TitleChanged`, `Exited`, `BellRang`, `KeyPreview` (args = `KeyPreviewEventArgs`,
  P3-04; setting `Handled` claims the key), `LinkActivated` (args with `Uri`, `Handled`; when not handled the control
  does nothing — the host opens links).
- Setting `Session` detaches the previous one (does not dispose it: the host owns sessions; VIGO keeps shells running
  while tabs are hidden).

## Tests

- [ ] Build + manual: the gallery starts a shell (nothing is drawn yet besides the placeholder).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Public API files updated (`./eng/Update-PublicApi.ps1 src/LoomTerminal.WinUI`)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing (P4-02 …).

## Suggested commit message

`feat(winui): add the TerminalControl public API skeleton`
