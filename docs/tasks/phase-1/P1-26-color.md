# P1-26 — `Color` helpers

- **Status:** done
- **Depends on:** P1-01
- **Size:** M

## Goal

Port the color helpers used by OSC color reports, the theme and contrast handling: channels, `IColor`, CSS parsing,
blending, luminance, contrast ratio.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/Color.ts` | 1–376 | `NULL_COLOR`, namespaces `channels`, `color`, `css`, `rgb`, `rgba`, `toPaddedHex`, `contrastRatio` |
| `src/common/Types.ts` | 106–110 | `IColor`, `IColorRGB` |
| `src/common/Color.test.ts` | 1–464 | all tests |

## Files to create or change

- `src/LoomTerminal.Core/Common/Color.cs` — create: `readonly record struct TerminalColor(string Css, uint Rgba)`
  (port of `IColor`) and static classes `Channels`, `ColorOps` (JS namespace `color`), `Css`, `Rgb`, `Rgba`, `ColorMath`
  (`ToPaddedHex`, `ContrastRatio`)
- `tests/LoomTerminal.Core.Tests/Common/ColorTests.cs` — create

## Design decisions (already made — do not change)

- `rgba` is a `uint` (`r << 24 | g << 16 | b << 8 | a`, JS `>>> 0`).
- `css.toColor` in JS falls back to a canvas (`document.createElement('canvas')`) for named colors and other CSS
  syntaxes. **Port only the branches that do not need the canvas** (hex and `rgb()/rgba()`); any other input throws
  `ArgumentException("css.toColor: Unsupported css format")`, the message JS throws when no canvas context exists
  (`Color.ts` line 189). List the JS tests that need a canvas as "not ported".
- The `$r`, `$g`, `$b`, `$a` module-level scratch variables become locals.

## Tests

- [x] Port every `Color.test.ts` test except those that need the canvas fallback.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (✅ P1-26) and "Behavior differences": "CSS color names are not parsed"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Theme resolution (phase 3).

## Suggested commit message

`feat(core): port the color helpers`
