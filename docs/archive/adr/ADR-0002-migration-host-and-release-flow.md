# ADR-0002: Migration Host and Release Flow

## Status

Accepted - approved by the project Owner on 2026-06-18.

## Context

Before Day 18, the API and Worker called `MigrateAsync` during startup. This was
useful for the local learning baseline, but it was not suitable for production:

- multiple API or Worker instances can race on schema changes;
- runtime identities need DDL permissions;
- failed application startup becomes coupled to migration behavior;
- there is no explicit migration approval, evidence, or rollback path.

Relevant risks:

- RISK-0003: API/Worker automatic migration.
- RISK-0012: No staging and artifact promotion chain.
- RISK-0013: No CI/CD automated gates.

## Decision

Stage 1 removes automatic migrations from API and Worker startup and adds a
dedicated migration executable path.

The implementation is a small `FinOps.Migrator` console project that:

1. references `FinOps.Infrastructure`;
2. loads the same PostgreSQL configuration shape as API and Worker;
3. acquires a target-database PostgreSQL advisory lock;
4. applies EF Core migrations once, then exits;
5. returns `0` on success and `1` on migration failure;
6. logs the database target, pending migration count, applied migration names,
   and elapsed time without logging connection strings or passwords.

Local development may run the migrator manually or through a script before API
or Worker startup. Future CI/CD will run the same migrator as a release step
before deploying application instances.

## Alternatives Considered

### Keep automatic startup migration for all environments

Rejected. It keeps local setup simple but does not close RISK-0003 and forces
application runtime identities to retain schema-change permissions.

### Use only `dotnet ef database update`

Rejected as the primary release path. It is useful for development, but a
project-owned migrator gives the release pipeline a stable executable and lets
the project add logging, validation, and environment checks.

### Run migrations only inside CI without a migrator

Deferred. CI/CD does not exist yet. The migrator creates the reusable unit that
CI/CD can later orchestrate.

## Consequences

- API and Worker startup become simpler and safer.
- Local setup gains one explicit migration step.
- Stage 1 must update scripts and documentation so an empty database can still
  be prepared repeatably.
- Production-like deployments can use different database identities: migrator
  gets DDL permissions; API and Worker get only required runtime permissions.
- Concurrent FinOps migrators for the same database fail before applying schema
  changes; deployment concurrency controls remain the outer release guard.
- Migration rollback remains a controlled release concern; EF `Down` methods
  are not automatically executed in production.

## Stage 1 Implementation Hooks

- Day 18 owns the implementation.
- Add `src/FinOps.Migrator/FinOps.Migrator.csproj`.
- Add the project to `FinOpsPlatform.slnx`.
- Remove `MigrateAsync` calls from `src/FinOps.Api/Program.cs` and
  `src/FinOps.Worker/Worker.cs`.
- Add or update scripts so local verification can run:

```powershell
dotnet run --project src/FinOps.Migrator
dotnet run --project src/FinOps.Api --urls http://localhost:5000
$env:Etl__Job = "Resources"
dotnet run --project src/FinOps.Worker
```

## Verification

Stage 1 is not complete until:

- An empty local database can be migrated by `FinOps.Migrator`.
- Re-running `FinOps.Migrator` is idempotent.
- Two concurrent FinOps migrators cannot both apply migrations to the same
  database.
- API and Worker start successfully after migration.
- API and Worker do not call `Database.MigrateAsync`.
- A migration failure returns a non-zero exit code.
- Documentation states that production migration is a separate release step.
