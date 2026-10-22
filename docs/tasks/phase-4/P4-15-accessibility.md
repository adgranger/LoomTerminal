# P4-15 — UI Automation: peer, text provider, announcements

- **Status:** done
- **Depends on:** P3-22, P4-13
- **Size:** M

## Goal

Make the terminal usable with Narrator / NVDA: an automation peer exposing the visible text through `ITextProvider`,
and live announcements of new output.

## Sources

| File | What to take |
|---|---|
| `D:\LoomEditor\src\LoomEditor.WinUI\Accessibility\LoomEditorAutomationPeer.cs` | peer shape (control type, name) |
| Windows Terminal (reference only, no code): `src/types/TermControlUiaProvider.cpp` | what a terminal exposes (Document control type, text ranges per line) |

## Files to create or change

- `src/LoomTerminal.WinUI/Accessibility/TerminalAutomationPeer.cs` — create
- `src/LoomTerminal.WinUI/Accessibility/TerminalTextProvider.cs`, `TerminalTextRange.cs` — create (internal)
- `src/LoomTerminal.WinUI/Controls/TerminalControl.cs` — `OnCreateAutomationPeer`; `Session.AccessibilityEnabled` set
  when a UIA client listens (`AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged)` checked on focus)

## Design decisions (already made — do not change)

- Control type `Document`, `Name` from `AutomationProperties.Name` or `"Terminal"`.
- `ITextProvider` over `RenderFrame.PlainLines` (P3-22): `DocumentRange` = all visible lines joined by `\r\n`;
  `GetSelection` = the terminal selection; ranges support `Character`, `Word`, `Line` units on that text. No editing.
- `TextAnnounced` → `RaiseNotificationEvent(AutomationNotificationKind.Other, AutomationNotificationProcessing.ImportantAll, text, "LoomTerminalOutput")`.

## Tests

- [ ] Manual: Narrator reads output as it arrives; Narrator's line navigation reads the visible lines;
      Accessibility Insights "FastPass" has no error on the gallery.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Scrollback navigation for screen readers.

## Suggested commit message

`feat(winui): add the UI Automation peer and text provider`
