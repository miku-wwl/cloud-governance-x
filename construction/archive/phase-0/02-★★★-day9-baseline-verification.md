# 02 ★★★ Day 9：Day 1～7 基线重新验收

> 本日目标：在同一 Commit 上重新执行本地、PostgreSQL、Terraform 和真实 Azure
> 验证，证明 Day 1～7 行为可重复，并证明测试资源被清理。
>
> 本日风险：会调用真实 Azure API，并短暂创建可能产生费用的 Azure 资源。

## 1. 完成定义

Day 9 不是“把六个脚本运行一遍”。完整闭环必须证明：

- 工具链和本地工程可从当前状态 restore、build、test；
- PostgreSQL readiness 是真实连接；
- API 和 Worker 能按当前配置启动；
- Terraform 能创建、核验并销毁资源；
- Azure subscription、Resource Graph 和 Cost Management 链路有明确结果；
- 资源和成本重跑具备当前承诺的幂等性；
- ETL 成功和失败状态能写入 PostgreSQL；
- sample 路径与真实 Azure 成本路径被分开记录；
- 测试数据库、端口、进程、Terraform 产物和 Azure 资源没有遗留。

## 2. 前置条件与风险确认

### 2.1 必要工具

```powershell
dotnet --info
docker version
docker compose version
az version
terraform version
git --version
```

最低要求以仓库文件为准：

- .NET SDK：`global.json`；
- Terraform：`terraform/azure/providers.tf`；
- PostgreSQL：`compose.yaml`；
- EF Tool：`dotnet-tools.json`。

### 2.2 Azure 身份和 scope

```powershell
az account show `
  --query "{subscriptionId:id,name:name,tenantId:tenantId,state:state}" `
  --output table
```

人工确认：

- 当前 subscription 是专门用于开发或受控测试的订阅；
- 有权限创建 Resource Group、Storage Account 和 Service Bus；
- 有 Resource Graph 读取权限；
- 有 Cost Management 读取权限，或明确记录外部权限限制；
- 没有把生产 subscription 当作默认测试环境；
- 已知本轮测试可能产生的短时费用。

禁止把 access token、client secret 或完整 credential 输出写入报告。

### 2.3 端口和数据库冲突

默认脚本使用：

| 用途 | 默认值 |
| --- | --- |
| Day 3 API | `5103` |
| Day 5 成功 API | `5105` |
| Day 5 失败 API | `5106` |
| Day 6 API | `5107` |
| Day 7 API | `5108` |
| Day 4 数据库 | `finops_day4` |
| Day 5 数据库 | `finops_day5` |
| Day 6 数据库 | `finops_day6` |
| Day 7 数据库 | `finops_day7` |

检查端口：

```powershell
5103,5105,5106,5107,5108 | ForEach-Object {
    Get-NetTCPConnection -LocalPort $_ -State Listen -ErrorAction SilentlyContinue
}
```

如果被其他程序占用，使用脚本参数换端口；不要停止不属于本项目的进程。

## 3. 证据目录

本日施工按“静态与本地工程 → PostgreSQL 与 Day 1 → 六个真实 E2E → 严格成本
复验 → 清理审计”的顺序执行。任何一轮失败，先记录、诊断和修复当前轮，不跳到
后面用更多成功输出来掩盖失败。

执行前建立本地目录：

```powershell
New-Item -ItemType Directory -Force `
  -Path "tmp/phase-0-evidence/day09" | Out-Null
```

推荐记录：

```text
tmp/phase-0-evidence/day09/
├── tool-versions.txt
├── dotnet-restore.txt
├── dotnet-build.txt
├── dotnet-test.txt
├── compose-status.txt
├── day2-terraform.txt
├── day3-sdk.txt
├── day4-resource-inventory.txt
├── day5-resource-etl.txt
├── day6-cost-poc.txt
├── day7-cost-etl.txt
├── day7-cost-strict.txt
└── cleanup-audit.txt
```

可以使用 `Tee-Object` 保存输出，但报告中只能记录必要摘要。

## 4. 第一轮：静态与本地工程验收

### 4.1 Git 起点

```powershell
git status --short --branch
git log -1 --oneline --decorate
git diff --check
```

若存在用户修改，记录但不回滚。若修改影响测试结论，必须在报告中说明。

### 4.2 配置解析

JSON：

```powershell
Get-ChildItem -Recurse -File -Filter *.json |
  Where-Object { $_.FullName -notmatch '\\(bin|obj|tmp)\\' } |
  ForEach-Object {
      Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json | Out-Null
  }
```

Compose：

```powershell
docker compose config --quiet
```

PowerShell：

```powershell
Get-ChildItem scripts -File -Filter *.ps1 | ForEach-Object {
    [void][scriptblock]::Create(
        (Get-Content -Raw -LiteralPath $_.FullName)
    )
}
```

Terraform：

```powershell
terraform -chdir=terraform/azure fmt -check
terraform -chdir=terraform/azure init -backend=false -input=false
terraform -chdir=terraform/azure validate
```

`terraform init` 会生成被忽略的 `.terraform/`，Day 9 结束前必须清理。

### 4.3 .NET

```powershell
dotnet tool restore
dotnet restore FinOpsPlatform.slnx
dotnet build FinOpsPlatform.slnx --no-restore
dotnet test FinOpsPlatform.slnx --no-build
```

记录：

- 实际测试总数；
- Passed / Failed / Skipped；
- warning 数量；
- 执行时长；
- 使用的 SDK 版本。

不要继续沿用历史“19 个测试”数字而不读取本轮实际输出。

## 5. 第二轮：PostgreSQL 和 Day 1 手工验收

### 5.1 启动数据库

```powershell
docker compose up -d
docker compose ps
```

等待 `postgres` 为 healthy。

### 5.2 人工验证数据库

```powershell
docker compose exec -T postgres `
  psql -U finops -d finops -c "SELECT 1;"
```

### 5.3 启动 API

在独立终端执行：

```powershell
dotnet run --no-build `
  --project src/FinOps.Api `
  --urls http://localhost:5000
```

验证：

```powershell
Invoke-WebRequest http://localhost:5000/health/live
Invoke-WebRequest http://localhost:5000/health
Invoke-RestMethod http://localhost:5000/
```

人工观察：

- `/health/live` 证明进程存活；
- `/health` 证明 PostgreSQL 可连接和查询；
- 根端点返回 `FinOps.Api`；
- 停止 PostgreSQL 后 `/health` 应失败；
- 恢复 PostgreSQL 后 readiness 应恢复。

完成后用启动 API 的同一终端按 `Ctrl+C` 正常停止。不要使用模糊的全局
`Stop-Process dotnet`。

## 6. 第三轮：六个真实 E2E

必须串行执行，避免 Terraform state、默认命名和端口互相影响。

### 6.1 Day 2 Terraform 生命周期

```powershell
./scripts/Test-AzureTerraformLifecycle.ps1
```

脚本应证明：

- Terraform fmt/validate/plan/apply；
- Storage Account、Service Bus Namespace 和 Queue 存在；
- tags 存在；
- destroy 成功；
- Terraform state 为空；
- Resource Group 已删除；
- 本地产物被清理。

不要使用 `-KeepResources`。`-KeepEvidence` 只在明确需要诊断时使用，完成后仍要
人工清理。

### 6.2 Day 3 Azure SDK

```powershell
./scripts/Test-AzureSdkIntegration.ps1
```

脚本应证明：

- API 可以启动；
- `DefaultAzureCredential` 能使用当前 Azure CLI 身份；
- API 返回 active subscription；
- subscription ID、name、tenant ID 和 state 与 `az account show` 一致；
- API 进程和临时日志被删除。

### 6.3 Day 4 资源清单

```powershell
./scripts/Test-AzureResourceInventory.ps1
```

脚本应证明：

- 独立测试数据库被创建；
- Azure 临时资源被创建；
- Resource Graph 最终发现预期资源；
- 两次 Worker 同步行数稳定；
- 没有 `(provider, resource_id_normalized)` 重复；
- `FirstSeenAt` 不被重跑改变；
- Azure Resource Group、测试数据库和 Terraform 产物被清理。

### 6.4 Day 5 正式资源 ETL

```powershell
./scripts/Test-AzureResourceEtl.ps1
```

脚本应证明：

- Worker 成功同步；
- 管理 API 成功同步；
- 两次成功结果进入 `etl_job_runs`；
- 强制 Azure 身份失败返回 HTTP 500；
- 失败运行有 `FinishedAt` 和错误信息；
- API 进程、日志和测试数据库被清理。

注意：HTTP 500 是当前基线行为，不代表生产 API 错误设计已经完成。

### 6.5 Day 6 成本 POC 与 sample

```powershell
./scripts/Test-AzureCostPoc.ps1
```

脚本会验证：

- 第一次使用真实 Cost Management 或 fallback；
- 强制 sample 路径明确返回 `usedSampleData=true`；
- sample 重跑幂等；
- sample 数据有 `raw_json.source=sample`；
- 测试数据库和日志被清理。

必须从输出记录第一次来源：

| 输出 | 可得结论 |
| --- | --- |
| `Azure Cost Management` | 本轮取得真实成本证据 |
| `sample fallback` | 只证明 fallback 行为，不能证明真实成本 API 成功 |

### 6.6 Day 7 成本 ETL 与查询

```powershell
./scripts/Test-AzureCostEtl.ps1
```

脚本应证明：

- Cost Worker 执行成功；
- API 重跑不插入重复行；
- daily、service、resource-group 三种查询总额按币种一致；
- service 百分比按币种约等于 100%；
- ETL 历史与数据库行数一致；
- API、日志和测试数据库被清理。

### 6.7 严格真实成本复验

标准 Day 7 脚本允许配置中的 fallback。若基线要继续声明“真实 Azure 成本已
验证”，必须临时关闭 fallback 再执行：

```powershell
$previousFallback = $env:AzureCost__UseSampleDataWhenUnavailable

try {
    $env:AzureCost__UseSampleDataWhenUnavailable = "false"
    ./scripts/Test-AzureCostEtl.ps1
}
finally {
    $env:AzureCost__UseSampleDataWhenUnavailable = $previousFallback
}
```

结果处理：

- 通过：可以记录本轮真实 Cost Management 闭环；
- 因权限、订阅类型或账单为空失败：记录外部限制，不得改成 sample 后声称通过；
- 程序解析或数据错误：Day 9 失败，修复后重跑；
- 清理失败：即使业务断言通过，Day 9 仍失败。

## 7. 第四轮：清理审计

### 7.1 测试数据库

```powershell
docker compose exec -T postgres `
  psql -U finops -d postgres `
  -t -A `
  -c "SELECT datname FROM pg_database WHERE datname LIKE 'finops_day%';"
```

预期：无输出。

### 7.2 测试端口

```powershell
5103,5105,5106,5107,5108 | ForEach-Object {
    Get-NetTCPConnection -LocalPort $_ -State Listen -ErrorAction SilentlyContinue
}
```

预期：无输出。

### 7.3 Terraform 产物

```powershell
Get-ChildItem terraform/azure -Force
Get-ChildItem -Recurse -Force `
  -Include *.tfstate,*.tfstate.*,*.tfplan |
  Where-Object { $_.FullName -notmatch '\\.git\\' }
```

预期：

- 无 `.terraform/`；
- 无 `day2.tfplan`、`day4.tfplan`；
- 无 state 和 backup；
- `.terraform.lock.hcl` 保留，它是应提交的 Provider 锁文件。

### 7.4 Azure 临时资源

先只查询，不自动删除：

```powershell
az group list `
  --query "[?tags.owner=='cloud-governance-x'].{name:name,location:location,tags:tags}" `
  --output table
```

人工与测试前清单对比。发现未知资源时：

1. 记录名称、tags、创建时间和可能来源；
2. 确认是否属于本轮脚本；
3. 只有确认属于测试且无保留价值时才按原创建方式清理；
4. 不对无法确认所有权的资源执行删除。

### 7.5 Git 与本地产物

```powershell
git status --short --ignored
git diff --check
```

预期：

- 只有预期文档或修复变更；
- `tmp/` 显示为 ignored；
- 无日志、state、plan、测试结果或 secret 待提交。

## 8. 结果分类

每条验收使用：

| 结果 | 含义 |
| --- | --- |
| `Passed` | 本轮在当前 Commit 真实通过 |
| `FailedProduct` | 产品代码或脚本缺陷 |
| `BlockedExternal` | 权限、订阅、Azure 服务或网络阻断 |
| `PassedWithSample` | 只证明 sample/fallback |
| `NotRun` | 未执行，必须说明原因 |
| `CleanupFailed` | 主流程可能通过，但清理未完成 |

`BlockedExternal`、`PassedWithSample` 和 `NotRun` 不能被汇总成 `Passed`。

## 9. 人工 Review 清单

- [ ] 工具版本与仓库约束一致；
- [ ] 实际测试数量从本轮输出读取；
- [ ] Day 1 readiness 真实依赖 PostgreSQL；
- [ ] 六个 E2E 串行执行；
- [ ] Day 5 失败状态确实持久化；
- [ ] Day 6 sample 有明确 provenance；
- [ ] Day 7 三类查询按币种一致；
- [ ] 严格真实成本复验有单独结论；
- [ ] 没有把 fallback 成功写成 Azure Cost API 成功；
- [ ] 所有测试数据库已删除；
- [ ] 所有测试端口已释放；
- [ ] Terraform state 和 plan 已删除；
- [ ] Azure 临时资源已核对；
- [ ] 未删除无法确认所有权的云资源；
- [ ] 失败和限制已进入 Day 11 风险输入。

## 10. 人工学习

### 10.1 测试层次

| 层次 | 当前例子 | 能证明什么 | 不能证明什么 |
| --- | --- | --- | --- |
| Domain 单元测试 | ID、币种、状态机 | 纯业务不变量 | 数据库和 Azure |
| Application 测试 | SyncService、QueryService | 用例编排和失败记录 | 真实 SDK/SQL |
| Parser 测试 | Resource Graph、Cost JSON | 外部响应映射 | 云权限和网络 |
| PostgreSQL E2E | Day 4～7 | migration、Upsert、查询 | 生产规模和 HA |
| Azure E2E | 六个脚本 | 真实身份、API、资源生命周期 | production identity 和多租户 |
| 手工验证 | health、清理审计 | 用户可观察行为和环境状态 | 自动回归保障 |

### 10.2 必须回答

1. 为什么 E2E 通过后仍需要检查清理？
2. 为什么 Day 6 成功不一定代表真实 Azure Cost 成功？
3. 为什么测试数据库必须独立，而不能使用默认 `finops`？
4. 为什么重复同步的“行数不变”只是幂等性证据之一？
5. Day 5 强制失败证明了什么？还缺什么生产错误模型？
6. Terraform state 为空与 Resource Group 不存在为什么都要检查？
7. 为什么不能看到一个 `dotnet` 进程就直接结束它？

## 11. 永久总结

将经过整理的结论写入：

```text
docs/phase-0/baseline-verification-summary.md
```

建议结构：

- Commit、日期、环境；
- 本地工程结果；
- 自动测试结果；
- 六个 E2E 结果；
- 严格真实成本结果；
- 清理结果；
- 外部阻断；
- 已知限制；
- 证据索引；
- 是否允许进入 Day 10。

原始输出保留在 `tmp/`，不得把 token、完整资源 payload 或敏感成本明细提交 Git。
