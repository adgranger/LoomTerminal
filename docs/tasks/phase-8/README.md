# Phase 8 conventions — first release (NuGet)

Phase 8 makes the first public release a matter of **creating one git tag**: everything the release needs
(version from the tag, a release workflow, package checks, changelog, runbook, checklist) exists and is tested before.
The four packages (`LoomTerminal.Core`, `.Pty`, `.Presentation`, `.WinUI`) are published to nuget.org together, with
symbols (`.snupkg`), and a GitHub Release carries the same files.

Phase 8 does **not** depend on phase 5 (VIGO integration happens in another repository, later, from the published
packages) nor on phases 6–7 (new packages, such as `LoomTerminal.Avalonia`, will reuse this machinery).

## Decisions of the whole phase (final)

- **Version = git tag.** Tag `vMAJOR.MINOR.PATCH[-prerelease]` (SemVer 2, no build metadata), for example
  `v0.1.0-alpha.1`. The workflow strips the `v` and passes it to MSBuild as the global property `Version`, which wins
  over `VersionPrefix`/`VersionSuffix`. Those two properties in `Directory.Build.props` stay as the version of
  **local** builds only; nothing is edited at release time. All four packages always share one version.
- **First tag:** `v0.1.0-alpha.1` (any later tag works the same way).
- **Authentication to nuget.org: Trusted Publishing (OIDC)** since P8-09 (it replaced the first decision, an API key
  in the secret `NUGET_API_KEY`). The publish job has `id-token: write`, `NuGet/login` trades the GitHub token for a
  one-hour key, and the environment `nuget` (required reviewer) holds only the secret `NUGET_USER`. No long-lived key.
- **Gates before anything is published** (all in one job, fail fast): tag format, tag commit reachable from `main`,
  Release build with 0 warnings, all tests, `eng/Test-Packages.ps1` (pack + content check + AOT consumer app),
  CHANGELOG section present (stable versions). Publishing only starts when every gate passed.
- **Dry run:** `workflow_dispatch` of the release workflow with a `version` input runs every gate and uploads the
  packages as a workflow artifact; it never publishes and never creates a GitHub Release.
- **Publish order:** Core, Pty, Presentation, WinUI (dependencies first), `--skip-duplicate` so a re-run after a partial
  failure is safe. `.snupkg` files go with their `.nupkg`.
- **No package icon** for the first releases (no artwork exists); the runbook says how to add one later.
- **Nothing in this phase creates a tag, publishes, or touches nuget.org / GitHub settings or secrets.** Those steps
  belong to the user and are written in `docs/RELEASING.md`.

## Rules

- Scripts are PowerShell 7 in `eng/`, same style as `eng/Pack.ps1`: comment-based help, `$ErrorActionPreference = 'Stop'`,
  `Set-StrictMode -Version Latest`, a failure `throw`s a message that says what is wrong. A script with logic has a
  `-SelfTest` switch that checks it on fixed cases and prints `Self-test passed: <n> cases`; CI runs the self-tests.
- Workflows follow `.github/workflows/ci.yml`: same action versions (`actions/checkout`, `actions/setup-dotnet`,
  `actions/upload-artifact`), `shell: pwsh`, `global-json-file: global.json`, `DOTNET_*` env, least-privilege
  `permissions`. **Never interpolate `${{ github.* }}` or `${{ inputs.* }}` into a script body**: pass it through `env:`.
- Cards in this phase cannot be fully verified without GitHub or nuget.org. A check only a human can do is written
  **`HUMAN:`** in the card, stays unticked, and is also collected in `docs/release-checklist.md` (P8-08). An agent never
  ticks it and never invents a result.
- Run the scripts from the repository root, through `rtk proxy pwsh -NoProfile -File eng/<script>.ps1 …` (CLAUDE.md).
