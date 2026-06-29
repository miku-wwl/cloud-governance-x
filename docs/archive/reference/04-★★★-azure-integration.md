# 04 ★★★ Azure 集成

## Day 3 认证

本地开发使用 `DefaultAzureCredential`。在当前开发配置下，它会发现已登录的 Azure CLI identity：

```powershell
az login
az account show
```

仓库不保存 access token、client secret 或 connection string。

如果需要指定 tenant，可配置 `Azure__TenantId`。留空时，credential chain 会使用当前 Azure CLI tenant：

```powershell
$env:Azure__TenantId = "<tenant-id>"
```

同一套代码未来可以在不修改 Application 或 API 层的情况下，使用 environment-based service principal credential 或 Managed Identity。

## 依赖方向

Azure SDK package 只由 `FinOps.Infrastructure` 引用。

```text
FinOps.Api
    -> IAzureSubscriptionReader (Application)
        -> AzureSubscriptionReader (Infrastructure)
            -> DefaultAzureCredential + ArmClient
```

API endpoint 不实例化 Azure client，也不依赖 Azure SDK request 或 response 类型。

## Subscription 验证

先登录 Azure CLI，再启动 API：

```powershell
dotnet run --project src/FinOps.Migrator
dotnet run --project src/FinOps.Api --urls http://localhost:5000
```

调用：

```powershell
Invoke-RestMethod http://localhost:5000/api/cloud/azure/subscriptions
```

也可以从仓库根目录执行可重复 Day 3 E2E：

```powershell
./scripts/Test-AzureSdkIntegration.ps1
```

脚本会 build solution、启动 API、调用 endpoint、把返回字段与当前 `az account show` 结果逐项比较，然后停止 API。

响应包含归一化 subscription 数据：

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

## Provider contract

Day 3 还定义了后续 ETL 所需的 provider-neutral contract：

- `ICloudResourceInventoryProvider`
- `ICloudCostProvider`
- `ICloudComplianceProvider`

inventory、cost 和 compliance 的 Azure 实现在对应 Day 进行真实端到端验证时加入。

## Day 6 Cost Management

`AzureCostProvider` 使用 `DefaultAzureCredential` 调用 subscription-scope Cost Management Query REST API。它请求自定义日粒度 `PreTaxCost`，按 `ServiceName` 和 `ResourceGroup` 分组。由于服务返回动态 column 和 row array，实现按列名映射响应。

当前实现使用 API version `2025-03-01`。当 cost data 为空或不可用时，明确标记的 sample row 可保持 POC 可演示。`AzureCost:ForceSampleData` 用于确定性 E2E 验证。API version 是实现契约的一部分；修改 Cost Management dimension、billing scope 或 account type 时必须重新复核。

## Day 7 Cost ETL

cost pipeline 可通过两种 host 触发：

```text
POST /api/admin/sync/azure/costs
Etl__Job=Costs dotnet run --project src/FinOps.Worker
```

两个入口都使用 `CloudCostSyncService`，写入 `azure-cost-sync` 执行历史，并调用同一个幂等 repository。Worker 默认执行 `Resources`；`Etl:Job` 显式选择 `Resources` 或 `Costs`。

读取 API 在 PostgreSQL 中聚合并返回归一化 DTO：

- `GET /api/costs/daily`
- `GET /api/costs/by-service`
- `GET /api/costs/by-resource-group`

service 和 resource-group 百分比按 currency 独立计算，不能把不同 billing currency 的值混合。

## Day 4 Resource Graph Inventory

`AzureResourceInventoryProvider` 使用以下 Azure Resource Graph 查询：

```kusto
Resources
| project id, name, type, location, resourceGroup, subscriptionId, tags
| order by id asc
```

Provider 以每页 1000 条请求 object-array 结果，并跟随 Resource Graph skip token，直到读取所有页面。这是 full-scan 开发实现，不是带 checkpoint 的生产 crawler。Azure SDK response type 保持在 Infrastructure 内；Application 接收归一化 `CloudResourceDto`。

Day 4 的 Worker 是一次性 ETL host：

1. 通过 `ICloudResourceInventoryProvider` 读取 Azure resources。
2. 通过 `ICloudResourceRepository` upsert 到 PostgreSQL。
3. 记录 retrieved、inserted、updated 数量。
4. 退出。

普通同步：

```powershell
docker compose up -d
dotnet run --project src/FinOps.Migrator
dotnet run --project src/FinOps.Worker
```

完整临时 Azure deployment、双次 sync、幂等检查和清理：

```powershell
./scripts/Test-AzureResourceInventory.ps1
```
