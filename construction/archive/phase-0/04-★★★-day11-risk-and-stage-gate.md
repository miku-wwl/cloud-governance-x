# 04 ★★★ Day 11：风险治理与阶段 0 出关

> 本日目标：基于 Day 8～10 的事实建立治理材料，并以证据决定阶段 0 是否出关。
>
> 本日不做：用新功能掩盖风险、临时降低门禁、提前实施阶段 1。

## 1. 完成定义

Day 11 完成时必须存在：

```text
docs/phase-0/risk-register.md
docs/phase-0/data-classification.md
docs/phase-0/dependency-license-inventory.md
docs/phase-0/adr-backlog.md
docs/phase-0/stage-0-gate-report.md
tmp/day11-closeout-report.md
```

阶段 0 只有两种有效结论：

- `Complete`：全部门禁通过，允许进入 Day 12；
- `Validation` 或 `Blocked`：列出阻断项，不开始依赖它的阶段 1 工作。

不存在“基本通过”。

## 2. 输入

必须读取：

- Day 8 能力真值与生产差距；
- Day 9 基线验收总结和原始证据索引；
- Day 10 当前架构、数据流和 trust boundary；
- `construction/01-★★★-construction-plan.md` 阶段 0 风险清单；
- `outline.md` 生产安全、数据、可靠性和运行要求；
- 当前 Git 状态和 Azure 清理结果。

本日施工顺序是：

```text
风险登记
  → 数据分类
  → 依赖与许可证
  → secret 与遗留资源检查
  → ADR backlog
  → 证据索引
  → Go/No-Go
```

前面任何材料不完整时，不提前填写“阶段通过”结论。

## 3. 风险登记册

### 3.1 风险字段

`risk-register.md` 每条风险至少包含：

| 字段 | 说明 |
| --- | --- |
| Risk ID | `RISK-0001` 格式 |
| 标题 | 一句话描述风险 |
| 类别 | Security、Data、Reliability、Operations、Delivery、Cost、Compliance |
| 事实证据 | 文件、图、测试或运行结果 |
| 触发场景 | 风险何时发生 |
| 影响 | 对用户、数据、费用或运行的后果 |
| Likelihood | Low / Medium / High |
| Impact | Low / Medium / High / Critical |
| Severity | 综合严重度 |
| Owner | 负责角色，不写“全体” |
| Treatment | Avoid / Mitigate / Transfer / Accept |
| 目标阶段 | 计划在哪个阶段关闭 |
| 验证方式 | 如何证明已关闭 |
| 状态 | Open / Mitigating / Accepted / Closed |
| 接受期限 | 仅 Accepted 时需要 |

### 3.2 严重度规则

初版可以使用简单矩阵：

| Impact \ Likelihood | Low | Medium | High |
| --- | --- | --- | --- |
| Low | Low | Low | Medium |
| Medium | Low | Medium | High |
| High | Medium | High | Critical |
| Critical | High | Critical | Critical |

以下风险默认不能降为 Low：

- tenant 数据越界；
- 无认证管理操作；
- secret 泄漏；
- 财务成本错误；
- destructive action 无审批；
- 无法恢复的生产数据丢失。

### 3.3 阶段 0 必须登记的风险

至少登记：

| 风险 | 当前事实 | 目标阶段 |
| --- | --- | --- |
| 管理 API 无认证 | `/api/admin` 可直接调用 | 阶段 2 |
| 无业务 tenant | 核心表没有 `tenant_id` | 阶段 2～3 |
| 自动 migration | API 和 Worker 启动时执行 | 阶段 1 |
| ETL 无调度 | 仅 Worker 或手工 POST | 阶段 4 |
| ETL 无互斥/租约 | 可被重复并发触发 | 阶段 4 |
| sample fallback | Cost Provider 可降级为 sample | 阶段 5 |
| 资源删除语义缺失 | 删除资源仍留在表中 | 阶段 3/5 |
| 成本粒度有限 | 当前按服务和 Resource Group | 阶段 3/6 |
| 无 outbox/inbox | 事件可靠性尚不存在 | 阶段 3/10 |
| 无可观测性 | 无统一 trace、metric、SLO | 阶段 11 |
| 无备份恢复 | 无 PITR 与恢复演练 | Release A/阶段 15 |
| 无 staging | 只有 local | Release A |
| 无 CI/CD | 验证依赖人工运行 | 阶段 1/12 |
| Terraform 本地 state | 无 remote backend/locking | 阶段 12 |
| Azure E2E 费用 | 真实资源短时收费 | 持续治理 |
| Azure 清理失败 | 脚本异常可能遗留资源 | 阶段 1/12 |

建议追加：

- 默认开发密码；
- `AllowedHosts=*`；
- Azure CLI 用户身份；
- PostgreSQL 宿主端口；
- API 错误模型不稳定；
- Provider timeout/retry 不统一；
- Cost fallback 捕获范围；
- 无数据 retention；
- 日志可能包含外部错误详情；
- 无 dependency/secret 自动门禁；
- 依赖和工具链版本漂移没有固定检查与升级流程。

### 3.4 Owner 规则

Owner 使用角色：

- Platform Architect；
- Application Owner；
- Data Owner；
- Security Owner；
- Platform SRE；
- FinOps Product Owner；
- Cloud Provider Owner。

单人项目可以由同一个人承担多个角色，但风险登记仍要写清“以哪个角色负责”。

## 4. 数据分类

### 4.1 分类等级

建议采用：

| 等级 | 定义 | 示例 |
| --- | --- | --- |
| Public | 可公开且无明显风险 | 开源 README、公共架构原则 |
| Internal | 仅项目和团队内部 | 内部运行说明、非敏感配置 |
| Confidential | 泄漏会暴露组织、财务或资源信息 | 成本、资源 ID、tags、审计 |
| Restricted | 泄漏可直接导致访问或重大损害 | token、secret、私钥、生产连接字符串 |

### 4.2 必须分类的数据

| 数据 | 建议等级 | 说明 |
| --- | --- | --- |
| 源代码 | Public/Internal | 取决于仓库公开策略 |
| Azure subscription/tenant ID | Internal/Confidential | 可用于组织枚举和攻击准备 |
| 资源名称、ID、region、tags | Confidential | tags 可能含人员、项目和成本中心 |
| 成本金额与币种 | Confidential | 财务和业务敏感 |
| `raw_json` | Confidential | 可能含 Provider 原始维度 |
| ETL 错误 | Internal/Confidential | 可能包含资源、scope 和 SDK 信息 |
| 数据库密码 | Restricted | 当前值仅为本地开发 |
| Azure access token | Restricted | 永不进入 Git、报告和普通日志 |
| Terraform state | Confidential/Restricted | 取决于资源和敏感属性 |
| 审计日志 | Confidential | 包含 actor、target、操作和来源 |
| 导出文件 | Confidential | 权限、过期和审计尚未实现 |

### 4.3 每类数据的控制字段

`data-classification.md` 至少记录：

- 数据 Owner；
- 来源；
- 当前存储位置；
- 当前传输方式；
- 当前日志行为；
- 当前 Git 风险；
- 访问主体；
- retention；
- 备份要求；
- 导出要求；
- 删除要求；
- 目标生产控制；
- 当前差距。

### 4.4 人工检查

重点搜索：

```powershell
rg -n -i `
  "password|secret|token|connection.?string|access.?key|client.?secret|private.?key" `
  . `
  -g '!tmp/**' `
  -g '!**/bin/**' `
  -g '!**/obj/**' `
  -g '!**/.terraform/**'
```

这是敏感词检查，不是完整 secret scanner。每个命中要分类：

- 安全的本地示例；
- 代码中的字段名；
- 文档说明；
- 疑似真实 secret；
- 必须立即处理的泄漏。

## 5. 依赖与许可证清单

### 5.1 范围

必须覆盖：

- .NET 直接 NuGet 依赖；
- 关键传递依赖；
- .NET SDK 和 EF Tool；
- Terraform Provider；
- Docker base image；
- Azure CLI、Terraform、Docker 等构建/运行工具；
- PowerShell；
- 后续新增的脚本工具。

### 5.2 .NET 依赖

```powershell
dotnet list FinOpsPlatform.slnx package --include-transitive
dotnet list FinOpsPlatform.slnx package --vulnerable --include-transitive
dotnet list FinOpsPlatform.slnx package --deprecated
dotnet list FinOpsPlatform.slnx package --outdated
```

若当前 SDK 支持 JSON：

```powershell
dotnet list FinOpsPlatform.slnx package `
  --include-transitive `
  --format json
```

直接依赖至少包括当前项目中的：

- Azure.Identity；
- Azure.ResourceManager；
- Azure.ResourceManager.ResourceGraph；
- Microsoft.EntityFrameworkCore.Design；
- Npgsql；
- Npgsql.EntityFrameworkCore.PostgreSQL；
- Microsoft.Extensions.Hosting；
- Microsoft.NET.Test.Sdk；
- xUnit；
- coverlet.collector。

不要凭记忆填写许可证。许可证必须来自：

- 官方 NuGet package metadata；
- 官方项目仓库；
- 随包提供的 license 文件；
- 组织认可的 license scanner。

无法确认时记录 `Unknown - review required`，不能猜测。

### 5.3 Terraform Provider

检查：

```powershell
Get-Content terraform/azure/.terraform.lock.hcl
terraform -chdir=terraform/azure providers
```

记录：

- Provider source；
- constraint；
- locked version；
- checksums；
- license 来源；
- 更新策略。

### 5.4 容器镜像

当前：

```text
postgres:18-alpine
```

记录：

- registry；
- tag；
- 是否固定 digest；
- 上游 license；
- 漏洞扫描状态；
- 支持周期；
- 当前仅用于 local；
- 阶段 12 的生产镜像策略。

### 5.5 工具依赖

记录版本：

```powershell
dotnet --version
dotnet tool list --local
terraform -chdir=terraform/azure version
az version
docker version
$PSVersionTable
```

工具许可证与再分发要求也属于供应链清单，但阶段 0 可先记录来源和版本，在阶段 14
完成正式政策门禁。

## 6. Secret 检查

### 6.1 自动扫描优先

使用项目认可的 secret scanner，例如：

```powershell
gitleaks detect --source . --redact
```

如果当前环境没有 scanner：

- 不要临时从不可信来源下载安装；
- 执行敏感词和高熵值人工检查；
- 将“缺少自动 secret scanner”登记为风险；
- 阶段 1 必须将 scanner 固化到静态门禁；
- 阶段 0 报告不得把人工 grep 描述成完整自动扫描。

### 6.2 Git 跟踪内容

```powershell
git grep -n -I -i `
  -e "password" `
  -e "client_secret" `
  -e "access_key" `
  -e "private_key" `
  -e "connectionstring"
```

允许本地开发示例存在，但必须满足：

- 明确标注只用于 local；
- 不复用真实环境凭据；
- `.env` 和 `terraform.tfvars` 不提交；
- 无真实 endpoint、token 或私钥。

### 6.3 Git 历史

自动 scanner 应覆盖历史。若未覆盖：

- 把历史扫描缺口登记为风险；
- 不因当前工作树无 secret 就断言历史安全；
- 一旦发现真实 secret，先轮换，再处理历史。

## 7. Azure 和本地遗留审计

### 7.1 Azure

```powershell
az group list `
  --query "[?tags.owner=='cloud-governance-x'].{name:name,location:location,tags:tags}" `
  --output table
```

要求：

- 与 Day 9 执行前清单对比；
- 每个资源有已知用途和 Owner；
- 未知资源进入风险登记；
- 未确认所有权前不删除。

### 7.2 PostgreSQL

```powershell
docker compose exec -T postgres `
  psql -U finops -d postgres `
  -t -A `
  -c "SELECT datname FROM pg_database WHERE datname LIKE 'finops_day%';"
```

预期无测试数据库。

### 7.3 Terraform 和仓库

```powershell
Get-ChildItem -Recurse -Force `
  -Include *.tfstate,*.tfstate.*,*.tfplan
git status --short --ignored
git diff --check
```

## 8. ADR Backlog

Day 11 默认只建立决策队列，不提前写完所有 ADR。若阶段 1 开工前需要更明确
输入，Agent 可以先为阶段 1 阻断项形成候选 ADR；候选 ADR 仍需项目 Owner
确认后才能视为正式 `Accepted`。

每项至少包含：

| 字段 | 说明 |
| --- | --- |
| ADR ID | `ADR-0001` |
| 标题 | 要做出的决策 |
| 触发原因 | 为什么需要 ADR |
| 备选方向 | 当前已知选项 |
| 决策期限 | 必须在哪个 Day/阶段前完成 |
| Owner | 负责角色 |
| 状态 | Proposed / CandidateDecision / InReview / Accepted / Superseded |
| 相关风险 | Risk ID |

初始 backlog 建议：

| ADR | 决策主题 | 最晚阶段 |
| --- | --- | --- |
| ADR-0001 | 模块边界与架构测试规则 | 阶段 1 |
| ADR-0002 | 独立 Migration Host 与发布流程 | 阶段 1 |
| ADR-0003 | Organization/Tenant/CloudAccount 模型 | 阶段 2 |
| ADR-0004 | Entra ID、service identity 与本地开发身份 | 阶段 2 |
| ADR-0005 | tenant 隔离与 PostgreSQL RLS | 阶段 2～3 |
| ADR-0006 | Raw/Normalized/Derived/Operational 数据分层 | 阶段 3 |
| ADR-0007 | 资源 full-scan 与 inactive/deleted 语义 | 阶段 3 |
| ADR-0008 | Job queue、scheduler、lease 与 checkpoint | 阶段 4 |
| ADR-0009 | production sample 隔离策略 | 阶段 5 |
| ADR-0010 | OpenTelemetry backend 与 SLO 平台 | Release A |
| ADR-0011 | development/staging/production 部署平台 | Release A/阶段 12 |
| ADR-0012 | Terraform remote state 与环境隔离 | Release A/阶段 12 |
| ADR-0013 | Service Bus topology、outbox/inbox | 阶段 10 |
| ADR-0014 | 备份、PITR、RPO/RTO 与 DR | Release A/阶段 15 |
| ADR-0018 | 依赖和工具链版本治理 | 阶段 1/14 |

阶段 1 开工前，ADR-0001、ADR-0002 和依赖工具链治理 ADR 必须至少达到
`CandidateDecision`，并明确可重复静态验证入口。是否提升为 `Accepted` 由
项目 Owner 在阶段 0/阶段 1 gate 中确认。

## 9. 阶段 0 证据索引

`stage-0-gate-report.md` 应建立：

| 证据 ID | 门禁 | 结论 | 永久文档 | 原始证据 | Commit |
| --- | --- | --- | --- | --- | --- |
| EVD-0001 | 能力边界 | Passed/Failed | Day 8 baseline | tmp path | hash |
| EVD-0002 | 自动测试 | Passed/Failed | Day 9 summary | tmp path | hash |
| EVD-0003 | Azure E2E | Passed/Blocked | Day 9 summary | tmp path | hash |
| EVD-0004 | 清理 | Passed/Failed | Day 9 summary | tmp path | hash |
| EVD-0005 | 架构图 | Passed/Failed | Day 10 architecture | review notes | hash |
| EVD-0006 | 风险登记 | Passed/Failed | risk register | review notes | hash |
| EVD-0007 | secret | Passed/Gap | gate report | scanner output | hash |

原始 `tmp/` 证据不会 push，因此永久报告还应记录：

- 命令；
- 执行日期；
- 结果摘要；
- 输出 hash 或关键断言；
- 执行者；
- 已知限制。

## 10. 阶段 0 Go/No-Go

### 10.1 必须全部通过

- [ ] Day 8 能力基线已人工 review；
- [ ] Day 9 本地 build/test 通过；
- [ ] Day 9 六个 E2E 均有真实分类；
- [ ] 真实成本声明有 strict 模式证据，或明确降级声明；
- [ ] Day 9 清理审计通过；
- [ ] Day 10 当前架构图与代码一致；
- [ ] 所有 trust boundary 有当前控制和缺口；
- [ ] 所有必登记风险有 Owner、严重度和目标阶段；
- [ ] 数据分类覆盖成本、资源、身份、凭据、日志和 state；
- [ ] 直接依赖和关键供应链对象已登记；
- [ ] secret 检查有真实结论和工具限制说明；
- [ ] ADR backlog 覆盖阶段 1～4 的关键决策；
- [ ] Git 仓库无不应提交的运行产物；
- [ ] 无未知 Azure 测试资源；
- [ ] 无测试数据库或测试端口遗留。

### 10.2 默认 No-Go

出现以下任一项，不进入 Day 12：

- 能力基线仍把规划写成已实现；
- 基线测试存在未解释失败；
- Azure 测试资源所有权不明；
- `finops_day*` 数据库未清理；
- 发现疑似真实 secret 尚未轮换；
- 风险无 Owner 或目标阶段；
- 当前架构图与代码不一致；
- 真实成本失败却仍声称真实闭环通过；
- Git 工作树混入 state、plan、日志或 credential；
- 为了出关而删除失败证据。

### 10.3 可以带入阶段 1 的已知风险

阶段 0 的职责是识别和安排，不要求关闭所有生产差距。

可以带入：

- 匿名管理 API；
- 自动 migration；
- 无 tenant；
- 无 scheduler；
- sample fallback；
- 无 telemetry；
- 本地 state。

前提：

- 风险已经登记；
- 当前生产结论明确为禁止或受限；
- Owner 和目标阶段明确；
- 阶段 1 不依赖风险已经关闭。

## 11. 人工 Review 会议顺序

建议按 60～90 分钟执行：

1. 10 分钟：Day 8 当前能力与生产禁止项；
2. 15 分钟：Day 9 自动测试、Azure E2E 和清理；
3. 15 分钟：Day 10 架构和 trust boundary walkthrough；
4. 20 分钟：最高严重度风险和 Owner；
5. 10 分钟：数据分类、secret 和许可证未知项；
6. 10 分钟：ADR backlog 与阶段 1 输入；
7. 5 分钟：作出 `Complete`、`Validation` 或 `Blocked` 结论。

Review 记录必须包含不同意见和未决问题，不能只写“同意”。

## 12. 人工学习

### 12.1 风险与缺陷

- 缺陷：当前行为不符合已经定义的预期；
- 风险：未来可能发生并造成影响的不确定事件；
- 限制：当前明确不支持的能力；
- 技术债：为了短期交付接受的未来维护成本；
- 风险接受：由有权 Owner 在期限内明确承担，不等于忽略。

### 12.2 ADR 与普通文档

ADR 记录“为什么选择某个重要方向”，包括备选项和后果。普通设计文档说明
“系统如何工作”。两者不能互相替代。

### 12.3 必须回答

1. 为什么风险必须有 Owner，而不能写“团队负责”？
2. 为什么低概率的 tenant 越界仍可能是 Critical？
3. 为什么 Terraform state 需要分类？
4. 为什么 package 名称和版本清单不等于许可证审查？
5. 为什么人工敏感词搜索不能替代 secret scanner？
6. 阶段 0 为什么允许带着大量风险出关？
7. 什么风险必须在阶段 1 前关闭，什么可以安排到阶段 5？
8. `Accepted` 风险为什么需要到期时间？

## 13. 最终收尾

```powershell
git diff --check
git status --short --branch
git diff --stat
git ls-files | Measure-Object
```

在 `tmp/day11-closeout-report.md` 记录：

- 阶段 0 最终状态；
- 最高风险；
- 外部阻断；
- secret 和许可证未知项；
- Day 12 前必须关闭的事项；
- 是否允许开始阶段 1；
- 人工 reviewer 的明确结论。

只有结论为 `Complete` 时，下一次才执行 Day 12。
