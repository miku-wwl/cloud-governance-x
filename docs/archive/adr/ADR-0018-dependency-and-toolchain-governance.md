# ADR-0018: Dependency and Toolchain Governance

## Status

Accepted - approved by the project Owner on 2026-06-18.

## Context

The June 18, 2026 documentation review found:

- no currently reported NuGet vulnerabilities from the configured source;
- `xunit 2.9.3` still marked Legacy;
- several NuGet packages with newer versions available;
- local Terraform CLI `1.14.0` while `1.15.6` was available;
- no committed dependency update policy or repeatable dependency gate.

Relevant risks:

- RISK-0013: No CI/CD automated gates.
- RISK-0022: No automated secret, dependency, container, or IaC gates.
- RISK-0023: xUnit v2 package marked Legacy.
- RISK-0027: Dependency and toolchain version drift.

## Decision

Stage 1 will establish a conservative, repeatable dependency and toolchain gate
before performing broad upgrades.

The first gate will be implemented as part of the planned
`scripts/Test-RepositoryStatic.ps1` entry point and will check:

1. `dotnet --version`
2. `dotnet tool restore`
3. `dotnet list FinOpsPlatform.slnx package --vulnerable --include-transitive`
4. `dotnet list FinOpsPlatform.slnx package --deprecated`
5. `dotnet list FinOpsPlatform.slnx package --outdated`
6. `terraform -chdir=terraform/azure version`
7. `terraform -chdir=terraform/azure fmt -check`
8. `terraform -chdir=terraform/azure init -backend=false -input=false`
9. `terraform -chdir=terraform/azure validate`

Stage 1 will treat vulnerable packages as blocking. Deprecated and outdated
packages are reported and tracked; they become blocking only when an ADR, risk,
or stage gate explicitly says so. This avoids accidental large upgrades before
the project has CI evidence and migration coverage.

The first upgrade target remains xUnit v2 to xUnit v3, but the migration should
be done as a focused change with all tests passing rather than mixed into the
static gate work.

## Alternatives Considered

### Upgrade every outdated dependency immediately

Rejected. It would mix governance with behavior-changing dependency upgrades
and make regressions harder to attribute.

### Ignore outdated dependencies until stage 14

Rejected. Stage 14 is the full supply-chain gate, but stage 1 needs enough
visibility to prevent known drift from silently accumulating.

### Dependabot or Renovate as the first step

Deferred. Automated PRs are useful after CI is reliable. Before that, automated
updates can create noise without enforceable regression evidence.

## Consequences

- Stage 1 gains repeatable dependency visibility without forcing unsafe broad
  upgrades.
- The repository can distinguish vulnerability blocking from maintenance drift.
- Future automation such as Dependabot or Renovate remains available after CI
  and owner review policy exist.
- Terraform provider lock changes remain manual and reviewed. `terraform
  init -upgrade` must not be run as an automatic static check.

## Stage 1 Implementation Hooks

- Day 12 records baseline formatting and analyzer settings.
- Day 13 adds `scripts/Test-RepositoryStatic.ps1` and makes dependency checks
  part of it.
- Day 19 CI calls the same script.
- RISK-0023 remains open until xUnit v3 migration is complete.
- RISK-0027 remains open until the gate exists and upgrade policy is documented
  in CI/review docs.

## Verification

Stage 1 is not complete until:

- The static verification script prints dependency and toolchain results.
- The script exits non-zero for vulnerable packages when the CLI reports them.
- Deprecated and outdated package output is preserved in logs or summarized in
  closeout notes.
- Terraform validation runs without writing state or plans to Git.
- Documentation explains that provider lock updates require explicit review.
