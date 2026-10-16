# P4-17 — Rendering performance: measure, then fix with numbers

- **Status:** todo
- **Depends on:** P4-16
- **Size:** S (measurement) — fixes go to follow-up cards

## Goal

Measure frame time and throughput in the AOT gallery under load, compare with VIGO's xterm.js terminal, and record the
numbers. Optimizations are separate cards, each with a before/after.

## Files to create or change

- `samples/LoomTerminal.Gallery/Diagnostics/FrameStatistics.cs` — create (copy the idea of LoomEditor's
  `samples/LoomEditor.Gallery/FrameStatistics.cs`: average / p95 frame time, frames per second, layout cache hit rate)
- `docs/performance.md` — add a "Rendering" section

## Design decisions (already made — do not change)

- Scenarios, at 200×50 and 1920×1080 window, 100 % and 150 %: `stress.ps1` (10 MB), `cmd /c dir /s C:\Windows`,
  `htop` idle with a 1 s refresh (WSL), `yes` for 10 s (WSL). Record: time to finish, CPU %, p95 frame time, working set.
- Same scenarios in VIGO's current terminal (WebView2 + xterm.js WebGL) for comparison.
- Target (from the project goal "faster and lighter than the web version"): finish time ≤ VIGO's, p95 frame ≤ 16 ms,
  working set ≤ VIGO terminal's WebView2 process. Misses become follow-up cards in `docs/tasks/README.md` (for example
  "dirty-row partial redraw", "glyph atlas renderer") — **not fixed here**.

## Tests

—

## Acceptance criteria

- [ ] `docs/performance.md` has the table for both implementations
- [ ] Follow-up cards created for every missed target
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Optimizations.

## Suggested commit message

`perf(winui): measure rendering performance`
