# 当前状态

最近审视日期：2026-06-30
状态基线：文档体系整改开始于 `main@b3efe97`，后续已合入文档重组 PR
当前执行模型：11 个 milestone + gate，Day 胶囊记录施工历史和回顾。

## 1. 项目位置

Cloud Governance X 已经超过最初 Day 1-7 开发基线，也已经通过 Phase 1 工程门禁。

当前位置：

| 维度 | 状态 |
| --- | --- |
| 当前 phase | Phase 2 - 身份、租户、RBAC 与审计 |
| 最新已实现 Day | Day 29 - 追加式审计 |
| 最新已接受 Day | Day 29 - 追加式审计 |
| 下一施工单元 | Day 30 - Phase 2 安全门禁 |
| 生产可用性 | 尚未生产可用 |
| 本地测试快照 | `dotnet test FinOpsPlatform.slnx --no-restore`：111 passed，1 skipped |
| 当前规划方式 | M0-M10 共 11 个里程碑，Day 1-148 共 148 个施工单元 |

项目不应再用旧 148-Day 长表作为主事实来源。新的 Day 1-148 施工总表已经迁入
[construction/engineering-plan.md](../construction/engineering-plan.md)，并按 M0-M10
作为当前权威规划维护。

## 2. 已完成里程碑

| 里程碑 | 范围 | 结果 | 证据 |
| --- | --- | --- | --- |
| 开发基线 | Day 1-7 Azure 数据底座 | 本地/开发基线完成 | [Day 胶囊](days/README.md)、[baseline summary](archive/phase-0/baseline-verification-summary.md) |
| M1 / 历史 Phase 0 | Day 8-11 基线审计、风险、架构和出关 | Complete | [milestone-1.md](milestones/milestone-1.md) |
| M2 / 历史 Phase 1 | Day 12-19 工程治理和 migration 分离 | Accepted | [milestone-2.md](milestones/milestone-2.md) |
| Phase 2 partial / M3-M4 | Day 20-29 租户、可信上下文、OIDC、Entra 开发身份、RBAC、端点保护和授权审计 | Day 29 Accepted，M4 / 历史 Phase 2 未关闭 | [Day 胶囊](days/README.md) |

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
- 可重复的 Microsoft Entra 开发 App Registration 和真实 token E2E 验证；
- Membership role、tenant/CloudAccount/platform scope 和 RBAC 授权评估服务；
- 现有业务端点的 RBAC permission filter 和基础 401/403 行为；
- 授权成功/失败的追加式审计事件、migration 和高权限 action 标记。

这些能力仍是开发和治理基础，不授权公开或生产部署。

## 4. 仍然生效的生产禁止项

以下内容在后续 gate 关闭前仍禁止用于生产：

- 把 Day 26 委托 scope 当作已经执行的授权条件；
- 把 Day 27 RBAC 模型当作现有端点已经全面受保护；
- 把 Day 29 追加式授权审计当作完整审计产品、RLS、rate limit 或生产安全门禁已完成；
- 使用本地 Azure CLI identity 作为 Azure Provider runtime identity；
- 在生产环境启用 cost sample fallback；
- 使用本地 Terraform state 管理团队或生产基础设施；
- 宣称已经完成多云、React 前端、审计、RLS、生产 ETL 调度、备份、SLO 或灾难恢复能力。

## 5. 立即下一步

Day 30 应执行 tenant escape、IDOR、RBAC、端点保护和审计 gate，判断 Phase 2 是否出关。

当前工程规划和施工手册：

- [construction/engineering-plan.md](../construction/engineering-plan.md)
- [construction/current-playbook.md](../construction/current-playbook.md)

Day 30 不能悄悄吞并 M5 生产数据模型。除非 Owner 明确改变
里程碑范围，否则这些仍是独立 gate。

## 6. 文档权威顺序

文档冲突时按以下顺序处理：

1. 生产安全与数据正确性；
2. [outline.md](../outline.md)；
3. 本 current-state 文件；
4. [docs/roadmap.md](roadmap.md)；
5. [construction/engineering-plan.md](../construction/engineering-plan.md)；
6. [docs/archive/adr](archive/adr/) 中已接受的 ADR；
7. [docs/days](days/) 中的 Day 胶囊；
8. Milestone 报告、风险登记和生产差距登记；
9. 归档施工计划和本地 review transcript。

`review.txt`、`website-reivew.md` 等本地 ignored 文件是外部 review transcript，可以辅助分析，
但其中结论只有复制到 tracked docs 后才成为项目事实。
