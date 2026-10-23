# P6-07 — Avalonia accessibility and gallery parity

- **Status:** done
- **Depends on:** P6-05, P6-06
- **Size:** M

## Goal

Give the Avalonia control an automation peer (P4-15 equivalent) and bring its gallery to the level of the WinUI one
(P4-16): tabs, settings, demo scripts, manual test checklist.

## Specification

P4-15, P4-16.

## Files to create or change

- `src/LoomTerminal.Avalonia/Accessibility/TerminalViewAutomationPeer.cs` — create (`ControlAutomationPeer`, text
  pattern if Avalonia exposes one for the platform; otherwise name + live announcements only — document the gap)
- `samples/LoomTerminal.AvaloniaGallery/` — tabs, settings, reuse `samples/LoomTerminal.Gallery/Demos/*.ps1`
- `docs/manual-tests.md` — add an "Avalonia" column

## Design decisions (already made — do not change)

- Compiled bindings only; the gallery is published with AOT on CI (P6-01 job).

## Tests

- [ ] Manual: the checklist in the AOT-published Avalonia gallery on Windows.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Notes

- The manual checklist (`docs/manual-tests.md`, "Avalonia gallery") needs a human at the keyboard: left unticked.
- Not in the Avalonia gallery (WinUI only): the Windows Terminal font switch and the frame statistics.
- Fixed on the way: `TerminalView.Render` updated the scrollbar, which crashed the AOT-published gallery ("Visual was
  invalidated during the render pass"); the update is now posted to the dispatcher.

## Out of scope

Linux and macOS (phase 7).

## Suggested commit message

`feat(avalonia): accessibility and gallery parity`
