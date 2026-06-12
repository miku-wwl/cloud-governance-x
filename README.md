# Cloud Governance X

基于 .NET 10、Terraform、PostgreSQL 和 React 构建的多云 FinOps 与资源治理平台。

当前版本已完成 Day 1 工程骨架、Day 2 Azure Terraform 基础设施和 Day 3
Azure SDK 认证，包含
Web API、后台 Worker、Clean Architecture 基础分层、PostgreSQL 本地环境、
健康检查、可重复验证的 Azure 资源生命周期，以及通过
`DefaultAzureCredential` 读取 Azure 订阅的能力。

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
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

启动 API：

```powershell
dotnet run --project src/FinOps.Api --urls http://localhost:5000
```

在另一个终端启动 Worker：

```powershell
dotnet run --project src/FinOps.Worker
```

## 健康检查

| 地址 | 用途 |
| --- | --- |
| `GET /health/live` | 检查 API 进程是否存活 |
| `GET /health` | 检查 API 是否就绪，包括 PostgreSQL 端口可达性 |

示例：

```powershell
Invoke-WebRequest http://localhost:5000/health/live
Invoke-WebRequest http://localhost:5000/health
```

PostgreSQL readiness 会使用 Npgsql 建立真实数据库连接并执行 `SELECT 1`。
EF Core `DbContext` 和迁移将在后续数据模型阶段加入。

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
