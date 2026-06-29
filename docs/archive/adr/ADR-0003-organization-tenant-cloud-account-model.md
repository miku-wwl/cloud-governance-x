# ADR-0003: Organization, Tenant, CloudAccount and scope model

## Status

Accepted

## Date

2026-06-19

## Owners

- Decision owner: Platform Architect
- Reviewers: Security Owner, Data Owner, Application Owner, Platform SRE

## Context

Cloud Governance X currently stores Azure resource, cost and ETL data without a
business tenant boundary. Azure SDK objects expose an Azure tenant ID, but that
identifier describes a Microsoft Entra directory and is not the platform's
customer, organization or data-isolation boundary.

Phase 2 must establish a stable model before adding schema, trusted tenant
context, OIDC, RBAC and audit. The decision must support:

- one organization with one or more isolated workspaces;
- Azure subscriptions and future AWS accounts;
- multiple Provider credentials or workload identities;
- human and service identities;
- tenant, account and future resource-level authorization scope;
- background jobs that cannot infer tenant from ambient HTTP state;
- an explicitly controlled platform-administrator path;
- future RLS or stronger physical isolation without changing business identity.

This decision addresses RISK-0001, RISK-0002 and RISK-0018, and GAP-001,
GAP-002 and GAP-005. It does not close them; implementation and isolation tests
remain required through Day 30.

## Decision

### Business hierarchy

The canonical hierarchy is:

```text
Organization
    └── Tenant
          ├── Membership
          ├── ProviderConnection
          └── CloudAccount
                └── narrower authorization scopes
```

All internal identifiers are opaque UUIDs. Display names and external Provider
identifiers are attributes, never authorization keys.

### Organization

`Organization` represents the customer, legal or administrative umbrella that
owns one or more platform tenants.

- An Organization may contain multiple Tenants.
- Organization is not the normal row-level data isolation key.
- Organization-wide access is never implied by membership in one Tenant.
- Initial deployments may create one Organization and one Tenant, but code and
  schema must not special-case that cardinality.

Minimum planned attributes:

- `id`
- `display_name`
- `status`
- `created_at`
- `updated_at`

### Tenant

`Tenant` is the primary security, authorization and data-isolation boundary.
Every operational or business row that belongs to a customer workspace must
eventually carry `tenant_id`, directly or through a tenant-bound aggregate with
a composite foreign key.

- A Tenant belongs to exactly one Organization.
- A Tenant cannot contain another Tenant.
- Tenant identity never comes from a query string, route value, arbitrary
  header, Azure tenant ID or cloud account ID.
- Suspension denies new business operations while preserving data for audit
  and controlled recovery.
- Decommissioning is an explicit lifecycle, not a hard cascade delete.

Minimum planned attributes:

- `id`
- `organization_id`
- `slug`
- `display_name`
- `status`
- `created_at`
- `updated_at`

The tenant slug is unique within an Organization. Authorization and joins use
the UUID, not the slug.

### CloudAccount

`CloudAccount` is the platform's normalized account boundary for a Provider:

- Azure: one Subscription;
- AWS: one AWS Account;
- future Providers: their equivalent billable/resource ownership account.

An Azure tenant/directory is Provider metadata and may be recorded as
`provider_directory_id`; it is not `tenant_id`.

A CloudAccount:

- belongs to exactly one business Tenant at a time;
- has a Provider and an immutable normalized external account ID;
- references the ProviderConnection used to access it;
- carries onboarding and operational status;
- does not contain credentials;
- cannot be silently reassigned to another Tenant.

Moving an account between Tenants requires a future explicit offboard/transfer
workflow with authorization, audit and data reconciliation. Day 21 must not
implement transfer as an unrestricted `tenant_id` update.

Minimum planned attributes:

- `id`
- `tenant_id`
- `provider`
- `external_account_id`
- `provider_directory_id`
- `provider_connection_id`
- `display_name`
- `status`
- `environment`
- `created_at`
- `updated_at`

The active identity is unique by `(provider, external_account_id)`. Every
tenant-owned foreign key and uniqueness rule must also preserve the tenant
boundary.

### ProviderConnection

`ProviderConnection` represents the configuration and identity binding used to
access one Provider.

- It belongs to exactly one Tenant.
- It may serve one or more CloudAccounts in that Tenant.
- It stores non-secret metadata, capability state and a secret/workload
  identity reference.
- It never stores an access token, client secret or raw credential.
- A CloudAccount cannot reference a ProviderConnection from another Tenant.

Minimum planned attributes:

- `id`
- `tenant_id`
- `provider`
- `display_name`
- `credential_reference`
- `status`
- `last_validated_at`
- `created_at`
- `updated_at`

`credential_reference` is an opaque locator for a future secret store or
workload identity configuration. Local Azure CLI identity remains a development
adapter and must not become persisted tenant identity.

### Membership and identity subjects

`Membership` grants an external identity subject access to one Tenant.

The stable external subject key is:

```text
issuer + subject
```

Email, display name and Entra object display attributes are mutable profile
data and cannot be authorization keys.

Membership supports human and service subjects. It establishes tenant
association and lifecycle only; Day 27 will define role and permission
assignments. A user may have Memberships in multiple Tenants, but each request
or job executes against one explicit effective Tenant.

Minimum planned attributes:

- `id`
- `tenant_id`
- `issuer`
- `subject`
- `subject_type`
- `display_name`
- `status`
- `created_at`
- `updated_at`

The active identity is unique by `(tenant_id, issuer, subject)`.

### Scope model

Authorization scope is a typed reference, not an arbitrary string prefix.

The initial hierarchy is:

```text
Tenant
    └── CloudAccount
          └── Provider-specific resource scope (future)
```

The canonical scope representation is:

- `scope_type`
- `tenant_id`
- optional `scope_id`

Rules:

- every non-platform scope includes `tenant_id`;
- a CloudAccount scope must resolve to an account in the same Tenant;
- narrower scopes cannot grant access outside their parent;
- Provider-native resource IDs are targets or metadata, not trusted scope by
  themselves;
- wildcard scope is not represented by null tenant;
- absence of a trusted scope is an authorization failure.

Day 27 may add permission assignments to these typed scopes without changing
the tenant identity model.

### Trusted identity and tenant selection

Human HTTP requests will obtain identity from a validated OIDC token. The
effective tenant must be selected from the authenticated subject's active
Memberships and validated server-side.

The client may request a tenant selection only by an opaque tenant identifier;
the server must verify membership or platform-level authority. A route, query,
header or body value never creates authority.

Background work has no ambient user identity. Each job definition/message must
carry a server-created tenant and account scope. The Worker must reject missing
or inconsistent tenant context before resolving a Repository or Provider.

### Platform administrator path

Cross-tenant platform administration is a separate platform-level grant, not a
Tenant Membership and not an implicit wildcard.

Every cross-tenant operation must:

1. authenticate a platform subject;
2. require a dedicated platform permission;
3. name one explicit target Tenant;
4. record reason, correlation ID and operation result;
5. pass normal tenant-bound Repository and Provider checks for that target;
6. emit append-only audit evidence.

Routine data queries must not execute with platform-wide scope. Break-glass
access, when introduced, requires separate approval, short duration and audit.

### Isolation strategy

The initial implementation uses one PostgreSQL database and shared schema with
explicit `tenant_id`.

Required invariants:

- tenant-owned primary lookup paths include `tenant_id`;
- tenant-owned unique indexes include `tenant_id` unless a documented global
  Provider identity requires an additional global constraint;
- tenant-owned relationships use composite keys or equivalent validation that
  prevents cross-tenant references;
- Application and Repository contracts require tenant context;
- cache keys, job messages, object paths and audit records include tenant;
- no default or empty Tenant exists in production execution.

PostgreSQL Row Level Security is deferred to ADR-0005 as defense in depth. RLS
must not replace explicit Application and Repository tenant boundaries.

### Lifecycle

Planned lifecycle values are intentionally small:

| Aggregate | States |
| --- | --- |
| Organization | Active, Suspended, Decommissioning |
| Tenant | Active, Suspended, Decommissioning |
| CloudAccount | Pending, Active, Suspended, Disconnected |
| ProviderConnection | Pending, Active, Degraded, Revoked |
| Membership | Invited, Active, Suspended, Revoked |

State transitions will be implemented and tested in the owning Day. Hard delete
is not the normal lifecycle operation for tenant-owned operational data.

## Alternatives considered

### Treat Azure tenant as the business Tenant

Rejected. One business customer may use multiple Entra directories, and one
directory may contain subscriptions belonging to different business
workspaces. It also does not support AWS.

### Use CloudAccount as the only tenant boundary

Rejected. Membership, organization-wide policy, audit, shared cost allocation
and multi-account views require a stable business workspace above Provider
accounts.

### Schema per Tenant

Deferred. It increases migration, connection and operational complexity before
the domain model is stable. The logical model must remain compatible with this
option for stronger isolation tiers.

### Database per Tenant

Deferred. It provides stronger isolation but adds provisioning, migration,
backup and cross-tenant operations overhead. It may be introduced for large or
regulated customers without changing public business identifiers.

### Store role directly on Membership

Deferred to Day 27. A single role column cannot express platform permissions,
account scope, future custom roles or separate service permissions.

### Trust an `X-Tenant-Id` header

Rejected. Client-controlled tenant context creates a direct tenant-escape
path. Any tenant selector is only a request to use authority already proven by
the server.

## Consequences

Benefits:

- business tenancy is Provider-neutral;
- Azure and AWS accounts share one stable model;
- tenant escape has explicit schema, Application and authorization controls;
- OIDC, background jobs, RBAC and audit have a common subject/scope language;
- stronger isolation tiers remain possible.

Costs and obligations:

- every existing core table and Repository must become tenant-aware;
- Day 24 must backfill current data into a controlled development Tenant;
- account onboarding and transfer need explicit workflows;
- cross-tenant support operations require dedicated audit and authorization;
- shared-schema isolation requires extensive negative testing and later RLS
  evaluation.

New risks:

- missing tenant predicates can expose data until all repositories are migrated;
- incorrect account-to-connection validation can cross credentials or scope;
- platform administrator privileges can become an ambient bypass if not kept
  separate and audited.

## Implementation hooks

- Day 21:
  - add Organization, Tenant, CloudAccount, ProviderConnection and Membership
    Domain models;
  - add EF configurations and expand-only migration;
  - add tenant-aware composite keys and relationship tests.
- Day 22:
  - add trusted HTTP and background `TenantContext`;
  - reject missing, forged and inconsistent tenant selection.
- Day 23:
  - require tenant in all Repository contracts and queries;
  - add tenant A/B isolation and IDOR tests.
- Day 24:
  - create one explicit development Organization/Tenant;
  - backfill existing rows without deleting or silently reclassifying data.
- Day 25～28:
  - bind OIDC subjects to Membership;
  - implement typed permission/scope checks;
  - protect every endpoint.
- Day 29:
  - add append-only audit for membership, connection, account and
    cross-tenant administration.
- Day 30:
  - execute tenant escape, RBAC, service identity and audit E2E.

Affected long-lived documents:

- `docs/archive/phase-0/adr-backlog.md`
- `docs/archive/phase-0/risk-register.md`
- `docs/archive/phase-0/production-gap-register.md`
- `docs/archive/phase-2/day-20-tenancy-model-review.md`

## Verification

Day 20 verification:

- the six required concepts have explicit ownership and cardinality;
- Azure tenant and business Tenant are distinguished;
- human, service and background identity sources are identified;
- account and tenant scope rules are enforceable;
- platform administration is explicit, target-bound and auditable;
- future implementation and negative tests are mapped to Day 21～30.

Required later negative tests:

- a client-supplied tenant without Membership is rejected;
- missing TenantContext fails closed;
- tenant A cannot reference tenant B's CloudAccount or ProviderConnection;
- duplicate Provider account onboarding is rejected;
- background jobs cannot start without tenant/account scope;
- a normal Tenant administrator cannot use platform scope;
- a platform administrator must name and audit one target Tenant;
- cross-tenant foreign keys and unique identities cannot be created;
- suspended/revoked Membership and ProviderConnection cannot be used.

## Revisit triggers

Review or supersede this ADR when:

- schema-per-tenant or database-per-tenant is required;
- a customer needs regional/data-residency isolation;
- CloudAccount transfer between Tenants is implemented;
- a Provider does not map cleanly to the account model;
- delegated administration requires organization-level policy inheritance;
- PostgreSQL RLS design changes the enforcement model;
- platform administration becomes a separate service or control plane.
