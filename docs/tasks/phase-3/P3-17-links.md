# P3-17 — Links: URL detection and OSC 8, Ctrl+click

- **Status:** todo
- **Depends on:** P3-15
- **Size:** M

## Goal

Detect web links in the buffer (port of `addon-web-links`), resolve OSC 8 hyperlinks (P1-51), underline the link under
the pointer while Ctrl is held, and hand activated links to the host — https only by default, as VIGO.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `addons/addon-web-links/src/WebLinkProvider.ts` | 1–199 | `isUrl`, `LinkComputer.computeLink`, `_getWindowedLineStrings`, `_mapStrIdx` |
| `addons/addon-web-links/src/WebLinksAddon.ts` | 1–58 | the default regex (`strictUrlRegex`) and the activation rule |
| `src/browser/Linkifier.ts` | 60–150, 249–324, 370–380 | hover → ask providers → current link; `_linkAtPosition` — logic only |
| `src/browser/OscLinkProvider.ts` | whole file | OSC 8 links as a provider |

## Files to create or change

- `src/LoomTerminal.Presentation/Links/WebLinkComputer.cs` — create (internal)
- `src/LoomTerminal.Presentation/Links/OscLinkProvider.cs` — create (internal)
- `src/LoomTerminal.Presentation/Links/LinkTracker.cs` — create (internal: current hovered link, Ctrl state)
- `src/LoomTerminal.Presentation/Links/ILinkHandler.cs` — create (public: `bool CanOpen(Uri uri)`, `void Open(Uri uri)`;
  default implementation `HttpsOnlyLinkHandler` that accepts `https` only and opens nothing — the host sets its own)
- `src/LoomTerminal.Presentation/Session/TerminalSession.cs` — public `ILinkHandler? LinkHandler`; pointer moves with
  Ctrl update the hovered link; Ctrl+click activates
- `src/LoomTerminal.Presentation/Rendering/RenderFrameBuilder.cs` — `TextRunStyle.IsLink` for the hovered link's cells
- `tests/LoomTerminal.Presentation.Tests/Links/*Tests.cs` — create

## Design decisions (already made — do not change)

- The regex is ported as a `[GeneratedRegex]` with the same pattern (check that .NET accepts every construct; if one
  is JS-only, rewrite it equivalently and add a test per URL shape listed in the addon's README or tests).
- Activation requires Ctrl (VIGO). Plain click never opens a link.
- `CanOpen` is checked before underlining: links the host refuses are never shown as links.

## Tests

- [ ] New: `https://example.com/a?b=c` detected with exact range; URL wrapped across two lines detected as one link;
      trailing `.`/`)` handling identical to the addon; OSC 8 link detected; `http://` refused by the default handler;
      Ctrl+click calls `Open`; click without Ctrl does not.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Public API files updated; `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`path:line` file links (deferred in VIGO, see roadmap).

## Suggested commit message

`feat(presentation): detect and open web and OSC 8 links`
