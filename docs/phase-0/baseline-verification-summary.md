# Day 1～7 基线复验总结

## 1. 基本信息

- 执行日期：2026 年 6 月 14 日
- 分支：`main`
- 复验 Commit：`6ce8e25`
- 环境：Windows、本地 Docker Desktop、Azure CLI、Azure for Students
- Day 9 状态：`ReadyForReview`

本文是 Day 9 的永久结论。原始命令输出位于
`tmp/phase-0-evidence/day09/`，该目录被 Git 忽略，不包含 access token、
client secret 或完整连接字符串。

## 2. 总体结论

Day 1～7 基线在同一 Commit 上重新验收通过：

- 工具、配置、Terraform 和 PowerShell 静态检查通过；
- .NET restore、build 和 test 通过；
- PostgreSQL、API、Worker 和 health 行为通过；
- Day 2～7 六个真实 E2E 串行通过；
- 严格关闭 sample fallback 后，真实 Azure Cost Management 链路通过；
- 测试数据库、端口、Azure 临时资源和 Terraform 产物清理通过。

本结论证明当前开发基线可重复，不证明 production identity、多租户、staging、
高可用、安全、性能、备份恢复或 SLO 已经完成。

## 3. 工具与本地工程

| 检查 | 结果 | 本轮事实 |
| --- | --- | --- |
| .NET SDK | `Passed` | 10.0.300，符合 `global.json` |
| Docker | `Passed` | Docker Engine 29.4.3，Compose 5.1.4 |
| Azure CLI | `Passed` | 2.86.0，当前订阅启用 |
| Terraform | `Passed` | 1.14.0，满足 `>= 1.9.0` |
| JSON | `Passed` | 排除 bin/obj/tmp 后全部可解析 |
| Compose | `Passed` | `docker compose config --quiet` |
| PowerShell | `Passed` | 6 个脚本均可解析 |
| Terraform 静态检查 | `Passed` | fmt、init、validate 全部通过 |
| restore | `Passed` | 工具与解决方案依赖还原成功 |
| build | `Passed` | 6 个项目，0 warning，0 error，13.07 秒 |
| test | `Passed` | 19 total，19 passed，0 failed，0 skipped，1.54 秒 |

## 4. PostgreSQL 与 API 手工验证

| 检查 | 结果 | 观察 |
| --- | --- | --- |
| PostgreSQL health | `Passed` | Compose 容器为 healthy |
| 数据库查询 | `Passed` | `SELECT 1` 返回 1 |
| `/health/live` | `Passed` | HTTP 200 |
| `/health` 正常 | `Passed` | PostgreSQL 可用时 HTTP 200 |
| 根端点 | `Passed` | 返回 `FinOps.Api/running` |
| readiness 失败 | `Passed` | 停止 PostgreSQL 后 HTTP 503 |
| readiness 恢复 | `Passed` | PostgreSQL 恢复后 HTTP 200 |
| API 清理 | `Passed` | 只停止本轮启动的 API PID |

这证明 `/health` 是真实数据库 readiness，不是固定返回成功。

## 5. 六个 E2E 结果

| Day | 脚本 | 结果 | 关键证据 |
| --- | --- | --- | --- |
| 2 | `Test-AzureTerraformLifecycle.ps1` | `Passed` | 5 个资源创建并核验，5 个销毁；state 空，Resource Group 不存在 |
| 3 | `Test-AzureSdkIntegration.ps1` | `Passed` | API 订阅读取与 Azure CLI 的启用订阅一致 |
| 4 | `Test-AzureResourceInventory.ps1` | `Passed` | 4 条资源入库，测试组 2 条；重跑 0 插入/4 更新，无重复 |
| 5 | `Test-AzureResourceEtl.ps1` | `Passed` | Worker/API 成功历史和强制身份失败历史均持久化 |
| 6 | `Test-AzureCostPoc.ps1` | `Passed` | 真实成本返回 28 行；强制 sample 覆盖 7 天并保持幂等 |
| 7 | `Test-AzureCostEtl.ps1` | `Passed` | 28 行真实成本，重跑无重复，三类查询按币种总额一致 |

Day 5 的强制失败目前表现为 HTTP 500，这是基线验证通过，不代表生产错误契约
已经完成。该差距继续由 `GAP-016` 跟踪。

## 6. 严格真实成本结论

标准 Day 7 脚本本身允许 fallback，因此本日额外设置：

```text
AzureCost__UseSampleDataWhenUnavailable=false
```

严格复验结果为 `Passed`：

- Azure Cost Management 返回 HTTP 200；
- Worker 取得 28 行；
- 日志明确记录 `sample data: False`；
- API 幂等、三类查询总额和 ETL 历史再次通过；
- `finops_day7` 测试数据库被删除。

因此当前 Commit 可以声明“真实 Azure Cost Management 开发链路已验证”，但
不能扩大为生产身份、所有订阅类型、账单完整性或生产成本语义已验证。

## 7. 清理审计

| 对象 | 结果 | 实际状态 |
| --- | --- | --- |
| `finops_day*` 数据库 | `Passed` | 无遗留 |
| 5000/5103/5105/5106/5107/5108 | `Passed` | 无监听 |
| Terraform `.terraform/` | `Passed` | 不存在 |
| Terraform state/plan | `Passed` | 无遗留 |
| `.terraform.lock.hcl` | `Passed` | 正确保留 |
| Azure 测试 Resource Group | `Passed` | owner 标签查询无结果 |
| API/Worker 进程 | `Passed` | 无本轮宿主遗留 |
| build server | `Passed` | 使用 `dotnet build-server shutdown` 关闭 |
| VS Code BuildHost | `Preserved` | 早于本轮且属于 IDE，不擅自结束 |
| Git | `Passed` | 运行产物均被忽略，未进入跟踪状态 |

本地 PostgreSQL Compose 服务保留为正常开发依赖，状态为 healthy；测试数据库
已经全部删除。

## 8. 已知限制

1. Azure E2E 使用本地 Azure CLI 用户身份，不是生产 workload identity。
2. Azure for Students 是本次开发订阅，不代表企业订阅和多账号行为。
3. 成本数据仍是日、服务、资源组和币种粒度，不是资源级精确归因。
4. sample fallback 默认仍开启，只允许本地演示，生产继续禁止。
5. 管理 API 仍匿名，API/Worker 仍自动 migration。
6. 当前 E2E 不覆盖生产负载、HA、tenant 隔离、备份恢复和安全测试。

## 9. Day 10 准入结论

Day 9 没有 `FailedProduct`、`BlockedExternal`、`CleanupFailed` 或 `NotRun`。
自动验收和清理均已完成，可以提交人工 review。人工确认后允许进入 Day 10，
以本次已验证行为绘制当前架构和数据流。
