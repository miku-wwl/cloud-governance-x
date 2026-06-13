# Cloud Governance X 生产级项目纲领

## 1. 文档定位

本文件是 Cloud Governance X 的长期项目纲领，用于定义：

- 项目为什么存在；
- 生产级意味着什么；
- 产品边界和核心能力是什么；
- 架构、数据、安全、可靠性和运维必须遵守哪些原则；
- 哪些能力可以分阶段交付，但不能用演示实现冒充生产实现。

本项目不再以固定天数或求职演示完成度作为最终标准。时间可以调整，质量门槛
不能降低。

`construction/01-★★★-construction-plan.md` 负责描述具体建设阶段、依赖关系、
交付物和出关条件。

## 2. 项目使命

Cloud Governance X 是一个面向组织级云环境的多云 FinOps 与资源治理平台。

平台统一接入 Azure 和 AWS 的资源、成本、合规、指标和事件数据，形成可审计、
可追踪、可扩展的治理控制平面，帮助平台团队、FinOps 团队、安全团队和资源
Owner 回答以下问题：

- 组织拥有哪些云账号、订阅和资源？
- 这些资源由谁负责，属于哪个环境、业务和成本中心？
- 成本发生在哪里，成本变化是否合理，能否正确归属？
- 哪些资源、标签、权限、网络和策略不合规？
- 哪些资源可能闲置、低效或存在浪费风险？
- 哪些成本或治理事件需要告警、确认、豁免或整改？
- 每条治理结论依据什么数据、规则和证据产生？
- 平台自身是否安全、可靠、可观测、可恢复？

## 3. 项目定位

项目定位不是“成本看板”，也不是“把多个云 API 包一层”。

目标定位是：

```text
基于 .NET 10、React、PostgreSQL、Terraform 和事件驱动架构构建的
生产级多云 FinOps 与资源治理平台。
```

平台由五个核心平面组成：

1. **接入平面**：管理组织、租户、云账号、订阅、凭据引用和 Provider 状态；
2. **数据平面**：采集、归一化、校验、存储资源、成本、合规和指标数据；
3. **治理平面**：执行归因、规则、异常检测、风险评估和整改工作流；
4. **控制平面**：提供安全 API、后台任务、事件处理、审批和审计能力；
5. **体验平面**：提供 Dashboard、查询、报告、告警和运维视图。

## 4. 生产级的定义

“生产级”不是功能数量多，而是系统可以在真实组织环境中被长期、重复、安全地
运行，并在失败时保持可解释和可恢复。

本项目达到生产级，至少必须同时满足以下条件。

### 4.1 安全

- 所有用户请求经过标准身份认证；
- 所有 API 和页面执行授权检查；
- 管理操作、读取操作和平台运维权限分离；
- 云凭据不进入代码、配置文件、日志和数据库明文；
- Azure 使用 Managed Identity、Workload Identity 或受控服务主体；
- AWS 使用 IAM Role、STS 和短期凭据；
- 服务、数据库、队列和对象存储遵循最小权限；
- 敏感配置来自密钥管理服务；
- 传输和静态数据加密；
- 存在威胁模型、依赖扫描、secret 扫描、容器扫描和 IaC 安全检查；
- 高风险治理动作必须审批、审计并支持 dry-run；
- 不允许未经授权的自动删除、停机或资源变更。

### 4.2 可靠性

- API、Worker 和调度器可以独立失败和恢复；
- 所有 ETL 和事件处理具有幂等性；
- 任务支持超时、取消、重试、退避和死信；
- 同一个任务不能无控制地并发执行；
- 外部云 API 限流、分页、暂时失败和最终一致性被显式处理；
- 失败状态持久化，不依赖单机日志；
- 数据库迁移不由多个生产实例争抢执行；
- 有备份、恢复、灾难恢复目标和定期演练；
- 发布失败可以回滚，数据库变更遵循向前兼容策略。

### 4.3 可观测性

- 日志、指标和分布式追踪统一使用关联 ID；
- 采用 OpenTelemetry 标准采集服务遥测；
- 核心 API、ETL、队列、数据库和 Provider 调用均有指标；
- 可以观察任务成功率、耗时、数据新鲜度、重试、死信和积压；
- 定义 SLI、SLO、告警阈值和错误预算；
- 告警必须可行动，不能只报告“系统出错”；
- 每类关键故障有 runbook。

### 4.4 数据正确性

- 每条数据都有 tenant、provider、account、source 和 ingestion lineage；
- 成本币种、成本类型、账期和云厂商修订行为被正确建模；
- 不同币种不会直接相加；
- 真实数据、推导数据、样例数据和测试数据严格隔离；
- sample fallback 不得在生产同步中伪装成功；
- 资源生命周期支持 active、inactive、deleted 或等价状态；
- 全量扫描和增量扫描有明确语义；
- 规则和 finding 支持版本、证据、指纹、首次发现、最近发现和解决状态；
- 数据质量检查可以发现缺字段、重复、异常突变和采集缺口。

### 4.5 可扩展性

- Azure 和 AWS 通过稳定的小型 Provider 契约接入；
- Application 和 Domain 不依赖云 SDK；
- 业务规则不复制为 Azure 版和 AWS 版两套；
- API 使用分页、过滤和限制，禁止无边界加载；
- Worker 支持水平扩展和任务分区；
- 数据库具备合理索引、归档和容量治理；
- 模块边界清晰，必要时可独立部署，但不为“微服务”而拆分微服务。

### 4.6 可运维性

- 环境至少区分 local、development、staging、production；
- 基础设施、配置和发布流程可重复；
- 变更通过 CI/CD 和环境门禁；
- 生产操作具有审计、审批和回滚；
- 平台自身成本可观测并受预算约束；
- 文档、架构决策记录、runbook 和恢复流程保持更新；
- 生产事件完成复盘并形成改进项。

### 4.7 分批生产，而不是一次性交付

生产级不等于等待所有规划功能完成后再第一次上线。

平台采用逐级发布：

1. **平台基础发布**：身份、tenant、RBAC、审计、数据模型、ETL、CI/CD、
   可观测性和恢复能力；
2. **Azure 生产发布**：Azure 资源、成本、Policy、FinOps 和治理工作流；
3. **多云生产发布**：AWS 接入和 Azure/AWS 统一治理；
4. **自动化治理发布**：事件、通知、审批和受控整改；
5. **高级优化发布**：闲置治理、预测、单位经济和更复杂算法。

每一批发布都必须独立满足安全、可靠性、数据正确性、可观测性和可恢复要求。
功能范围可以逐批增加，生产质量不能按批次打折。

## 5. 当前工程状态

当前仓库已经完成原 Day 1～7 的开发基线：

- .NET 10 API、Worker、Application、Domain、Infrastructure 和 Tests；
- PostgreSQL 本地环境和真实健康检查；
- Azure Terraform 创建、验证、销毁闭环；
- Azure CLI + `DefaultAzureCredential` 本地认证；
- Azure Resource Graph 资源清单同步；
- Azure Cost Management 成本同步；
- 资源、成本和 ETL 执行历史数据模型；
- Worker 与管理 API 两种手工同步入口；
- 幂等写入和真实 Azure 端到端验收。

这些成果是生产系统的有效基础，但仍属于**开发基线**，不能直接视为生产就绪。

当前必须承认的差距包括：

- 没有用户身份认证和 RBAC；
- 没有 tenant 隔离模型；
- 管理 API 可以直接触发云同步；
- API 和 Worker 启动时自动执行 migration；
- ETL 没有生产调度、租约、互斥、限流和断点续跑；
- sample fallback 与生产配置尚未物理隔离；
- 资源失效和删除状态尚未建模；
- 成本粒度不足以支持精确单资源或标签归因；
- 没有 OpenTelemetry、SLO、告警和 runbook；
- 没有 CI/CD、staging、生产部署和回滚能力；
- 没有 Azure Policy、AWS、前端和正式合规工作流；
- 没有负载、故障、恢复和安全验证。

后续建设必须优先补齐基础质量，不允许在这些差距上继续堆叠大量功能。

## 6. 用户和职责角色

### 6.1 Platform Administrator

- 管理组织、租户、云账号和 Provider 连接；
- 管理平台配置、规则、调度和集成；
- 查看系统健康、任务、队列和审计；
- 不能绕过审计直接执行隐蔽治理操作。

### 6.2 FinOps Analyst

- 查看成本趋势、归因、预算和异常；
- 管理成本中心、标签映射、预算和异常确认；
- 生成成本优化建议；
- 不默认拥有云资源修改权限。

### 6.3 Governance Operator

- 查看合规 finding；
- 分配 Owner、设置状态、记录豁免和整改证据；
- 发起受控整改；
- 不能修改平台级安全设置。

### 6.4 Resource Owner

- 查看自己负责的资源、成本、finding 和整改任务；
- 确认、评论、申请豁免或提交整改证据；
- 只能访问授权范围。

### 6.5 Auditor / Read-only Viewer

- 查看历史数据、规则版本、事件和审计记录；
- 不执行同步、修改、豁免或整改。

### 6.6 Platform SRE

- 维护服务、数据库、队列、发布、备份、恢复和告警；
- 可以执行受控运维操作；
- 运维行为必须进入审计日志。

## 7. 核心业务能力

### 7.1 组织、租户和云账号管理

- Organization / Tenant；
- Azure Tenant / Subscription；
- AWS Organization / Account；
- Provider connection；
- credential reference；
- region、environment 和 ownership 元数据；
- onboarding、suspend、reconnect 和 offboarding 生命周期；
- 连接健康、权限检查和采集能力探测。

### 7.2 多云资源清单

- Azure Resource Graph；
- AWS Resource Explorer、Resource Groups Tagging API 和必要的服务 API；
- 资源唯一身份、类型、区域、账号、标签和关系；
- 资源 active/inactive/deleted 生命周期；
- 全量同步、增量同步和快照；
- 资源关系，如磁盘到实例、IP 到网络接口、资源到 Resource Group；
- 数据新鲜度和采集覆盖率。

### 7.3 多云成本与账单

- Azure Cost Management；
- AWS Cost Explorer 或 CUR 路径；
- 实际、摊销、未摊销、信用、退款、税费等成本类型；
- 日期、账期、服务、账号、区域、Resource Group、标签和资源维度；
- 原币种存储；
- 可选统一展示币种及汇率来源；
- 账单修订、迟到数据和重算；
- 成本数据 lineage 和质量检查；
- 预算、预测、环比、同比和单位成本扩展。

### 7.4 成本归因

- account / subscription；
- Resource Group；
- service；
- environment；
- owner；
- cost-center；
- business unit；
- tag mapping；
- shared cost allocation；
- unallocated cost；
- attribution confidence 和依据。

归因结果必须区分：

- 云账单直接提供的维度；
- 通过资源关系推导的维度；
- 组织规则分摊的维度；
- 无法归属的成本。

不得把 Resource Group 级成本描述为单资源精确成本。

### 7.5 合规和治理规则

- 平台自有规则引擎；
- Azure Policy compliance；
- AWS Config compliance；
- 标签、权限、网络、加密、公开访问、备份和生命周期规则；
- 规则版本、适用范围、参数、严重性和生效时间；
- finding 指纹、证据、状态、Owner、首次/最近发现时间；
- 误报、豁免、到期、重新打开和解决；
- 规则 dry-run 和影响预览；
- 整改建议、工单和审批。

平台自有规则、Azure Policy 和 AWS Config 是三种不同来源，必须在模型和界面中
清楚区分，不能用 Policy-style 模拟结果冒充真实云策略结果。

### 7.6 闲置和低效资源治理

- 孤立公网 IP；
- 未挂载磁盘；
- 长期停止实例；
- 低 CPU、内存、网络利用率资源；
- 闲置负载均衡；
- 过期快照和日志；
- Kubernetes 节点、PVC、requests/limits 等后续扩展。

闲置判断必须包含：

- 观察窗口；
- 指标来源；
- 阈值和规则版本；
- 数据缺失处理；
- 置信度；
- 预计节省；
- 风险和排除条件。

平台默认只生成建议，不自动删除资源。

### 7.7 成本异常检测

3σ 滑动窗口可以作为第一版可解释基线，但生产能力必须进一步支持：

- 最小历史数据要求；
- 周期性和工作日模式；
- 稀疏数据；
- 零成本到非零成本；
- 退款和负成本；
- 按账号、服务、区域和标签的不同基线；
- 算法版本和参数；
- backtest；
- precision、recall 和误报率评估；
- 用户确认、忽略和反馈；
- 与预算告警区分。

异常检测输出必须可解释，不得简单标记为“AI”。

### 7.8 事件和通知

- Governance Event 标准模型；
- Azure Service Bus 事件传输；
- outbox / inbox；
- 幂等消费；
- retry、backoff、dead-letter；
- 处理历史和关联 ID；
- Email、Teams、Webhook 等通知适配器；
- 告警去重、抑制、聚合、升级和静默窗口；
- 通知模板版本和发送审计。

### 7.9 整改工作流

- finding 分配；
- Owner 确认；
- 修复建议；
- 豁免申请；
- 审批；
- 到期提醒；
- 修复验证；
- 解决和重新打开；
- 自动整改的 dry-run、审批、执行、验证和回滚。

生产首个版本不允许无审批自动删除或停机。

### 7.10 Dashboard 和报告

- Overview；
- Costs；
- Resources；
- Compliance；
- Findings；
- Anomalies；
- Events；
- ETL Runs；
- Provider Connections；
- Platform Operations；
- 可保存过滤器；
- CSV / JSON / Markdown / PDF 等受控导出；
- 面向管理层、FinOps、Owner 和审计员的不同视图。

## 8. 目标架构

### 8.1 架构策略

优先采用**模块化单体 + 独立 Worker**，而不是过早拆成大量微服务。

原因：

- 领域模型仍在演进；
- 当前团队规模有限；
- 事务、部署和本地开发成本更低；
- 仍可通过模块边界、独立 Worker 和事件接口实现演进；
- 当某个模块出现明确的独立扩缩容、隔离或发布需求时再拆分。

### 8.2 逻辑组件

```text
React Web
    ↓ OIDC
API Gateway / Ingress
    ↓
FinOps.Api
    ├── Identity / Tenant / RBAC
    ├── Resource Query
    ├── Cost Query
    ├── Compliance / Finding
    ├── Admin Control
    └── Audit Query

Scheduler / Orchestrator
    ↓
FinOps.Worker
    ├── Resource Inventory Jobs
    ├── Cost Ingestion Jobs
    ├── Compliance Jobs
    ├── Anomaly Jobs
    ├── Event Consumers
    └── Report Jobs

Provider Adapters
    ├── Azure
    │   ├── Resource Graph
    │   ├── Cost Management
    │   ├── Policy Insights
    │   └── Monitor
    └── AWS
        ├── Resource Explorer / Tagging
        ├── Cost Explorer / CUR
        ├── Config
        └── CloudWatch

PostgreSQL
    ├── normalized operational data
    ├── job state
    ├── governance workflow
    ├── audit
    └── outbox / inbox

Object Storage
    ├── raw payload archive
    ├── reports
    └── large exports

Azure Service Bus
    ├── governance events
    ├── notifications
    └── dead-letter

Observability
    ├── OpenTelemetry
    ├── metrics
    ├── logs
    ├── traces
    └── SLO dashboards
```

### 8.3 代码边界

```text
Api / Worker / Migration Host
        ↓
Application
        ↓
Domain

Infrastructure ──实现──> Application Ports
```

约束：

- Domain 不引用 Application、Infrastructure、EF Core 或云 SDK；
- Application 不引用 Infrastructure、ASP.NET、EF Core 或云 SDK；
- API 只负责 HTTP、认证授权、输入输出和组合；
- Worker 只负责执行控制和任务生命周期；
- Infrastructure 实现数据库、队列、云 SDK、对象存储和通知适配；
- migration 使用独立宿主或发布步骤；
- 不创建承担所有能力的巨型 `ICloudProviderAdapter`；
- 资源、成本、合规、指标和事件采用小型能力接口。

## 9. 多租户和数据隔离

生产目标采用显式 tenant 模型。

所有核心数据至少包含：

```text
tenant_id
provider
account_id
```

关键要求：

- tenant 从认证上下文获得，不能信任客户端任意传入；
- 所有查询和写入必须带 tenant 过滤；
- 唯一索引包含 tenant 边界；
- 后台任务携带 tenant 上下文；
- cache key、队列消息、对象存储路径和审计均包含 tenant；
- 平台管理员跨租户访问需要单独权限和审计；
- 使用自动化架构测试和集成测试防止漏 tenant 条件；
- 评估 PostgreSQL Row Level Security 作为纵深防御；
- 大型或强隔离客户可演进为独立数据库或独立部署。

## 10. 身份、授权和审计

### 10.1 用户身份

- 首选 OIDC/OAuth 2.0；
- Azure 环境优先接入 Microsoft Entra ID；
- API 使用 bearer token；
- 前端使用授权码 + PKCE；
- 不自行存储用户密码；
- service-to-service 使用 workload identity。

### 10.2 RBAC

权限按 action 和 scope 表达：

```text
cost.read
resource.read
finding.read
finding.manage
sync.trigger
rule.manage
provider.manage
audit.read
platform.operate
remediation.approve
remediation.execute
```

scope 至少支持 tenant、provider account 和 resource group。

### 10.3 审计

审计记录至少包含：

- actor；
- tenant；
- action；
- target；
- request/correlation ID；
- before/after 摘要；
- result；
- timestamp；
- source IP 或调用身份；
- 审批和豁免依据。

审计日志追加写，业务用户不能修改。

## 11. 数据模型原则

### 11.1 分层存储

生产数据建议分为：

1. **Raw**：云 API 原始或接近原始的数据，带 source、schema version 和 hash；
2. **Normalized**：统一资源、成本、合规和指标模型；
3. **Derived**：归因、汇总、异常、建议和 Dashboard projection；
4. **Operational**：任务、事件、审批、审计和通知状态。

### 11.2 数据 lineage

每条关键数据应能追溯：

- 来源 Provider；
- 来源账号；
- API 或数据集；
- source timestamp；
- ingestion timestamp；
- job run；
- parser/schema version；
- raw payload reference；
- normalization version。

### 11.3 成本语义

- 使用 decimal，不使用 float/double 保存货币；
- 原始币种必须保留；
- 统一币种转换必须记录汇率、日期和来源；
- cost type、charge type、pricing model 和 billing period 显式建模；
- 支持云厂商修订历史数据；
- 聚合必须按币种和成本语义隔离；
- 大范围查询使用预聚合或物化策略，不能每次扫描全部明细。

### 11.4 资源生命周期

资源至少包含：

- first_seen_at；
- last_seen_at；
- lifecycle_status；
- source_updated_at；
- inventory_run_id；
- normalized identity；
- optional deleted_at。

只有一次完整成功扫描后，才能把本次未发现资源标记为 inactive。

### 11.5 Finding 生命周期

finding 至少支持：

```text
Open
Acknowledged
InProgress
Resolved
Suppressed
Waived
Expired
```

finding 使用稳定 fingerprint 去重，并保留规则版本、证据、Owner、评论、豁免
到期和状态历史。

## 12. Provider 接入标准

每个 Provider 能力都必须满足统一生产要求：

- connection validation；
- capability discovery；
- pagination；
- throttling；
- retry with jitter；
- cancellation；
- timeout；
- partial failure；
- rate-limit telemetry；
- data freshness；
- permission diagnostics；
- contract tests；
- sandbox/staging validation；
- least-privilege permission document；
- provider-specific runbook。

Azure 和 AWS 可以有不同实现，但输出必须符合统一 Application 契约。

## 13. ETL 和任务平台

任务不能只依赖手工 API 触发。

生产任务平台至少提供：

- schedule；
- manual trigger；
- tenant/provider/account scope；
- job definition 和 version；
- lease / distributed lock；
- idempotency key；
- checkpoint / continuation token；
- retry policy；
- timeout；
- cancellation；
- heartbeat；
- progress；
- records read/written/skipped/failed；
- failure category；
- rerun 和 backfill；
- dead-letter 或 quarantine；
- execution history；
- operator notes。

任务状态至少区分：

```text
Queued
Running
Succeeded
PartiallySucceeded
Failed
Cancelled
TimedOut
Skipped
```

## 14. API 标准

- `/api/v1` 或等价版本策略；
- OpenAPI；
- OIDC authentication；
- policy-based authorization；
- Problem Details；
- request/correlation ID；
- pagination；
- filter/sort 白名单；
- 参数上限；
- rate limiting；
- idempotency key；
- optimistic concurrency；
- consistent error codes；
- UTC 时间和明确日期语义；
- backward compatibility；
- deprecation policy；
- audit for privileged operations；
- API contract tests。

管理接口不能以无认证的 `/api/admin` 形式暴露到生产。

## 15. 前端标准

- React + TypeScript；
- OIDC 登录；
- 权限驱动导航和操作；
- 类型安全 API client；
- loading、empty、partial 和 error 状态；
- 大数据分页和虚拟化；
- 时间、时区、币种和单位一致；
- 图表可访问性；
- 键盘操作和基础 WCAG 要求；
- 敏感字段脱敏；
- 不把 secret 放入浏览器；
- 前端错误与后端 correlation ID 关联；
- 单元、组件和关键流程 E2E 测试。

## 16. 可观测性与 SLO

### 16.1 初始 SLI

- API availability；
- API p50/p95/p99 latency；
- API error rate；
- ETL success rate；
- ETL duration；
- resource freshness；
- cost freshness；
- provider throttling rate；
- queue backlog age；
- dead-letter count；
- database saturation；
- notification success rate；
- data quality failure rate。

### 16.2 初始 SLO 目标

初始目标需要在负载测试和真实运行后校准：

- 用户查询 API 月可用性不低于 99.9%；
- 读取 API p95 在目标数据规模下不高于 1 秒；
- 资源清单在正常 Provider 条件下 95% 于 60 分钟内更新；
- 成本数据在云厂商数据可用后 95% 于 2 小时内入库；
- 调度 ETL 月成功率不低于 99%；
- 高优先级事件在入队后 95% 于 5 分钟内处理；
- 生产死信和数据新鲜度违约必须告警。

这些是服务目标，不是对云厂商数据发布时间的承诺。

## 17. 部署和环境

至少包含：

- local；
- development；
- staging；
- production。

生产环境要求：

- 容器化；
- 非 root；
- read-only filesystem，除必要临时目录；
- health/readiness/startup probes；
- CPU/memory requests 和 limits；
- 自动扩缩容依据；
- 多实例 API；
- Worker 按任务类型扩缩；
- 托管 PostgreSQL 高可用；
- 托管 Service Bus；
- 私网或严格网络边界；
- TLS；
- secret store；
- remote Terraform state 和 locking；
- 环境隔离；
- immutable artifact；
- rolling、blue/green 或 canary 发布策略；
- 独立 migration job；
- 自动回滚或明确人工回滚流程。

## 18. CI/CD 和供应链

每个 Pull Request 至少执行：

- formatting；
- build；
- unit tests；
- architecture tests；
- integration tests；
- API contract validation；
- migration validation；
- PowerShell parsing；
- Terraform fmt/validate；
- secret scanning；
- dependency vulnerability scanning；
- SAST；
- IaC scanning；
- container build 和 image scanning；
- license policy；
- Markdown link check。

发布产物要求：

- 固定版本；
- SBOM；
- provenance；
- image digest；
- changelog；
- migration plan；
- rollback plan；
- staging evidence；
- production approval。

## 19. 测试策略

### 19.1 测试层级

- Domain unit tests；
- Application orchestration tests；
- architecture tests；
- repository integration tests with real PostgreSQL；
- Azure/AWS parser and contract tests；
- API integration tests；
- queue integration tests；
- frontend unit/component tests；
- end-to-end tests；
- migration tests；
- load tests；
- resilience tests；
- security tests；
- backup/restore tests；
- disaster recovery exercises。

### 19.2 测试数据

- 测试和 sample 数据必须带明确标记；
- production 查询默认排除 sample；
- 不从生产导出敏感数据用于测试；
- 使用合成数据和受控测试账号；
- 云端 E2E 使用独立资源范围和预算；
- 测试结束自动清理，清理失败立即告警。

## 20. Terraform 和基础设施治理

- Azure 和 AWS 分环境 state；
- remote backend；
- state locking；
- encryption；
- 最小权限；
- modules；
- version pinning；
- plan review；
- policy checks；
- drift detection；
- tagging；
- budget and cost guardrails；
- deletion protection for stateful production resources；
- backup before destructive changes；
- break-glass process；
- import 和 brownfield 策略；
- destroy 只允许非生产临时环境自动执行。

生产数据库、审计和关键对象存储不能沿用测试脚本的自动 destroy 模式。

## 21. 备份、恢复和灾难恢复

至少定义：

- RPO；
- RTO；
- PostgreSQL PITR；
- 备份保留；
- 对象存储版本和生命周期；
- Terraform state 备份；
- Service Bus backlog 和死信恢复；
- 关键配置恢复；
- 跨区域策略；
- 恢复顺序；
- 恢复验证；
- 定期演练和证据。

初始目标建议：

```text
RPO：15 分钟
RTO：4 小时
```

目标必须通过恢复演练证明，不能只写在文档中。

## 22. 数据保留和隐私

- 明确资源、成本、指标、finding、事件和审计保留期；
- 标签可能包含人员、项目和敏感业务信息，必须进行数据分类；
- 日志禁止记录 token、secret、完整 connection string 和敏感 payload；
- 导出数据受权限和审计控制；
- 删除租户时执行可证明的数据清理或合法保留；
- 生产数据访问和运维查询进入审计；
- 遵循适用的数据驻留和合规要求。

## 23. 运行手册

至少为以下场景建立 runbook：

- Azure/AWS credential 失效；
- 权限不足；
- Provider API 限流；
- 成本数据延迟或修订；
- Resource Graph/Explorer 部分失败；
- ETL 长时间运行；
- 分布式锁未释放；
- PostgreSQL 连接耗尽；
- migration 失败；
- Service Bus backlog；
- dead-letter 增长；
- 通知失败；
- 数据新鲜度违约；
- 错误规则大规模产生 finding；
- 发布回滚；
- 备份恢复；
- 云端测试资源清理失败；
- 安全事件和凭据轮换。

## 24. 工程决策原则

1. 先保证数据语义，再增加 Dashboard；
2. 先保证 tenant、安全和审计，再开放管理操作；
3. 先保证幂等、失败和恢复，再增加调度频率；
4. 先保证 Azure 生产质量，再扩展 AWS；
5. Provider 差异放在 Infrastructure，不污染核心模型；
6. 使用小接口和模块，不创建万能 Adapter；
7. 生产路径不使用隐式 sample fallback；
8. 自动整改默认关闭，先建议、审批、dry-run；
9. 每个外部依赖都有超时、重试、指标和 runbook；
10. 每个数据库变更都有兼容、迁移和回滚方案；
11. 每个上线能力都有测试、文档、遥测和运维入口；
12. 不以赶工为理由跳过安全、恢复和可观测性。

## 25. 明确禁止的生产捷径

- 无认证管理 API；
- 在代码或仓库保存真实 secret；
- 使用本地开发密码部署生产；
- API/Worker 多实例启动时自动争抢 migration；
- 用 sample 数据掩盖生产 Provider 故障；
- 把 Policy-style 模拟写成真实 Azure Policy/AWS Config；
- 跨币种直接汇总；
- 没有 tenant 条件的查询和唯一索引；
- 无分页的大结果接口；
- 无租约的并发 ETL；
- 只写日志、不记录失败状态；
- 无审计的豁免和整改；
- 自动删除生产资源；
- 未验证恢复能力却声称具备灾难恢复；
- 只通过 happy path 测试就宣布生产就绪。

## 26. 最终成功标准

项目最终成功不是“功能列表完成”，而是能够提供以下证据：

- 一套真实 Azure 和 AWS staging 环境；
- 受控 onboarding 的多租户、多账号连接；
- 资源、成本、合规和指标数据可追溯；
- API 和前端通过认证授权；
- ETL 有调度、锁、重试、断点和失败恢复；
- finding 有完整生命周期、证据和审计；
- 告警通过可靠事件管道处理；
- CI/CD 可以重复发布和回滚；
- SLO、Dashboard、告警和 runbook 可用；
- 数据库备份和灾难恢复经过演练；
- 安全扫描和威胁模型没有未接受的高危问题；
- 负载、故障和恢复测试达到目标；
- 文档与运行事实一致；
- 平台在连续运行中能够被维护，而不是只能演示一次。
