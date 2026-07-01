# ADR-0005: 生产数据分层、Lineage 与 Raw Reference

## 状态

Proposed

## 日期

2026-07-01

## 背景

M4 已完成 RBAC、端点保护与追加式授权审计，项目正式进入 M5 - 生产数据模型。M5 的首个问题不是立刻改表，而是先定义生产数据如何分层、如何追溯来源、哪些 payload 可以保存、哪些只能引用，以及后续 schema、migration、backfill 必须服从哪些边界。

当前代码已经有 `CloudResource`、`CloudCostDaily`、`EtlJobRun`、`AuthorizationAuditEvent`、Tenant/Organization/CloudAccount/Membership 等实体。它们支撑了开发链路，但仍存在以下生产缺口：

- `CloudCostDaily.RawJson` 把 provider 原始片段嵌入到 normalized cost 行中；
- `CloudResource.TagsJson` 保存 provider tag 字典，但缺少 scan identity、source run、schema version 和 parser version；
- `EtlJobRun` 只记录一次 run 的粗粒度状态，缺少 attempt、trigger、scope、checkpoint、heartbeat 和错误分类；
- `AuthorizationAuditEvent` 是 operational security audit，不等同于业务数据 lineage；
- `TenantId` 在部分实体上仍保留 nullable 兼容形态，后续需要结合 backfill 和 migration gate 收敛；
- 生产风险登记中仍存在 resource lifecycle、cost semantics、raw JSON 泄漏、retention、provider identity、Terraform state、ETL 调度等开放风险。

Day31 只建立分层与约束，不实施大规模 schema migration 或 backfill。

## 决策

生产数据模型采用四层分层：

| 层 | 职责 | 当前/后续代表对象 | 生产约束 |
| --- | --- | --- | --- |
| Raw | 保存或引用 provider 原始事实，供重放、审计和 parser 迭代使用 | 后续 raw payload store、raw reference、capture manifest | 默认不暴露给查询 API；必须有敏感级别、source、hash、retention 和访问控制 |
| Normalized | 把 provider 差异规整成平台可查询的业务事实 | `CloudResource`、`CloudCostDaily`、CloudAccount 相关事实 | 必须带 tenant/account/provider/source/run/schema/parser/时间语义；不能只靠 raw JSON 解释业务含义 |
| Derived | 从 normalized 数据计算出的聚合、规则结果、发现和报表事实 | 后续 cost aggregation、resource state view、governance finding | 必须可从上游版本和规则版本重放；不能覆盖 normalized 原始事实 |
| Operational | 支撑系统运行、安全、审计和控制面的事实 | `EtlJobRun`、`AuthorizationAuditEvent`、后续 checkpoint/lease/outbox | 用于运维与审计；不作为财务或资源事实口径 |

每一条 Normalized 或 Derived 数据在进入生产前必须能回答：

- 属于哪个 `tenant_id`、provider、cloud account 或 subscription；
- 来自哪个 source system、ingestion job、job run 或 scan；
- 使用哪个 `schema_version` 和 `parser_version`；
- provider 事实发生时间、观测时间、加载时间分别是什么；
- raw payload 是否保存、保存在哪里、hash 是什么、敏感级别和保留期是什么；
- 该行是否可由 raw reference、parser version 和规则版本重放；
- 是否存在 data quality 状态、错误分类或人工豁免。

当前实体归层如下：

| 当前实体/表 | 归层 | Day31 结论 | 后续输入 |
| --- | --- | --- | --- |
| `CloudResource` | Normalized | 当前是资源清单事实，但缺 scan identity、lifecycle、schema/parser version 和 raw reference | Day32/33 补 lineage 和 lifecycle |
| `CloudCostDaily` | Normalized cost fact | 当前以日、服务、资源组、币种聚合，`RawJson` 是过渡兼容字段 | Day32/34 补 raw reference、账期、charge/cost type、修订语义 |
| `EtlJobRun` | Operational | 当前记录 run 状态和记录数，不足以支撑可靠调度和恢复 | Day35/42 补 attempt、trigger、scope、checkpoint、heartbeat |
| `AuthorizationAuditEvent` | Operational security audit | 当前用于授权 allow/deny 追踪，不作为业务数据 lineage | 后续补 retention、查询、append-only 硬化 |
| Organization/Tenant/Membership/ProviderConnection/CloudAccount | Operational / Reference | 当前作为身份、租户和云账号主数据 | 后续补 lifecycle、状态机、审计和生产配置边界 |
| API 查询 DTO / 统计结果 | Derived / Projection | 当前主要是即时投影，不作为持久 derived fact | 后续在需要持久化报表、finding、event 时补 rule/version |

`CloudResource.TagsJson` 视为 normalized attribute bag，不视为完整 Raw payload。`CloudCostDaily.RawJson` 视为历史兼容和开发阶段证据，不再作为生产 raw 存储方向。Day32 以后应引入 raw reference 和 metadata 后逐步降低对嵌入式 raw JSON 的依赖。

## 不做的决策

Day31 不做以下事项：

- 不新增或修改数据库 migration；
- 不执行 legacy backfill；
- 不把当前 `RawJson` 宣称为生产 lineage；
- 不把 raw payload 暴露给查询 API；
- 不引入单一万能事件表替代明确的业务表；
- 不关闭 RLS、retention、provider identity、ETL 调度、成本语义或资源生命周期风险。

## 备选方案

### 方案 A：继续把 raw JSON 嵌入业务表

拒绝。该方案短期方便，但会让敏感数据、retention、schema 版本和 parser 重放边界混在业务查询表中，后续很难证明财务和治理结论。

### 方案 B：Day31 直接实施大规模 schema migration

拒绝。当前需要先统一模型和迁移边界，再由 Day32-40 分步骤做 expand/contract、backfill 和 gate。提前大改表会增加回滚和语义风险。

### 方案 C：所有数据统一进入一张通用事件表

拒绝。通用事件表会削弱资源、成本、审计、调度等不同事实的约束和索引语义，也不利于 FinOps 查询和合规审计。

## 后果

正向后果：

- Day32-40 有明确输入，不会在 schema 改造时反复争论分层边界；
- raw payload、lineage、parser version、retention 和敏感数据访问控制成为生产数据模型的一等约束；
- M5 能把资源生命周期、成本语义、数据质量和 migration 演练串成连续路径。

代价和风险：

- Day31 本身不关闭生产数据缺口；
- 当前代码仍存在嵌入式 `RawJson`、nullable `TenantId` 兼容和 ETL operational metadata 不足；
- 若后续 Day 不按该 ADR 落地字段和 gate，M5 仍无法出关。

## 后续执行顺序

- Day32：ingestion metadata 和 raw payload reference；
- Day33：resource lifecycle、scan identity 和 inactive/deleted 语义；
- Day34：成本语义、账期、币种、修订和重算；
- Day35：ETL run、attempt、trigger、checkpoint 和 heartbeat；
- Day36：Rule、Finding、Waiver、Event、Outbox、Inbox、Notification 基础 schema；
- Day37：数据质量规则和 retention 骨架；
- Day38：expand/contract、backfill、schema compatibility；
- Day39：staging-like migration 和 rollback rehearsal；
- Day40：M5 数据门禁。

## 验证

Day31 验证方式：

- ADR 与当前 `CloudResource`、`CloudCostDaily`、`EtlJobRun`、`AuthorizationAuditEvent` 和 repository 行为逐项对齐；
- 不产生数据库 migration；
- 通过静态验证、数据库 migration 验证和 Azure Terraform apply/verify/destroy 闭环；
- Day31 胶囊和 current-state 明确记录 M5 仍未生产可用。

## 相关风险

- GAP-007 / RISK-0007：资源生命周期缺失；
- GAP-008、GAP-019 / RISK-0008：成本语义和账单 lineage 不足；
- GAP-020：ETL 运行模型字段不足；
- GAP-021 / RISK-0020：数据 retention 与分类缺失；
- RISK-0021：错误、日志和 raw JSON 可能泄漏敏感元数据；
- RISK-0014：Terraform 本地 state 仍仅允许个人临时资源。
