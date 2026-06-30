# 里程碑路线图

本文是当前规划入口。详细 Day 施工表见
[construction/engineering-plan.md](../construction/engineering-plan.md)。

## 1. 当前规划口径

当前权威规划是：

- **M0-M10 共 11 个里程碑**；
- **Day 1-148 共 148 个施工单元**；
- 当前处于 **Phase 2 / M4**；
- 最新已实现 **Day 29**，最新已接受 **Day 29**；
- 当前施工单元是 **Day 30**；
- Phase 2 要到 **Day 30** 才判断是否出关；
- 下一里程碑 **M5** 从 **Day 31** 开始。

Day 编号用于施工和回顾，不等于自然日，也不代表生产成熟度百分比。

## 2. 规划规则

每个 Day 必须有明确目标、范围、设计决策、验证证据、review 结论和遗留风险。

每个里程碑必须用以下证据关闭：

- 已接受的设计或 ADR；
- 必要的实现和 migration；
- 自动化测试和负向测试；
- 范围要求真实集成时，必须有真实环境或 staging 证据；
- 更新后的风险、生产差距和运行文档；
- 明确的 gate decision。

如果某个 Day 未通过 review，不新建 Day 掩盖问题；当前 Day 保持 `Validation` 或
`Blocked`，修复后重新验收。

## 3. 11 个里程碑

| 里程碑 | Day 范围 | 目标 | 状态 |
| --- | --- | --- | --- |
| M0 开发基线 | Day 1-7 | 本地 Azure/PostgreSQL/API/Worker 验证链路 | Complete |
| M1 基线治理 | Day 8-11 | 当前事实、架构、风险和出关 | Complete |
| M2 工程基础 | Day 12-19 | 静态门禁、架构测试、宿主模块化和 migration 分离 | Accepted |
| M3 身份与租户基础 | Day 20-26 | 租户模型、可信上下文、tenant-aware data、OIDC 和 Entra 开发身份 | Accepted，Phase 2 未关闭 |
| M4 RBAC、端点保护与审计 | Day 27-30 | 权限/范围 RBAC、端点策略、稳定 401/403 和追加式审计 | Day 29 Accepted，Day 30 当前工作 |
| M5 生产数据模型 | Day 31-40 | lineage、资源生命周期、成本语义、数据质量和 migration 演练 | 下一里程碑 |
| M6 可靠 ETL 平台 | Day 41-50 | scheduler、lease、retry、checkpoint、backfill 和 operator control | 未开始 |
| M7 Release A 平台基础 | Day 51-59 | observability、容器、环境、CI/CD、备份和恢复基础 | 未开始 |
| M8 Azure 生产能力 | Day 60-127 | 生产 Azure Provider、FinOps、治理 workflow、API、frontend、事件、SLO 和平台发布 | 未开始 |
| M9 多云能力 | Day 128-136 | AWS Provider 与 Azure/AWS 统一契约 | 未开始 |
| M10 系统加固与上线 | Day 137-148 | 安全、供应链、性能、韧性、DR、Go/No-Go、canary 和运营接管 | 未开始 |

## 4. 当前里程碑 M4

| Day | 目标 |
| --- | --- |
| Day 27 | 定义并执行权限与范围 RBAC：Accepted |
| Day 28 | 保护现有业务端点，并稳定授权错误契约：Accepted |
| Day 29 | 建立追加式审计模型和高权限 action record：Accepted |
| Day 30 | 执行 tenant escape、IDOR、RBAC 和审计 gate，判断 Phase 2 是否出关 |

工程规划和当前施工手册：

- [construction/engineering-plan.md](../construction/engineering-plan.md)
- [construction/current-playbook.md](../construction/current-playbook.md)

## 5. 旧路线状态

旧 Day 8-148 路线保留为历史上下文：

- [construction/archive/02-★★★-day8-production-roadmap.md](../construction/archive/02-★★★-day8-production-roadmap.md)

如果旧文档与本文、current-state 或 engineering-plan 冲突，以当前文档为准。
