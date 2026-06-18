# Phase 1 Stage Gate Report

Date: 2026-06-18
Scope: Day 12～19
Status: EngineeringGatePassed
Independent acceptance: Pending

## Implemented controls

- shared analyzer, formatting, and compilation baseline;
- one repository static-verification entry point;
- executable architecture and infrastructure-package boundaries;
- modular API endpoint, DI, and Worker Job composition;
- dedicated database Migrator with advisory locking;
- repeatable database migration and restricted-role regression;
- GitHub Actions CI, pull-request template, ADR template, CODEOWNERS, and
  responsibility boundaries.

## Required evidence

The Phase 1 engineering gate is complete only when:

- `Static verification` passes on a clean GitHub-hosted runner;
- `Database migration` passes on a clean GitHub-hosted runner;
- a formatting, architecture, test, or migration failure makes its job fail;
- actionlint accepts the committed workflow;
- local and CI verification leave no repository artifacts;
- `main` branch protection requires both CI checks before merge;
- ADR-0001, ADR-0002, and ADR-0018 are accepted;
- risk, gap, baseline, README, and configuration documentation match the final
  repository state.

## GitHub-hosted evidence

The first two workflow runs exposed real cross-platform and offline-execution
defects instead of completing the gate:

- run `27747051177` for commit `029b048` failed both jobs. The failures proved
  that the workflow blocked invalid PowerShell path assumptions and a
  Linux-specific architecture-test path mismatch;
- run `27747251543` for commit `c52f9f5` passed `Static verification`, including
  build and all 37 tests, on `ubuntu-24.04`;
- the same run cancelled `Database migration` at its 20-minute timeout. Empty,
  repeat, concurrent, failure-exit, and restricted-role API checks had passed,
  but the restricted-role Costs Worker waited for Azure authentication even
  though `AzureCost__ForceSampleData=true`.

The cancellation root cause was that `AzureCostProvider` enumerated Azure
subscriptions before honoring forced sample mode. The remediation now returns
forced sample data before constructing any Azure request path, and a regression
test uses a credential that fails if it is touched.

The first remediation run then exposed a second Linux-only process-boundary
defect: `Start-Process dotnet run` stopped its wrapper process while leaving the
actual API child process alive, so the migration script waited indefinitely.
The gate now runs the built API and Worker DLLs directly.

Pull request `#2` proved the complete merge contract:

- before the required checks completed, GitHub reported the PR as `BLOCKED`;
- workflow run `27750735956` passed `Static verification` and
  `Database migration` on commit `ecf9bba`;
- the same run passed all 39 tests, formatting, build, actionlint, dependency,
  Terraform, migration, concurrency, failure-exit, and restricted-role checks;
- after both required checks succeeded, GitHub changed the PR state to `CLEAN`;
- PR `#2` merged as commit `a7d09f5`.

## Local remediation evidence

After the cancelled workflow:

- `Test-RepositoryStatic.ps1` passed;
- actionlint 1.7.12 accepted the workflow;
- an invalid runner-label probe made actionlint fail as expected;
- build completed with 0 warnings and 0 errors;
- the forced-sample offline regression tests passed;
- all 39 tests passed;
- `Test-DatabaseMigration.ps1 -NoBuild` passed in 22 seconds after the direct
  host-process fix, including the
  restricted-role Costs Worker scenario that timed out in GitHub Actions;
- Terraform fmt/init/validate passed without changing Git status;
- all migration test databases, roles, processes, and temporary logs were
  removed.

Earlier Day 12～18 closeout evidence also records deliberate formatting,
architecture, route, DI, migration, and test failures returning non-zero
results. Those local files are intentionally excluded by `.gitignore`; this
report is the committed stage summary.

## Closure confirmation

The Phase 1 engineering gate closed on 2026-06-18:

- `main` branch protection requires `Static verification` and
  `Database migration`;
- required checks use strict/up-to-date mode;
- administrators cannot bypass the protection;
- force pushes and branch deletion are disabled;
- the protected PR was blocked while checks were pending and became mergeable
  only after both checks passed;
- the GitHub-hosted and local gates left no tracked repository artifacts.

Final Owner acceptance is intentionally separate. Before Day 20 starts, the
fixed `main` commit must complete the independent review defined in
`construction/04-★★★-phase-1-independent-review-guide.md`. The review must
produce a complete ledger, resolve all Critical/High findings, disposition
Medium findings, and record the Owner decision.

## Remaining risk

A green Phase 1 gate does not provide authentication, tenant isolation,
production identities, deployment approval, backup, PITR, reliable scheduling,
staging, SLOs, container scanning, SBOM, or provenance. Those controls remain
assigned to later phases.
