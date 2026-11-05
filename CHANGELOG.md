# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html). Versions are published when a `vX.Y.Z` tag is pushed, see
[docs/RELEASING.md](docs/RELEASING.md).

## [Unreleased]

## [0.2.0] - 2026-11-05

### Added

- `LoomTerminal.Avalonia`: new package, the terminal control for Avalonia (`TerminalView`) drawn with Skia, with keyboard, IME, pointer, clipboard, scrollbar and accessibility support. Native AOT compatible, checked by CI on Windows, Linux and macOS.
- `LoomTerminal.Pty`: Unix pseudo terminal (Linux and macOS) through a native `forkpty` shim shipped in the package (`runtimes/<rid>/native` for `linux-x64`, `linux-arm64`, `osx-x64` and `osx-arm64`), with shell detection.
- Cross-platform CI: build, tests and the shim smoke test on Windows, Linux and macOS.

### Changed

- The release workflow builds the Unix shim on each OS and now publishes five packages: `LoomTerminal.Core`, `.Pty`, `.Presentation`, `.WinUI` and `.Avalonia`.
- `LoomTerminal.Presentation` and `LoomTerminal.Core` need no change for Avalonia: the abstractions held.

### Notes

- The manual checklists on Linux (X11, Wayland) and macOS are not done yet ([docs/manual-tests.md](docs/manual-tests.md)).

## [0.1.0-alpha.1] - 2026-10-23

### Added

- `LoomTerminal.Core`: port of the xterm.js 6.0.0 terminal emulator (VT parser, buffers, reflow, input handler, services), with no UI dependency.
- `LoomTerminal.Pty`: Windows ConPTY pseudo terminal and shell detection.
- `LoomTerminal.Presentation`: `TerminalSession` (parser thread, frames), keyboard and pointer input encoding, theme and colors, selection, clipboard, links, search, render contracts and accessibility model.
- `LoomTerminal.WinUI`: native WinUI 3 `TerminalControl` drawn with Win2D (no WebView2), custom glyphs and decorations, IME, UI Automation, scrollbar, theming and Windows Terminal font lookup.
- All four packages are Native AOT compatible, checked by CI.
- Windows only for now (Windows 10 build 26100 or later for the WinUI control).
