# 生产差距登记

## 1. 文档定位

- 登记日期：2026 年 6 月 14 日
- 对应基线：`docs/phase-0/current-capability-baseline.md`
- 当前状态：`ReadyForReview`

本文记录“当前能力为什么还不能进入生产，以及计划在哪个阶段解决”。它不是
Day 11 的正式风险登记册，因此本日不虚构 Owner、概率和风险评分；Day 11 会在
本表基础上补充治理责任、严重度、决策和阶段门禁。

## 2. 生产阻断差距

| ID | 差距 | 当前证据 | 为什么阻止生产 | 临时边界 | 目标阶段 | 主要依赖 |
| --- | --- | --- | --- | --- | --- | --- |
| GAP-001 | 管理 API 和查询 API 匿名 | `FinOps.Api/Program.cs` 无认证授权 | 任意调用者可读取成本、枚举订阅或触发云采集与写库 | 仅绑定本机并保持非公开 | 阶段 2、8 | Identity、RBAC、API policy |
| GAP-002 | 无业务 tenant 隔离 | 核心表无 `tenant_id` | 无法证明组织间数据和操作隔离 | 仅单人单环境学习 | 阶段 2～3 | tenancy ADR、可信 TenantContext |
| GAP-003 | migration 发布编排不足 | Day 18 已由独立 `FinOps.Migrator` 替代；API/Worker 无 migration API，IL 门禁覆盖直接调用和方法组别名；Migrator 使用 advisory lock | 自动 migration 和同库 Migrator 并发风险已关闭；仍缺发布审批、生产身份和回滚编排 | Migrator 必须先于业务宿主运行 | 阶段 12 | CI/CD migration gate、部署顺序 |
| GAP-004 | 成本 sample fallback 默认开启 | 两个 appsettings 中为 `true` | Provider 故障或空数据可能表现为成功数据 | 只允许明确标记的本地演示 | 阶段 5～6 | 环境隔离、数据 provenance |
| GAP-005 | Azure CLI 用户身份 | `DefaultAzureCredential` + 本地 `az login` | 无 workload identity、轮换和最小权限证明 | 仅开发机使用 | 阶段 2、5 | Managed/Workload Identity、RBAC |
| GAP-006 | 无调度、租约和恢复协议 | Worker 一次执行后退出 | 并发执行、崩溃接管、重试和断点恢复不可控 | 只手工单实例触发 | 阶段 4 | Job 模型、queue、lease、checkpoint |
| GAP-007 | 资源无 inactive/deleted 生命周期 | 仅 FirstSeenAt/LastSeenAt | 已删除资源仍会参与清单和统计 | 不用于实时 CMDB 结论 | 阶段 3、5 | scan identity、失活策略 |
| GAP-008 | 成本语义和粒度有限 | Daily + Service + ResourceGroup + Currency | 无法支持资源级归因、账期修订和成本类型解释 | 明确标注为聚合视图 | 阶段 3、6 | 成本 ADR、lineage、billing semantics |
| GAP-009 | Terraform 本地 state | 无 backend 配置 | 无团队锁、恢复、审计和环境隔离 | 仅个人临时资源 | 阶段 12 | 远端 backend、身份和环境策略 |
| GAP-010 | 开发配置不适合生产 | 开发密码、`AllowedHosts=*`、本地端口 | secret、Host 约束和网络边界不合格 | 禁止暴露公网 | 阶段 1～2、12 | Secret store、环境验证、网络设计 |
| GAP-011 | 无完整可观测性和 SLO | 只有结构化日志与 health | 无 trace、metric、告警、错误预算和容量证据 | 人工查看日志 | Release A、阶段 11 | OpenTelemetry、监控后端 |
| GAP-012 | 无 CI/CD 与 staging | Day 19 已建立初版 GitHub Actions 静态和数据库门禁；仍无 staging、artifact promotion 和部署定义 | 变更可自动验证，但仍无法自动、可重复地晋级和回滚 | CI 仅作为代码合并证据，不代表可部署 | Release A、阶段 12 | artifact、staging、CD、branch protection |
| GAP-013 | 无备份、PITR 和恢复演练 | Compose 单卷，无恢复证据 | 数据丢失后无法满足 RPO/RTO | 数据可随时重建的学习环境 | Release A、阶段 15 | 托管数据库、备份策略、runbook |
| GAP-014 | 无 outbox/inbox | 无事件持久化和消费模型 | 数据提交与消息发布无法保证一致性 | 暂不发送治理事件 | 阶段 10 | 事件 ADR、幂等消费 |

## 3. 质量与规模差距

| ID | 差距 | 当前证据 | 影响 | 目标阶段 |
| --- | --- | --- | --- | --- |
| GAP-015 | 测试层次不足 | 当前 44 个执行测试加独立数据库 migration/权限回归 | 仍无认证、tenant、Provider 故障注入、负载和生产规模回归 | 阶段 2～4、14 |
| GAP-016 | API 契约未生产化 | Minimal API 已拆分为按领域组织的 endpoint modules | 无版本、分页、稳定错误码、限流和 OpenAPI 治理 | 阶段 8 |
| GAP-017 | Provider 可靠性策略不统一 | 外部调用无统一 retry/backoff/错误分类 | 限流、暂时故障和永久错误无法稳定区分 | 阶段 4～7 |
| GAP-018 | 资源同步无 checkpoint | Resource Graph 仅单次内消费 SkipToken | 大规模扫描中断后需要从头开始 | 阶段 4～5 |
| GAP-019 | 成本查询无账单 lineage | 仅保存聚合行及 raw_json | 无法证明账期版本、重算来源和可追溯修订 | 阶段 3、6 |
| GAP-020 | ETL 运行模型字段不足 | run 仅含状态、数量、错误摘要 | 无 attempt、trigger、scope、correlation、heartbeat | 阶段 3～4 |
| GAP-021 | 无数据保留和分类策略 | 表和文档未定义 retention/classification | 存储增长、隐私和审计边界不明确 | Day 11、阶段 3 |
| GAP-022 | 无依赖与供应链自动门禁 | Day 19 CI 已运行候选文件 secret 模式、NuGet vulnerable/deprecated/outdated、actionlint 和 Terraform 静态门禁 | 仍无历史 secret 扫描、SBOM、许可证策略、容器扫描和 provenance | 阶段 14 |

## 4. 尚未实现的产品范围

以下能力是明确的 `Planned`，不能算作当前生产缺陷修补完成，也不能在对外材料
中使用“已支持”：

| 能力 | 当前事实 | 目标阶段 |
| --- | --- | --- |
| Azure Policy、Monitor、Finding、Waiver | 只有合规 DTO/interface，没有 Provider、规则引擎或数据表 | 阶段 7、9 |
| React 前端 | 仓库没有前端工程 | 阶段 8 |
| 通知和治理事件 | 无 outbox、inbox、通知渠道 | 阶段 10 |
| AWS Provider | 无 AWS SDK、身份、资源或成本实现 | 阶段 13 |
| 多云统一生产能力 | 目前只有 Azure 数据底座 | Release C |
| 自动整改 | 无审批、审计、执行和回滚链路 | Release D |

## 5. Day 8 决策

1. 当前项目定位为“Azure 数据底座和生产化建设起点”，不是生产平台。
2. Day 9 只负责重新取得运行证据，不顺手修复本表差距。
3. Day 10 负责把现状数据流和依赖方向画清楚。
4. Day 11 为本表补 Owner、严重度、阶段门禁和 ADR 队列。
5. 任何后续实现都必须关联一个差距 ID、阶段目标或明确的新风险。
