# Day 23 Tenant-Aware Repositories

Date: 2026-06-19
Phase: 2 — Identity, tenancy, RBAC and audit
Status: Validation

## 1. Outcome

Resource, cost and ETL persistence now requires a trusted TenantContext for
every read and write path.

The three existing core tables gain nullable `tenant_id` columns:

- `cloud_resources`
- `cloud_cost_daily`
- `etl_job_runs`

The columns are nullable only for expand/contract compatibility with rows
created before tenancy existed. New Domain objects require a non-empty tenant,
and every Repository calls `ITenantContext.RequireCurrent()` before accessing
the database.

## 2. Isolation rules

- resource upsert lookup includes `tenant_id`;
- cost upsert lookup and all cost aggregations include `tenant_id`;
- ETL create, list, complete and fail operations include `tenant_id`;
- an ETL run ID owned by another Tenant is reported as not found;
- tenant-owned resource and cost unique indexes include `tenant_id`;
- Provider values are normalized to lowercase before identity comparison;
- missing TenantContext fails before an empty write or database query.

Rows with `tenant_id IS NULL` are preserved but invisible through the
Repositories. Day 24 owns their controlled backfill. That backfill must also
normalize legacy Provider casing and reconcile every legacy account ID to an
onboarded CloudAccount before `tenant_id` can become non-nullable.

## 3. Cloud-account ownership

Writing a tenant ID beside an arbitrary Provider account ID is insufficient.
Resource and cost rows therefore use database composite foreign keys:

```text
(tenant_id, provider, account_id)
    →
CloudAccount(tenant_id, provider, external_account_id)
```

This rejects:

- an account belonging to another Tenant;
- an account that was never onboarded;
- a Provider/account combination inconsistent with CloudAccount.

Test and E2E fixtures create an explicit Organization, Tenant,
ProviderConnection and CloudAccount. No default production Tenant is added.

## 4. Migration and compatibility

`AddTenantAwareCoreData` is expand-only for existing rows:

- existing data is not changed or deleted;
- `tenant_id` is added nullable;
- tenant-aware resource/cost indexes protect new writes;
- filtered legacy indexes preserve the old uniqueness contract while
  `tenant_id IS NULL`, so an older application instance cannot create
  duplicates during a rolling deployment;
- CloudAccount composite uniqueness is represented once by the alternate key
  required by the resource/cost foreign keys;
- tenancy and CloudAccount foreign keys are added;
- immediate Down and reapply are verified.

Rollback limitation:

After two Tenants have legally stored values that collide under the old global
unique indexes, the Down migration cannot recreate those indexes without data
reconciliation. Production rollback after tenant traffic therefore requires a
reviewed reconciliation/roll-forward plan, not blind schema downgrade.

## 5. Verification

Automated tests prove:

- all four Repository implementations fail without TenantContext;
- Tenant A and Tenant B can store and query isolated data;
- Tenant B cannot list or complete Tenant A's ETL run;
- the same resource identity can exist in separate Tenant scopes;
- a cross-tenant CloudAccount write is rejected by PostgreSQL;
- unique indexes and foreign keys include tenant scope;
- PostgreSQL rejects duplicate legacy NULL-tenant resource and cost rows;
- the PostgreSQL integration test is explicitly skipped when its connection
  string is absent instead of being counted as passed;
- migration Up, immediate Down and reapply succeed;
- API and Worker still start with a DDL-restricted runtime role.

Azure API E2E scripts select the fixture Tenant explicitly. Their synthetic
issuer/subject identity is disabled by default and can only be enabled when
the API runs in the dedicated `E2E` environment; production authentication
remains Day 25 scope.

## 6. Deliberate boundaries

- Day 24 assigns existing NULL rows to a controlled development Tenant.
- Day 25 supplies production HTTP bearer authentication.
- Day 27 adds permission and narrower scope decisions.
- PostgreSQL RLS remains ADR-0005 defense-in-depth work.

Until Day 24, historical pre-tenancy rows are intentionally unavailable through
tenant-aware APIs and jobs.
