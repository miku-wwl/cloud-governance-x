# 当前状态

最近审视日期：2026-06-29
状态基线：文档体系整改开始于 `main@b3efe97`，后续已合入文档重组 PR
当前执行模型：milestone + gate，Day 胶囊只保留回顾历史。

## 1. 项目位置

Cloud Governance X 已经超过最初 Day 1-7 开发基线，也已经通过 Phase 1 工程门禁。

当前位置：

| 维度 | 状态 |
| --- | --- |
| 当前 phase | Phase 2 - 身份、租户、RBAC 与审计 |
| 最新已实现 Day | Day 26 - Microsoft Entra 开发集成 |
| 下一施工单元 | Day 27 - 权限与范围 RBAC |
| 生产可用性 | 尚未生产可用 |
| 本地测试快照 | `dotnet test FinOpsPlatform.slnx --no-restore`：84 passed，1 skipped |
| 当前规划方式 | 旧 148-Day 长表已归档，当前按 milestone 和 gate 推进 |

项目不应再用旧 148-Day 长表作为主事实来源。后续只展开当前 milestone 和下一个
milestone，其他内容保持里程碑级规划。

## 2. 已完成里程碑

| 里程碑 | 范围 | 结果 | 证据 |
| --- | --- | --- | --- |
| 开发基线 | Day 1-7 Azure 数据底座 | 本地/开发基线完成 | [Day 胶囊](days/README.md)、[baseline summary](archive/phase-0/baseline-verification-summary.md) |
| Phase 0 | Day 8-11 基线审计、风险、架构和出关 | Complete | [stage-0-gate-report.md](archive/phase-0/stage-0-gate-report.md) |
| Phase 1 | Day 12-19 工程治理和 migration 分离 | Accepted | [independent-acceptance-report.md](archive/phase-1/independent-acceptance-report.md) |
| Phase 2 partial | Day 20-26 租户、可信上下文、OIDC 和 Entra 开发身份 | 已实现到 Day 26，phase 未关闭 | [Phase 2 Day 胶囊](days/README.md) |

## 3. 当前能力

仓库当前具备：

- .NET 10 API、Worker、Migrator、Application、Domain、Infrastructure 和 Tests；
- PostgreSQL 本地开发环境和 health check；
- Azure Terraform 开发生命周期；
- Azure subscription、resource inventory 和 cost 数据采集；
- ETL run history 和独立数据库 migration host；
- 静态验证和数据库 migration CI gate；
- 业务 Tenant 模型、可信 TenantContext 和租户感知 repository；
- 受控 legacy tenant backfill 工具；
- OIDC JWT Bearer 验证；
- 可重复的 Microsoft Entra 开发 App Registration 和真实 token E2E 验证。

这些能力仍是开发和治理基础，不授权公开或生产部署。

## 4. 仍然生效的生产禁止项

以下内容在后续 gate 关闭前仍禁止用于生产：

- 暴露没有授权策略的业务端点；
- 把 Day 26 委托 scope 当作已经执行的授权条件；
- 使用本地 Azure CLI identity 作为 Azure Provider runtime identity；
- 在生产环境启用 cost sample fallback；
- 使用本地 Terraform state 管理团队或生产基础设施；
- 宣称已经完成多云、React 前端、审计、RLS、生产 ETL 调度、备份、SLO 或灾难恢复能力。

## 5. 立即下一步

Day 27 应实现权限与范围 RBAC。

当前工程规划和施工手册：

- [construction/engineering-plan.md](../construction/engineering-plan.md)
- [construction/current-playbook.md](../construction/current-playbook.md)

Day 27 不能悄悄吞并 Day 28 端点保护或 Day 29 审计存储。除非 Owner
明确改变里程碑范围，否则这些仍是独立 gate。

## 6. 文档权威顺序

文档冲突时按以下顺序处理：

1. 生产安全与数据正确性；
2. [outline.md](../outline.md)；
3. 本 current-state 文件；
4. [docs/archive/adr](archive/adr/) 中已接受的 ADR；
5. [docs/days](days/) 中的 Day 胶囊；
6. 阶段报告、风险登记和生产差距登记；
7. 归档施工计划和本地 review transcript。

`review.txt`、`website-reivew.md` 等本地 ignored 文件是外部 review transcript，可以辅助分析，
但其中结论只有复制到 tracked docs 后才成为项目事实。
