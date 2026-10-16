# P1-29 — `Buffer` (1/3): structure, cells, tab stops, markers, strings

- **Status:** todo
- **Depends on:** P1-28, P1-23, P1-24, P1-25
- **Size:** M

## Goal

Port `Buffer` without resize and reflow: lines, cursor and scroll region fields, null/whitespace cells, blank lines,
`fillViewportRows`, `clear`, tab stops, `translateBufferLineToString`, `getWrappedRangeForLine`, markers.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/Buffer.ts` | 1–149 | `MAX_BUFFER_SIZE`, fields, constructor, `getNullCell`, `getWhitespaceCell`, `getBlankLine`, `hasScrollback`, `isCursorInViewport`, `_getCorrectBufferLength`, `fillViewportRows`, `clear` |
| | 535–662 | `translateBufferLineToString`, `getWrappedRangeForLine`, `setupTabStops`, `prevStop`, `nextStop`, `clearMarkers`, `clearAllMarkers`, `addMarker`, `_removeMarker` |
| `src/common/buffer/Types.ts` | 1–39 | `BufferIndex`, `IBuffer` (port as an interface) |
| `src/common/services/Services.ts` | 13–28 | `IBufferService` — **only `Cols` and `Rows` in this card** (see design decisions) |
| `src/common/TestUtils.test.ts` | 17–60 | `MockBufferService` — **only `Cols`, `Rows`, `Resize` in this card** |
| `src/common/buffer/Buffer.test.ts` | 1–109 | setup, `constructor`, `fillViewportRows`, `getWrappedRangeForLine` |
| | 1069–1181 | `addMarker` |

## Files to create or change

- `src/LoomTerminal.Core/Buffer/IBuffer.cs` — create (declare the members of P1-30/P1-31 too; implement them as
  `throw new NotImplementedException()` until then)
- `src/LoomTerminal.Core/Buffer/Buffer.cs` — create (part 1)
- `src/LoomTerminal.Core/Services/IBufferService.cs` — create with `int Cols { get; }` and `int Rows { get; }` only
- `tests/LoomTerminal.Core.Tests/TestUtils/MockBufferService.cs` — create with `Cols`, `Rows`, `Resize` only
- `tests/LoomTerminal.Core.Tests/Buffer/BufferTests.cs` — create

## Design decisions (already made — do not change)

- **Why a partial `IBufferService` now:** `Buffer` only reads `cols` and `rows` from it (`Buffer.ts` lines 53, 54,
  88), while the full service needs `BufferSet`, which needs `Buffer`. P1-33 adds the other members. This breaks the
  cycle without changing the code that is ported.
- `Lines` is a `CircularList<IBufferLine>` (public field). `X`, `Y`, `YDisp`, `YBase`, `ScrollTop`, `ScrollBottom`,
  `SavedX`, `SavedY`, `SavedCurAttrData`, `SavedCharset`, `Markers` are public fields named like JS in PascalCase.
- `tabs` (`{ [column: number]: boolean | undefined }`) → `Dictionary<int, bool>`; a missing key means `undefined`.
- `translateBufferLineToString(lineIndex, trimRight, startCol = 0, endCol?)` → `endCol = -1` means "undefined".

## Tests

- [ ] Port `Buffer` › `constructor`, `fillViewportRows`, `getWrappedRangeForLine` (lines 18–109).
- [ ] Port `Buffer` › `addMarker` (lines 1069–1181).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `buffer/Buffer.ts` (🔶 P1-29)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`resize`, memory cleanup, reflow (P1-30, P1-31).

## Suggested commit message

`feat(core): port Buffer structure, tab stops and markers`
