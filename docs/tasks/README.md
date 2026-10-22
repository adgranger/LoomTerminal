# Task cards

`docs/ROADMAP.md` says **what** each phase delivers. Task cards say **exactly how to get there, one small step at a
time**, so that a coding agent (Claude Sonnet) can implement one card per session without design work:

- **one card = one commit** (the user commits; the agent proposes the message), with its tests;
- at most ~400 lines of production code per card (most are much smaller);
- the xterm.js source to port is given with **file and line numbers** at the pinned commit
  `f447274f430fd22513f6adbf9862d19524471c04` (tag 6.0.0);
- design decisions are **already made** in the card: an agent must not re-decide them; if a card is wrong or
  ambiguous, it stops and reports instead of improvising;
- acceptance criteria are commands that pass or fail (always run through `rtk`).

How to work on a card: `CLAUDE.md` at the repository root and the `/next-task` skill
(`.claude/skills/next-task/SKILL.md`). Template for new cards: [`_template.md`](_template.md).
Pick the **first** card in table order whose status is `todo` and whose dependencies are all `done`
("P1-37 … P1-52" means every card in that range).

Cards of phases 5–7 are less detailed than those of phases 1–4 (they depend on choices made earlier): refine them,
card by card, when their phase starts, in the same format.

Design records: [ADR 0001](../adr/0001-baseline.md) (baseline), [ADR 0002](../adr/0002-core-design.md) (core design),
[ADR 0003](../adr/0003-testing-strategy.md) (testing), [ADR 0004](../adr/0004-input-model.md) (input model).

## Phase 1 — Emulator core (`LoomTerminal.Core`)

Port of xterm.js `src/common`. **Read [phase-1/README.md](phase-1/README.md) first.**

### Step A — Foundations

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P1-01](phase-1/P1-01-core-test-project-and-lifecycle.md) | Core test project, `Disposable`, `Emitter`, ported-file header rule | — | S | done |
| [P1-02](phase-1/P1-02-circular-list.md) | `CircularList<T>` | P1-01 | S | done |
| [P1-03](phase-1/P1-03-text-decoder.md) | Text decoders: `StringToUtf32`, `Utf8ToUtf32`, `Utf32ToString` | P1-01 | S | done |
| [P1-04](phase-1/P1-04-params.md) | `Params` (CSI / DCS parameters with sub-parameters) | P1-01 | S | done |

### Step B — Parser

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P1-05](phase-1/P1-05-parser-constants-and-types.md) | Parser constants and handler types | P1-04 | S | done |
| [P1-06](phase-1/P1-06-osc-parser.md) | `OscParser` and `OscHandler` | P1-05, P1-03 | S | done |
| [P1-07](phase-1/P1-07-dcs-parser.md) | `DcsParser` and `DcsHandler` | P1-06 | S | done |
| [P1-08](phase-1/P1-08-transition-table.md) | `TransitionTable` and the VT500 transition table | P1-05 | S | done |
| [P1-09](phase-1/P1-09-escape-sequence-parser-registry.md) | `EscapeSequenceParser` (1/2): fields, identifiers, handler registry | P1-06, P1-07, P1-08 | M | done |
| [P1-10](phase-1/P1-10-escape-sequence-parser-parse.md) | `EscapeSequenceParser` (2/2): the parse loop | P1-09 | M | done |
| [P1-11](phase-1/P1-11-parser-tests-1.md) | Parser tests (1/4): test terminal, init, transitions GROUND … CSI | P1-10 | M | done |
| [P1-12](phase-1/P1-12-parser-tests-2.md) | Parser tests (2/4): transitions CSI_IGNORE … DCS_PASSTHROUGH | P1-11 | M | done |
| [P1-13](phase-1/P1-13-parser-tests-3.md) | Parser tests (3/4): examples, coverage, set/clear handlers | P1-12 | M | done |
| [P1-14](phase-1/P1-14-parser-tests-4.md) | Parser tests (4/4): function identifiers | P1-13 | S | done |

### Step C — Cells and lines

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P1-15](phase-1/P1-15-buffer-constants-and-control-codes.md) | Buffer constants, `CharData`, C0/C1 control codes | P1-01 | S | done |
| [P1-16](phase-1/P1-16-attribute-data.md) | `AttributeData` and `ExtendedAttrs` | P1-15 | S | done |
| [P1-17](phase-1/P1-17-cell-data.md) | `CellData` | P1-16 | S | done |
| [P1-18](phase-1/P1-18-buffer-line-1.md) | `BufferLine` (1/2): storage and cell access | P1-17 | M | done |
| [P1-19](phase-1/P1-19-buffer-line-2.md) | `BufferLine` (2/2): editing, resize, copy, strings | P1-18 | M | done |
| [P1-20](phase-1/P1-20-buffer-line-tests.md) | `BufferLine` tests: combining chars, full width, extended attributes | P1-19 | S | done |

### Step D — Unicode, options, small services

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P1-21](phase-1/P1-21-unicode-service-v6.md) | `UnicodeService` and `UnicodeV6` | P1-01 | S | done |
| [P1-22](phase-1/P1-22-unicode-v11.md) | `UnicodeV11` (from `addon-unicode11`), active by default | P1-21 | S | done |
| [P1-23](phase-1/P1-23-options-service.md) | `TerminalOptions` and `OptionsService` | P1-01 | M | done |
| [P1-24](phase-1/P1-24-log-service-and-test-mocks.md) | `ILogService` and the test mocks | P1-21, P1-23 | S | done |
| [P1-25](phase-1/P1-25-charsets.md) | Charsets and `CharsetService` | P1-24 | S | done |
| [P1-26](phase-1/P1-26-color.md) | `Color` helpers | P1-01 | M | done |
| [P1-27](phase-1/P1-27-xparsecolor.md) | `XParseColor` | P1-01 | S | done |

### Step E — Buffer and core services

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P1-28](phase-1/P1-28-marker-range-reflow.md) | `Marker`, `BufferRange`, reflow helper functions | P1-02, P1-20 | M | done |
| [P1-29](phase-1/P1-29-buffer-1.md) | `Buffer` (1/3): structure, cells, tab stops, markers, strings | P1-28, P1-23, P1-24, P1-25 | M | done |
| [P1-30](phase-1/P1-30-buffer-2.md) | `Buffer` (2/3): resize without reflow, memory cleanup | P1-29 | M | done |
| [P1-31](phase-1/P1-31-buffer-3-reflow.md) | `Buffer` (3/3): reflow | P1-30 | M | done |
| [P1-32](phase-1/P1-32-buffer-reflow-cases.md) | `Buffer` tests: `reflowLarger` and `reflowSmaller` cases | P1-31 | M | done |
| [P1-33](phase-1/P1-33-buffer-set-and-buffer-service.md) | `BufferSet` and `BufferService` | P1-32 | M | done |
| [P1-34](phase-1/P1-34-core-service-and-osc-link-service.md) | `CoreService` and `OscLinkService` | P1-33 | S | done |
| [P1-35](phase-1/P1-35-core-mouse-service.md) | `CoreMouseService` (mouse protocols and encodings) | P1-34 | M | done |

### Step F — InputHandler (one family of sequences per card)

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P1-36](phase-1/P1-36-input-handler-shell.md) | `InputHandler` shell: fields, events, `Parse`, dirty rows, test harness | P1-10, P1-22, P1-35 | M | done |
| [P1-37](phase-1/P1-37-input-handler-print.md) | `InputHandler.Print`: printing, wrapping, wide and combining characters | P1-36 | M | done |
| [P1-38](phase-1/P1-38-input-handler-controls.md) | `InputHandler.Controls`: BEL, LF/VT/FF, CR, BS, HT, SO, SI | P1-37 | S | done |
| [P1-39](phase-1/P1-39-input-handler-cursor.md) | `InputHandler.Cursor`: CUU, CUD, CUF, CUB, CNL, CPL, CHA, CUP, HPA, HPR, VPA, VPR, HVP | P1-38 | M | done |
| [P1-40](phase-1/P1-40-input-handler-tabs.md) | `InputHandler.Tabs`: TBC, CHT, CBT, HTS | P1-39 | S | done |
| [P1-41](phase-1/P1-41-input-handler-erase.md) | `InputHandler.Erase`: ED, EL, ECH, DECSCA, DECSED, DECSEL | P1-39 | M | done |
| [P1-42](phase-1/P1-42-input-handler-insert-delete.md) | `InputHandler.InsertDelete`: IL, DL, ICH, DCH, SU, SD, SL, SR, DECIC, DECDC | P1-41 | M | done |
| [P1-43](phase-1/P1-43-input-handler-rep-and-device-attributes.md) | `InputHandler.DeviceAttributes`: REP, DA1, DA2 | P1-37 | S | done |
| [P1-44](phase-1/P1-44-input-handler-esc.md) | `InputHandler.Esc`: DECSC/DECRC, IND, NEL, RI, keypad modes, charsets, RIS, DECALN | P1-38, P1-25 | M | done |
| [P1-45](phase-1/P1-45-input-handler-modes.md) | `InputHandler.Modes`: SM, RM, DECSET, DECRST (incl. alternate screen, mouse, bracketed paste) | P1-44, P1-35 | M | done |
| [P1-46](phase-1/P1-46-input-handler-request-mode.md) | `InputHandler.RequestMode`: DECRQM (ANSI and DEC private) | P1-45 | S | done |
| [P1-47](phase-1/P1-47-input-handler-sgr-colors.md) | `InputHandler.Sgr` (1/2): color and underline helpers | P1-36 | M | done |
| [P1-48](phase-1/P1-48-input-handler-sgr.md) | `InputHandler.Sgr` (2/2): `charAttributes` and the SGR test suites | P1-47 | M | done |
| [P1-49](phase-1/P1-49-input-handler-status-and-margins.md) | `InputHandler.Status`: DSR, DECSTR, DECSCUSR, DECSTBM (+ scroll margin tests) | P1-42, P1-45, P1-48 | M | done |
| [P1-50](phase-1/P1-50-input-handler-window-options.md) | `InputHandler.WindowOptions`: XTWINOPS and title stacks | P1-49 | S | done |
| [P1-51](phase-1/P1-51-input-handler-osc.md) | `InputHandler.Osc`: titles, colors (4/10/11/12/104/110–112), hyperlinks (8) | P1-48, P1-26, P1-27, P1-34 | M | done |
| [P1-52](phase-1/P1-52-input-handler-dcs.md) | `InputHandler.Dcs`: DECRQSS | P1-49, P1-41 | S | done |
| [P1-53](phase-1/P1-53-input-handler-regression-tests.md) | `InputHandler` regression tests and chunked parsing | P1-37 … P1-52 | M | done |

### Step G — Terminal composition and close

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P1-54](phase-1/P1-54-terminal-core.md) | `TerminalCore` (port of `CoreTerminal`) | P1-53 | M | done |
| [P1-55](phase-1/P1-55-windows-pty-heuristics.md) | Windows PTY heuristics (`windowsPty`, `windowsMode`) | P1-54 | S | done |
| [P1-56](phase-1/P1-56-terminal-writer.md) | `TerminalWriter`: the single work loop of the emulator | P1-54 | S | done |
| [P1-57](phase-1/P1-57-close-phase-1.md) | Close phase 1 | all P1 cards | S | done |

## Phase 2 — Conformance and performance

Differential tests against xterm.js headless, recorded sessions, benchmarks. **Read [phase-2/README.md](phase-2/README.md) first.**

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P2-01](phase-2/P2-01-screen-snapshot.md) | Screen snapshots of `TerminalCore` | P1-57 | M | done |
| [P2-02](phase-2/P2-02-xterm-oracle.md) | `tools/xterm-oracle`: expected snapshots from xterm.js itself | P2-01 | M | done |
| [P2-03](phase-2/P2-03-scenario-fixtures.md) | Scenario fixtures and the differential test runner | P2-02 | M | done |
| [P2-04](phase-2/P2-04-session-recorder.md) | Record real ConPTY sessions (`PtyConsole --record`) | P2-01 | S | done |
| [P2-05](phase-2/P2-05-recorded-conformance.md) | Differential tests on recorded sessions | P2-03, P2-04 | S | done |
| [P2-06](phase-2/P2-06-benchmarks.md) | Benchmarks and reference numbers | P2-05 | S | done |
| [P2-07](phase-2/P2-07-close-phase-2.md) | Close phase 2 | P2-01 … P2-06 and any follow-up card they opened | S | done |

## Phase 3 — Presentation (UI-independent)

Session and threads, render frames, input, selection, links, search. **Read [phase-3/README.md](phase-3/README.md) first.**

### Step A — Keyboard and text input

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P3-01](phase-3/P3-01-presentation-test-project.md) | Presentation test project and public API baseline | P2-07 | S | done |
| [P3-02](phase-3/P3-02-key-event-model.md) | Key event model (`TerminalKeyEvent`) | P3-01 | S | done |
| [P3-03](phase-3/P3-03-keyboard-evaluation.md) | Port `evaluateKeyboardEvent` (`Keyboard.ts`) | P3-02 | M | done |
| [P3-04](phase-3/P3-04-key-input-handler.md) | `KeyInputHandler`: from key event to pty bytes, with host shortcuts | P3-03 | M | done |
| [P3-05](phase-3/P3-05-text-input-and-paste.md) | Text input and paste encoding | P3-02 | S | done |

### Step B — Session, theme and frames

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P3-06](phase-3/P3-06-terminal-session-1.md) | `TerminalSession` (1/2): pty, parser thread, input, resize, exit | P3-01, P1-56 | M | done |
| [P3-07](phase-3/P3-07-theme.md) | `TerminalTheme` and the 256-color palette | P3-01, P1-26 | M | done |
| [P3-08](phase-3/P3-08-cell-color-resolver.md) | Cell color resolution | P3-07 | M | done |
| [P3-09](phase-3/P3-09-render-frame.md) | `RenderFrame` and `RenderFrameBuilder` | P3-08 | M | done |
| [P3-10](phase-3/P3-10-terminal-session-2-frames.md) | `TerminalSession` (2/2): frame publication and idle work | P3-06, P3-09 | M | done |
| [P3-11](phase-3/P3-11-cursor-blink.md) | Cursor blinking and cursor styles | P3-09 | S | done |
| [P3-12](phase-3/P3-12-viewport-and-wheel.md) | Viewport scrolling and the mouse wheel | P3-10 | M | done |

### Step C — Selection, clipboard, links, search

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P3-13](phase-3/P3-13-selection-model.md) | `SelectionModel` | P3-01 | S | done |
| [P3-14](phase-3/P3-14-selection-logic.md) | Selection logic (`SelectionService`, non-DOM parts) | P3-13, P3-09 | M | done |
| [P3-15](phase-3/P3-15-pointer-handling.md) | Pointer handling: selection gestures vs. mouse reporting | P3-14, P3-12 | M | done |
| [P3-16](phase-3/P3-16-clipboard.md) | Clipboard: copy, paste, right click | P3-04, P3-05, P3-15 | S | done |
| [P3-17](phase-3/P3-17-links.md) | Links: URL detection and OSC 8, Ctrl+click | P3-15 | M | todo |
| [P3-18](phase-3/P3-18-search-engine.md) | Search engine (`addon-search`) | P3-14 | M | todo |
| [P3-19](phase-3/P3-19-search-session-api.md) | Search API on the session and match highlights | P3-18, P3-09 | S | todo |

### Step D — Rendering contracts, accessibility, close

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P3-20](phase-3/P3-20-backend-contracts.md) | Backend contracts: `ITextMeasurer`, `CellMetrics`, `IRenderSurface` | P3-09 | S | todo |
| [P3-21](phase-3/P3-21-frame-renderer.md) | `FrameRenderer`: from a render frame to draw calls | P3-20, P3-14, P3-11 | M | todo |
| [P3-22](phase-3/P3-22-accessibility-model.md) | Accessibility text model | P3-09 | S | todo |
| [P3-23](phase-3/P3-23-close-phase-3.md) | Close phase 3 | all P3 cards | S | todo |

## Phase 4 — WinUI 3 control (`LoomTerminal.WinUI`)

Win2D renderer and WinUI input. **Read [phase-4/README.md](phase-4/README.md) first.**

### Step A — Rendering

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P4-01](phase-4/P4-01-control-api-skeleton.md) | `TerminalControl` public API skeleton | P3-23 | S | todo |
| [P4-02](phase-4/P4-02-win2d-text-measurer.md) | Win2D text measurer (cell metrics) | P4-01 | S | todo |
| [P4-03](phase-4/P4-03-win2d-render-surface.md) | Win2D render surface: backgrounds and text runs | P4-02 | M | todo |
| [P4-04](phase-4/P4-04-custom-glyphs.md) | Box drawing, block elements and powerline glyphs as vectors | P4-03 | M | todo |
| [P4-05](phase-4/P4-05-decorations-and-cursor.md) | Underlines, strikethrough, overline and the cursor in Win2D | P4-03 | S | todo |
| [P4-06](phase-4/P4-06-layout-dpi-frames.md) | Size to columns/rows, DPI and device loss, frame scheduling | P4-05 | M | todo |

### Step B — Input

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P4-07](phase-4/P4-07-keyboard.md) | Keyboard: `VirtualKey` → `TerminalKeyEvent`, AltGr, host shortcuts | P4-06 | M | todo |
| [P4-08](phase-4/P4-08-character-input.md) | Character input (`CharacterReceived`), dead keys, surrogate pairs | P4-07 | S | todo |
| [P4-09](phase-4/P4-09-ime.md) | IME composition with `CoreTextEditContext` | P4-08 | M | todo |
| [P4-10](phase-4/P4-10-pointer.md) | Pointer: selection, mouse reporting, wheel, links, cursor shape | P4-06 | M | todo |
| [P4-11](phase-4/P4-11-clipboard.md) | WinRT clipboard | P4-10 | S | todo |

### Step C — Control features

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P4-12](phase-4/P4-12-scrollbar.md) | Scrollbar and scrollback keys | P4-06 | S | todo |
| [P4-13](phase-4/P4-13-focus-blink-events.md) | Focus, cursor blink timer, title / bell / exit events | P4-06 | S | todo |
| [P4-14](phase-4/P4-14-theme-and-font-properties.md) | Live theme and font changes | P4-12, P4-13 | S | todo |
| [P4-15](phase-4/P4-15-accessibility.md) | UI Automation: peer, text provider, announcements | P3-22, P4-13 | M | todo |

### Step D — Gallery, performance, close

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P4-16](phase-4/P4-16-gallery.md) | Gallery: shells in tabs, settings, demo scripts | P4-14, P4-15, P4-11, P4-09, P4-04 | M | todo |
| [P4-17](phase-4/P4-17-performance.md) | Rendering performance: measure, then fix with numbers | P4-16 | S | todo |
| [P4-18](phase-4/P4-18-close-phase-4.md) | Close phase 4 | all P4 cards (follow-up optimization cards may stay open) | S | todo |

## Phase 5 — Integration into VIGO

Changes happen in `D:\VIGO`, under VIGO's rules. **Read [phase-5/README.md](phase-5/README.md) first.**

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P5-01](phase-5/P5-01-consume-packages.md) | VIGO consumes the LoomTerminal packages | P4-18 | S | todo |
| [P5-02](phase-5/P5-02-replace-conpty-session.md) | Replace VIGO's ConPTY code with `LoomTerminal.Pty` | P5-01 | S | todo |
| [P5-03](phase-5/P5-03-native-terminal-view.md) | `NativeTerminalView` in VIGO's terminal panel | P5-02 | M | todo |
| [P5-04](phase-5/P5-04-remove-webview-terminal.md) | Remove the WebView2 terminal from VIGO | P5-03 | S | todo |
| [P5-05](phase-5/P5-05-parity-validation.md) | Parity validation and close phase 5 | P5-04 | S | todo |

## Phase 6 — Avalonia control (Windows first)

Same behavior as WinUI, drawn with Skia. **Read [phase-6/README.md](phase-6/README.md) first.**

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P6-01](phase-6/P6-01-avalonia-project.md) | `LoomTerminal.Avalonia` project, sample and CI | P5-05 (or P4-18 if VIGO integration is postponed) | S | todo |
| [P6-02](phase-6/P6-02-skia-text-measurer.md) | Skia text measurer | P6-01 | S | todo |
| [P6-03](phase-6/P6-03-skia-render-surface.md) | Skia render surface (text, rectangles, decorations, custom glyphs) | P6-02 | M | todo |
| [P6-04](phase-6/P6-04-control-layout-frames.md) | Avalonia control: size, scaling, frames, session events | P6-03 | M | todo |
| [P6-05](phase-6/P6-05-keyboard-text-ime.md) | Avalonia keyboard, text input and IME | P6-04 | M | todo |
| [P6-06](phase-6/P6-06-pointer-clipboard-scrollbar.md) | Avalonia pointer, clipboard and scrollbar | P6-04 | M | todo |
| [P6-07](phase-6/P6-07-accessibility-and-gallery.md) | Avalonia accessibility and gallery parity | P6-05, P6-06 | M | todo |
| [P6-08](phase-6/P6-08-close-phase-6.md) | Close phase 6 | all P6 cards | S | todo |

## Phase 7 — Unix pty, Linux and macOS

Native spawn shim, Unix pty, cross-platform CI.

| Card | Title | Depends on | Size | Status |
|---|---|---|---|---|
| [P7-01](phase-7/P7-01-unix-pty-design.md) | Design: starting a process under a Unix pty from .NET | P6-08 | S | todo |
| [P7-02](phase-7/P7-02-native-shim.md) | Native shim `libloomterm_pty` and its build | P7-01 (ADR accepted) | S | todo |
| [P7-03](phase-7/P7-03-unix-pty-connection.md) | `UnixPtyConnection` | P7-02 | M | todo |
| [P7-04](phase-7/P7-04-unix-shell-detection.md) | Unix shell detection | P7-03 | S | todo |
| [P7-05](phase-7/P7-05-cross-platform-ci.md) | Cross-platform CI: tests and Avalonia AOT on Linux and macOS | P7-03 | S | todo |
| [P7-06](phase-7/P7-06-avalonia-linux-macos.md) | Avalonia on Linux and macOS: platform details | P7-04, P7-05 | M | todo |
| [P7-07](phase-7/P7-07-close-phase-7.md) | Close phase 7 and prepare 1.0 | all P7 cards | S | todo |

**125 cards.** Status values: `todo`, `in progress`, `done`. Follow-up cards found while working are added at the end of their phase, numbered after its last card.
