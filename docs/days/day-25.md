# Day 25 - OIDC Bearer Authentication

## Status

Implemented and merged. Source day report remains `Validation`.

## Commit Evidence

- `c405220` - `feat: add OIDC bearer authentication`
- `ad59306` - `Merge pull request #11 from miku-wwl/feat/day25-oidc-bearer-authentication`

## Goal

Add provider-independent ASP.NET Core JWT Bearer validation without implementing
a custom identity system.

## Implemented

- `Authentication:Oidc` configuration;
- optional JWT Bearer authentication;
- issuer, audience, signature, expiration and lifetime validation;
- disabled-by-default local configuration;
- preservation of raw OIDC `iss` and `sub` claims;
- anonymous root and health endpoints;
- in-memory token tests with ephemeral RSA keys and static OIDC metadata.

## Verification

The tracked report records local build with zero warnings and errors, 82 tests
passed and one PostgreSQL integration test skipped because its opt-in database
environment was not enabled.

Evidence:

- [docs/archive/phase-2/day-25-oidc-bearer-authentication.md](../archive/phase-2/day-25-oidc-bearer-authentication.md)
- [src/FinOps.Api/Authentication](../../src/FinOps.Api/Authentication)
- [src/FinOps.Tests/Api/OidcBearerAuthenticationTests.cs](../../src/FinOps.Tests/Api/OidcBearerAuthenticationTests.cs)

## Boundaries

Day 25 did not attach authorization policies to business endpoints. RBAC,
complete endpoint protection, stable 401/403 contracts and audit remained
Days 27-29 work.
