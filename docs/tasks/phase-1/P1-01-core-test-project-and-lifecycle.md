# P1-01 — Core test project, `Disposable`, `Emitter`, ported-file header rule

- **Status:** done
- **Depends on:** —
- **Size:** S

## Goal

Create `tests/LoomTerminal.Core.Tests`, the two lifecycle helpers every later card uses (`Disposable`, `Emitter<T>`),
and the architecture test that checks the header of ported files.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | What to take |
|---|---|
| `src/vs/base/common/lifecycle.ts` | Ideas only: `Disposable` with `_register`, `toDisposable`, `MutableDisposable`, `DisposableStore` |
| `src/vs/base/common/event.ts` | Ideas only: `Emitter<T>` with `event` and `fire` |

These files are large (VS Code base library vendored by xterm.js). **Do not port them**: write the small API below
and mark both files `// Inspired by xtermjs/xterm.js: src/vs/base/common/lifecycle.ts` (resp. `event.ts`).

## Files to create or change

- `tests/LoomTerminal.Core.Tests/LoomTerminal.Core.Tests.csproj` — create (copy the shape of
  `tests/LoomTerminal.Pty.Tests/LoomTerminal.Pty.Tests.csproj`, reference `src/LoomTerminal.Core`)
- `LoomTerminal.slnx` — add the test project under `/tests/`
- `tests/LoomTerminal.Architecture.Tests/LoomTerminal.Architecture.Tests.csproj` — nothing to add (Core is already referenced)
- `src/LoomTerminal.Core/Common/Disposable.cs` — create
- `src/LoomTerminal.Core/Common/Emitter.cs` — create
- `tests/LoomTerminal.Core.Tests/Common/DisposableTests.cs`, `EmitterTests.cs` — create
- `tests/LoomTerminal.Architecture.Tests/PortedFileHeaderTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 1 conventions](README.md) first.
- `internal abstract class Disposable : IDisposable` with:
  `protected T Register<T>(T disposable) where T : IDisposable` (disposed with the owner, in reverse order),
  `public bool IsDisposed { get; }`, `public void Dispose()` (idempotent), `protected virtual void DisposeCore()` (called once, after the registered disposables).
- `internal static class Disposables` with `IDisposable Create(Action onDispose)` (port of `toDisposable`: runs the
  action once) and `IDisposable Empty`.
- `internal sealed class DisposableStore : IDisposable` with `Add`, `Clear` (disposes and empties), `Dispose`.
- `internal sealed class MutableDisposable<T> : IDisposable where T : class, IDisposable` with `Value` (setting a new
  value disposes the previous one) and `Clear()`.
- `internal interface IEvent<T> { IDisposable Subscribe(Action<T> listener); }` and
  `internal sealed class Emitter<T> : IDisposable` with `IEvent<T> Event`, `void Fire(T value)`, `Dispose()`
  (removes all listeners; `Fire` after dispose does nothing). Listeners are called in subscription order; a listener
  removed during `Fire` is not called afterwards; a listener added during `Fire` is called from the next `Fire` on.
  Use a copy-on-write array, no locks (Core is single-threaded, ADR 0002).
- There is no non-generic `Emitter`. When xterm.js fires an event without payload (`Emitter<void>`), use
  `Emitter<EventArgs>` and fire `EventArgs.Empty`.
- `PortedFileHeaderTests`: every `*.cs` under `src/` and `tests/` (excluding `bin/` and `obj/`) whose first line
  starts with `// Ported from xtermjs/xterm.js:` or `// Inspired by xtermjs/xterm.js:` must have a second line
  starting with `// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04`. Copy the shape of LoomEditor's
  `ConventionTests.PortedFiles_StateTheUpstreamCommit`, and use `RepositoryLayout.Root`.

## Tests

- [x] New: disposing a `Disposable` disposes registered objects once, in reverse registration order; a second
      `Dispose` does nothing; `Register` after dispose disposes the argument immediately.
- [x] New: `Disposables.Create` runs its action once.
- [x] New: `MutableDisposable.Value` setter disposes the previous value; `Clear` disposes and sets null.
- [x] New: `Emitter` calls listeners in order; unsubscribing (disposing the subscription) stops calls; unsubscribe
      during `Fire`; subscribe during `Fire`; `Fire` after `Dispose` is a no-op.
- [x] New: `PortedFileHeaderTests` passes on the repository (and fails if you temporarily break a header — check by hand once).

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Any xterm.js class. `DisposableStore` features beyond `Add`/`Clear`/`Dispose`.

## Suggested commit message

`feat(core): add the core test project, lifecycle helpers and the ported-file header rule`
