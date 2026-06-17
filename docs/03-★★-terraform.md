# 03 ★★ Terraform

## Day 2 Azure Foundation

The Azure Terraform root is located at `terraform/azure`. It deliberately
contains a small, low-cost foundation that demonstrates the full infrastructure
lifecycle before application code starts consuming Azure SDKs.

The deployment creates:

| Resource | Configuration | Later use |
| --- | --- | --- |
| Resource Group | One isolated demo scope | Lifecycle and resource inventory |
| Storage Account | StorageV2, Standard LRS | ETL artifacts and future state options |
| Service Bus Namespace | Basic | Planned governance event transport |
| Service Bus Queue | `governance-events` | Anomaly and compliance events |
| Log Analytics | Optional, disabled by default | Future operational logs |

## Governance Tags

Terraform applies these tags to every taggable resource:

- `owner`
- `environment`
- `cost-center`
- `managed-by=terraform`
- `project=cloud-governance-x`

The first three tags become required governance tags in later compliance work.

## Authentication

Local development uses the active Azure CLI identity:

```powershell
az login
az account show
```

No Azure credential is stored in the repository.

## Lifecycle Verification

The reusable verification script runs the complete Day 2 acceptance flow:

```powershell
./scripts/Test-AzureTerraformLifecycle.ps1
```

It initializes providers, validates formatting, creates a saved plan, applies
it, queries Azure Resource Manager through Azure CLI, validates expected
resource types, destroys the deployment, and confirms the resource group no
longer exists. After a verified destroy it removes provider cache, local state,
the saved plan, and evidence by default. Use `-KeepEvidence` when the local JSON
evidence is needed for troubleshooting or a demo.

Use `-KeepResources` only when the resources are needed for the next
non-production development session. Kept resources can continue to incur Azure
charges:

```powershell
./scripts/Test-AzureTerraformLifecycle.ps1 -KeepResources
```

Log Analytics is opt-in because ingestion can create ongoing charges:

```powershell
./scripts/Test-AzureTerraformLifecycle.ps1 -EnableLogAnalytics
```

This root currently requires Terraform `>= 1.9.0` and relies on the committed
`.terraform.lock.hcl` for exact provider versions. A June 18, 2026 refresh saw
local Terraform CLI `1.14.0` while `1.15.6` was available; that is an upgrade
candidate, not a reason to change state or provider locks without a reviewed
`terraform init -upgrade`, release-note check, and plan review.
