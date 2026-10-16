# P3-18 — Search engine (`addon-search`)

- **Status:** todo
- **Depends on:** P3-14
- **Size:** M

## Goal

Port the search engine of `addon-search` (find next/previous, case sensitivity, whole word, regex, wrapped lines) and
its line cache, with their tests.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `addons/addon-search/src/SearchLineCache.ts` | 1–114 | `LineCacheEntry`, `SearchLineCache` |
| `addons/addon-search/src/SearchEngine.ts` | 1–394 | `ISearchPosition`, `ISearchResult`, `SearchEngine` |
| `addons/addon-search/src/SearchState.ts` | 1–106 | `SearchState` |
| `addons/addon-search/src/SearchLineCache.test.ts` | 1–287 | all |
| `addons/addon-search/src/SearchEngine.test.ts` | 1–708 | all |

## Files to create or change

- `src/LoomTerminal.Presentation/Search/SearchLineCache.cs`, `SearchEngine.cs`, `SearchState.cs` — create (internal)
- `src/LoomTerminal.Presentation/Search/SearchOptions.cs`, `SearchResult.cs` — create (public records)
- `tests/LoomTerminal.Presentation.Tests/Search/SearchLineCacheTests.cs`, `SearchEngineTests.cs` — create

## Design decisions (already made — do not change)

- The addon works on the public xterm.js `Terminal` API (`terminal.buffer.active.getLine(y)` …). Add an internal
  adapter `SearchBufferView` over `TerminalCore` exposing just what the engine reads; port the engine against it.
- The line cache in JS is invalidated by a timer (`Constants.LINES_CACHE_TIME_TO_LIVE`) and on write/resize: keep the
  write/resize invalidation; replace the timer by "invalidate at the end of each search session" (document it).
- Regex search uses `Regex` with `RegexOptions.None` / `IgnoreCase` and a 1 s match timeout (no `[GeneratedRegex]`:
  patterns come from the user). Invalid patterns return "no result", as JS does on `SyntaxError`.

## Tests

- [ ] Port `SearchLineCache.test.ts` and `SearchEngine.test.ts` (the JS tests create a real `Terminal`: use a
      `TerminalCore` and the adapter).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Public API files updated; `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Highlighting results (P3-19).

## Suggested commit message

`feat(presentation): port the search engine of addon-search`
