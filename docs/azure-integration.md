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

## Day 6 Cost Management

`AzureCostProvider` calls the subscription-scope Cost Management Query REST API
with `DefaultAzureCredential`. It requests custom daily `PreTaxCost`, grouped by
`ServiceName` and `ResourceGroup`, and maps response columns by name because the
service returns dynamic column and row arrays.

The implementation uses API version `2025-03-01`. If cost data is empty or
unavailable, explicitly marked sample rows keep the POC demonstrable.
`AzureCost:ForceSampleData` provides deterministic end-to-end verification.

## Day 7 Cost ETL

The cost pipeline is available through both host types:

```text
POST /api/admin/sync/azure/costs
Etl__Job=Costs dotnet run --project src/FinOps.Worker
```

Both entry points use `CloudCostSyncService`, write `azure-cost-sync` execution
history, and call the same idempotent repository. The Worker defaults to
`Resources`; `Etl:Job` explicitly selects `Resources` or `Costs`.

Read APIs aggregate in PostgreSQL and return normalized DTOs:

- `GET /api/costs/daily`
- `GET /api/costs/by-service`
- `GET /api/costs/by-resource-group`

Service and resource-group percentages are calculated independently per
currency, so values from different billing currencies are never combined.

## Day 4 Resource Graph Inventory

`AzureResourceInventoryProvider` queries Azure Resource Graph with:

```kusto
Resources
| project id, name, type, location, resourceGroup, subscriptionId, tags
| order by id asc
```

The provider requests object-array results in pages of 1,000 records and follows
the Resource Graph skip token until all pages are read. Azure SDK response types
remain inside Infrastructure; Application receives normalized
`CloudResourceDto` records.

The Worker is a one-shot ETL host for Day 4:

1. Apply pending EF Core migrations.
2. Read Azure resources through `ICloudResourceInventoryProvider`.
3. Upsert them into PostgreSQL through `ICloudResourceRepository`.
4. Log retrieved, inserted, and updated counts.
5. Exit.

Run a normal sync:

```powershell
docker compose up -d
dotnet run --project src/FinOps.Worker
```

Run the complete temporary Azure deployment, double sync, idempotency check,
and cleanup:

```powershell
./scripts/Test-AzureResourceInventory.ps1
```
