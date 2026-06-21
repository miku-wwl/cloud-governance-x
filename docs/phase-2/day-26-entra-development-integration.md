# Day 26 Microsoft Entra Development Integration

Date: 2026-06-21
Phase: 2 — Identity, tenancy, RBAC and audit
Status: Validation

## 1. Outcome

The development environment now uses real Microsoft Entra ID tokens to call
the local FinOps API.

Two single-tenant directory applications were created:

- `cloud-governance-x-api-dev`, which exposes the delegated
  `access_as_user` scope;
- `cloud-governance-x-local-dev-client`, a public client that uses Device Code
  flow.

Neither registration contains a password credential, client secret or
certificate. They are Entra Tenant directory objects, not Azure subscription or
Resource Group resources.

## 2. Accepted identity boundary

ADR-0004 separates four identities:

1. the human Microsoft Entra user;
2. the local public client requesting a delegated token;
3. the FinOps API resource and audience;
4. the Azure Provider credential used by backend Azure SDK calls.

Day 26 changes the first three. It does not replace the Azure Provider's
development `DefaultAzureCredential`; production workload identity remains a
later Azure Provider stage.

The local public client cannot keep a secret and cannot silently authenticate
as a background service. Staging, production and the future React SPA must use
separate registrations.

## 3. Repeatable Entra lifecycle

The initialization script:

```powershell
./scripts/Initialize-DevelopmentEntraIdentity.ps1
```

- verifies the active Tenant and signed-in user;
- creates or verifies both app registrations;
- creates their Service Principals;
- sets the signed-in user as owner;
- creates only the delegated permission grant required by the current user;
- validates existing objects instead of silently replacing incompatible ones;
- writes non-secret IDs to the Git-ignored
  `tmp/day26-entra-development.json`.

The cleanup script is dry-run by default:

```powershell
./scripts/Remove-DevelopmentEntraIdentity.ps1 `
  -ConfirmTenantId <tenant-id>
```

Deletion requires both the exact Tenant ID and `-Apply`. Resource Group
deletion cannot remove these objects.

## 4. Real-token verification

The real E2E uses OAuth 2.0 Device Code flow:

```powershell
./scripts/Test-EntraOidcIntegration.ps1 -RequestDeviceCode
./scripts/Test-EntraOidcIntegration.ps1
```

The first command prints a short-lived, one-time user code. It is not a
password, secret or token. The second command exchanges the approved device
request without printing the access or refresh token. Expired, declined or
invalid pending Device Code state is removed and must be explicitly restarted.

The successful evidence verified:

- tenant-specific Microsoft identity platform v2 issuer;
- API Application Client ID audience;
- delegated `access_as_user` scope;
- a signed JWT with a `kid` present in current Microsoft Entra JWKS;
- no credentials on either app registration;
- the same real token received HTTP 403 before an Active Membership existed;
- token `iss/sub` mapped to an Active Membership;
- the local API accepted the real token and established TenantContext;
- the tenant-aware cost endpoint returned HTTP 200;
- the temporary PostgreSQL database was removed afterward.

## 5. Metadata and key rotation

The tenant-specific OIDC metadata endpoint returned a matching issuer, token
endpoint and JWKS URI. Current signing keys use RS256.

An automated configuration-manager regression verifies rollover behavior:

1. a token signed with an unknown `kid` is rejected;
2. the JWT handler requests OIDC configuration refresh;
3. the next request succeeds after the refreshed signing key is available.

This deliberately accepts a brief 401 window during key discovery instead of
weakening signature validation or accepting an unknown key.

An additional regression makes the OIDC configuration manager fail while
retrieving metadata/JWKS. A correctly formed and signed token still receives
HTTP 401; metadata failure never bypasses issuer or signing-key validation.

## 6. Deliberate boundaries

- Day 27 defines permission and scope policy-based RBAC.
- Day 28 protects every business endpoint and stabilizes 401/403 contracts.
- Day 29 adds append-only audit.
- Day 30 performs the Phase 2 tenant escape and authorization gate.
- Azure Provider workload identity is not part of Day 26.
- No Azure subscription resource, Resource Group or Terraform resource was
  created.
- No database schema or migration changed.

## 7. Remaining risk

The real caller identity is proven, but the business endpoint surface remains
anonymous until Day 28. The new delegated scope is evidence inside the token;
it is not yet enforced by a Day 27 authorization policy.

The development app registrations survive Resource Group deletion. They must
remain owner-tracked and be explicitly deleted when this development
environment is retired. The real E2E also invokes the cleanup script with a
different Tenant ID and requires rejection before any Graph lookup or delete.
