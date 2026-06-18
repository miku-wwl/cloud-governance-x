# Phase 1 Engineering Governance

## Purpose

Phase 1 turns local engineering rules into a repeatable merge contract. CI does
not make the system production-ready; it prevents known formatting,
architecture, dependency, secret, Terraform, test, and migration regressions
from entering the main branch unnoticed.

## Required CI checks

The workflow `.github/workflows/ci.yml` publishes two check names:

- `Static verification`
- `Database migration`

Repository branch protection requires both checks before merging a pull request
into `main`. It uses strict/up-to-date mode, applies to administrators, and
disallows force pushes and branch deletion. Branch protection remains an
external repository-owner setting; the workflow does not create or preserve it
automatically.

## Verification entry points

| Entry point | Responsibility |
| --- | --- |
| `scripts/Test-RepositoryStatic.ps1` | Candidate-file, secret, syntax, actionlint, dependency, format, build, test, and Terraform checks |
| `scripts/Test-DatabaseMigration.ps1` | Empty/repeat/concurrent/failure migration paths and restricted runtime database identity |
| Azure/Terraform E2E scripts | Explicit external-resource verification; never run automatically in Phase 1 CI |

CI must reuse these repository-owned scripts instead of duplicating their
rules inside workflow YAML.

## Responsibility boundaries

| Area | Primary responsibility | Mandatory review focus |
| --- | --- | --- |
| Domain/Application | Application Owner | Business invariants, ports, tenant-safe future evolution |
| Infrastructure/PostgreSQL | Data Owner / Platform SRE | Mapping, migration, credentials, permissions, failure behavior |
| API/Worker/Migrator | Application Owner / Platform SRE | Composition, lifecycle, exit codes, release ordering |
| Terraform | Cloud Provider Owner | Provider lock, state safety, destructive impact, identity |
| CI/scripts/dependencies | Platform SRE / Security Owner | Least privilege, pinned tools, secret and supply-chain gates |
| ADR/risk/gap documents | Decision owner | Facts, acceptance status, evidence, remaining risk |

The initial CODEOWNERS file maps all areas to the current project Owner until
additional maintainers and teams exist.

## Pull request contract

Every pull request must:

1. have one reviewable purpose;
2. identify runtime, schema, identity, data, deployment, and rollback impact;
3. pass both required CI checks when applicable;
4. include a negative test when adding or changing a gate;
5. update ADR, risk, gap, README, and operating documentation when facts change;
6. exclude credentials, state, plans, logs, local evidence, and generated output.

## External action and tool policy

- GitHub Actions are pinned to immutable commit SHAs with the reviewed release
  version recorded in a comment.
- .NET is selected from `global.json`.
- Terraform CLI and actionlint versions are explicit.
- `terraform init -upgrade` is never part of the automatic gate.
- Vulnerable NuGet packages block CI; deprecated and outdated packages remain
  visible but require focused review before upgrade.

## Deferred controls

The following remain outside the Day 19 gate:

- signed commits;
- Dependabot or Renovate;
- SBOM, container, license-policy, provenance, and historical secret scanning;
- deployment, staging, release approval, rollback, backup, and PITR.

They remain tracked by later phases and must not be inferred from a green Phase
1 workflow.
