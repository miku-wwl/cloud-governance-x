# Day 22 Trusted Tenant Context

Date: 2026-06-19
Phase: 2 — Identity, tenancy, RBAC and audit
Status: Accepted

## 1. Outcome

Day 22 introduces one scoped, immutable-after-initialization tenant execution
context for HTTP users and background jobs.

The context contains:

- business `tenant_id`;
- trusted source: HTTP user or background job;
- HTTP issuer and subject when applicable.

`RequireCurrent()` fails closed when no trusted context was established.
Read and initialization capabilities are registered as separate interfaces over
the same scoped instance. Business services consume `ITenantContext`; only HTTP
and Worker adapters consume `ITenantContextInitializer`.

## 2. HTTP trust flow

The client may request a tenant using `X-FinOps-Tenant-Id`, but the header does
not create authority.

The middleware requires:

1. one valid, non-empty tenant UUID;
2. an already authenticated principal;
3. non-empty `iss` and `sub` claims;
4. an Active Membership matching `(tenant_id, issuer, subject)`.

Only after all checks pass is the scoped TenantContext initialized. An
unauthenticated selector returns 401, and an authenticated subject selecting a
tenant without Membership returns 403.

Query string, route and body values are not inspected as tenant authority.
OIDC token validation is intentionally deferred to Day 25; until then, normal
production HTTP authentication is not claimed.

The middleware must remain after authentication middleware when Day 25 adds
Bearer authentication. Requests without a tenant selector continue through the
legacy endpoints during this transition; Day 23 makes tenant-owned Repository
operations require context, and Day 28 protects the complete endpoint surface.

## 3. Background-job trust flow

Worker configuration now binds an explicit `Etl:TenantId`. The Worker creates a
`WorkerJobRequest` containing job name and tenant ID. `WorkerExecution` rejects
an empty tenant before dispatch, verifies that the Tenant exists and is Active,
initializes the scoped context once, and then invokes the selected handler.

Repository filtering is not part of Day 22. Day 23 will consume
`ITenantContext.RequireCurrent()` from every resource, cost and ETL repository.

Legacy E2E scripts now set an explicit development test tenant ID rather than
depending on a default tenant. The committed Worker configuration leaves the
tenant empty so an operator must supply it.

## 4. Security properties

- arbitrary headers do not grant tenant access;
- authenticated identity alone does not grant every tenant;
- only Active Membership is accepted;
- HTTP Membership is ineffective when its Tenant is not Active;
- background jobs reject unknown, suspended or decommissioning Tenants;
- issuer and subject are used instead of mutable email/display name;
- context cannot be replaced after initialization in one scope;
- missing HTTP or Worker context fails closed;
- HTTP and background sources are distinguishable for later authorization and
  audit.

## 5. Verification

Automated tests cover:

- missing context rejection;
- second initialization rejection;
- unauthenticated tenant spoofing;
- authenticated cross-tenant selection rejection;
- valid Active Membership initialization;
- query-string tenant values being ignored;
- Worker explicit tenant propagation into a handler;
- Worker missing tenant rejection before dispatch;
- Worker unknown/inactive tenant rejection before dispatch;
- real Worker process exit code for an all-zero tenant ID;
- DI lifetime and duplicate-registration checks.

## 6. Deliberate boundaries

- Day 23 owns tenant predicates and tenant-aware writes.
- Day 24 owns development Organization/Tenant/Membership seeding and backfill.
- Day 25 owns bearer-token and OIDC validation.
- Day 27 owns permission and scope authorization.
- Day 28 owns stable API authorization error contracts for every endpoint.

## 7. Merge-gate review

The initial implementation was not accepted immediately. Review found and
closed:

- High: a non-empty but unknown or inactive Tenant could be trusted by a
  background job;
- Medium: the concrete context exposed its initialization capability to any
  scoped consumer.

The final implementation verifies Active Tenant state before Worker dispatch,
requires Active Tenant plus Active Membership for HTTP selection, and separates
read access from initialization access while retaining one scoped instance.

Final decision: **ACCEPT**.

Critical open: 0
High open: 0
Medium open: 0
