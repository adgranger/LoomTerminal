# ADR 0001: Baseline

- Status: accepted
- Date: 2026-10-16

## Context

VIGO (a WinUI 3 Git client) has an integrated terminal made of xterm.js in a WebView2 on top of a ConPTY session.
We want a native, Native AOT-compatible terminal control, published as open source like LoomEditor, for WinUI 3
first and Avalonia second.

## Decisions

1. **Emulator ported from xterm.js** (`src/common` and its tests), not written from scratch (VT edge cases are
   endless) and not wrapped from Windows Terminal (C++ interop would lose the "pure C#, AOT" goal; Windows
   Terminal stays a behavior reference).
2. **Separate repository, not a fork of LoomEditor.** An editor model (piece tree, tokens, one color per line) does
   not fit a terminal grid (per-cell attributes, ring buffer). We reuse LoomEditor's *tooling*: build props,
   analyzers, banned APIs, public API tracking, architecture tests, AOT CI. A shared text-rendering library may be
   extracted later, once both projects have a stable renderer (font metrics, DPI and device-loss handling, IME,
   caret blinking are the candidates).
3. **.NET 11 only** (`net11.0`, RC until GA), same SDK pin as VIGO and LoomEditor. No multi-targeting. Moving to
   .NET 12 (LTS) is a one-line change in `Directory.Build.props`.
4. **Four projects**: Core, Pty, Presentation, WinUI (see ARCHITECTURE.md). Pty is plain `net11.0` with OS folders,
   so the Avalonia backend can use it on every OS.
5. **Win2D for the WinUI renderer**, like LoomEditor (ADR 0002 there). Skia for Avalonia. A glyph atlas renderer
   is only considered if measurements show Win2D text layouts are too slow.
6. **Windows App SDK 2.5.1, Win2D 1.4.0**, Windows 10 2004 (19041) minimum, Windows SDK 26100.

## Known issues

- **AOT publish drops the app's PRI file** (Windows App SDK 2.5.1, .NET 11 RC1 and rc.2): the app crashes with "Cannot
  locate resource from 'ms-appx:///MainWindow.xaml'". Worked around by
  `src/LoomTerminal.WinUI/buildTransitive/LoomTerminal.WinUI.targets` (same as LoomEditor). Delete it once fixed.
- **Local AOT publish needs the Visual Studio C++ workload** ("Desktop development with C++", component
  `Microsoft.VisualStudio.Component.VC.Tools.x86.x64`). Without it, ILCompiler fails with "vswhere.exe failed to
  locate Visual Studio". CI runners have it.
- **Optimized AOT publish fails with WMC1007** (.NET SDK 11 rc.2): `PrepareResources` runs before
  `ResolveReferences`. `UseOptimizedPublish=false` in the Gallery csproj works around it. Remove when fixed.
