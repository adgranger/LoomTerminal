# P1-16 — `AttributeData` and `ExtendedAttrs`

- **Status:** todo
- **Depends on:** P1-15
- **Size:** S

## Goal

Port the cell attribute model: `fg`/`bg` bit fields (colors, color modes, flags) and the extended attributes
(underline style and color, URL id, underline variant offset).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/AttributeData.ts` | 1–211 | `AttributeData` (all members), `ExtendedAttrs` |
| `src/common/Types.ts` | 106–126, 136–209 | `IColor`, `IColorRGB`, `IExtendedAttrs`, `IAttributeData` (reference) |
| `src/common/buffer/BufferLine.test.ts` | 27–136 | `AttributeData` › `extended attributes` |

## Files to create or change

- `src/LoomTerminal.Core/Buffer/AttributeData.cs` — create
- `src/LoomTerminal.Core/Buffer/ExtendedAttrs.cs` — create
- `tests/LoomTerminal.Core.Tests/Buffer/AttributeDataTests.cs` — create

## Design decisions (already made — do not change)

- `internal class AttributeData` (not sealed: `CellData` derives from it) with public **fields** `Fg`, `Bg` (`uint`)
  and `Extended` (`ExtendedAttrs`), as in xterm.js. No `IAttributeData` interface.
- The `is*()` methods return `uint` (the masked bits, non-zero means true), exactly like JS returns a number:
  `public uint IsBold() => Fg & FgFlags.Bold;`. Callers compare with `!= 0`.
- `IColorRGB` → `(byte R, byte G, byte B)` tuple: `static (int R, int G, int B) ToColorRgb(uint value)` and
  `static uint FromColorRgb((int R, int G, int B) value)` (ints, as JS numbers).
- `ExtendedAttrs` is a class with `Ext`, `UnderlineStyle`, `UnderlineColor`, `UrlId`, `UnderlineVariantOffset`
  properties packing bits as in JS; `Clone()`, `IsEmpty()`.
- `DEFAULT_ATTR_DATA` (`BufferLine.ts` line 38, `Object.freeze(new AttributeData())`) becomes
  `static readonly AttributeData DefaultAttrData` on `AttributeData`. C# cannot freeze: document "never mutate".

## Tests

- [ ] Port `AttributeData` › `extended attributes` (all `it`s).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (✅ P1-16)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`CellData` (P1-17).

## Suggested commit message

`feat(core): port AttributeData and ExtendedAttrs`
