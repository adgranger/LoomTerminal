# P1-23 — `TerminalOptions` and `OptionsService`

- **Status:** done
- **Depends on:** P1-01
- **Size:** M

## Goal

Port the options the emulator reads, as an immutable record, and the service that holds them, validates changes and
notifies listeners — without reflection (ADR 0002, decision 4).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/services/OptionsService.ts` | 12–61 | `DEFAULT_OPTIONS` (Core subset, below) |
| | 64–212 | `OptionsService`: `onOptionChange`, `onSpecificOptionChange`, `onMultipleOptionChange`, `_sanitizeAndValidateOption`, `isCursorStyle` |
| `src/common/services/Services.ts` | 181–275 | `IOptionsService`, `ITerminalOptions` (reference for types and docs) |
| `src/common/Types.ts` | 387–412 | `IWindowOptions` |
| `src/common/services/OptionsService.test.ts` | 1–150 | all tests that apply to the Core subset |

## Files to create or change

- `src/LoomTerminal.Core/Services/TerminalOptions.cs` — create (`TerminalOptions`, `WindowOptions`, `WindowsPtyOptions`,
  `CursorStyle`, `CursorInactiveStyle`, `LogLevel`, `FastScrollModifier`)
- `src/LoomTerminal.Core/Services/TerminalOption.cs` — create (`enum TerminalOption`, one value per property)
- `src/LoomTerminal.Core/Services/IOptionsService.cs`, `OptionsService.cs` — create
- `tests/LoomTerminal.Core.Tests/Services/OptionsServiceTests.cs` — create

## Design decisions (already made — do not change)

- **Core subset** of `ITerminalOptions` (others are UI options and live in Presentation, phase 3):
  `Cols`, `Rows`, `CursorBlink`, `CursorStyle`, `CursorInactiveStyle`, `CursorWidth`, `DrawBoldTextInBrightColors`,
  `FastScrollModifier`, `FastScrollSensitivity`, `IgnoreBracketedPasteMode`, `LogLevel`, `MacOptionIsMeta`,
  `MinimumContrastRatio`, `DisableStdin`, `RightClickSelectsWord`, `ScreenReaderMode`, `Scrollback`,
  `ScrollOnEraseInDisplay`, `ScrollOnUserInput`, `ScrollSensitivity`, `TabStopWidth`, `ReflowCursorLine`,
  `WindowOptions`, `WindowsMode`, `WindowsPty`, `WordSeparator`, `AltClickMovesCursor`, `ConvertEol`, `TermName`.
  Defaults are xterm.js `DEFAULT_OPTIONS`, except `RightClickSelectsWord = false` (JS: `isMac`, we are on Windows).
- `sealed record TerminalOptions` with init-only properties; `static TerminalOptions Default { get; }`.
  `WindowOptions` is a sealed record of `bool` properties named like `IWindowOptions` (`RestoreWin`, `MinimizeWin`,
  …, `SetWinLines`). `WindowsPtyOptions` is a sealed record `(string? Backend, int? BuildNumber)`.
- `IOptionsService`: `TerminalOptions RawOptions { get; }`, `TerminalOptions Options => RawOptions`,
  `void SetOptions(TerminalOptions value)` (validates every property, then raises `OnOptionChange` once per property
  whose value changed, in `TerminalOption` declaration order), `IEvent<TerminalOption> OnOptionChange`,
  `IDisposable OnSpecificOptionChange(TerminalOption key, Action listener)`,
  `IDisposable OnMultipleOptionChange(IReadOnlyList<TerminalOption> keys, Action listener)`.
  Listeners read the new value from `RawOptions` (no generic per-option payload: keeps it reflection-free).
- Validation ports `_sanitizeAndValidateOption` (lines 149–209) for the subset, with the JS message texts in
  `ArgumentException`s: `CursorStyle` (an enum here, so only the "empty → default" rule remains), `WordSeparator`
  (empty → default), `CursorWidth` (floored, then `< 1` throws like `TabStopWidth`), `TabStopWidth < 1` throws,
  `MinimumContrastRatio` clamped to `[1, 21]` and rounded to one decimal, `Scrollback` capped at `4294967295` and
  `< 0` throws, `FastScrollSensitivity` / `ScrollSensitivity <= 0` throw, `WindowsPty` null → empty record.
  `Scrollback` is a `long` (JS number up to 2^32 − 1).
- No property names as strings anywhere (AOT). Change detection compares property by property in a hand-written
  method (`static IEnumerable<TerminalOption> Diff(TerminalOptions a, TerminalOptions b)`).

## Tests

- [x] Port `OptionsService` tests (`constructor`, `setOption`, `onOptionChange`, `onSpecificOptionChange`,
      `onMultipleOptionChange`) for options in the subset; JS tests about UI options (font, theme) are skipped and
      listed in the card's PR description. `setOption('x', v)` becomes `SetOptions(service.RawOptions with { X = v })`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (✅ P1-23) and "Behavior differences": "options are an immutable record; UI options live in Presentation"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

UI options (`fontFamily`, `fontSize`, `lineHeight`, `letterSpacing`, `theme`, …): phase 3.

## Suggested commit message

`feat(core): add TerminalOptions and port OptionsService`
