# P5-04 — Remove the WebView2 terminal from VIGO

- **Status:** todo
- **Depends on:** P5-03
- **Size:** S

## Goal

Delete the xterm.js page, its build and the WebView2 `TerminalView`, now unused. Monaco (also WebView2) stays.

## Files to create or change (in `D:\VIGO`)

- `src/Vigo.App/Controls/TerminalView.cs` — delete
- `tools/editor/src/terminal.js`, `terminal.css`, `terminal.html` — delete
- `tools/editor/build.mjs` — remove the terminal entry; `tools/editor/package.json` — remove `@xterm/*` dependencies;
  regenerate `package-lock.json` (`cd tools/editor; rtk npm install`) and the generated `src/Vigo.App/Editor/` output
  (`rtk node build.mjs`) — generated files are never edited by hand (VIGO rule)
- `CLAUDE.md` (VIGO) — architecture section: `Controls/TerminalView` → `Controls/NativeTerminalView`; remove the
  `window.vigoTerminal` debugging note
- `docs/DECISIONS.md` — mark ADR-071's WebView2 part as superseded by the ADR of P5-01

## Design decisions (already made — do not change)

- The WebView2 environment sharing code (`CodeEditor.EnvironmentAsync()`) stays for Monaco.

## Tests

- [ ] VIGO builds, tests pass; the editor (Monaco) still works (diff, blame, conflicts).

## Acceptance criteria

- [ ] VIGO builds, tests pass, `/aot-check` OK (the publish size should drop: note it)
- [ ] Commit message proposed (VIGO)
- [ ] Card status and `D:\LoomTerminal\docs\tasks\README.md` updated

## Out of scope

—

## Suggested commit message (VIGO)

`J<n>.<m>: remove the WebView2 terminal`
