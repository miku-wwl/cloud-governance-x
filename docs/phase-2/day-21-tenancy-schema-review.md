# Day 21 Tenancy Domain and Schema Review

Date: 2026-06-19
Phase: 2 — Identity, tenancy, RBAC and audit
Decision source: ADR-0003
Status: Validation

## 1. Implemented scope

Day 21 adds the expand-only tenancy foundation without changing or backfilling
the existing resource, cost or ETL tables:

- `Organization`
- `Tenant`
- `ProviderConnection`
- `CloudAccount`
- `Membership`

Each aggregate has an opaque UUID, explicit lifecycle status and timestamps.
Azure directory identity remains `provider_directory_id` metadata and is not
the business `tenant_id`.

## 2. Database invariants

The migration creates `organizations`, `tenants`, `provider_connections`,
`cloud_accounts` and `memberships`.

The following constraints are enforced by PostgreSQL:

- a Tenant belongs to an existing Organization;
- tenant-owned relationships use `ON DELETE RESTRICT`;
- a CloudAccount connection foreign key includes tenant, connection and
  Provider, preventing cross-tenant or cross-Provider references;
- tenant-owned uniqueness includes the tenant boundary;
- CloudAccount Provider identity also has the ADR-required global
  `(provider, external_account_id)` uniqueness;
- the migration can be rolled back to the preceding migration and reapplied.

ProviderConnection stores only an opaque `credential_reference`. It does not
store an access token, client secret or raw credential.

## 3. Compatibility boundary

This is an expand-only migration:

- existing `cloud_resources`, `cloud_cost_daily` and `etl_job_runs` remain
  unchanged;
- no default or fabricated Tenant is assigned;
- API and Worker behavior remains compatible with the Day 1–19 baseline;
- Day 22 owns trusted TenantContext;
- Day 23 owns tenant-aware repositories;
- Day 24 owns repeatable legacy-data backfill.

Until Days 22–24 are complete, the new tables do not establish end-to-end
tenant isolation and RISK-0002 remains open.

## 4. Verification

Automated model tests verify:

- slug and Provider normalization;
- business Tenant and Azure directory separation;
- tenant-aware unique index definitions;
- composite CloudAccount-to-ProviderConnection scope;
- restricted cascade deletion.

The real PostgreSQL migration verification additionally proves:

- empty database upgrade and idempotent rerun;
- migration advisory-lock behavior across same and different databases;
- latest migration Down and reapply;
- cross-tenant ProviderConnection references are rejected;
- cross-Provider connection references are rejected;
- duplicate tenant membership identity is rejected;
- the same external subject may belong to different Tenants;
- duplicate global Provider account identity is rejected across Tenants;
- deleting a Tenant with owned rows is rejected;
- API and Worker still run with a schema-restricted runtime role.

## 5. Self-review outcome

The implementation was reviewed again after the initial automated acceptance:

- six trivial status/type files were consolidated into `TenancyEnums.cs`;
- generated migration and snapshot files remain separate because EF owns them;
- per-aggregate EF configurations remain separate because each owns materially
  different keys and relationships;
- PostgreSQL rejection tests now match stable constraint names rather than
  localized error prose;
- Membership rejects undefined `SubjectType` values before persistence;
- tenant-scoped and global uniqueness semantics are both exercised against
  PostgreSQL.

No Day 22 TenantContext or Day 23 repository behavior was pulled into this
change.

## 6. Review focus

Human review should confirm:

1. no existing core row was silently assigned to a Tenant;
2. every tenant-owned uniqueness rule includes `tenant_id`, except the
   documented additional global CloudAccount identity;
3. account and connection tenant/Provider consistency is enforced in the
   database, not only in application code;
4. hard cascade delete is unavailable for the tenancy hierarchy;
5. no secret material is represented by the new schema.
