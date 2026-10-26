# P7-07 — Close phase 7 and prepare 1.0

- **Status:** done
- **Depends on:** all P7 cards
- **Size:** S (docs)

## Goal

Document the cross-platform state, and list what remains before a 1.0 release (the decision to publish is the user's).

## Files to create or change

- `README.md` — supported platforms table (Windows WinUI / Avalonia, Linux Avalonia, macOS Avalonia)
- `docs/ROADMAP.md` — phase 7 ✅, and a "Before 1.0" list: API review (`PublicAPI.Unshipped` → `Shipped`), nuget.org
  publishing workflow, deferred features (search UI sample, OSC 133 shell integration, OSC 52, `path:line` links,
  scrollback restore, ligatures, grapheme clusters addon), performance follow-ups
- `docs/adr/*` — every `proposed` ADR is `accepted` or `rejected`

## Tests

- [x] CI green on the three OSes.

## Acceptance criteria

- [x] Docs updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Publishing packages.

## Suggested commit message

`docs: close phase 7`
