# Phase 5 conventions — integration into VIGO

Phase 5 replaces VIGO's WebView2 + xterm.js terminal (`D:\VIGO`, ADR-071 there) with `LoomTerminal.WinUI`.
**The code changes happen in the VIGO repository**: before any phase 5 card, read `D:\VIGO\CLAUDE.md`,
`D:\VIGO\docs\PROGRESS.md` and `D:\VIGO\docs\DECISIONS.md`, and follow VIGO's rules, which win over this repository's
for VIGO files:

- every shell command through `rtk`; tests with `rtk test dotnet test --project …`;
- **never commit and never create a branch in VIGO**: propose the commit title and message (English, no trailer);
- milestone ids `J<n>.<m>` (ask the user which one to use, or take the next free one in `docs/PROGRESS.md`);
- every user-visible string localized (`Strings/{fr-FR,en-US}/Resources.resw`, `x:Uid`, `Loc.Get`);
- AOT rules (`x:Bind`, no resource casts…), `/aot-check` after the milestone, replay the touched journeys in the AOT exe;
- ADRs go to `D:\VIGO\docs\DECISIONS.md` with the next free number.

Cards in this folder say **what** to change in VIGO; VIGO's conventions say **how**.
