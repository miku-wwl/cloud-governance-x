# 01 ★★★ Day 8：当前能力真值与生产差距

> 本日目标：把 Day 1～7 的工程事实冻结成可审查的能力基线。
>
> 本日不做：新增业务功能、架构重构、认证、多租户、调度或云端部署。

## 1. 完成定义

Day 8 完成时，任何人只阅读能力基线，就能准确区分：

- 已实现且有证据的能力；
- 已实现但仅适合本地或受控验证的能力；
- 代码中存在但尚未达到生产语义的能力；
- 尚未实现的规划；
- 明确禁止进入生产的行为；
- 每项结论由哪个代码、测试、脚本或文档证明。

## 2. 前置条件

- 当前分支和远程状态已知；
- 工作区中的用户修改已识别；
- 已阅读：
  - `outline.md`
  - `construction/01-★★★-construction-plan.md`
  - `construction/02-★★★-day8-production-roadmap.md`
  - `README.md`
  - `docs/01-★★-project-feasibility-review.md`
  - `construction/03-★★★-manual-review-boundaries.md`
- 不使用历史 30 天计划作为当前生产目标；
- 不把“计划实现”写成“当前实现”。

## 3. 计划交付物

### 3.1 永久交付物

```text
docs/phase-0/current-capability-baseline.md
docs/phase-0/production-gap-register.md
```

同时按事实更新：

- `README.md`
- `docs/01-★★-project-feasibility-review.md`
- 其他仍把旧 Day 8～30 描述为现行计划的文档

历史评估可以保留，但必须明确标注：

```text
这是基于旧 30 天作品集计划的历史评估。
当前生产化路线以 outline.md、
construction/01-★★★-construction-plan.md 和
construction/02-★★★-day8-production-roadmap.md 为准。
```

### 3.2 临时交付物

```text
tmp/day08-closeout-report.md
tmp/phase-0-evidence/day08-tracked-files.txt
tmp/phase-0-evidence/day08-document-claims.txt
```

## 4. 施工步骤

### 4.1 冻结起点

记录当前分支、Commit 和工作区：

```powershell
git status --short --branch
git log -1 --oneline --decorate
git rev-parse HEAD
git remote -v
```

验收重点：

- 不覆盖用户尚未提交的修改；
- 报告记录准确 Commit；
- Day 8 的结论只针对该 Commit 和本日修改后的结果。

### 4.2 建立 Git 文件清单

```powershell
git ls-files | Sort-Object
git ls-files | Measure-Object
git status --ignored --short
```

将跟踪文件按以下范围分类：

| 范围 | 主要内容 |
| --- | --- |
| 根配置 | solution、MSBuild、Compose、工具和环境示例 |
| API | HTTP 路由、健康检查、启动和 migration |
| Worker | 一次性 ETL Job 执行 |
| Application | 用例、DTO 和端口 |
| Domain | 资源、成本和 ETL 不变量 |
| Infrastructure | Azure、PostgreSQL、EF Core 和 DI |
| Tests | 单元和解析测试 |
| Scripts | Day 2～7 真实 E2E |
| Terraform | Azure 临时基础设施 |
| Docs | 当前说明、历史评估和 review 路线 |

不要把以下内容算入产品源文件：

- `bin/`、`obj/`；
- `.terraform/`、`*.tfstate`、`*.tfplan`；
- `tmp/`；
- 日志、coverage、TestResults；
- IDE 配置和用户文件。

### 4.3 建立能力状态词典

能力真值表只能使用以下状态：

| 状态 | 定义 |
| --- | --- |
| `VerifiedBaseline` | 当前 Commit 上有自动或真实 E2E 证据 |
| `ImplementedLimited` | 有实现，但只适合本地或语义受限 |
| `PresentUnverified` | 代码存在，但本轮尚未取得有效证据 |
| `Planned` | 只存在于纲领或施工计划 |
| `ProductionProhibited` | 当前行为明确不能进入生产 |
| `DeprecatedHistorical` | 旧计划或旧表述，仅保留历史价值 |

禁止使用模糊词：

- “基本完成”；
- “应该可以”；
- “大概支持”；
- “生产可用雏形”；
- “未来稍微改一下即可”。

### 4.4 逐域建立能力真值表

`current-capability-baseline.md` 至少包含以下列：

| 字段 | 说明 |
| --- | --- |
| 能力域 | Foundation、Resource、Cost、ETL、API 等 |
| 能力 | 可被单独验证的行为 |
| 当前状态 | 使用统一状态词典 |
| 当前实现 | 只写代码真实行为 |
| 证据 | 文件、测试、脚本或运行报告 |
| 当前限制 | 安全、数据、可靠性、规模或环境限制 |
| 生产结论 | 允许、受限或禁止 |
| 后续阶段 | 在哪个阶段解决 |

必须覆盖以下能力：

#### 工程基础

- .NET 10 solution 和项目边界；
- API、Worker、Application、Domain、Infrastructure、Tests；
- PostgreSQL Compose；
- 配置覆盖方式；
- health/live 和 readiness；
- EF Core migration；
- warnings as errors；
- 本地工具版本固定。

#### Azure 与 Terraform

- Azure CLI 本地身份；
- `DefaultAzureCredential`；
- subscription 读取；
- Terraform Resource Group；
- Storage Account；
- Service Bus Namespace 和 Queue；
- Terraform apply/destroy；
- 本地 state；
- 真实资源清理。

#### 资源数据

- Resource Graph；
- 多 subscription 枚举的当前行为；
- 分页；
- 资源字段映射；
- PostgreSQL Upsert；
- 唯一键；
- `FirstSeenAt` / `LastSeenAt`；
- 重跑幂等；
- 当前缺少 inactive/deleted 语义。

#### 成本数据

- Cost Management Query API；
- 最近 7 天；
- ServiceName / ResourceGroup 分组；
- 币种；
- sample fallback；
- `ForceSampleData`；
- Upsert 幂等；
- daily/service/resource-group 查询；
- 当前成本粒度限制；
- 当前缺少成本类型、账期修订和多租户。

#### ETL

- Worker 触发；
- 管理 API 触发；
- `etl_job_runs`；
- 成功和失败状态；
- records processed；
- 当前缺少 scheduler、lease、checkpoint、retry policy 和并发控制。

#### 测试与运行

- 自动化测试；
- 六个 E2E 脚本；
- 测试数据库隔离；
- 临时 Azure 资源；
- 进程和日志清理；
- 当前缺少 CI、staging、SLO、备份和恢复。

#### 尚未实现

- 用户认证；
- RBAC；
- organization / tenant；
- audit；
- Azure Policy；
- Azure Monitor；
- finding 与 waiver；
- React 前端；
- outbox / inbox；
- 通知；
- AWS；
- 生产容器和平台；
- CI/CD；
- DR。

### 4.5 建立生产差距表

`production-gap-register.md` 不是完整风险登记册。它负责把能力差距映射到未来
阶段，Day 11 再补 Owner、严重度和治理状态。

至少包含：

| 差距 | 当前证据 | 为什么阻止生产 | 目标阶段 | 依赖 |
| --- | --- | --- | --- | --- |
| 管理 API 匿名 | `Program.cs` 路由无 auth | 任意调用者可触发云 API 和写库 | 阶段 2 | identity/RBAC |
| 无 tenant_id | 当前 schema | 无法证明组织间隔离 | 阶段 2～3 | tenancy ADR |
| 宿主自动 migration | API/Worker 启动代码 | 多实例竞争且运行身份权限过大 | 阶段 1 | Migration Host |
| sample fallback | Azure Cost 配置与 Provider | Provider 故障可能表现为成功 | 阶段 5 | 环境物理隔离 |
| 无调度和租约 | 一次性 Worker/API | 并发、重试和恢复不可控 | 阶段 4 | Job 模型 |
| 资源不失活 | 当前资源模型 | 已删除资源继续参与统计 | 阶段 3/5 | scan lifecycle |
| 成本粒度有限 | 当前唯一键和查询维度 | 不能声称单资源精确归因 | 阶段 3/6 | 成本语义 |
| 本地 Terraform state | 当前 Terraform | 团队协作和恢复风险 | 阶段 12 | remote backend |

### 4.6 审查所有能力声明

搜索可能过时或夸大的描述：

```powershell
rg -n `
  "生产级|生产可用|多云|React|AWS|RBAC|多租户|30 天|Day 8|Day 30|已完成" `
  README.md outline.md construction docs
```

逐条判断：

1. 这是目标、当前事实还是历史描述？
2. 是否有清楚的时态和范围？
3. 是否有证据链接？
4. 是否把 sample、Policy-style 或计划能力写成真实能力？
5. 是否仍引用已经废弃的固定工期？

### 4.7 修正文档

文档应统一为三层表述：

```text
项目目标：最终要达到的生产级能力。
当前基线：Day 1～7 已经实现且可以验证的能力。
后续计划：Day 8 以后尚未实现的生产化路线。
```

特别检查：

- README 开头不能让读者误以为 React 和 AWS 已存在；
- 历史可行性评估不能继续充当当前计划；
- `outline.md` 只描述纲领和长期目标；
- `construction/01-★★★-construction-plan.md` 只描述阶段与门禁；
- `construction/02-★★★-day8-production-roadmap.md` 只描述施工顺序；
- 专题文档不能把当前 local 行为写成 production 方案。

## 5. 自动验收

### 5.1 Markdown 与 Git

```powershell
git diff --check
git status --short
git diff --stat
git diff --name-only
```

通过标准：

- 无 trailing whitespace；
- 无意外二进制文件；
- 只修改能力事实相关文档；
- 没有 `tmp/`、日志或运行产物进入 Git。

### 5.2 链接检查

对本日新增或修改的相对链接逐个确认目标存在：

```powershell
Test-Path README.md
Test-Path outline.md
Test-Path construction/01-★★★-construction-plan.md
Test-Path construction/02-★★★-day8-production-roadmap.md
Test-Path docs/phase-0/current-capability-baseline.md
Test-Path docs/phase-0/production-gap-register.md
```

若仓库尚无 Markdown link checker，Day 8 使用人工加 PowerShell 路径检查，并将
自动化链接检查列入阶段 1 静态门禁。

### 5.3 事实抽样

至少对以下声明从源代码反查：

```powershell
rg -n "Map(Get|Post)|MigrateAsync" src/FinOps.Api src/FinOps.Worker
rg -n "ForceSampleData|UseSampleDataWhenUnavailable" src
rg -n "cloud_resources|cloud_cost_daily|etl_job_runs" src
rg -n "ICloud.*Provider|IAzureSubscriptionReader" src/FinOps.Application
```

如果文档说“存在”但搜索不到实现，状态必须改为 `Planned` 或
`PresentUnverified`。

## 6. 人工验证

### 6.1 能力表逐行验证

人工 reviewer 对每一行回答：

- 证据是否直接支持结论？
- 状态是否过高？
- 限制是否写清楚？
- 生产结论是否保守？
- 后续阶段是否正确？

抽样至少覆盖：

- 一个工程基础能力；
- 一个真实 Azure 能力；
- 一个 PostgreSQL 数据能力；
- 一个失败路径；
- 一个 sample 路径；
- 一个尚未实现能力。

### 6.2 反向检查

从以下高风险代码出发，确认能力表没有漏项：

- `src/FinOps.Api/Program.cs`
- `src/FinOps.Worker/Worker.cs`
- `src/FinOps.Infrastructure/DependencyInjection.cs`
- `src/FinOps.Infrastructure/Azure/AzureCostProvider.cs`
- `src/FinOps.Infrastructure/Persistence/FinOpsDbContext.cs`
- `terraform/azure/main.tf`

### 6.3 生产禁止项确认

必须明确标为 `ProductionProhibited`：

- 匿名管理 API；
- API/Worker 启动自动 migration；
- production 使用 Azure CLI 用户身份；
- production 开启 sample fallback；
- 本地 Terraform state 管理生产；
- 开发密码用于生产；
- 无 tenant 条件的数据查询；
- 无调度租约的并发 ETL；
- 未验证恢复能力却声称可灾备。

## 7. Day 8 Review 清单

- [ ] 能力状态词使用统一词典；
- [ ] 每项 `VerifiedBaseline` 都有直接证据；
- [ ] 目标能力和当前能力严格分开；
- [ ] README 没有暗示 React/AWS 已实现；
- [ ] 历史 30 天评估已标记为历史；
- [ ] sample 数据没有被写成真实 Azure 成本；
- [ ] 成本粒度没有被写成单资源精确成本；
- [ ] 多 subscription 当前行为没有被写成多租户能力；
- [ ] 生产禁止项完整；
- [ ] 后续差距映射到正确阶段；
- [ ] 无业务代码或 migration 的无关变动；
- [ ] `tmp/` 和运行产物未进入 Git。

## 8. 人工学习

### 8.1 必须理解的概念

- **Capability baseline**：在某个 Commit 上能够被证据证明的能力集合；
- **Target state**：系统未来要达到的状态，不代表当前已有；
- **Gap**：当前状态与目标状态之间可描述、可安排的差距；
- **Evidence**：可以重复检查的测试、脚本、数据或运行结果；
- **Production prohibition**：即使本地可用，也不能原样进入生产的行为。

### 8.2 自测问题

1. 为什么“代码存在”不等于“能力已验证”？
2. 为什么 Azure tenant 与平台业务 tenant 不是同一个概念？
3. 当前成本数据为什么不能证明单资源准确成本？
4. sample fallback 在学习环境有何价值，在生产环境为何危险？
5. 为什么自动 migration 对本地友好、对多实例生产危险？
6. 旧的 30 天计划应该删除，还是作为历史材料标记？为什么？
7. 哪三项当前能力最有价值？哪三项风险最严重？

### 8.3 口头复述标准

不看文档，用 5 分钟说明：

```text
项目目标是什么；
Day 1～7 真正完成了什么；
为什么它仍只是开发基线；
接下来阶段 1～4 为什么优先于前端和 AWS。
```

如果无法准确区分“当前”和“目标”，Day 8 不应标记 `Complete`。

## 9. 收尾

```powershell
git diff --check
git status --short --branch
git diff -- construction docs README.md outline.md
```

在 `tmp/day08-closeout-report.md` 记录：

- 发现了哪些过时声明；
- 修正了哪些文档；
- 哪些能力被降级或升级状态；
- 哪些差距进入 Day 11 风险登记；
- 人工 review 是否允许进入 Day 9。
