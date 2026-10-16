# ADR 0002: Design of LoomTerminal.Core

- Status: accepted (2026-10-16)
- Date: 2026-10-16

## Context

`LoomTerminal.Core` is a port of xterm.js `src/common` (commit `f447274f`, tag 6.0.0). xterm.js runs in a browser:
one thread, promises, a dependency-injection container, DOM events, `setTimeout`-driven write batching. Some of
that does not fit a .NET control that parses on a background thread and renders on the UI thread.

## Decisions

1. **Synchronous handlers only.** Parser handlers return `bool`; `Promise<boolean>` results, `_preserveStack`,
   `promiseResult` and the parse stack are not ported. VIGO and every known use run synchronous handlers; async
   handlers are an embedding API for browser addons (image decoding) we do not need. Revisit with an ADR if a
   feature requires it.
2. **No dependency-injection container.** xterm.js `InstantiationService` and `createDecorator` are not ported.
   Services are plain classes created and wired by hand in `TerminalCore` (the port of `CoreTerminal`).
   Service interfaces (`IBufferService`, `IOptionsService`…) are ported only when tests need a mock of them.
3. **Events**: `Common/Emitter.cs` mirrors xterm.js `Emitter<T>`: `Fire(T)`, and `Event` (an `IEvent<T>`) whose
   `Subscribe(Action<T>)` returns an `IDisposable`. No `System.EventHandler` in Core: it keeps the ported
   `this._register(x.onFoo(...))` code one-to-one.
4. **Options without reflection.** `TerminalOptions` is a sealed record of init-only properties (the Core subset of
   xterm.js `ITerminalOptions`). `OptionsService` holds the current value and raises `OptionChanged` with a
   `TerminalOption` enum value naming the changed option. No string keys, no reflection (Native AOT).
5. **Threading: Core is single-threaded.** No locks inside Core. `LoomTerminal.Presentation.TerminalSession` (P3)
   owns a parser thread that is the only caller of Core, and hands immutable render snapshots to the UI thread
   under one lock taken per frame and per parsed chunk.
6. **No `WriteBuffer` port.** Its role (batching writes, yielding to the browser every 12 ms, discard watermark) is
   replaced by `TerminalWriter` (P1-56): it consumes `IPtyConnection.Output` chunks on the parser thread, decodes
   UTF-8 statefully (`Utf8ToUtf32`) and calls `InputHandler.Parse`. Backpressure comes from the bounded channel.
7. **Unicode**: `UnicodeV6` and `UnicodeV11` (from `addon-unicode11`) are both ported; **V11 is the default**, as
   in VIGO. The grapheme addon (`addon-unicode-graphemes`) is out of scope for phase 1.
8. **Windows PTY heuristics are ported** (`windowsPty` option, `WindowsMode.ts`, the ConPTY reflow rules of
   `CoreTerminal`): LoomTerminal's first backend is ConPTY.
9. **Not ported from `src/common`**: `public/*` (xterm.js public API wrappers), `TaskQueue`, `Platform`, `Clone`,
   `DecorationService` (decorations come with search highlights in phase 3, through Presentation), `AddonManager`,
   `InstantiationService`, `ServiceRegistry`, `LogService` beyond a minimal `ILogService` with a null implementation.

## Consequences

- The ported test suites drop their `async` variants (listed per card and in `docs/PORTING.md`).
- An embedder cannot register async OSC/DCS handlers. Sync handlers registration stays available internally.
