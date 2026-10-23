# P8-06 — Release workflow: tag → gates → nuget.org → GitHub Release (and a dry run)

- **Status:** todo
- **Depends on:** P8-04, P8-05
- **Size:** M (one workflow file, about 150 lines of YAML)

## Goal

`.github/workflows/release.yml` publishes the four packages (and their symbols) to nuget.org and creates a GitHub
Release when a `v*` tag is pushed, after every gate passed; the same workflow started by hand with a version only
runs the gates and uploads the packages (dry run).

## Sources

| File | What to take |
|---|---|
| `.github/workflows/ci.yml` | action versions, `env`, `permissions`, `shell: pwsh`, `setup-dotnet` with `global-json-file`, `upload-artifact` usage |
| `eng/Get-ReleaseVersion.ps1`, `eng/Get-ReleaseNotes.ps1`, `eng/Invoke-ReleaseBuild.ps1` | called by the jobs |
| `docs/tasks/phase-8/README.md` | decisions of the phase (auth, gates, order, dry run) |

## Files to create or change

- `.github/workflows/release.yml` — create

## Design decisions (already made — do not change)

- Name `Release`. Triggers: `push` with `tags: ['v*']`; `workflow_dispatch` with the required string input `version`
  (description: "Dry run: version to build (for example 0.1.0-alpha.1). Nothing is published."). Top-level
  `permissions: contents: read`, the same `env` block as `ci.yml`, and
  `concurrency: { group: release, cancel-in-progress: false }` (two releases never run together).
- Job `build` ("Gates and packages", `runs-on: windows-latest`, `outputs: version, prerelease`):
  1. `actions/checkout` with `fetch-depth: 0`; `actions/setup-dotnet` with `global-json-file: global.json`.
  2. "Resolve the version" (`id: version`, `shell: pwsh`, `env: RAW: ${{ github.event_name == 'push' && github.ref_name || inputs.version }}`):
     `./eng/Get-ReleaseVersion.ps1 $env:RAW -GitHubOutput`. The raw value goes through `env`, never into the script
     text (a tag name is attacker-controllable text).
  3. "Check the tag is on main" (`if: github.event_name == 'push'`): `git merge-base --is-ancestor $env:GITHUB_SHA origin/main`
     (fetch-depth 0 provides `origin/main`); on failure `throw "Tag $env:GITHUB_REF_NAME points to a commit that is not on main."`.
  4. "Extract the release notes" (`if: github.event_name == 'push'`): `./eng/Get-ReleaseNotes.ps1 -Version ${{ steps.version.outputs.version }} -OutFile artifacts/release-notes.md`
     plus `-AllowMissing` when `steps.version.outputs.prerelease == 'true'` (the version output is safe to interpolate:
     it was validated by the regex). Reason: a stable release without a changelog section fails here, before publishing.
  5. "Release build" (`shell: pwsh`): `./eng/Invoke-ReleaseBuild.ps1 -Version ${{ steps.version.outputs.version }}`.
  6. `actions/upload-artifact` `nuget-packages` (path `artifacts/packages`, `if-no-files-found: error`,
     `retention-days: 30`); on `push` also `release-notes` (path `artifacts/release-notes.md`, `if-no-files-found: error`).
  7. "Summary": append to `$env:GITHUB_STEP_SUMMARY` the version and the file names of `artifacts/packages`; when the
     event is `workflow_dispatch` add the line `Dry run: nothing was published.`
- Job `publish` ("Publish to nuget.org and GitHub", `runs-on: ubuntu-latest`, `needs: build`,
  `if: github.event_name == 'push'`, `environment: nuget`, `permissions: contents: write`):
  1. `actions/checkout` (no history needed), `actions/setup-dotnet` with `global-json-file: global.json`,
     `actions/download-artifact` for `nuget-packages` into `artifacts/packages` and `release-notes` into `artifacts`
     (use the major version that matches the `upload-artifact` major of `ci.yml`; check that the tag exists).
  2. "Check the API key" (`env: NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}`):
     `if ([string]::IsNullOrEmpty($env:NUGET_API_KEY)) { throw 'The secret NUGET_API_KEY is not set in the environment "nuget" (see docs/RELEASING.md).' }`.
  3. "Push to nuget.org" (same `env`, plus `VERSION: ${{ needs.build.outputs.version }}`): for each id in the fixed order
     `LoomTerminal.Core, LoomTerminal.Pty, LoomTerminal.Presentation, LoomTerminal.WinUI`:
     `dotnet nuget push "artifacts/packages/$id.$env:VERSION.nupkg" --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate`
     and `throw` on a non-zero `$LASTEXITCODE`. `dotnet nuget push` of a `.nupkg` also pushes the sibling `.snupkg`;
     nothing else is needed for symbols. The key is never echoed; do not enable `--no-service-endpoint`.
  4. "Create the GitHub Release" (`env: GH_TOKEN: ${{ github.token }}`, `TAG: ${{ github.ref_name }}`,
     `PRERELEASE: ${{ needs.build.outputs.prerelease }}`): if `gh release view $env:TAG` succeeds, run
     `gh release upload $env:TAG (Get-ChildItem artifacts/packages/*) --clobber`; otherwise
     `gh release create $env:TAG (Get-ChildItem artifacts/packages/*) --verify-tag --title $env:TAG --notes-file artifacts/release-notes.md --generate-notes`
     with `--prerelease` added when `$env:PRERELEASE -eq 'true'`. (`--generate-notes` appends GitHub's generated notes
     to the changelog section, which may be empty for a prerelease.)
- A failed `publish` job can be re-run from the Actions page ("Re-run failed jobs"): `--skip-duplicate` and the
  upload/create branching make it idempotent.
- Do not add triggers other than the two above, do not publish from a pull request, do not write the API key to a
  file, do not use `pull_request_target`.
- The environment `nuget` and its secret are created by the user (`docs/RELEASING.md`); if the environment does not
  exist GitHub creates it empty on first use, and step 2 then fails with the clear message above.

## Tests

- [ ] The YAML is valid: `rtk proxy python -I -c "import yaml,sys; yaml.safe_load(open('.github/workflows/release.yml'))"`
  if PyYAML is installed; otherwise load it with `actionlint` if available; otherwise re-read it line by line against
  this card and say in the Notes which check was possible
- [ ] `rtk git grep -n '\${{ *\(github\|inputs\)' .github/workflows/release.yml` — every match is inside an `env:`
  block, a step `if:`, or a `with:` value, never in a `run:` body (steps 4 of `build` and 3–4 of `publish` use
  `needs`/`steps` outputs that were validated; say so in the Notes)
- [ ] `HUMAN:` after the commit is on `main`: Actions → Release → "Run workflow" with `version` = `0.0.0-dryrun.1`:
  the `build` job is green, the artifact `nuget-packages` holds eight files, the `publish` job is **skipped**, no
  release and no package appeared. Not ticked by an agent.
- [ ] `HUMAN:` the same dry run with `version` = `1.2` fails in "Resolve the version". Not ticked by an agent.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `release.yml` exists, has exactly the two triggers, the two jobs and the steps above
- [ ] `rtk git status` shows only `.github/workflows/release.yml`, the card and the index
- [ ] Card status and `docs/tasks/README.md` updated (the two `HUMAN:` boxes stay unticked; status `done` means the
  file is delivered)

## Out of scope

- Creating the tag, the environment, the secret or the nuget.org key (user, `docs/RELEASING.md`).
- Trusted Publishing (OIDC), package signing, ARM64 package tests, posting to Discord or similar.
- Adding the workflow to branch protection (it only runs on tags and on demand).

## Suggested commit message

`ci: add the release workflow (tag to nuget.org and GitHub Release, with a dry run)`
