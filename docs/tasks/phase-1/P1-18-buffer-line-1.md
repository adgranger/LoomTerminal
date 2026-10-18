# P1-18 — `BufferLine` (1/2): storage and cell access

- **Status:** done
- **Depends on:** P1-17
- **Size:** M

## Goal

Port the packed line storage (3 `uint` per cell: content, fg, bg), combined strings and extended attributes maps,
and every read/write accessor of a single cell.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/BufferLine.ts` | 1–60 | `CELL_SIZE`, `Cell` enum, `DEFAULT_ATTR_DATA` (already in P1-16), `w`/`CLEANUP_THRESHOLD` |
| | 61–255 | constructor, `get`, `set`, `getWidth`, `hasWidth`, `getFg`, `getBg`, `hasContent`, `getCodePoint`, `isCombined`, `getString`, `isProtected`, `loadCell`, `setCell`, `setCellFromCodepoint`, `addCodepointToCell` |
| `src/common/Types.ts` | 225–255 | `IBufferLine` (port as an interface: `Buffer` and tests use it) |
| `src/common/buffer/BufferLine.test.ts` | 163–277 | `BufferLine` top-level tests (before `resize`) |

## Files to create or change

- `src/LoomTerminal.Core/Buffer/IBufferLine.cs` — create (all members; the ones of P1-19 can be declared now and
  implemented as `throw new NotImplementedException()` until P1-19)
- `src/LoomTerminal.Core/Buffer/BufferLine.cs` — create (part 1)
- `tests/LoomTerminal.Core.Tests/Buffer/BufferLineTests.cs` — create

## Design decisions (already made — do not change)

- `_data` is a `uint[]` of `cols * 3`. `_combined` is `Dictionary<int, string>`, `_extendedAttrs` is
  `Dictionary<int, ExtendedAttrs>` (JS objects keyed by column).
- `Length` (JS `length`) is a public property; `IsWrapped` a public field/property as in JS.
- `get(index)` returns `CharData` (P1-15); `set(index, CharData)`.
- Keep the JS behavior of `getString` for combined and empty cells exactly (empty string for a null cell).

## Tests

- [x] Port `BufferLine` tests from line 163 to 277 (before `describe('resize')`): `ctor` here; `insertCells`, `deleteCells`, `replaceCells`, `fill`, `clone`, `copyFrom` and the combining chars test need the P1-19 methods and are ported there.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `buffer/BufferLine.ts` (🔶 P1-18)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Line editing, resize, strings (P1-19).

## Suggested commit message

`feat(core): port BufferLine storage and cell access`
