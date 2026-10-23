# Release checklist

Everything that must be true before the first tag. Commands and the one-time setup are explained in
[RELEASING.md](RELEASING.md); this file only collects them in one place.

Boxes marked **HUMAN** are for the user; an agent never ticks them and never records a result it did not observe.

Release: v____

## 1. Automated gates

An agent or the user may run them. Tick with the date and the commit hash.

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings (date / commit: ________)
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green (date / commit: ________)
- [ ] `rtk proxy pwsh -NoProfile -File eng/Invoke-ReleaseBuild.ps1 -Version <version>` (date / commit: ________)
- [ ] The CI run of the latest `main` commit is green (jobs "Build and test", "Native AOT publish" for `win-x64` and
  `win-arm64`, "Pack NuGet packages")

## 2. Manual tests of the AOT gallery (HUMAN)

Source of truth: [manual-tests.md](manual-tests.md). Sections that still had an unticked item when this file was
written:

- [ ] **HUMAN:** run on the AOT-published gallery, not a Debug build
- [ ] **HUMAN:** "VIGO parity": all items of the section ticked in `docs/manual-tests.md`
- [ ] **HUMAN:** "Rendering (P4-03, P4-04, P4-05, P4-06)": all items of the section ticked in `docs/manual-tests.md`
- [ ] **HUMAN:** "Input (P4-07, P4-08, P4-09, P4-10, P4-11)": all items of the section ticked in `docs/manual-tests.md`
- [ ] **HUMAN:** "Scrollbar, focus, settings (P4-12, P4-13, P4-14)": all items of the section ticked in
  `docs/manual-tests.md`
- [ ] **HUMAN:** "Accessibility (P4-15)": all items of the section ticked in `docs/manual-tests.md`
- [ ] **HUMAN:** "Gallery itself (P4-16)": all items of the section ticked in `docs/manual-tests.md`
- [ ] **HUMAN:** "Gallery tabs (P4-24)": all items of the section ticked in `docs/manual-tests.md`
- [ ] **HUMAN:** "Windows Terminal font (P4-27)": all items of the section ticked in `docs/manual-tests.md`

## 3. Packages on a clean machine (HUMAN)

On a Windows 11 machine or VM without Visual Studio, outside the repository:

- [ ] **HUMAN:** take `artifacts/packages` of the dry run, build and run a tiny app that references
  `LoomTerminal.WinUI` from that folder (the published `LoomTerminal.PackageTest` of `eng/package-test` is such an app:
  copy its published folder and start the exe): a window opens and a shell answers
- [ ] **HUMAN:** the README of each package is readable
- [ ] **HUMAN:** `LoomTerminal.Pty` alone works from a console app (the snippet of its README)

## 4. Repository content (agent can prepare, HUMAN confirms)

- [ ] **HUMAN:** `CHANGELOG.md`: the `## [Unreleased]` section is renamed to the version (RELEASING.md section 3)
- [ ] **HUMAN:** `README.md`: status and install section are correct for this version
- [ ] **HUMAN:** `LICENSE` and `NOTICE.md` are present
- [ ] **HUMAN:** no secret or personal path in the repository (review the output of
  `rtk git grep -n -i "api[_-]key\|password\|C:\\Users"`)
- [ ] **HUMAN:** the gallery screenshot TODO in `README.md` is resolved or consciously left

## 5. External setup (HUMAN, RELEASING.md section 2)

- [ ] **HUMAN:** nuget.org account with 2FA
- [ ] **HUMAN:** the four ids are free (`LoomTerminal.Core`, `LoomTerminal.Pty`, `LoomTerminal.Presentation`,
  `LoomTerminal.WinUI`)
- [ ] **HUMAN:** ID prefix reservation `LoomTerminal.` requested (optional)
- [ ] **HUMAN:** Trusted Publishing policy created (owner `adgranger`, repository `LoomTerminal`, workflow
  `release.yml`, environment `nuget`, glob `LoomTerminal.*`); no API key exists
- [ ] **HUMAN:** GitHub environment `nuget` with a required reviewer and the secret `NUGET_USER` (nuget.org profile
  name)
- [ ] **HUMAN:** tag ruleset on `v*`
- [ ] **HUMAN:** branch ruleset on `main`

## 6. Dry run (HUMAN)

- [ ] **HUMAN:** Release workflow run with `version` = the version: green, eight files in the `nuget-packages`
  artifact, `publish` skipped

## 7. Go

- [ ] **HUMAN:** the tag is created by the user (RELEASING.md section 4); nobody else creates it
