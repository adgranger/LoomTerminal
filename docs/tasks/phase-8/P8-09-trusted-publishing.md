# P8-09 — Trusted Publishing instead of an API key

- **Status:** in progress
- **Depends on:** P8-06, P8-07, P8-08
- **Size:** S (one workflow step change, three Markdown files)

## Goal

The release workflow publishes to nuget.org with NuGet.org Trusted Publishing (OIDC) instead of the long-lived secret
`NUGET_API_KEY`: no key to create, store or rotate.

## Sources

| File | What to read |
|---|---|
| https://learn.microsoft.com/nuget/nuget-org/trusted-publishing | policy fields, one-hour key, 7-day activation, scopes |
| https://github.com/NuGet/login | action `NuGet/login`, input `user`, output `NUGET_API_KEY` |

## Files to create or change

- `.github/workflows/release.yml` — change
- `docs/RELEASING.md`, `docs/release-checklist.md`, `docs/tasks/phase-8/README.md` — change
- `docs/tasks/README.md` — regenerate the index

## Design decisions (already made — do not change)

- Action `NuGet/login@v1.2.0` (a released tag, not a branch), step `id: login`, input `user: ${{ secrets.NUGET_USER }}`;
  the key is `steps.login.outputs.NUGET_API_KEY`, passed through `env:` to `dotnet nuget push --api-key`.
- `permissions: id-token: write` only on the `publish` job (next to `contents: write`); the workflow default stays
  `contents: read`. The `nuget` environment with its required reviewer stays; the dry run still publishes nothing.
- The nuget.org user name (profile name, not the e-mail) is the secret `NUGET_USER` of the environment `nuget`; a
  step fails with a clear message when it is empty. No use of `secrets.NUGET_API_KEY` remains.
- Policy on nuget.org: repository owner `adgranger`, repository `LoomTerminal`, workflow file `release.yml`, environment
  `nuget`, glob `LoomTerminal.*`. The login step stays right before the push (the key lasts one hour).
- Unclear in the documentation, written as "to verify at the first release" in `docs/RELEASING.md`: pushing ids that do
  not exist yet, and the policy leaving the "temporarily active" state.

## Tests

- [x] The workflow YAML parses (js-yaml through `npx`; PyYAML and actionlint are absent here)
- [x] `rtk git grep -n NUGET_API_KEY` finds no use outside the history cards P8-06 to P8-08 and this card

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] **HUMAN:** policy created on nuget.org, secret `NUGET_USER` added to the environment `nuget`
- [ ] **HUMAN:** verified at the first (dry run, then real) release
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

- Creating the policy or the secret, publishing, tagging. Editing the done cards P8-06 to P8-08 (history).

## Suggested commit message

`ci: publish to nuget.org with trusted publishing instead of an API key`
