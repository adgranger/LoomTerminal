# P5-03 — `NativeTerminalView` in VIGO's terminal panel

- **Status:** todo
- **Depends on:** P5-02
- **Size:** M

## Goal

A VIGO control hosting `TerminalControl` that reproduces everything `TerminalView` (WebView2) does, and the terminal
panel uses it instead of `TerminalView`.

## Sources (VIGO)

| File | What to reproduce |
|---|---|
| `src/Vigo.App/Controls/TerminalView.cs` | 177–258: start after the first size is known, failure message (localized, red); 221–228, 297–301: dimmed `[exited code]` line and Enter restarts; 207–211: https-only links → `IUrlLauncher`; 353–376: relayed shortcuts (`KeyboardShortcuts` table, Ctrl+Shift combinations except C and V, terminal toggle); 379–426: theme colors from `DesignTokens` + `ActualThemeChanged` |
| `src/Vigo.App/Views/TerminalPanel.xaml(.cs)` | 78–117: one view per tab, only the selected one visible, hidden panel keeps shells |

## Files to create or change (in `D:\VIGO`)

- `src/Vigo.App/Controls/NativeTerminalView.cs` (+ `.xaml` if needed) — create
- `src/Vigo.App/Views/TerminalPanel.xaml(.cs)` — use `NativeTerminalView`
- `src/Vigo.App/Strings/{fr-FR,en-US}/Resources.resw` — any new string

## Design decisions (already made — do not change)

- One `TerminalSession` per tab, owned by the tab view-model's lifetime (closing a tab disposes it; hiding the panel
  does not).
- Theme: build a `TerminalTheme` from `DesignTokens` (same colors as `TerminalView.cs` 379–426) on load and on
  `ActualThemeChanged`. Font: VIGO's font stack (`Cascadia Mono, Cascadia Code, <Nerd Font fallbacks>` — copy it from
  `tools/editor/src/terminal.js` 26–38), size 12, line height 1.15. Scrollback 10 000, cursor blink on.
- `WindowsPty` option: `{ Backend = "conpty", BuildNumber = Environment.OSVersion.Version.Build }` (VIGO passed
  `?build=` to the page).
- Exit: `Session.WriteToDisplay("\r\n\x1b[2m[exited {code}]\x1b[0m")` (P3-06), then a `KeyPreview` handler restarts the
  shell on Enter (new session in the same view).
- Shortcuts: `KeyPreview` → `KeyboardShortcuts.TryInvoke(key, modifiers)` for the same keys the page relayed; `Handled`
  when it returns true.
- Links: `LinkActivated` → `IUrlLauncher` for `https` only; set `Handled`.

## Tests

- [ ] VIGO unit tests green; any view-model change covered by a test in `Vigo.Core.Tests`.
- [ ] Manual + UIA replay (VIGO's CLAUDE.md "Rejeu UI Automation"): open the panel, start each shell, type, copy/paste,
      resize, switch tabs, hide/show the panel, exit and restart with Enter, Ctrl+click a link, run a relayed shortcut,
      switch light/dark theme.

## Acceptance criteria

- [ ] VIGO builds, tests pass, `/aot-check` OK and the journeys above replayed in the AOT exe
- [ ] Commit message proposed (VIGO)
- [ ] Card status and `D:\LoomTerminal\docs\tasks\README.md` updated

## Out of scope

Deleting the WebView2 terminal (P5-04).

## Suggested commit message (VIGO)

`J<n>.<m>: native terminal view (LoomTerminal) in the terminal panel`
