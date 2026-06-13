# Cloud Governance X

基于 .NET 10、Terraform、PostgreSQL 和 React 构建的多云 FinOps 与资源治理平台。

当前版本已完成 Day 1～7：工程骨架、Azure Terraform、Azure SDK 认证、
Azure Resource Graph 资源清单同步、正式 ETL 执行追踪和 Azure Cost
Management 成本 ETL 与查询 API，包含
Web API、后台 Worker、Clean Architecture 基础分层、PostgreSQL 本地环境、
健康检查、可重复验证的 Azure 资源生命周期，以及通过
`DefaultAzureCredential` 读取 Azure 订阅和资源清单并写入 PostgreSQL 的能力。

## 项目结构

```text
src/
├── FinOps.Api/              # HTTP API 与应用入口
├── FinOps.Application/      # 用例、端口与应用服务
├── FinOps.Domain/           # 核心领域模型
├── FinOps.Infrastructure/   # 数据库、云 SDK 与外部服务实现
├── FinOps.Worker/           # ETL 与异步任务宿主
└── FinOps.Tests/            # 自动化测试
terraform/
└── azure/                   # Day 2 Azure 基础设施
scripts/                     # 端到端验收脚本
docs/                        # 架构与运行文档
```

依赖方向：

```text
Api/Worker -> Infrastructure -> Application -> Domain
```

`Application` 和 `Domain` 不依赖基础设施或宿主项目。

## 环境要求

- .NET SDK 10.0.300 或兼容的 10.0 SDK
- Docker Desktop / Docker Compose
- Azure CLI
- Terraform 1.9+

根目录的 `global.json` 将 SDK 基线固定为 .NET 10.0.300，并允许使用更新的
.NET 10 feature band。它用于避免开发机和 CI 意外选择 .NET 9 或未来的
.NET 11，不是构建产物。

## 本地启动

启动 PostgreSQL：

```powershell
docker compose up -d
docker compose ps
```

构建并测试：

```powershell
dotnet tool restore
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

启动 API：

```powershell
dotnet run --project src/FinOps.Api --urls http://localhost:5000
```

执行一次 Azure Resource Graph 资源同步：

```powershell
dotnet run --project src/FinOps.Worker
```

Worker 会应用 EF Core migration、同步资源、输出处理数量后退出。

## 健康检查

| 地址 | 用途 |
| --- | --- |
| `GET /health/live` | 检查 API 进程是否存活 |
| `GET /health` | 检查 API 是否就绪，包括 PostgreSQL 登录和查询 |

示例：

```powershell
Invoke-WebRequest http://localhost:5000/health/live
Invoke-WebRequest http://localhost:5000/health
```

PostgreSQL readiness 会使用 Npgsql 建立真实数据库连接并执行 `SELECT 1`。
资源数据通过 EF Core migration 写入 `cloud_resources`。

## 配置

默认开发数据库配置位于 `src/FinOps.Api/appsettings.json`：

```json
{
  "PostgreSql": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "finops",
    "Username": "finops",
    "Password": "finops_dev_password",
    "TimeoutSeconds": 3
  }
}
```

这里的密码只用于本机 Docker 开发容器，不是生产凭据。部署环境必须通过环境变量
或密钥服务覆盖数据库配置；仓库不会提交 `.env` 或其他 `.env.*` 文件。

环境变量可使用双下划线覆盖，例如：

```powershell
$env:PostgreSql__Host = "localhost"
$env:PostgreSql__Port = "5432"
```

停止本地数据库：

```powershell
docker compose down
```

如需同时删除本地数据库卷：

```powershell
docker compose down --volumes
```

## Azure Terraform

Day 2 创建 Resource Group、Storage Account、Service Bus Namespace 和 Queue，
并为资源统一设置 `owner`、`environment`、`cost-center` 等治理标签。Log
Analytics 可选且默认关闭，以避免产生持续日志摄取费用。

先确认 Azure CLI 已登录：

```powershell
az account show
```

执行完整的创建、核验和销毁闭环：

```powershell
./scripts/Test-AzureTerraformLifecycle.ps1
```

详细配置和手动命令见
[`terraform/azure/README.md`](terraform/azure/README.md) 与
[`docs/terraform.md`](docs/terraform.md)。

## Azure SDK 认证

本地开发使用 Azure CLI 登录和 `DefaultAzureCredential`：

```powershell
az login
az account show
dotnet run --project src/FinOps.Api --urls http://localhost:5000
```

验证真实 Azure 订阅读取：

```powershell
Invoke-RestMethod http://localhost:5000/api/cloud/azure/subscriptions
```

Azure SDK 仅由 `FinOps.Infrastructure` 引用；API 通过 Application 层的
`IAzureSubscriptionReader` 调用，不直接依赖 Azure SDK。详细设计见
[`docs/azure-integration.md`](docs/azure-integration.md)。

完整 Day 3 端到端验收：

```powershell
./scripts/Test-AzureSdkIntegration.ps1
```

## Azure Resource Inventory

Day 4 Worker 使用 Azure Resource Graph 分页读取：

```text
id, name, type, location, resourceGroup, subscriptionId, tags
```

结果归一化后写入 PostgreSQL `cloud_resources`。数据库唯一索引
`(provider, resource_id_normalized)` 保证重复同步不会插入重复资源，
`FirstSeenAt` 保持首次发现时间，`LastSeenAt` 每次同步更新。

完整 Day 4 端到端验收：

```powershell
./scripts/Test-AzureResourceInventory.ps1
```

该脚本使用独立的 `finops_day4` 测试数据库，临时创建 Azure 资源，运行两次
Worker 并验证幂等性，随后销毁 Azure 资源、删除测试数据库和本地 Terraform
运行产物。数据模型见 [`docs/data-model.md`](docs/data-model.md)。

## Azure Resource ETL

Day 5 将资源同步正式化为可审计 ETL Job。Worker 与 API 手动触发入口共用
`CloudResourceSyncService`，每次运行都会写入 `etl_job_runs`：

```text
job_name, provider, started_at, finished_at, status,
records_processed, error_message
```

手动触发同步并查看历史：

```powershell
Invoke-RestMethod `
  http://localhost:5000/api/admin/sync/azure/resources `
  -Method Post

Invoke-RestMethod `
  "http://localhost:5000/api/admin/etl-runs?jobName=azure-resource-sync&take=20"
```

成功执行记录处理数量；Azure 调用或数据库写入失败时，运行状态更新为
`Failed` 并保留错误消息，同时 API 返回失败、Worker 以非零退出码结束。

完整 Day 5 端到端验收：

```powershell
./scripts/Test-AzureResourceEtl.ps1
```

脚本使用独立的 `finops_day5` 数据库，验证 Worker、真实 Azure 手动同步、
执行历史 API 以及强制认证失败记录，最后删除测试数据库和临时日志。

## Azure Cost POC

Day 6 使用 Azure Cost Management Query API 拉取最近 7 天日成本，并按
`ServiceName` 和 `ResourceGroup` 分组后 Upsert 到 `cloud_cost_daily`。

```powershell
Invoke-RestMethod `
  "http://localhost:5000/api/admin/sync/azure/costs?days=7" `
  -Method Post
```

成本 API 需要当前身份在订阅 scope 具备 Cost Management 读取权限。账单为空、
学生订阅不支持或 API 暂时不可用时，默认生成明确标记为 `source=sample` 的
7 天样例数据，保证本地演示不会卡死。样例数据不会伪装为真实账单，可通过
`raw_json` 追溯来源。

完整 Day 6 端到端验收：

```powershell
./scripts/Test-AzureCostPoc.ps1
```

## Azure Cost ETL

Day 7 将成本 POC 正式化。成本同步既可以由管理 API 手动触发，也可以由
一次性 Worker Job 执行：

```powershell
$env:Etl__Job = "Costs"
$env:Etl__CostDays = "7"
dotnet run --project src/FinOps.Worker
```

成本查询 API：

| 地址 | 用途 |
| --- | --- |
| `GET /api/costs/daily` | 按日期和币种返回成本趋势 |
| `GET /api/costs/by-service` | 按服务返回成本与币种内占比 |
| `GET /api/costs/by-resource-group` | 按资源组返回成本与币种内占比 |

三个查询都支持 `provider`、`from`、`to` 参数；默认查询 Azure 最近 7 天。

```powershell
Invoke-RestMethod `
  "http://localhost:5000/api/costs/by-service?provider=Azure&from=2026-06-01&to=2026-06-07"
```

完整 Day 7 端到端验收：

```powershell
./scripts/Test-AzureCostEtl.ps1
```

脚本使用独立的 `finops_day7` 数据库，由 Cost Worker 写入真实 Azure 成本，
再通过管理 API 重跑验证幂等性，并交叉核对日趋势、服务和资源组 API 总额。
