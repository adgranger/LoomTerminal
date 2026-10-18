# xterm-oracle

Generates the **expected** screen snapshots of the conformance fixtures by running `@xterm/headless` 6.0.0
(the xterm.js version LoomTerminal ports) with the Unicode 11 addon. The format is described in
`docs/tasks/phase-2/README.md`.

```powershell
npm ci
node oracle.mjs ../../tests/LoomTerminal.Core.Tests/Conformance/Fixtures          # writes <name>.json next to each fixture
node oracle.mjs ../../tests/LoomTerminal.Core.Tests/Conformance/Fixtures --check  # writes nothing, exits 1 if a JSON would change
```

Node.js 20 or later is required. CI does not run the oracle: the generated JSON files are committed.
**Never edit a generated JSON file by hand**: change the fixture and regenerate it.
