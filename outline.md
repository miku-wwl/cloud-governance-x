# Cloud Governance X Project Charter

This file is the stable project charter. It defines the goal, production
principles and hard boundaries for Cloud Governance X.

For current status, use:

- [docs/current-state.md](docs/current-state.md)

For milestone planning, use:

- [docs/roadmap.md](docs/roadmap.md)

For day-by-day review history, use:

- [docs/days/README.md](docs/days/README.md)

## 1. Mission

Cloud Governance X is a multi-cloud FinOps and resource-governance platform for
organization-scale cloud environments.

The platform should help platform, FinOps, security and resource-owner teams
answer:

- which cloud accounts, subscriptions and resources the organization owns;
- who owns them and which tenant, environment and cost center they belong to;
- where cost is generated and whether it is attributable and explainable;
- which resources, tags, policies, permissions or configurations are risky;
- which governance findings require acknowledgment, waiver or remediation;
- what evidence, rule version and source data produced each conclusion;
- whether the platform itself is secure, observable and recoverable.

## 2. Product Shape

The long-term product is a production-grade platform built around:

- .NET services and Workers;
- PostgreSQL;
- Terraform-managed infrastructure;
- Azure and AWS Provider adapters;
- React frontend when the API and authorization boundary are ready;
- reliable event, audit and notification flows.

The current repository is not yet that final product. Current facts are tracked
in [docs/current-state.md](docs/current-state.md).

## 3. Production Definition

Production-grade means the system can run repeatedly in a real organization
with security, data correctness, observability and recovery evidence.

It does not mean "many features exist" or "the demo works once".

Production requires evidence for:

- standard authentication and authorization on every non-anonymous surface;
- tenant isolation on reads, writes, indexes, jobs, cache keys and storage paths;
- append-only audit for privileged and governance actions;
- cloud credentials stored outside code, logs, config files and database
  plaintext;
- Provider access through least-privilege managed/workload identities or short
  lived credentials;
- repeatable CI/CD, immutable artifacts and migration gates;
- data lineage, quality checks and correct cost semantics;
- reliable ETL with leases, retries, idempotency and checkpoints;
- logs, metrics, traces, SLOs, alerts and runbooks;
- backup, restore and disaster-recovery rehearsals;
- security, dependency, secret, container and IaC gates;
- load, failure and recovery tests at the target scale.

## 4. Architecture Principles

The preferred shape is a modular monolith with independent Workers until there
is clear evidence that a module needs independent deployment or scaling.

Required boundaries:

- Domain does not depend on Application, Infrastructure, hosts, EF Core or cloud
  SDKs.
- Application does not depend on Infrastructure, ASP.NET, EF Core or cloud SDKs.
- API owns HTTP, authentication/authorization, input/output contracts and
  composition.
- Worker owns background execution control and job lifecycle.
- Infrastructure owns database, cloud SDK, queue, object-storage and external
  adapter implementations.
- Migration runs through a dedicated host or release step, not API/Worker
  startup.
- Provider differences stay behind small capability-specific interfaces.

## 5. Data Principles

Core data must be tenant, provider and account scoped.

Production data must support:

- source and ingestion lineage;
- raw, normalized, derived and operational separation where useful;
- original currency and explicit cost semantics;
- no cross-currency aggregation without conversion evidence;
- resource lifecycle semantics;
- rule and finding versioning;
- durable operational state for failures and retries;
- retention and deletion policy.

Sample data, test data, inferred data and real Provider data must be clearly
distinguished. Sample fallback must not hide production Provider failure.

## 6. Security Principles

Security is part of the model, not an afterthought.

Required rules:

- tenant context comes from trusted authentication and membership checks, not
  arbitrary client input;
- every non-platform scope contains a tenant boundary;
- platform cross-tenant operations require separate permission, explicit target,
  reason and audit;
- high-risk remediation is dry-run and approval first;
- secrets are never committed or logged;
- production management endpoints are never anonymous.

## 7. Delivery Principles

The project now uses milestone and gate planning.

Day numbers are retained as review capsules, not as a long-range schedule or a
measure of maturity.

Planning rules:

- expand only the active milestone and the next milestone into detailed work;
- close each milestone with evidence, not optimism;
- keep historical plans for audit, but do not let them override current facts;
- when review fails, fix the current unit rather than creating a cosmetic new
  Day;
- do not claim production readiness from local-only evidence.

## 8. Prohibited Shortcuts

The following are explicitly prohibited in production:

- anonymous management APIs;
- storing real secrets in the repository or plaintext database fields;
- API/Worker startup racing to apply migrations;
- sample data masking Provider failures;
- cross-tenant queries or unique indexes without tenant scope;
- unbounded result APIs;
- concurrent ETL without lease or idempotency controls;
- logging-only failure handling with no persisted failure state;
- unaudited waivers, remediation or platform operations;
- automatic destructive remediation without approval and rollback evidence;
- claiming disaster recovery without a tested restore path;
- claiming multi-cloud production before Azure and AWS have both passed their
  Provider gates.

## 9. Current Decision

The documentation system is now organized around:

- this charter for principles;
- current-state for the present truth;
- milestone roadmap for planning;
- Day capsules for review history;
- ADRs and risk/gap registers for durable decisions and open risk.

The old 100+ Day roadmap remains historical context only.
