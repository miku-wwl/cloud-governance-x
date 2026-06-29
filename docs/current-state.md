# Current State

Last reviewed: 2026-06-29  
Branch baseline: `main` at `b3efe97`  
Current execution model: milestone and gate based, with Day capsules retained
for review history.

## 1. Where The Project Is

Cloud Governance X has moved beyond the original Day 1-7 development baseline
and beyond the Phase 1 engineering gate.

Current position:

| Dimension | Status |
| --- | --- |
| Current phase | Phase 2 - Identity, tenancy, RBAC and audit |
| Latest implemented Day | Day 26 - Microsoft Entra development integration |
| Next construction unit | Day 27 - permission and scope based RBAC |
| Production readiness | Not production ready |
| Local test snapshot | `dotnet test FinOpsPlatform.slnx --no-restore`: 84 passed, 1 skipped |
| Working tree snapshot | Clean at the start of this documentation migration |

The project should no longer be managed by the old 148-Day long-range table as
the primary source of truth. Future work should use milestone gates, with only
the active and next milestone expanded into a detailed playbook.

## 2. Completed Milestones

| Milestone | Scope | Result | Evidence |
| --- | --- | --- | --- |
| Development baseline | Day 1-7 Azure data foundation | Complete as local/dev baseline | [Day capsules](days/README.md), [baseline summary](archive/phase-0/baseline-verification-summary.md) |
| Phase 0 | Day 8-11 baseline audit, risk, architecture and gate | Complete | [stage-0-gate-report.md](archive/phase-0/stage-0-gate-report.md) |
| Phase 1 | Day 12-19 engineering governance and migration separation | Accepted | [independent-acceptance-report.md](archive/phase-1/independent-acceptance-report.md) |
| Phase 2 partial | Day 20-26 tenancy, trusted context, OIDC and Entra development identity | Implemented through Day 26; phase not closed | [Phase 2 day capsules](days/README.md) |

## 3. Current Capabilities

The repository currently provides:

- .NET 10 API, Worker, Migrator, Application, Domain, Infrastructure and Tests;
- PostgreSQL local development environment and health checks;
- Azure Terraform development resource lifecycle;
- Azure subscription, resource inventory and cost data ingestion;
- ETL run history and explicit database migration host;
- static verification and database migration CI gates;
- business Tenant model, trusted TenantContext and tenant-aware repositories;
- controlled legacy tenant backfill tooling;
- OIDC JWT Bearer validation;
- repeatable Microsoft Entra development app registration and real-token E2E
  verification.

These are still development and governance foundations. They do not authorize a
public or production deployment.

## 4. Production Prohibitions Still In Force

The following remain prohibited for production until later gates close them:

- exposing business endpoints without authorization policies;
- treating the Day 26 delegated scope as enforced authorization;
- using local Azure CLI identity as the Azure Provider runtime identity;
- enabling cost sample fallback in production;
- using local Terraform state for team or production infrastructure;
- claiming complete multi-cloud, React frontend, audit, RLS, production ETL
  scheduling, backup, SLO or disaster recovery capability.

## 5. Immediate Next Work

Day 27 should implement permission and scope based RBAC.

The current playbook is:

- [construction/current-playbook.md](../construction/current-playbook.md)

Day 27 must not silently absorb Day 28 endpoint protection or Day 29 audit
storage. Those remain separate gates unless the Owner explicitly changes the
milestone plan.

## 6. Documentation Authority

Use this precedence when documents disagree:

1. production safety and data correctness;
2. [outline.md](../outline.md);
3. this current-state file;
4. accepted ADRs in [docs/archive/adr](archive/adr/);
5. Day capsules in [docs/days](days/);
6. stage reports and risk/gap registers;
7. archived construction plans and local review transcripts.

Ignored local files such as `review.txt` and `website-reivew.md` are external
review transcripts. They can be useful during analysis, but they are not current
project truth unless their conclusions are copied into tracked docs.
