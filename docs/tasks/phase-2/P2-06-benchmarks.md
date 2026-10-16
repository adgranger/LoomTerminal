# P2-06 — Benchmarks and reference numbers

- **Status:** todo
- **Depends on:** P2-05
- **Size:** S

## Goal

Measure the emulator's throughput on representative inputs and write the reference numbers, before any optimization.

## Files to create or change

- `Directory.Packages.props` — add `BenchmarkDotNet` (same version as LoomEditor: `0.16.0-preview.2`)
- `tests/LoomTerminal.Benchmarks/LoomTerminal.Benchmarks.csproj`, `Program.cs` — create (copy LoomEditor's
  `tests/LoomEditor.Benchmarks` shape); add to `LoomTerminal.slnx`
- `tests/LoomTerminal.Benchmarks/ParseBenchmarks.cs` — create
- `docs/performance.md` — create (machine, date, commit, table of results, how to run)

## Design decisions (already made — do not change)

- Benchmarks (all through `TerminalCore.Write(byte[])`, 80×24 then 200×50):
  `PlainText_10MB` (ASCII lines of 79 chars + CRLF), `ColoredText_10MB` (SGR 256-color every 8 chars),
  `Yes_10MB` (`y\r\n` repeated), `CursorHeavy_1MB` (CUP + text, like a TUI redraw), `UnicodeWide_1MB` (CJK + emoji),
  `RecordedGitLog` (the `git-log-graph.vtrec` output replayed 100×).
- Report MB/s and allocations (`[MemoryDiagnoser]`). Run: `rtk dotnet run -c Release --project tests/LoomTerminal.Benchmarks -- --filter *`.
- **No optimization in this card.** If a number is surprisingly bad (< 20 MB/s on `PlainText`), open a follow-up
  card in `docs/tasks/README.md` with the profile, do not fix it here.

## Tests

—

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green (the benchmark project is not a test project: check it is not picked up by `dotnet test`, like LoomEditor's)
- [ ] `docs/performance.md` filled
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Optimizations.

## Suggested commit message

`perf(core): add parse benchmarks and reference numbers`
