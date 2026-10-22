# Architecture

## Projects and dependencies

```
LoomTerminal.WinUI ──► LoomTerminal.Presentation ──► LoomTerminal.Core
        │                        │
        └────────────────────────┴──► LoomTerminal.Pty
(later) LoomTerminal.Avalonia ──► Presentation, Pty
```

| Project | TFM | May reference | Role |
|---|---|---|---|
| `LoomTerminal.Core` | `$(LoomNetTargetFramework)` | nothing | Emulator: bytes in, screen state out. Headless. |
| `LoomTerminal.Pty` | `$(LoomNetTargetFramework)` | nothing | Pseudo terminal and shell detection. OS code in `Windows/` (later `Unix/`). |
| `LoomTerminal.Presentation` | `$(LoomNetTargetFramework)` | Core, Pty | Render model, input encoding, selection, search, backend interfaces. |
| `LoomTerminal.WinUI` | `$(LoomWindowsTargetFramework)` | Presentation, Pty | XAML control, Win2D renderer, WinUI input, UIA. |

These rules are **checked by tests** (`tests/LoomTerminal.Architecture.Tests`): allowed references, plain `net11.0`
TFM and no UI package for the three portable projects, no Windows API in their sources (except `Pty/Windows/`).
Changing the table above means changing the architecture: write an ADR.

## Why a Presentation layer

Everything that is not "draw this" or "this key was pressed" lives in Presentation, so the second backend (Avalonia)
costs one renderer and one input adapter. In particular:

- **Render model.** Core exposes cells; Presentation turns the visible rows into runs of same-attribute cells,
  tracks which rows changed since the last frame, and adds cursor, selection, links and search highlights.
  A backend draws runs; it never reads the buffer.
- **Own input model.** Key and modifier enums defined here; each backend maps `VirtualKey` / Avalonia `Key` to them.
  Encoding to VT sequences (application cursor keys, Alt as Escape, function keys, mouse protocols, bracketed paste)
  is shared, so both backends type the same bytes.
- **Measurement is the backend's job.** `ITextMeasurer` (cell size, baseline, glyph width, font fallback) is
  implemented per backend: DirectWrite and Skia measure differently.

## Data flow and threading

```
shell ─ConPTY─► IPtyConnection.Output (bounded channel, backpressure to the shell)
              ─► parser thread: Core parses, updates buffers, marks rows dirty
              ─► parser thread: after each batch, Presentation builds an immutable RenderFrame (swapped under a lock)
              ─► UI thread, once per frame: TryTakeFrame ─► FrameRenderer.Render ─► backend IRenderSurface draws
keys/mouse ─► backend ─► Presentation encodes ─► IPtyConnection.Write (never blocks)
```

Parsing never runs on the UI thread, and the UI never waits for the parser: it renders the latest snapshot.
This replaces VIGO's WebView2 bridge (JSON messages, ack-based flow control): backpressure is now the bounded
output channel alone.

## Native AOT rules

1. Every library has `IsAotCompatible=true` (trim and AOT analyzers on). Warnings are errors in Release and on CI.
2. No reflection-based serialization: `System.Text.Json` with a source-generated context only.
3. No `Activator`, no `Type.GetType(string)`, no assembly scanning: banned by `src/BannedSymbols.txt`.
4. Native calls with source-generated `[LibraryImport]` only, never `[DllImport]`.
5. CI publishes `LoomTerminal.Gallery` (WinUI) and `LoomTerminal.PtyConsole` with `PublishAot=true` and fails on any
   IL/AOT warning, then starts them.

## Public API

Each `src/` project tracks its public surface in `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`
(`./eng/Update-PublicApi.ps1 src/<Project>` after adding public members). `internal` by default.
