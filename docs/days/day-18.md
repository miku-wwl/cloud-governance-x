# Day 18 - Dedicated Migration Host

## Status

Verified by Phase 1 independent acceptance.

## Commit Evidence

- `650e902` - `refactor: isolate database migrations`
- Hardened by `e646913` and later Phase 1 remediation

## Goal

Remove schema migration execution from API and Worker startup and create a
dedicated migration path.

## Implemented

- `FinOps.Migrator`;
- `scripts/Invoke-DatabaseMigration.ps1`;
- `scripts/Test-DatabaseMigration.ps1`;
- PostgreSQL advisory lock behavior;
- empty, repeat, concurrent, failed-connection and restricted-role checks.

## Verification

Phase 1 final acceptance verified empty database migration, idempotent rerun,
same-database concurrency rejection, different-database isolation, failure exit
codes and runtime-role behavior without DDL rights.

Evidence:

- [src/FinOps.Migrator](../../src/FinOps.Migrator)
- [scripts/Test-DatabaseMigration.ps1](../../scripts/Test-DatabaseMigration.ps1)
- [docs/archive/adr/ADR-0002-migration-host-and-release-flow.md](../archive/adr/ADR-0002-migration-host-and-release-flow.md)
- [docs/archive/phase-1/independent-acceptance-report.md](../archive/phase-1/independent-acceptance-report.md)

## Boundaries

Production release orchestration, migration approval, roll-forward/rollback
rehearsal and staging data-volume tests remain later platform work.
