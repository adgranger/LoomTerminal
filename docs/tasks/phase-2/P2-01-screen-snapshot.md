# P2-01 — Screen snapshots of `TerminalCore`

- **Status:** todo
- **Depends on:** P1-57
- **Size:** M

## Goal

Produce the JSON screen snapshot described in [the phase 2 conventions](README.md) from a `TerminalCore`, and the
`.input` / `.vtrec` readers that replay a fixture into it.

## Files to create or change

- `tests/LoomTerminal.Core.Tests/Conformance/ScreenSnapshot.cs` — create (records + `static ScreenSnapshot Capture(TerminalCore core)`)
- `tests/LoomTerminal.Core.Tests/Conformance/ScreenSnapshotJson.cs` — create (`JsonSerializerContext`, source-generated)
- `tests/LoomTerminal.Core.Tests/Conformance/FixtureReader.cs` — create (`.input` escapes and directives, `.vtrec` events)
- `tests/LoomTerminal.Core.Tests/Conformance/ScreenSnapshotTests.cs` — create

## Design decisions (already made — do not change)

- Snapshots compare as **normalized JSON text** (same key order, same indentation), so a failing test prints a
  readable diff: `Assert.Equal(expectedJson, actualJson)`.
- The `.input` escape decoder is a small hand-written state machine (no regex) with exactly the escapes listed in
  the conventions; an unknown escape throws `FormatException` with the line number.
- Color and flag names are those of the conventions; `underline:<style>` uses `single`, `double`, `curly`,
  `dotted`, `dashed`.
- Use source-generated `System.Text.Json` only (no reflection-based serializer: banned by `src/BannedSymbols.txt`
  rules in spirit, even in tests).

## Tests

- [ ] New: snapshot of `hello\r\n\e[1;31mred\e[0m` has two lines, the second with one run `p16:1` + `bold`.
- [ ] New: a wide character produces one run covering two cells; `#!resize 40 10` in an `.input` file resizes.
- [ ] New: `.vtrec` with `O aGVsbG8=` writes `hello`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The Node oracle (P2-02).

## Suggested commit message

`test(core): add screen snapshots and fixture readers`
