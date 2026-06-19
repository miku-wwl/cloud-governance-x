# Day 24 Legacy Tenant Backfill

Date: 2026-06-19
Phase: 2 — Identity, tenancy, RBAC and audit
Status: Validation

## 1. Outcome

Day 1–7 resource, cost and ETL rows with `tenant_id IS NULL` can now be
assigned to an explicitly selected development Tenant without deleting or
recreating those rows.

Backfill is a separate `FinOps.Migrator` operation. It is not part of normal
schema migration and cannot be invoked by API or Worker.

## 2. Safety boundary

The operation:

- runs only when `DOTNET_ENVIRONMENT=Development`;
- requires explicit Organization and Tenant IDs;
- requires an acknowledgement that every pre-Day24 writer is stopped;
- takes `NOWAIT` table locks and fails if a writer is still active;
- defaults to a transactionally rolled-back dry-run;
- requires an explicit `-Apply` switch to commit;
- requires the exact database name and dry-run row counts again on apply;
- rejects a batch above the configured maximum legacy-row count;
- refuses to run while schema migrations are pending;
- uses a database-scoped transaction advisory lock;
- changes only rows whose `tenant_id` is NULL;
- preserves all resource, cost and ETL row IDs and row counts;
- creates only the ProviderConnection and CloudAccount records required by
  legacy rows;
- installs database check constraints that reject every future NULL-Tenant
  write after apply;
- records completion in the independent
  `legacy_tenant_backfill_control` table;
- performs no ownership write when no legacy rows exist.

The command is:

```powershell
./scripts/Invoke-DevelopmentTenantBackfill.ps1 `
  -Database finops `
  -OrganizationId <approved-development-organization-id> `
  -TenantId <approved-development-tenant-id> `
  -AcknowledgeLegacyWritersStopped
```

That command is a dry-run. Repeat it with `-Apply` only after reviewing its
counts and taking the required database recovery point:

```powershell
./scripts/Invoke-DevelopmentTenantBackfill.ps1 `
  -Database finops `
  -OrganizationId <approved-development-organization-id> `
  -TenantId <approved-development-tenant-id> `
  -AcknowledgeLegacyWritersStopped `
  -Apply `
  -ConfirmDatabase finops `
  -ExpectedResourceRows <dry-run-resource-count> `
  -ExpectedCostRows <dry-run-cost-count> `
  -ExpectedEtlRunRows <dry-run-etl-count>
```

The default maximum is 100,000 total legacy rows. Raising
`-MaximumLegacyRows` is a reviewed decision, not an automatic retry.

## 3. Data reconciliation

Before updating any core row, the operation:

1. normalizes legacy Provider values with trim plus lowercase;
2. rejects resource or cost identities that would collide after normalization;
3. rejects a Provider/account pair already owned by another Tenant;
4. creates one development ProviderConnection per legacy Provider;
5. creates missing CloudAccount rows for every resource/cost account;
6. verifies every legacy account maps to the selected Tenant.

Any failed check rolls back the complete transaction. It does not partially
assign Tenant IDs.

## 4. Repeatability evidence

The database gate verifies:

- dry-run leaves all rows and ownership records unchanged;
- apply preserves the original table row counts;
- all three legacy tables have zero NULL Tenant IDs afterward;
- Provider casing is normalized;
- required ownership records are created;
- a second apply reports zero updated rows;
- normalization collisions fail and preserve the original NULL rows;
- active writers fail the operation without waiting;
- stale dry-run counts, the wrong database name and excessive row counts fail;
- Production environment execution is rejected;
- post-backfill NULL writes from old artifacts are rejected by PostgreSQL;
- schema Down succeeds before backfill but is rejected after the persistent
  completion marker exists;
- a rejected Down leaves tenant columns, completion marker and migration
  history unchanged.

## 5. Upgrade sequence

1. Apply all schema migrations.
2. Stop and verify absence of every pre-Day24 API and Worker process.
3. Record table counts and create a database backup/PITR recovery point.
4. Run backfill without `-Apply` and record all three row counts.
5. Review collision, ownership and update counts.
6. Run backfill with `-Apply`, exact database confirmation and those counts.
7. Run it again and require zero updates.
8. Start only Day24-or-newer API and Worker artifacts.
9. Verify the selected Tenant can query the migrated rows.

Old writers must not restart after step 6. PostgreSQL check constraints reject
their tenant-less writes.

## 6. Rollback and recovery

Failure before commit is handled by transaction rollback.

After commit, the preferred response is roll-forward. Provider normalization
and generated ownership records mean a blind SQL update back to NULL would not
restore the exact pre-backfill state.

The Day24 control migration owns a completion table that does not depend on the
three `tenant_id` columns. Its Down method refuses to run after backfill has
committed. This blocks EF from reaching the Day23 Down path, so PostgreSQL
cannot silently remove the column-level check constraints.

Removing the completion marker or control table manually is prohibited.
Downgrading without a database restore would reopen NULL writes and discard the
reviewed ownership transition.

If application rollback to a pre-Day24 artifact is unavoidable:

1. stop every writer;
2. restore the database recovery point created before backfill;
3. deploy the preceding application artifact;
4. verify row counts and legacy unique indexes;
5. investigate and correct the backfill conflict before retrying.

Manual deletion of the Tenant or CloudAccount records is prohibited because
foreign keys and later development data may depend on them.

## 7. Remaining boundary

The EF model remains nullable for expand/contract compatibility, while a
successfully backfilled database enforces non-NULL writes through check
constraints. Day38 owns the reviewed model/schema contract migration.

Large-data timing, lock duration and exact restore rehearsal remain Day39
work. Managed backup and PITR capability remains Day58 work.

OIDC authentication, RBAC, full endpoint authorization and PostgreSQL RLS
remain Days 25–30 work.
