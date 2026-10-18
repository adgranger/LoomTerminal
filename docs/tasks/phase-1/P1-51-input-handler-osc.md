# P1-51 — `InputHandler.Osc`: titles, colors (4/10/11/12/104/110–112), hyperlinks (8)

- **Status:** done
- **Depends on:** P1-48, P1-26, P1-27, P1-34
- **Size:** M

## Goal

Port the OSC handlers: window title and icon name, palette and special color set/query/restore (reported as color
events for the UI), and OSC 8 hyperlinks.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 2941–2964 | `setTitle`, `setIconName` |
| | 2965–3007 | `setOrReportIndexedColor` |
| | 3008–3057 | `setHyperlink`, `_createHyperlink`, `_finishHyperlink` |
| | 3058–3185 | `_setOrReportSpecialColor`, `setOrReportFgColor`, `setOrReportBgColor`, `setOrReportCursorColor`, `restoreIndexedColor`, `restoreFgColor`, `restoreBgColor`, `restoreCursorColor` |
| | 3493–3495 | `isValidColorIndex` |
| | constructor | OSC `0 1 2 4 8 10 11 12 104 110 111 112` |
| `src/common/Types.ts` | 413–450 | `ColorRequestType`, `ColorIndex`, `SpecialColorIndex`, `IColorReportRequest`, `IColorSetRequest`, `IColorRestoreRequest`, `IColorEvent` |
| `src/common/InputHandler.test.ts` | 1981–2149 | `OSC` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/ColorEvents.cs` — create (types from `Types.ts` 413–450)
- `src/LoomTerminal.Core/InputHandling/InputHandler.Osc.cs` — create (with `RegisterOscHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerOscTests.cs` — create
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerSgrTests.cs` — remove the skip set by P1-48

## Design decisions (already made — do not change)

- See P1-36. `IColorEvent` (a union array) → `IReadOnlyList<ColorRequest>` where `ColorRequest` is an abstract record
  with three sealed records `ColorReportRequest(int Index)`, `ColorSetRequest(int Index, (int R, int G, int B) Color)`,
  `ColorRestoreRequest(int? Index)`. `SpecialColorIndex` (`FOREGROUND = 256`, `BACKGROUND`, `CURSOR`) keeps its values.
- `OnColor` (declared in P1-36) is `IEvent<IReadOnlyList<ColorRequest>>`.

## Already done by P1-50

`InputHandler.Osc.cs` exists with `SetTitle`, `SetIconName` and `RegisterOscHandlers` (OSC 0, 1, 2): extend it, do
not recreate it.

## Tests

- [x] Port `OSC` (all `it`s).
- [x] Remove the skip of `reset text attributes (SGR 0)` › `resets all attributes except for the url` (P1-48) and make it pass.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green, no skip left
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Answering color queries with the theme (the UI does it from `OnColor`, phase 3).

## Suggested commit message

`feat(core): port the OSC handlers (titles, colors, hyperlinks)`
