# Releasing LoomTerminal

Steps prefixed **You:** can only be done by the repository owner (accounts, secrets, settings, the tag).

## 1. How a release works

1. You push a tag `vX.Y.Z[-pre]` (first release: `v0.1.0-alpha.1`); the `Release` workflow
   ([release.yml](../.github/workflows/release.yml)) starts.
2. Job "Gates and packages" runs every gate and stops at the first failure: tag format, tag commit reachable from
   `main`, CHANGELOG section present, Release build with 0 warnings, all tests, `eng/Test-Packages.ps1` (pack, content
   check, Native AOT consumer app).
3. Job "Publish to nuget.org and GitHub" waits for your approval of the `nuget` environment.
4. It pushes `LoomTerminal.Core`, `.Pty`, `.Presentation`, `.WinUI` (in that order) with their symbols (`.snupkg`).
5. It creates the GitHub Release with the same files and the CHANGELOG notes.

The version of the packages is the tag. `VersionPrefix`/`VersionSuffix` in `Directory.Build.props` are for local builds
only; nothing is edited at release time.

## 2. One-time setup

nuget.org:

- [ ] **You:** create an account with 2FA enabled.
- [ ] **You:** check that the four ids are free: `https://www.nuget.org/packages/<id>` must return 404 for
  `LoomTerminal.Core`, `LoomTerminal.Pty`, `LoomTerminal.Presentation`, `LoomTerminal.WinUI`. If one is taken, stop and
  choose another id before tagging (ids are in the csproj files, `eng/Pack.ps1`, `eng/Test-Packages.ps1`, the workflow
  and the package READMEs).
- [ ] **You (optional):** request the ID prefix reservation `LoomTerminal.` (nuget.org → account menu →
  "Contact support" / "Reserve ID prefix" form). It takes days and gives the verified check mark; your first push owns
  the ids anyway.
- [ ] **You:** create an API key (Account → API keys): scope "Push new packages and package versions", glob pattern
  `LoomTerminal.*`, expiry 365 days (note the date in a calendar), owner = your account.

GitHub:

- [ ] **You:** Settings → Environments → `nuget`: required reviewer = you; deployment branches/tags limited to `v*`.
- [ ] **You:** add the secret `NUGET_API_KEY` **to that environment** (not as a repository secret).
- [ ] **You:** Settings → Rules → Rulesets: a tag ruleset on `v*` (restrict creation to admins, block deletion and
  force-push).
- [ ] **You:** a branch ruleset on `main` requiring the four CI checks "Build and test", "Native AOT publish (win-x64)",
  "Native AOT publish (win-arm64)" and "Pack NuGet packages", and a linear history (signed commits optional).
- [ ] **You:** Settings → Actions → General: default workflow permissions "Read repository contents".

## 3. Before the tag

- [ ] [docs/release-checklist.md](release-checklist.md) is done.
- [ ] `main` is green.
- [ ] In `CHANGELOG.md`, rename `## [Unreleased]` to `## [<version>] - <YYYY-MM-DD>` and add a new empty
  `## [Unreleased]` above it; commit and push to `main`.
- [ ] Local gates:

```powershell
./eng/Invoke-ReleaseBuild.ps1 -Version 0.1.0-alpha.1
```

- [ ] **You:** dry run on GitHub: Actions → Release → Run workflow → enter the version. It must be green and the
  `nuget-packages` artifact must hold eight files (four `.nupkg`, four `.snupkg`). Nothing is published.

## 4. Cut the release

**You:**

```bash
git switch main && git pull
```

```bash
git tag -s v0.1.0-alpha.1 -m "v0.1.0-alpha.1"
```

(a plain `git tag -a` also works)

```bash
git push origin v0.1.0-alpha.1
```

Open the run in the Actions tab, approve the `nuget` environment, and wait for green.

Never move or reuse a tag that was pushed: a published version is immutable.

## 5. After the push

nuget.org validates and indexes the packages (a few minutes up to an hour; the page says "validating" first, and the
symbols appear on the package page).

- [ ] **You:** check the four package pages: README rendering, license, repository link, "Symbols".
- [ ] **You:** install check in a scratch folder:

```bash
dotnet new console && dotnet add package LoomTerminal.Pty --prerelease && dotnet restore
```

- [ ] **You:** check the GitHub Release: four `.nupkg`, four `.snupkg`, notes.
- [ ] Optional: add the nuget badges to `README.md`, and set `VersionPrefix`/`VersionSuffix` of local builds to the
  next version.

## 6. Rolling back

| Case | What to do |
|---|---|
| (a) A gate failed before the push | Fix, delete the remote tag (`git push origin :refs/tags/v<version>`) and the local one (`git tag -d v<version>`), tag again. Allowed because nothing was published. |
| (b) The publish job failed halfway | "Re-run failed jobs" (`--skip-duplicate` makes it safe). |
| (c) A bad version reached nuget.org | It cannot be deleted. **Unlist** it: nuget.org → Manage Packages → the version → "List in search results" off, or `dotnet nuget delete <id> <version> --source https://api.nuget.org/v3/index.json --api-key <key>` (which only unlists on nuget.org). Mark the GitHub Release as pre-release or edit its notes, then publish the fix as the next version (`alpha.2`, `0.1.1`…). Unlist all four packages of the version together. |
| (d) The API key leaked | Regenerate it on nuget.org, update the `NUGET_API_KEY` secret of the `nuget` environment. |

## 7. Versions and public API

- Order: `alpha.N` → `beta.N` → `rc.N` → stable. One version for the four packages.
- The `PublicAPI.Unshipped.txt` files are moved into `PublicAPI.Shipped.txt` when the first **stable** version is cut
  (not for alphas).
- A package id never changes.

## 8. Later improvements

- **Trusted Publishing:** nuget.org → Trusted Publishing → policy for repository `adgranger/LoomTerminal`, workflow
  `release.yml`, environment `nuget`; then replace the API key by `NuGet/login@v1` in `release.yml` and remove the
  secret (a change to the workflow, to be made in a card of its own).
- Package icon: `PackageIcon` plus a 128×128 PNG in `Directory.Build.props`, and one more check in `eng/Pack.ps1`.
- Package signing.
- `LoomTerminal.Avalonia`: add its id to the publish order and to `eng/Pack.ps1`.

## 9. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| 403 on push | The key expired, its glob does not match the id, or its owner is wrong: create a new key (section 2). |
| 409 on push | The version already exists; harmless with `--skip-duplicate`. |
| "points to a commit that is not on main" | The tag was created on another branch: delete it (case (a)) and tag `main`. |
| "CHANGELOG.md has no (or an empty) section [x.y.z]" | Rename `[Unreleased]` first (section 3). |
| "The secret NUGET_API_KEY is not set in the environment "nuget"" | Add it to the environment, not to the repository (section 2). |
| "validation failed" mail from nuget.org | Read the mail. Missing README or license fields would already have failed `eng/Pack.ps1`. |
| AOT test fails only on the runner | Re-run once; if it persists, run `./eng/Test-Packages.ps1` locally. |
