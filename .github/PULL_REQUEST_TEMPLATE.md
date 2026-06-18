## Summary

Describe the behavior, architecture, documentation, or governance change.

## Scope

- [ ] The change has one clear purpose.
- [ ] Unrelated formatting or generated-file churn is excluded.
- [ ] Runtime behavior changes are called out explicitly.

## Verification

- [ ] `./scripts/Test-RepositoryStatic.ps1`
- [ ] `./scripts/Test-DatabaseMigration.ps1` when database startup, persistence,
      migration, API composition, or Worker composition changes
- [ ] Relevant manual or external E2E evidence is attached when required
- [ ] A deliberate negative check was run for any new or changed gate

## Risk and operations

- [ ] Security, data, migration, compatibility, deployment, and rollback impacts
      have been considered.
- [ ] No credentials, connection strings, state, plans, logs, `tmp/`, `bin/`,
      `obj/`, or other generated evidence are committed.
- [ ] Risk register, production gaps, ADRs, README, and operating documentation
      are updated when facts or decisions change.

## Reviewer focus

List the files, invariants, failure paths, and assumptions that need the most
attention.
