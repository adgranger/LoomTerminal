# P1-33 — `BufferSet` and `BufferService`

- **Status:** done
- **Depends on:** P1-32
- **Size:** M

## Goal

Port the normal/alternate buffer pair and the service that owns it (resize, scroll, scrollLines, reset), and
complete `IBufferService` and `MockBufferService`.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/BufferSet.ts` | 1–134 | `BufferSet` (`reset`, `alt`, `active`, `normal`, `activateNormalBuffer`, `activateAltBuffer`, `resize`, `setupTabStops`, `onBufferActivate`) |
| `src/common/buffer/Types.ts` | 40–52 | `IBufferSet` (reference) |
| `src/common/services/BufferService.ts` | 1–154 | `MINIMUM_COLS`, `MINIMUM_ROWS`, `BufferService` |
| `src/common/services/Services.ts` | 13–35 | the rest of `IBufferService`, `IBufferResizeEvent` |
| `src/common/TestUtils.test.ts` | 17–60 | the rest of `MockBufferService` |
| `src/common/buffer/BufferSet.test.ts` | 1–84 | all |

## Files to create or change

- `src/LoomTerminal.Core/Buffer/BufferSet.cs` — create
- `src/LoomTerminal.Core/Services/IBufferService.cs` — complete (`Buffer`, `Buffers`, `IsUserScrolling`, `OnResize`,
  `OnScroll`, `Scroll`, `ScrollLines`, `Resize`, `Reset`)
- `src/LoomTerminal.Core/Services/BufferService.cs` — create
- `tests/LoomTerminal.Core.Tests/TestUtils/MockBufferService.cs` — complete (creates a real `BufferSet`, as in JS)
- `tests/LoomTerminal.Core.Tests/Buffer/BufferSetTests.cs` — create
- `tests/LoomTerminal.Core.Tests/Services/BufferServiceTests.cs` — create

## Design decisions (already made — do not change)

- `IBufferService.Buffers` is typed `BufferSet` (no `IBufferSet` interface). `Buffer` is the active `Buffer`.
- `OnBufferActivate` payload → `readonly record struct BufferActivateEvent(Buffer ActiveBuffer, Buffer InactiveBuffer)`.
- `IBufferResizeEvent` → `readonly record struct BufferResizeEvent(int Cols, int Rows, bool ColsChanged)` (check the
  JS fields and copy them).
- Scroll events: `OnScroll` is `IEvent<int>` (the new `ydisp`).

## Tests

- [x] Port `BufferSet` tests (all).
- [x] New: `BufferService.Resize` clamps to `MINIMUM_COLS` / `MINIMUM_ROWS`; `ScrollLines(-1)` at the top does not
      go negative; `Scroll` with a full scrollback trims (`lines.isFull` path) and keeps `ybase` stable.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (✅ P1-33)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`CoreService`, `OscLinkService` (P1-34).

## Suggested commit message

`feat(core): port BufferSet and BufferService`
