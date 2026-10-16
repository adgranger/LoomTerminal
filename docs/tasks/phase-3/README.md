# Phase 3 conventions — read before any phase 3 card

Phase 3 builds `LoomTerminal.Presentation`: everything between the emulator and the pixels that does not depend on a
UI framework. A UI backend (WinUI in phase 4, Avalonia in phase 6) will only (1) forward input events as the types
defined here, (2) implement the backend interfaces (`ITextMeasurer`, `IRenderSurface`, `IClipboard`, …) and
(3) call `FrameRenderer.Render` once per frame. Read [ADR 0004](../../adr/0004-input-model.md) first.

## Upstream

Same pinned xterm.js commit as phase 1 (`f447274f430fd22513f6adbf9862d19524471c04`). Phase 3 ports from
`src/common/input/Keyboard.ts`, `src/browser/*` (only the logic, never the DOM code) and three addons
(`addon-search`, `addon-web-links`, `addon-webgl` for `CellColorResolver` and `CursorBlinkStateManager`). Headers,
TypeScript → C# map and JS traps: [phase 1 conventions](../phase-1/README.md).

## Folders and namespaces

| Folder | Namespace | Content |
|---|---|---|
| `Input/` | `LoomTerminal.Presentation.Input` | key and pointer events, key encoding, paste |
| `Session/` | `LoomTerminal.Presentation.Session` | `TerminalSession` (threads, pty, snapshots) |
| `Theming/` | `LoomTerminal.Presentation.Theming` | `TerminalTheme`, palette, color resolution |
| `Rendering/` | `LoomTerminal.Presentation.Rendering` | render model, backend interfaces, `FrameRenderer` |
| `Selection/` | `LoomTerminal.Presentation.Selection` | selection model and logic |
| `Links/` | `LoomTerminal.Presentation.Links` | URL detection, OSC 8 links |
| `Search/` | `LoomTerminal.Presentation.Search` | search engine |
| `Viewport/` | `LoomTerminal.Presentation.Viewport` | scrolling, wheel |

## Public API

Phase 3 defines the **first public API** of the project. Public types (and only these, unless a card says so):
`TerminalSession`, `TerminalSessionOptions`, `TerminalTheme`, `TerminalKeyEvent`, `TerminalKeyEventType`,
`TerminalPointerEvent` and its enums, `KeyPreviewEventArgs`, `ITextMeasurer`, `CellMetrics`, `IRenderSurface`,
`TextRunStyle`, `IClipboard`, `ILinkHandler`, `FrameRenderer`, `RenderFrame` and the types it exposes,
`SearchOptions`, `SearchResult`. Each public member needs XML doc and a threading note, and goes to
`PublicAPI.Unshipped.txt` (`./eng/Update-PublicApi.ps1 src/LoomTerminal.Presentation`).

## Threading model (ADR 0002, decision 5)

- The **parser thread** (owned by `TerminalSession`) is the only thread that touches `TerminalCore`.
- The **UI thread** never touches `TerminalCore`. It reads `RenderFrame` snapshots, and sends input through
  `TerminalSession` methods, which marshal to the parser thread through a `Channel<Action<TerminalCore>>`.
- One `Lock` in `TerminalSession` protects the latest `RenderFrame` reference. The parser thread builds a new frame
  after each batch of output (`TerminalWriter`'s `afterChunk`) or input action, then swaps it in under the lock.
- Every public type documents which thread may call it.

## Tests

`tests/LoomTerminal.Presentation.Tests` (created by P3-01). No UI. Rendering is tested with a
`RecordingRenderSurface` (P3-21) that records draw calls as text.
