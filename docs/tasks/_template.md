# P<n>-<nn> — <Short title>

- **Status:** todo | in progress | done
- **Depends on:** P<n>-<nn>, …
- **Size:** S (< 150 lines of production code) | M (150–400) | L (split it!)

## Goal

One or two sentences: what exists after this task that did not exist before.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/...` | 10–80 | `functionName`, `ClassName.method` |

If a member is not exactly at the given line, search for its name in the file.

## Files to create or change

- `src/LoomTerminal.Core/...` — create
- `tests/LoomTerminal.Core.Tests/...` — create

## Design decisions (already made — do not change)

- …

## Tests

- [ ] Port: `describe` › `it`, … (keep the xterm.js name in a comment above each test)
- [ ] New: …

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (if a file was ported)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

- …

## Suggested commit message

`feat(core): …`
