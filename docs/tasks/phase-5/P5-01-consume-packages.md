# P5-01 — VIGO consumes the LoomTerminal packages

- **Status:** todo
- **Depends on:** P4-18
- **Size:** S

## Goal

VIGO references `LoomTerminal.WinUI` (and through it Presentation, Core, Pty) from a local package feed, builds and
publishes in AOT with it, without using it yet.

## Files to create or change (in `D:\VIGO`)

- `nuget.config` — add a local source `LoomTerminal` → `D:\LoomTerminal\artifacts\packages` (absolute path, with a
  comment explaining how to rebuild the packages: P4-18)
- package versions (central or per project, follow VIGO's current setup) — `LoomTerminal.WinUI` `0.1.0-alpha`
- `src/Vigo.App/Vigo.App.csproj` — `PackageReference` to `LoomTerminal.WinUI`
- `docs/DECISIONS.md` — new ADR "Native terminal control (LoomTerminal)": context (ADR-071 "second step"), decision,
  local feed until LoomTerminal is published, consequences

## Design decisions (already made — do not change)

- **Package reference, not project reference**: VIGO and LoomTerminal stay separate repositories with their own
  analyzers and settings; the local feed is replaced by nuget.org when LoomTerminal is published.
- The `buildTransitive` PRI workaround (ADR 0001 of LoomTerminal) applies automatically to VIGO through the package;
  check that VIGO does not already have an equivalent target that would now run twice.

## Tests

- [ ] `rtk dotnet build VIGO.slnx -p:Platform=x64` and the VIGO test projects are green.
- [ ] `/aot-check`: no new IL warning compared with `il-baseline.txt`; the AOT exe starts.

## Acceptance criteria

- [ ] VIGO builds and its tests pass; AOT check OK
- [ ] ADR written; commit message proposed (VIGO)
- [ ] This card's status and `D:\LoomTerminal\docs\tasks\README.md` updated

## Out of scope

Using the control (P5-03).

## Suggested commit message (VIGO)

`J<n>.<m>: reference the LoomTerminal packages`
