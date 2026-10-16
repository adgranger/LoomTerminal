# LoomTerminal

**A native terminal control for WinUI 3, with Avalonia next: a C# terminal emulator ported from xterm.js.**

- **Native**: no WebView2, no JavaScript. C# all the way, drawn with DirectWrite (through Win2D) on Windows.
- **Native AOT compatible**: no run-time reflection; CI publishes the samples with `PublishAot=true` and zero warnings.
- **Portable core**: the emulator (`LoomTerminal.Core`) and the presentation layer run headless on any .NET 11
  platform; a UI backend only draws and forwards input. WinUI 3 first, Avalonia (Windows, macOS, Linux) second.
- **xterm.js, ported faithfully**: same parser state machine, same buffer model, xterm.js tests ported.
- **MIT licensed.** Sibling project of [LoomEditor](https://github.com/adgranger/LoomEditor) (Monaco for WinUI 3).

> **Status: skeleton (pre-alpha).** `LoomTerminal.Pty` works (Windows ConPTY, extracted from the VIGO Git client);
> the emulator and the renderer are not written yet. See [docs/ROADMAP.md](docs/ROADMAP.md).

## Packages

| Package | Target | Content |
|---|---|---|
| `LoomTerminal.Core` | `net11.0` | VT parser, escape sequence handlers, buffers, scrollback, reflow |
| `LoomTerminal.Pty` | `net11.0` | Start a shell behind a pseudo terminal (ConPTY; openpty planned) and detect installed shells |
| `LoomTerminal.Presentation` | `net11.0` | Render model, key/mouse encoding, selection, search: shared by every UI backend |
| `LoomTerminal.WinUI` | `net11.0-windows` | The WinUI 3 control and its Win2D renderer |
| `LoomTerminal.Avalonia` | `net11.0` | *(planned)* The Avalonia control and its Skia renderer |

## Using the pseudo terminal today

```csharp
using LoomTerminal.Pty;

ShellInfo shell = Shells.Detect()[0];                     // PowerShell 7, Windows PowerShell, Git Bash, cmd, WSL…
using IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions(shell.CommandLine)
{
    Columns = 120,
    Rows = 30,
    Environment = new Dictionary<string, string?> { ["TERM_PROGRAM"] = "MyApp" },
});

pty.Write("dir\r"u8);
await foreach (byte[] chunk in pty.Output.ReadAllAsync()) { /* VT bytes for the emulator */ }
```

## Building

Requirements: Windows 10 1809+, the .NET SDK pinned in `global.json` (11.0 RC), and for Native AOT publishing the
Visual Studio "Desktop development with C++" workload.

```powershell
dotnet build LoomTerminal.slnx -c Release            # warnings are errors in Release
dotnet test --solution LoomTerminal.slnx -c Release
dotnet run --project samples/LoomTerminal.PtyConsole  # a shell through LoomTerminal.Pty, inside this console
dotnet publish samples/LoomTerminal.Gallery -c Release -r win-x64   # Native AOT
```

## Documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md): projects, dependency rules, Native AOT rules.
- [docs/ROADMAP.md](docs/ROADMAP.md): phases, and the feature parity list with VIGO's xterm.js terminal.
- [docs/PORTING.md](docs/PORTING.md): how xterm.js is ported.
- [docs/adr/](docs/adr/): architecture decisions.

## License

MIT, see [LICENSE](LICENSE). Third-party notices (xterm.js): [NOTICE.md](NOTICE.md).
