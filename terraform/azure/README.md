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
Set-Location terraform/azure
terraform init
terraform fmt -check
terraform validate
terraform plan -out day2.tfplan
terraform apply day2.tfplan
terraform output
terraform destroy
```

For a complete create, verify, and destroy lifecycle from the repository root:

```powershell
./scripts/Test-AzureTerraformLifecycle.ps1
```

Local state, plans, variable overrides, and evidence files are excluded from Git.
