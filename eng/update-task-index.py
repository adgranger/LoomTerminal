# Regenerates docs/tasks/README.md from the task cards (titles, dependencies, sizes, statuses).
# Usage (from the repository root): rtk proxy python -I eng/update-task-index.py docs/tasks

import re, glob, os, sys

os.chdir(sys.argv[1])
cards = {}
for f in sorted(glob.glob('phase-*/P*-*.md')):
    f = f.replace(os.sep, '/')
    t = open(f, encoding='utf-8').read().splitlines()
    m = re.match(r'# (P\d-\d\d) — (.*)', t[0])
    dep = [l for l in t if l.startswith('- **Depends on:**')][0][len('- **Depends on:** '):].strip()
    size = [l for l in t if l.startswith('- **Size:**')][0][len('- **Size:** '):].strip().split(' ')[0]
    status = [l for l in t if l.startswith('- **Status:**')][0][len('- **Status:** '):].strip()
    cards[m.group(1)] = (f, m.group(2), dep, size, status)

phases = {
    '1': ('Phase 1 — Emulator core (`LoomTerminal.Core`)',
          'Port of xterm.js `src/common`. **Read [phase-1/README.md](phase-1/README.md) first.**',
          [('Step A — Foundations', 1, 4), ('Step B — Parser', 5, 14), ('Step C — Cells and lines', 15, 20),
           ('Step D — Unicode, options, small services', 21, 27), ('Step E — Buffer and core services', 28, 35),
           ('Step F — InputHandler (one family of sequences per card)', 36, 53),
           ('Step G — Terminal composition and close', 54, 57)]),
    '2': ('Phase 2 — Conformance and performance',
          'Differential tests against xterm.js headless, recorded sessions, benchmarks. **Read [phase-2/README.md](phase-2/README.md) first.**',
          [(None, 1, 7)]),
    '3': ('Phase 3 — Presentation (UI-independent)',
          'Session and threads, render frames, input, selection, links, search. **Read [phase-3/README.md](phase-3/README.md) first.**',
          [('Step A — Keyboard and text input', 1, 5), ('Step B — Session, theme and frames', 6, 12),
           ('Step C — Selection, clipboard, links, search', 13, 19),
           ('Step D — Rendering contracts, accessibility, close', 20, 23)]),
    '4': ('Phase 4 — WinUI 3 control (`LoomTerminal.WinUI`)',
          'Win2D renderer and WinUI input. **Read [phase-4/README.md](phase-4/README.md) first.**',
          [('Step A — Rendering', 1, 6), ('Step B — Input', 7, 11), ('Step C — Control features', 12, 15),
           ('Step D — Gallery, performance, close', 16, 19)]),
    '5': ('Phase 5 — Integration into VIGO',
          "Changes happen in `D:\\VIGO`, under VIGO's rules. **Read [phase-5/README.md](phase-5/README.md) first.**",
          [(None, 1, 5)]),
    '6': ('Phase 6 — Avalonia control (Windows first)',
          'Same behavior as WinUI, drawn with Skia. **Read [phase-6/README.md](phase-6/README.md) first.**',
          [(None, 1, 8)]),
    '7': ('Phase 7 — Unix pty, Linux and macOS', 'Native spawn shim, Unix pty, cross-platform CI.', [(None, 1, 7)]),
}

out = ['''# Task cards

`docs/ROADMAP.md` says **what** each phase delivers. Task cards say **exactly how to get there, one small step at a
time**, so that a coding agent (Claude Sonnet) can implement one card per session without design work:

- **one card = one commit** (the user commits; the agent proposes the message), with its tests;
- at most ~400 lines of production code per card (most are much smaller);
- the xterm.js source to port is given with **file and line numbers** at the pinned commit
  `f447274f430fd22513f6adbf9862d19524471c04` (tag 6.0.0);
- design decisions are **already made** in the card: an agent must not re-decide them; if a card is wrong or
  ambiguous, it stops and reports instead of improvising;
- acceptance criteria are commands that pass or fail (always run through `rtk`).

How to work on a card: `CLAUDE.md` at the repository root and the `/next-task` skill
(`.claude/skills/next-task/SKILL.md`). Template for new cards: [`_template.md`](_template.md).
Pick the **first** card in table order whose status is `todo` and whose dependencies are all `done`
("P1-37 … P1-52" means every card in that range).

Cards of phases 5–7 are less detailed than those of phases 1–4 (they depend on choices made earlier): refine them,
card by card, when their phase starts, in the same format.

Design records: [ADR 0001](../adr/0001-baseline.md) (baseline), [ADR 0002](../adr/0002-core-design.md) (core design),
[ADR 0003](../adr/0003-testing-strategy.md) (testing), [ADR 0004](../adr/0004-input-model.md) (input model).
''']
total = 0
for p, (title, intro, steps) in phases.items():
    out.append(f'## {title}\n\n{intro}\n')
    for name, a, b in steps:
        if name:
            out.append(f'### {name}\n')
        out.append('| Card | Title | Depends on | Size | Status |\n|---|---|---|---|---|')
        for n in range(a, b + 1):
            cid = f'P{p}-{n:02d}'
            f, t, d, s, st = cards.pop(cid)
            total += 1
            out.append(f'| [{cid}]({f}) | {t.replace("|", chr(92) + "|")} | {d} | {s} | {st} |')
        out.append('')
if cards:
    raise SystemExit(f'cards not placed in the index: {sorted(cards)}')
out.append(f'**{total} cards.** Status values: `todo`, `in progress`, `done`. Follow-up cards found while working are '
           'added at the end of their phase, numbered after its last card.\n')
open('README.md', 'w', encoding='utf-8', newline='\n').write('\n'.join(out))
print(total)
