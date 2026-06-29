# Day 5 - 资源 ETL

## 1. 目标
把资源同步升级为有运行记录、可审计的 ETL 操作，解决同步成功失败不可追踪、API 和 Worker 执行路径分裂的工程风险。

## 2. 前置条件
依赖 Day 4 资源清单、PostgreSQL 持久化和 Worker 执行入口。

## 3. 施工范围
允许创建 `etl_job_runs`、资源同步服务编排、API 手动触发入口、Worker 资源同步路径和失败记录。不允许把匿名或未授权管理 API 视为生产可用。

## 4. 设计决策
API 和 Worker 共享 `CloudResourceSyncService`，每次运行记录 Running/Succeeded/Failed，失败先持久化再抛出，保证操作结果可追溯。

## 5. 实现摘要
新增 ETL run 模型、资源同步 orchestration、管理 API 触发入口、Worker 触发入口和失败状态记录。

## 6. 验证证据
Day 9 重新运行 Day 5 E2E，验证 Worker 成功历史、API 触发同步和强制认证失败历史。证据包括 [05-★★★-data-model.md](../archive/reference/05-★★★-data-model.md)、[baseline-verification-summary.md](../archive/phase-0/baseline-verification-summary.md) 和 [Test-AzureResourceEtl.ps1](../../scripts/Test-AzureResourceEtl.ps1)。

## 7. Review 结论
Accepted。作为开发基线完成。

## 8. 遗留风险
管理 API 仍需身份、RBAC、端点授权和审计后才可进入生产；调度、lease、retry 和 checkpoint 未完成。

## 9. 相关链接
- Commit: `ae3bcb2` - `feat: complete day 5 resource ETL`
- [scripts/Test-AzureResourceEtl.ps1](../../scripts/Test-AzureResourceEtl.ps1)