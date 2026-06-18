# Phase 1 Stage Gate Report

Date: 2026-06-18
Scope: Day 12～19
Status: RemediationReadyForCI

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

Phase 1 is complete only when:

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

## Local remediation evidence

After the cancelled workflow:

- `Test-RepositoryStatic.ps1` passed;
- actionlint 1.7.12 accepted the workflow;
- an invalid runner-label probe made actionlint fail as expected;
- build completed with 0 warnings and 0 errors;
- the forced-sample offline regression tests passed;
- `Test-DatabaseMigration.ps1 -NoBuild` passed in 28 seconds, including the
  restricted-role Costs Worker scenario that timed out in GitHub Actions;
- Terraform fmt/init/validate passed without changing Git status;
- all migration test databases, roles, processes, and temporary logs were
  removed.

Earlier Day 12～18 closeout evidence also records deliberate formatting,
architecture, route, DI, migration, and test failures returning non-zero
results. Those local files are intentionally excluded by `.gitignore`; this
report is the committed stage summary.

## Closure actions

Phase 1 remains open until all of the following are complete:

1. commit and push the forced-sample remediation;
2. obtain one run on that exact commit where both `Static verification` and
   `Database migration` succeed;
3. configure `main` branch protection to require both checks before merge;
4. confirm the protected-branch settings through the GitHub API and change this
   report to `Passed`.

## Remaining risk

A green Phase 1 gate does not provide authentication, tenant isolation,
production identities, deployment approval, backup, PITR, reliable scheduling,
staging, SLOs, container scanning, SBOM, or provenance. Those controls remain
assigned to later phases.
