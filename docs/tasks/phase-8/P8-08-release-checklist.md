# P8-08 — Release checklist (needs a human)

- **Status:** done
- **Depends on:** P8-07
- **Size:** S (one Markdown file)

## Goal

`docs/release-checklist.md` groups, in one place, everything that must be true before the first tag: the automated
gates (commands), the manual tests deferred from phase 4 (only the **remaining** ones, referenced not copied), and the
external setup that only the user can do. An agent writes the file; **the boxes inside it are ticked by the user**.

## Sources

| File | What to take |
|---|---|
| `docs/manual-tests.md` | its sections and which items are still unticked **at the time the card is done** |
| `docs/tasks/phase-4/P4-18-close-phase-4.md` | the pending "manual-tests fully ticked on the AOT gallery" item |
| `docs/RELEASING.md` (P8-07) | one-time setup, dry run, tag |

## Files to create or change

- `docs/release-checklist.md` — create
- `docs/manual-tests.md` — one line under the title: "The remaining unticked items are part of
  [docs/release-checklist.md](release-checklist.md)."

## Design decisions (already made — do not change)

- The file starts with: purpose, "Boxes marked **HUMAN** are for the user; an agent never ticks them and never
  records a result it did not observe", and the version being released (a line `Release: v____` for the user).
- Sections, in this order:
  1. **Automated gates** (an agent or the user may run them; tick with the date and commit hash):
     `rtk dotnet build LoomTerminal.slnx -c Release`; `rtk test dotnet test --solution LoomTerminal.slnx -c Release`;
     `rtk proxy pwsh -NoProfile -File eng/Invoke-ReleaseBuild.ps1 -Version <version>`;
     the CI run of the latest `main` commit is green (three jobs).
  2. **Manual tests of the AOT gallery (HUMAN)** — one box per **section title** of `docs/manual-tests.md` that still
     has an unticked item (for example "VIGO parity", "Rendering", "Input", "Scrollback, focus, settings",
     "Accessibility", "Gallery itself", "Gallery tabs", "Windows Terminal font"), each saying "all items of the section
     ticked in `docs/manual-tests.md`". Do not copy the items (one source of truth); list the sections found, not a
     fixed guess. Add one box: "run on the AOT-published gallery, not a Debug build".
  3. **Packages on a clean machine (HUMAN)** — install check outside the repo: on a Windows 11 machine or VM without
     Visual Studio, take `artifacts/packages` of the dry run, build and run a tiny app that references
     `LoomTerminal.WinUI` from that folder (the published `LoomTerminal.PackageTest` of `eng/package-test` is such an
     app: copy its published folder and start the exe): a window opens and a shell answers; the README of each
     package is readable; `LoomTerminal.Pty` alone works from a console app (the snippet of its README).
  4. **Repository content (agent can prepare, HUMAN confirms)** — `CHANGELOG.md` section renamed to the version;
     `README.md` status and install section; `LICENSE` and `NOTICE.md` present; no secret or personal path in the
     repository (`rtk git grep -n -i "api[_-]key\|password\|C:\\\\Users"` reviewed); the gallery screenshot TODO in
     `README.md` resolved or consciously left.
  5. **External setup (HUMAN, `docs/RELEASING.md` section 2)** — nuget.org account and 2FA; the four ids free;
     prefix reservation requested; API key created (expiry date noted); GitHub environment `nuget` with a reviewer and
     the secret; tag ruleset; branch ruleset.
  6. **Dry run (HUMAN)** — Release workflow run with `version` = the version: green, eight files in the artifact,
     `publish` skipped.
  7. **Go** — the tag is created by the user (`docs/RELEASING.md` section 4); nobody else creates it.
- The checklist is a living file: after the release it is copied (not edited in place) to
  `docs/releases/<version>.md` by the user if they want a record; not required.
- Card status: `done` when the file is delivered. The ticks inside `docs/release-checklist.md` are **PENDING:
  needs a human**, as for the manual tests in P4-18; this card does not tick them.

## Tests

- [x] Every command, path and section name in the file exists as spelled (`rtk git grep`); the list in section 2
  matches the unticked sections of `docs/manual-tests.md` at that moment
- [ ] `HUMAN:` the user goes through the checklist before the first tag and ticks it. **PENDING: needs a human.**

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

- Running the manual tests, ticking any box of the checklist or of `docs/manual-tests.md`, taking the screenshot,
  creating the tag, touching nuget.org or GitHub settings.

## Suggested commit message

`docs: add the release checklist (manual checks and external setup)`
