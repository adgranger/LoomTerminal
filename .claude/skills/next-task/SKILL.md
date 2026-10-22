---
name: next-task
description: Implement the next LoomTerminal task card (or the one given), verify it, update the bookkeeping, and propose a commit message. Never commits.
argument-hint: "[card id, e.g. P1-03 — optional]"
---

# Implement the next task card

Requested card: `$ARGUMENTS` (empty = pick the next one automatically).

Follow these steps **in order**. Stop and report to the user (in French) as soon as a step says "stop". Everything you
write in the repository (code, docs, commit message) is in **English**. **Every shell command goes through `rtk`**
(`rtk dotnet build …`, `rtk test dotnet test …`, `rtk git status`, `rtk proxy <cmd>` when the raw output is needed).

## 1. Check the starting point

1. Run `rtk git status --porcelain`. If anything is modified or untracked, **stop**: the user must commit (or stash)
   the previous card first, so that each card is one commit.
2. Never create or switch branches: work on the current branch.

## 2. Pick the card

- If `$ARGUMENTS` names a card (e.g. `P1-03`), use `docs/tasks/phase-*/<id>-*.md`.
- Otherwise, open `docs/tasks/README.md` and take the **first** card, in table order, whose status is `todo` and whose
  "Depends on" cards are all `done` ("P1-37 … P1-52" means every card in that range; "all P1 cards" means every card
  of the phase except this one).
- If no card qualifies, **stop** and say so. If the chosen card's dependencies are not all `done`, **stop** and list them.

Tell the user which card you picked and why, in one sentence, then continue without waiting.

## 3. Implement it

1. Read `CLAUDE.md`, the phase conventions (`docs/tasks/phase-<n>/README.md`) and the card. The card's
   "Design decisions" are final; "Out of scope" is forbidden territory.
2. Set the card's `Status:` to `in progress`.
3. Read the sources listed in the card **at the pinned commit**
   (`https://raw.githubusercontent.com/xtermjs/xterm.js/f447274f430fd22513f6adbf9862d19524471c04/<path>`), at the
   given lines. Never port from memory.
4. Port or write the tests first, then the production code, exactly as the card describes.
5. If the card is ambiguous, contradicts the code base, or cannot be done as written, **stop** without finishing:
   explain the problem and propose a fix to the card.

## 4. Verify

From the repository root:

```
rtk dotnet build LoomTerminal.slnx -c Release
rtk test dotnet test --solution LoomTerminal.slnx -c Release
```

plus every extra command of the card's acceptance criteria. Required: **0 warnings, 0 errors, all tests green**.
Fix and re-run until they pass. If you cannot, **stop** and report the failures with their output.

Never weaken a test, never change the expected values of a ported xterm.js test, never add `#pragma warning disable`
or `NoWarn` to get green unless the card says so.

## 5. Update the bookkeeping

- Card: tick every checkbox that is done, set `Status: done`.
- Index: `rtk proxy python -I eng/update-task-index.py docs/tasks` (regenerates `docs/tasks/README.md` from the cards).
- `docs/PORTING.md`: update the map / behavior differences / tests not ported as the card asks.
- New public API: `./eng/Update-PublicApi.ps1 src/<Project>` (through `rtk proxy pwsh -NoProfile -File …`) and check the diff.

## 6. Close everything you opened

Before handing over (and also before any "stop" above), **force-close every process and window you launched** during
the card: Gallery / sample apps, `PtyConsole`, shells or ConPTY children (`pwsh`, `cmd`, `conhost`, `OpenConsole`),
dev servers, background `dotnet` hosts (test hosts, `build-server`), preview/browser tabs, and background shell tasks.

1. Stop what you started yourself first (background tasks, `preview_stop`, terminal tabs you opened).
2. Then check for leftovers started from this repo, e.g.
   `rtk proxy pwsh -NoProfile -Command "Get-Process | Where-Object { $_.Path -like '*LoomTerminal*' } | Select-Object Id,ProcessName,Path"`,
   and kill them (`Stop-Process -Id <id> -Force`). Only kill processes you started or that run from this repository;
   never touch the user's own terminals or unrelated `dotnet`/shell processes.
3. Report in the hand-over summary what was closed (or "nothing left running").

## 7. Hand over (no commit)

1. `rtk git status` and `rtk git diff --stat`: only files related to this card changed.
2. **Do not commit, do not stage, never push.** Give the user, ready to paste:
   - the commit subject: the card's "Suggested commit message" (adjusted only if it no longer describes the change);
   - a body of 2–5 lines saying what was ported or added and which tests;
   - **no trailer** (no `Co-Authored-By`, no "Generated with", no `Signed-off-by`).
3. Summarize in French in a few lines: card done, tests count, anything the next card should know.
