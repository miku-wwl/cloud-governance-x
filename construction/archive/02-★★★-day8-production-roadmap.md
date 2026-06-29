# Cloud Governance X Day 8 之后的生产化学习施工计划

> Historical planning note, 2026-06-29:
>
> This long Day 8-148 roadmap is retained for context and review history. It is
> no longer the primary planning source. Current planning uses milestone gates
> in `docs/roadmap.md`, the active execution playbook in
> `construction/current-playbook.md`, and day-by-day review capsules in
> `docs/days/`.

> 文档状态：第一版
>
> 起点：已完成原 Day 1～7 开发基线
>
> 终点：首轮生产上线、canary 验证和持续运营接管
>
> 重要说明：本文中的 `Day` 是一个可独立验收的施工单元，不等于一个自然日。

## 1. 对这项要求的合理性判断

你的要求是合理的，而且适合这个项目当前的状态。

原因如下：

1. Day 1～7 已经形成了一条真实可运行的 Azure、PostgreSQL、API、Worker 和
   Terraform 链路，继续使用 Day 编号有利于回顾学习过程。
2. `construction/01-★★★-construction-plan.md` 使用“阶段”描述生产成熟度，
   而不是描述每天做什么。单独增加一份 Day 级施工索引，可以把长期阶段拆成
   可执行任务。
3. 你会频繁进行人工 review。把工作切成小的闭环单元，可以让每次 review 只
   聚焦有限的代码、数据和风险，不必反复全仓审查。
4. 生产级项目不能继续使用旧的“30 天内堆完功能”思路。认证、多租户、迁移、
   调度、可观测性、恢复和安全验证都需要先设计、再实现、再取证。
5. 一边实践一边学习时，工期必须服从理解程度和验收结果。没有学懂或没有通过
   门禁的 Day，不应该因为编号计划而被强行宣布完成。

但这个要求只有在以下约束下才真正合理：

- `Day` 表示施工编号，不表示必须在一天内完成；
- 一个 Day 可以持续多个自然日；
- 前一个 Day 未闭环时，不开始后一个依赖它的 Day；
- review 发现问题时，继续修复当前 Day，不创建“表面完成”的新编号；
- 阶段状态与 Day 编号并行存在，不能用“已经 Day 30”替代阶段出关证据；
- 计划允许因真实技术发现而调整，但调整必须留下原因和影响记录。

结论：

```text
继续使用 Day 8、Day 9……作为学习和实施索引是合理的。
把 Day 当作固定工期或完成度百分比是不合理的。
```

## 2. 当前起点为什么仍是阶段 0 后期

Day 1～7 已经完成的是“开发基线”：

- .NET 10 Clean Architecture 基础结构；
- API、Worker 和 PostgreSQL；
- Azure Terraform 临时资源生命周期；
- Azure SDK 身份验证；
- Azure Resource Graph 资源同步；
- Azure Cost Management 成本同步；
- 资源、成本和 ETL 执行历史；
- 自动化测试和真实 Azure 端到端脚本。

这些成果证明核心技术链可以工作，但新版生产门禁还要求：

- 正式的当前架构图和数据流图；
- 风险登记册、Owner、严重度和处理阶段；
- 数据分类和敏感信息边界；
- 依赖、许可证和供应链清单；
- ADR backlog；
- 可重复保存的基线验收证据；
- 对“当前可用、当前受限、生产禁止”的统一表述。

因此：

```text
Day 编号：已经完成 Day 1～7，下一施工单元是 Day 8。
阶段状态：阶段 0 处于 Validation，尚未通过出关门禁。
```

两者描述的是不同维度，不矛盾。

## 3. 本文与其他指导文件的关系

| 文件 | 负责回答的问题 | 权威范围 |
| --- | --- | --- |
| `outline.md` | 项目最终要成为什么，哪些原则不能突破 | 产品、架构和生产质量纲领 |
| `construction/01-★★★-construction-plan.md` | 要经过哪些阶段，每个阶段如何出关 | 阶段依赖、门禁和 Definition of Done |
| `construction/02-★★★-day8-production-roadmap.md` | 从 Day 8 开始具体按什么顺序实践和学习 | Day 级施工顺序和 review 节奏 |
| `docs/` | 某个专题当前如何设计、配置、运行和审查 | 专题知识与长期文档 |
| `tmp/` | 本次命令输出、临时报告和验证证据 | 本地临时材料，不提交 Git |

发生冲突时使用以下优先级：

```text
生产安全与数据正确性
    >
outline.md
    >
construction/01-★★★-construction-plan.md 的阶段门禁
    >
本文件的 Day 顺序
    >
旧文档中的历史工期和旧范围
```

Day 计划可以调整，生产原则和已经确认的安全门禁不能为了赶进度而降低。

## 4. 每个 Day 的统一闭环契约

以后要求 Codex “实践 Day X”时，该 Day 默认必须完成以下全部动作。

### 4.1 开工前

1. 阅读本文件中该 Day 的目标、前置条件和验收证据；
2. 阅读 `outline.md`、`construction/01-★★★-construction-plan.md` 的对应阶段；
3. 检查 Git 状态，不覆盖用户已有修改；
4. 检查上一 Day 或上一阶段门禁是否已通过；
5. 列出本次允许修改的势力范围和明确不修改的范围；
6. 对高风险设计先记录 ADR 或设计说明，再写实现。

### 4.2 实现中

1. 保持 Domain、Application、Infrastructure、API、Worker 的依赖方向；
2. 数据模型变化必须包含 EF Configuration、migration、兼容和回滚考虑；
3. 外部依赖必须处理 timeout、cancellation、retry、限流和错误分类；
4. tenant、安全、审计、幂等、数据 lineage 和 telemetry 不能事后补丁式加入；
5. 只实现本 Day 的必要范围，不顺手堆叠尚未评审的后续功能；
6. 不提交 secret、本地日志、Terraform state、测试数据库或临时报告。

### 4.3 自动化验收

根据影响范围至少执行：

- 格式和静态解析；
- restore、build 和相关测试；
- architecture tests；
- migration 测试；
- PostgreSQL 集成测试；
- API contract 测试；
- Provider contract 测试；
- Terraform fmt、validate 和必要的 plan；
- secret、dependency、IaC、container 或 license 检查；
- 对应的浏览器、负载、安全、恢复测试。

### 4.4 手工与真实环境验收

根据本 Day 的边界执行：

- 本地启动和 API/Worker 手工验证；
- 真实 PostgreSQL 数据核对；
- Azure 或 AWS sandbox/staging E2E；
- 身份、权限和 tenant 隔离验证；
- 失败、超时、重试、重复、取消和恢复验证；
- telemetry、Dashboard 和告警验证；
- 云资源、进程、数据库和临时文件清理验证。

### 4.5 收尾

1. 在 `tmp/dayNN-closeout-report.md` 写本次闭环报告；
2. 报告记录命令、结果、人工步骤、遗留风险和清理结果；
3. 长期有效的设计进入 `docs/` 或 ADR，不能只留在 `tmp/`；
4. 更新受到影响的 README、配置说明、数据模型和运行文档；
5. 执行 `git diff --check`，检查垃圾文件和 secret；
6. 由人工 review 确认该 Day 是 `Complete`、继续 `Validation` 或 `Blocked`；
7. 默认不 push，只有用户明确要求后才提交和推送。

## 5. Review 和学习节奏

### 5.1 每个 Day 的聚焦 review

只审查：

- 本 Day 的目标是否真的实现；
- 变更文件及其直接联动文件；
- 新增测试是否覆盖主要风险；
- 手工验收是否验证真实行为；
- 文档是否准确；
- 是否留下资源、日志、数据库或生成垃圾。

### 5.2 每个阶段的门禁 review

阶段最后一个 Day 不增加业务功能，只做：

- 阶段需求逐条对照；
- 全量相关自动化测试；
- staging E2E；
- 安全、数据、运行和回滚证据；
- 风险关闭或接受；
- 文档和 review 地图更新；
- 决定是否允许进入下一阶段。

### 5.3 每个 Release 的全局 review

Release A、B、C 和生产上线前执行跨阶段 review：

- 架构边界；
- tenant、安全和审计；
- 数据语义和 migration；
- ETL 可靠性；
- Provider 真实性；
- API 和前端契约；
- 可观测性、SLO 和 runbook；
- CI/CD、回滚、备份和恢复；
- 云费用和资源清理；
- 仓库整洁度。

### 5.4 Review 不通过时的编号规则

不创建新的“修复 Day”掩盖问题。

例如 Day 24 review 不通过：

```text
错误做法：把 Day 24 标记完成，Day 25 再补 Day 24 的缺陷。
正确做法：Day 24 保持 Validation，修复并重新验收，出关后再开始 Day 25。
```

## 6. 总体路线

| Day 范围 | 对应阶段 | 主要成果 |
| --- | --- | --- |
| Day 8～11 | 阶段 0 | 冻结 Day 1～7 基线并建立风险证据 |
| Day 12～19 | 阶段 1 | 架构治理、静态门禁、模块拆分和独立 migration |
| Day 20～30 | 阶段 2 | tenant、OIDC、RBAC、审计和隔离 |
| Day 31～40 | 阶段 3 | 生产数据模型、lineage、migration 和数据质量 |
| Day 41～50 | 阶段 4 | 调度、租约、幂等、恢复和 ETL 运维 |
| Day 51～59 | Release A 基础 | 遥测、容器、环境、CI/CD、备份恢复和发布门禁 |
| Day 60～70 | 阶段 5 | Azure Provider 生产化 |
| Day 71～78 | 阶段 6 | FinOps 归因、预算、趋势和异常 |
| Day 79～87 | 阶段 7 | 规则、finding、豁免和整改闭环 |
| Day 88～95 | 阶段 8 | 生产 API |
| Day 96～104 | 阶段 9 | 生产前端 |
| Day 105～112 | 阶段 10 | 事件、告警和通知 |
| Day 113～119 | 阶段 11 | OpenTelemetry、SLO、Dashboard 和 runbook |
| Day 120～127 | 阶段 12 | 平台、Terraform、CI/CD 和 Release B |
| Day 128～136 | 阶段 13 | AWS Provider、双云统一和 Release C |
| Day 137～140 | 阶段 14 | 威胁模型、安全测试和供应链门禁 |
| Day 141～142 | 阶段 15 | 性能和容量 |
| Day 143～144 | 阶段 15 | 韧性、备份和灾难恢复 |
| Day 145～148 | 阶段 16～17 | 上线门禁、内部发布、canary 和持续运营接管 |

## 7. Day 8～11：完成阶段 0

### 阶段目标

把 Day 1～7 从“已经做过”变成“行为、风险和证据都可以被重复验证”。

详细施工入口：

[`phase-0/00-★★★-phase-0-guide.md`](phase-0/00-★★★-phase-0-guide.md)

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 8 | 建立当前能力真值表；统一 README、旧可行性报告和新生产纲领中“已实现、受限、未实现”的表述；列出生产禁止项 | 文档交叉检查；Git 跟踪文件清单；无过时的 30 天完成承诺；`git diff --check` | 学习项目事实来源、历史文档与现行纲领的区别；重点审查是否夸大能力 |
| Day 9 | 重新执行 Day 1～7 自动化、PostgreSQL、API、Worker、Terraform 和真实 Azure E2E；形成基线验收报告 | build/test 结果；六条 E2E 结果；数据库、进程、Azure 资源和 Terraform 产物清理证明 | 学习测试金字塔和真实 E2E 的成本；重点审查失败路径及资源清理 |
| Day 10 | 绘制当前组件图、部署图、数据流图和 trust boundary；标记 API、Worker、数据库、Azure、Terraform 和用户身份边界 | Mermaid 或等价图可渲染；图与代码和配置一致；每条外部数据流有身份、协议和数据类型 | 学习 C4、数据流和信任边界；重点审查隐含依赖与敏感数据流 |
| Day 11 | 建立风险登记册、数据分类、依赖与许可证清单、ADR backlog 和阶段 0 证据索引；执行阶段 0 出关 review | 每个风险有 Owner、严重度、处理阶段；secret 扫描；依赖清单；无未知云资源；阶段 0 checklist 全部有结论 | 学习风险管理、数据分类和 ADR；只做门禁，不增加功能 |

### 阶段 0 出关结果

通过后，项目从“开发基线”进入“受治理的生产化建设”。

## 8. Day 12～19：阶段 1 架构治理与工程门禁

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 12 | 增加 `.editorconfig`、分析器、格式规则和统一编译策略 | 本地与干净 restore/build 通过；故意引入违规可使检查失败 | 学习编译器、analyzer 和格式化边界；避免纯样式大改掩盖行为 diff |
| Day 13 | 建立单一静态验证入口，覆盖 JSON、YAML、XML、PowerShell、Markdown、Terraform、build、test、secret 和垃圾文件检查 | 一个命令可重复运行；任何子检查失败时返回非零退出码；无云资源费用 | 学习质量门禁脚本设计；重点审查误报、漏报和跨平台可执行性 |
| Day 14 | 增加 architecture tests，自动约束 Domain、Application、Infrastructure、API、Worker 和云 SDK 依赖 | 故意制造反向依赖时测试失败；正常仓库全绿 | 学习 Clean Architecture 的可执行约束；重点审查规则是否验证真实程序集 |
| Day 15 | 将 API endpoint 从 `Program.cs` 拆分为 Resources、Costs、ETL、Cloud 和 Health 模块 | 路由与响应兼容；API 集成测试和现有 E2E 通过 | 学习 composition root 与 endpoint module；不在重构中改变业务语义 |
| Day 16 | 拆分 Infrastructure DI 为 PostgreSQL、Azure、Health 和应用用例注册；明确生命周期 | API 和 Worker 都能启动；DI 验证通过；无重复或 captive dependency | 学习 DI 生命周期和宿主差异；重点审查 credential、DbContext 和 HttpClient |
| Day 17 | 将 Worker 的 `if/else` Job 分派改为 Job Handler 注册表和显式 Job 契约 | Resources、Costs、未知 Job、取消和失败退出码测试通过 | 学习策略模式与后台任务生命周期；避免引入万能 Handler |
| Day 18 | 新增独立 Migration Host 或 migration command；移除 API 和 Worker 启动时自动 migration | 空库升级、已有库升级、重复执行、失败退出码验证；API/Worker 使用受限数据库权限启动 | 学习生产 migration 的职责隔离；重点审查并发迁移与回滚策略 |
| Day 19 | 建立初版 CI、PR 模板、ADR 模板、责任边界和阶段 1 总门禁 | 干净环境 CI 全绿；架构违规、格式违规、测试失败会阻断；阶段证据完整 | 学习 CI 作为合并契约；本日只做阶段 review 和必要修复 |

### 阶段 1 独立签收门禁

Day 19 工程门禁通过后，仍必须按照
[`04-★★★-phase-1-independent-review-guide.md`](04-★★★-phase-1-independent-review-guide.md)
对固定的 `main` commit 执行独立全面 review。只有 Review Ledger 中无未关闭的
Critical/High，Medium 已修复或由 Owner 书面接受，且 Owner 完成 Independent
Acceptance，才允许进入 Day 20。

## 9. Day 20～30：阶段 2 身份、多租户、RBAC 与审计

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 20 | 编写 tenancy ADR，定义 Organization、Tenant、CloudAccount、ProviderConnection、Membership 和 scope | 模型评审记录；身份来源、隔离边界和平台管理员路径明确 | 学习 SaaS tenant 模型；先澄清业务 tenant 与 Azure tenant 的区别 |
| Day 21 | 实现 tenant 基础 Domain、EF Configuration 和 migration | migration Up/Down；唯一索引包含 tenant；数据库集成测试 | 学习 tenant-aware schema；重点审查外键、删除和唯一性 |
| Day 22 | 建立可信 `TenantContext`，分别支持 HTTP 用户和后台 Job | 客户端伪造 tenant 被拒绝；缺失上下文失败；后台任务显式携带 tenant | 学习身份声明到业务上下文的转换；不能信任任意 query/header |
| Day 23 | 将资源、成本和 ETL Repository 改为 tenant-aware；禁止默认无 tenant 查询 | tenant A/B 数据隔离测试；所有查询和写入均有 tenant 条件 | 学习防 IDOR 和数据访问默认安全；重点审查漏过滤和唯一索引 |
| Day 24 | 为 Day 1～7 现有数据设计兼容 backfill，迁入受控开发 tenant | 老数据无丢失；backfill 可重复；升级和回退路径有记录 | 学习 expand/contract 和历史数据迁移；禁止直接删除旧数据重来 |
| Day 25 | 接入 API Bearer Token 验证和可测试的 OIDC 配置 | 无 token、过期、issuer、audience、签名错误测试；health 匿名边界明确 | 学习 OAuth 2.0/OIDC 与 JWT 验证；避免自己实现密码系统 |
| Day 26 | 完成 Microsoft Entra ID 开发环境集成和本地开发身份策略 | 真实 Entra token 调用；key rotation 行为；配置不含 secret | 学习 app registration、scope、role 和 metadata；重点审查 redirect/audience |
| Day 27 | 实现 permission + scope 的 policy-based RBAC | admin、operator、analyst、auditor、owner 的允许与拒绝矩阵测试 | 学习角色与权限分离；重点审查只在 UI 隐藏而后端未授权的问题 |
| Day 28 | 保护现有管理和查询 API；增加 correlation ID 与授权失败的稳定错误 | 所有 endpoint 有 auth policy 或显式 anonymous 理由；管理操作不可匿名执行 | 学习 API trust boundary；重点审查 `/api/admin` 和跨 scope 操作 |
| Day 29 | 实现 append-only audit 模型，记录 actor、tenant、action、target、result 和 correlation | 高权限操作成功与失败都有审计；普通业务身份不能修改审计 | 学习审计与普通日志的区别；重点审查敏感 before/after 数据 |
| Day 30 | 执行 tenant escape、IDOR、后台 tenant 丢失、RBAC 和审计 E2E；阶段 2 出关 | tenant A 无法访问 tenant B；越权测试全绿；真实 Entra 闭环；风险已更新 | 本日只做安全门禁；任何隔离缺陷都阻止进入数据模型阶段 |

## 10. Day 31～40：阶段 3 数据模型与迁移生产化

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 31 | 编写 Raw、Normalized、Derived、Operational 四层数据 ADR 和 lineage 契约 | 每类现有数据归层；source、job、schema/parser version 和 raw reference 明确 | 学习数据平台分层；避免把所有 payload 永久塞进业务表 |
| Day 32 | 实现 ingestion metadata 和受控 raw payload reference | raw 与 normalized 可相互追溯；敏感 payload 分类、保留和访问受控 | 学习数据 lineage、hash 和 schema evolution |
| Day 33 | 扩展资源生命周期：first/last seen、scan run、active/inactive/deleted 和关系 | 完整成功扫描后才失活；部分失败不误删；资源重现语义测试 | 学习快照、全量同步和最终一致性；重点审查误标删除 |
| Day 34 | 扩展成本语义：cost type、charge type、账期、币种、原金额、修订和 source lineage | 多币种不混加；负成本、refund、迟到修订测试；decimal 精度确认 | 学习云账单语义；不能把 Resource Group 成本冒充单资源成本 |
| Day 35 | 重构 ETL 模型为 JobDefinition、JobRun、Attempt、Checkpoint 和 Trigger | 状态机、attempt、heartbeat、progress 和 error category 测试 | 学习控制平面与业务数据分离 |
| Day 36 | 建立 Rule、Finding、Waiver、Event、Outbox、Inbox 和 Notification 基础 schema | 指纹、版本、状态历史和 tenant 边界评审；migration 测试 | 学习未来工作流的数据前置设计；本日不实现业务引擎 |
| Day 37 | 实现数据质量规则和 retention 配置骨架 | 缺字段、重复、错误日期、币种、tenant/account mismatch 可检测 | 学习数据质量不是异常日志，而是可查询的运行结果 |
| Day 38 | 对现有表执行 expand/contract 和 backfill；建立 schema compatibility 测试 | 老版本应用与扩展阶段兼容；backfill 可恢复；无静默截断 | 学习零停机迁移；重点审查默认值和大表锁 |
| Day 39 | 在接近 staging 数据量上演练 migration、备份、失败、roll-forward 和恢复 | migration 时长、锁、恢复点和数据 reconciliation 证据 | 学习数据库发布风险；不能只验证空数据库 |
| Day 40 | 执行资源生命周期、多币种、修订、lineage、质量和恢复总 E2E；阶段 3 出关 | 数据模型 checklist 全绿；data dictionary 与代码一致 | 本日只做数据门禁；数据语义不清时不进入调度平台 |

## 11. Day 41～50：阶段 4 任务调度与可靠 ETL

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 41 | 定义 Job Handler、JobDefinition、参数 schema、版本和 concurrency policy | 非法参数、未知版本和不支持 Job 失败可诊断 | 学习稳定任务契约和版本化 |
| Day 42 | 实现持久化 schedule、due-job scan、manual trigger、pause/resume 和 backfill | 时区规则、重复扫描和禁用任务测试；手工触发有授权审计 | 学习调度器只负责产生任务，不直接承载全部业务 |
| Day 43 | 通过 ADR 选择 Release A 的任务队列方式，并实现 enqueue/dequeue 基础能力 | 并发消费、重复投递和数据库暂不可用测试 | 学习 PostgreSQL 队列与 Service Bus 的取舍；保留可替换端口 |
| Day 44 | 实现 lease、heartbeat、lease expiry、distributed lock 和并发策略 | 两个 Worker 争抢；Worker 崩溃；租约过期接管测试 | 学习 at-least-once 执行和分布式互斥 |
| Day 45 | 实现 idempotency key、checkpoint、continuation token 和 partition | 中断后续跑；重复消息不重复写；跨 partition 行为测试 | 学习幂等与 exactly-once 幻觉 |
| Day 46 | 实现 timeout、cancellation、指数退避、jitter、最大尝试和错误分类 | 429、5xx、超时、取消、永久错误路径测试 | 学习 transient、permanent、data、security failure |
| Day 47 | 实现 partial success、quarantine、dead-letter 等价状态和 operator replay | 部分订阅失败仍可追踪；重放有审计且不绕过次数限制 | 学习失败数据的运维生命周期 |
| Day 48 | 实现受保护的 Job 管理 API：查询、触发、取消、重试和回放 | RBAC、tenant scope、并发和审计测试 | 学习控制面 API 与普通查询 API 的风险差异 |
| Day 49 | 增加 queue delay、duration、records、retry、throttling、freshness 和 backlog 指标 | 指标可采集；失败类别可聚合；不记录 secret 或敏感 payload | 学习 ETL SLI 和可行动 telemetry |
| Day 50 | 执行双 Worker、崩溃、续跑、重复、取消、数据库故障和 Provider 故障 E2E；阶段 4 出关 | 无静默丢失和无控制重复；runbook 初稿；阶段证据完整 | 本日只做可靠性门禁 |

## 12. Day 51～59：Release A 平台基础

这一段提前落实阶段 11 和阶段 12 中 Release A 必需的基础能力。后续仍会继续
完善完整 SLO、平台部署和发布体系。

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 51 | 为 API、Worker、Migration Host 和外部调用接入 OpenTelemetry 基线 | 同一 correlation 可跨宿主、数据库和外部调用查询；敏感字段脱敏 | 学习 traces、metrics、logs 的职责 |
| Day 52 | 建立开发环境 API、Worker、DB、Job 和 freshness Dashboard 与基础告警 | 故意失败能触发预期指标和告警；告警附 Owner 与处理入口 | 学习 symptom-based alert |
| Day 53 | 为 API、Worker、Migration Host 建立 non-root、多阶段生产容器 | 镜像构建、启动、健康、优雅关闭和只读文件系统验证 | 学习容器进程模型；重点审查镜像中的 SDK、secret 和权限 |
| Day 54 | 建立 development 基础设施、remote Terraform state、locking 和 secret store | state 加密与锁验证；环境可重建；无本地生产 secret | 学习 IaC state 是关键生产数据 |
| Day 55 | 建立最小 staging 环境，与生产采用相同部署方式和独立身份数据 | staging 部署、身份、数据库、队列和遥测闭环 | 学习环境同构和配置差异管理 |
| Day 56 | CI 生成固定版本 artifact、container image、SBOM 和扫描结果 | artifact 可追溯到 commit；高危扫描阻断；镜像 digest 固定 | 学习供应链基础与不可变产物 |
| Day 57 | CD 自动部署 development，并将同一 artifact 晋级 staging | 部署审计、smoke、migration gate 和回滚演练 | 学习 build once, promote many |
| Day 58 | 配置备份和 PITR，执行首次数据库恢复与 reconciliation | 恢复到独立环境；RPO/RTO 实测；恢复后核心数据一致 | 学习“有备份”与“能恢复”的区别 |
| Day 59 | 执行 Release A 全局门禁，不增加新功能 | 身份、tenant、数据、ETL、CI/CD、遥测、备份和回滚证据包 | 通过后才允许把平台用于受控内部数据 |

## 13. Day 60～70：阶段 5 Azure Provider 生产化

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 60 | 实现 tenant-aware Azure connection 和 subscription onboarding 生命周期 | onboard、suspend、reconnect、offboard 测试；连接不保存明文 secret | 学习控制平面连接模型 |
| Day 61 | 将 staging/production 身份切换为 Managed Identity 或 Workload Identity | 不依赖开发机 Azure CLI；凭据轮换和权限撤销测试 | 学习 Azure identity chain 和最小权限 |
| Day 62 | 实现 permission preflight、capability discovery 和 connection health | 缺权限能指出具体 scope/action；能力差异不伪装成功 | 学习 onboarding 前置诊断 |
| Day 63 | 加固 Resource Graph 多 subscription、分页、限流、重试和 query version | 大结果分页、429、单订阅失败和取消测试 | 学习 Azure Resource Graph 的一致性与限流 |
| Day 64 | 完成资源 full-scan、一致性、关系、inactive 和 partial failure 处理 | 部分扫描不误失活；关系与 lifecycle E2E | 学习资源清单不是简单 Upsert |
| Day 65 | 重做 Cost Management 生产语义，移除生产 sample fallback | actual/amortized、动态列、币种、空账单和错误响应测试 | 学习 Provider 无数据与 Provider 故障的区别 |
| Day 66 | 实现成本 backfill、迟到修订、重算和 source reconciliation | 同一账期重跑安全；修订可追溯；原币种保留 | 学习账单最终一致性 |
| Day 67 | 接入真实 Azure Policy Insights，保存 definition、assignment、state 和 exemption | 真实 Policy 结果与平台规则来源严格区分 | 学习 Azure Policy 数据模型与 scope |
| Day 68 | 接入 Azure Monitor 批量指标，为闲置判断提供 observation input | missing metric、窗口、限流和批量查询测试 | 学习“无指标”和“指标为零”的区别 |
| Day 69 | 完成 Azure Provider contract、sandbox resilience 和权限测试 | credential 过期、429、5xx、partial、empty、malformed response 全覆盖 | 学习 Provider 适配器的生产标准 |
| Day 70 | Azure staging 全链路 E2E 和阶段 5 出关 | 多订阅资源、成本、Policy、Monitor、freshness、runbook 和清理证据 | 本日不增加 Azure 功能 |

## 14. Day 71～78：阶段 6 FinOps 能力

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 71 | 定义 direct、relationship-derived、rule-allocated 和 unallocated 归因模型 | 每种结果有依据、rule version 和 confidence；不伪造精度 | 学习成本归因边界 |
| Day 72 | 实现 subscription、service、Resource Group、tag、owner、environment 和 cost-center 归因 | 标签变化、冲突、缺失和 shared cost 测试 | 学习维度来源与组织规则 |
| Day 73 | 实现 tenant/account/scope 预算、阈值、owner 和通知事件 | 月/季边界、币种、阈值重复和审计测试 | 学习预算不是简单百分比字段 |
| Day 74 | 实现日/月趋势、period comparison 和可解释 forecast 基线 | 账期边界、修订重算、稀疏数据测试 | 学习时间序列展示与预测边界 |
| Day 75 | 实现第一版异常检测、最小历史、窗口、seasonality 基础和 backtest | 负成本、零转非零、稀疏数据、误报反馈测试 | 学习算法版本、precision 和 recall |
| Day 76 | 建立成本数据质量、raw-normalized reconciliation 和安全重算流程 | 缺数据、重复、异常突变和汇率缺失可见 | 学习财务决策数据的质量门禁 |
| Day 77 | 提供 FinOps 查询 API 和每个决策指标的 lineage 解释 | API 结果可追溯 source/job/rule；多币种隔离 | 学习可解释性与查询性能 |
| Day 78 | FinOps staging E2E 和阶段 6 出关 | 归因、预算、趋势、异常、质量、重算和文档证据 | 本日只做数据正确性 review |

## 15. Day 79～87：阶段 7 合规与整改工作流

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 79 | 实现 Rule definition、version、parameter、scope、severity 和 effective date | 规则版本不可覆盖历史；非法参数失败 | 学习规则即数据和版本治理 |
| Day 80 | 实现 evaluator、dry-run、影响预览和首批 required-tags 规则 | dry-run 不写 finding；真实执行可重复 | 学习规则执行与发布分离 |
| Day 81 | 实现稳定 finding fingerprint、evidence、first/last seen 和状态历史 | 去重、重开、解决、规则升级测试 | 学习 finding 生命周期 |
| Day 82 | 实现 waiver、approver、scope、expiry、renewal 和自动到期 | 过期后 finding 恢复；越权豁免失败；完整审计 | 学习风险接受不是永久忽略 |
| Day 83 | 实现 Owner、assignment、comment 和 evidence 工作流 | 权限、并发更新和历史记录测试 | 学习协作状态与审计 |
| Day 84 | 实现 recommendation、manual task、外部 ticket 契约和 remediation approval 骨架 | 默认只读建议；执行身份和审批边界明确 | 学习自动整改前的控制面 |
| Day 85 | 将 Azure Policy finding 与平台规则映射但保持来源隔离 | UI/API/source 字段不能混淆；云端 exemption 映射测试 | 学习统一体验不等于抹平 Provider 语义 |
| Day 86 | 提供 Compliance、Finding、Waiver 和 Remediation API/报告 | 分页、tenant、RBAC、审计和导出边界测试 | 学习治理 API 契约 |
| Day 87 | 合规闭环 staging E2E 和阶段 7 出关 | 发现、分配、豁免、到期、整改建议、解决、重开全链路 | 本日只做工作流和安全门禁 |

## 16. Day 88～95：阶段 8 生产 API

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 88 | 建立 `/api/v1`、API 分类和稳定 endpoint module 结构 | 旧路由迁移或兼容策略；OpenAPI 可生成 | 学习版本化和模块契约 |
| Day 89 | 统一 Problem Details、error code、correlation、日志脱敏和错误映射 | 所有失败响应 schema 一致；不泄漏 stack、SQL、token | 学习错误是外部契约 |
| Day 90 | 实现分页、filter/sort 白名单、query limits 和 UTC/日期语义 | 大结果不能无界返回；非法过滤稳定失败 | 学习查询 API 的资源保护 |
| Day 91 | 实现 rate limit、idempotency key、optimistic concurrency 和必要的 ETag | 重复写、并发写、限流和重试测试 | 学习 HTTP 幂等和并发控制 |
| Day 92 | 完成 query、operator、admin、internal、webhook、export 的差异化权限与审计 | 每类 API 的 auth、scope、limit 和 audit 矩阵 | 学习不同 API 风险等级 |
| Day 93 | 实现异步 export job、对象存储、过期、下载授权和审计 | 大导出不占用同步请求；URL 不含 secret；跨 tenant 下载失败 | 学习异步文件交付和数据泄露风险 |
| Day 94 | 建立 OpenAPI contract、breaking-change 和客户端兼容测试 | 实现与 OpenAPI 一致；破坏性变更阻断 CI | 学习 contract-first 和 deprecation |
| Day 95 | API 安全、负载、tenant、错误和 staging E2E；阶段 8 出关 | p95 达标；越权与大查询被阻断；API 证据包完整 | 本日不新增 endpoint |

## 17. Day 96～104：阶段 9 生产前端

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 96 | 创建 React + TypeScript 工程、lint、test、build、router 和 query state 基础 | CI 构建；环境配置不打包 secret；目录边界清晰 | 学习前端构建产物和运行时配置 |
| Day 97 | 接入 OIDC PKCE、token lifecycle、typed API client 和 tenant/account selector | 登录、登出、过期、刷新、未授权和 tenant 切换测试 | 学习浏览器身份安全 |
| Day 98 | 建立 design shell、权限导航、loading/empty/stale/partial/error 和 correlation 体验 | 不同角色菜单与操作；错误能定位后端 trace | 学习前端状态不是只有成功页面 |
| Day 99 | 实现 Overview 和 Cost 页面 | 多币种、筛选、预算、趋势、异常和数据新鲜度展示正确 | 学习图表语义与财务数据表达 |
| Day 100 | 实现 Resources 和 Provider Connections 页面 | lifecycle、关系、连接健康、权限诊断和分页验证 | 学习资产清单与连接运维 |
| Day 101 | 实现 Compliance、Findings、Waivers 和 Remediation 页面 | 权限动作、状态流、证据、豁免到期和审计可见 | 学习治理工作流 UI |
| Day 102 | 实现 Anomalies、ETL Runs、Audit 和 Platform Operations 页面 | 任务重试/取消需授权；审计不可编辑；大表分页 | 学习操作面与业务面的区别 |
| Day 103 | 完成 accessibility、键盘、响应式、虚拟化、CSP 和安全 header | 基础 WCAG、XSS/CSP、超大表和慢网状态测试 | 学习可访问性和浏览器安全 |
| Day 104 | 执行关键浏览器 E2E、角色矩阵和阶段 9 出关 | 登录到核心治理流程闭环；前后端 correlation；无越权 | 本日只做产品体验门禁 |

## 18. Day 105～112：阶段 10 事件、告警与通知

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 105 | 定义 Governance Event contract、schema version、correlation 和 causation | contract compatibility 测试；tenant/provider/subject 必填 | 学习事件不是随意 JSON |
| Day 106 | 实现 transactional outbox 和可靠 publisher | DB 提交后事件最终可发布；重复发布有稳定 event ID | 学习 dual-write 问题 |
| Day 107 | 建立 Azure Service Bus topology、权限、重试和 dead-letter 基础设施 | Terraform、网络、身份、队列和费用 review | 学习消息基础设施和最小权限 |
| Day 108 | 实现 inbox、幂等 consumer、handler version 和处理历史 | 重复、乱序、consumer crash 和并发测试 | 学习 at-least-once 消费 |
| Day 109 | 实现 retry、poison、dead-letter 查询、诊断和 replay | 最大重试不会被绕过；重放有授权审计 | 学习失败消息运维 |
| Day 110 | 实现告警 dedupe、aggregation、suppression、maintenance、ack 和 resolution | 告警风暴、静默窗口和恢复测试 | 学习告警生命周期 |
| Day 111 | 实现通知适配器，先完成一个真实渠道，再保持 Email、Teams、Webhook 扩展契约 | 429、5xx、模板失败、重试和 delivery audit | 学习通知是可失败的下游，不阻塞核心 finding |
| Day 112 | 事件到告警到通知 staging E2E 和阶段 10 出关 | outbox、bus、inbox、告警、通知、死信和重放证据 | 本日只做可靠事件门禁 |

## 19. Day 113～119：阶段 11 可观测性、SLO 与运行手册

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 113 | 完善跨 API、Worker、DB、Service Bus、Provider 的 trace propagation | 一个业务动作可端到端追踪；sampling 和 redaction 验证 | 学习 trace parent、baggage 和采样 |
| Day 114 | 完善 API、runtime、DB、queue、Job、Provider、quality 和 freshness 指标 | 指标名称、单位、维度和 cardinality review | 学习高基数指标风险 |
| Day 115 | 定义并实现 availability、latency、ETL、freshness、event 和 dead-letter SLI/SLO | SLO 可自动计算；云厂商数据延迟与平台责任分开 | 学习服务目标与外部依赖边界 |
| Day 116 | 建立 executive、API、Worker、DB、queue、Provider、tenant hotspot 和 release Dashboard | Dashboard 可回答健康、影响范围和变化时间 | 学习面向决策的可视化 |
| Day 117 | 建立可行动告警、路由、严重度、maintenance 和 test-alert 流程 | 每个告警有 Owner、阈值理由和 runbook 链接 | 学习减少噪声而不是增加告警数量 |
| Day 118 | 为关键故障编写 runbook 并执行 staging incident drill | 非作者可按 runbook 定位和恢复；演练问题进入 backlog | 学习运维知识可执行化 |
| Day 119 | 阶段 11 门禁 review | SLO、Dashboard、告警、trace、runbook 和演练证据完整 | 本日不增加遥测项 |

## 20. Day 120～127：阶段 12 平台与 Release B

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 120 | 完成容器 hardening、resource limits、graceful shutdown、probe 和镜像策略 | non-root、read-only、信号处理、扫描和资源限制测试 | 学习容器安全与调度契约 |
| Day 121 | 通过 ADR 选择目标运行平台，并实现 API、Worker、Migration 和 Scheduler 部署单元 | staging 部署同构；各组件可独立扩缩和失败 | 学习 Kubernetes 或目标托管平台的取舍 |
| Day 122 | 实现 TLS、入口、网络策略、workload identity、secret integration 和环境隔离 | 网络拒绝、secret rotation、证书和身份测试 | 学习平台 trust boundary |
| Day 123 | 实现 HPA、PDB、anti-affinity、rolling/canary 和 graceful drain | 扩缩、节点维护、pod kill 和 rollout 验证 | 学习可用性不是副本数等于 2 |
| Day 124 | 重构 Terraform modules、环境 state、drift、policy check 和生产保护 | plan artifact、apply approval、drift 检测和 state restore | 学习生产 IaC 变更控制 |
| Day 125 | 补齐 CI：integration、contract、migration、frontend、安全、IaC、container、license 和 provenance | 所有门禁可重复；失败阻断 artifact 晋级 | 学习 CI 的反馈速度与覆盖平衡 |
| Day 126 | 补齐 CD：immutable promotion、migration、smoke、canary、rollback、release notes 和审计 | 同一 artifact 从 staging 晋级；应用和 migration 回滚演练 | 学习发布编排 |
| Day 127 | 执行 Azure Production Release B 总门禁，包含 Azure 范围的 threat review、安全测试、目标负载、故障注入和恢复复验 | Azure 资源、成本、Policy、FinOps、治理、API、前端、事件、SLO、安全、性能和恢复全证据 | 通过后才可面向首批 Azure tenant；阶段 14、15 仍会对双云整体重复并扩展验证 |

## 21. Day 128～136：阶段 13 AWS 与 Release C

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 128 | 完成 AWS 账号、Organizations、Cost Explorer/CUR、Config 和权限预检；编写 AWS Provider ADR | 功能可用性、费用、区域和账号边界清楚 | 学习 AWS 与 Azure 的身份和账单差异 |
| Day 129 | 实现 IAM Role、STS AssumeRole、external ID 和 connection health | 短期凭据、轮换、错误信任策略和最小权限测试 | 学习 cross-account access |
| Day 130 | 实现 Organization/account/region discovery 和 onboarding | global/region 语义；suspend/offboard；partial account failure | 学习 AWS 账号是核心隔离边界 |
| Day 131 | 实现 Resource Explorer、Tagging API 和分页限流基础 inventory | 多 region、global resource、覆盖缺口和 429 测试 | 学习统一 inventory 的 Provider 差异 |
| Day 132 | 补充 EC2、EBS、EIP、S3、ELB、RDS 等必要专用适配和 lifecycle | identity、关系、标签、删除和 unsupported diagnostics | 学习通用 API 不能覆盖所有资源 |
| Day 133 | 实现 Cost Explorer/CUR 生产成本链路 | unblended/amortized、credit/refund、迟到数据和币种测试 | 学习 AWS 成本数据源取舍 |
| Day 134 | 接入 AWS Config 和 CloudWatch，为 compliance 与 idle input 提供真实来源 | Config 来源隔离；missing metrics；限流和权限测试 | 学习 Config、CloudWatch 与平台规则的关系 |
| Day 135 | 统一 Azure/AWS 查询、归因、finding、事件和 Dashboard，验证故障域隔离 | 一个 Provider 故障不阻塞另一个；特有语义可追溯 | 学习统一契约不等于最低公分母 |
| Day 136 | 双云 staging E2E 和 Release C 门禁 | 双云 onboarding、资源、成本、合规、指标、事件、SLO 和 runbook 证据 | 通过后才正式使用“多云生产能力”表述 |

## 22. Day 137～140：阶段 14 安全与供应链

Release B 已完成 Azure 范围的生产安全验证。本阶段在 AWS 接入后，对统一身份、
双云 Provider、供应链和整个平台重新进行系统级安全验证，不能直接复用 Azure
单云结论。

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 137 | 完整 threat model、资产、攻击者、trust boundary 和 STRIDE 分析 | 每项威胁有预防、检测、响应或明确接受 | 学习威胁建模驱动测试 |
| Day 138 | 执行 OWASP API、IDOR、SSRF、injection、auth bypass、tenant escape 和 export leakage 测试 | 自动化安全测试和人工验证；无未接受 Critical/High | 学习业务授权漏洞通常比语法漏洞更危险 |
| Day 139 | 执行 Azure/AWS 最小权限、secret rotation、break-glass 和 incident response 演练 | 权限差距清单；凭据轮换不中断或有受控窗口 | 学习身份生命周期和应急访问 |
| Day 140 | 完成 SAST、DAST、dependency、container、IaC、SBOM、provenance、license 和安全总门禁 | artifact 可验证；风险接受有 Owner 和到期时间 | 本日只做安全门禁，不以扫描“无结果”代替人工判断 |

## 23. Day 141～144：阶段 15 性能、韧性和灾难恢复

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 141 | 建立 tenant、account、resource、cost row、finding、event、user、retention 的容量模型和负载数据集 | 目标规模、增长率、数据库/队列/存储成本和余量明确 | 学习先定义负载，再谈性能 |
| Day 142 | 执行 API、aggregation、export、ETL、bulk upsert、queue、Dashboard 和 hotspot 负载测试 | p50/p95/p99、throughput、资源使用、索引和容量结论 | 学习性能瓶颈定位，避免只做单机 happy path |
| Day 143 | 执行 DB/queue/Provider/network/Worker/API/bad deployment/bad rule 故障注入 | 无 silent data loss；自动恢复和告警符合预期 | 学习韧性来自已验证的失败行为 |
| Day 144 | 执行 PostgreSQL PITR、对象存储、Terraform state、配置、队列和环境重建 DR 演练 | RPO/RTO 实测达标；恢复后 reconciliation；非开发者可执行 runbook | 学习灾难恢复是业务流程，不只是数据库命令 |

## 24. Day 145～148：生产上线与运营接管

| Day | 本日工程闭环 | 必须验收的证据 | 学习与人工 review 重点 |
| --- | --- | --- | --- |
| Day 145 | 汇总产品、工程、数据、运行、安全、性能和 DR 证据，执行正式 Go/No-Go | 所有阻断项关闭；已知限制公开；Owner、on-call、预算、回滚和支持明确 | 学习用证据做上线决策；任何 tenant 隔离或财务数据错误默认 No-Go |
| Day 146 | 只向内部 tenant 发布，限制账号、用户和只读能力 | 内部用户流程、SLO、告警、数据质量、支持和回滚决定 | 学习先验证真实运行，不在同一天扩大用户范围 |
| Day 147 | 选择小范围 canary tenant，执行 onboarding、运行观察和扩大/回滚决策 | canary 指标、错误预算、用户反馈、数据 reconciliation 和发布记录 | 学习渐进式发布和 blast radius 控制 |
| Day 148 | 完成稳定期 review、遗留问题分级和运营接管 | on-call、运营日历、access review、容量、成本、漏洞、DR 和复盘节奏生效 | 学习上线不是终点；正式进入阶段 17 持续运营循环 |

## 25. Day 148 之后的持续运营编号

Day 148 后不再预先穷举所有自然日。新的 Day 应来自真实运营需求，并继续遵守
同一闭环契约。

优先来源：

- SLO 和 error budget review；
- incident postmortem；
- Provider API/version 变化；
- dependency 和漏洞更新；
- capacity 与成本 review；
- 规则效果、异常 precision 和 finding aging；
- tenant 反馈；
- access review；
- DR 演练；
- GCP、Kubernetes、Unit Economics、Savings Plan 等新能力。

可使用以下编号：

```text
Day 149：第一个经过评审的生产运营改进单元
Day 150：下一个独立闭环单元
……
```

新 Day 必须先说明它来自哪个风险、SLO、事件、用户需求或阶段 17 目标，不能因为
“想加一个功能”就绕过产品和生产门禁。

## 26. 以后让 Codex 实践某个 Day 的推荐指令

```text
请按照 construction/02-★★★-day8-production-roadmap.md 实践 Day XX。

先核对前置 Day 和 construction/01-★★★-construction-plan.md
对应阶段是否已经出关。
只处理本 Day 的范围，先设计再实现。
必须完成代码、migration、自动化测试、真实手工或 staging E2E、失败路径、
资源清理、文档更新和 tmp/dayXX-closeout-report.md。
完成后重新检查仓库垃圾、secret 和 Git diff。
先不要 push，等我人工 review 后再决定。
```

如果该 Day 主要是设计、门禁或演练，也必须产出可审查证据，不能因为“没有大量
业务代码”而省略闭环。

## 27. 人工学习记录建议

每个 Day review 时，建议自己回答以下问题：

1. 这一天解决了哪个生产风险？
2. 为什么必须在后续能力之前完成？
3. 关键数据和控制流从哪里到哪里？
4. 最重要的不变量是什么？
5. 失败时系统留下什么状态，如何恢复？
6. 哪个测试最能证明实现是真的？
7. 哪一步使用了真实云、数据库、身份或消息系统？
8. 哪些内容仍然只是受限能力？
9. 如果删除本 Day 的实现，哪个生产门禁会失效？
10. 我能否不看代码，用自己的话讲清楚设计取舍？

无法回答时，该 Day 可以处于 `Implementation` 或 `Validation`，但不应急着标记
为 `Complete`。

## 28. 计划调整规则

本文件是施工顺序，不是不可修改的合同。

允许调整：

- 外部云服务不可用导致的顺序变化；
- ADR 证明另一种技术路线更合适；
- review 发现前置模型必须重做；
- 安全、数据或恢复风险要求插入工作；
- 用户学习节奏需要把一个 Day 拆成多个更小单元。

不允许调整：

- 为了更快看到页面而跳过 tenant 和授权；
- 为了演示成功而恢复生产 sample fallback；
- 为了赶编号而跳过 migration、失败、恢复和清理测试；
- 把真实 Azure Policy/AWS Config 与平台模拟规则混为一谈；
- 在无审批、无 dry-run、无回滚时开放 destructive remediation；
- 用“已经完成很多 Day”替代 Release 或生产门禁。

每次调整需要记录：

```text
调整原因
受影响的 Day
受影响的阶段门禁
新增或降低的风险
新的依赖关系
人工 review 结论
```

## 29. 最终原则

```text
Day 用来帮助实践和学习，不用来制造进度幻觉。
阶段用来表达成熟度，Release 用来表达可交付范围。
代码写完不是闭环，测试、真实验收、清理、文档和 review 都是工程的一部分。
没有通过前置门禁，就不继续在不可信基础上堆功能。
```
