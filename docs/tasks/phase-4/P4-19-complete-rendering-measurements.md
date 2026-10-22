# P4-19 — Complete the rendering measurements

- **Status:** todo
- **Depends on:** P4-17
- **Size:** S (measurement, no code)

## Goal

Finish what P4-17 started: record the missing numbers in `docs/performance.md`, judge them against the targets and open
optimization cards for every miss.

## Files to create or change

- `docs/performance.md` — fill the "Rendering" table
- `docs/tasks/README.md` — follow-up cards for missed targets

## Design decisions (already made — do not change)

- Same scenarios, sizes, scales and targets as P4-17 (`docs/performance.md`, "Rendering"). Known so far: `stress.ps1`
  (10 MB) wrote its output in 1.2 s on the script side at 128×38; this is not the time until the screen settles.
- Needs a human at the keyboard with the AOT gallery and VIGO's terminal; a coding agent cannot take these readings.

## Tests

—

## Acceptance criteria

- [ ] Every scenario recorded for LoomTerminal and VIGO: finish time until the screen settles, CPU %, p95 frame, working set
- [ ] Follow-up cards created for every missed target
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Optimizations.
