# P1-48 — `InputHandler.Sgr` (2/2): `charAttributes` and the SGR test suites

- **Status:** todo
- **Depends on:** P1-47
- **Size:** M

## Goal

Port `charAttributes` (CSI `m`) and all its tests: bold/dim/italic/underline/blink/inverse/invisible/strikethrough/
overline, 16/256/RGB colors, colon notation, SGR 0 with hyperlinks, extended underline styles, underline colors.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 2510–2652 | `charAttributes` |
| | constructor | CSI `m` |
| `src/common/InputHandler.test.ts` | 679–823 | `text attributes` |
| | 824–971 | `colon notation` |
| | 1726–1756 | `reset text attributes (SGR 0)` |
| | 1757–1829 | `extended underline style support (SGR 4)` |
| | 1830–1931 | `underline colors (SGR 58 & SGR 59)` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.Sgr.cs` — add `charAttributes` and `RegisterSgrHandlers`
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerSgrTests.cs` — create (split into
  `InputHandlerSgrTests.Colon.cs` / `.Underline.cs` partials if the file exceeds ~400 lines)

## Design decisions (already made — do not change)

- See P1-36. `SGR 0` keeps the current URL id when a hyperlink is open (test at 1739): this depends on OSC 8
  (P1-51). Port that one test with `[Fact(Skip = "Needs OSC 8 (P1-51)")]`; P1-51 removes the skip.

## Tests

- [ ] Deferred from P1-45: port `alt screen` (all six tests, InputHandler.test.ts 624–678; they use SGR colors).

- [ ] Port the five suites listed in the sources table.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green (one skip, as above)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(core): port SGR (character attributes)`
