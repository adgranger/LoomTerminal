# P1-52 — `InputHandler.Dcs`: DECRQSS

- **Status:** done
- **Depends on:** P1-49, P1-41
- **Size:** S

## Goal

Port the DCS status string request (`DCS $ q Pt ST`), which reports SGR, DECSCUSR, DECSTBM, DECSCL and DECSCA state.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 3416–3435 | `requestStatusString` |
| | constructor | DCS `$q` (`new DcsHandler((data, params) => this.requestStatusString(data, params))`) |
| `src/common/InputHandler.test.ts` | 2260–2276 | `DECSCA and DECSED/DECSEL` › `DECRQSS reports correct DECSCA state` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.Dcs.cs` — create (with `RegisterDcsHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerDcsTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. Replies are built exactly as in JS (including the `\x1bP1$r…\x1b\\` / `\x1bP0$r\x1b\\` forms).

## Tests

- [x] Port `DECRQSS reports correct DECSCA state`.
- [x] New: `DCS $ q m ST` after `CSI 1;31 m` replies with the JS SGR status string; `DCS $ q r ST` with a scroll
      region replies with it; an unknown request replies with the "invalid" form.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Sixel and other DCS protocols.

## Suggested commit message

`feat(core): port DECRQSS`
