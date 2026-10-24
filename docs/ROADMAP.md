# Roadmap

Every phase is split into small task cards: see [`docs/tasks/README.md`](tasks/README.md) (142 cards).

| Phase | Content | Cards | Done when |
|---|---|---|---|
| 0 | Skeleton: projects, CI with Native AOT smoke test, architecture tests, `LoomTerminal.Pty` (ConPTY from VIGO) | — | ✅ build + tests green (AOT checked on CI) |
| 1 | **Core**: port of xterm.js `src/common` — parser, buffers, reflow, services, `InputHandler`, `TerminalCore`, `TerminalWriter` | P1-01 … P1-57 | ✅ xterm.js tests ported and green |
| 2 | **Conformance and performance**: screen snapshots, xterm.js headless oracle, scenario and recorded-session fixtures, benchmarks | P2-01 … P2-07 | ✅ all fixtures match xterm.js; throughput measured ([performance.md](performance.md)) |
| 3 | **Presentation**: `TerminalSession` (parser thread, frames), keyboard and pointer input, theme and colors, selection, clipboard, links, search, render contracts, accessibility model | P3-01 … P3-23 | ✅ unit tests without UI |
| 4 | **WinUI control**: Win2D measurer and surface, custom glyphs, decorations, layout/DPI/frames, keyboard, IME, pointer, clipboard, scrollbar, focus, theming, UIA, gallery, performance | P4-01 … P4-18 ✅ Gallery runs a real shell, AOT published (manual checklist pending, rendering measurements: P4-19) |
| 5 | **VIGO integration**: packages, `LoomTerminal.Pty` in VIGO, native terminal view, removal of the WebView2 terminal, parity | P5-01 … P5-05 | Feature parity list below is green in VIGO |
| 6 | **Avalonia on Windows**: Skia measurer and surface, control, input, accessibility, gallery | P6-01 … P6-08 | ✅ Avalonia control and gallery on Windows, AOT published (manual checklist pending). No change needed in Core or Presentation (if one is, the abstraction leaks) |
| 7 | **Unix pty** (native forkpty shim), shell detection, cross-platform CI, Avalonia on Linux and macOS | P7-01 … P7-07 | ✅ Same tests green on the three OSes (CI); manual checklists on Linux and macOS pending |
| 8 | **First release (NuGet)**: version from the git tag, package metadata and README checks, changelog, release workflow (tag → gates → nuget.org + GitHub Release, with a dry run), runbook, release checklist | P8-01 … P8-08 | Creating the first `v*` tag publishes the four packages; the user-only steps are in [RELEASING.md](RELEASING.md) (independent of phase 5) |

### Abstraction leaks found

Phase 6: none. No Presentation card was opened; the only change outside `LoomTerminal.Avalonia` is an
`InternalsVisibleTo` entry in `LoomTerminal.Presentation.csproj` (same mechanism as WinUI).

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

## Before 1.0

The decision to publish is the maintainer's; what is left:

- [ ] Manual checklists run on Linux (X11, Wayland) and macOS ([manual-tests.md](manual-tests.md)).
- [ ] API review: move `PublicAPI.Unshipped.txt` to `PublicAPI.Shipped.txt` once the surface is frozen.
- [ ] Add `LoomTerminal.Avalonia` and the native pty shim (`runtimes/<rid>/native/`) to the release workflow, `Pack.ps1`
      and the changelog ([RELEASING.md](RELEASING.md)); first nuget.org publication.
- [ ] Deferred features: search UI in the samples (the engine exists), OSC 133 shell integration, OSC 52 clipboard,
      `path:line` file links, scrollback restore, ligatures, grapheme clusters (`addon-unicode-graphemes`).
- [ ] Performance follow-ups (rendering measurements: P4-19, see [performance.md](performance.md)).

## Possible later improvements to `LoomTerminal.Pty`

- SafeHandles instead of `nint` for the console, process and job handles.
- Pooled output buffers instead of one array per chunk.
- `CreatePseudoConsole` flags (`PSEUDOCONSOLE_INHERIT_CURSOR`) as an option.
