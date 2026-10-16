# LoomTerminal.Core

The terminal emulator, with no UI: bytes from the pseudo terminal in, screen state out.

Planned content (phase 1, ported from xterm.js `src/common`, see `docs/ROADMAP.md`):

| Folder | xterm.js source |
|---|---|
| `Parser/` | `parser/EscapeSequenceParser.ts`, `Params.ts`, `OscParser.ts`, `DcsParser.ts` |
| `Input/` | `InputHandler.ts`, `input/TextDecoder.ts`, `input/UnicodeV6.ts` / unicode11 addon |
| `Buffer/` | `buffer/Buffer.ts`, `BufferLine.ts`, `CellData.ts`, `AttributeData.ts`, `BufferReflow.ts`, `CircularList.ts` |
| `Services/` | `services/CoreService.ts` (modes), `CharsetService.ts`, `OptionsService.ts` (only what Core needs) |

Rules: every ported file starts with the two-line header described in `docs/PORTING.md`; port the tests first.
