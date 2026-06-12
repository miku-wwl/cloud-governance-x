# Azure Integration

## Day 3 Authentication

Local development authenticates with `DefaultAzureCredential`. With the current
developer setup it discovers the signed-in Azure CLI identity:

```powershell
az login
az account show
```

No access token, client secret, or connection string is stored in this
repository.

For a specific tenant, configure `Azure__TenantId`. Leaving it empty allows the
credential chain to use the active Azure CLI tenant:

```powershell
$env:Azure__TenantId = "<tenant-id>"
```

The same code can later use environment-based service principal credentials or
Managed Identity without changing the Application or API layers.

## Dependency Direction

The Azure SDK packages are referenced only by `FinOps.Infrastructure`.

```text
FinOps.Api
    -> IAzureSubscriptionReader (Application)
        -> AzureSubscriptionReader (Infrastructure)
            -> DefaultAzureCredential + ArmClient
```

The API endpoint does not instantiate Azure clients and does not depend on
Azure SDK request or response types.

## Subscription Verification

Start the API after signing in with Azure CLI:

```powershell
dotnet run --project src/FinOps.Api --urls http://localhost:5000
```

Call:

```powershell
Invoke-RestMethod http://localhost:5000/api/cloud/azure/subscriptions
```

Or execute the repeatable Day 3 end-to-end check from the repository root:

```powershell
./scripts/Test-AzureSdkIntegration.ps1
```

The script builds the solution, starts the API, calls the endpoint, compares all
returned fields with the active `az account show` result, and stops the API.

The response contains normalized subscription data:

```json
[
  {
    "subscriptionId": "<subscription-id>",
    "displayName": "<subscription-name>",
    "tenantId": "<tenant-id>",
    "state": "Enabled"
  }
]
```

## Provider Contracts

Day 3 also defines the provider-neutral contracts required by upcoming ETL
work:

- `ICloudResourceInventoryProvider`
- `ICloudCostProvider`
- `ICloudComplianceProvider`

Azure implementations for inventory, costs, and compliance are added on the
days where each integration is exercised end to end.
