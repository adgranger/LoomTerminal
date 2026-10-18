# P2-04 — Record real ConPTY sessions (`PtyConsole --record`)

- **Status:** in progress
- **Depends on:** P2-01
- **Size:** S

## Goal

Let `samples/LoomTerminal.PtyConsole` save everything a real shell sends (and every resize) to a `.vtrec` file, and
record a first set of non-interactive sessions.

## Files to create or change

- `samples/LoomTerminal.PtyConsole/Program.cs` — add `--record <file>` (before the command line) and
  `--size <cols>x<rows>` (fixed size, no window watching: makes recordings reproducible)
- `samples/LoomTerminal.PtyConsole/SessionRecorder.cs` — create (writes `O <base64>` / `R <cols> <rows>` lines, flushes on exit)
- `tests/LoomTerminal.Core.Tests/Conformance/Fixtures/recorded/*.vtrec` — create (list below)
- `docs/tasks/phase-2/README.md` — add a "How to record" section

## Design decisions (already made — do not change)

- Non-interactive recordings made by the card (run each through `rtk proxy` so the raw output is not filtered):
  `cmd-dir` (`cmd.exe /c dir C:\Windows\System32\drivers\etc`), `pwsh-get-childitem` (`pwsh -NoProfile -Command
  Get-ChildItem $env:WINDIR -Filter *.exe`), `git-log-graph` (`git -C <this repo> log --graph --oneline --color=always -n 30`),
  `pwsh-format-table-colors` (`pwsh -NoProfile -Command "$PSStyle.OutputRendering='Ansi'; Get-Process | Select -First 20 | Format-Table"`).
  All at `--size 100x30`.
- Interactive recordings (vim, htop, less under WSL, PSReadLine editing) are listed in the README as **manual**
  ("ask the user to record them"): the card does not fake them.
- Recordings must not contain personal data: review each file (decode the base64) and drop any that shows user
  names, paths under `C:\Users\…`, tokens or e-mail addresses. Use `--size` and neutral folders to avoid them.

## Progress

Done: `--record` / `--size`, `SessionRecorder`, the README section and the reader test (the sample builds with 0 warnings
on Linux). **Left (needs Windows and ConPTY):** record `cmd-dir`, `pwsh-get-childitem`, `git-log-graph` and
`pwsh-format-table-colors` at `--size 100x30`, review them for personal data, generate their JSON with the oracle,
then set the status to `done`.

## Tests

- [x] New (in `LoomTerminal.Core.Tests`): `FixtureReader` reads every committed `.vtrec` without error.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Running the recordings against the oracle (P2-05).

## Suggested commit message

`feat(samples): record real pty sessions as conformance fixtures`
