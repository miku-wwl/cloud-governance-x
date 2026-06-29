# Day 6 - Azure Cost POC

## Status

Complete as development baseline.

## Commit Evidence

- `be28bd6` - `feat: complete day 6 Azure cost POC`

## Goal

Prove that Azure Cost Management data can be queried and stored as daily
aggregates.

## Implemented

- Azure Cost Management query path;
- daily cost persistence grouped by service, resource group and currency;
- sample fallback behavior for local learning when external cost data is not
  available.

## Verification

Day 9 reran the cost checks. It also performed a strict run with fallback
disabled and obtained 28 rows of real Azure cost data.

Evidence:

- [docs/archive/reference/04-★★★-azure-integration.md](../archive/reference/04-★★★-azure-integration.md)
- [docs/archive/reference/05-★★★-data-model.md](../archive/reference/05-★★★-data-model.md)
- [docs/archive/phase-0/baseline-verification-summary.md](../archive/phase-0/baseline-verification-summary.md)
- [scripts/Test-AzureCostPoc.ps1](../../scripts/Test-AzureCostPoc.ps1)

## Boundaries

The sample fallback is production-prohibited. Cost semantics remained limited:
no amortized/unblended distinction, charge type, billing period, revision
history, resource-level precision or currency conversion.
