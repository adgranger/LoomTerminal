# P8-03 — A README for each package, written for nuget.org

- **Status:** done
- **Depends on:** P8-02
- **Size:** S (four Markdown files and one extra check in `eng/Pack.ps1`)

## Goal

Each of the four packages ships a README that reads well on its nuget.org page: what it is, how to install it
(prerelease included), a minimal example, where to learn more. `eng/Pack.ps1` fails if a packed README has a relative
link (broken on nuget.org) or does not name its package.

## Sources

| File | What to take |
|---|---|
| `src/LoomTerminal.Core/README.md`, `src/LoomTerminal.Presentation/README.md`, `src/LoomTerminal.WinUI/README.md` | existing content (kept) |
| `README.md` (root) | the "Using the pseudo terminal today" snippet (it compiles today) for the Pty README |
| `src/Directory.Build.props` | README packing: the project's own `README.md` is used when it exists, so a new `src/LoomTerminal.Pty/README.md` is packed automatically |

## Files to create or change

- `src/LoomTerminal.Pty/README.md` — create
- `src/LoomTerminal.Core/README.md`, `src/LoomTerminal.Presentation/README.md`, `src/LoomTerminal.WinUI/README.md` — edit
- `eng/Pack.ps1` — README checks (below)

## Design decisions (already made — do not change)

- Structure of the top of every README, in this order: `# <Id>`; one sentence saying what the package is (reuse the
  csproj `Description`); `## Install` with
  `dotnet add package <Id> --prerelease` (a sentence says `--prerelease` is needed while the version has a suffix);
  for Core, Pty and Presentation one line "Most applications want `LoomTerminal.WinUI`, which brings the other
  packages"; then the existing content. WinUI: the `## Install` section goes before its current usage section and says
  that Windows App SDK 2.5.1 and Win2D come transitively and that the app must be unpackaged or packaged WinUI 3 with
  `net11.0-windows10.0.26100.0` or later (read the exact requirement from `LoomTerminal.WinUI.csproj`, do not guess).
- Pty README: install, the snippet of the root README (same code), the list of what is supported (Windows ConPTY;
  macOS and Linux planned), a link to the repository.
- Core and Presentation READMEs keep their folder tables, moved under a final heading
  `## Contents of the package` (for readers of the source); their `## Usage` code stays untouched. Do not add new code
  samples (they could not be compiled by a test).
- Every link is **absolute** (`https://github.com/adgranger/LoomTerminal/blob/main/docs/...`); every image too. Reason:
  relative links break on nuget.org.
- The README of WinUI mentions in "Packages" that the packages are built by `./eng/Pack.ps1`; add that tagged releases
  are published to nuget.org by the release workflow (`docs/RELEASING.md`, absolute link). Remove the sentence "not
  published to nuget.org".
- `eng/Pack.ps1` checks, per `.nupkg`, on the packed `README.md` (read from the zip): it contains the text
  `dotnet add package <Id>`; it has no Markdown link or image with a relative target: regex
  `\]\((?!https?://|#|mailto:)[^)]+\)` finds nothing. Messages start with `"$id.nupkg README.md: "`. Reason: the
  package page is the first thing a user sees and cannot be edited after publishing.
- The root `README.md` is no longer packed by any project (all four have their own); do not delete the fallback
  in `src/Directory.Build.props` (future packages may use it).

## Tests

- [x] `rtk proxy pwsh -NoProfile -File eng/Pack.ps1` — exit 0
- [x] Negative check (not committed): add `[x](docs/ARCHITECTURE.md)` to `src/LoomTerminal.Pty/README.md`, run
  `eng/Pack.ps1`: fails and names `LoomTerminal.Pty.nupkg README.md`; revert. Then remove the `## Install` line of
  `src/LoomTerminal.Core/README.md`: fails and names Core; revert
- [x] `rtk proxy pwsh -NoProfile -File eng/Test-Packages.ps1` — exit 0
- [ ] `HUMAN:` open each `.nupkg` in NuGet Package Explorer (or read the README on nuget.org after the first publish)
  and check the rendering. Not ticked by an agent.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] The negative checks failed as expected and were reverted
- [x] `rtk git grep -n "](" -- src/*/README.md` shows no relative target
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

- Root `README.md` status text and install section (P8-07), CHANGELOG (P8-04), badges (after the first publish),
  screenshots, a package icon.

## Suggested commit message

`docs: write a README for each package, checked at pack time`
