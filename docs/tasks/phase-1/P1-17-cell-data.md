# P1-17 — `CellData`

- **Status:** done
- **Depends on:** P1-16
- **Size:** S

## Goal

Port the cell value object used to read and write buffer cells (content word + attributes + combined string).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/CellData.ts` | 1–94 | `CellData` (`fromCharData`, `isCombined`, `getWidth`, `getChars`, `getCode`, `setFromCharData`, `getAsCharData`) |
| `src/common/Types.ts` | 211–223 | `ICellData` (reference) |
| `src/common/buffer/BufferLine.test.ts` | 137–162 | `CellData` |

## Files to create or change

- `src/LoomTerminal.Core/Buffer/CellData.cs` — create
- `tests/LoomTerminal.Core.Tests/Buffer/CellDataTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class CellData : AttributeData` with public fields `Content` (`uint`) and `CombinedData`
  (`string`, default `""`), as in JS. `Extended` is re-created per cell as in JS (`new ExtendedAttrs()`).
- `IsCombined()` and `GetWidth()` return `uint`/`int` as JS does (numbers): `IsCombined` → `uint` (masked bits),
  `GetWidth` → `int`.

## Tests

- [x] Port `CellData` (all `it`s).

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (✅ P1-17)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`BufferLine` (P1-18).

## Suggested commit message

`feat(core): port CellData`
