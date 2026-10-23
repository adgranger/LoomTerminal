# LoomTerminal — instructions for coding agents

LoomTerminal is a native, Native AOT-compatible terminal control for WinUI 3 (Avalonia next) on .NET 11, with an
emulator ported from xterm.js (tag 6.0.0, commit `f447274f430fd22513f6adbf9862d19524471c04`).
Read `docs/ARCHITECTURE.md` once; `docs/ROADMAP.md` says what each phase delivers.

## How work is organized

- Work is split into **task cards**: `docs/tasks/phase-<n>/P<n>-<nn>-<name>.md`. Index: `docs/tasks/README.md`.
  Each phase has a conventions file, `docs/tasks/phase-<n>/README.md`: **read it before any card of that phase**.
- **Do one task card at a time**, and only what the card says, with the `/next-task` skill
  (`.claude/skills/next-task/SKILL.md`). Decisions listed under "Design decisions (already made)" are final. If a card
  is ambiguous or wrong, stop and ask; do not redesign.
- When a card is done: tick its checkboxes, set `Status: done`, regenerate the index
  (`rtk proxy python -I eng/update-task-index.py docs/tasks`), update `docs/PORTING.md` if a file was ported.

## Rules

- **Every shell command goes through `rtk`** (`rtk dotnet build`, `rtk test dotnet test …`, `rtk git status`;
  `rtk proxy <cmd>` when the raw output is needed). `rtk dotnet test` breaks the Microsoft.Testing.Platform
  handshake: always `rtk test dotnet test …`.
- Everything written in the repository is in **English**: code, comments, docs, test names, commit messages.
- **Commits:** never commit, never stage, never push, never create branches. Propose ONE commit message
  (Conventional Commits, English, **no trailer**: no `Co-Authored-By`, no "Generated with").
- Before a structural choice not covered by an ADR or a card: present the options and wait for a decision.

## Commands (repository root)

```powershell
rtk dotnet build LoomTerminal.slnx -c Release                 # MUST pass: warnings are errors in Release
rtk test dotnet test --solution LoomTerminal.slnx -c Release  # MUST pass
rtk test dotnet test --project tests/LoomTerminal.Core.Tests -c Release   # faster loop on Core
rtk proxy pwsh -NoProfile -File eng/Update-PublicApi.ps1 src/LoomTerminal.Pty   # after adding PUBLIC members (prefer internal!)
rtk dotnet publish samples/LoomTerminal.Gallery -c Release -r win-x64   # Native AOT check (needs the VS C++ workload)
rtk proxy pwsh -NoProfile -File eng/Test-AotApp.ps1 samples/LoomTerminal.Gallery   # AOT publish + start + crash check
rtk proxy pwsh -NoProfile -File eng/Invoke-ReleaseBuild.ps1 -Version 0.1.0-alpha.1   # every release gate, local dry run
rtk proxy pwsh -NoProfile -File eng/Get-ReleaseVersion.ps1 -SelfTest   # also eng/Get-ReleaseNotes.ps1 -SelfTest
```

A card is done only when the Release build has **0 warnings** and **all tests pass**.

## Porting rules (details: `docs/PORTING.md` and `docs/tasks/phase-1/README.md`)

- Read xterm.js at the pinned commit:
  `https://raw.githubusercontent.com/xtermjs/xterm.js/f447274f430fd22513f6adbf9862d19524471c04/<path>`.
  Cards give exact files and line numbers. Do not port from memory.
- Header of every ported file (checked by an architecture test):
  ```csharp
  // Ported from xtermjs/xterm.js: src/common/parser/Params.ts
  // Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.
  ```
- **Port the tests first, then the code.** Keep the xterm.js test name in a comment above each test.
- Translate faithfully; keep xterm.js algorithms and names. Do not optimize while porting.
- Handlers are synchronous (no `Promise` handlers, ADR 0002); async test suites are not ported.

## Code rules

- `internal` by default; XML doc on every public member, and a threading note on every class (`<remarks>Threading: ...</remarks>`).
- File-scoped namespaces; namespace = project + folder. Braces always; private fields `_camelCase`;
  private constants and `static readonly` fields `PascalCase`. ~400 lines max per file (`partial` files by concern).
- No reflection, no `dynamic`, no `Activator`, no reflection-based JSON; `[LibraryImport]` only (Native AOT).
- Core, Pty and Presentation never use Windows or UI APIs, except `LoomTerminal.Pty/Windows/` (checked by tests).

## Tests

xUnit v3 on Microsoft.Testing.Platform, `Assert` only. Test names are sentences (`Resize_is_seen_by_the_shell`).
Test projects mirror `src/`. Pty tests start real shells behind a real ConPTY.
