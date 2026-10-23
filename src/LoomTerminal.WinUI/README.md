# LoomTerminal.WinUI

A native WinUI 3 terminal control (Win2D drawing, no WebView2), Native AOT compatible. The control contains no
terminal logic: it forwards input to a `TerminalSession` (from `LoomTerminal.Presentation`) and draws its frames.

Status: `0.1.0-alpha`, not published to nuget.org.

## Use it in XAML

```xml
<Page xmlns:lt="using:LoomTerminal.WinUI.Controls" ...>
    <lt:TerminalControl x:Name="Terminal" CursorBlink="True" CursorStyle="Bar" LineHeight="1.15" />
</Page>
```

## Start a session

```csharp
using LoomTerminal.Pty;
using LoomTerminal.Presentation.Session;

ShellInfo shell = Shells.Detect()[0];
Terminal.Session = TerminalSession.Start(new PtyStartOptions(shell.CommandLine));

// The session owns the shell: dispose it (this kills the shell) when the tab or window closes.
Terminal.Session = null;
session.Dispose();
```

Setting `Session` attaches the control to the session (theme, cursor and size settings are applied to it). The
control resizes the session (`Columns`, `Rows`) from its own size and DPI.

## Theme and settings

`Theme` is a `TerminalTheme` (background, foreground, cursor, selection, 16 ANSI colors; `string?` CSS colors).
Assign a new instance to change it live, for example when the app switches between light and dark:

```csharp
Terminal.Theme = Terminal.Theme with { Background = "#1e1e1e", Foreground = "#d4d4d4" };
```

Other dependency properties: `CursorBlink`, `CursorStyle`, `LineHeight`.

## Fonts

`FontFamily` takes a comma-separated list ("Cascadia Mono, Segoe UI Emoji"): the first installed family draws the
grid, and each non-ASCII grapheme is drawn with the first listed family that has a glyph for it. Nerd Font icons
(oh-my-posh, starship) come from any installed Nerd Font, even when the primary family is plain Cascadia Mono; no font
is bundled, so without one they show as missing-glyph boxes. `FontWeight`, `FontSize` and `LineHeight` apply live.
Fonts installed while the app runs are seen after a restart.

## Events

All events are raised on the UI thread (they are marshaled from the session's parser thread).

| Event | Meaning |
|---|---|
| `TitleChanged` (`string`) | The shell set the window title (OSC 0/2) |
| `Exited` (`int`) | The shell exited, with its exit code |
| `BellRang` | BEL received |
| `LinkActivated` (`LinkActivatedEventArgs`) | Ctrl+click on a link; set `Handled` to open it yourself |
| `KeyPreview` (`KeyPreviewEventArgs`) | Before a key is encoded; set `Handled` to claim a shortcut |

## Packages

Four packages share one version (`VersionPrefix` + `VersionSuffix` in `Directory.Build.props`):

```text
LoomTerminal.WinUI ──> LoomTerminal.Presentation ──> LoomTerminal.Core
        │                        └─────────────────> LoomTerminal.Pty
        ├──> Microsoft.WindowsAppSDK
        └──> Microsoft.Graphics.Win2D
```

They are built into `artifacts/packages` (`.nupkg` and `.snupkg`), not published to nuget.org. To build and check them:

```powershell
./eng/Pack.ps1
```

## Native AOT notes

- Publish with `dotnet publish -c Release -r win-x64` (needs the Visual Studio "Desktop development with C++"
  workload). No reflection is used by the control.
- **PRI workaround.** With Windows App SDK 2.5.1 and `PublishAot=true`, the app's `.pri` file is dropped from the
  publish folder and the app crashes at startup ("Cannot locate resource from 'ms-appx:///MainWindow.xaml'").
  The package ships `buildTransitive/LoomTerminal.WinUI.targets`, imported automatically into every app that
  references it, which adds the PRI file back to the publish list. Nothing to do on your side; it will be
  removed once fixed upstream (see `docs/adr/0001-baseline.md`, "Known issues").
- With .NET SDK 11 rc.2, an app that publishes with `PublishAot=true` may need
  `<UseOptimizedPublish>false</UseOptimizedPublish>` (error WMC1007 otherwise, see the Gallery project).

## Known limitations

- Windows only (Avalonia is planned). IME composition needs Windows 11.
- Rendering performance has only been measured partially (`docs/performance.md`, follow-up card P4-19).
- The Gallery manual checklist (`docs/manual-tests.md`) has not been fully ticked on the AOT build yet.
