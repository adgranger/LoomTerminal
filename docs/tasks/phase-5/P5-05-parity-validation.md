# P5-05 — Parity validation and close phase 5

- **Status:** todo
- **Depends on:** P5-04
- **Size:** S

## Goal

Check every item of the parity list (`docs/ROADMAP.md`, "Feature parity with VIGO's terminal") in VIGO's AOT exe, and
record the result in both repositories.

## Files to create or change

- `D:\LoomTerminal\docs\ROADMAP.md` — tick the parity list; phase 5 ✅
- `D:\VIGO\docs\PROGRESS.md` — milestone done, with the VIGO handoff rules
- `D:\LoomTerminal\docs\performance.md` — VIGO before/after numbers (P4-17 scenarios, now inside VIGO)

## Design decisions (already made — do not change)

- An item that fails becomes a LoomTerminal card (if the control is at fault) or a VIGO task (if the integration is),
  listed in `docs/tasks/README.md`; the phase closes only when the list is all green.

## Tests

- [ ] Every parity item checked by hand or UIA replay in the AOT exe.

## Acceptance criteria

- [ ] Parity list all green
- [ ] Commit messages proposed (both repositories)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`docs: close phase 5 (VIGO uses LoomTerminal)`
