# Day 26 - Microsoft Entra Development Integration

## Status

Implemented and merged. Source day report remains `Validation`.

## Commit Evidence

- `2189cde` - `feat: integrate Microsoft Entra development identity`
- `b3efe97` - `Merge pull request #12 from miku-wwl/feat/day26-entra-development-integration`

## Goal

Use real Microsoft Entra ID tokens to call the local API in development, while
keeping API caller identity separate from Azure Provider runtime identity.

## Implemented

- repeatable development app registration initialization script;
- API app registration exposing delegated `access_as_user`;
- public local development client using Device Code flow;
- cleanup script with dry-run default and exact Tenant confirmation;
- real-token E2E script;
- OIDC metadata and JWKS validation evidence;
- signing-key rollover regression;
- metadata failure regression.

## Verification

The tracked report records successful real-token evidence:

- tenant-specific issuer;
- API audience;
- delegated scope;
- signed JWT `kid` present in Microsoft Entra JWKS;
- no credentials on either app registration;
- 403 before Active Membership existed;
- Membership mapping from token `iss/sub`;
- local API accepted the real token and established TenantContext;
- tenant-aware cost endpoint returned 200;
- temporary PostgreSQL database cleanup.

Current local snapshot from this documentation migration:

- `dotnet test FinOpsPlatform.slnx --no-restore`: 84 passed, 1 skipped.

Evidence:

- [docs/archive/phase-2/day-26-entra-development-integration.md](../archive/phase-2/day-26-entra-development-integration.md)
- [docs/archive/adr/ADR-0004-entra-and-development-identity.md](../archive/adr/ADR-0004-entra-and-development-identity.md)
- [scripts/Initialize-DevelopmentEntraIdentity.ps1](../../scripts/Initialize-DevelopmentEntraIdentity.ps1)
- [scripts/Test-EntraOidcIntegration.ps1](../../scripts/Test-EntraOidcIntegration.ps1)
- [scripts/Remove-DevelopmentEntraIdentity.ps1](../../scripts/Remove-DevelopmentEntraIdentity.ps1)

## Boundaries

Day 26 did not enforce the delegated scope as an authorization policy. Existing
business endpoints remain effectively unprotected until Day 28. Azure Provider
runtime identity still uses the local development credential chain.
