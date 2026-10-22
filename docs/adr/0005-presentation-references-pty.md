# ADR 0005: Presentation references Pty

- Status: accepted
- Date: 2026-10-22

## Context

`TerminalSession` (phase 3) wires the pty output to the parser thread and the input to the pty. Keeping the pty
outside Presentation would force every backend to start shells and plumb the connection by hand.

## Decision

`LoomTerminal.Presentation` may reference `LoomTerminal.Pty`. The session owns the pty: it starts a shell itself
(`TerminalSession.Start(PtyStartOptions)`) or attaches to any `IPtyConnection`.

## Consequences

- Dependency rules (`docs/ARCHITECTURE.md`, architecture tests) allow `Presentation → Pty`; Core still references nothing.
- Presentation stays free of UI and Windows APIs: only `Pty/Windows/` touches the OS.
- Backends no longer need a direct Pty reference unless they use it themselves.
