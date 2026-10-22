# P4-18 — Close phase 4

- **Status:** done
- **Depends on:** all P4 cards (follow-up optimization cards may stay open)
- **Size:** S (docs)

## Goal

Document the control, its public API and known limitations; release `0.1.0-alpha` packages locally for VIGO.

## Files to create or change

- `src/LoomTerminal.WinUI/README.md` — create (usage: XAML, creating a session, theme, events, AOT notes, the PRI
  workaround of `buildTransitive`)
- `README.md` — status, screenshot (`docs/images/gallery.png`)
- `docs/ROADMAP.md` — phase 4 ✅
- `docs/adr/0001-baseline.md` — "Known issues" still accurate?

## Design decisions (already made — do not change)

- Packages: `rtk dotnet pack src/<Project> -c Release -o artifacts/packages` for the four projects; they are **not**
  published to nuget.org (that is the user's decision, later).

## Tests

- [ ] `docs/manual-tests.md` fully ticked on the AOT gallery. **PENDING: needs a human on the AOT gallery (not ticked, no result invented).**

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Four `.nupkg` in `artifacts/packages`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`docs: close phase 4`
