# 架构决策记录 Backlog

## 1. 使用规则

本文件登记必须作出的重要决策。状态 `Proposed` 表示尚未形成候选方案；
`CandidateDecision` 表示 Agent 已经形成可执行候选决策，但仍需要项目 Owner
确认后才能视为正式 `Accepted`。进入实现前应建立独立 ADR，记录上下文、选项、
决策和后果。

## 2. 决策队列

| ADR ID | 标题 | 触发原因 | 备选方向 | 决策期限 | Owner | 状态 | 相关风险 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [ADR-0001](../adr/ADR-0001-module-boundaries-and-architecture-tests.md) | 模块边界与架构测试规则 | 当前依赖仅靠人工检查 | 反射架构测试；专用架构测试库；编译边界 | 阶段 1 | Platform Architect | CandidateDecision | RISK-0013 |
| [ADR-0002](../adr/ADR-0002-migration-host-and-release-flow.md) | 独立 Migration Host 与发布流程 | API/Worker 自动 migration | 独立 Host；CI migration Job；显式 CLI command | 阶段 1 | Platform SRE | CandidateDecision | RISK-0003 |
| ADR-0003 | Organization/Tenant/CloudAccount 模型 | Azure tenant 不等于业务 tenant | 单库共享 schema；schema-per-tenant；database-per-tenant | 阶段 2 | Platform Architect | Proposed | RISK-0002 |
| ADR-0004 | Entra ID、service identity 与本地身份 | 当前依赖 Azure CLI 用户 | Managed Identity；Workload Identity；受控服务主体 | 阶段 2 | Security Owner | Proposed | RISK-0001、RISK-0018 |
| ADR-0005 | tenant 隔离与 PostgreSQL RLS | 查询和唯一键无 tenant 条件 | 应用过滤 + RLS；仅应用过滤；物理隔离 | 阶段 2～3 | Security Owner | Proposed | RISK-0002 |
| ADR-0006 | Raw/Normalized/Derived/Operational 数据分层 | raw JSON、业务表和运行状态混合 | PostgreSQL 分层；对象存储 Raw + DB；事件流分层 | 阶段 3 | Data Owner | Proposed | RISK-0008、RISK-0020、RISK-0021 |
| ADR-0007 | 资源 full-scan 与 inactive/deleted 语义 | 删除资源仍保留为当前数据 | scan ID + mark inactive；事件增量；双轨 reconciliation | 阶段 3 | Data Owner | Proposed | RISK-0007 |
| ADR-0008 | Job queue、scheduler、lease 与 checkpoint | ETL 仅手工且可并发 | PostgreSQL queue；Service Bus；平台原生调度 | 阶段 4 | Application Owner | Proposed | RISK-0004、RISK-0005、RISK-0019 |
| ADR-0009 | production sample 物理隔离策略 | fallback 可将故障变为样例成功 | 编译/部署排除；环境启动失败；独立 demo Provider | 阶段 5 | FinOps Product Owner | Proposed | RISK-0006 |
| ADR-0010 | OpenTelemetry backend 与 SLO 平台 | 当前无 trace/metric/SLO | Azure Monitor；Grafana stack；托管可观测平台 | Release A、阶段 11 | Platform SRE | Proposed | RISK-0010 |
| ADR-0011 | development/staging/production 部署平台 | 当前只有 local | Azure Container Apps；AKS；其他托管容器平台 | Release A、阶段 12 | Platform Architect | Proposed | RISK-0012、RISK-0025 |
| ADR-0012 | Terraform remote state 与环境隔离 | 本地 state 无锁与审计 | Azure Storage backend；Terraform Cloud；其他受控 backend | Release A、阶段 12 | Cloud Provider Owner | Proposed | RISK-0014 |
| ADR-0013 | Service Bus topology 与 outbox/inbox | 当前 Queue 未接入 runtime | topic/subscription；queue-per-workload；DB queue | 阶段 10 | Platform Architect | Proposed | RISK-0009 |
| ADR-0014 | 备份、PITR、RPO/RTO 与 DR | 当前单卷无恢复能力 | Azure Database for PostgreSQL HA/PITR；其他托管 PostgreSQL | Release A、阶段 15 | Platform SRE | Proposed | RISK-0011、RISK-0025 |
| ADR-0015 | 成本事实、修订、币种与 lineage | 当前聚合语义不足 | Actual/Amortized 分表；统一事实模型；Provider 原生模型 | 阶段 3、6 | FinOps Product Owner | Proposed | RISK-0008 |
| ADR-0016 | 数据 retention、删除和合法保留 | 分类已建立但期限未定 | 分类默认期限；按租户策略；法规/地区策略 | 阶段 3 | Data Owner | Proposed | RISK-0020 |
| ADR-0017 | API 契约、版本、分页和错误模型 | 当前 Minimal API 契约有限 | URI/version header；cursor/offset；统一 Problem Details | 阶段 8 | Application Owner | Proposed | RISK-0026 |
| [ADR-0018](../adr/ADR-0018-dependency-and-toolchain-governance.md) | 依赖和工具链版本治理 | NuGet 与 Terraform CLI 已出现版本漂移 | 固定升级节奏；Renovate/Dependabot；人工批量升级；CI 门禁 | 阶段 1、14 | Application Owner / Platform SRE | CandidateDecision | RISK-0027 |

## 3. 阶段 1 最小决策输入

ADR-0001、ADR-0002 和 ADR-0018 已形成可执行候选决策。Agent 可以基于这些
ADR 继续实现阶段 1 的脚本、测试、Migration Host 和文档；项目 Owner 仍需在
阶段 1 开工或合并前确认这些 CandidateDecision 是否接受为正式 `Accepted`。

阶段 1 的可重复入口确定为 `scripts/Test-RepositoryStatic.ps1`，由 Day 13
实现并在 Day 19 CI 中调用。该入口至少覆盖：

- secret 检查；
- dependency vulnerable/deprecated/outdated 检查；
- JSON/YAML/XML/PowerShell/Markdown 解析；
- 格式检查；
- 架构测试；
- Terraform fmt/init/validate；
- build/test；
- Git 垃圾文件、`tmp/`、`bin/obj`、Terraform state/plan 检查。

ADR-0003～0005 可以在阶段 1 保持 Proposed，但阶段 2 实现身份或 tenant
schema 前必须完成审批。
