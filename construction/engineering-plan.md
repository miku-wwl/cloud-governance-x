# Cloud Governance X 工程规划总纲

本文是 `construction/archive/02-★★★-day8-production-roadmap.md` 的新版对应文档。
旧文档保留为历史材料，不再作为当前施工依据。

本文负责回答：

- 项目现在处于哪个工程阶段；
- 当前阶段和下一阶段应该按什么顺序推进；
- 每个施工单元如何开工、验收和出关；
- 旧 100+ Day 长表中哪些规划原则仍然有效，哪些已经退役。

## 1. 当前判断

截至 2026-06-29，项目已经完成：

- M0：Day 1-7 Azure/PostgreSQL/API/Worker 开发基线；
- M1：Day 8-11 Phase 0 基线、风险、架构和出关；
- M2：Day 12-19 Phase 1 工程门禁、架构边界和独立 Migration Host；
- M3：Day 20-26 身份与租户基础，已经实现到 Microsoft Entra 开发集成。

当前没有达到生产可用。最直接的阻断项是：

- 业务端点尚未完成授权策略；
- Day 26 的委托 scope 还没有作为后端授权条件强制执行；
- 追加式审计尚未建立；
- PostgreSQL RLS、生产 Provider 身份、可靠 ETL、staging、备份恢复、SLO 和
  发布链路尚未完成。

因此，当前工程重点不是继续拉长 Day 编号，而是完成 M4 的安全边界：

```text
M4 = RBAC + 端点保护 + 稳定授权错误 + 追加式审计 + Phase 2 gate
```

## 2. 文档权威关系

发生冲突时按以下顺序处理：

1. 生产安全与数据正确性；
2. [outline.md](../outline.md)；
3. [docs/current-state.md](../docs/current-state.md)；
4. [docs/roadmap.md](../docs/roadmap.md)；
5. 本工程规划总纲；
6. [current-playbook.md](current-playbook.md)；
7. [docs/days/](../docs/days/) 中的 Day 胶囊；
8. [archive/](archive/) 中的历史施工材料。

`current-playbook.md` 可以比本文更细，但不能扩大本文和 roadmap 明确排除的范围。

## 3. 规划原则

本项目继续保留 Day 编号，但 Day 只表示可验收的施工单元，不表示自然日，也不表示
生产成熟度百分比。

规划规则：

- 只展开当前里程碑和下一里程碑；
- 后续里程碑只保留目标和门禁，不预先写成 100+ Day 任务表；
- 当前 Day 没有验收通过时，不创建新的 Day 来掩盖问题；
- review 发现阻断问题时，当前 Day 保持 `Validation` 或 `Blocked`，修复后重新验收；
- 每个 Day 必须能追溯到风险、生产差距、ADR、阶段目标或明确的用户需求；
- 每个阶段最后一个 Day 只做门禁、证据汇总和出关判断，不偷偷增加新功能。

## 4. 里程碑地图

| 里程碑 | 范围 | 状态 | 工程目标 |
| --- | --- | --- | --- |
| M0 开发基线 | Day 1-7 | Complete | 建立本地 Azure、PostgreSQL、API、Worker 数据底座 |
| M1 基线治理 | Day 8-11 | Complete | 建立事实基线、风险、架构和 Phase 0 gate |
| M2 工程基础 | Day 12-19 | Accepted | 建立静态门禁、架构测试、模块化和独立 Migration Host |
| M3 身份与租户基础 | Day 20-26 | Implemented, phase open | 建立 Tenant、TenantContext、租户感知 repository、OIDC 和 Entra 开发集成 |
| M4 RBAC、端点保护与审计 | Day 27-30 | Active | 关闭“已认证但未授权”和缺失审计的 Phase 2 核心风险 |
| M5 生产数据模型 | Day 31-40 | Next | 建立 lineage、资源生命周期、成本语义、数据质量和 migration 演练 |
| M6 可靠 ETL 平台 | 后续 | Not started | 调度、lease、retry、checkpoint、backfill 和运维控制 |
| M7 Release A 平台基础 | 后续 | Not started | telemetry、容器、环境、CI/CD、备份恢复和发布门禁 |
| M8 Azure 生产能力 | 后续 | Not started | 生产 Azure Provider、FinOps 语义、治理 workflow、API/frontend |
| M9 多云能力 | 后续 | Not started | AWS Provider 和 Azure/AWS 统一契约 |
| M10 系统加固与上线 | 后续 | Not started | 安全、供应链、性能、韧性、DR、Go/No-Go 和 canary |

## 5. 当前里程碑 M4

M4 的目标是关闭 Phase 2 最核心的安全缺口：认证主体已经可以进入系统，但后端还没有
完整的权限、范围、端点策略和审计闭环。

### Day 27 - 权限与范围 RBAC

目标：

- 定义权限词汇表；
- 定义 tenant、CloudAccount、平台范围；
- 建立 role/grant 或等价授权模型；
- 将 已认证的 `iss/sub`、Membership 和 TenantContext 接入授权评估；
- 用 allow/deny matrix 覆盖正向和负向路径。

不得顺手完成：

- 全端点策略应用；
- 全局 401/403 Problem Details；
- 追加式审计持久化；
- PostgreSQL RLS；
- React/browser authorization。

出关条件：

- RBAC 模型和范围评估有自动化测试；
- 负向授权路径被验证；
- Day 27 胶囊进入 `Accepted`，或明确保留 `Validation` 的未决项。

### Day 28 - 端点保护与授权错误契约

目标：

- 所有现有业务端点要么绑定授权 policy，要么明确记录 anonymous 理由；
- `/api/admin` 类管理操作不可匿名；
- 稳定 401/403 response contract；
- correlation ID 和错误脱敏策略进入 API 边界；
- 授权失败不泄漏 tenant、resource、SQL、token 或内部 stack。

不得顺手完成：

- 审计数据模型；
- API v1 全面版本化；
- rate limit、pagination、OpenAPI breaking-change gate。

出关条件：

- 端点清单与授权策略对齐；
- 匿名、无权限、跨租户、跨范围路径都有负例；
- RISK-0001/GAP-001 有新的控制证据。

### Day 29 - 追加式审计

目标：

- 建立 追加式审计 数据模型；
- 记录 actor、subject、tenant、permission/action、target、result、correlation；
- 覆盖高权限操作的成功和失败；
- 普通业务身份不能修改或删除审计记录；
- 审计与普通日志分离。

不得顺手完成：

- 全量治理 workflow；
- 外部 SIEM 集成；
- 长期 retention 和归档策略。

出关条件：

- privileged action 的成功/失败都有审计记录；
- 审计记录不可被普通路径篡改；
- 审计字段不泄漏 secret 或敏感 payload。

### Day 30 - Phase 2 安全门禁

目标：

- 执行 tenant escape、IDOR、RBAC、端点保护和审计 E2E；
- 复验真实 Entra token 到 TenantContext 到授权决策的闭环；
- 更新风险登记、生产差距和 current-state；
- 给出 Phase 2 是否出关的明确结论。

不得顺手完成：

- M5 数据模型功能；
- 可靠 ETL 调度；
- 前端授权体验。

出关条件：

- tenant A 无法访问 tenant B；
- 未授权主体无法访问业务端点；
- 高权限动作有审计；
- RISK-0001 和 RISK-0002 至少完成 Phase 2 范围内的控制证据；
- Owner 明确给出 `Accepted`、`Validation`、`Rejected` 或 `Blocked`。

## 6. 下一里程碑 M5

M5 只在 M4 出关后启动。目标是把当前资源、成本、ETL 和运行数据从开发形态推进到
可解释、可追溯、可迁移和可恢复的生产数据模型。

建议顺序：

| Unit | 目标 | 关键证据 |
| --- | --- | --- |
| Day 31 | Raw / Normalized / Derived / Operational 数据分层 ADR | 每类现有数据归层，source 和 lineage 字段清楚 |
| Day 32 | ingestion metadata 与 raw payload reference | raw 与 normalized 可追溯，敏感 payload 有分类和访问边界 |
| Day 33 | 资源 lifecycle：scan run、active/inactive/deleted、关系 | 部分失败不误删，完整扫描才允许失活 |
| Day 34 | 成本语义：cost type、charge type、账期、修订、原币种 | 多币种不混加，迟到修订可追溯 |
| Day 35 | ETL operational model：JobDefinition、Run、Attempt、Checkpoint | heartbeat、attempt、progress 和 error category 可验证 |
| Day 36 | Rule、Finding、Waiver、Event 基础 schema | 指纹、版本、状态历史和 tenant 边界清楚 |
| Day 37 | 数据质量与 retention 骨架 | 缺字段、重复、错误日期、币种和 tenant/account mismatch 可检测 |
| Day 38 | expand/contract 和 backfill 兼容测试 | 老版本兼容、backfill 可恢复、无静默截断 |
| Day 39 | 接近 staging 数据量的 migration 与恢复演练 | 时长、锁、恢复点和 reconciliation 有证据 |
| Day 40 | M5 数据门禁 | 数据语义、lineage、质量、恢复和 data dictionary 全部有结论 |

M5 直接关联以下风险和差距：

- RISK-0007：资源删除和失活语义缺失；
- RISK-0008：成本粒度和账单语义有限；
- RISK-0020：无数据 retention 与删除策略；
- RISK-0021：错误、日志和 raw JSON 可能泄漏敏感元数据；
- GAP-007、GAP-008、GAP-019、GAP-020、GAP-021。

## 7. 后续里程碑只保留目标

M6 以后不再预写 Day 级长表，直到 M5 的门禁证据足够明确。

保留目标：

- M6：可靠 ETL 平台，关闭调度、lease、retry、checkpoint 和 operator replay 缺口；
- M7：Release A 平台基础，建立 telemetry、staging、artifact promotion、backup 和恢复演练；
- M8：Azure 生产能力，完成 Provider 身份、权限预检、Resource Graph、Cost、Policy、Monitor 和 Azure staging E2E；
- M9：多云能力，接入 AWS 并验证 Azure/AWS 统一契约；
- M10：系统加固与上线，完成安全、供应链、性能、韧性、DR、Go/No-Go 和 canary。

这些里程碑可以调整，但调整必须说明原因、影响范围、被延后的风险和新的门禁证据。

## 8. 每个 Day 的执行契约

每个 Day 开工前必须确认：

- 当前 Day 胶囊存在，且使用固定 9 段结构；
- 上一依赖 Day 或阶段门没有阻断项；
- 工作树状态已检查，不覆盖无关修改；
- 本 Day 的风险、差距、ADR 或阶段目标已明确；
- 本 Day 的 non-goals 已写清楚。

每个 Day 收尾必须至少更新：

- 对应 [docs/days/day-x.md](../docs/days/)；
- 受影响的 current-state、roadmap、risk/gap 或 ADR；
- 必要的运行说明或施工手册；
- closeout 证据，长期有效结论不能只留在 `tmp/`。

## 9. 验证基线

默认最低验证：

```powershell
./scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit -SkipDependencyOutdated
```

如果改动包含数据库 schema：

```powershell
./scripts/Test-DatabaseMigration.ps1
```

如果改动包含 Azure、Entra、Terraform 或真实 Provider 行为，必须补充对应 E2E 或手工证据，
并记录清理结果。

## 10. 计划调整规则

允许调整计划的情况：

- review 发现设计模型不成立；
- 安全、数据正确性或生产门禁要求改变；
- 真实 Provider、数据库、身份平台行为与假设不一致；
- 当前 Day 暴露出必须先解决的前置风险。

不允许的调整：

- 为了显得进度快而把阻断问题挪到后续 Day；
- 用“已经完成很多 Day”替代阶段门证据；
- 把样例 fallback、匿名端点或本地开发身份包装成生产能力；
- 在没有 Owner 接受的情况下关闭 Critical/High 风险。

调整后必须更新：

- 本文；
- [docs/roadmap.md](../docs/roadmap.md)；
- 相关 Day 胶囊；
- 风险或生产差距登记。

## 11. 与旧路线的关系

旧文档仍有参考价值：

- 它记录了最初从 Day 8 到 Day 148 的学习路线；
- 它保留了很多生产化主题清单；
- 它适合回看“当时为什么这么规划”。

但它不再作为当前计划来源。

当前做法是：

- 用 [docs/roadmap.md](../docs/roadmap.md) 管里程碑；
- 用本文管理工程规划；
- 用 [current-playbook.md](current-playbook.md) 管当前 Day 执行；
- 用 [docs/days/](../docs/days/) 管历史回顾；
- 用 [archive/](archive/) 保留旧施工材料。
