# P1-04 — `Params` (CSI / DCS parameters with sub-parameters)

- **Status:** done
- **Depends on:** P1-01
- **Size:** S

## Goal

Port the parameter accumulator used by the parser for CSI and DCS sequences, including colon sub-parameters
(`38:2::255:0:0`).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/parser/Params.ts` | 1–229 | `MAX_VALUE`, `MAX_SUBPARAMS`, `Params` (all members, including `fromArray`) |
| `src/common/parser/Types.ts` | 11–48 | `ParamsArray`, `IParams` (port `IParams` as an interface: handlers receive it) |
| `src/common/parser/Params.test.ts` | 1–237 | all tests |

## Files to create or change

- `src/LoomTerminal.Core/Parser/IParams.cs` — create
- `src/LoomTerminal.Core/Parser/Params.cs` — create
- `tests/LoomTerminal.Core.Tests/Parser/ParamsTests.cs` — create

## Design decisions (already made — do not change)

- `ParamsArray` (`(number | number[])[]`) is ported with the same shape: `ToArray()` returns `object[]` whose items
  are `int` (a parameter) or `int[]` (the sub-parameters of the parameter before it), and
  `static Params FromArray(object[] values)` accepts the same shape. Tests compare with `Assert.Equal` on `object[]`.
- The JS field `params` (`Int32Array`) is named `ParamValues` (`int[]`, public field): a member cannot be named like
  its class. `Length` is a public field. `MaxLength` and `MaxSubParamsLength` are public readonly fields.
- `getSubParams(idx)` returns `ReadOnlySpan<int>`-like data in JS (`Int32Array | null`): return `int[]?` (a copy, as
  `subarray` is a view but callers only read it).
- `getSubParamsAll()` returns `Dictionary<int, int[]>`.
- `-1` means "default/empty sub-parameter" as in xterm.js. Keep the `MAX_VALUE` clamp (`0x7FFFFFFF`).

## Tests

- [x] Port all of `Params.test.ts`: the top-level tests, `parse tests`, `should not overflow to negative`,
      `issue 2389`. The `parse` helper at the top of the test file (lines 1–45) becomes a private static helper in
      the test class.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (✅ P1-04)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The parser itself.

## Suggested commit message

`feat(core): port the parser Params`
