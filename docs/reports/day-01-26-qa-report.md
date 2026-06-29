# Day 1-26 QA 质量审计报告

## 1. 审计结论

本报告覆盖 [Day 1](../days/day-01.md) 到 [Day 26](../days/day-26.md)，同时引用 [当前状态](../current-state.md)、[风险台账](../archive/phase-0/risk-register.md)、[生产差距台账](../archive/phase-0/production-gap-register.md)、[Phase 0 报告](../phase/phase-0.md) 和 [Phase 1 报告](../phase/phase-1.md)。

总体结论：

| 维度 | 结论 |
| --- | --- |
| 开发基线 | 通过。Day 1-7 已建立 .NET 解决方案、EF Core、PostgreSQL、Terraform、Azure SDK、Worker 和基础测试。 |
| 工程治理 | 基本通过。Day 12-19 已建立格式化、静态分析、架构测试、CI 与迁移宿主。 |
| 身份与租户 | 部分通过。Day 20-26 已实现租户模型、TenantContext、租户感知 Repository、OIDC/JWT 开发集成，但 RBAC、端点授权策略、审计和 RLS 仍未闭环。 |
| 生产就绪 | 不通过。当前仍不得声称具备生产能力，也不得对外暴露管理、查询、成本或资源类业务接口。 |
| QA 放行 | 允许进入 Day 27 RBAC 施工；禁止进入生产、预生产验收或商业演示口径。 |

质量判定：

- 当前项目是“开发环境可运行、工程骨架已成型、关键生产控制未完成”的状态。
- Day 1-26 的主要成果是真实工程骨架和治理基线，不是生产级平台。
- 当前最大风险不是单点 bug，而是“身份、授权、审计、租户隔离、运行可靠性、发布治理”六类生产控制尚未形成闭环。

## 2. 审计范围

审计对象包括：

| 范围 | 文档 |
| --- | --- |
| 开发基线 | [Day 1](../days/day-01.md) 到 [Day 7](../days/day-07.md) |
| Phase 0 生产差距建模 | [Day 8](../days/day-08.md) 到 [Day 11](../days/day-11.md) |
| Phase 1 工程治理 | [Day 12](../days/day-12.md) 到 [Day 19](../days/day-19.md) |
| Phase 2 已完成部分 | [Day 20](../days/day-20.md) 到 [Day 26](../days/day-26.md) |
| 风险与缺口 | [风险台账](../archive/phase-0/risk-register.md)、[生产差距台账](../archive/phase-0/production-gap-register.md) |

不在本次审计范围内：

- Day 27 及之后的未施工能力。
- `tmp/` 下的临时证据文件。
- 未在仓库文档中形成记录的口头计划。

## 3. 阶段质量画像

| 阶段 | Day | 状态 | QA 评价 |
| --- | --- | --- | --- |
| 开发起步 | Day 1-7 | 完成 | 能跑通本地开发闭环，但无生产安全边界。 |
| Phase 0 | Day 8-11 | Accepted | 生产差距识别充分，风险台账有效，但只完成“识别”，未完成“治理”。 |
| Phase 1 | Day 12-19 | Accepted | 工程治理显著增强，CI、静态检查、架构测试和迁移宿主形成基础门禁。 |
| Phase 2 已实施部分 | Day 20-26 | 进行中 | 租户与身份基础已进入代码，但授权、审计、RLS 和全端点保护仍是硬阻断项。 |

## 4. 关键质量问题

### QF-001：业务端点尚未绑定授权策略

严重级别：Critical

证据：

- [Day 25](../days/day-25.md) 明确 OIDC Bearer 只完成认证基础，端点仍未绑定统一授权策略。
- [Day 26](../days/day-26.md) 明确 Entra delegated scope 仍不是业务授权策略。
- [风险台账 RISK-0001](../archive/phase-0/risk-register.md) 与 [生产差距 GAP-001](../archive/phase-0/production-gap-register.md) 均记录匿名管理与查询 API 风险。

影响：

- 管理、资源、成本、同步、ETL 等接口可能在缺少业务授权边界时被调用。
- 即使 JWT 可验证，也不能证明调用方有访问某租户、某订阅、某成本数据的权限。

整改要求：

- Day 27 必须建立 RBAC 权限模型、范围模型和策略映射。
- Day 28 必须完成端点级授权策略清单，并禁止新增未标注授权意图的业务端点。
- 未完成前，所有业务端点不得进入生产或对外演示环境。

### QF-002：RBAC 尚未实现，delegated scope 被误用风险高

严重级别：Critical

证据：

- [Day 26](../days/day-26.md) 将 Entra 开发集成标记为 Validation，且明确 delegated scope 不等同于 RBAC。
- [当前状态](../current-state.md) 明确 Day 27 是“权限与范围 RBAC”。

影响：

- 用户只要具备登录能力，不代表具备租户、订阅、资源组、成本数据的访问权限。
- 如果后续开发误把 `scp` 当作业务授权，会形成横向越权。

整改要求：

- RBAC 必须独立于 OIDC scope 设计。
- 权限判断必须同时包含主体、租户、范围、动作、资源类型。
- 验证用例必须覆盖无权限、跨租户、跨范围、最小权限和拒绝优先。

### QF-003：审计链路缺失，无法满足生产追责

严重级别：Critical

证据：

- Day 20-26 均将审计列为后续遗留风险。
- [当前状态](../current-state.md) 明确不得声称审计能力已完成。

影响：

- 无法回答谁在什么时间访问、修改、同步或导出过哪些租户数据。
- 出现误操作、越权访问、数据泄露或成本异常时，缺少追责证据。

整改要求：

- Day 29 必须建立审计事件模型和写入路径。
- 管理类、授权类、成本查询类、资源同步类接口必须进入审计范围。
- 审计记录必须包含主体、租户、范围、动作、结果、关联请求、时间戳。

### QF-004：租户隔离仍未达到生产闭环

严重级别：High

证据：

- [Day 20](../days/day-20.md) 完成租户领域模型。
- [Day 21](../days/day-21.md) 完成数据库租户字段与索引，但状态为 Validation。
- [Day 22](../days/day-22.md) 完成 TenantContext。
- [Day 23](../days/day-23.md) 完成租户感知 Repository。
- [Day 24](../days/day-24.md) 完成 legacy backfill，但 EF 模型仍保留 nullable 过渡。
- [风险台账 RISK-0002](../archive/phase-0/risk-register.md) 仍保留 RLS、全端点认证、RBAC、环境数据回填等遗留事项。

影响：

- 应用层过滤已经增强，但数据库层强隔离尚未完成。
- 旧数据、空租户字段、旁路查询、后台任务和未来报表能力仍可能绕开租户边界。

整改要求：

- Phase 2 必须完成业务端点授权和审计。
- 后续 Phase 必须补齐 RLS 或等效数据库层隔离策略。
- 所有跨租户后台任务必须显式携带租户上下文，不得依赖隐式全局查询。

### QF-005：Azure Provider 运行身份仍停留在开发形态

严重级别：Critical

证据：

- [Day 3](../days/day-03.md) 采用 Azure SDK 与 `DefaultAzureCredential`。
- [Day 26](../days/day-26.md) 明确 Entra 认证不改变 Azure Provider 运行身份。
- [风险台账 RISK-0018](../archive/phase-0/risk-register.md) 与 [GAP-005](../archive/phase-0/production-gap-register.md) 记录 Azure CLI 用户身份风险。

影响：

- 本地 Azure CLI 身份不具备生产可审计、可轮换、最小权限、环境隔离能力。
- Provider 操作可能受个人账号权限、登录状态和本机环境影响。

整改要求：

- 生产 Provider 必须使用托管身份、工作负载身份或明确的服务主体方案。
- 权限必须按租户、订阅和动作最小化。
- Provider 调用必须纳入审计、重试、限流、错误分类和超时控制。

### QF-006：成本数据仍不满足 FinOps 生产语义

严重级别：Critical

证据：

- [Day 6](../days/day-06.md) 完成成本 POC，但明确 sample fallback 只允许开发使用。
- [Day 7](../days/day-07.md) 完成 Cost ETL 基础，但未完成 FinOps 生产语义。
- [风险台账 RISK-0006](../archive/phase-0/risk-register.md)、RISK-0008 与 [GAP-004](../archive/phase-0/production-gap-register.md)、GAP-008 仍开放。

影响：

- sample fallback 若误入生产，会造成虚假成本数据。
- 成本口径缺少 amortized、actual、charge type、billing period、revision、currency、resource attribution 等关键语义。
- 管理层可能基于不完整数据做预算或优化决策。

整改要求：

- 生产环境必须硬禁 sample fallback。
- 成本数据模型必须显式表达账期、币种、计费口径、粒度、来源和修订状态。
- 成本报表在语义未闭环前不得标注为“财务可信”。

### QF-007：ETL 可靠性不足，无法承载生产作业

严重级别：High

证据：

- [Day 5](../days/day-05.md) 建立 ETL Run 基础。
- [Day 7](../days/day-07.md) 完成成本 ETL 基础。
- [Day 17](../days/day-17.md) 完成 Worker 任务注册表，但仍是 one-shot Worker。
- [风险台账 RISK-0004](../archive/phase-0/risk-register.md)、RISK-0005 和 [GAP-006](../archive/phase-0/production-gap-register.md) 仍开放。

影响：

- 没有可靠调度、租约、并发互斥、checkpoint、恢复和重试策略。
- 多实例运行可能造成重复写入、漏采或成本数据不一致。

整改要求：

- ETL 必须具备运行状态机、租约、幂等键、checkpoint、失败恢复和告警。
- Worker 任务必须区分手工触发、调度触发和补偿触发。
- 成本与资源同步任务必须具备可追踪 run id。

### QF-008：发布治理尚未形成生产门禁

严重级别：Critical

证据：

- [Day 18](../days/day-18.md) 完成迁移宿主。
- [Day 19](../days/day-19.md) 完成 CI 收口。
- [GAP-012](../archive/phase-0/production-gap-register.md) 仍记录缺少 staging、CD、artifact promotion。
- [当前状态](../current-state.md) 明确不得声称具备生产部署能力。

影响：

- 代码、数据库迁移、配置、基础设施和制品之间缺少可审计发布链路。
- 迁移失败、配置漂移或错误制品发布时，缺少回滚和责任边界。

整改要求：

- 建立开发、测试、预发、生产环境分层。
- 制品必须不可变并通过 promotion 进入更高环境。
- 数据库迁移必须与发布审批、回滚预案、备份策略绑定。

### QF-009：备份、恢复、PITR 和 DR 未验证

严重级别：Critical

证据：

- [风险台账 RISK-0011](../archive/phase-0/risk-register.md) 与 [GAP-013](../archive/phase-0/production-gap-register.md) 仍开放。
- Day 1-26 未形成恢复演练证据。

影响：

- 数据损坏、误删、迁移失败或云服务故障时，恢复目标不可证明。
- 无法定义 RPO、RTO，也无法通过生产上线评审。

整改要求：

- 必须建立备份策略、恢复演练、PITR 验证和 DR 文档。
- 每次重大迁移前必须有恢复点和回滚路径。

### QF-010：可观测性、SLO 和告警缺失

严重级别：Critical

证据：

- [风险台账 RISK-0010](../archive/phase-0/risk-register.md) 与 [GAP-011](../archive/phase-0/production-gap-register.md) 仍开放。
- Day 1-26 主要验证为本地测试与 CI 测试，缺少生产级运行指标。

影响：

- 无法判断 API、Worker、Provider、数据库和 ETL 是否健康。
- 失败可能沉默发生，成本数据与资源数据可能长期不可信。

整改要求：

- 建立指标、日志、trace、告警和仪表盘。
- 为 API、ETL、Provider、数据库迁移定义 SLI/SLO。
- 关键失败必须进入告警，而不是只写本地日志。

### QF-011：API 契约仍未生产化

严重级别：High

证据：

- [Day 15](../days/day-15.md) 完成 endpoint 模块化，但将 API versioning、OpenAPI、pagination、auth、error contract 留到后续。
- [RISK-0026](../archive/phase-0/risk-register.md) 与 [GAP-016](../archive/phase-0/production-gap-register.md) 仍开放。

影响：

- 客户端集成无法获得稳定契约。
- 错误处理、分页、限流、幂等、兼容性和版本演进缺少统一规范。

整改要求：

- 建立统一 API 错误结构、分页结构、版本策略和 OpenAPI 输出。
- 管理类 API 必须有幂等和审计要求。
- 高风险接口必须具备 rate limit 或等效保护。

### QF-012：供应链与依赖治理仍是中后期风险

严重级别：Medium

证据：

- [Day 12](../days/day-12.md) 和 [Day 13](../days/day-13.md) 完成 analyzer、format、warning gate 和 Central Package Management。
- [风险台账 RISK-0023](../archive/phase-0/risk-register.md)、RISK-0024、RISK-0027 仍记录 xUnit v2、Postgres 镜像 digest、依赖漂移等风险。

影响：

- 当前治理已能防止部分代码质量退化，但尚不足以支撑完整供应链安全。
- 容器镜像、依赖漏洞、SBOM、签名、密钥扫描、IaC 扫描仍未形成闭环。

整改要求：

- 后续 Phase 必须补齐 SBOM、漏洞扫描、容器镜像 digest、密钥扫描和依赖升级流程。
- CI 门禁应区分开发检查和发布检查。

## 5. Day 级 QA 复盘

| Day | 结论 | 主要质量关注点 |
| --- | --- | --- |
| Day 1 | Accepted | 本地工程骨架成立，但无 auth、tenant、CI、生产迁移和运维能力。 |
| Day 2 | Accepted | Terraform 只适合开发环境，local state、开发身份和销毁保护缺失是生产阻断。 |
| Day 3 | Accepted | Azure SDK 接入成功，但 Provider 身份仍是开发形态。 |
| Day 4 | Accepted | 资源同步基础成立，但缺少 checkpoint、inactive/deleted 生命周期和规模化同步控制。 |
| Day 5 | Accepted | ETL Run 基础成立，但调度、并发互斥、恢复和审计缺失。 |
| Day 6 | Accepted | 成本 POC 成立，但 sample fallback 与成本口径是高风险点。 |
| Day 7 | Accepted | Cost ETL 基础成立，但 FinOps 语义、预算、异常和运维闭环缺失。 |
| Day 8 | Accepted | 生产能力基线清晰，但未新增控制面能力。 |
| Day 9 | Accepted | 验证基线有效，但无 staging、HA、备份、租户隔离和安全测试。 |
| Day 10 | Accepted | 架构与数据流可追溯，但关键生产控制仍在台账中。 |
| Day 11 | Accepted | Phase 0 gate 合理，明确不生产可用。 |
| Day 12 | Accepted | analyzer 与 format 门禁有效，但不是完整 SAST。 |
| Day 13 | Accepted | 静态门禁增强，但供应链安全仍未闭环。 |
| Day 14 | Accepted | 架构测试能防止明显依赖倒挂，但不能替代人工架构评审。 |
| Day 15 | Accepted | endpoint 模块化改善可维护性，但 API 契约未生产化。 |
| Day 16 | Accepted | DI 分层改善模块边界，但生产配置和密钥治理未完成。 |
| Day 17 | Accepted | Worker 注册表改善任务扩展性，但可靠调度仍未完成。 |
| Day 18 | Accepted | 迁移宿主建立基础，但生产发布、审批、回滚仍缺失。 |
| Day 19 | Accepted | Phase 1 工程治理可接受，但生产控制仍大面积开放。 |
| Day 20 | Accepted | 租户模型建立，但尚未进入运行时和授权闭环。 |
| Day 21 | Validation | 数据库租户字段与索引进入代码，但报告状态仍需后续验证闭环。 |
| Day 22 | Accepted | TenantContext 成立，但 repository、OIDC、RBAC 和 audit 尚未完成。 |
| Day 23 | Accepted | Repository 租户过滤成立，但 historic NULL、RLS 和 endpoint auth 未闭环。 |
| Day 24 | Validation | legacy backfill 完成，但大数据量、锁、恢复和 EF nullable 仍需关注。 |
| Day 25 | Validation | OIDC Bearer 成立，但未绑定业务授权策略。 |
| Day 26 | Validation | Entra 开发集成成立，但 delegated scope 不能替代 RBAC。 |

## 6. 质量红线

以下事项在完成前不得进入生产、预生产验收或外部用户试用：

- 未完成 Day 27 RBAC 权限与范围模型。
- 未完成端点授权策略绑定与拒绝默认策略。
- 未完成审计事件模型和关键接口审计写入。
- 未完成生产 Provider 身份替换。
- 未硬禁生产环境 sample fallback。
- 未完成 ETL 调度、租约、幂等、checkpoint 和恢复策略。
- 未完成备份、恢复、PITR 和 DR 演练。
- 未完成 staging、artifact promotion、发布审批和回滚流程。

## 7. 下一步 QA 门禁

| 门禁 | 必须完成项 | 不通过后果 |
| --- | --- | --- |
| Day 27 RBAC 门禁 | 权限模型、范围模型、最小权限测试、跨租户拒绝测试 | 不允许继续扩大 API 面 |
| Day 28 端点保护门禁 | 所有业务端点有授权策略，未标注端点默认拒绝 | 不允许进入审计施工 |
| Day 29 审计门禁 | 关键管理与查询行为有审计记录 | 不允许进入 Phase 2 完工评审 |
| Day 30 Phase 2 门禁 | 身份、租户、RBAC、端点保护、审计形成闭环 | Phase 2 不得标记 Accepted |

## 8. QA 结论

Day 1-26 的工程推进是有效的：项目从零散本地实现，进入了有文档、有 CI、有静态门禁、有架构测试、有迁移宿主、有租户模型、有 OIDC 开发认证的状态。

但从生产质量看，当前仍处于“生产控制建设中”。真正的放行条件不是测试数量，而是安全边界、数据边界、审计边界、发布边界和运维边界同时成立。当前 Day 27-30 必须集中完成 Phase 2 闭环，不能提前转向功能扩张。
