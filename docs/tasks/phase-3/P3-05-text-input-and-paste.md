# P3-05 — Text input and paste encoding

- **Status:** done
- **Depends on:** P3-02
- **Size:** S

## Goal

Port the helpers that turn typed or pasted text into pty input: line ending normalization and bracketed paste.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/browser/Clipboard.ts` | 13–31 | `prepareTextForTerminal`, `bracketTextForPaste` |
| `src/browser/Clipboard.test.ts` | 1–32 | all |
| `src/browser/Clipboard.ts` | 51–62 | `paste`: read only (it calls the two helpers then `coreService.triggerDataEvent(text, true)`) |

## Files to create or change

- `src/LoomTerminal.Presentation/Input/PasteEncoding.cs` — create (internal static)
- `tests/LoomTerminal.Presentation.Tests/Input/PasteEncodingTests.cs` — create

## Design decisions (already made — do not change)

- `string PreparePaste(string text, bool bracketedPasteMode, bool ignoreBracketedPasteMode)` = prepare, then
  bracket if `bracketedPasteMode && !ignoreBracketedPasteMode` (JS `paste` logic).
- Typed text (`CharacterReceived`, IME commit) is sent as-is (UTF-8), without `prepareTextForTerminal`.

## Tests

- [x] Port `Clipboard.test.ts`.
- [x] New: CRLF and LF both become CR; bracketed paste wraps with `\x1b[200~` / `\x1b[201~`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Clipboard access (P3-16).

## Suggested commit message

`feat(presentation): port the paste encoding helpers`
