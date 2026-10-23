# P8-07 — `docs/RELEASING.md`: the runbook for the user

- **Status:** done
- **Depends on:** P8-06
- **Size:** S (documentation only)

## Goal

One document tells the user, without guessing, what to configure once on nuget.org and GitHub, how to cut the first
tag, how to check the result, and how to undo a bad release. README and CLAUDE.md point to it.

## Sources

| File | What to take |
|---|---|
| `docs/tasks/phase-8/README.md` | the decisions (tag format, API key, environment `nuget`, gates, dry run, order) |
| `.github/workflows/release.yml`, `eng/*.ps1` (P8-01 … P8-06) | exact names of jobs, secret, environment, script parameters |
| `.github/workflows/ci.yml` | job names for branch protection ("Build and test", "Native AOT publish (win-x64)", "Native AOT publish (win-arm64)", "Pack NuGet packages") |

## Files to create or change

- `docs/RELEASING.md` — create
- `README.md` — "Status" paragraph: replace "Packages are built locally, not published yet" by "Packages are published to
  nuget.org when a version tag is pushed (see [docs/RELEASING.md](docs/RELEASING.md))" **only as a statement of the
  process**; keep the sentence that no version is published yet, as long as no tag exists; add a short `## Install`
  section (the four ids, `dotnet add package LoomTerminal.WinUI --prerelease`) marked "(from the first release)"
- `CLAUDE.md` — "Commands": two lines under the Pack/AOT lines:
  `rtk proxy pwsh -NoProfile -File eng/Invoke-ReleaseBuild.ps1 -Version 0.1.0-alpha.1   # every release gate, local dry run`
  and
  `rtk proxy pwsh -NoProfile -File eng/Get-ReleaseVersion.ps1 -SelfTest   # also eng/Get-ReleaseNotes.ps1 -SelfTest`

## Design decisions (already made — do not change)

`docs/RELEASING.md` has exactly these sections, in this order (English, direct, commands in fenced `bash`/`powershell`
blocks, one command per block; every step that only the user can do is prefixed **You:**):

1. **How a release works** — five lines: tag `vX.Y.Z[-pre]` → `Release` workflow → gates (list) → approval of the
   `nuget` environment → push of four packages + symbols → GitHub Release. The version of the packages is the tag; the
   `VersionPrefix`/`VersionSuffix` of `Directory.Build.props` are for local builds only.
2. **One-time setup** (a checklist the user ticks):
   - nuget.org: account with 2FA; check that `LoomTerminal.Core`, `.Pty`, `.Presentation`, `.WinUI` are free
     (`https://www.nuget.org/packages/<id>` returns 404) — if one is taken, stop and choose another id before tagging
     (ids are in the csproj files, in `Pack.ps1`, `Test-Packages.ps1`, the workflow and the READMEs);
     request the ID prefix reservation `LoomTerminal.` (nuget.org → account menu → "Contact support"/"Reserve ID prefix"
     form; optional, takes days, gives the verified check mark; the first push by the user owns the ids anyway);
     create an API key (Account → API keys): "Push new packages and package versions", glob pattern `LoomTerminal.*`,
     expiry 365 days (note the date in a calendar), owner = the user's account.
   - GitHub: Settings → Environments → `nuget`: required reviewer = the user, deployment branches/tags limited to
     `v*`; add the secret `NUGET_API_KEY` **to that environment** (not as a repository secret); Settings → Rules →
     Rulesets: a tag ruleset on `v*` (restrict creation to admins, block deletion and force-push), a branch ruleset on
     `main` requiring the four CI checks named in the Sources table and a linear history (signed commits optional);
     Settings → Actions → General: default workflow permissions "Read repository contents".
3. **Before the tag** — checklist: `docs/release-checklist.md` done; `main` is green; in `CHANGELOG.md` rename
   `## [Unreleased]` to `## [<version>] - <YYYY-MM-DD>` and add a new empty `## [Unreleased]` above (commit and push to
   `main`); local gates: `./eng/Invoke-ReleaseBuild.ps1 -Version <version>`; **dry run on GitHub**: Actions → Release →
   Run workflow → the version → green and eight files in the artifact.
4. **Cut the release** — `git switch main && git pull`, `git tag -s v<version> -m "v<version>"` (signed; plain `git tag
   -a` also works), `git push origin v<version>`; open the run; approve the `nuget` environment; wait for green.
   First release: `v0.1.0-alpha.1`. Never move or reuse a tag that was pushed: a published version is immutable.
5. **After the push** — nuget.org validates and indexes the packages (a few minutes up to an hour: the page says
   "validating" first; the symbols appear on the package page); check the four pages, the README rendering, the
   license, the repository link, and "Symbols"; install check in a scratch folder
   (`dotnet new console`, `dotnet add package LoomTerminal.Pty --prerelease`, `dotnet restore`); check the GitHub
   Release (four `.nupkg`, four `.snupkg`, notes); optionally add the nuget badges to `README.md` and set
   `VersionPrefix`/`VersionSuffix` of local builds to the next version.
6. **Rolling back** — table of cases: (a) a gate failed before the push: fix, delete the remote tag
   (`git push origin :refs/tags/v<version>`) and the local one, tag again (allowed because nothing was published);
   (b) the publish job failed halfway: "Re-run failed jobs" (skip-duplicate makes it safe); (c) a bad version reached
   nuget.org: it cannot be deleted; **unlist** it (nuget.org → Manage Packages → the version → "List in search
   results" off, or `dotnet nuget delete <id> <version> --source https://api.nuget.org/v3/index.json --api-key <key>`,
   which unlists on nuget.org), mark the GitHub Release as pre-release/edit its notes, then publish the fix as the
   next version (`alpha.2`, `0.1.1`…); unlist all four packages of the version together; (d) the API key leaked:
   regenerate it on nuget.org, update the environment secret.
7. **Versions and public API** — `alpha.N` → `beta.N` → `rc.N` → stable; one version for the four packages; the
   `PublicAPI.Unshipped.txt` files are moved into `PublicAPI.Shipped.txt` when the first **stable** version is cut
   (not for alphas); a package id never changes.
8. **Later improvements** — Trusted Publishing: nuget.org → Trusted Publishing → policy for repository
   `adgranger/LoomTerminal`, workflow `release.yml`, environment `nuget`; then in `release.yml` replace the API key by
   `NuGet/login@v1` and remove the secret (a change to the workflow, to be made in a card of its own); package icon
   (`PackageIcon` + a 128×128 PNG in `src/Directory.Build.props`, one more check in `Pack.ps1`); package signing;
   `LoomTerminal.Avalonia` (add its id to the publish order and to `Pack.ps1`).
9. **Troubleshooting** — a table: 403 on push (key expired, wrong glob or owner), 409 (version exists, harmless with
   skip-duplicate), "tag is not on main", "no section [x.y.z] in CHANGELOG.md", "NUGET_API_KEY is not set",
   "validation failed" mails from nuget.org (read the mail; missing README or license fields would already have
   failed `Pack.ps1`), AOT test fails only on the runner (re-run once; if it persists, run
   `eng/Test-Packages.ps1` locally).

## Tests

- [x] Every path, script, parameter, job name, secret and environment name written in the document exists in the
  repository exactly as spelled (check with `rtk git grep`); every link works, except the link to `docs/release-checklist.md`, which P8-08 creates (write it anyway)
- [x] `rtk git grep -n "not published yet" README.md` — still says so only if no tag exists (`rtk git tag` is empty)
- [ ] `HUMAN:` the user reads the "One-time setup" and "Cut the release" sections once and confirms they are
  unambiguous. Not ticked by an agent.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

- Doing any of the user steps (accounts, secrets, rulesets, tag), changing the workflow, screenshots.

## Suggested commit message

`docs: add the release runbook`
