# 里程碑路线图

本文替代旧的长期 Day 表，作为当前规划入口。Day 编号仍可用于回顾，但不能作为成熟度或生产可用性的代理指标。

## 1. 规划规则

只展开当前里程碑和下一个里程碑。更远的工作保持里程碑级描述，等前置 gate 产生足够证据后再展开。

每个里程碑必须用以下证据关闭：

- 已接受的设计或 ADR；
- 必要的实现和 migration；
- 自动化测试和负向测试；
- 范围要求真实集成时，必须有真实环境或 staging 证据；
- 更新后的风险、生产差距和运行文档；
- 明确的 gate decision。

## 2. 里程碑

| 里程碑 | 原 Day 范围 | 目标 | 状态 |
| --- | --- | --- | --- |
| M0 开发基线 | Day 1-7 | 本地 Azure/PostgreSQL/API/Worker 验证链路 | Complete |
| M1 基线治理 | Day 8-11 | 当前事实、架构、风险和出关 | Complete |
| M2 工程基础 | Day 12-19 | 静态门禁、架构测试、宿主模块化和 migration 分离 | Accepted |
| M3 身份与租户基础 | Day 20-26 | 租户模型、可信上下文、tenant-aware data、OIDC 和 Entra 开发身份 | 已实现到 Day 26，phase 未关闭 |
| M4 RBAC、端点保护与审计 | Day 27-30 | 权限/范围 RBAC、端点策略、稳定 401/403 和追加式审计 | 当前工作 |
| M5 生产数据模型 | 原 Day 31-40 | lineage、资源生命周期、成本语义、数据质量和 migration 演练 | 未开始 |
| M6 可靠 ETL 平台 | 原 Day 41-50 | scheduler、lease、retry、checkpoint、backfill 和 operator control | 未开始 |
| M7 Release A 平台基础 | 原 Day 51-59 及 Phase 11/12 基础 | observability、容器、环境、CI/CD、备份和恢复基础 | 未开始 |
| M8 Azure 生产能力 | 原 Day 60-127 中选定 gate | 生产 Azure Provider、FinOps 语义、治理 workflow、API/frontend 和 release gate | 未开始 |
| M9 多云能力 | 原 Day 128-136 | AWS Provider 与 Azure/AWS 统一契约 | 未开始 |
| M10 系统加固与上线 | 原 Day 137-148 | 安全、供应链、性能、韧性、DR、Go/No-Go 和 canary | 未开始 |

## 3. 当前里程碑边界

M4 是当前规划单元。

预期顺序：

| 单元 | 目标 |
| --- | --- |
| Day 27 | 定义并执行权限与范围 RBAC contract |
| Day 28 | 保护现有业务端点，并稳定 auth error |
| Day 29 | 建立追加式审计模型和高权限 action record |
| Day 30 | 执行 tenant escape、IDOR、RBAC 和审计 gate |

工程规划和当前施工手册：

- [construction/engineering-plan.md](../construction/engineering-plan.md)
- [construction/current-playbook.md](../construction/current-playbook.md)

## 4. 退役的规划形态

旧 Day 8-148 路线只作为历史上下文保留：

- [construction/archive/02-★★★-day8-production-roadmap.md](../construction/archive/02-★★★-day8-production-roadmap.md)

不要把旧文档作为当前规划主来源。如果旧文档与本文或 current-state 证据冲突，以当前路线和当前事实为准。
