# Day 23 - Tenant-Aware Repositories

## Status

Accepted in tracked day report.

## Commit Evidence

- `f1840f8` - `feat: make core repositories tenant aware`

## Goal

Make resource, cost and ETL persistence require trusted tenant context and
tenant-owned account relationships.

## Implemented

- nullable expand-only `tenant_id` columns on resources, costs and ETL runs;
- TenantContext-required repository reads and writes;
- tenant-aware unique indexes;
- CloudAccount composite foreign keys for resource and cost rows;
- tenant-scoped ETL ID updates;
- explicit E2E Tenant, ProviderConnection and CloudAccount fixtures;
- PostgreSQL Tenant A/B repository integration.

## Verification

The tracked report records `Status: Accepted`. The local closeout recorded
static verification, build, 69 tests, migration Up/Down/reapply, Tenant A/B
integration, cross-tenant CloudAccount rejection and cleanup passing.

Evidence:

- [docs/archive/phase-2/day-23-tenant-aware-repositories.md](../archive/phase-2/day-23-tenant-aware-repositories.md)
- ignored local closeout: `tmp/day23-closeout-report.md`

## Boundaries

Historical NULL-row backfill remained Day 24. OIDC, RBAC, narrower account
scope and RLS remained future work.
