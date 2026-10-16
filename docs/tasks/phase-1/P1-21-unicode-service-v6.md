# P1-21 — `UnicodeService` and `UnicodeV6`

- **Status:** todo
- **Depends on:** P1-01
- **Size:** S

## Goal

Port the Unicode width service (provider registry, `wcwidth`, char properties with joining state, string cell
width) and the built-in Unicode 6 provider.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/services/Services.ts` | 336–376 | `UnicodeCharProperties`, `UnicodeCharWidth`, `IUnicodeService`, `IUnicodeVersionProvider` |
| `src/common/services/UnicodeService.ts` | 1–111 | `UnicodeService` (static `extractShouldJoin`, `extractWidth`, `extractCharKind`, `createPropertyValue`; `versions`, `activeVersion`, `register`, `wcwidth`, `getStringCellWidth`, `charProperties`, `onChange`) |
| `src/common/input/UnicodeV6.ts` | 1–145 | `BMP_COMBINING`, `HIGH_COMBINING`, `bisearch`, `UnicodeV6` |
| `src/common/services/UnicodeService.test.ts` | 1–53 | `unicode provider` |
| `src/common/input/UnicodeV6.test.ts` | 1–185 | `wcwidth should match all values from the old implementation` |

## Files to create or change

- `src/LoomTerminal.Core/Services/IUnicodeService.cs`, `IUnicodeVersionProvider.cs` — create
- `src/LoomTerminal.Core/Services/UnicodeService.cs` — create
- `src/LoomTerminal.Core/Input/UnicodeV6.cs` — create
- `tests/LoomTerminal.Core.Tests/Services/UnicodeServiceTests.cs`, `tests/LoomTerminal.Core.Tests/Input/UnicodeV6Tests.cs` — create

## Design decisions (already made — do not change)

- `UnicodeCharProperties` is an `int` (packed, as JS); `UnicodeCharWidth` is an `int` 0, 1 or 2 (no enum: the
  value is arithmetic).
- `IUnicodeService.OnChange` is an `IEvent<string>` (P1-01).
- The width table of `UnicodeV6` is built once into a `static readonly byte[] Table` of 65536 entries, as the JS
  constructor does (a static constructor, not per instance).
- `UnicodeService` constructor registers `UnicodeV6` and activates `"6"`. Unicode 11 is added by P1-22.

## Tests

- [ ] Port `unicode provider` (all `it`s).
- [ ] Port the `UnicodeV6` test: the "old implementation" `wcwidth` in the test file is ported into the test class
      and compared for every code point `0 … 0x10FFFF` (one `[Fact]`).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (✅ P1-21)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Unicode 11 (P1-22), graphemes addon (not in phase 1).

## Suggested commit message

`feat(core): port the unicode service and the Unicode 6 width provider`
