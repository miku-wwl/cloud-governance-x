# 当前能力基线

## 1. 文档定位

- 基线日期：2026 年 6 月 14 日
- 运行复验 Commit：`6ce8e25`
- 基线范围：Day 1～7 工程与 Day 8～11 阶段 0 审计、验证、架构及治理材料
- 当前阶段：阶段 0，Day 11 `Complete`
- Day 8 状态：`ReadyForReview`
- Day 9 状态：`ReadyForReview`
- Day 10 状态：`ReadyForReview`
- Day 11 状态：`Complete`

本文是“当前仓库有什么”的事实源，不描述愿景，也不把后续计划写成现状。
Day 9 已在同一提交上重新执行本地、PostgreSQL、Terraform 和真实 Azure
验证。运行结论见 `docs/phase-0/baseline-verification-summary.md`，原始输出
保存在被 Git 忽略的 `tmp/phase-0-evidence/day09/`。Day 10 当前组件、部署、
数据流和信任边界见 `docs/phase-0/current-architecture.md`。

2026 年 6 月 18 日执行过一次文档与依赖事实复核：未重新跑真实 Azure E2E，
未改变 Day 9 基线结论；新增的事实是当前 NuGet 源未报告已知漏洞，但部分
NuGet 包和 Terraform CLI 已有可用更新，已登记为 RISK-0027。阶段状态仍为
`Complete`；Reviewer Weilai Wang 已于 2026 年 6 月 18 日签发阶段 0 gate，
允许进入 Phase 1。

## 2. 状态词典

| 状态 | 定义 |
| --- | --- |
| `VerifiedBaseline` | 当前源码、配置、测试或静态检查直接证明该事实 |
| `ImplementedLimited` | 已实现，但仅适合本地、学习或有限数据语义 |
| `PresentUnverified` | 代码存在，但当前环境或当前复验轮次尚未取得有效运行证据 |
| `Planned` | 仅存在于纲领或施工计划 |
| `ProductionProhibited` | 当前行为明确禁止进入生产 |
| `DeprecatedHistorical` | 旧计划或旧表述，仅保留历史价值 |

## 3. 能力真值表

### 3.1 工程基础

| 能力 | 当前状态 | 当前实现与证据 | 当前限制 | 生产结论 | 后续阶段 |
| --- | --- | --- | --- | --- | --- |
| .NET 解决方案 | `VerifiedBaseline` | `FinOpsPlatform.slnx` 组织 7 个项目；`global.json` 固定 SDK 10.0.300 | 尚无 CI 环境复验 | 受限 | 阶段 1 |
| 分层依赖 | `VerifiedBaseline` | Api/Worker 组合 Infrastructure，Application 只引用 Domain，Domain 无项目依赖 | 依赖规则尚无架构测试 | 受限 | 阶段 1 |
| 编译质量门槛 | `VerifiedBaseline` | `Directory.Build.props` 启用 nullable、implicit usings、warnings as errors | 无格式化、静态分析和依赖漏洞统一门禁 | 受限 | 阶段 1 |
| 本地 PostgreSQL | `ImplementedLimited` | `compose.yaml` 提供 PostgreSQL 18、healthcheck 和持久卷 | 开发密码、单实例、无备份/PITR | 禁止直接生产 | Release A、阶段 15 |
| 配置覆盖 | `VerifiedBaseline` | JSON 默认值可由双下划线环境变量覆盖；`.env` 被忽略 | 无集中 secret provider 和环境配置验证 | 受限 | 阶段 1、2 |
| liveness/readiness | `ImplementedLimited` | `/health/live` 检查进程；`/health` 连接 PostgreSQL 并执行查询 | 无 Provider、队列和依赖降级状态 | 受限 | Release A、阶段 11 |
| EF Core migration | `ImplementedLimited` | Day 18 新增 `FinOps.Migrator` 和 PostgreSQL advisory lock；API/Worker 不再调用 migration API；空库、重复执行、并发拒绝、失败退出码和无 DDL runtime role 已验证 | 尚无 CI/CD migration approval、回滚和生产身份配置 | 受限 | 阶段 12 |

### 3.2 Azure 与 Terraform

| 能力 | 当前状态 | 当前实现与证据 | 当前限制 | 生产结论 | 后续阶段 |
| --- | --- | --- | --- | --- | --- |
| 本地 Azure 身份 | `ImplementedLimited` | `DefaultAzureCredential`，本地验收依赖 Azure CLI | 未建立 Managed Identity、Workload Identity 和最小权限 | 仅本地允许 | 阶段 2、5 |
| 订阅读取 | `VerifiedBaseline` | Day 9 真实 E2E 使用 Azure CLI 身份读取启用状态订阅，并与 CLI 结果一致 | 管理 API 匿名，身份仍是本地用户身份 | 禁止公开部署 | 阶段 2 |
| Terraform 基础资源 | `VerifiedBaseline` | Day 9 创建并核验 Resource Group、Storage、Service Bus Namespace/Queue | 仅开发规模，Storage 仍启用 shared key | 禁止直接生产 | 阶段 12 |
| apply/destroy 生命周期 | `VerifiedBaseline` | Day 9 完整 apply/destroy，确认 state 为空且 Resource Group 不存在 | 使用本地 state 和个人 Azure CLI 身份 | 受限 | 阶段 12 |
| Terraform state | `ProductionProhibited` | 当前使用本地 state，state/plan 已被 Git 忽略 | 无远端锁、加密、审计、环境隔离 | 禁止团队生产 | 阶段 12 |

### 3.3 资源数据

| 能力 | 当前状态 | 当前实现与证据 | 当前限制 | 生产结论 | 后续阶段 |
| --- | --- | --- | --- | --- | --- |
| Resource Graph 采集 | `VerifiedBaseline` | Day 9 创建临时资源并由 Resource Graph 发现；两次 Worker 同步结果稳定 | 仍无 checkpoint、失活语义和生产身份 | 受限 | 阶段 5 |
| 多订阅枚举 | `VerifiedBaseline` | 当前身份可见订阅均加入查询，并按 tenant 分组 | 无 CloudAccount onboarding、scope allowlist | 禁止多租户生产 | 阶段 2、5 |
| 分页 | `VerifiedBaseline` | 每页 Top 1000，持续消费 `SkipToken` | 无 checkpoint 和跨运行续传 | 受限 | 阶段 4、5 |
| 字段映射 | `VerifiedBaseline` | 保存 provider、subscription、resource id/name/type、region、resource group、tags | 未保存 lifecycle、managedBy、SKU 等生产字段 | 受限 | 阶段 3、5 |
| 幂等 Upsert | `VerifiedBaseline` | 唯一键 `(provider, resource_id_normalized)`；测试覆盖 ID 归一化 | Provider 大小写未统一归一化 | 受限 | 阶段 3 |
| 发现时间 | `VerifiedBaseline` | 首次写入 `FirstSeenAt`，重跑更新 `LastSeenAt` | 无本次扫描 ID、inactive/deleted 时间 | 受限 | 阶段 3、5 |
| 删除资源识别 | `Planned` | 当前模型和同步服务没有失活逻辑 | 已删除资源会继续留在当前数据集 | 禁止用于精确现状判断 | 阶段 3、5 |

### 3.4 成本数据

| 能力 | 当前状态 | 当前实现与证据 | 当前限制 | 生产结论 | 后续阶段 |
| --- | --- | --- | --- | --- | --- |
| Cost Management 查询 | `VerifiedBaseline` | Day 9 在关闭 fallback 后取得 HTTP 200 和 28 行真实成本，`sample data: False` | 仍受订阅账单延迟、权限和有限成本语义约束 | 受限 | 阶段 6 |
| 成本粒度 | `ImplementedLimited` | Daily + ServiceName + ResourceGroup + Currency | 不是资源级成本；无 charge type、billing period、amortized cost | 禁止声称精确资源归因 | 阶段 3、6 |
| 成本 Upsert | `VerifiedBaseline` | 六列业务唯一键，重复同步更新 cost/raw_json | 账单修订语义和 lineage 不完整 | 受限 | 阶段 3、6 |
| 成本查询 API | `VerifiedBaseline` | daily、by-service、by-resource-group；按币种独立计算占比 | 无分页、版本、认证、租户范围 | 禁止公开部署 | 阶段 2、8 |
| 样例 fallback | `ProductionProhibited` | `UseSampleDataWhenUnavailable=true`；失败、空响应可转为 `source=sample` | Provider 故障可能被业务流程视为成功 | 仅本地演示允许 | 阶段 5、6 |
| 强制样例 | `ImplementedLimited` | `ForceSampleData` 为测试和演示生成两种服务的日数据 | 不是 Azure 账单证据 | 禁止生产 | 阶段 1、5 |
| 多币种处理 | `ImplementedLimited` | 存储 currency，查询按币种分别汇总比例 | 无汇率、统一展示币种和汇率日期 | 受限 | 阶段 3、6 |

### 3.5 ETL、API 与运行

| 能力 | 当前状态 | 当前实现与证据 | 当前限制 | 生产结论 | 后续阶段 |
| --- | --- | --- | --- | --- | --- |
| 一次性 Worker | `VerifiedBaseline` | `Etl.Job` 选择 Resources 或 Costs，执行后停止宿主 | 无调度、队列和长期运行策略 | 仅手工/学习允许 | 阶段 4 |
| 管理 API 触发 | `ProductionProhibited` | 两个匿名 POST 路由可触发资源或成本同步 | 无身份、RBAC、审计、限流 | 禁止生产 | 阶段 2、8 |
| ETL 历史 | `VerifiedBaseline` | `etl_job_runs` 记录 Running/Succeeded/Failed、数量和错误摘要 | 无 attempt、checkpoint、correlation、owner | 受限 | 阶段 3、4 |
| 失败记录 | `VerifiedBaseline` | Application 服务捕获异常，写 Failed 后重新抛出 | 失败记录写入使用独立上下文但无可靠 outbox | 受限 | 阶段 4、10 |
| 调度与恢复 | `Planned` | 当前仅 Worker/API 手工触发 | 无 scheduler、lease、heartbeat、retry、checkpoint、dead-letter | 禁止生产 ETL | 阶段 4 |
| API 错误契约 | `ImplementedLimited` | 启用 ASP.NET Core ProblemDetails 和全局异常处理 | 参数异常未映射稳定业务错误码 | 受限 | 阶段 8 |

### 3.6 测试、证据与未实现能力

| 能力 | 当前状态 | 当前实现与证据 | 当前限制 | 生产结论 | 后续阶段 |
| --- | --- | --- | --- | --- | --- |
| 自动化测试 | `VerifiedBaseline` | 10 个测试文件、19 个 `[Fact]`，覆盖映射、领域行为和应用服务 | 主要为单元测试，无数据库集成、架构、安全和并发测试 | 受限 | 阶段 1～4 |
| E2E 脚本 | `VerifiedBaseline` | Day 9 串行执行 6 个脚本全部通过，并额外完成严格真实成本复验 | 仍是本地开发身份和单订阅规模 | 受限 | 阶段 1～15 |
| CI、staging、SLO、备份 | `Planned` | 施工计划中存在，仓库无实现 | 无自动发布门禁和恢复证据 | 禁止生产 | Release A、阶段 11～15 |
| 用户、RBAC、tenant、audit | `Planned` | 当前没有身份中间件、业务 tenant schema 或审计模型 | 无法保护管理操作和证明组织隔离 | 禁止生产 | 阶段 2～3 |
| Policy、Monitor、finding、waiver | `Planned` | 只有 compliance DTO/interface，无 Provider 和持久化实现 | 不能声称合规治理能力 | 未实现 | 阶段 7、9 |
| React 前端、AWS、多云统一 | `Planned` | 当前仓库没有前端项目或 AWS SDK/Provider | “多云”仅是目标 | 未实现 | 阶段 8、13 |
| outbox/inbox、通知 | `Planned` | 当前没有事件可靠投递模型 | 无可靠异步治理流程 | 未实现 | 阶段 10 |

## 4. 当前生产结论

当前仓库可以作为本地学习、受控演示和后续生产化建设的 Azure 数据底座，
不能作为生产服务部署。以下行为在整改前明确禁止进入生产：

1. 匿名暴露管理 API 和成本查询 API。
2. Migrator 尚未接入 CI/CD 审批、生产身份和回滚流程。
3. 启用成本 sample fallback 或强制样例。
4. 使用 Azure CLI 用户身份作为部署身份。
5. 使用本地 Terraform state 进行团队或生产变更。
6. 把当前资源表解释为包含删除状态的实时 CMDB。
7. 把当前成本粒度解释为单资源精确归因。
8. 对外宣称已具备 React、AWS、多租户、合规治理或生产 SLO。

## 5. 证据入口

- 工程和配置：`FinOpsPlatform.slnx`、`Directory.Build.props`、`global.json`、
  `compose.yaml`、`src/*/appsettings.json`
- API 与 Worker：`src/FinOps.Api/Program.cs`、`src/FinOps.Worker/Worker.cs`
- Azure：`src/FinOps.Infrastructure/Azure/`
- 数据模型与仓储：`src/FinOps.Domain/`、`src/FinOps.Infrastructure/Persistence/`
- 自动化测试：`src/FinOps.Tests/`
- 历史 E2E：`scripts/`
- Terraform：`terraform/azure/`
- Day 8 本地审查证据：`tmp/phase-0-evidence/`
- Day 9 永久总结：`docs/phase-0/baseline-verification-summary.md`
- Day 9 原始证据：`tmp/phase-0-evidence/day09/`
- Day 10 当前架构：`docs/phase-0/current-architecture.md`
- Day 10 本地审查证据：`tmp/phase-0-evidence/day10/`
- Day 11 风险、分类、依赖、ADR 与 gate：`docs/phase-0/` 对应专题文档
- Day 11 本地审查证据：`tmp/phase-0-evidence/`、`tmp/day11-closeout-report.md`

## 6. 阶段 0 Gate

Day 11 永久治理材料和机械门禁已经形成，当前没有产品测试失败、未知 Azure
资源、测试数据库、端口或 Terraform 运行产物阻断。Reviewer Weilai Wang 已于
2026 年 6 月 18 日签发 `stage-0-gate-report.md`，最终状态为 `Complete`，允许
进入 Phase 1 / Day 12。

允许带入 Phase 1 的已登记风险包括 RISK-0003、RISK-0013、RISK-0022、
RISK-0023、RISK-0027，以及所有仍为 Open 的生产化风险。Phase 1 必须优先处理
ADR-0001、ADR-0002、ADR-0018 和统一静态门禁。阶段完成仍不代表系统可部署到
生产。
