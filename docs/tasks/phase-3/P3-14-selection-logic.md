# P3-14 — Selection logic (`SelectionService`, non-DOM parts)

- **Status:** done
- **Depends on:** P3-13, P3-09
- **Size:** M

## Goal

Port everything of `SelectionService` that does not touch the DOM: selected text (with wrapped lines and column mode),
word and line selection, select all, select lines, column selection, hit testing, trimming; and show the selection in
the render frame.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/browser/services/SelectionService.ts` | 43–71 | constants, `IWordPosition`, `SelectionMode` |
| | 165–266 | `reset`, `disable`, `enable`, `selectionStart`, `selectionEnd`, `hasSelection`, `selectionText` |
| | 267–343 | `clearSelection`, `isCellInSelection`, `_areCoordsInSelection` |
| | 366–396 | `selectAll`, `selectLines`, `_handleTrim` |
| | 437–448, 591–599 | `shouldForceSelection`, `shouldColumnSelect` (with our pointer event type) |
| | 759–1039 | `_handleBufferActivate`, `_convertViewportColToCharacterIndex`, `setSelection`, `_getWordAt`, `_selectWordAt`, `_selectToWordAt`, `_isCharWordSeparator`, `_selectLineAt` |
| `src/browser/renderer/shared/SelectionRenderModel.ts` | 1–93 | how the renderer clips the selection to the viewport |
| `src/browser/services/SelectionService.test.ts` | 1–500 | all |

## Files to create or change

- `src/LoomTerminal.Presentation/Selection/SelectionService.cs` — create (internal, partial if > 400 lines)
- `src/LoomTerminal.Presentation/Rendering/RenderFrame.cs` — add `SelectionRange? Selection` (viewport-relative
  start/end, `bool IsColumnMode`)
- `src/LoomTerminal.Presentation/Rendering/RenderFrameBuilder.cs` — fill it (port `SelectionRenderModel`)
- `tests/LoomTerminal.Presentation.Tests/Selection/SelectionServiceTests.cs` — create

## Design decisions (already made — do not change)

- The service lives on the parser thread (it reads the buffer). The UI calls `TerminalSession` methods that post
  actions: `SelectAll()`, `ClearSelection()`, `Task<string> GetSelectionTextAsync()`; `HasSelection` is read from the
  latest frame (`RenderFrame.Selection != null`).
- Mouse-driven parts (drag, double click, drag scroll) are P3-15; this card exposes the primitives they call.
- `wordSeparator` comes from `TerminalOptions.WordSeparator`. Non-breaking spaces in the selection text become spaces
  (JS `ALL_NON_BREAKING_SPACE_REGEX`).

## Tests

- [x] Port all of `SelectionService.test.ts` (`_selectWordAt` incl. emoji, `_selectLineAt`, `selectAll`,
      `selectLines`, `hasSelection`, `column selection`, `_areCoordsInSelection`). The JS tests build a
      `TestSelectionService` over a mock buffer: build the same over a real `BufferService`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Public API files updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Pointer handling (P3-15), clipboard (P3-16).

## Suggested commit message

`feat(presentation): port the selection logic`
