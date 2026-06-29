# 03 ★★★ Day 1～7 人工 Review 与学习路线

> 历史 review 说明，2026-06-29：
>
> 本文件仍可用于理解 Day 1-7 基线，但当前 review 入口是
> `docs/days/README.md` 和 `docs/current-state.md`。

> 盘点日期：2026-06-14
>
> 盘点对象：原 Day 1～7 基线的 108 个 Git 跟踪文件；后续路线文档不计入本稿的
> 逐文件清单
>
> 目标：给所有文件划定职责边界，减少以后人工 review 的重复阅读和无效阅读。

## 文档分区与星级

`docs/` 只保留项目事实、配置、架构和运行专题：

| 顺序 | 文档 | 重要性 | 用途 |
| --- | --- | --- | --- |
| 01 | 项目合理性与工期评估 | ★★ | 先理解目标、范围和风险 |
| 02 | 配置文件说明 | ★★★ | 理解整个工程如何构建和运行 |
| 03 | Terraform | ★★ | 理解 Day 2 Azure 基础设施生命周期 |
| 04 | Azure Integration | ★★★ | 理解 Day 3～7 的 Azure 外部集成 |
| 05 | Data Model | ★★★ | 理解资源、成本和 ETL 的数据库语义 |

`construction/` 根目录保存跨阶段总控文件：

| 顺序 | 文档 | 重要性 | 用途 |
| --- | --- | --- | --- |
| 01 | 生产级阶段化建设计划 | ★★★ | 定义阶段、依赖和出关门禁 |
| 02 | Day 8 之后生产化路线 | ★★★ | 定义逐 Day 施工顺序 |
| 03 | 人工 Review 与学习路线 | ★★★ | 串联文件并指导人工审查 |

`construction/phase-0/` 保存阶段 0 的局部施工文件：

| 顺序 | 文档 | 重要性 | 用途 |
| --- | --- | --- | --- |
| 00 | 阶段 0 Day 8～11 总指南 | ★★★ | 定义阶段证据和闭环规则 |
| 01 | Day 8 能力基线 | ★★★ | 冻结当前能力、限制和生产差距 |
| 02 | Day 9 基线验收 | ★★★ | 重跑本地、PostgreSQL、Terraform 和 Azure E2E |
| 03 | Day 10 架构与数据流 | ★★★ | 绘制当前架构、数据流和信任边界 |
| 04 | Day 11 风险与出关 | ★★★ | 建立风险、分类、依赖、ADR 和阶段结论 |

星级含义：

- `★★★`：核心必读；修改相关工程前应先理解；
- `★★`：重要专题；进入对应能力时必读；
- `★`：辅助说明；需要时阅读；
- 无星：存档或可选材料。

星级表示当前 Day 1～7 的学习和审查重要性，不是文档质量评分。

## 1. 这份划分解决什么问题

当前仓库已经完成 Day 1～7。如果每次 review 都从根目录开始逐个文件阅读，会有
三个问题：

1. 无法区分业务源代码、配置、测试、文档和工具生成物；
2. 修改一个成本查询，也会被迫重新检查 Terraform、资源 ETL 和所有 migration；
3. 人工精力被格式、锁文件和 Designer 文件消耗，真正的业务风险反而容易漏掉。

因此，本稿给每个文件附加四种属性：

- **势力范围**：这个文件属于哪个职责域；
- **Day 来源**：它最初服务于 Day 1～7 中哪一阶段；
- **人工等级**：需要人工审查到什么深度；
- **联动范围**：它变化时还必须检查哪些相邻文件。

这不是代码所有权制度，也不是禁止跨目录修改，而是一张 review 路由表。

## 2. 人工审查等级

| 等级 | 含义 | 人工动作 |
| --- | --- | --- |
| R0 | 工具生成物 | 不逐行审；只验证生成来源、是否意外重写、是否与源模型一致 |
| R1 | 低风险说明或开发便利文件 | 检查事实、路径、命令和是否泄密 |
| R2 | 配置或稳定契约 | 检查字段语义、默认值、兼容性、依赖方向 |
| R3 | 核心业务实现 | 逐行审查行为、异常路径、边界、幂等性和测试 |
| R4 | 外部系统或破坏性操作 | 最高强度；检查权限、费用、资源清理、数据一致性和失败恢复 |

### 2.1 机械校验不能替代人工审查

机械校验适合回答：

- JSON、YAML、XML、HCL 能否解析；
- .NET 能否 restore、build、test；
- Terraform 能否 fmt、validate、plan；
- PowerShell 是否有语法错误；
- migration 是否能应用；
- Git 是否混入 `tmp/`、state、secret、`bin/obj`。

人工审查必须回答：

- 业务语义是否正确；
- 数据粒度是否被夸大；
- sample data 是否伪装成真实数据；
- 重复执行是否安全；
- 失败是否可见和可恢复；
- Azure 资源是否会遗留或持续收费；
- 层间依赖是否倒置；
- 文档是否把未来计划写成已完成事实。

## 3. 十一个势力范围

| 编号 | 势力范围 | 核心责任 | 默认人工等级 |
| --- | --- | --- | --- |
| Z0 | 项目治理与范围 | 项目目标、工期、README、架构判断 | R1～R2 |
| Z1 | 工具链与仓库卫生 | SDK、工具、解决方案、忽略规则、公共编译配置 | R1～R2 |
| Z2 | 本地运行与宿主配置 | Compose、环境变量、API/Worker settings 和启动配置 | R2～R3 |
| Z3 | API 边界 | HTTP 路由、参数、返回值、错误与健康检查 | R3 |
| Z4 | Application 契约与用例 | DTO、接口、同步与查询编排 | R2～R3 |
| Z5 | Domain 业务不变量 | 领域实体、状态转换、归一化规则 | R3 |
| Z6 | Azure 外部适配 | 认证、SDK、Resource Graph、Cost Management | R4 |
| Z7 | PostgreSQL 与 EF Core | DbContext、映射、Repository、migration | R3～R4 |
| Z8 | Worker 执行控制 | Job 选择、生命周期、退出码、日志 | R3 |
| Z9 | 自动化与端到端证据 | 单元测试、集成映射测试、真实云闭环脚本 | R2～R4 |
| Z10 | Terraform Azure | 资源、变量、Provider、生命周期与费用 | R3～R4 |

文档不单独成为一个孤立势力。每份专题文档归属于它描述的代码域，避免出现
“文档组只看文字、代码组只看实现”的断裂。

## 4. Day 1～7 的业务纵向范围

| 纵向范围 | 主要能力 | 主要势力范围 |
| --- | --- | --- |
| D1 基础底座 | .NET 工程、PostgreSQL、健康检查、API/Worker 宿主 | Z1、Z2、Z3、Z7、Z8 |
| D2 Azure IaC | Azure 资源创建、验证、销毁 | Z10、Z9 |
| D3 Azure 认证 | 订阅读取、DefaultAzureCredential、Provider 契约 | Z4、Z6、Z3、Z9 |
| D4 资源盘点 | Resource Graph、资源实体、Upsert、首次 migration | Z4～Z9 |
| D5 资源 ETL | Job Run、失败记录、手工触发 | Z3～Z9 |
| D6 成本 POC | Cost Management、成本实体、Upsert、sample fallback | Z4～Z9 |
| D7 成本正式化 | Worker Cost Job、聚合查询 API、查询 Repository | Z3、Z4、Z7～Z9 |
| X 跨 Day | README、配置、公共 DI、DbContext、快照 | Z0～Z2、Z6～Z7 |

后续 review 应先确定改动属于哪个纵向范围，再进入对应势力范围，不需要默认读取
其他所有 Day 的文件。

## 4.1 推荐的人工审查暨学习顺序

第一次完整理解 Day 1～7 时，不建议按目录顺序阅读，也不建议直接从最复杂的
`AzureCostProvider.cs` 开始。最合适的方式是沿着真实请求和数据流，从外到内、
再从内回到验证证据。

整套顺序分为两个循环：

```text
第一轮：建立地图，知道系统为什么存在、如何启动、数据如何流动。
第二轮：沿每条业务链逐行审查，验证边界、失败、幂等和清理。
```

### 总体路线

| 顺序 | 学习站点 | 主要问题 | 建议深度 |
| --- | --- | --- | --- |
| 0 | 项目目标和完成边界 | 做什么、不做什么、Day 1～7 到哪里结束 | 快速 |
| 1 | 解决方案和依赖方向 | 有哪些项目、谁可以依赖谁 | 标准 |
| 2 | 本地运行底座 | PostgreSQL、配置、健康检查如何工作 | 标准 |
| 3 | 两个程序入口 | API 和 Worker 如何启动及装配依赖 | 深度 |
| 4 | Day 3 认证链 | 一个最短 Azure 调用如何穿过各层 | 标准 |
| 5 | Day 4 资源数据链 | Azure 资源如何读取、归一化、入库 | 深度 |
| 6 | Day 5 ETL 可靠性链 | 执行历史和失败如何被记录 | 深度 |
| 7 | Day 6 成本写入链 | 成本粒度、fallback、Upsert 如何工作 | 深度 |
| 8 | Day 7 成本查询链 | 聚合、日期、币种和百分比如何工作 | 深度 |
| 9 | EF 模型和 migration | 代码模型如何成为 PostgreSQL schema | 标准 |
| 10 | Terraform Azure 链 | 测试资源从创建到销毁如何闭环 | 深度 |
| 11 | 测试和 E2E 证据 | 上述理解是否被自动化证据支持 | 深度 |
| 12 | 文档一致性和最终复盘 | 文档、代码、脚本是否说同一件事 | 标准 |

### 第 0 站：先确定项目目标和 Day 1～7 边界

按顺序阅读：

1. `README.md`
2. `outline.md`
3. `construction/01-★★★-construction-plan.md`，只读 Day 1～7 基线相关内容
4. `docs/01-★★-project-feasibility-review.md`，重点看当前完成度和范围风险

需要回答：

- 这个项目当前是 Azure 数据底座，还是已经完成多云平台？
- Day 1～7 实际交付了哪些能力？
- React、AWS、合规、异常检测是否还只是后续计划？
- 作品集 MVP 和生产级平台的边界在哪里？

通过标准：

> 能用三句话准确介绍“项目目标、当前事实、下一阶段”，并且不夸大尚未实现的
> React、AWS、Policy 或 Config 能力。

### 第 1 站：理解解决方案骨架和依赖方向

按顺序阅读：

1. `FinOpsPlatform.slnx`
2. `Directory.Build.props`
3. 六个 `.csproj`
4. `global.json`
5. `dotnet-tools.json`

先画出：

```text
Api ─────────────┐
                 ├──> Infrastructure ──> Application ──> Domain
Worker ──────────┘
Tests ───────────────> Application + Infrastructure
```

需要回答：

- SLNX 和 `ProjectReference` 的职责有什么区别？
- 为什么 Domain 不应引用 EF Core 或 Azure SDK？
- 为什么 Infrastructure 同时引用 Application 和 Domain？
- `Directory.Build.props` 为什么会影响全部项目？
- `global.json` 和 `dotnet-tools.json` 分别控制什么？

通过标准：

> 不看文件也能画出项目依赖图，并能判断新增代码应该放在哪一层。

### 第 2 站：理解本地运行底座

按顺序阅读：

1. `.env.example`
2. `compose.yaml`
3. `src/FinOps.Api/appsettings.json`
4. `src/FinOps.Worker/appsettings.json`
5. 两个 `launchSettings.json`
6. `PostgreSqlHealthCheckOptions.cs`
7. `PostgreSqlHealthCheck.cs`
8. `docs/02-★★★-configuration-guide.md`

建议先执行无破坏性命令：

```powershell
docker compose config
docker compose up -d
docker compose ps
```

需要回答：

- 为什么没有 `.env` 也可以启动？
- Compose 默认值和 appsettings 默认值是否一致？
- 宿主机端口和容器端口有什么区别？
- `/health/live` 与 `/health` 分别证明什么？
- 本地密码为什么可以进示例配置，但生产密码不可以？

通过标准：

> 能解释从配置文件到 PostgreSQL connection string 的完整来源，并能判断数据库
> 不可用时 readiness 为什么失败。

### 第 3 站：理解 API、Worker 和依赖注入

按顺序阅读：

1. `src/FinOps.Infrastructure/DependencyInjection.cs`
2. `src/FinOps.Api/Program.cs`
3. `src/FinOps.Worker/Program.cs`
4. `src/FinOps.Worker/EtlWorkerOptions.cs`
5. `src/FinOps.Worker/Worker.cs`

这一步只建立控制流，不急着深入 Provider 和 Repository：

```text
Configuration
    ↓
DependencyInjection
    ↓
API endpoint / Worker Job
    ↓
Application service
```

需要回答：

- API 和 Worker 为什么共用 Infrastructure 注册？
- Scoped、Singleton、HttpClient 各自用于哪些对象？
- API 与 Worker 在哪里应用 migration？
- Worker 如何决定执行 Resources 还是 Costs？
- Worker 失败时如何设置非零退出码并停止进程？

通过标准：

> 能从一个 HTTP 请求或 Worker Job 的入口，指出下一步会调用哪个 Application
> 接口，而不是在 DI 注册中迷路。

### 第 4 站：用 Day 3 认证链学习最短垂直切片

按真实调用顺序阅读：

1. `Program.cs` 中 `/api/cloud/azure/subscriptions`
2. `IAzureSubscriptionReader.cs`
3. `AzureSubscriptionDto.cs`
4. `AzureSubscriptionReader.cs`
5. `AzureSubscriptionMapper.cs`
6. `DependencyInjection.cs` 中 Azure credential 注册
7. `AzureSubscriptionMapperTests.cs`
8. `scripts/Test-AzureSdkIntegration.ps1`
9. `docs/04-★★★-azure-integration.md` 的 Day 3 部分

调用链：

```text
HTTP endpoint
  → IAzureSubscriptionReader
  → AzureSubscriptionReader
  → ArmClient / DefaultAzureCredential
  → AzureSubscriptionMapper
  → AzureSubscriptionDto
  → HTTP response
```

需要回答：

- 为什么 API 不直接 `new ArmClient()`？
- 为什么 DTO 中没有 Azure SDK 类型？
- `DefaultAzureCredential` 如何使用当前 Azure CLI 身份？
- mapper 如何处理 Azure 返回的 null？
- E2E 如何证明返回值与 `az account show` 一致？

通过标准：

> 完整讲清一个外部 Azure 调用如何穿过 API、Application、Infrastructure 和测试。

### 第 5 站：沿 Day 4 资源链学习核心 ETL

按数据流顺序阅读：

1. `ICloudResourceInventoryProvider.cs`
2. `CloudResourceDto.cs`
3. `AzureResourceInventoryProvider.cs`
4. `CloudResource.cs`
5. `ICloudResourceRepository.cs`
6. `CloudResourceRepository.cs`
7. `CloudResourceConfiguration.cs`
8. `CloudResourceSyncService.cs`
9. `ICloudResourceSyncService.cs`
10. `CloudResourceSyncResult.cs`
11. `CloudResourceUpsertResult.cs`
12. `Worker.cs` 的 Resources 分支
13. `Program.cs` 的资源同步 endpoint

数据链：

```text
Resource Graph
  → CloudResourceDto
  → CloudResourceSyncService
  → CloudResource Domain
  → CloudResourceRepository
  → cloud_resources
```

需要回答：

- Resource Graph 如何分页？
- Azure resource ID 为什么要归一化？
- `FirstSeenAt` 和 `LastSeenAt` 为什么不能同样更新？
- Upsert 的唯一身份由什么保证？
- 第二次同步为什么应该 inserted=0？
- 资源在 Azure 中删除后，当前模型会发生什么？

通过标准：

> 能手画一条资源从 Azure 到 PostgreSQL 的字段映射，并指出幂等性由代码和数据库
> 哪两层共同保证。

### 第 6 站：沿 Day 5 理解 ETL 执行可靠性

按状态变化顺序阅读：

1. `EtlJobRun.cs`
2. `IEtlJobRunRepository.cs`
3. `EtlJobRunRepository.cs`
4. `EtlJobRunConfiguration.cs`
5. `EtlJobRunDto.cs`
6. 回看 `CloudResourceSyncService.cs`
7. `Program.cs` 中 `/api/admin/etl-runs`
8. `EtlJobRunTests.cs`
9. `CloudResourceSyncServiceTests.cs`
10. `scripts/Test-AzureResourceEtl.ps1`

状态链：

```text
Running
  ├── Succeeded + records_processed
  └── Failed + error_message
```

需要回答：

- 为什么 Job 要在调用 Azure 前先写 Running？
- 为什么失败记录使用独立 DbContext？
- Provider 失败和 Repository 失败如何进入 Failed？
- 如果记录 Failed 本身失败，会发生什么？
- 错误信息为什么截断而不保存完整 stack trace？

通过标准：

> 能解释“业务执行失败”和“失败状态持久化失败”是两个不同风险。

### 第 7 站：沿 Day 6 学习成本写入链

按数据流顺序阅读：

1. `ICloudCostProvider.cs`
2. `CloudCostDailyDto.cs`
3. `AzureCostOptions.cs`
4. `AzureCostProvider.cs`
5. `CloudCostDaily.cs`
6. `ICloudCostRepository.cs`
7. `CloudCostRepository.cs`
8. `CloudCostDailyConfiguration.cs`
9. `ICloudCostSyncService.cs`
10. `CloudCostSyncService.cs`
11. `CloudCostSyncResult.cs`
12. `CloudCostUpsertResult.cs`
13. `Program.cs` 的成本同步 endpoint

数据链：

```text
Azure Cost Management
  → 动态 columns/rows
  → CloudCostDailyDto
  → CloudCostDaily
  → CloudCostRepository
  → cloud_cost_daily
```

需要回答：

- 当前成本粒度到底是什么？
- 为什么不能据此声称精确到单资源或标签成本？
- 动态列为什么必须按列名映射，不能固定数组位置？
- 真实数据和 sample 数据如何区分？
- fallback 捕获范围是否可能掩盖程序错误？
- 成本复合唯一键包含哪些字段？

通过标准：

> 能明确说出当前系统“可以回答”和“不能回答”的成本问题，并能识别 sample
> provenance。

### 第 8 站：沿 Day 7 学习成本查询链

按请求顺序阅读：

1. `Program.cs` 中三个成本 GET endpoint
2. `ICloudCostQueryService.cs`
3. `CloudCostQueryService.cs`
4. `ICloudCostQueryRepository.cs`
5. `CloudCostQueryRepository.cs`
6. `CloudCostDailyPointDto.cs`
7. `CloudCostAggregateDto.cs`
8. `CloudCostBreakdownDto.cs`
9. `CloudCostQueryServiceTests.cs`
10. `scripts/Test-AzureCostEtl.ps1`

查询链：

```text
HTTP query parameters
  → CloudCostQueryService
  → CloudCostQueryRepository
  → PostgreSQL aggregation
  → Daily / Aggregate / Breakdown DTO
```

需要回答：

- 未传日期时为什么默认最近 7 天？
- from > to 时应该如何处理？
- 为什么不同币种不能直接相加？
- 百分比为什么必须在每种币种内部计算？
- Daily、Service、Resource Group 三类结果如何交叉核对总额？

通过标准：

> 给定一组多币种成本数据，能判断 API 聚合和百分比结果是否正确。

### 第 9 站：回头统一理解 EF 模型和 migration

此时已经理解业务，才阅读 schema：

1. `FinOpsDbContext.cs`
2. 三个 `Configurations/*.cs`
3. 三个非 Designer migration
4. `FinOpsDbContextModelSnapshot.cs`
5. `FinOpsDbContextFactory.cs`
6. `docs/05-★★★-data-model.md`

Designer 文件只做机械核对，不逐行学习。

需要回答：

- Domain 属性如何映射为 PostgreSQL 类型？
- 哪些唯一索引保证幂等性？
- `numeric(20,8)` 为什么适合当前成本数据？
- `jsonb` 存了什么，哪些字段不应该只藏在 JSON 中？
- 每个 migration 的 Up 和 Down 是否对称？
- Snapshot 为什么不是手工业务代码？

通过标准：

> 能从 Domain 实体推导出数据库表，并从唯一索引反推业务 identity。

### 第 10 站：独立学习 Terraform Azure 生命周期

按声明依赖顺序阅读：

1. `terraform/azure/providers.tf`
2. `terraform/azure/variables.tf`
3. `terraform/azure/main.tf`
4. `terraform/azure/outputs.tf`
5. `terraform/azure/terraform.tfvars.example`
6. `terraform/azure/.terraform.lock.hcl`，只看 Provider 版本，不读 hash
7. `terraform/azure/README.md`
8. `docs/03-★★-terraform.md`
9. `scripts/Test-AzureTerraformLifecycle.ps1`

生命周期：

```text
init → fmt/validate → plan → apply → Azure 核验 → destroy → 清理 state/plan
```

需要回答：

- 为什么需要随机后缀？
- common tags 如何合并？
- 哪些资源可能持续产生费用？
- Log Analytics 为什么默认关闭？
- output 如何被验收脚本使用？
- destroy 失败时如何避免误报成功？
- lockfile 为什么提交，而 state 为什么不能提交？

通过标准：

> 能在不查看脚本的情况下说出 Day 2 闭环的每一步，以及哪一步证明资源已真正
> 删除。

### 第 11 站：最后审查测试和端到端证据

不要一开始读完所有测试。完成对应生产链学习后，再按以下顺序回看：

1. Domain tests：证明不变量；
2. Application tests：证明编排和失败路径；
3. Infrastructure parser/mapper tests：证明外部数据映射；
4. Day 2～7 E2E：证明真实系统集成；
5. `tmp/day1-day7-review-report.md`：查看最近一次完整验收结果。

对每条测试都问：

- 这个测试防止哪一种真实回归？
- 它是在证明行为，还是只在复述实现？
- Stub 没有覆盖的外部风险由哪个 E2E 补上？
- E2E 结束后是否断言云资源、数据库和进程已清理？

通过标准：

> 能为每个核心生产文件指出至少一个对应测试或 E2E，并能指出当前尚未覆盖的
> 风险。

### 第 12 站：文档一致性和完整复盘

最后联合阅读：

1. `README.md`
2. `docs/02-★★★-configuration-guide.md`
3. `docs/04-★★★-azure-integration.md`
4. `docs/05-★★★-data-model.md`
5. `docs/03-★★-terraform.md`
6. `docs/01-★★-project-feasibility-review.md`

做一次反向复盘：

```text
文档声明
  → API / Worker 入口
  → Application 用例
  → Domain 不变量
  → Azure / PostgreSQL 适配
  → 测试和 E2E 证据
```

通过标准：

> 文档中的每个“已经完成”都能落到具体代码和验证证据；每个“未来计划”都没有
> 被误写成当前能力。

## 4.2 建议的实际学习节奏

不建议一天内读完 108 个文件。可以分为六个学习单元：

| 单元 | 内容 | 建议时间 | 产出 |
| --- | --- | --- | --- |
| A | 第 0～3 站：目标、工程、配置、入口 | 2～3 小时 | 架构图和启动流程 |
| B | 第 4 站：Azure 认证最短切片 | 1～1.5 小时 | 一条完整调用链 |
| C | 第 5～6 站：资源 ETL | 3～4 小时 | 资源数据流和状态机 |
| D | 第 7～8 站：成本 ETL 与查询 | 3～4 小时 | 成本粒度和聚合说明 |
| E | 第 9～10 站：数据库和 Terraform | 3～4 小时 | schema 与 IaC 生命周期 |
| F | 第 11～12 站：证据和复盘 | 2～3 小时 | 风险清单和最终理解 |

每个单元都建议留下三类笔记：

```text
我已经确认的事实
我仍然不理解的问题
我认为存在但尚未修改的风险
```

在完整学习结束前，不急着重构。先证明自己理解当前行为，再决定是否调整结构。

## 4.3 日常增量 Review 的顺序

以后不是每次都重走 12 站。面对一个新 diff，按以下顺序：

1. **先看变更文件列表**：确定命中哪个 Day 和势力范围；
2. **看契约变化**：DTO、接口、配置键、路由、数据库 identity；
3. **看核心实现**：只读被修改行为和直接调用者；
4. **看失败和边界**：空数据、非法输入、异常、并发、取消；
5. **看测试变化**：确认断言覆盖了本次风险；
6. **看外部影响**：Azure、PostgreSQL、Terraform、费用和清理；
7. **看文档事实**：用户可见行为是否需要同步；
8. **按影响矩阵运行验证**：不默认六个 E2E 全跑。

简化成一句话：

```text
先契约，后实现；先失败，后成功；先相关测试，后必要 E2E；最后核对文档和清理。
```

## 5. 全部文件的势力范围

## 5.1 Z0：项目治理与范围

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `README.md` | X | R2 | 当前能力与实际一致；命令可执行；不能把 React/AWS 计划写成已完成 |
| `outline.md` | 规划 | R1 | 项目愿景与生产化施工边界是否一致 |
| `construction/01-★★★-construction-plan.md` | 规划 | R2 | 阶段交付、验收标准、依赖顺序和范围膨胀 |
| `docs/01-★★-project-feasibility-review.md` | X | R1 | 评估事实是否仍有效；数字和计划是否过期 |
| `docs/02-★★★-configuration-guide.md` | D1～D7 | R1 | 配置字段解释是否与实际文件同步 |
| `construction/03-★★★-manual-review-boundaries.md` | X | R2 | 文件覆盖、顺序、星级和影响矩阵是否仍有效 |

### 联动规则

- 修改任何用户可见能力时，检查 `README.md`。
- 修改配置键时，检查 `docs/02-★★★-configuration-guide.md`。
- 修改工期或范围时，联合检查 `outline.md`、
  `construction/01-★★★-construction-plan.md` 和可行性报告。
- 这组文件不参与运行，但错误表述会直接损害演示可信度。

## 5.2 Z1：工具链与仓库卫生

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `.gitignore` | D1/X | R2 | secret、构建物、Terraform state、`tmp/` 是否完整排除 |
| `global.json` | D1 | R2 | SDK 基线和 roll-forward 是否符合 CI/开发机 |
| `dotnet-tools.json` | D4 | R2 | `dotnet-ef` 与 EF Core 包版本是否兼容 |
| `Directory.Build.props` | D1 | R2 | 全局框架、nullable、warning 策略的影响面 |
| `FinOpsPlatform.slnx` | D1 | R1 | 项目是否完整；不把它误当运行时依赖定义 |
| `src/FinOps.Api/FinOps.Api.csproj` | D1/X | R2 | Web SDK 和项目引用方向 |
| `src/FinOps.Application/FinOps.Application.csproj` | D1/X | R2 | 只能依赖 Domain |
| `src/FinOps.Domain/FinOps.Domain.csproj` | D1/X | R2 | 应保持无基础设施依赖 |
| `src/FinOps.Infrastructure/FinOps.Infrastructure.csproj` | D1～D7 | R3 | Azure、EF、Npgsql 包版本和传递依赖 |
| `src/FinOps.Tests/FinOps.Tests.csproj` | D1/X | R2 | 测试 SDK、xUnit、coverage 和被测项目 |
| `src/FinOps.Worker/FinOps.Worker.csproj` | D1/X | R2 | Worker SDK、Hosting 版本和项目引用 |

### 联动规则

- 任一 `.csproj` 包版本变化：必须 restore、build、test。
- `Directory.Build.props` 变化：视为全仓变更，所有项目都要校验。
- `global.json` 变化：检查开发机、CI、Docker SDK 版本是否一致。
- `.gitignore` 变化：执行 `git status --ignored` 和 secret/生成物检查。

## 5.3 Z2：本地运行与宿主配置

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `.env.example` | D1 | R2 | 只包含本地示例，不含真实 secret |
| `compose.yaml` | D1 | R3 | 镜像版本、端口、卷、健康检查、数据删除语义 |
| `src/FinOps.Api/appsettings.json` | D1/D3/D6 | R3 | 数据库、Tenant、sample fallback、开发密码边界 |
| `src/FinOps.Api/Properties/launchSettings.json` | D1 | R1 | 本地 URL 和环境名，不参与发布 |
| `src/FinOps.Worker/appsettings.json` | D1/D4/D6/D7 | R3 | 数据库、Azure、成本 fallback、Job 默认值 |
| `src/FinOps.Worker/Properties/launchSettings.json` | D1 | R1 | Worker 本地环境名 |
| `src/FinOps.Worker/EtlWorkerOptions.cs` | D7 | R2 | Job 名和 CostDays 默认值、合法范围 |

### 联动规则

- 新增或重命名配置键：同时检查 API、Worker、Options、DI、文档和脚本环境变量。
- PostgreSQL 默认值变化：联合检查 Compose、两个 appsettings、健康检查和脚本。
- `UseSampleDataWhenUnavailable` 变化：必须人工确认演示便利与数据真实性边界。

## 5.4 Z3：API 边界

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Api/Program.cs` | D1/D3/D5/D6/D7 | R3 | DI、migration、路由、参数默认值、异常、管理接口暴露 |
| `src/FinOps.Api/FinOps.Api.http` | D1/D3 | R1 | 手工请求是否仍匹配真实路由和端口 |

### `Program.cs` 的内部势力范围

当前一个文件包含多个边界，review 时应按段落拆开：

- D1：应用启动、ProblemDetails、migration、根路由、health；
- D3：`/api/cloud/azure/subscriptions`；
- D5：资源同步和 ETL 历史；
- D6/D7：成本同步与三个成本查询。

### 联动规则

- 路由或 DTO 变化：检查对应 Application 接口、测试、`.http`、README 和 E2E。
- 管理 POST 接口变化：检查并发、权限、超时、取消和失败响应。
- migration 启动策略变化：同时检查 Worker 和部署文档。

## 5.5 Z4：Application 契约与用例

### D3：Azure 订阅与云 Provider 契约

| 文件 | 等级 | 人工审查重点 |
| --- | --- | --- |
| `src/FinOps.Application/Cloud/Azure/AzureSubscriptionDto.cs` | R2 | API 暴露字段；Azure SDK 类型不能泄漏 |
| `src/FinOps.Application/Cloud/Azure/IAzureSubscriptionReader.cs` | R2 | 订阅读取契约和取消 |
| `src/FinOps.Application/Cloud/ICloudResourceInventoryProvider.cs` | R3 | Provider 无关的资源读取边界 |
| `src/FinOps.Application/Cloud/ICloudCostProvider.cs` | R3 | 日期范围、返回粒度、Provider 无关性 |
| `src/FinOps.Application/Cloud/ICloudComplianceProvider.cs` | R2 | 尚未实现的预留契约，避免提前扩张 |
| `src/FinOps.Application/Cloud/ComplianceFindingDto.cs` | R2 | Day 9 以后才会正式使用；当前只是契约占位 |

### D4/D5：资源同步

| 文件 | 等级 | 人工审查重点 |
| --- | --- | --- |
| `src/FinOps.Application/Cloud/CloudResourceDto.cs` | R3 | 归一化资源字段和必填语义 |
| `src/FinOps.Application/Cloud/ICloudResourceRepository.cs` | R3 | Upsert 事务边界 |
| `src/FinOps.Application/Cloud/ICloudResourceSyncService.cs` | R2 | 用例入口 |
| `src/FinOps.Application/Cloud/CloudResourceSyncService.cs` | R3 | Job 创建、Provider 调用、Upsert、成功/失败记录 |
| `src/FinOps.Application/Cloud/CloudResourceSyncResult.cs` | R2 | 对外统计语义 |
| `src/FinOps.Application/Cloud/CloudResourceUpsertResult.cs` | R2 | inserted/updated 计数契约 |

### D6/D7：成本同步和查询

| 文件 | 等级 | 人工审查重点 |
| --- | --- | --- |
| `src/FinOps.Application/Cloud/CloudCostDailyDto.cs` | R3 | 成本粒度、币种、来源数据 |
| `src/FinOps.Application/Cloud/ICloudCostRepository.cs` | R3 | 成本 Upsert 契约 |
| `src/FinOps.Application/Cloud/ICloudCostSyncService.cs` | R2 | 同步入口和 days 语义 |
| `src/FinOps.Application/Cloud/CloudCostSyncService.cs` | R3 | 日期窗口、provenance、Job 状态、异常路径 |
| `src/FinOps.Application/Cloud/CloudCostSyncResult.cs` | R2 | sample 标记和统计 |
| `src/FinOps.Application/Cloud/CloudCostUpsertResult.cs` | R2 | inserted/updated 计数 |
| `src/FinOps.Application/Cloud/ICloudCostQueryRepository.cs` | R3 | 查询参数和聚合输入 |
| `src/FinOps.Application/Cloud/ICloudCostQueryService.cs` | R2 | 查询用例边界 |
| `src/FinOps.Application/Cloud/CloudCostQueryService.cs` | R3 | 默认 7 天、日期验证、Provider 规范化 |
| `src/FinOps.Application/Cloud/CloudCostAggregateDto.cs` | R2 | 分组总额输出 |
| `src/FinOps.Application/Cloud/CloudCostBreakdownDto.cs` | R3 | 百分比语义和币种隔离 |
| `src/FinOps.Application/Cloud/CloudCostDailyPointDto.cs` | R2 | 日趋势输出 |

### D5/D7：ETL 执行历史

| 文件 | 等级 | 人工审查重点 |
| --- | --- | --- |
| `src/FinOps.Application/Etl/EtlJobRunDto.cs` | R2 | 状态、时间和错误信息的 API 表达 |
| `src/FinOps.Application/Etl/IEtlJobRunRepository.cs` | R3 | Start/Complete/Fail/GetRecent 的一致性 |

### 联动规则

- DTO 字段变化：必须检查 Domain、Infrastructure 映射、API 和测试。
- 接口变化：必须检查所有实现与 stub，不接受只让编译通过。
- SyncService 变化：必须同时检查成功、Provider 失败、Repository 失败和 Job
  状态记录。
- 成本查询变化：必须检查多币种，不能跨币种计算百分比。

## 5.6 Z5：Domain 业务不变量

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Domain/CloudResources/CloudResource.cs` | D4/D5 | R3 | ID 归一化、FirstSeen/LastSeen、更新语义 |
| `src/FinOps.Domain/Costs/CloudCostDaily.cs` | D6/D7 | R3 | identity、币种、resource group 缺省、raw JSON |
| `src/FinOps.Domain/Etl/EtlJobRun.cs` | D5/D7 | R3 | Running→Succeeded/Failed 状态机、错误截断 |

### 联动规则

- Domain 改动必须有对应 Domain 测试。
- 修改 identity 或 normalize 规则：视为数据兼容性变更，检查唯一索引和已有数据。
- 修改状态机：检查所有调用者和失败路径，不能只测成功路径。
- Domain 不得引用 EF、Azure、ASP.NET 或 Npgsql 类型。

## 5.7 Z6：Azure 外部适配

### 认证与订阅

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Infrastructure/Azure/AzureSubscriptionReader.cs` | D3 | R4 | credential、订阅枚举、取消、SDK 边界 |
| `src/FinOps.Infrastructure/Azure/AzureSubscriptionMapper.cs` | D3 | R3 | null/default 映射和 SDK 类型隔离 |
| `src/FinOps.Infrastructure/Properties/AssemblyInfo.cs` | D3 | R1 | InternalsVisibleTo 只开放给测试程序集 |

### 资源清单

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Infrastructure/Azure/AzureResourceInventoryProvider.cs` | D4 | R4 | Kusto、分页、skip token、字段缺失、标签 JSON |

### 成本

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Infrastructure/Azure/AzureCostOptions.cs` | D6 | R3 | fallback 和 force sample 开关 |
| `src/FinOps.Infrastructure/Azure/AzureCostProvider.cs` | D6/D7 | R4 | token scope、API version、请求粒度、动态列映射、fallback |

### 依赖装配

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Infrastructure/DependencyInjection.cs` | D1～D7 | R4 | credential 生命周期、HTTP client、Provider 注册、Repository 注册 |

### 联动规则

- Azure API version、scope 或查询变化：必须运行真实 Azure E2E。
- fallback 捕获范围变化：检查是否吞掉了程序 bug 或数据解析错误。
- Resource Graph 投影变化：同步检查 DTO、Domain、Repository 和解析测试。
- DI 变化：API 与 Worker 都必须启动验证。

## 5.8 Z7：PostgreSQL 与 EF Core

### 健康检查和连接

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Infrastructure/HealthChecks/PostgreSqlHealthCheck.cs` | D1 | R3 | 真实连接、SELECT 1、错误分类和敏感信息 |
| `src/FinOps.Infrastructure/HealthChecks/PostgreSqlHealthCheckOptions.cs` | D1 | R3 | connection string 构造、超时、默认值 |

### DbContext

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Infrastructure/Persistence/FinOpsDbContext.cs` | D4～D7 | R4 | DbSet、映射注册、schema 总入口 |
| `src/FinOps.Infrastructure/Persistence/FinOpsDbContextFactory.cs` | D4 | R3 | 设计时配置是否与运行时足够一致 |

### Repository

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Infrastructure/Persistence/CloudResourceRepository.cs` | D4/D5 | R4 | 批量查询、Upsert、并发、计数、时间 |
| `src/FinOps.Infrastructure/Persistence/CloudCostRepository.cs` | D6/D7 | R4 | 复合 identity、日期窗口、更新语义 |
| `src/FinOps.Infrastructure/Persistence/CloudCostQueryRepository.cs` | D7 | R4 | SQL/EF 聚合、币种隔离、过滤边界 |
| `src/FinOps.Infrastructure/Persistence/EtlJobRunRepository.cs` | D5/D7 | R4 | 独立 DbContext、失败状态能否可靠保存 |

### EF 映射

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Infrastructure/Persistence/Configurations/CloudResourceConfiguration.cs` | D4 | R4 | 长度、jsonb、唯一索引、时间类型 |
| `src/FinOps.Infrastructure/Persistence/Configurations/EtlJobRunConfiguration.cs` | D5 | R4 | 状态、错误长度、历史查询索引 |
| `src/FinOps.Infrastructure/Persistence/Configurations/CloudCostDailyConfiguration.cs` | D6 | R4 | numeric 精度、复合唯一索引、jsonb |

### 手写 migration

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Infrastructure/Persistence/Migrations/20260612213537_InitialCloudResources.cs` | D4 | R3 | Up/Down、表字段、唯一索引 |
| `src/FinOps.Infrastructure/Persistence/Migrations/20260612220117_AddEtlJobRuns.cs` | D5 | R3 | 表、索引、回滚 |
| `src/FinOps.Infrastructure/Persistence/Migrations/20260612224510_AddCloudCostDaily.cs` | D6 | R3 | 精度、唯一索引、回滚 |

### EF 工具生成物

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Infrastructure/Persistence/Migrations/20260612213537_InitialCloudResources.Designer.cs` | D4 | R0 | 不逐行；确认与 migration 成对且非异常全量重写 |
| `src/FinOps.Infrastructure/Persistence/Migrations/20260612220117_AddEtlJobRuns.Designer.cs` | D5 | R0 | 同上 |
| `src/FinOps.Infrastructure/Persistence/Migrations/20260612224510_AddCloudCostDaily.Designer.cs` | D6 | R0 | 同上 |
| `src/FinOps.Infrastructure/Persistence/Migrations/FinOpsDbContextModelSnapshot.cs` | D4～D6 | R0/R3 | 通常不逐行；模型变更时重点核对表、索引、精度和删除项 |

### 联动规则

- Domain identity 或 EF Configuration 变化：必须生成 migration，不允许手工只改
  snapshot。
- migration review 顺序：
  1. 先审 Domain 和 Configuration；
  2. 再审手写 migration 的 Up/Down；
  3. 最后机械检查 Designer/Snapshot。
- Repository 变化：至少运行对应 Application 测试和真实 PostgreSQL E2E。
- 唯一索引变化：必须讨论历史数据迁移、重复数据和并发。

## 5.9 Z8：Worker 执行控制

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `src/FinOps.Worker/Program.cs` | D1/D4/D5/D7 | R3 | DI、Options、HostedService 和 TimeProvider |
| `src/FinOps.Worker/Worker.cs` | D1/D4/D5/D7 | R3 | migration、Job 分派、取消、异常、退出码、停止 |

### 联动规则

- 新增 Job：不能只在 `Worker.cs` 增加分支，还要补 Options、配置、DI、日志和 E2E。
- 异常处理变化：验证进程退出码非零且 ETL failure 已持久化。
- Worker 生命周期变化：确认一次性任务不会常驻，也不会在任务完成前退出。

## 5.10 Z9：自动化测试

### Application 测试

| 文件 | 对应生产代码 | 等级 | 重点 |
| --- | --- | --- | --- |
| `src/FinOps.Tests/Application/CloudResourceSyncServiceTests.cs` | Resource Sync | R3 | 成功、Provider/Repository 失败、Job 状态 |
| `src/FinOps.Tests/Application/CloudCostSyncServiceTests.cs` | Cost Sync | R3 | 日期、provenance、成功失败、Job 状态 |
| `src/FinOps.Tests/Application/CloudCostQueryServiceTests.cs` | Cost Query | R3 | 默认范围、Provider、多币种百分比 |

### Domain 测试

| 文件 | 对应生产代码 | 等级 | 重点 |
| --- | --- | --- | --- |
| `src/FinOps.Tests/Domain/CloudResourceTests.cs` | CloudResource | R3 | ID 归一化和观察时间 |
| `src/FinOps.Tests/Domain/CloudCostDailyTests.cs` | CloudCostDaily | R3 | 币种和缺省 Resource Group |
| `src/FinOps.Tests/Domain/EtlJobRunTests.cs` | EtlJobRun | R3 | 状态终结和错误截断 |

### Infrastructure 测试

| 文件 | 对应生产代码 | 等级 | 重点 |
| --- | --- | --- | --- |
| `src/FinOps.Tests/Infrastructure/AzureSubscriptionMapperTests.cs` | Subscription Mapper | R2 | SDK null/default 映射 |
| `src/FinOps.Tests/Infrastructure/AzureResourceInventoryProviderTests.cs` | Resource Graph Parser | R3 | 动态 JSON、缺字段、映射 |
| `src/FinOps.Tests/Infrastructure/AzureCostProviderTests.cs` | Cost Parser/Sample | R3 | 动态列顺序、样例数据 |
| `src/FinOps.Tests/Infrastructure/PostgreSqlHealthCheckOptionsTests.cs` | PG Options | R2 | connection string |

### 测试 review 原则

- 测试不是生产代码的附属文件，而是行为证据。
- 不能只看测试“是否存在”，要检查断言是否覆盖改动风险。
- Stub 只能验证应用编排；不能替代 PostgreSQL、Azure 或 HTTP 真实闭环。
- 纯重构若行为不变，可只运行相关测试；契约和数据模型变化必须扩大测试范围。

## 5.11 Z9：真实端到端脚本

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `scripts/Test-AzureTerraformLifecycle.ps1` | D2 | R4 | 创建、核验、destroy、异常时资源是否遗留 |
| `scripts/Test-AzureSdkIntegration.ps1` | D3 | R4 | API 与 `az account show` 对比、进程清理 |
| `scripts/Test-AzureResourceInventory.ps1` | D4 | R4 | 临时 Azure 资源、双跑幂等、DB/云清理 |
| `scripts/Test-AzureResourceEtl.ps1` | D5 | R4 | Worker/API、成功历史、强制失败、DB 清理 |
| `scripts/Test-AzureCostPoc.ps1` | D6 | R4 | 真实成本、强制 sample、幂等、DB 清理 |
| `scripts/Test-AzureCostEtl.ps1` | D7 | R4 | Cost Worker、API 重跑、三类查询总额一致 |

### 脚本联动规则

- 脚本中任何 Azure 创建操作都必须有 `finally` 或等价清理路径。
- 修改端口、数据库名、配置键或 API 路由时，脚本必须同步。
- E2E 失败后首先审查“是否遗留资源”，再分析功能失败。
- 不应每次普通代码 review 都执行六个真实脚本，应按影响矩阵选择。

## 5.12 Z10：Terraform Azure

| 文件 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- |
| `terraform/azure/providers.tf` | D2 | R3 | Terraform 和 Provider 版本约束 |
| `terraform/azure/variables.tf` | D2 | R3 | 默认区域、命名、费用开关、校验 |
| `terraform/azure/main.tf` | D2 | R4 | 实际资源、SKU、安全配置、标签、持续费用 |
| `terraform/azure/outputs.tf` | D2 | R2 | 脚本依赖的输出是否稳定 |
| `terraform/azure/terraform.tfvars.example` | D2 | R2 | 示例安全、默认成本可控 |
| `terraform/azure/README.md` | D2 | R1 | 手工命令与脚本是否一致 |
| `terraform/azure/.terraform.lock.hcl` | D2 | R0/R2 | 不逐 hash；审 Provider 版本变化和来源 |
| `docs/03-★★-terraform.md` | D2 | R1 | 生命周期、认证、费用和清理说明 |

### 联动规则

- `main.tf` 资源变化：必须 fmt、validate、plan，并人工审费用和 destroy。
- output 名称变化：检查生命周期脚本和文档。
- lockfile 大量变化：确认来自预期的 `terraform init -upgrade`，否则拒绝。
- 默认启用会持续收费的资源属于 R4 变更。

## 5.13 各业务域专题文档

| 文件 | 归属势力 | Day | 等级 | 人工审查重点 |
| --- | --- | --- | --- | --- |
| `docs/04-★★★-azure-integration.md` | Z6 | D3～D7 | R1/R2 | 认证、Provider、Resource Graph、Cost 行为 |
| `docs/05-★★★-data-model.md` | Z5/Z7 | D4～D7 | R2 | 字段、唯一键、状态和 migration 是否同步 |

这两份文档跟随对应代码审查，不应单独形成一次“全量文档 review”。

## 6. 文件变化到 review 范围的路由

## 6.1 最小影响矩阵

| 改动类型 | 必审文件 | 必跑机械检查 | 真实 E2E |
| --- | --- | --- | --- |
| 只改说明文字 | 目标文档 + 被描述配置/接口 | `git diff --check`、链接检查 | 不需要 |
| 只改 DTO | DTO、调用方、映射、API 契约、相关测试 | build + related tests | 视外部契约决定 |
| 改 Domain | Domain、Configuration、Repository、migration、Domain tests | full build/test | 对应 D4/D5/D6/D7 |
| 改 SyncService | Service、接口、Job Repo、对应 tests | related tests | 对应资源或成本 E2E |
| 改 Azure Provider | Provider、DTO、Options、DI、parser tests | full tests | 必须跑对应真实 Azure E2E |
| 改 Repository | Repository、Domain identity、EF Configuration、tests | PostgreSQL + tests | 对应 ETL E2E |
| 改 API 路由 | Program、接口/DTO、`.http`、README、脚本 | build/test | 调用该路由的 E2E |
| 改 Worker | Worker、Options、配置、DI、脚本 | build/test | 对应 Worker E2E |
| 改 Terraform | `.tf`、outputs、脚本、文档、lockfile | fmt/validate/plan | D2 lifecycle |
| 改配置键 | 两宿主配置、Options、DI、Compose、文档、脚本 | JSON/YAML + build | 启动相关宿主 |
| 只变 Designer/Snapshot | 先找对应 Domain/Configuration/migration 来源 | migration 校验 | 通常不单独跑 |

## 6.2 Review 不应自动扩散的边界

- 修改成本查询，不默认 review Terraform。
- 修改 README，不默认跑 Azure E2E。
- 修改 Resource Graph 解析，不默认 review成本 Repository。
- 修改 Worker 日志文字，不默认 review所有 Domain。
- lockfile 未变化时，不重新审查每个 Provider hash。
- migration Designer 文件不作为独立业务逻辑阅读。
- 测试脚本未受配置、路由、资源或行为影响时，不需要六个全部重跑。

## 7. 人工 Review 套餐

## 7.1 快速套餐：15～30 分钟

适用：

- 文档；
- 注释；
- 启动说明；
- 非行为配置说明；
- 测试名称或日志文字。

动作：

1. 查看 diff；
2. 检查事实、路径、命令和 secret；
3. 运行格式/解析检查；
4. 不运行云端 E2E。

## 7.2 标准套餐：45～90 分钟

适用：

- 单个 Application 用例；
- DTO；
- Domain 小改动；
- API 查询；
- Options；
- 单个 Repository 的非 schema 变更。

动作：

1. 只读所属势力范围和直接联动文件；
2. 检查成功、空数据、非法输入和失败路径；
3. 运行相关单元测试；
4. build；
5. 只有命中外部边界时才跑对应 E2E。

## 7.3 深度套餐：2～4 小时

适用：

- Azure Provider；
- Repository identity；
- EF schema/migration；
- Worker Job；
- 管理 API；
- Terraform；
- E2E 脚本。

动作：

1. 审业务语义和外部系统契约；
2. 审数据一致性、费用、安全、并发和清理；
3. 运行全量单元测试；
4. 运行命中的真实 E2E；
5. 验证 Azure、PostgreSQL、进程和本地产物清理。

## 8. 减少人工复杂性的候选措施

以下是候选建议，目前尚未实施对应的工程改造。

### 8.1 建立 `review-map`

将本稿收敛成稳定的 `construction/review-map.md`，保留：

- 势力范围；
- 风险等级；
- 影响矩阵；
- 每个范围的负责人式检查清单。

逐文件明细可由脚本生成，避免文档过期。

### 8.2 增加路径到验证命令的映射

例如：

```text
src/FinOps.Domain/**                  -> domain tests + full build
src/FinOps.Infrastructure/Azure/**   -> Azure tests + 对应 Azure E2E
terraform/azure/**                   -> fmt + validate + Day 2 E2E
src/**/Persistence/**                -> tests + 对应 PostgreSQL E2E
docs/**                              -> link + diff check
construction/**                      -> link + diff check
```

以后根据 `git diff --name-only` 自动输出“本次应该审什么、跑什么”。

### 8.3 将静态检查合并成单一入口

候选脚本：

```text
scripts/Test-RepositoryStatic.ps1
```

只做无云费用检查：

- JSON/XML/YAML/PowerShell 解析；
- `dotnet format --verify-no-changes`；
- restore/build/test；
- Terraform fmt/validate；
- Git 垃圾与 secret 模式检查。

人工 review 不再重复执行零散命令。

### 8.4 E2E 按能力分组，不默认全跑

建议保留三组：

```text
Foundation：Day 1 + Day 3
Resources：Day 2 + Day 4 + Day 5
Costs：Day 6 + Day 7
```

只有跨域、发布前或基础配置变化时才运行全部。

### 8.5 为生成物建立明确规则

R0 文件只接受以下审查：

- 是否由官方工具生成；
- 是否与一个明确源变更成对出现；
- 是否出现异常删除、重命名或全量重写；
- 生成后 migration 是否可应用。

不再人工阅读每一行 Designer、Snapshot hash 或 Terraform checksum。

### 8.6 补齐架构边界测试

未来可以增加自动测试，确保：

- Domain 不引用 Application/Infrastructure/API；
- Application 不引用 Infrastructure/Azure SDK；
- API 不直接引用 Azure SDK 类型；
- Infrastructure 是唯一 Azure SDK 所在层。

这样依赖方向从“每次人工记忆”变成“测试自动阻止”。

### 8.7 给高风险行为增加固定断言

未来 E2E 应统一断言：

- 临时数据库已删除；
- 临时 Resource Group 已删除；
- Terraform state 为空；
- API/Worker 进程已停止；
- sample 数据带明确 provenance；
- 重跑不产生重复记录；
- 失败执行有 `Failed` 记录。

这些断言自动化后，人工只需要判断断言设计是否正确。

## 9. 当前最值得人工关注的文件

下面是**风险优先级**，不是第一次学习顺序。第一次学习应使用第 4.1 节的
12 站路线；只有时间不足或做发布前风险抽查时，才优先深审以下文件。

如果只允许人工深审 15 个文件，优先顺序建议如下：

1. `src/FinOps.Api/Program.cs`
2. `src/FinOps.Infrastructure/DependencyInjection.cs`
3. `src/FinOps.Worker/Worker.cs`
4. `src/FinOps.Application/Cloud/CloudResourceSyncService.cs`
5. `src/FinOps.Application/Cloud/CloudCostSyncService.cs`
6. `src/FinOps.Application/Cloud/CloudCostQueryService.cs`
7. `src/FinOps.Infrastructure/Azure/AzureResourceInventoryProvider.cs`
8. `src/FinOps.Infrastructure/Azure/AzureCostProvider.cs`
9. `src/FinOps.Infrastructure/Persistence/CloudResourceRepository.cs`
10. `src/FinOps.Infrastructure/Persistence/CloudCostRepository.cs`
11. `src/FinOps.Infrastructure/Persistence/CloudCostQueryRepository.cs`
12. `src/FinOps.Infrastructure/Persistence/EtlJobRunRepository.cs`
13. `src/FinOps.Domain/CloudResources/CloudResource.cs`
14. `src/FinOps.Domain/Costs/CloudCostDaily.cs`
15. `terraform/azure/main.tf`

但这不表示其他文件可以忽略，而是这 15 个文件承载了大部分行为风险。

## 10. 当前结论

原 Day 1～7 基线的 108 个文件不应该被当成 108 个同等重要的 review 单元。
合理拆分后：

- 约 15 个文件是核心高风险行为；
- Domain、Application 契约、EF Configuration 和 Worker 属于强联动区；
- 测试和 E2E 是行为证据，应随生产域进入 review；
- 文档和配置按事实同步审查；
- migration Designer、Snapshot 和 lockfile 主要使用机械审查；
- 每次 review 应由“变化路径”决定范围，而不是默认全仓重读。

后续迭代需要重点讨论：

1. 势力范围是否要按目录，还是按 Resource/Cost/ETL 三条业务链进一步拆分；
2. `Program.cs` 和 `DependencyInjection.cs` 是否应该拆文件以降低跨 Day 冲突；
3. 是否为每个势力范围建立固定 checklist；
4. 是否实现一个根据 Git diff 自动推荐测试范围的脚本；
5. 第 4.1 节的 12 站学习路线是否需要拆成独立的打卡清单。
