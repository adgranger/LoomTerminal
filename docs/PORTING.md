# Porting xterm.js

## Pinned upstream

Port from **one pinned commit** of https://github.com/xtermjs/xterm.js, never from memory or from `master`:

- Tag `6.0.0` (the version of `@xterm/xterm` used by VIGO), commit **`f447274f430fd22513f6adbf9862d19524471c04`**
- Raw file URL: `https://raw.githubusercontent.com/xtermjs/xterm.js/f447274f430fd22513f6adbf9862d19524471c04/<path>`
- Oracle for differential tests (phase 2): `@xterm/headless` `6.0.0` from npm

## Rules

1. **Port the tests first, then the code.** Keep each xterm.js test name in a comment above the test:
   `// xterm.js: 'should parse CSI with params'`.
2. Translate faithfully: keep xterm.js algorithm and concept names (`EscapeSequenceParser`, `BufferLine`,
   `CellData`…). Do not optimize while porting; measure first (phase 2 benchmarks).
3. Typed arrays become arrays or spans (`Uint32Array` → `uint[]` / `Span<uint>`); JS numbers used as bit fields
   become `uint` / `int` with the same masks. Full map and JS traps: [`tasks/phase-1/README.md`](tasks/phase-1/README.md).
4. Header of every ported file:
   ```csharp
   // Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.ts
   // Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.
   ```
5. Any deliberate behavior difference: document it in the member's XML doc and in the table below.

## Map (xterm.js → LoomTerminal)

Filled by the task cards (✅ done, 🔶 in progress, "replaced", "not ported").

| xterm.js | LoomTerminal | Status | Card |
|---|---|---|---|
| `src/common/CircularList.ts` (+ test) | `Core/Common/CircularList.cs` | ✅ | P1-02 |
| `src/common/input/TextDecoder.ts` (+ test) | `Core/Input/TextDecoder.cs`, `StringToUtf32.cs`, `Utf8ToUtf32.cs` | ✅ | P1-03 |
| `src/common/parser/Params.ts`, `Types.ts` (`IParams`) (+ test) | `Core/Parser/Params.cs`, `IParams.cs` | ✅ | P1-04 |
| `src/common/parser/Constants.ts`, `Types.ts` (handler types) | `Core/Parser/ParserConstants.cs`, `ParserTypes.cs`, `IOscHandler.cs`, `IDcsHandler.cs` | ✅ (stack types not ported) | P1-05 |
| `src/common/parser/OscParser.ts` (+ test) | `Core/Parser/OscParser.cs`, `OscHandler.cs` | ✅ | P1-06 |
| `src/common/parser/DcsParser.ts` (+ test) | `Core/Parser/DcsParser.cs`, `DcsHandler.cs` | ✅ | P1-07 |
| `src/common/parser/EscapeSequenceParser.ts` (+ test) | `Core/Parser/TransitionTable.cs`, `Vt500TransitionTable.cs`, `EscapeSequenceParser.cs`, `EscapeSequenceParser.Parse.cs`, `IEscapeSequenceParser.cs` | ✅ | P1-08 … P1-14 |
| `src/common/buffer/Constants.ts`, `Types.ts` (`CharData`), `data/EscapeSequences.ts` | `Core/Buffer/BufferConstants.cs`, `CharData.cs`, `Core/Data/EscapeSequences.cs` | ✅ | P1-15 |
| `src/common/buffer/AttributeData.ts` (+ `BufferLine.test.ts` AttributeData suite) | `Core/Buffer/AttributeData.cs`, `ExtendedAttrs.cs` | ✅ | P1-16 |
| `src/common/buffer/CellData.ts` (+ `BufferLine.test.ts` CellData suite) | `Core/Buffer/CellData.cs` | ✅ | P1-17 |
| `src/common/buffer/BufferLine.ts` (+ `IBufferLine` of `Types.ts`) | `Core/Buffer/BufferLine*.cs`, `IBufferLine.cs` | ✅ | P1-18, P1-19 |
| `src/common/services/UnicodeService.ts`, `Services.ts` (Unicode types), `src/common/input/UnicodeV6.ts` (+ tests) | `Core/Services/UnicodeService.cs`, `IUnicodeService.cs`, `IUnicodeVersionProvider.cs`, `Core/Input/UnicodeV6.cs` | ✅ | P1-21 |
| `addons/addon-unicode11/src/UnicodeV11.ts`, `Unicode11Addon.ts` | `Core/Input/UnicodeV11.cs` (registered and active in `UnicodeService`) | ✅ | P1-22 |
| `src/common/services/OptionsService.ts` (+ test), `Services.ts` (`IOptionsService`) | `Core/Services/TerminalOptions.cs`, `TerminalOption.cs`, `IOptionsService.cs`, `OptionsService.cs` | ✅ (Core subset) | P1-23 |
| `src/common/data/Charsets.ts`, `services/CharsetService.ts`, `Services.ts` (`ICharsetService`) | `Core/Data/Charsets.cs`, `Core/Services/CharsetService.cs`, `ICharsetService.cs` | ✅ | P1-25 |
| `src/common/Color.ts`, `Types.ts` (`IColor`) (+ test) | `Core/Common/Color.cs` (`TerminalColor`, `Channels`, `ColorOps`, `Css`, `Rgb`, `Rgba`, `ColorMath`) | ✅ | P1-26 |
| `src/common/input/XParseColor.ts` (+ test) | `Core/Input/XParseColor.cs` | ✅ | P1-27 |
| `src/common/buffer/Buffer.ts` (structure, tab stops, markers), `buffer/Types.ts` (`IBuffer`), `Services.ts` (`IBufferService`: `Cols`, `Rows`) | `Core/Buffer/Buffer.cs`, `IBuffer.cs`, `Core/Services/IBufferService.cs` | ✅ | P1-29 … P1-32 |
| `src/common/buffer/Marker.ts`, `BufferRange.ts`, `BufferReflow.ts` (+ `BufferRange` and `BufferReflow` tests), `Types.ts` (`IMarker`) | `Core/Buffer/Marker.cs`, `IMarker.cs`, `BufferRange.cs`, `BufferReflow.cs` | ✅ | P1-28 |
| `src/common/buffer/BufferSet.ts`, `services/BufferService.ts`, `Services.ts` (`IBufferService`) (+ `BufferSet` test) | `Core/Buffer/BufferSet.cs`, `Core/Services/BufferService.cs`, `IBufferService.cs` | ✅ | P1-33 |
| `src/common/services/CoreService.ts`, `OscLinkService.ts`, `Services.ts` (`ICoreService`, `IOscLinkService`), `Types.ts` (`IModes`, `IDecPrivateModes`, `IOscLinkData`) (+ tests) | `Core/Services/CoreService.cs`, `ICoreService.cs`, `TerminalModes.cs`, `OscLinkService.cs`, `IOscLinkService.cs` | ✅ | P1-34 |
| `src/common/services/CoreMouseService.ts`, `Services.ts` (`ICoreMouseService`), `Types.ts` (`ICoreMouseEvent`, `CoreMouseEncoding`, …) (+ test, `MockCoreMouseService`) | `Core/Services/CoreMouseService.cs`, `ICoreMouseService.cs`, `CoreMouseTypes.cs` | ✅ | P1-35 |
| `src/common/InputHandler.ts` (+ `InputHandler.test.ts`, `Types.ts` color events) | `Core/InputHandling/InputHandler.cs` (+ partials `.Print`, `.Controls`, `.Cursor`, `.Tabs`, `.Erase`, `.InsertDelete`, `.DeviceAttributes`, `.Modes`, `.RequestMode`, `.Sgr`, `.Status`, `.WindowOptions`, `.Esc`, `.Osc`, `.Dcs`), `DirtyRowTracker.cs`, `ColorEvents.cs` | ✅ | P1-36 … P1-53 |
| `src/common/CoreTerminal.ts` (`write` is replaced by `TerminalWriter`) | `Core/TerminalCore.cs` (+ `InputHandler.Registration.cs` for the `register*Handler` forwarders) | ✅ | P1-54, P1-55 |
| `src/common/WindowsMode.ts` | `Core/WindowsMode.cs` (heuristics wired in `TerminalCore`) | ✅ | P1-55 |
| `src/common/services/LogService.ts`, `Services.ts` (`ILogService`) | `Core/Services/ILogService.cs` (interface only, test mocks in the test project) | replaced | P1-24 |
| `src/common/input/WriteBuffer.ts` (ideas only) | `Core/TerminalWriter.cs`, `TerminalWorkItem.cs` (replaces `WriteBuffer`) | ✅ | P1-56 |
| `src/common/input/Keyboard.ts` (+ test), `Types.ts` (`IKeyboardResult`) | `Presentation/Input/Keyboard.cs`, `KeyboardResult.cs` (`DomKeyCodes.cs` for key codes) | ✅ | P3-03 |
| `src/browser/Clipboard.ts` (`prepareTextForTerminal`, `bracketTextForPaste`, the `paste` logic) (+ test) | `Presentation/Input/PasteEncoding.cs` (copy, paste events and textarea code not ported: DOM) | ✅ | P3-05 |
| `src/browser/services/ThemeService.ts`, `Types.ts` (`DEFAULT_ANSI_COLORS`), `CoreBrowserTerminal._handleColorEvent` (+ test), `Services.ts` (`ITheme`) | `Presentation/Theming/ThemePalette.cs`, `TerminalTheme.cs` (scrollbar and overview ruler colors not ported; contrast caches: see P3-08) | ✅ | P3-07 |
| `addons/addon-webgl/src/CellColorResolver.ts`, color functions of `TextureAtlas.ts` (`_getBackgroundColor`, `_getForegroundColor`, `_getMinimumContrastColor`, underline color), `src/browser/ColorContrastCache.ts`, `renderer/shared/RendererUtils.ts` (+ tests) | `Presentation/Theming/CellColorResolver.cs`, `ColorContrastCache.cs`, `Presentation/Rendering/RendererUtils.cs` (decorations, dotted underline variant offset and unused utils not ported) | ✅ | P3-08 |
| `addons/addon-webgl/src/CursorBlinkStateManager.ts` | `Presentation/Rendering/CursorBlinkState.cs` (a function of time instead of timers and animation frames) | ✅ | P3-11 |
| `addons/addon-webgl/src/WebglRenderer.ts`, `renderLayer/BaseRenderLayer.ts`, `renderer/shared/RendererUtils.ts` (ideas only) | `Presentation/Rendering/FrameRenderer.cs` (draw order and cursor shapes; selection and search colors come blended in the run backgrounds; underline continuity by merging adjacent runs instead of variant offsets; bar cursor is 1 DIP) | ✅ | P3-21 |
| `src/browser/AccessibilityManager.ts` (live region logic only: `_handleChar`, `_handleTab`, `_handleKey`, `_clearLiveRegion`, `MAX_ROWS_TO_READ`) | `Presentation/Session/TerminalSession.Accessibility.cs` (no DOM tree or debouncer: announcements are flushed after each parser batch; a trailing newline is trimmed; keys are noted through `SendText` only) | ✅ | P3-22 |
| `addons/addon-webgl/src/CustomGlyphs.ts` (`blockElementDefinitions`, `patternCharacterDefinitions`, `boxDrawingDefinitions`, `powerlineDefinitions`, `tryDrawCustomChar` and the `draw*Char` functions, `translateArgs`) | `Presentation/Rendering/CustomGlyphs.cs`, `CustomGlyphs.Definitions.cs`, `CustomGlyphs.BoxDrawing.cs`, `CustomGlyphPath.cs` (each canvas call becomes one `CustomGlyphCommand`; `IRenderSurface.DrawCustomGlyph` replays them; shade patterns are alpha fills, see Behavior differences) | ✅ | P4-04 |
| `src/browser/selection/SelectionModel.ts` (+ test) | `Presentation/Selection/SelectionModel.cs` (positions are `(int X, int Y)?` tuples; `ISelectionRedrawRequestEvent` not needed yet) | ✅ | P3-13 |
| `src/browser/services/SelectionService.ts` (non-DOM parts) (+ test), `renderer/shared/SelectionRenderModel.ts` | `Presentation/Selection/SelectionService.cs`, `SelectionService.Words.cs`, `Presentation/Rendering/SelectionRenderModel.cs` (mouse handling, drag scroll and Linux selection are P3-15) | ✅ | P3-14 |
| `src/browser/services/SelectionService.ts` (mouse handling), `CoreBrowserTerminal.ts` (`bindMouse`), `src/browser/input/MoveToCell.ts` (+ test) | `Presentation/Input/PointerInputHandler.cs`, `MoveToCell.cs` (no DOM listeners: the backend sends `TerminalPointerEvent`; Linux mouse selection and the link under the pointer are not ported) | ✅ | P3-15 |
| `addons/addon-web-links/src/WebLinkProvider.ts`, `WebLinksAddon.ts` (regex), `src/browser/OscLinkProvider.ts`, `src/browser/Linkifier.ts` (hover, `_linkAtPosition`) | `Presentation/Links/WebLinkComputer.cs`, `OscLinkProvider.cs`, `LinkTracker.cs` (no hover/leave callbacks, no link cache; the link under the pointer is recomputed on each pointer event with Ctrl; `Uri` replaces JS `URL` in `isUrl`; OSC 8 links other than http(s) are always ignored) | ✅ | P3-17 |
| `addons/addon-search/src/SearchLineCache.ts`, `SearchEngine.ts`, `SearchState.ts` (+ tests) | `Presentation/Search/SearchLineCache.cs`, `SearchEngine.cs`, `SearchEngine.InLine.cs`, `SearchState.cs`, `SearchBufferView.cs` (adapter over `TerminalCore` replacing the public `Terminal` API), `SearchOptions.cs`, `SearchResult.cs` | ✅ | P3-18 |
| `addons/addon-search/src/SearchAddon.ts` (find, highlight, select, scroll), `SearchResultTracker.ts` | `Presentation/Search/SearchAddon.cs`, `SearchResultTracker.cs`, `SearchHighlightMap.cs` (decorations as cell ranges), `TerminalSession.FindNextAsync` / `FindPreviousAsync` / `ClearSearch` | ✅ | P3-19 |

## What is not ported

- `src/browser/` DOM code (renderers, DOM input, `Viewport` DOM, `CoreBrowserTerminal`): replaced by Presentation and
  the UI backends; only their logic is taken (cards say which lines).
- `src/common/public/*`, `TaskQueue`, `Platform`, `Clone`, `DecorationService`, `AddonManager`,
  `InstantiationService`, `ServiceRegistry`, `LogService` (ADR 0002, decision 9).
- Async parser handlers and their tests (ADR 0002, decision 1).
- `WriteBuffer.ts`: replaced by `TerminalWriter` (P1-56).
- Addons, except: `unicode11` (P1-22), `web-links` (P3-17), `search` (P3-18, P3-19), and from `webgl`:
  `CellColorResolver` (P3-08), `CursorBlinkStateManager` (P3-11), `CustomGlyphs` (P4-04).

## Behavior differences

| Area | xterm.js | LoomTerminal | Why |
|---|---|---|---|
| OSC and DCS handlers | `end` / `unhook` may return a `Promise`; the parser pauses and resumes | Handlers are synchronous (`bool`); `End` / `Unhook` return `void`, no stack state | ADR 0002 |
| Parser | `parse` may return a `Promise` and resume later | `Parse` is synchronous and returns `void`: no handler can pause the parser; `_parseStack` is not ported | ADR 0002 |
| Unicode version | `UnicodeService` starts on version 6; Unicode 11 comes from `addon-unicode11` | Unicode 11 widths are the default (`"6"` stays registered) | ADR 0002, decision 7 |
| Options | A mutable `options` object with getters/setters and `onOptionChange(key)` | `TerminalOptions` is an immutable record; `SetOptions` validates it all, then raises one `OnOptionChange(TerminalOption)` per changed property. UI options (font, theme, …) live in Presentation | ADR 0002, decision 4 |
| Buffer memory cleanup | `resize` schedules `_batchedMemoryCleanup` on an `IdleTaskQueue` | `Buffer.Resize` sets `MemoryCleanupPending`; the session's idle loop calls `BatchedMemoryCleanup()` (returns `true` while it must run again, like the JS task) | No timers in Core |
| Windows pty reflow | `windowsPty` with a `BuildNumber`: reflow is on for ConPTY ≥ 21376, off for older ConPTY and winpty | Same (`Buffer.IsReflowEnabled`); a supported Windows 11 ConPTY therefore reflows and the wrapping heuristics are off | Faithful port, recorded here because the P1-55 card first claimed the opposite |
| Write loop | `WriteBuffer` with a `setTimeout` chunking loop and `Promise` callbacks | `TerminalWriter`, a single synchronous work loop | ADR 0002 |
| CSS colors | `css.toColor` falls back to a canvas for named colors and other CSS syntaxes | Only `#rgb[a]`, `#rrggbb[aa]`, `rgb()` and `rgba()` are parsed; any other input throws `ArgumentException("css.toColor: Unsupported css format")`: CSS color names are not parsed | No canvas in Core |
| Selection | `selectionText` joins lines with CRLF when the platform is Windows | `SelectionService` takes `isWindows` (default: the running OS), the tests pass `false` as the node tests do | testability |
| Selection | `shouldForceSelection` honors the `macOptionClickForcesSelection` option on macOS | The option does not exist: on macOS Shift forces nothing and Alt is the column modifier | no such option in Core |
| Selection | Drag scrolling runs on a 50 ms `setInterval` while the button is down | `PointerInputHandler` scrolls on `Move` events, at most once per 50 ms (`TimeProvider`); the backend keeps sending moves while the button is held | no timers in Presentation |
| Search | The line cache is dropped after `LINES_CACHE_TIME_TO_LIVE` (15 s, timer); an invalid regex throws a `SyntaxError` | `SearchLineCache.EndSession()` drops it at the end of each search session (and, as upstream, on line feed, cursor move, resize); an invalid or too slow (1 s) regex finds nothing | no timers in Presentation (P3-18) |
| Search | `onWriteParsed` / `onResize` re-run the search after 200 ms (`_updateMatches`); highlights are `IDecoration` markers that follow the buffer | No timer: results and highlights are computed by `FindNextAsync` / `FindPreviousAsync` and not refreshed when output arrives or the terminal resizes (search again); highlights are painted by `RenderFrameBuilder` as `RunHighlight` plus a blend of the theme colors | no timers in Presentation (P3-19) |
| Custom glyphs | `drawPatternChar` fills a repeating pixel pattern for `░▒▓`; coordinates are rounded in device pixels; the powerline stroke is `fontSize / 12` | The cell is filled at 25 %, 50 % and 75 % opacity; coordinates are rounded to 0.5 DIP (crisp at 100 % scale only); the powerline stroke is `cellHeight / 14.4` (the frame carries no font size) | backend-neutral paths (P4-04) |
| Keyboard | `evaluateKeyboardEvent` handles `ev.key === 'Dead'` using the DOM `code` | Branch not ported: `TerminalKeyEvent` has no `code` | ADR 0004 |

## Tests not ported

| Suite | Reason |
|---|---|
| `ThemeService.test.ts`: jsdom and canvas mock in `beforeEach` | DOM only; the palette does not parse colors with a canvas |
| `OscParser - async tests` (`OscParser.test.ts`, lines 280–453) | Async handlers are not ported (ADR 0002) |
| `DcsParser - async tests` (`DcsParser.test.ts`, lines 284–459) | Async handlers are not ported (ADR 0002) |
| `OptionsService.test.ts`: `fontWeight` setOption tests, cols/rows `undefined` constructor test, `Object.keys` test | UI option, non-nullable ints, and a record instead of a getter/setter object |
| `EscapeSequenceParser - async` (`EscapeSequenceParser.test.ts`, lines 1744–2220) | Async handlers are not ported (ADR 0002) |
| `InputHandler - async handlers` (`InputHandler.test.ts`, lines 2353–2408) | Async handlers are not ported (ADR 0002) |
