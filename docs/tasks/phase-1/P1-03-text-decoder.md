# P1-03 — Text decoders: `StringToUtf32`, `Utf8ToUtf32`, `Utf32ToString`

- **Status:** done
- **Depends on:** P1-01
- **Size:** S

## Goal

Port the stateful decoders that turn input (UTF-8 bytes from the pty, or strings) into UTF-32 code points for the
parser, keeping incomplete sequences between chunks.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/input/TextDecoder.ts` | 1–346 | `stringFromCodePoint`, `utf32ToString`, `StringToUtf32` (`clear`, `decode`), `Utf8ToUtf32` (`clear`, `decode`) |
| `src/common/input/TextDecoder.test.ts` | 1–274 | all tests |

## Files to create or change

- `src/LoomTerminal.Core/Input/TextDecoder.cs` — create (`internal static class TextDecoder` for the two functions,
  plus the two decoder classes in the same file or in `StringToUtf32.cs` / `Utf8ToUtf32.cs`)
- `tests/LoomTerminal.Core.Tests/Input/TextDecoderTests.cs` — create

## Design decisions (already made — do not change)

- `int Decode(string input, uint[] target)` and `int Decode(ReadOnlySpan<byte> input, uint[] target)`: return the
  number of code points written, exactly as xterm.js. Keep the internal state fields (`_interim`, `interim`
  `Uint8Array(3)` → `byte[3]`).
- `Utf32ToString(ReadOnlySpan<uint> data, int start = 0, int end = -1)`: `-1` means `data.Length`
  (JS default `data.length`).
- **Do not use `System.Text.Decoder`**: port the algorithm, including its handling of invalid sequences, so the
  ported tests keep their exact expectations.

## Tests

- [x] Port `text encodings` › `StringToUtf32 decoder` (full codepoint test, stream handling) and
      `Utf8ToUtf32 decoder` (full codepoint test, stream handling).
- [x] The "full codepoint test" loops over all code points: keep it, but run it in a single `[Fact]` (it must stay
      under ~2 s in Release; if not, split the range into a `[Theory]` with 16 slices).

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (✅ P1-03)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`WriteBuffer.ts` (replaced by `TerminalWriter`, P1-56).

## Suggested commit message

`feat(core): port the UTF-8 and UTF-16 to UTF-32 decoders`
