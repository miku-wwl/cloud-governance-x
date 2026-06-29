# Milestone Roadmap

This file replaces the old long-range Day table as the planning entrypoint.
Day numbers remain useful as review capsules, but they should not be used as a
proxy for maturity or production readiness.

## 1. Planning Rule

Only the active milestone and the next milestone should be expanded into
day-level implementation detail. Later work stays at milestone level until
earlier gates produce enough evidence to plan responsibly.

Every milestone must close with:

- accepted design or ADRs;
- implementation and migrations where applicable;
- automated tests and negative tests;
- real integration or staging evidence when the scope requires it;
- updated risks, production gaps and operating docs;
- an explicit gate decision.

## 2. Milestones

| Milestone | Former Day Range | Purpose | Status |
| --- | --- | --- | --- |
| M0 Development Baseline | Day 1-7 | Local Azure/PostgreSQL/API/Worker proof | Complete |
| M1 Baseline Governance | Day 8-11 | Current facts, architecture, risks and gate | Complete |
| M2 Engineering Foundation | Day 12-19 | Static gate, architecture tests, modular hosts and migration separation | Accepted |
| M3 Identity And Tenant Foundation | Day 20-26 | Tenant model, trusted context, tenant-aware data, OIDC and Entra dev identity | Implemented through Day 26; phase still open |
| M4 RBAC, Endpoint Protection And Audit | Day 27-30 | Permission/scope RBAC, endpoint policies, stable 401/403 and append-only audit | Active next work |
| M5 Production Data Model | Former Day 31-40 | lineage, resource lifecycle, cost semantics, data quality and migration rehearsals | Not started |
| M6 Reliable ETL Platform | Former Day 41-50 | scheduler, lease, retry, checkpoint, backfill and operator controls | Not started |
| M7 Release A Platform Base | Former Day 51-59 plus Phase 11/12 foundations | observability, containers, environments, CI/CD, backup and recovery basics | Not started |
| M8 Azure Production Capability | Former Day 60-127 selected gates | production Azure Provider, FinOps semantics, governance workflow, API/frontend and release gate | Not started |
| M9 Multi-Cloud Capability | Former Day 128-136 | AWS Provider and Azure/AWS unified contracts | Not started |
| M10 System Hardening And Launch | Former Day 137-148 | security, supply chain, performance, resilience, DR, Go/No-Go and canary | Not started |

## 3. Active Milestone Boundary

M4 is the active planning unit.

Expected sequence:

| Unit | Purpose |
| --- | --- |
| Day 27 | Define and enforce permission + scope RBAC contracts |
| Day 28 | Protect existing business endpoints and stabilize auth errors |
| Day 29 | Add append-only audit model and privileged action records |
| Day 30 | Execute tenant escape, IDOR, RBAC and audit gate |

The detailed plan lives in:

- [construction/current-playbook.md](../construction/current-playbook.md)

## 4. Retired Planning Shape

The old Day 8-148 roadmap remains as historical context:

- [construction/archive/02-★★★-day8-production-roadmap.md](../construction/archive/02-★★★-day8-production-roadmap.md)

Do not use it as the primary source for current planning. If it conflicts with
this roadmap or current-state evidence, treat the old file as archived context.
