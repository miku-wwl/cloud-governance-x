# Day 25 OIDC Bearer Authentication

Date: 2026-06-21
Phase: 2 — Identity, tenancy, RBAC and audit
Status: Validation

## 1. Outcome

The API now has a standard ASP.NET Core JWT Bearer authentication boundary for
tokens issued by an external OIDC Provider.

The implementation validates:

- cryptographic signature;
- expiration and token lifetime;
- issuer;
- audience;
- signed-token and expiration requirements.

It does not issue tokens, store passwords or implement an identity system.

## 2. Configuration

Configuration is under `Authentication:Oidc`:

```json
{
  "Authentication": {
    "Oidc": {
      "Enabled": false,
      "Authority": "",
      "Audience": "",
      "RequireHttpsMetadata": true,
      "ClockSkewSeconds": 60
    }
  }
}
```

Authentication is disabled in the committed local default. When disabled, a
Bearer token cannot establish an authenticated principal even if the remaining
settings contain valid values.

When enabled:

- Authority must be an absolute URI;
- Audience must be non-empty;
- clock skew must be between zero and 300 seconds;
- OIDC metadata uses HTTPS by default.

The configuration contains no client secret, private key or token.

## 3. Request trust flow

The API request pipeline is:

```text
Exception handling
  -> JWT Bearer authentication
  -> synthetic E2E identity, only in the E2E environment
  -> HTTP TenantContext and Membership validation
  -> authorization middleware
  -> endpoint
```

`MapInboundClaims` is disabled so the original OIDC `iss` and `sub` Claims
remain available. `HttpTenantContextMiddleware` then verifies that the subject
has an Active Membership in the explicitly selected Tenant before creating a
trusted context.

The E2E identity remains a separate test-only adapter and still fails startup
when enabled outside the E2E environment.

## 4. Anonymous boundary

The following endpoints are explicitly anonymous:

- `/`;
- `/health`;
- `/health/live`.

This preserves liveness and readiness access when Day 28 applies authorization
to the complete endpoint surface.

Day 25 does not attach authorization policies to existing business endpoints.
Authentication answers who the caller is; Day 27 defines permissions and
scopes, and Day 28 requires those policies across every endpoint.

## 5. Verification

In-memory integration tests use ephemeral RSA keys and a static OIDC metadata
configuration. They do not contact Microsoft Entra ID or any other external
service.

The tests verify:

- a protected endpoint rejects a missing token;
- a valid signed token succeeds;
- raw issuer and subject Claims are preserved;
- expired tokens are rejected;
- a wrong issuer is rejected;
- a wrong audience is rejected;
- an untrusted signature is rejected;
- disabled authentication rejects an otherwise valid token;
- health remains anonymous;
- invalid enabled configuration fails validation;
- HTTPS metadata configuration rejects an HTTP Authority at startup;
- a valid token enters Membership validation and establishes the expected
  trusted TenantContext.

The current local solution run completed with 82 tests passed and one
PostgreSQL integration test skipped because its opt-in database environment
was not enabled. Build completed with zero warnings and zero errors.

## 6. Deliberate boundaries

- Day 26 owns real Microsoft Entra ID metadata, token acquisition, development
  identity policy and key-rotation evidence.
- Day 27 owns permission and scope policy-based RBAC.
- Day 28 owns protection of every existing business endpoint, correlation ID
  and stable authorization errors.
- No Azure resource or Terraform change is part of Day 25.
- No database schema, migration, Domain or Repository change is part of Day 25.

## 7. Remaining risk

`RISK-0001` remains open. Token validation exists, but current business
endpoints are not yet required to authenticate or satisfy an authorization
policy. The API must remain local and non-public until Days 27–28 close that
boundary.

Real OIDC metadata availability, Microsoft Entra application configuration and
signing-key rotation remain Day 26 evidence. Day 25 only proves the provider-
independent validation boundary.
