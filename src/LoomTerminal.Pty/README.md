# LoomTerminal.Pty

Pseudo terminal for LoomTerminal: Windows ConPTY (macOS and Linux planned), Native AOT-compatible.

## Install

```powershell
dotnet add package LoomTerminal.Pty --prerelease
```

`--prerelease` is needed while the version has a suffix (for example `0.1.0-alpha.1`).

Most applications want `LoomTerminal.WinUI`, which brings the other packages.

## Usage

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

## Supported platforms

- Windows 10 1809 and later (ConPTY).
- macOS and Linux (`openpty`): planned.

Source and documentation: <https://github.com/adgranger/LoomTerminal>
