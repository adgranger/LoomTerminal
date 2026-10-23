# P8-02 — nuspec metadata checks

- **Status:** todo
- **Depends on:** P8-01
- **Size:** S (MSBuild property and extra checks in `eng/Pack.ps1`)

## Goal

`eng/Pack.ps1` also fails when a package would look wrong on nuget.org: missing id/version/authors/description,
wrong project or repository URL, no tags, no release notes link, no README reference. Together with the checks of
P4-21 this is the "package metadata check" run on every CI build and before every release.

## Sources

| File | What to take |
|---|---|
| `eng/Pack.ps1` | the per-package nuspec section (`$meta = $nuspec.package.metadata`), the `$problems` list |
| `Directory.Build.props` | "Package metadata" group: authors, copyright, URLs, tags |
| `src/*/*.csproj` | each project already has a `<Description>` (kept as is) |

## Files to create or change

- `Directory.Build.props` — "Package metadata" group: add
  `<PackageReleaseNotes>https://github.com/adgranger/LoomTerminal/releases</PackageReleaseNotes>`
- `eng/Pack.ps1` — new checks in the nuspec section (below)

## Design decisions (already made — do not change)

- New checks, each adds a message that starts with `"$id.nuspec: "` to `$problems` (never `throw` inside the loop):
  - `id` equals `$id`; `version` equals `$version`;
  - `authors` is `LoomTerminal contributors`; `copyright` is not empty;
  - `projectUrl` equals `https://github.com/adgranger/LoomTerminal`;
  - `repository` has `type` `git`, a `url` starting with `https://github.com/adgranger/LoomTerminal`, and a `commit`
    of 40 hex characters (the existing "non-empty commit" check is replaced by this one);
  - `tags` contains the words `terminal` and `xterm`;
  - `description` is at least 40 characters long and is not equal to `$id`;
  - `releaseNotes` is not empty;
  - `readme` equals `README.md`;
  - `requireLicenseAcceptance` is `false`.
  Reason: these are the fields nuget.org shows or validates; a wrong URL or a missing description cannot be fixed
  after publishing (a version is immutable).
- The id/owner/prefix side (is `LoomTerminal.*` free on nuget.org, who owns it) is **not** checkable offline: it is a
  `HUMAN:` step of `docs/RELEASING.md` and `docs/release-checklist.md`.
- No package icon: nothing to check. Do not add a `PackageIcon` property.
- Descriptions stay as they are in the csproj files; do not rewrite them.

## Tests

- [ ] `rtk proxy pwsh -NoProfile -File eng/Pack.ps1` — exit 0
- [ ] Negative check (not committed), one at a time, run `eng/Pack.ps1`, see it fail naming the right field for the
  four packages, revert: (a) delete `PackageReleaseNotes` from `Directory.Build.props` (`releaseNotes`); (b) change
  `Authors` to `x` (`authors`); (c) change `PackageProjectUrl` to `https://example.com` (`projectUrl`)
- [ ] `rtk proxy pwsh -NoProfile -File eng/Pack.ps1 -Version 0.1.0-alpha.1` — exit 0 (still works with `-Version`)

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `rtk proxy pwsh -NoProfile -File eng/Test-Packages.ps1` — exit 0
- [ ] The three negative checks failed as expected and were reverted (`rtk git status` shows only this card's files)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

- Package README content (P8-03), CHANGELOG (P8-04), package icon, package signing.
- Checking nuget.org (id availability, prefix reservation): human, in `docs/RELEASING.md`.

## Suggested commit message

`build: check the nuspec metadata of every package`
