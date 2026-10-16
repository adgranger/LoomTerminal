# P3-01 — Presentation test project and public API baseline

- **Status:** todo
- **Depends on:** P2-07
- **Size:** S

## Goal

Create `tests/LoomTerminal.Presentation.Tests`, allow Presentation to reference `LoomTerminal.Pty` (the session
starts shells), and update the architecture rules.

## Files to create or change

- `tests/LoomTerminal.Presentation.Tests/LoomTerminal.Presentation.Tests.csproj` — create (same shape as the Core
  tests; references `src/LoomTerminal.Presentation`); add to `LoomTerminal.slnx`
- `src/LoomTerminal.Presentation/LoomTerminal.Presentation.csproj` — add a `ProjectReference` to `LoomTerminal.Pty`
- `tests/LoomTerminal.Architecture.Tests/DependencyRulesTests.cs` — `["LoomTerminal.Presentation"] = ["LoomTerminal.Core", "LoomTerminal.Pty"]`
- `docs/ARCHITECTURE.md` — update the dependency table and diagram
- `docs/adr/0005-presentation-references-pty.md` — create (short: why the session owns the pty, decision, consequence)
- `tests/LoomTerminal.Presentation.Tests/SmokeTests.cs` — create

## Design decisions (already made — do not change)

- **Presentation references Pty** so `TerminalSession` can start a shell itself (`TerminalSession.Start(PtyStartOptions)`)
  and also attach to any `IPtyConnection`. WinUI keeps its own reference to Pty only if it uses it directly; it
  should not need to after this card (leave the reference, P4 cards decide).
- `InternalsVisibleTo LoomTerminal.Presentation.Tests` comes from `src/Directory.Build.props` already.

## Tests

- [ ] New: smoke test that creates a `TerminalCore` through the internal API (proves `InternalsVisibleTo` from Core
      to Presentation works from Presentation code — add a tiny internal helper in Presentation for it).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green (architecture tests included)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Any feature.

## Suggested commit message

`chore(presentation): add the test project and let Presentation use the pty`
