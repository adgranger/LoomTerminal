# P1-44 — `InputHandler.Esc`: DECSC/DECRC, IND, NEL, RI, keypad modes, charsets, RIS, DECALN

- **Status:** done
- **Depends on:** P1-38, P1-25
- **Size:** M

## Goal

Port the ESC-sequence handlers and the C1 execute handlers IND and NEL: save/restore cursor, index / reverse index /
next line, keypad application/numeric mode, character set selection (G0–G3, locking shifts), full reset, screen
alignment pattern.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 2898–2940 | `saveCursor`, `restoreCursor` |
| | 3186–3287 | `nextLine`, `keypadApplicationMode`, `keypadNumericMode`, `selectDefaultCharset`, `selectCharset`, `index` |
| | 3302–3337 | `reverseIndex`, `fullReset`, `reset` |
| | 3354–3415 | `setgLevel`, `screenAlignmentPattern` |
| | constructor | execute `C1.IND`, `C1.NEL`; ESC `7 8 D E M = > c n o \| } ~`, `%@`, `%G`, the charset loop over `CHARSETS` (`( ) * + - . /`), `#8`; CSI `s`, `u` |
| `src/common/InputHandler.test.ts` | 182–201 | `save and restore cursor` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.Esc.cs` — create (with `RegisterEscHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerEscTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. CSI `s` (SCOSC) and CSI `u` (SCORC) call `saveCursor(params)` / `restoreCursor(params)`: register them
  here, the methods take an optional `IParams?`.
- `fullReset` fires `OnRequestReset`; the actual reset of the terminal is done by `TerminalCore` (P1-55), as in JS.
- `GLEVEL` (JS map `'(' → 0`…) becomes a `static readonly Dictionary<char, int>`.

## Tests

- [x] Port `save and restore cursor`.
- [x] New: `ESC ( 0` then `q` prints `─`; `ESC ( B` returns to ASCII; `ESC M` at the top of the scroll region
      scrolls down; `ESC # 8` fills the screen with `E`; `ESC =` sets `ApplicationKeypad`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

HTS / ESC `H` (P1-40).

## Suggested commit message

`feat(core): port the InputHandler ESC handlers`
