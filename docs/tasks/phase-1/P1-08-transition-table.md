# P1-08 — `TransitionTable` and the VT500 transition table

- **Status:** todo
- **Depends on:** P1-05
- **Size:** S

## Goal

Port the table-driven state machine data (Paul Williams' DEC VT500 parser, as adapted by xterm.js).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/parser/EscapeSequenceParser.ts` | 19–228 | `TableAccess`, `TransitionTable` (`setDefault`, `add`, `addMany`), `NON_ASCII_PRINTABLE`, `VT500_TRANSITION_TABLE` (the whole IIFE, including its local helpers `r`, `PRINTABLES`, `EXECUTABLES`) |

## Files to create or change

- `src/LoomTerminal.Core/Parser/TransitionTable.cs` — create (class + `internal static class Vt500TransitionTable { public static TransitionTable Instance { get; } = Create(); }`)
- `tests/LoomTerminal.Core.Tests/Parser/TransitionTableTests.cs` — create

## Design decisions (already made — do not change)

- `Table` is a `byte[]` whose length is the one the JS code passes to `new TransitionTable(...)`. Keep the exact
  packing of `TableAccess` (`TRANSITION_ACTION_SHIFT = 4`, `TRANSITION_STATE_MASK = 15`, `INDEX_STATE_SHIFT = 8`:
  index `state << 8 | code`, value `action << 4 | next`), so the parse loop (P1-10) is a literal port.
- The JS helper `r(start, end)` (range) becomes `static int[] Range(int start, int end)`.
- The table is built once (static, lazy-free: `static readonly`), and shared by every parser.

## Tests

- [ ] New (xterm.js tests this table only through the parser): check a handful of entries against the JS source:
      `GROUND` + `0x1b` → `ESCAPE` / `CLEAR`; `GROUND` + `0x41` → `GROUND` / `PRINT`; `ESCAPE` + `0x5b` → `CSI_ENTRY`
      / `CLEAR`; `CSI_ENTRY` + `0x3f` → `CSI_PARAM` / `COLLECT`; `CSI_PARAM` + `0x6d` → `GROUND` / `CSI_DISPATCH`;
      `OSC_STRING` + `0x07` → `GROUND` / `OSC_END`; any state + `0x18` → `GROUND` / `EXECUTE`;
      `GROUND` + `0xA0` (NON_ASCII_PRINTABLE) → `GROUND` / `PRINT`.
      Read each expected value from the JS table code, do not guess.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `parser/EscapeSequenceParser.ts` (🔶 in progress, P1-08)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The `EscapeSequenceParser` class (P1-09, P1-10).

## Suggested commit message

`feat(core): port the VT500 transition table`
