# P1-15 — Buffer constants, `CharData`, C0/C1 control codes

- **Status:** todo
- **Depends on:** P1-01
- **Size:** S

## Goal

Port the bit layouts of cell content and attributes, the legacy `CharData` tuple, and the C0/C1 control code tables.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/Constants.ts` | 1–157 | `DEFAULT_COLOR`, `DEFAULT_ATTR`, `DEFAULT_EXT`, `CHAR_DATA_*`, `NULL_CELL_*`, `WHITESPACE_CELL_*`, `Content`, `Attributes`, `FgFlags`, `BgFlags`, `ExtFlags`, `UnderlineStyle` |
| `src/common/Types.ts` | 104 | `CharData` (`[number, string, number, number]`) |
| `src/common/data/EscapeSequences.ts` | 1–153 | `C0`, `C1`, `C1_ESCAPED` |

## Files to create or change

- `src/LoomTerminal.Core/Buffer/BufferConstants.cs` — create
- `src/LoomTerminal.Core/Buffer/CharData.cs` — create
- `src/LoomTerminal.Core/Data/EscapeSequences.cs` — create
- `tests/LoomTerminal.Core.Tests/Buffer/BufferConstantsTests.cs` — create

## Design decisions (already made — do not change)

- Bit masks are `uint` constants in static classes named like the enums: `static class Content { public const uint
  CodepointMask = 0x1FFFFF; ... }`, `Attributes`, `FgFlags`, `BgFlags`, `ExtFlags`. Names in PascalCase
  (`CM_MASK` → `CmMask`, `IS_COMBINED_MASK` → `IsCombinedMask`), values copied exactly.
- `UnderlineStyle` is a real enum (`None`, `Single`, `Double`, `Curly`, `Dotted`, `Dashed`, values as in JS).
- `CharData` → `internal readonly record struct CharData(uint Attr, string Char, int Width, uint Code)`.
- `C0`, `C1`, `C1_ESCAPED` → static classes of `const string` (`C0.ESC = "\x1b"` → `C0.Esc = "\u001b"`); also add
  `C0Code`/`C1Code` static classes with the same names as `const int` codes, used by execute handlers.

## Tests

- [ ] New: a few values checked against the JS file (`Attributes.CmRgb`, `FgFlags.Inverse`, `BgFlags.Italic`,
      `DEFAULT_ATTR == 256 | (0 << 9)`, `C0.Esc == "\u001b"`, `C1.Csi == "\u009b"`).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (✅ P1-15)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`AttributeData`, `CellData` (P1-16, P1-17).

## Suggested commit message

`feat(core): port the buffer constants and control codes`
