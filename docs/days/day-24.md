# Day 24 - Legacy Tenant Backfill

## Status

Implemented. Source day report remains `Validation`.

## Commit Evidence

- `0d5f98f` - `feat: add controlled legacy tenant backfill`

## Goal

Provide a controlled development-only backfill path for Day 1-7 rows whose
`tenant_id` was still NULL.

## Implemented

- separate Migrator operation for legacy backfill;
- dry-run by default and explicit `-Apply`;
- required Organization and Tenant IDs;
- writer-stop acknowledgement;
- NOWAIT locks and advisory lock;
- row-count confirmation on apply;
- Provider normalization and collision checks;
- controlled creation of ProviderConnection and CloudAccount records;
- completion marker and post-backfill NULL-write constraints.

## Verification

The tracked report records database-gate coverage for dry-run, apply, second
apply, collision failure, active-writer failure, stale count failure, production
environment rejection, post-backfill NULL write rejection and Down rejection
after completion marker.

Evidence:

- [docs/archive/phase-2/day-24-legacy-tenant-backfill.md](../archive/phase-2/day-24-legacy-tenant-backfill.md)
- [scripts/Invoke-DevelopmentTenantBackfill.ps1](../../scripts/Invoke-DevelopmentTenantBackfill.ps1)

## Boundaries

The EF model remains nullable for compatibility. Large-data timing, lock
duration, restore rehearsal, OIDC, RBAC, endpoint authorization, RLS and audit
remain later work.
