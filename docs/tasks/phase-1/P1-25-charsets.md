# P1-25 — Charsets and `CharsetService`

- **Status:** todo
- **Depends on:** P1-24
- **Size:** S

## Goal

Port the G0–G3 character set tables (DEC Special Graphics for line drawing, national charsets) and the service that
tracks the active set.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/data/Charsets.ts` | 1–256 | `CHARSETS`, `DEFAULT_CHARSET`, every table |
| `src/common/Types.ts` | 100–103 | `ICharset` |
| `src/common/services/Services.ts` | 113–135 | `ICharsetService` |
| `src/common/services/CharsetService.ts` | 1–34 | `CharsetService` |
| `src/common/TestUtils.test.ts` | 79–86 | `MockCharsetService` |

## Files to create or change

- `src/LoomTerminal.Core/Data/Charsets.cs` — create
- `src/LoomTerminal.Core/Services/ICharsetService.cs`, `CharsetService.cs` — create
- `tests/LoomTerminal.Core.Tests/TestUtils/MockCharsetService.cs` — create
- `tests/LoomTerminal.Core.Tests/Data/CharsetsTests.cs` — create

## Design decisions (already made — do not change)

- `ICharset` (`{ [key: string]: string | undefined }`) → `IReadOnlyDictionary<char, string>`; `CHARSETS` →
  `static IReadOnlyDictionary<char, IReadOnlyDictionary<char, string>?> All` keyed by the designator character
  (`'0'`, `'A'`, `'B'`, …). `DEFAULT_CHARSET` (`CHARSETS['B']`) is `null` in JS (US ASCII has no table): keep `null`.
- `CharsetService` fields `Charset` (`IReadOnlyDictionary<char, string>?`), `GLevel` (`int`), `Charsets` (array of 4).

## Tests

- [ ] New (no JS tests for this file): DEC Special Graphics (`'0'`) maps `'q'` to `"─"` and `'x'` to `"│"`;
      `setgCharset(0, All['0'])` then `setgLevel(0)` makes `Charset` that table; `Reset` restores the default.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (✅ P1-25)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Using the charsets when printing (P1-37), ESC charset handlers (P1-44).

## Suggested commit message

`feat(core): port the charsets and CharsetService`
