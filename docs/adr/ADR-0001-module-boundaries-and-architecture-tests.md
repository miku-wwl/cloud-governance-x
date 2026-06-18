# ADR-0001: Module Boundaries and Architecture Tests

## Status

CandidateDecision - owner approval required before stage 1 implementation.

## Context

The current codebase is a modular monolith with seven projects:

- `FinOps.Domain`
- `FinOps.Application`
- `FinOps.Infrastructure`
- `FinOps.Migrator`
- `FinOps.Api`
- `FinOps.Worker`
- `FinOps.Tests`

The intended dependency direction is:

```text
Api / Worker -> Infrastructure -> Application -> Domain
```

This is currently enforced by project references and review discipline, not by
automated architecture tests. Stage 1 requires CI to fail when core boundaries
are violated.

Relevant risks:

- RISK-0013: No CI/CD automated gates.
- RISK-0022: No automated secret, dependency, container, or IaC gates.

## Decision

Stage 1 will keep the system as a modular monolith and enforce boundaries with
reflection-based architecture tests in `FinOps.Tests`.

The first architecture rules are:

1. `FinOps.Domain` must not reference Application, Infrastructure, API, Worker,
   EF Core, ASP.NET Core, Azure SDK, Npgsql, or hosting packages.
2. `FinOps.Application` may reference Domain, but must not reference
   Infrastructure, API, Worker, EF Core, ASP.NET Core, Azure SDK, Npgsql, or
   hosting packages.
3. Azure SDK and Npgsql implementation dependencies are allowed only in
   `FinOps.Infrastructure`.
4. `FinOps.Api` and `FinOps.Worker` may compose Application and Infrastructure,
   but business use cases stay in Application.
5. `FinOps.Migrator` may reference Infrastructure but must remain a dedicated
   migration executable.
6. `FinOps.Tests` may reference production projects and test-only libraries.

Stage 1 will initially implement these tests without introducing a new external
architecture-test package. If reflection tests become noisy or too limited,
the project may later evaluate a dedicated library through ADR-0018.

## Alternatives Considered

### Review-only enforcement

Rejected. Review is still required, but it does not provide repeatable CI
evidence and will miss accidental package or project reference drift.

### Add a dedicated architecture-test package immediately

Deferred. A package can improve ergonomics, but adding tooling before the first
rule set is known increases dependency surface. The first stage can use
assembly metadata and project/package file checks.

### Split into microservices

Rejected for stage 1. The current system does not yet have production identity,
tenant isolation, reliable jobs, or deployment automation. Splitting services
now would increase operational complexity before the monolith boundaries are
stable.

## Consequences

- Architecture rules become executable and can block CI.
- The first rules focus on project and package boundaries, not every future
  domain module.
- Module names such as Identity, Tenancy, Costs, Inventory, Compliance,
  Findings, Events, Audit, and Operations remain logical module targets until
  code grows enough to justify explicit namespaces and tests for each one.
- Any intentional boundary exception must update this ADR or create a follow-up
  ADR before implementation.

## Stage 1 Implementation Hooks

- Day 12: add `.editorconfig` and analyzer baseline without changing runtime
  behavior.
- Day 13: create a single static verification entry point, planned as
  `scripts/Test-RepositoryStatic.ps1`.
- Day 14: add architecture tests under `src/FinOps.Tests/Architecture/`.
- Day 15-17: preserve route, DI, and Worker behavior while splitting code.
- Day 19: CI must run the static verification entry point and architecture
  tests.

## Verification

Stage 1 is not complete until:

- A normal run of `dotnet test` includes architecture tests.
- A deliberately introduced reverse dependency fails the architecture tests.
- The static verification script fails when architecture tests fail.
- README or review documentation maps the rules back to this ADR.
