# ADR-0004: Microsoft Entra and Development Identity

Date: 2026-06-21
Status: Accepted
Owner: Security Owner

## Context

Day 25 added provider-independent JWT Bearer validation, but the API still
lacked a real identity provider and a repeatable local-development token flow.
The current Azure CLI user identity is suitable for Azure SDK development, but
an Azure Resource Manager token is not an access token for the FinOps API.

App registrations are Microsoft Entra directory objects. They are not Azure
Resource Manager resources, do not belong to a subscription or Resource Group,
and are not deleted when a Resource Group is removed.

## Decision

Development uses two single-tenant Microsoft Entra app registrations:

1. `cloud-governance-x-api-dev`
   - represents the FinOps API;
   - uses Microsoft identity platform v2 access tokens;
   - exposes `api://<api-client-id>/access_as_user`;
   - has no redirect URI, credential, certificate or client secret.
2. `cloud-governance-x-local-dev-client`
   - represents an operator's local command-line session;
   - is a public client;
   - uses OAuth 2.0 Device Code flow;
   - receives only the delegated `access_as_user` permission;
   - has no client secret.

The API validates the tenant-specific v2 issuer, API client ID audience,
signature and lifetime. Tenant authority is still established independently by
matching the token's `iss` and `sub` against an Active Membership.

The development app registrations are created and removed by reviewed scripts.
Their object IDs and application IDs may be stored as non-secret evidence.
Access tokens, refresh tokens and device codes must never be committed or
written to normal logs.

## Consequences

- local development proves the same external-token trust boundary later used
  by deployed environments;
- a leaked client ID does not authenticate an application because client IDs
  are public identifiers;
- Device Code flow requires an interactive user and cannot silently become a
  production service identity;
- the app registrations survive Resource Group deletion and need an explicit
  Entra cleanup operation;
- Day 27 may evaluate delegated scopes and application roles without changing
  the token acquisition boundary;
- staging and production must use separate registrations and workload identity
  decisions; they must not reuse the development public client.

## Rejected alternatives

### Reuse the Azure CLI application as the product client

Rejected. Azure CLI is a Microsoft-owned first-party client and does not give
this project an explicit, reviewable client registration or permission
lifecycle.

### Use a client secret for local development

Rejected. A public developer client cannot keep a secret, and introducing one
would create unnecessary storage and rotation risk.

### Put Entra objects in the existing AzureRM Terraform root

Rejected for Day 26. The existing Terraform root manages subscription-scoped
Azure resources. Entra directory objects have different ownership, permissions
and lifecycle. A future dedicated `azuread` root can replace the current Graph
scripts after remote state and environment ownership are designed.

### Use one registration for both API and local client

Rejected. Resource-server audience/scope ownership and client token acquisition
are separate responsibilities and need independent lifecycle and review.
