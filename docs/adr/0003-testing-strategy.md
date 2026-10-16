# ADR 0003: Testing strategy

- Status: accepted (2026-10-16)
- Date: 2026-10-16

## Decisions

1. **Ported unit tests** (phase 1): every `*.test.ts` of the ported xterm.js files is ported, except the async
   suites (ADR 0002). They are the first line of defense and are written before the code.
2. **Differential tests against xterm.js itself** (phase 2): `tools/xterm-oracle` is a Node.js script that runs
   `@xterm/headless` **6.0.0** (same version as the port) on an input file and writes the resulting screen (text,
   attributes, cursor, modes) as JSON. The C# side replays the same input through `TerminalCore` and compares.
   Fixtures are committed (input + expected JSON), so CI does not need Node.js; Node is only needed to regenerate
   them.
3. **Recorded real sessions** (phase 2): `LoomTerminal.PtyConsole --record <file>` saves the raw bytes a real shell
   sends through ConPTY (pwsh, git log, vim and htop under WSL…). They become differential fixtures.
4. **Presentation** (phase 3): plain unit tests, no UI; renderers are tested through a recording surface.
5. **UI backends** (phases 4, 6): manual checklists in the gallery plus CI Native AOT smoke tests; the parity list
   of `docs/ROADMAP.md` is checked in VIGO (phase 5).
6. **Benchmarks** (phase 2): BenchmarkDotNet in `tests/LoomTerminal.Benchmarks`, numbers written to
   `docs/performance.md`. No optimization lands without a before/after number.
