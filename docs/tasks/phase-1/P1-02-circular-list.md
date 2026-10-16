# P1-02 — `CircularList<T>`

- **Status:** done
- **Depends on:** P1-01
- **Size:** S

## Goal

Port the ring buffer that stores the buffer lines (scrollback + viewport).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/CircularList.ts` | 1–241 | `IInsertEvent`, `IDeleteEvent`, `CircularList<T>` (all members) |
| `src/common/Types.ts` | 65–85 | `ICircularList<T>` (for reference only: do not port the interface) |
| `src/common/CircularList.test.ts` | 1–260 | all tests |

## Files to create or change

- `src/LoomTerminal.Core/Common/CircularList.cs` — create
- `tests/LoomTerminal.Core.Tests/Common/CircularListTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class CircularList<T> : Disposable where T : class`. Elements are reference types
  (`BufferLine`): the backing array is `T?[]` and `Get` returns `T?` (JS `undefined` → `null`).
- Events with `Emitter` (P1-01): `OnDeleteEmitter`/`OnInsertEmitter`/`OnTrimEmitter` exposed as
  `IEvent<DeleteEvent> OnDelete`, `IEvent<InsertEvent> OnInsert`, `IEvent<int> OnTrim`.
  `DeleteEvent` and `InsertEvent` are `internal readonly record struct`s with `Index` and `Amount`.
- `splice(start, deleteCount, ...items)` → `void Splice(int start, int deleteCount, params ReadOnlySpan<T> items)`.
- `MaxLength` setter reallocates exactly as xterm.js does (new array, copy in order, `_startIndex = 0`).
- `Length` setter: same semantics as xterm.js (growing fills with `null`/keeps old slots, shrinking only moves the length).

## Tests

- [x] Port every test of `CircularList.test.ts`: `push`, `maxLength`, `length`, `splice`, `trimStart`, `shiftElements`.
      The JS tests use strings as elements: use `string` as `T` in the tests.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `src/common/CircularList.ts` → `Core/Common/CircularList.cs` (✅ P1-02)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Anything else in `src/common`.

## Suggested commit message

`feat(core): port CircularList`
