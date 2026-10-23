# LoomTerminal

**A native terminal control for WinUI 3, with Avalonia next: a C# terminal emulator ported from xterm.js.**

- **Native**: no WebView2, no JavaScript. C# all the way, drawn with DirectWrite (through Win2D) on Windows.
- **Native AOT compatible**: no run-time reflection; CI publishes the samples with `PublishAot=true` and zero warnings.
- **Portable core**: the emulator (`LoomTerminal.Core`) and the presentation layer run headless on any .NET 11
  platform; a UI backend only draws and forwards input. WinUI 3 first, Avalonia (Windows, macOS, Linux) second.
- **xterm.js, ported faithfully**: same parser state machine, same buffer model, xterm.js tests ported.
- **MIT licensed.** Sibling project of [LoomEditor](https://github.com/adgranger/LoomEditor) (Monaco for WinUI 3).

> **Status: `0.1.0-alpha`.** `LoomTerminal.Pty` (Windows ConPTY), the emulator core (`LoomTerminal.Core`, ported from
> xterm.js and checked against it), the presentation layer and the WinUI 3 control (`LoomTerminal.WinUI`, see its
> [README](src/LoomTerminal.WinUI/README.md)) are written; the Gallery sample runs real shells in tabs. Packages are
> published to nuget.org when a version tag is pushed (see [docs/RELEASING.md](docs/RELEASING.md)); no version is
> published yet. See [docs/ROADMAP.md](docs/ROADMAP.md).

<!-- TODO: screenshot of the Gallery (docs/images/gallery.png), to be taken by hand: ![Gallery](docs/images/gallery.png) -->

## Packages

| Package | Target | Content |
|---|---|---|
| `LoomTerminal.Core` | `net11.0` | VT parser, escape sequence handlers, buffers, scrollback, reflow |
| `LoomTerminal.Pty` | `net11.0` | Start a shell behind a pseudo terminal (ConPTY; openpty planned) and detect installed shells |
| `LoomTerminal.Presentation` | `net11.0` | Render model, key/mouse encoding, selection, search: shared by every UI backend |
| `LoomTerminal.WinUI` | `net11.0-windows` | The WinUI 3 control and its Win2D renderer |
| `LoomTerminal.Avalonia` | `net11.0` | *(planned)* The Avalonia control and its Skia renderer |

## Install (from the first release)

Packages: `LoomTerminal.Core`, `LoomTerminal.Pty`, `LoomTerminal.Presentation`, `LoomTerminal.WinUI`.

```powershell
dotnet add package LoomTerminal.WinUI --prerelease
```

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
