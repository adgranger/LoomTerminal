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
| `src/common/buffer/Buffer.ts` (structure, tab stops, markers), `buffer/Types.ts` (`IBuffer`), `Services.ts` (`IBufferService`: `Cols`, `Rows`) | `Core/Buffer/Buffer.cs`, `IBuffer.cs`, `Core/Services/IBufferService.cs` | ✅ (`IBufferService` completed by P1-33) | P1-29 … P1-32 |
| `src/common/buffer/Marker.ts`, `BufferRange.ts`, `BufferReflow.ts` (+ `BufferRange` and `BufferReflow` tests), `Types.ts` (`IMarker`) | `Core/Buffer/Marker.cs`, `IMarker.cs`, `BufferRange.cs`, `BufferReflow.cs` | ✅ | P1-28 |

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
| CSS colors | `css.toColor` falls back to a canvas for named colors and other CSS syntaxes | Only `#rgb[a]`, `#rrggbb[aa]`, `rgb()` and `rgba()` are parsed; any other input throws `ArgumentException("css.toColor: Unsupported css format")`: CSS color names are not parsed | No canvas in Core |

## Tests not ported

| Suite | Reason |
|---|---|
| `OscParser - async tests` (`OscParser.test.ts`, lines 280–453) | Async handlers are not ported (ADR 0002) |
| `DcsParser - async tests` (`DcsParser.test.ts`, lines 284–459) | Async handlers are not ported (ADR 0002) |
| `OptionsService.test.ts`: `fontWeight` setOption tests, cols/rows `undefined` constructor test, `Object.keys` test | UI option, non-nullable ints, and a record instead of a getter/setter object |
| `EscapeSequenceParser - async` (`EscapeSequenceParser.test.ts`, lines 1744–2220) | Async handlers are not ported (ADR 0002) |
