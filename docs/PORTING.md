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
| OSC handlers | `end` may return a `Promise`; the parser pauses and resumes | Handlers are synchronous (`bool`); `End` returns `void`, no stack state | ADR 0002 |

## Tests not ported

| Suite | Reason |
|---|---|
| `OscParser - async tests` (`OscParser.test.ts`, lines 280–453) | Async handlers are not ported (ADR 0002) |
