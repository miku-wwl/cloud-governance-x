# Day 21 - Tenancy Domain And Schema

## Status

Implemented and merged. Source day report remains `Validation` pending human
review wording.

## Commit Evidence

- `6284c4c` - `feat: add day 21 tenancy foundation`
- `5273a43` - `Merge pull request #9 from miku-wwl/feat/day21-tenancy-foundation`

## Goal

Implement the expand-only tenancy foundation from ADR-0003 without backfilling
legacy rows.

## Implemented

- Organization, Tenant, ProviderConnection, CloudAccount and Membership Domain
  models;
- EF Core configurations;
- expand-only PostgreSQL migration;
- tenant-owned uniqueness and composite relationship invariants;
- tests for business Tenant and Azure directory separation.

## Verification

The local closeout recorded:

- `scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit`: passed;
- build: 0 warnings and 0 errors;
- tests: 52 passed;
- `scripts/Test-DatabaseMigration.ps1`: passed;
- PostgreSQL negative constraints for cross-tenant references, Provider
  mismatch, duplicate membership, global Provider account identity and
  restricted delete.

Evidence:

- [docs/archive/phase-2/day-21-tenancy-schema-review.md](../archive/phase-2/day-21-tenancy-schema-review.md)
- [docs/archive/adr/ADR-0003-organization-tenant-cloud-account-model.md](../archive/adr/ADR-0003-organization-tenant-cloud-account-model.md)
- ignored local closeout: `tmp/day21-closeout-report.md`

## Boundaries

Day 21 did not implement trusted TenantContext, tenant-aware repositories,
legacy backfill, OIDC, RBAC, audit or RLS.
