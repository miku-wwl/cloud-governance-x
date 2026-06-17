# Azure Day 2 Infrastructure

This Terraform root creates the minimum Azure foundation used by later days:

- Resource Group
- StorageV2 account with Standard LRS replication
- Basic Service Bus namespace
- `governance-events` Service Bus queue
- Optional Log Analytics workspace
- Mandatory FinOps and governance tags

Authenticate with Azure CLI before running Terraform:

```powershell
az login
az account show
```

Run manually:

```powershell
Push-Location terraform/azure
terraform init
terraform fmt -check
terraform validate
terraform plan -out day2.tfplan
terraform apply day2.tfplan
terraform output
terraform destroy
Pop-Location
```

For a complete create, verify, and destroy lifecycle from the repository root:

```powershell
./scripts/Test-AzureTerraformLifecycle.ps1
```

If the current shell is already inside `terraform/azure`, run the script as
`../../scripts/Test-AzureTerraformLifecycle.ps1` or return to the repository
root first.

Local state, plans, variable overrides, and evidence files are excluded from
Git. The lifecycle script removes its runtime artifacts after a verified
destroy unless `-KeepEvidence` is specified.
