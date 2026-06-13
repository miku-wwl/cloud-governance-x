# 当前能力基线

## 1. 文档定位

- 基线日期：2026 年 6 月 14 日
- 基线起点 Commit：`09619a9`
- 基线范围：Day 1～7 已提交工程，以及 Day 8 的静态事实复核
- 当前阶段：阶段 0，Day 8
- Day 8 状态：`ReadyForReview`

本文是“当前仓库有什么”的事实源，不描述愿景，也不把后续计划写成现状。
Day 8 只完成静态审查和历史证据映射；依赖 Docker、PostgreSQL、Azure 和
Terraform 的运行证据将在 Day 9 重新执行，因此云端运行事实会保守标记为
`PresentUnverified`。

## 2. 状态词典

| 状态 | 定义 |
| --- | --- |
| `VerifiedBaseline` | 当前源码、配置、测试或静态检查直接证明该事实 |
| `ImplementedLimited` | 已实现，但仅适合本地、学习或有限数据语义 |
| `PresentUnverified` | 代码和历史 E2E 脚本存在，Day 9 尚未重新取得运行证据 |
| `Planned` | 仅存在于纲领或施工计划 |
| `ProductionProhibited` | 当前行为明确禁止进入生产 |
| `DeprecatedHistorical` | 旧计划或旧表述，仅保留历史价值 |

## 3. 能力真值表

### 3.1 工程基础

| 能力 | 当前状态 | 当前实现与证据 | 当前限制 | 生产结论 | 后续阶段 |
| --- | --- | --- | --- | --- | --- |
| .NET 解决方案 | `VerifiedBaseline` | `FinOpsPlatform.slnx` 组织 6 个项目；`global.json` 固定 SDK 10.0.300 | 尚无 CI 环境复验 | 受限 | 阶段 1 |
| 分层依赖 | `VerifiedBaseline` | Api/Worker 组合 Infrastructure，Application 只引用 Domain，Domain 无项目依赖 | 依赖规则尚无架构测试 | 受限 | 阶段 1 |
| 编译质量门槛 | `VerifiedBaseline` | `Directory.Build.props` 启用 nullable、implicit usings、warnings as errors | 无格式化、静态分析和依赖漏洞统一门禁 | 受限 | 阶段 1 |
| 本地 PostgreSQL | `ImplementedLimited` | `compose.yaml` 提供 PostgreSQL 18、healthcheck 和持久卷 | 开发密码、单实例、无备份/PITR | 禁止直接生产 | Release A、阶段 15 |
| 配置覆盖 | `VerifiedBaseline` | JSON 默认值可由双下划线环境变量覆盖；`.env` 被忽略 | 无集中 secret provider 和环境配置验证 | 受限 | 阶段 1、2 |
| liveness/readiness | `ImplementedLimited` | `/health/live` 检查进程；`/health` 连接 PostgreSQL 并执行查询 | 无 Provider、队列和依赖降级状态 | 受限 | Release A、阶段 11 |
| EF Core migration | `ProductionProhibited` | API 与 Worker 启动时均调用 `MigrateAsync`；已有 3 次 migration | 多实例竞争，运行身份需要 DDL 权限 | 禁止生产 | 阶段 1 |

### 3.2 Azure 与 Terraform

| 能力 | 当前状态 | 当前实现与证据 | 当前限制 | 生产结论 | 后续阶段 |
| --- | --- | --- | --- | --- | --- |
| 本地 Azure 身份 | `ImplementedLimited` | `DefaultAzureCredential`，本地验收依赖 Azure CLI | 未建立 Managed Identity、Workload Identity 和最小权限 | 仅本地允许 | 阶段 2、5 |
| 订阅读取 | `PresentUnverified` | `AzureSubscriptionReader` 枚举当前身份可见订阅；`Test-AzureSdkIntegration.ps1` 有历史 E2E | Day 9 尚未重跑；管理 API 匿名 | 禁止公开部署 | Day 9、阶段 2 |
| Terraform 基础资源 | `PresentUnverified` | Resource Group、Storage、Service Bus Namespace/Queue、可选 Log Analytics | 仅开发规模，Storage 仍启用 shared key | 禁止直接生产 | Day 9、阶段 12 |
| apply/destroy 生命周期 | `PresentUnverified` | `Test-AzureTerraformLifecycle.ps1` 创建、验证并销毁临时资源 | Day 9 尚未重跑 | 受限 | Day 9、阶段 12 |
| Terraform state | `ProductionProhibited` | 当前使用本地 state，state/plan 已被 Git 忽略 | 无远端锁、加密、审计、环境隔离 | 禁止团队生产 | 阶段 12 |

### 3.3 资源数据

| 能力 | 当前状态 | 当前实现与证据 | 当前限制 | 生产结论 | 后续阶段 |
| --- | --- | --- | --- | --- | --- |
| Resource Graph 采集 | `PresentUnverified` | 按 Azure tenant 分组订阅，查询 Resource Graph | Day 9 尚未重跑真实 Azure | 受限 | Day 9、阶段 5 |
| 多订阅枚举 | `VerifiedBaseline` | 当前身份可见订阅均加入查询，并按 tenant 分组 | 无 CloudAccount onboarding、scope allowlist | 禁止多租户生产 | 阶段 2、5 |
| 分页 | `VerifiedBaseline` | 每页 Top 1000，持续消费 `SkipToken` | 无 checkpoint 和跨运行续传 | 受限 | 阶段 4、5 |
| 字段映射 | `VerifiedBaseline` | 保存 provider、subscription、resource id/name/type、region、resource group、tags | 未保存 lifecycle、managedBy、SKU 等生产字段 | 受限 | 阶段 3、5 |
| 幂等 Upsert | `VerifiedBaseline` | 唯一键 `(provider, resource_id_normalized)`；测试覆盖 ID 归一化 | Provider 大小写未统一归一化 | 受限 | 阶段 3 |
| 发现时间 | `VerifiedBaseline` | 首次写入 `FirstSeenAt`，重跑更新 `LastSeenAt` | 无本次扫描 ID、inactive/deleted 时间 | 受限 | 阶段 3、5 |
| 删除资源识别 | `Planned` | 当前模型和同步服务没有失活逻辑 | 已删除资源会继续留在当前数据集 | 禁止用于精确现状判断 | 阶段 3、5 |

### 3.4 成本数据

| 能力 | 当前状态 | 当前实现与证据 | 当前限制 | 生产结论 | 后续阶段 |
| --- | --- | --- | --- | --- | --- |
| Cost Management 查询 | `PresentUnverified` | REST Query API，按订阅查询自定义日期范围 | Day 9 尚未在关闭 fallback 后重跑 | 受限 | Day 9、阶段 6 |
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
| E2E 脚本 | `PresentUnverified` | 6 个 PowerShell 脚本覆盖 Day 2～7，使用独立数据库并清理临时日志/资源 | Day 8 不重跑；部分成本脚本允许 fallback | 受限 | Day 9 |
| CI、staging、SLO、备份 | `Planned` | 施工计划中存在，仓库无实现 | 无自动发布门禁和恢复证据 | 禁止生产 | Release A、阶段 11～15 |
| 用户、RBAC、tenant、audit | `Planned` | 当前没有身份中间件、业务 tenant schema 或审计模型 | 无法保护管理操作和证明组织隔离 | 禁止生产 | 阶段 2～3 |
| Policy、Monitor、finding、waiver | `Planned` | 只有 compliance DTO/interface，无 Provider 和持久化实现 | 不能声称合规治理能力 | 未实现 | 阶段 7、9 |
| React 前端、AWS、多云统一 | `Planned` | 当前仓库没有前端项目或 AWS SDK/Provider | “多云”仅是目标 | 未实现 | 阶段 8、13 |
| outbox/inbox、通知 | `Planned` | 当前没有事件可靠投递模型 | 无可靠异步治理流程 | 未实现 | 阶段 10 |

## 4. 当前生产结论

当前仓库可以作为本地学习、受控演示和后续生产化建设的 Azure 数据底座，
不能作为生产服务部署。以下行为在整改前明确禁止进入生产：

1. 匿名暴露管理 API 和成本查询 API。
2. 在 API 或 Worker 运行身份下自动执行 migration。
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

## 6. Day 9 交接条件

Day 9 必须重新执行编译、19 个测试、PostgreSQL、API、Worker、6 个 E2E 脚本
和 Terraform 生命周期，并将本表中的 `PresentUnverified` 按运行结果升级为
`VerifiedBaseline` 或降级为失败项。真实成本验证必须显式关闭 sample fallback。
