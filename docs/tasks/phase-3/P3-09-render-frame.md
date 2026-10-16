# P3-09 — `RenderFrame` and `RenderFrameBuilder`

- **Status:** todo
- **Depends on:** P3-08
- **Size:** M

## Goal

Define the immutable snapshot a backend draws, and build it from `TerminalCore`: visible rows as runs of cells with
identical resolved style, cursor, selection range, dirty rows, viewport position.

## Files to create or change

- `src/LoomTerminal.Presentation/Rendering/RenderFrame.cs` — create (public: `RenderFrame`, `RenderRow`, `RenderRun`,
  `RenderCursor`, `TextRunStyle`, `CursorShape`)
- `src/LoomTerminal.Presentation/Rendering/RenderFrameBuilder.cs` — create (internal)
- `src/LoomTerminal.Presentation/Rendering/ViewState.cs` — create (internal: what the UI decides and the builder needs —
  focus, selection, cursor blink phase, search highlights; filled by later cards)
- `tests/LoomTerminal.Presentation.Tests/Rendering/RenderFrameBuilderTests.cs` — create

## Design decisions (already made — do not change)

- `RenderFrame`: `Columns`, `Rows`, `IReadOnlyList<RenderRow> Lines` (viewport only), `RenderCursor? Cursor`,
  `uint Background` (theme background), `int ViewportY` (`ydisp`), `int ScrollbackLength` (`ybase`),
  `bool IsAlternateBuffer`, `ulong Version` (incremented per frame), `IReadOnlySet<int> DirtyRows` (rows changed since
  the previous frame: from `InputHandler.OnRequestRefreshRows` ranges accumulated between frames, all rows when the
  viewport scrolled or resized).
- `RenderRow`: `IReadOnlyList<RenderRun> Runs`, `bool IsWrapped`, `int ContentHash` (stable hash of the runs, used by
  backends to cache text layouts).
- `RenderRun`: `int Column`, `int CellCount`, `string Text` (the cells' strings concatenated; a wide char counts 2
  cells, 1 string), `TextRunStyle Style`. Runs split on any style change **and** on wide characters' boundaries
  only if the style changes (a wide char does not force a split). Empty trailing cells with the default background
  produce no run.
- `TextRunStyle` (record struct): `Foreground`, `Background` (RGBA `uint`), `Bold`, `Italic`, `Dim`, `Underline`
  (`UnderlineStyle`), `UnderlineColor` (`uint?`), `Strikethrough`, `Overline`, `Inverse` already applied (not a field),
  `IsLink` (OSC 8 or detected link under hover, P3-17), `Highlight` (`enum { None, SearchMatch, ActiveSearchMatch }`).
- `RenderCursor`: `Column`, `Row` (viewport-relative), `CursorShape` (`Block`, `Underline`, `Bar`, `Outline` for the
  inactive block), `Visible` (after blink and DECTCEM), `uint Color`, `uint AccentColor`, `string CellText`.
- Rows are built with `Buffer.Lines.Get(ydisp + y)` and `BufferLine.LoadCell` + `CellColorResolver`.

## Tests

- [ ] New: `hello` → one run; `ab\e[31mcd` → two runs; a wide char + ASCII with the same style → one run of 3 cells
      and 2 chars; dirty rows after writing to row 2 only = `{2}`; after a resize = all rows; cursor hidden by
      `\e[?25l` → `Visible = false`; `ContentHash` equal for equal rows.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Public API files updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Selection and search overlays (P3-14, P3-19), cursor blink (P3-11).

## Suggested commit message

`feat(presentation): add the immutable render frame and its builder`
