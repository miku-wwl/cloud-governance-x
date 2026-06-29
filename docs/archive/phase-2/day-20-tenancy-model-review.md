# Day 20 Tenancy Model Review

Date: 2026-06-19  
Phase: 2 — Identity, tenancy, RBAC and audit  
Decision: ADR-0003 Accepted  
Implementation scope: design only; no Domain, EF or migration changes

## 1. Outcome

Day 20 establishes the business and security vocabulary required by Day 21～30.

The accepted model is:

```text
Organization
    └── Tenant (primary security and data boundary)
          ├── Membership (issuer + subject)
          ├── ProviderConnection (credential reference, never credential)
          └── CloudAccount (Azure Subscription / AWS Account)
                └── typed narrower scopes
```

Azure tenant/directory ID is Provider metadata. It is not the Cloud Governance
X business Tenant and must never be used as `tenant_id`.

## 2. Model review matrix

| Concept | Owns / belongs to | Security meaning | Cardinality | Key invariant |
| --- | --- | --- | --- | --- |
| Organization | top-level customer/administrative umbrella | no implicit row-level access | 1:N Tenants | membership in one Tenant does not grant Organization-wide access |
| Tenant | one Organization | primary authorization and data-isolation boundary | 1:N accounts, connections, memberships | every tenant-owned operation has one trusted tenant |
| CloudAccount | one Tenant and one ProviderConnection | normalized Provider account scope | one active Tenant at a time | Azure Subscription/AWS Account cannot be silently reassigned |
| ProviderConnection | one Tenant | Provider identity/configuration boundary | 1:N CloudAccounts | stores only credential reference; account and connection tenant must match |
| Membership | one Tenant, one external subject | permits a subject to participate in a Tenant | subject may join multiple Tenants | issuer + subject is stable identity; email is not |
| Scope | one Tenant, optionally one account/resource | bounds a permission to a typed target | hierarchical | no wildcard is represented by missing tenant |

## 3. Identity-source review

| Execution path | Identity source | Tenant source | Rejected source |
| --- | --- | --- | --- |
| Human HTTP | validated OIDC issuer + subject | active Membership selected and verified server-side | arbitrary header/query/route/body |
| Service HTTP | validated workload/service subject | explicit service grant and target Tenant | shared secret identity or client claim alone |
| Background Job | server-created job definition/message | persisted tenant/account scope | ambient HTTP state, default tenant, Provider account inference |
| Platform administration | separate platform-level grant | one explicitly named target Tenant | implicit all-tenant wildcard |
| Local development | controlled development identity adapter | seeded development Tenant | Azure CLI tenant ID as business tenant |

## 4. Scope and authorization review

The initial scope hierarchy is:

```text
Tenant → CloudAccount → future Provider resource scope
```

Authorization evaluates both permission and scope. Day 20 deliberately does not
choose final role names or implement policies; Day 27 owns those details.

The following rules are fixed now:

1. Every non-platform scope includes `tenant_id`.
2. Account scope resolves through a CloudAccount owned by that Tenant.
3. Provider-native IDs do not independently grant access.
4. Missing tenant context is a failure, not a global query.
5. Cross-tenant platform operations require a separate permission, explicit
   target, reason, correlation and append-only audit.

## 5. Isolation-boundary review

Initial storage uses a shared PostgreSQL database and schema. This is a logical
isolation decision, not a claim of complete physical isolation.

Required implementation controls:

- `tenant_id` on all tenant-owned core rows;
- tenant-aware unique indexes;
- composite relationships or equivalent checks preventing cross-tenant joins;
- tenant-required Application and Repository contracts;
- tenant in cache keys, jobs, object paths and audit;
- no empty/default production Tenant;
- PostgreSQL RLS evaluated separately as defense in depth.

## 6. Lifecycle and destructive-operation review

Organization, Tenant, CloudAccount, ProviderConnection and Membership use
explicit status transitions. Tenant offboarding and CloudAccount transfer are
not direct delete/update operations.

Day 21 must use expand-only schema changes. Existing resource, cost and ETL rows
are not assigned a fabricated tenant during Day 20 or Day 21. Day 24 owns a
repeatable backfill into an explicit development Tenant.

## 7. Threat review

| Threat | Day 20 control | Verification owner |
| --- | --- | --- |
| Azure tenant confused with business Tenant | explicit distinct concepts and field names | Day 21 model tests |
| forged tenant selector | selector only requests server-proven Membership authority | Day 22 |
| cross-tenant account/connection reference | same-tenant relationship invariant | Day 21/23 |
| background job loses tenant | job must carry server-created tenant/account scope | Day 22/30 |
| normal admin becomes platform admin | separate platform-level grant | Day 27/30 |
| support operator runs global query | explicit one-target operation and audit | Day 27/29/30 |
| credentials stored in database | ProviderConnection stores opaque reference only | Day 21/26 |
| account silently moved between tenants | transfer requires future controlled workflow | later ADR/workflow |

## 8. Alternatives disposition

| Alternative | Disposition | Reason |
| --- | --- | --- |
| Azure tenant equals business Tenant | Rejected | Provider-specific and wrong customer boundary |
| CloudAccount-only tenancy | Rejected | cannot model membership, shared policy or multi-account workspace |
| schema per Tenant | Deferred | premature migration/operations complexity |
| database per Tenant | Deferred | stronger tier may be added later without changing IDs |
| role column on Membership | Deferred | Day 27 needs permission + typed scope |
| trusted `X-Tenant-Id` | Rejected | direct tenant-escape risk |

## 9. Day 20 acceptance checklist

- [x] Organization is defined.
- [x] Tenant is defined as the primary isolation boundary.
- [x] CloudAccount maps Azure Subscription and future AWS Account.
- [x] ProviderConnection stores no credential material.
- [x] Membership uses stable issuer + subject identity.
- [x] Tenant/account scope hierarchy is defined.
- [x] Human, service and background identity sources are defined.
- [x] Azure tenant and business Tenant are explicitly separated.
- [x] Platform administrator path is explicit and auditable.
- [x] Shared-schema isolation requirements are defined.
- [x] Day 21～30 implementation and negative tests are mapped.
- [x] No Day 21 schema or migration work was pulled into Day 20.

## 10. Decision

**Day 20 Complete**

ADR-0003 is accepted. Day 21 may begin with the Domain, EF configuration and
expand-only migration defined by this model.
