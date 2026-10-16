# Phase 1 conventions — read before any phase 1 card

Phase 1 ports the emulator of xterm.js (`src/common`) into `LoomTerminal.Core`. Every card relies on the rules below.
If a card contradicts this file, the card wins (it is more specific); if the code base contradicts both, stop and ask.

## Upstream

- Repository: https://github.com/xtermjs/xterm.js — tag `6.0.0`, commit **`f447274f430fd22513f6adbf9862d19524471c04`**.
- Read a file: `https://raw.githubusercontent.com/xtermjs/xterm.js/f447274f430fd22513f6adbf9862d19524471c04/<path>`
  (for example `<path>` = `src/common/parser/Params.ts`). **Never port from memory or from `master`.**
- Line numbers in cards are for this commit. If a member is not exactly at the given line, search for its name.

## Where files go (namespace = project + folder)

| xterm.js | LoomTerminal.Core | Namespace |
|---|---|---|
| `src/common/CircularList.ts`, `Color.ts`, `Lifecycle` helpers | `Common/` | `LoomTerminal.Core.Common` |
| `src/common/parser/*` | `Parser/` | `LoomTerminal.Core.Parser` |
| `src/common/buffer/*` | `Buffer/` | `LoomTerminal.Core.Buffer` |
| `src/common/input/TextDecoder.ts`, `UnicodeV6.ts`, `XParseColor.ts`, `addons/addon-unicode11/src/UnicodeV11.ts` | `Input/` | `LoomTerminal.Core.Input` |
| `src/common/data/*` | `Data/` | `LoomTerminal.Core.Data` |
| `src/common/services/*` | `Services/` | `LoomTerminal.Core.Services` |
| `src/common/InputHandler.ts` | `InputHandling/InputHandler*.cs` (partial files) | `LoomTerminal.Core.InputHandling` |
| `src/common/CoreTerminal.ts`, `WindowsMode.ts` | root | `LoomTerminal.Core` |
| `*.test.ts` | `tests/LoomTerminal.Core.Tests/<same folder>/<Name>Tests.cs` | `LoomTerminal.Core.Tests.<folder>` |
| `src/common/TestUtils.test.ts` (mocks) | `tests/LoomTerminal.Core.Tests/TestUtils/` | `LoomTerminal.Core.Tests.TestUtils` |

`InputHandling` (not `InputHandler`) avoids a namespace named like the class.

## File header (checked by an architecture test from P1-01)

Every ported file, production or test, starts with exactly these two lines:

```csharp
// Ported from xtermjs/xterm.js: src/common/parser/Params.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.
```

A file that only takes ideas from xterm.js uses `// Inspired by xtermjs/xterm.js: <path>` as the first line, with the
same second line. Brand-new files have no header.

## TypeScript → C# map

| TypeScript | C# |
|---|---|
| `number` used as index, count, column, row | `int` |
| `number` holding bits (cell content, `fg`, `bg`, `ext`, metadata) | `uint` (write `>>> 0` as a plain `uint` cast) |
| `Uint32Array` / `Int32Array` / `Uint16Array` / `Uint8Array` | `uint[]` / `int[]` / `ushort[]` / `byte[]` (or `Span<T>` parameters) |
| `string` | `string` (both are UTF-16: indices match) |
| `String.fromCharCode(c)` | `((char)c).ToString()` |
| `String.fromCodePoint(cp)` / `codePointAt` | `char.ConvertFromUtf32` / `char.ConvertToUtf32` (or `Rune`) |
| `const enum X { A = 1 }` holding states or kinds | `internal enum X { A = 1 }` |
| `const enum` holding bit masks (`Attributes`, `FgFlags`…) | `internal static class X { public const uint Name = ...; }` |
| `UPPER_SNAKE` constant names | `PascalCase` (`CM_MASK` → `CmMask`), same value |
| `Object.create(null)` used as a map | `Dictionary<int, T>` |
| `interface IFoo` (only one implementation, internal) | port the interface only if a card says so; otherwise use the class |
| optional parameter `x?: number` | `int? x = null` or an overload, as the card says |
| `IDisposable` / `Disposable` / `toDisposable` / `MutableDisposable` | `System.IDisposable`; `Common/Disposable.cs` (P1-01) |
| `Emitter<T>` / `Event<T>` (`onFoo(listener)` returns a disposable) | `Common/Emitter.cs` (P1-01): `Emitter<T>.Event` is an `IEvent<T>` with `IDisposable Subscribe(Action<T>)` |
| `boolean \| Promise<boolean>` handler results | `bool` only (see "Async handlers") |
| `assert.equal` / `assert.deepEqual` / `assert.strictEqual` (chai) | `Assert.Equal` (collections: `Assert.Equal(expected, actual)` works on arrays and lists) |
| `beforeEach` | the test class constructor (xUnit creates one instance per test) |
| `it.skip` | `[Fact(Skip = "…")]` with the same reason, or leave it out and list it in the card |

## JavaScript traps

- **Bit operations are 32-bit signed in JS.** `a | b`, `a & b`, `a << n` produce a signed `int`; `>>> 0` makes it
  unsigned. Do the same operations on `uint` in C# and the results match, except `>>` (arithmetic in JS on the signed
  value): check each `>>` on a value that can have bit 31 set and use `>>>` semantics (`uint >> n`) only where JS used `>>>`.
- **`x | 0` truncates to int32**; `Math.floor` on negatives differs from a cast. Port literally (`(int)Math.Floor(x)`).
- **Array out of range returns `undefined` in JS**, it throws in C#. Where xterm.js reads past the end on purpose
  (`this._data[i] || 0`), write the guard explicitly.
- **Typed arrays are zero-filled** and `new Uint32Array(n)` truncates values; `uint[]` does the same, but resizing is
  manual: port `resize` helpers as `Array.Resize` or a copy, as xterm.js does.
- **`string.length` and `charCodeAt` are UTF-16**, like C#: no translation needed. `for…of` over a string iterates
  code points: use `Rune` enumeration.
- **Default parameter values are evaluated at call time** in JS: `fn(x = this.foo)` cannot be a C# default; use an
  overload or `int? x = null` then `x ??= _foo`.
- **`undefined` vs `null` vs `0`**: keep the distinction only when the code tests for it.

## Async handlers (decision — do not port)

xterm.js lets CSI/ESC/OSC/DCS handlers return a `Promise<boolean>`; the parser then pauses and resumes
(`_preserveStack`, `promiseResult`, `IParserStackState`, `InputHandler._parseStack`). **LoomTerminal does not port
this**: every handler returns `bool`, `parse` returns `void`, and all `promiseResult` parameters are dropped.
Tests in the `… - async` suites are not ported. Each card that touches such code says what to drop.
This is recorded in `docs/adr/0002-core-design.md` and in `docs/PORTING.md` ("Behavior differences").

## Visibility and style

- Everything `internal` unless a card says `public`. `InternalsVisibleTo` already covers `LoomTerminal.Core.Tests`
  and `LoomTerminal.Presentation`.
- Keep xterm.js names in PascalCase: `eraseInDisplay` → `EraseInDisplay`, `_curAttrData` → `_curAttrData`.
- Big classes are split into `partial` files named by concern (`InputHandler.Cursor.cs`), each under ~400 lines.
- `<remarks>Threading: not thread-safe; used from the parser thread only.</remarks>` on every Core class, unless the
  card says otherwise (see ADR 0002: Core is single-threaded).
- Do not optimize while porting. Phase 2 measures; optimizations come after, with numbers.

## Tests

- **Port the tests first, then the code.** Keep the xterm.js test name in a comment above each test:
  `// xterm.js: 'should parse CSI with params'`. C# test names are sentences with underscores.
- Never change an expected value of a ported test. If a ported test fails and you believe xterm.js is right, the port
  is wrong. If you believe the test only passes in JS because of a JS quirk, stop and report it.
- Shared helpers go to `tests/LoomTerminal.Core.Tests/TestUtils/`. Do not duplicate them.

## Commands (always through `rtk`)

```powershell
rtk dotnet build LoomTerminal.slnx -c Release
rtk test dotnet test --solution LoomTerminal.slnx -c Release
rtk test dotnet test --project tests/LoomTerminal.Core.Tests -c Release   # faster loop
```

`rtk dotnet test` breaks the Microsoft.Testing.Platform handshake: always use `rtk test dotnet test …`.
