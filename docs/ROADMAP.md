# Roadmap

Every phase is split into small task cards: see [`docs/tasks/README.md`](tasks/README.md) (125 cards).

| Phase | Content | Cards | Done when |
|---|---|---|---|
| 0 | Skeleton: projects, CI with Native AOT smoke test, architecture tests, `LoomTerminal.Pty` (ConPTY from VIGO) | — | ✅ build + tests green (AOT checked on CI) |
| 1 | **Core**: port of xterm.js `src/common` — parser, buffers, reflow, services, `InputHandler`, `TerminalCore`, `TerminalWriter` | P1-01 … P1-57 | ✅ xterm.js tests ported and green |
| 2 | **Conformance and performance**: screen snapshots, xterm.js headless oracle, scenario and recorded-session fixtures, benchmarks | P2-01 … P2-07 | ✅ all fixtures match xterm.js; throughput measured ([performance.md](performance.md)) |
| 3 | **Presentation**: `TerminalSession` (parser thread, frames), keyboard and pointer input, theme and colors, selection, clipboard, links, search, render contracts, accessibility model | P3-01 … P3-23 | ✅ unit tests without UI |
| 4 | **WinUI control**: Win2D measurer and surface, custom glyphs, decorations, layout/DPI/frames, keyboard, IME, pointer, clipboard, scrollbar, focus, theming, UIA, gallery, performance | P4-01 … P4-18 ✅ Gallery runs a real shell, AOT published (manual checklist pending, rendering measurements: P4-19) |
| 5 | **VIGO integration**: packages, `LoomTerminal.Pty` in VIGO, native terminal view, removal of the WebView2 terminal, parity | P5-01 … P5-05 | Feature parity list below is green in VIGO |
| 6 | **Avalonia on Windows**: Skia measurer and surface, control, input, accessibility, gallery | P6-01 … P6-08 | No change needed in Core or Presentation (if one is, the abstraction leaks) |
| 7 | **Unix pty** (native forkpty shim), shell detection, cross-platform CI, Avalonia on Linux and macOS | P7-01 … P7-07 | Same tests green on the three OSes |

## Feature parity with VIGO's terminal (xterm.js 6.0, ADR-071 in VIGO)

The native control must match what VIGO's `TerminalView` does today before it replaces it (checked in P5-05):

- [ ] Scrollback 10,000 lines; cursor blink; font size 12, line height 1.15
- [ ] Font stack Cascadia Mono / Cascadia Code, then Nerd Font fallbacks (oh-my-posh, starship glyphs)
- [ ] Unicode 11 character widths
- [ ] ConPTY-aware reflow (xterm.js `windowsPty: { backend: 'conpty', buildNumber }`: do not reflow what ConPTY rewraps)
- [ ] Theme set live by the host: background, foreground, cursor, cursor accent, selection, inactive selection,
      16 ANSI colors; follows light/dark switches
- [ ] Links: https URLs, activated with Ctrl+click only; the host opens them
- [ ] Keyboard: AltGr types characters, never shortcuts; the host can claim shortcuts (VIGO relays its Ctrl+Shift table)
- [ ] Copy: Ctrl+Shift+C, or Ctrl+C when there is a selection (otherwise Ctrl+C is the interrupt)
- [ ] Paste: Ctrl+V, Ctrl+Shift+V, Shift+Insert, with bracketed paste
- [ ] Right click: copy the selection, or paste when there is none (Windows Terminal behavior)
- [ ] Resize to the control size (skipped while hidden / zero-sized)
- [ ] Shell exit reported to the host (VIGO prints `[exited code]` and restarts on Enter)

Deferred in VIGO and therefore not required for parity, but planned after phase 7: search UI (the engine is in P3-18),
OSC 133 shell integration marks, `path:line` file links, OSC 52 clipboard, scrollback restore, ligatures, grapheme
clusters (`addon-unicode-graphemes`).

## Possible later improvements to `LoomTerminal.Pty`

- SafeHandles instead of `nint` for the console, process and job handles.
- Pooled output buffers instead of one array per chunk.
- `CreatePseudoConsole` flags (`PSEUDOCONSOLE_INHERIT_CURSOR`) as an option.
