# P8-04 — CHANGELOG and release notes extraction

- **Status:** todo
- **Depends on:** P8-01
- **Size:** S (a Markdown file and one PowerShell script)

## Goal

`CHANGELOG.md` (Keep a Changelog format) exists with the content of the first release, and
`eng/Get-ReleaseNotes.ps1 -Version <v>` extracts the section of one version, to be used as the body of the GitHub
Release.

## Sources

| File | What to take |
|---|---|
| `docs/ROADMAP.md` | what phases 0–4 delivered (content of the first entry) |
| `README.md`, `src/LoomTerminal.WinUI/README.md` | the feature list |
| `eng/Get-ReleaseVersion.ps1` (P8-01) | script style and `-SelfTest` convention |

## Files to create or change

- `CHANGELOG.md` — create (repository root)
- `eng/Get-ReleaseNotes.ps1` — create
- `.github/workflows/ci.yml` — job `pack`: add `./eng/Get-ReleaseNotes.ps1 -SelfTest` to the step
  "Self-test the release scripts" (P8-01)

## Design decisions (already made — do not change)

- `CHANGELOG.md` follows https://keepachangelog.com/en/1.1.0/: a header paragraph (format, SemVer, "versions are
  published when a `vX.Y.Z` tag is pushed, see docs/RELEASING.md"), then `## [Unreleased]` with the sub-headings
  `### Added`, `### Changed`, `### Fixed` (only those that have content). The `[Unreleased]` section is filled **now**
  with what the first release contains: one bullet per package (Core: the xterm.js port and what it supports; Pty:
  ConPTY, shell detection; Presentation: session, input encoding, selection, links, search, accessibility model;
  WinUI: the Win2D control, IME, UIA, scrollbar, theming, Windows Terminal font), plus "Native AOT compatible,
  checked by CI" and "Windows only for now". Facts only, from the docs, no marketing; at most 25 lines.
- At release time the **user** renames `## [Unreleased]` to `## [0.1.0-alpha.1] - YYYY-MM-DD` and adds a fresh empty
  `## [Unreleased]` above it (runbook, P8-07). The link references at the bottom of the file are optional and are
  written by the user when they want them.
- `Get-ReleaseNotes.ps1`: `param([Parameter(Position = 0)][string] $Version, [string] $ChangelogPath = 'CHANGELOG.md',
  [string] $OutFile, [switch] $AllowMissing, [switch] $SelfTest)`.
  - Finds the first line matching `^## \[<escaped $Version>\]` (the date after it is free text), takes every line up to
    (excluding) the next line starting with `## [` or a link reference definition (`^\[[^\]]+\]:\s`), trims leading and
    trailing blank lines.
  - Writes the text to `$OutFile` (UTF-8 without BOM, LF, final newline) or to the success stream when no `$OutFile`.
  - Section missing or empty: `throw "CHANGELOG.md has no (or an empty) section [<Version>]: rename [Unreleased] first."`;
    with `-AllowMissing` write a warning instead and write an **empty** file / output, exit 0. Reason: a stable release
    must have notes, a prerelease may use only GitHub's generated notes (P8-06).
  - `$Version` must pass the same format check as `Get-ReleaseVersion.ps1`: call it (`& "$PSScriptRoot/Get-ReleaseVersion.ps1" $Version`)
    and use its output as the version. `Unreleased` is never accepted.
- `-SelfTest` writes a temp changelog with: `[Unreleased]` with text, `[1.0.0] - 2030-01-01` with two bullets,
  `[1.0.0-rc.1]` empty, `[0.9.0]` with text, and trailing link references; checks: `1.0.0` returns exactly its two
  bullets (not the rc.1 or 0.9.0 text, not the link lines); `1.0.0-rc.1` throws; `1.0.0-rc.1 -AllowMissing` returns
  empty; `2.0.0` throws; `2.0.0 -AllowMissing` returns empty; `-OutFile` creates the file with the same text. Prints
  `Self-test passed: <n> cases`. Reason: the version `1.0.0` must not match `1.0.0-rc.1` (the `]` after the escaped
  version in the pattern does this).

## Tests

- [ ] `rtk proxy pwsh -NoProfile -File eng/Get-ReleaseNotes.ps1 -SelfTest` — prints `Self-test passed: <n> cases`
- [ ] `rtk proxy pwsh -NoProfile -File eng/Get-ReleaseNotes.ps1 -Version 0.1.0-alpha.1` fails with the "rename
  [Unreleased]" message (the real changelog is not renamed yet: this is expected), and succeeds with `-AllowMissing`
  (empty output, warning on stderr)
- [ ] `rtk git grep -n "\[Unreleased\]" CHANGELOG.md` finds the heading

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Every command of "Tests" above behaves as written
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

- Renaming `[Unreleased]` for a real version (done by the user when tagging).
- Generating the changelog from commits; the workflow (P8-06) also asks GitHub for generated notes.

## Suggested commit message

`docs: add the changelog and a script that extracts a release section`
