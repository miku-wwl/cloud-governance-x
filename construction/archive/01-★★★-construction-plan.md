# Cloud Governance X 生产级阶段化建设计划

> 历史规划说明，2026-06-29：
>
> 本文件保留原始 phase 词汇和 gate 模型。当前状态见
> `docs/current-state.md`，里程碑规划见 `docs/roadmap.md`，当前执行入口见
> `construction/current-playbook.md`。

## 1. 计划原则

本计划不规定固定天数，也不以“按时完成全部功能”为目标。

建设节奏由以下因素决定：

- 前置依赖是否完成；
- 设计是否通过评审；
- 自动化测试和真实验收是否通过；
- 安全、可靠性、可观测性和回滚能力是否达到门禁；
- 风险是否被关闭或明确接受；
- 文档和运行证据是否完整。

任何阶段未通过出关门禁，不进入依赖它的后续阶段。

## 2. 计划与项目纲领的关系

仓库根目录的 `outline.md` 定义生产级目标、边界和长期原则。

本文件负责：

- 划分阶段；
- 定义阶段依赖；
- 列出工作包；
- 规定交付物；
- 规定自动化和人工验收；
- 规定安全、运维和回滚要求；
- 记录当前 Day 1～7 基线如何演进为生产系统。

## 3. 当前基线

原 Day 1～7 已完成：

- .NET 10 Clean Architecture 基础项目；
- API 和 Worker；
- PostgreSQL 本地环境和健康检查；
- Azure Terraform 临时资源生命周期；
- Azure Subscription 读取；
- Azure Resource Graph 资源同步；
- Azure Cost Management 成本同步；
- 资源、成本和 ETL 执行历史表；
- 管理 API 和一次性 Worker；
- 19 个自动化测试；
- 6 个真实 Azure 端到端脚本。

基线价值：

- 证明核心技术链可以工作；
- 证明 Azure 身份、资源、成本、数据库和应用可以形成闭环；
- 提供后续重构的可执行回归证据。

基线限制：

- 仅适合本地开发和受控验证；
- 未实现认证授权、多租户、生产调度、CI/CD、可观测性和灾难恢复；
- sample fallback、自动 migration 和管理 API 不符合生产边界；
- 当前数据模型尚未满足完整成本和资源生命周期语义。

## 4. 阶段依赖图

```text
阶段 0：基线审计
    ↓
阶段 1：架构治理与工程门禁
    ↓
阶段 2：身份、多租户、RBAC、审计
    ↓
阶段 3：数据模型与迁移生产化
    ↓
阶段 4：任务调度与可靠 ETL 平台
    ↓
阶段 5：Azure Provider 生产化
    ↓
阶段 6：FinOps 成本语义与归因
    ↓
阶段 7：合规、Finding 与整改工作流
    ↓
阶段 8：生产 API
    ↓
阶段 9：生产前端
    ↓
阶段 10：事件、告警与通知
    ↓
阶段 11：可观测性、SLO 与运行手册
    ↓
阶段 12：容器、平台与 CI/CD
    ↓
阶段 13：AWS Provider 与双云统一
    ↓
阶段 14：安全验证和供应链治理
    ↓
阶段 15：性能、容量、韧性和灾难恢复
    ↓
阶段 16：生产上线门禁
    ↓
阶段 17：持续运营和治理扩展
```

部分工作可以并行，但不能绕过依赖门禁。例如：

- 前端设计可与 API 设计并行，但不能在 API 契约未稳定前固化；
- AWS 账号预检可以提前，但不能在 Provider 契约和 tenant 模型未稳定前正式接入；
- CI 基础可以提前建设，但生产发布流程依赖环境和部署架构；
- 可观测性规范应提前设计，并在每个阶段逐步落地。

### 4.1 发布列车

本计划不采用“一次完成全部能力再上线”的方式。

### Release A：生产平台基础

范围：

- 阶段 0～4；
- 阶段 11～12 中与基础平台有关的能力。

结果：

- 有身份、tenant、RBAC、审计；
- 有生产数据模型和 migration；
- 有可靠 ETL；
- 有 CI/CD、遥测、备份和恢复基础；
- 可以承载受控内部数据，但还不对外宣称完整 Azure FinOps 产品。

### Release B：Azure Production

范围：

- 阶段 5～10；
- Azure 对应的安全、性能和运行验证。

结果：

- Azure 资源、成本、Policy、合规、finding、异常、事件和 Dashboard；
- 面向内部或首批 Azure tenant 生产使用；
- AWS 尚未完成时，产品明确标记为 Azure 能力，不宣传多云完成。

### Release C：Multi-cloud Production

范围：

- 阶段 13；
- AWS 对应的安全、性能和运行验证；
- 双云统一查询、规则和事件。

结果：

- Azure 和 AWS 均达到 Provider 生产标准；
- 多云表述可以正式启用；
- Provider 特有语义仍然可追溯。

### Release D：Governance Automation

范围：

- 更广泛整改；
- 深度闲置治理；
- 高级通知；
- 自动化动作；
- 高级预测和单位经济。

结果：

- 从只读治理与建议逐步扩展到受控执行；
- 每类 destructive action 单独通过安全、审批和回滚门禁。

### 4.2 阶段状态

每个阶段使用统一状态：

```text
NotStarted
Design
Implementation
Validation
ReadyForGate
Complete
Blocked
```

`Complete` 必须包含证据包：

- 设计或 ADR；
- 代码和 migration；
- 自动化测试结果；
- staging E2E；
- 安全检查；
- telemetry 截图或查询；
- runbook；
- 发布和回滚记录；
- 已知限制；
- 风险接受记录。

没有证据包的“代码写完”只能处于 `Implementation` 或 `Validation`。

## 5. 全局 Definition of Done

任何业务能力只有同时满足以下条件才算完成。

### 5.1 设计

- 需求和边界清楚；
- 数据来源和语义清楚；
- 威胁、失败和并发场景被识别；
- 必要时创建 ADR；
- API、数据和事件契约经过评审；
- 兼容性和迁移策略明确。

### 5.2 实现

- 代码遵守分层和模块边界；
- 无硬编码 secret；
- 输入有校验和上限；
- 外部调用有 timeout、cancellation、retry 和 telemetry；
- 写操作具备幂等性；
- tenant 边界不可绕过；
- 权限检查位于可信边界；
- 错误可分类且不泄漏敏感信息。

### 5.3 测试

- Domain 单元测试；
- Application 行为测试；
- Infrastructure 集成或 contract 测试；
- API/Worker 集成测试；
- 失败、超时、重试、重复和取消路径；
- tenant 隔离测试；
- migration 测试；
- 对应 staging E2E；
- 必要的负载、安全和恢复测试。

### 5.4 运行

- 日志、指标和 trace；
- Dashboard 和告警；
- runbook；
- 配置说明；
- 发布和回滚步骤；
- 数据迁移和恢复说明；
- 容量和成本影响；
- Owner 和维护责任。

### 5.5 文档

- README 或专题文档更新；
- API/OpenAPI 更新；
- 数据模型更新；
- 运维手册更新；
- ADR 更新；
- “已完成、受限能力、未来计划”表述准确。

## 6. 环境策略

### 6.1 Local

用途：

- 快速开发；
- 单元和集成测试；
- Docker PostgreSQL；
- 本地 Azure CLI / AWS profile 验证。

允许：

- 开发密码；
- 可控 sample 数据；
- 手工启动；
- 自动清理临时资源。

禁止：

- 复用生产 secret；
- 连接生产数据库写入；
- 将 local 成功视为 staging 成功。

### 6.2 Development

用途：

- 共享集成；
- 自动部署；
- Provider sandbox；
- 多人联调。

要求：

- 独立 tenant/account；
- 受控 secret store；
- CI 自动部署；
- 基础监控；
- 可重建。

### 6.3 Staging

用途：

- 生产同构验证；
- 性能、安全、迁移和恢复测试；
- 发布候选验收。

要求：

- 与生产相同部署方式；
- 独立数据和云账号；
- 不使用 production secret；
- 真实 Azure/AWS API；
- 生产级认证授权；
- SLO 和告警启用；
- 发布和回滚演练。

### 6.4 Production

要求：

- 变更审批；
- immutable artifact；
- 高可用；
- 备份和 PITR；
- 最小权限；
- 私网或严格入口控制；
- 审计；
- on-call 和 runbook；
- SLO；
- 容量和预算治理。

## 7. 阶段 0：基线审计与风险登记

### 目标

冻结当前 Day 1～7 行为，形成生产化改造前的可验证基线。

### 工作包

1. 重新运行自动化测试和 Day 1～7 端到端闭环；
2. 记录当前 API、Worker、数据库和 Terraform 行为；
3. 建立 architecture diagram 和 data flow；
4. 建立风险登记册；
5. 标记当前非生产行为；
6. 建立依赖、包和许可证清单；
7. 建立数据分类初稿；
8. 建立生产化 ADR 列表。

### 必须登记的风险

- 无认证管理接口；
- 无 tenant_id；
- 自动 migration；
- 无 ETL 调度和互斥；
- sample fallback；
- 资源删除语义；
- 成本粒度；
- 无 outbox/inbox；
- 无可观测性；
- 无备份恢复；
- 无 staging；
- 无 CI/CD；
- Terraform 本地 state；
- 云端 E2E 费用和清理风险。

### 交付物

- 基线验收报告；
- 风险登记册；
- 当前架构图；
- 数据流图；
- ADR backlog；
- 生产化差距清单。

### 出关门禁

- 当前所有行为有测试或手工证据；
- 风险均有 Owner、严重度和处理阶段；
- 没有未知的 Azure 临时资源或测试数据库遗留；
- Git 仓库干净且无 secret。

## 8. 阶段 1：架构治理与工程门禁

### 目标

建立能够长期约束代码质量和架构边界的工程系统。

### 工作包

#### 8.1 模块和目录

- 明确 Identity、Tenancy、Providers、Inventory、Costs、Compliance、
  Findings、Events、Audit、Operations 模块；
- 拆分 `Program.cs` endpoint registration；
- 拆分 `DependencyInjection.cs` 注册；
- Worker 使用 Job Handler 注册表，替代不断增长的 if/else；
- 创建独立 migration host 或 migration command；
- 保持模块化单体，不提前拆微服务。

#### 8.2 架构测试

自动验证：

- Domain 无外部依赖；
- Application 不依赖 Infrastructure；
- Azure/AWS SDK 只存在于 Infrastructure；
- API 不直接引用云 SDK；
- tenant-aware 模块不能调用非 tenant-aware Repository；
- 模块间引用符合约束。

#### 8.3 编码和版本治理

- `.editorconfig`；
- analyzers；
- nullable；
- warnings as errors；
- dependency version policy；
- conventional commits 或明确提交规范；
- ADR 模板；
- PR 模板；
- CODEOWNERS 或责任边界；
- release/version 策略。

#### 8.4 静态验证入口

建立单一脚本或 CI job：

- JSON/YAML/XML/PowerShell 解析；
- format；
- build；
- tests；
- Terraform fmt/validate；
- Markdown link；
- secret scan；
- generated artifact 检查。

### 交付物

- 模块边界；
- endpoint 和 DI 拆分；
- architecture tests；
- 工程规范；
- CI 静态门禁初版；
- ADR 机制。

### 出关门禁

- 架构违规会自动使 CI 失败；
- 任何项目均可从干净环境 restore/build/test；
- 生产代码没有重新生成的模板垃圾；
- migration 不再依赖 API/Worker 多实例启动争抢；
- review 地图与新结构一致。

## 9. 阶段 2：身份、多租户、RBAC 与审计

### 目标

先建立可信边界，再继续扩展管理和治理能力。

### 工作包

#### 9.1 Tenant 模型

新增：

- organizations；
- tenants；
- cloud_accounts；
- provider_connections；
- account scopes；
- tenant membership。

所有核心表规划加入 tenant_id。

#### 9.2 身份认证

- Entra ID OIDC；
- API bearer token；
- React PKCE；
- service-to-service workload identity；
- local 开发身份；
- token validation 和 key rotation。

#### 9.3 授权

- policy-based authorization；
- role + permission；
- tenant/account/resource scope；
- admin/read/operator/auditor 分离；
- 后台任务服务身份权限。

#### 9.4 审计

- 追加式审计模型；
- privileged API audit；
- provider connection audit；
- rule、finding、waiver 和 remediation audit；
- correlation ID；
- before/after summary；
- retention。

#### 9.5 Tenant 隔离

- Repository 默认 tenant filter；
- composite unique index；
- cache/queue/storage path tenant 化；
- 防 IDOR；
- 跨 tenant 平台管理员路径；
- RLS 可行性验证。

### 安全测试

- 无 token；
- token 过期；
- issuer/audience 错误；
- 角色不足；
- 跨 tenant ID 猜测；
- 后台任务 tenant 丢失；
- audit 不可修改；
- 管理接口权限；
- export 权限。

### 交付物

- tenant-aware schema；
- OIDC；
- RBAC；
- audit；
- tenant 隔离测试；
- 身份和权限文档。

### 出关门禁

- 生产 API 不存在匿名管理操作；
- 自动化测试证明 tenant A 无法读取/修改 tenant B；
- 每个高权限动作有审计；
- 云 Provider 凭据不通过用户请求直接传递；
- secret 不存数据库明文。

## 10. 阶段 3：数据模型与迁移生产化

### 目标

建立可以长期承载资源、成本、合规和运维数据的数据平台。

### 工作包

#### 10.1 数据分层

- Raw ingestion metadata；
- normalized tables；
- derived projections；
- operational state；
- raw payload 对象存储或受控 raw 表；
- schema/parser version；
- lineage。

#### 10.2 资源模型

- tenant_id；
- provider/account；
- normalized identity；
- lifecycle_status；
- first/last/source seen；
- inventory run；
- relationships；
- deleted/inactive；
- tags；
- source hash。

#### 10.3 成本模型

- cost type；
- charge type；
- billing period；
- currency；
- original amount；
- optional converted amount；
- exchange rate reference；
- service/account/region/resource/tag dimensions；
- adjustment/version；
- source lineage。

#### 10.4 ETL 模型

- job definition；
- job run；
- attempt；
- checkpoint；
- heartbeat；
- progress counts；
- error category；
- partial success；
- trigger source；
- idempotency key。

#### 10.5 Finding、事件和审计模型

- rule；
- rule version；
- finding fingerprint；
- evidence；
- state history；
- waiver；
- assignment；
- event；
- outbox；
- inbox；
- notification；
- audit。

#### 10.6 Migration 策略

- expand/contract；
- backward compatible migration；
- backfill job；
- online index；
- large table migration；
- rollback/roll-forward；
- staging rehearsal；
- schema compatibility test；
- migration lock。

### 数据质量

- required fields；
- duplicate detection；
- stale data；
- impossible dates；
- invalid currency；
- malformed resource ID；
- tenant/account mismatch；
- cost spikes caused by ingestion error；
- raw/normalized count reconciliation。

### 交付物

- 生产数据模型；
- migrations；
- backfill；
- migration host；
- data dictionary；
- lineage 说明；
- retention 方案；
- 数据质量规则。

### 出关门禁

- 所有核心表 tenant-aware；
- migration 在 staging 数据量上验证；
- rollback 或 roll-forward 路径可执行；
- 资源删除语义通过 E2E；
- 多币种和成本修订测试通过；
- raw 到 normalized 可追溯；
- 备份后 migration 和恢复演练通过。

## 11. 阶段 4：任务调度与可靠 ETL 平台

### 目标

把一次性 Worker 和手工 POST 触发升级为可调度、可恢复、可扩展的任务平台。

### 工作包

#### 11.1 Job 定义

- job type；
- tenant；
- provider；
- account；
- schedule；
- parameters；
- enabled；
- version；
- concurrency policy。

#### 11.2 调度

- cron 或受控调度器；
- due job scan；
- enqueue；
- manual trigger；
- backfill；
- pause/resume；
- timezone 规则；
- duplicate prevention。

#### 11.3 分布式执行

- lease；
- heartbeat；
- lease expiry；
- distributed lock；
- idempotency key；
- attempt；
- timeout；
- cancellation；
- checkpoint；
- continuation token；
- partition。

#### 11.4 失败处理

- transient/permanent/data/security 分类；
- exponential backoff + jitter；
- max attempts；
- partial success；
- quarantine；
- dead-letter；
- operator retry；
- replay audit。

#### 11.5 可观测性

- queue delay；
- start delay；
- duration；
- records read/written/skipped/failed；
- retries；
- throttling；
- freshness；
- error category。

### 测试

- 双 Worker 争抢同一任务；
- Worker 在处理中崩溃；
- lease 到期；
- 重复消息；
- Provider 429/5xx；
- DB 暂时不可用；
- checkpoint 恢复；
- cancel；
- timeout；
- backfill；
- partial failure。

### 交付物

- scheduler；
- job queue；
- reliable Worker；
- operator API；
- job UI/operations view；
- runbook。

### 出关门禁

- 同一幂等键不会重复产生业务记录；
- Worker 崩溃后任务可恢复；
- 重试不会绕过最大次数；
- 失败可分类、可查询、可重放；
- 任务新鲜度和 backlog 可告警；
- 管理触发经过授权和审计。

## 12. 阶段 5：Azure Provider 生产化

### 目标

将当前 Azure POC 实现升级为生产 Provider。

### 工作包

#### 12.1 Azure Connection

- Managed Identity / Workload Identity；
- service principal 仅作受控兼容；
- subscription onboarding；
- permission preflight；
- capability discovery；
- connection health；
- credential rotation；
- least-privilege role 文档。

#### 12.2 Resource Graph

- 多 subscription；
- 分页；
- throttling；
- retry；
- partial subscription failure；
- resource relationship；
- active/inactive；
- full scan consistency；
- data freshness；
- query version；
- unsupported resource diagnostics。

#### 12.3 Cost Management

- 多 scope；
- 日期和账期；
- cost type；
- grouping；
- pagination；
- dynamic columns；
- currency；
- refunds/credits；
- late arriving data；
- rerun/backfill；
- permission and enrollment differences；
- API version policy。

#### 12.4 Azure Policy

- Policy Insights；
- assignment、definition、initiative；
- compliance state；
- exemption；
- evidence；
- scope；
- evaluation timestamp；
- 与平台自有规则分离。

#### 12.5 Azure Monitor

- 指标查询；
- observation window；
- batch；
- throttling；
- missing metrics；
- idle rule input。

### 生产 sample 策略

- `ForceSampleData` 只能存在于测试环境；
- production 配置 schema 禁止启用；
- Provider 无数据返回 empty + metadata；
- Provider 故障使任务失败或部分失败；
- sample 数据进入专用测试 tenant；
- UI 明确显示测试来源。

### 测试

- contract tests；
- Azure sandbox E2E；
- 多 subscription；
- permission denied；
- throttling；
- empty cost；
- delayed cost；
- malformed dynamic response；
- partial failure；
- credential expiration。

### 交付物

- Azure production adapter；
- 权限模板；
- onboarding；
- runbook；
- Provider SLI；
- staging E2E。

### 出关门禁

- 不使用开发机 Azure CLI 作为生产身份；
- Azure 故障不会被 sample 掩盖；
- 多 subscription 隔离正确；
- 限流和重试有指标；
- 资源和成本 freshness 达标；
- Policy 结果来源清晰。

## 13. 阶段 6：FinOps 成本语义、归因、预算和异常

### 目标

从“能读取成本”升级为可用于真实管理决策的 FinOps 能力。

### 工作包

#### 13.1 成本语义

- actual / amortized；
- charge type；
- pricing model；
- credit/refund/tax；
- billing period；
- cost revision；
- 原币种；
- 汇率服务；
- currency conversion audit。

#### 13.2 归因

- direct dimension；
- resource relationship；
- tag mapping；
- cost-center；
- environment；
- owner；
- business unit；
- shared cost allocation；
- unallocated；
- confidence；
- rule version。

#### 13.3 预算

- tenant/account/scope budget；
- monthly/quarterly；
- threshold；
- forecast；
- notification；
- rollover policy；
- owner；
- audit。

#### 13.4 趋势和单位经济

- daily/monthly；
- period comparison；
- service trend；
- unit cost extension；
- resource count correlation；
- anomaly context。

#### 13.5 异常检测

- 3σ baseline；
- minimum history；
- seasonality；
- sparse data；
- configurable window；
- backtest；
- algorithm version；
- feedback；
- suppression；
- severity；
- false-positive metrics。

### 数据正确性测试

- 多币种；
- 汇率缺失；
- 负成本；
- refund；
- late correction；
- shared allocation；
- tag change；
- unallocated；
- period boundary；
- daylight saving 对日期无影响；
- anomaly backtest。

### 交付物

- 成本语义模型；
- 归因引擎；
- 预算；
- 异常检测；
- FinOps API；
- 数据质量 Dashboard；
- 规则和算法文档。

### 出关门禁

- 不跨币种错误汇总；
- attribution 显示依据和置信度；
- 不能归属的成本单独呈现；
- 异常算法经过 backtest；
- 生产决策指标可追溯到 source；
- 成本修订可安全重算。

## 14. 阶段 7：合规、Finding 与整改工作流

### 目标

建立真实、可审计、可操作的治理闭环。

### 工作包

#### 14.1 平台规则引擎

- rule definition；
- version；
- parameters；
- scope；
- severity；
- evaluation；
- dry-run；
- effective date；
- deprecation。

#### 14.2 Finding

- stable fingerprint；
- evidence；
- first/last seen；
- status；
- owner；
- assignment；
- comments；
- recurrence；
- source；
- rule version。

#### 14.3 豁免

- reason；
- approver；
- expiry；
- scope；
- evidence；
- renewal；
- automatic expiry；
- audit。

#### 14.4 整改

- recommendation；
- manual task；
- external ticket；
- dry-run；
- approval；
- execution；
- verification；
- rollback；
- status history。

#### 14.5 真实云策略

- Azure Policy；
- AWS Config；
- 平台规则；
- 三种来源清晰区分；
- 云端豁免与平台豁免映射。

### 首批规则

- required tags；
- public storage；
- encryption；
- backup；
- unattached disk；
- unassociated IP；
- expired exemption；
- stale owner；
- policy noncompliance。

### 安全边界

- 自动整改默认关闭；
- destructive remediation 双人审批；
- production scope allowlist；
- dry-run；
- execution identity 最小权限；
- before/after evidence；
- rollback；
- emergency stop。

### 交付物

- rule engine；
- finding workflow；
- waiver；
- remediation；
- reports；
- compliance API；
- audit；
- runbook。

### 出关门禁

- finding 去重和重开正确；
- 豁免到期自动恢复；
- 规则升级不破坏历史证据；
- 真实 Policy/Config 与平台规则不混淆；
- destructive action 无法绕过审批；
- 每次整改可追溯和回滚。

## 15. 阶段 8：生产 API

### 目标

提供稳定、安全、可扩展的外部和前端契约。

### 工作包

- `/api/v1`；
- OpenAPI；
- OIDC；
- 授权策略；
- Problem Details；
- correlation ID；
- pagination；
- filter/sort；
- query limits；
- rate limiting；
- idempotency key；
- optimistic concurrency；
- ETag 可行性；
- export job；
- consistent error code；
- deprecation；
- audit；
- health/readiness/startup；
- API metrics。

### API 分类

- query APIs；
- operator APIs；
- admin APIs；
- internal service APIs；
- webhook APIs；
- export APIs。

不同类别使用不同权限、限流和审计策略。

### 测试

- contract；
- auth；
- RBAC；
- tenant isolation；
- pagination；
- invalid filters；
- large query；
- rate limit；
- idempotency；
- concurrency；
- backward compatibility；
- error schema；
- sensitive data leakage。

### 出关门禁

- OpenAPI 与实现一致；
- 所有 endpoint 有 auth policy 或显式 anonymous 理由；
- 大结果必须分页；
- 管理写操作有审计；
- API contract tests 阻止破坏性变更；
- p95 达到阶段目标。

## 16. 阶段 9：生产前端

### 目标

提供可用于日常治理和运维的安全 Web 产品。

### 工作包

#### 16.1 工程

- React + TypeScript；
- router；
- state/query management；
- generated typed client；
- design system；
- lint/test/build；
- environment config；
- CSP。

#### 16.2 身份和权限

- OIDC PKCE；
- token lifecycle；
- role-aware navigation；
- permission-aware actions；
- tenant/account selector；
- unauthorized states。

#### 16.3 页面

- Overview；
- Cost；
- Resources；
- Compliance；
- Findings；
- Anomalies；
- Events；
- ETL Runs；
- Provider Connections；
- Audit；
- Platform Operations。

#### 16.4 体验

- loading；
- empty；
- stale；
- partial；
- retry；
- error + correlation ID；
- pagination；
- saved filters；
- timezone；
- currency；
- accessibility；
- responsive layout；
- large table virtualization。

#### 16.5 导出

- async export；
- access check；
- expiry；
- audit；
- large file object storage；
- no secret in URL。

### 测试

- unit；
- component；
- accessibility；
- role navigation；
- tenant switch；
- critical browser E2E；
- error states；
- large data；
- visual smoke；
- security headers。

### 出关门禁

- 前端不能绕过后端授权；
- 关键流程有浏览器 E2E；
- 所有错误可关联后端 trace；
- 大数据页面不一次加载全部记录；
- 权限不足和空数据体验明确；
- 基础可访问性通过。

## 17. 阶段 10：事件、告警与通知

### 目标

建立可靠事件驱动治理管道。

### 工作包

#### 17.1 Event contract

- event ID；
- type；
- version；
- tenant；
- provider；
- subject；
- severity；
- occurred_at；
- correlation/causation；
- payload；
- schema version。

#### 17.2 可靠发布

- transactional outbox；
- publisher；
- retry；
- duplicate-safe publish；
- publish telemetry。

#### 17.3 可靠消费

- inbox；
- idempotent handler；
- lock/lease；
- retry；
- dead-letter；
- poison message；
- replay；
- handler version。

#### 17.4 告警治理

- dedupe；
- aggregation；
- suppression；
- maintenance window；
- escalation；
- acknowledgement；
- resolution；
- notification policy。

#### 17.5 通知

- Email；
- Teams；
- Webhook；
- template；
- secret；
- rate limit；
- delivery result；
- unsubscribe/route policy。

### 测试

- duplicate event；
- out-of-order；
- handler crash；
- poison；
- dead-letter replay；
- provider unavailable；
- notification 429/5xx；
- template failure；
- tenant isolation；
- schema compatibility。

### 出关门禁

- 数据库提交后事件不会静默丢失；
- 重复事件不会重复执行治理动作；
- 死信可见、可诊断、可重放；
- 通知失败不影响核心 finding；
- backlog 和 dead-letter 有 SLO/告警。

## 18. 阶段 11：可观测性、SLO 和运行手册

### 目标

让平台的健康、性能、数据新鲜度和故障可以被持续管理。

### 工作包

#### 18.1 OpenTelemetry

- traces；
- metrics；
- structured logs；
- propagation；
- baggage 使用规范；
- redaction；
- sampling；
- exporter。

#### 18.2 服务指标

- request count/error/latency；
- DB pool；
- external API；
- retries；
- circuit breaker；
- thread pool；
- memory/GC；
- queue；
- job；
- data quality；
- freshness。

#### 18.3 SLO

- user query availability；
- latency；
- ETL success；
- freshness；
- event processing；
- dead-letter；
- provider connection health；
- error budget。

#### 18.4 Dashboard

- executive service health；
- API；
- Worker；
- database；
- queue；
- Provider；
- data freshness；
- tenant hotspots；
- release comparison。

#### 18.5 Alert

- symptom-based；
- actionable；
- severity；
- routing；
- dedupe；
- runbook link；
- maintenance；
- test alert。

#### 18.6 Runbook

覆盖 `outline.md` 中定义的关键故障场景。

### 出关门禁

- 关键请求可跨 API、Worker、DB、queue 关联；
- 告警都有 Owner 和 runbook；
- SLO 自动计算；
- 敏感信息不进入遥测；
- staging 故障演练能触发预期告警；
- on-call 可以仅凭 Dashboard 和 runbook 定位主要故障。

## 19. 阶段 12：容器、平台和 CI/CD

### 目标

建立可重复、可审计、可回滚的交付系统。

### 工作包

#### 19.1 Container

- multi-stage；
- non-root；
- minimal/distroless 评估；
- read-only filesystem；
- health probes；
- resource limits；
- graceful shutdown；
- SBOM；
- image scan；
- digest pinning。

#### 19.2 Kubernetes 或目标平台

- namespace/environment；
- deployment；
- service；
- ingress；
- TLS；
- network policy；
- workload identity；
- secret integration；
- HPA；
- PDB；
- anti-affinity；
- rollout；
- migration job；
- cron/scheduler。

#### 19.3 Terraform

- remote state；
- locking；
- modules；
- environment separation；
- drift；
- policy checks；
- protected production resources；
- plan artifact；
- apply approval。

#### 19.4 CI

- restore/build/test；
- architecture；
- integration；
- contract；
- migration；
- frontend；
- static security；
- dependency；
- secret；
- IaC；
- container；
- license；
- links；
- artifact publish。

#### 19.5 CD

- dev auto deploy；
- staging promotion；
- production approval；
- immutable artifact promotion；
- migration gate；
- smoke；
- canary/rolling；
- rollback；
- release notes；
- deployment audit。

### 出关门禁

- 同一 artifact 在 staging 验证后晋级 production；
- 生产部署不本地手工 build；
- migration 与应用兼容；
- 回滚演练成功；
- 容器无未接受高危漏洞；
- Terraform state 安全且可恢复；
- 发布过程有完整审计。

## 20. 阶段 13：AWS Provider 与双云统一

### 前置条件

- tenant/account 模型稳定；
- Provider 契约稳定；
- ETL 平台稳定；
- Azure production adapter 已验证；
- AWS 账号、Organizations、Cost Explorer/CUR 和权限完成预检。

### 工作包

#### 20.1 AWS Connection

- IAM Role；
- STS AssumeRole；
- external ID；
- region discovery；
- account onboarding；
- permission preflight；
- credential rotation；
- organization account discovery。

#### 20.2 Resource Inventory

- Resource Explorer；
- Resource Groups Tagging API；
- EC2；
- EBS；
- EIP；
- S3；
- ELB；
- RDS；
- 必要服务专用 API；
- global vs regional resource；
- pagination/throttling。

#### 20.3 Cost

- Cost Explorer；
- CUR 可行性和生产路径；
- account/service/region/tag；
- amortized/unblended；
- credits/refunds；
- late data；
- multi-currency；
- billing entity。

#### 20.4 Compliance

- AWS Config；
- rule/aggregator；
- compliance result；
- resource config；
- remediation；
- 与平台规则分离。

#### 20.5 Metrics

- CloudWatch；
- idle resource metrics；
- missing data；
- batch and throttling。

### 双云统一验证

- 同一 tenant 下 Azure + AWS；
- Provider filter；
- 统一 cost/resource/finding API；
- 统一规则；
- 不丢失 Provider 特有语义；
- 同一事件和通知管道；
- 独立故障域。

### 出关门禁

- 使用短期 IAM 凭据；
- account/region 隔离正确；
- AWS 数据不被 Azure 术语扭曲；
- Cost Explorer/CUR 语义准确；
- Config 结果来源清晰；
- 一个 Provider 故障不阻塞另一个 Provider；
- 双云 staging E2E 通过。

## 21. 阶段 14：安全验证与供应链治理

### 目标

通过系统化安全验证，而不是只依赖编码习惯。

### 工作包

- threat model；
- trust boundary；
- STRIDE 或等价方法；
- OWASP API；
- IDOR；
- SSRF；
- injection；
- auth bypass；
- tenant escape；
- mass assignment；
- export leakage；
- webhook verification；
- secret rotation；
- dependency scanning；
- SAST；
- DAST；
- container；
- IaC；
- SBOM；
- provenance；
- license；
- penetration test；
- incident response。

### 云权限审计

- Azure role；
- AWS policy；
- unused permission；
- break-glass；
- service identity；
- remediation identity；
- credential lifetime。

### 出关门禁

- 无未接受 Critical/High；
- tenant escape 测试通过；
- secret rotation 演练通过；
- audit 覆盖 privileged action；
- SBOM 和 provenance 可验证；
- 安全事件 runbook 演练；
- 风险接受有 Owner 和到期时间。

## 22. 阶段 15：性能、容量、韧性和灾难恢复

### 目标

证明系统在目标规模、依赖故障和灾难场景下仍可运行或恢复。

### 容量模型

定义：

- tenants；
- cloud accounts；
- resources；
- cost rows/day；
- findings；
- events/sec；
- concurrent users；
- exports；
- retention；
- database growth；
- queue growth。

### 性能测试

- API query；
- pagination；
- aggregation；
- export；
- ETL throughput；
- DB bulk upsert；
- queue consume；
- dashboard concurrency；
- cold/warm cache；
- tenant hotspot。

### 韧性测试

- DB failover；
- Service Bus unavailable；
- Provider 429；
- Provider 5xx；
- DNS/network timeout；
- Worker kill；
- API pod kill；
- duplicate messages；
- disk/storage failure；
- partial region failure；
- bad deployment；
- bad rule rollout。

### 灾难恢复

- PostgreSQL PITR；
- object storage restore；
- Terraform state restore；
- configuration restore；
- queue recovery；
- rebuild environment；
- regional failover；
- RPO/RTO measurement；
- restore evidence。

### 出关门禁

- 目标负载达到 SLO；
- 容量余量明确；
- autoscaling 行为验证；
- 关键故障不会造成 silent data loss；
- RPO/RTO 通过演练；
- 恢复后数据 reconciliation 通过；
- DR runbook 可由非开发者执行。

## 23. 阶段 16：生产上线门禁

### 目标

以证据决定是否上线。

### 产品门禁

- 核心用户流程完成；
- 权限模型确认；
- 数据语义确认；
- 已知限制公开；
- 支持和 Owner 明确。

### 工程门禁

- CI 全绿；
- staging release candidate 稳定；
- migration rehearsal；
- load/resilience/security 通过；
- rollback 通过；
- SBOM/provenance；
- 无高危漏洞。

### 数据门禁

- onboarding 验证；
- quality checks；
- lineage；
- retention；
- tenant isolation；
- backup；
- restore；
- production sample 禁用。

### 运行门禁

- SLO；
- Dashboard；
- alerts；
- runbooks；
- on-call；
- incident channel；
- vendor/provider escalation；
- capacity；
- budget。

### 安全门禁

- threat model；
- pentest；
- least privilege；
- secret rotation；
- audit；
- break-glass；
- incident response。

### 上线策略

1. 内部 tenant；
2. 小范围 canary tenant；
3. 限制账号和只读能力；
4. 观察错误预算；
5. 扩大 tenant；
6. 最后开放受控整改。

### Go / No-Go

任何以下情况默认 No-Go：

- tenant 隔离缺陷；
- 数据错误可能影响财务判断；
- migration 无恢复路径；
- secret 泄漏；
- Critical/High 未接受；
- backup 未验证；
- SLO 无监控；
- destructive action 可绕过审批；
- Provider 故障被 sample 掩盖；
- 无 on-call 或 runbook。

## 24. 阶段 17：持续运营和治理扩展

生产上线不是完成，而是进入长期运营。

### 运营循环

- SLO review；
- error budget；
- incident review；
- capacity review；
- cloud API/version review；
- dependency update；
- vulnerability remediation；
- cost review；
- rule effectiveness；
- anomaly precision；
- finding aging；
- tenant feedback；
- data quality；
- DR exercise；
- access review。

### 后续能力

- GCP Provider；
- Kubernetes 深度治理；
- Unit Economics；
- commitment/RI/Savings Plan；
- chargeback/showback；
- business KPI；
- advanced forecasting；
- policy-as-code；
- automated remediation expansion；
- ticketing integration；
- Teams/Email/PagerDuty；
- CMDB integration；
- data warehouse/lake；
- machine learning anomaly models。

所有扩展继续遵守全局 Definition of Done，不因系统已上线而降低门禁。

## 25. 每阶段 Review 顺序

每个阶段按照以下顺序审查：

1. 业务目标和边界；
2. tenant、安全和权限；
3. 数据语义和 migration；
4. API/事件契约；
5. Application 和 Domain；
6. Provider/数据库/队列实现；
7. 失败、并发和恢复；
8. 自动化测试；
9. staging E2E；
10. telemetry、SLO 和 runbook；
11. 发布和回滚；
12. 文档和风险登记。

## 26. 生产级验收总表

项目只有在以下能力均有证据时，才可称为生产级。

### 架构

- [ ] 模块边界自动验证
- [ ] Provider SDK 不泄漏
- [ ] migration 独立执行
- [ ] API/Worker 可独立扩缩

### 安全

- [ ] OIDC
- [ ] RBAC
- [ ] tenant isolation
- [ ] audit
- [ ] secret store
- [ ] least privilege
- [ ] threat model
- [ ] security tests
- [ ] SBOM/provenance

### 数据

- [ ] tenant-aware schema
- [ ] lineage
- [ ] raw/normalized/derived 分层
- [ ] resource lifecycle
- [ ] multi-currency
- [ ] cost revision
- [ ] finding lifecycle
- [ ] quality checks
- [ ] retention

### ETL

- [ ] scheduler
- [ ] distributed lease
- [ ] idempotency
- [ ] checkpoint
- [ ] retry/backoff
- [ ] partial failure
- [ ] dead-letter/quarantine
- [ ] freshness SLO

### Provider

- [ ] Azure production identity
- [ ] Azure Resource/Cost/Policy
- [ ] AWS production identity
- [ ] AWS Resource/Cost/Config
- [ ] throttling and permission diagnostics
- [ ] Provider runbooks

### API 和前端

- [ ] versioning
- [ ] OpenAPI
- [ ] pagination
- [ ] rate limiting
- [ ] contract tests
- [ ] typed frontend client
- [ ] accessibility
- [ ] browser E2E

### 事件

- [ ] outbox
- [ ] inbox
- [ ] idempotent consumer
- [ ] dead-letter
- [ ] replay
- [ ] notification delivery audit

### 运维

- [ ] OpenTelemetry
- [ ] SLO
- [ ] dashboards
- [ ] actionable alerts
- [ ] runbooks
- [ ] on-call
- [ ] incident process

### 交付

- [ ] CI gates
- [ ] staging
- [ ] immutable artifacts
- [ ] environment promotion
- [ ] migration gate
- [ ] rollback rehearsal
- [ ] Terraform remote state
- [ ] drift detection

### 韧性

- [ ] load test
- [ ] failure injection
- [ ] backup
- [ ] PITR
- [ ] restore drill
- [ ] RPO/RTO evidence
- [ ] DR runbook

## 27. 最终建设原则

```text
不以天数判断完成，以证据判断完成。
不以功能能跑判断生产，以安全、可靠、可观测、可恢复判断生产。
不以一次演示判断质量，以长期运行和故障恢复判断质量。
不为了微服务而拆分，不为了赶工而跳过基础能力。
先建立可信平台，再扩大 Provider、规则和自动化范围。
```
