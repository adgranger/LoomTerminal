# P3-19 — Search API on the session and match highlights

- **Status:** todo
- **Depends on:** P3-18, P3-09
- **Size:** S

## Goal

Expose search on `TerminalSession` and show matches in the render frame (all matches dim, the active one strong),
scrolling the viewport to the active match.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to take |
|---|---|---|
| `addons/addon-search/src/SearchAddon.ts` | 1–241 | `findNext`, `findPrevious`, `clearDecorations`, result counting, `onDidChangeResults` — logic only |
| `addons/addon-search/src/SearchResultTracker.ts` | 1–119 | result list and selected result |

## Files to create or change

- `src/LoomTerminal.Presentation/Session/TerminalSession.cs` — public `Task<SearchResultSummary> FindNextAsync(string term, SearchOptions options)`,
  `FindPreviousAsync`, `ClearSearch()`; event `SearchResultsChanged`
- `src/LoomTerminal.Presentation/Search/SearchResultTracker.cs` — create (internal)
- `src/LoomTerminal.Presentation/Rendering/RenderFrameBuilder.cs` — `TextRunStyle.Highlight` from the tracker
- `tests/LoomTerminal.Presentation.Tests/Search/SearchSessionTests.cs` — create

## Design decisions (already made — do not change)

- `SearchResultSummary(int ResultIndex, int ResultCount)` (public record; `-1` index when not found), capped at 1000
  results like the addon's `highlightLimit` default.
- The active match is also selected (xterm.js selects it), so Ctrl+Shift+C copies it.
- Highlight colors come from the theme: add `SearchMatchBackground`, `SearchActiveMatchBackground` to `TerminalTheme`
  (P3-07) with defaults `#515c6a80` and `#f8b64c80`.

## Tests

- [ ] New: two matches → summary `(0, 2)` then `(1, 2)` after `FindNextAsync`; frame runs carry `Highlight`;
      `ClearSearch` removes highlights; the viewport scrolls to a match in the scrollback.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Public API files updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Search UI (find box): backends/hosts (VIGO decides later).

## Suggested commit message

`feat(presentation): expose search on the session with match highlights`
