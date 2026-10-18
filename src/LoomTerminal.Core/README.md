# LoomTerminal.Core

The terminal emulator, with no UI: bytes from the pseudo terminal in, screen state out.

Content (phase 1, ported from xterm.js `src/common`; the entry point is `TerminalCore`, driven by `TerminalWriter`):

| Folder | xterm.js source |
|---|---|
| `Parser/` | `parser/EscapeSequenceParser.ts`, `Params.ts`, `OscParser.ts`, `DcsParser.ts` |
| `InputHandling/` | `InputHandler.ts` (partial files by concern) |
| `Input/` | `input/TextDecoder.ts`, `UnicodeV6.ts`, `XParseColor.ts`, unicode11 addon |
| `Buffer/` | `buffer/Buffer.ts`, `BufferLine.ts`, `CellData.ts`, `AttributeData.ts`, `BufferReflow.ts` |
| `Common/` | `CircularList.ts`, `Color.ts`, `Disposable`, `Emitter` |
| `Data/` | `data/Charsets.ts`, `EscapeSequences.ts` |
| `Services/` | buffer, core, mouse, charset, Unicode, options, OSC link services |
| root | `TerminalCore.cs` (`CoreTerminal.ts`), `WindowsMode.cs`, `TerminalWriter.cs` (replaces `WriteBuffer.ts`) |

Rules: every ported file starts with the two-line header described in `docs/PORTING.md`; port the tests first.
