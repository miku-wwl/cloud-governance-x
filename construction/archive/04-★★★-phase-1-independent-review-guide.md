# 04 ★★★ 阶段 1 独立全面 Review 与 ChatGPT 网页版执行指南

> Historical review note, 2026-06-29:
>
> Phase 1 has already been accepted. This guide is preserved as the historical
> review method for that gate, not as the active planning entrypoint.

> 适用范围：Day 1～19 当前仓库，重点审查 Day 12～19 阶段 1  
> 审查目的：在进入 Day 20 之前，独立复核阶段 1 的代码、门禁、证据和剩余风险  
> 执行环境：ChatGPT 网页版，优先使用 Project；也可使用一个专用长会话  
> 最终决策：由项目 Owner 作出，ChatGPT 只能提交审查意见，不能代替 Owner 签收

## 0. 这份指南的法律地位

Day 19 的 CI、branch protection 和阶段工程门禁已经通过，但这只证明：

- 仓库拥有可重复运行的工程检查；
- 已知格式、架构、测试和 migration 回归能够阻断合并；
- 当前 `main` 在 GitHub-hosted runner 上通过要求的检查。

它不自动证明：

- 所有检查规则都设计正确；
- 没有漏检、误判或被实现细节绕过的边界；
- Day 12～19 的重构没有产生隐藏行为变化；
- 文档、代码和阶段报告完全一致；
- 阶段 1 已经适合作为 tenant、身份和授权工作的可信前置基础。

因此，从本指南进入仓库后，阶段 1 使用两层状态：

```text
Engineering Gate
    已通过：自动化与 GitHub 合并契约成立

Independent Acceptance
    待执行：全面人工/模型辅助审查完成后，由 Owner 签收
```

在 Independent Acceptance 变为 `Accepted` 前：

- 不启动 Day 20 的 tenancy ADR；
- 不实现 tenant schema、身份认证、RBAC 或 audit；
- 不把阶段 1 的 `EngineeringGatePassed` 简化表述为“所有架构风险已关闭”；
- 可以修复本次 review 发现的阶段 1 缺陷，但修复必须重新走受保护 PR。

## 1. Review 的目标与非目标

### 1.1 必须回答的六个问题

全面 review 必须给出有证据的答案：

1. Day 12～19 的每个交付目标是否真实完成？
2. 自动化门禁是否验证了真实风险，而不只是验证脚本自身？
3. API、DI、Worker 和 migration 重构是否保持了原业务契约？
4. GitHub CI 与 branch protection 是否形成了真实合并契约？
5. 阶段文档、ADR、风险、差距和 README 是否与实现一致？
6. 是否存在阻止进入 Day 20 的 Critical、High 或未解释 Medium finding？

### 1.2 本次不是生产上线评审

以下能力仍属于后续阶段，不能因为本次 review 没有实现它们就直接判定阶段 1
失败：

- 登录、OIDC、Bearer Token；
- tenant isolation；
- RBAC；
- append-only audit；
- 生产 scheduler、queue、lease、retry；
- staging、CD、deployment approval；
- backup、PITR、DR；
- SLO、alert、runbook；
- container、SBOM、provenance；
- AWS、多云生产能力。

但 reviewer 必须检查这些缺口是否被诚实记录，是否被 CI 全绿掩盖，是否错误地
标记为已关闭。

### 1.3 禁止把 review 变成实现任务

第一轮全面 review 必须只读：

- 不修改文件；
- 不生成“顺手修复”的代码；
- 不重写架构；
- 不进行依赖升级；
- 不因个人偏好要求大规模命名或格式调整。

只有最终报告确认 finding 后，才另开修复会话。这样可以防止 reviewer 在审查
过程中改变被审对象，导致证据失去稳定基线。

## 2. ChatGPT 网页版的使用策略

### 2.1 推荐使用一个独立 Project

如果账号界面提供 Projects，建议新建：

```text
Cloud Governance X - Phase 1 Independent Review
```

Project 只放本次 review 的材料，避免历史对话、其他代码和个人文件污染上下文。

建议上传：

1. GitHub `main` 的最新源码；
2. 本指南；
3. 如果 GitHub 下载包中没有提交信息，再附上本次 review 的 commit SHA；
4. 如需引用 CI，可附 GitHub Actions 链接，不要上传包含 token 的日志；
5. 最终各轮输出可以保存到 Project，作为下一轮的输入材料。

OpenAI 官方帮助中心说明，Projects 可以保存文件、项目指令和会话上下文；文件
上传支持常见文本、文档、表格和演示文件。界面、套餐和文件限额可能变化，因此
本指南不假定一个固定的上传数量或大小。

官方参考：

- <https://help.openai.com/en/articles/10169521-using-projects-in-chatgpt>
- <https://help.openai.com/en/articles/8555545-file-uploads-faq>

### 2.2 下载源码

在 GitHub 仓库页面确认分支为 `main`，然后：

```text
Code → Download ZIP
```

下载前记录：

```text
Repository: miku-wwl/cloud-governance-x
Branch: main
Commit SHA: <网页显示的最新 main SHA>
Downloaded at: <本地日期时间和时区>
```

不要上传：

- 本机 `.git/`；
- `errorlog.txt`；
- `tmp/` 下的个人或临时证据；
- `.env`；
- Terraform state、plan；
- `bin/`、`obj/`；
- Azure、GitHub、数据库 token 或密码；
- 浏览器导出的认证 cookie。

GitHub 源码 ZIP 正常不包含 `.git/` 历史。reviewer 因此不能仅凭 ZIP 重建真实
commit diff，也不能假装看过未上传的历史。阶段变更范围应以本指南、提交清单和
阶段报告为入口，再对当前最终状态做审查。

### 2.3 ZIP 可读性必须先验证

不同 ChatGPT 网页环境对压缩包的处理能力可能不同。上传 ZIP 后，第一步不是开始
review，而是要求模型证明它能递归读取源码。

如果模型：

- 无法列出压缩包内容；
- 只能读取顶层文件；
- 看不到 `.github/`、`.editorconfig` 等隐藏路径；
- 报告的项目结构明显缺失；
- 把文件名中的中文或星号解码失败；

则立即停止，不允许继续形成 review 结论。

可用替代方式：

1. 本地解压后分目录上传；
2. 优先上传第 5 节每轮列出的文件；
3. 使用 Project 分批保存材料；
4. 给出公开 GitHub 文件链接作为补充；
5. 让模型明确列出“未能读取的文件”，而不是静默跳过。

### 2.4 不要一次要求“全面 review”

不推荐的提示：

```text
这是我的仓库，请全面 review 并告诉我能不能进入下一阶段。
```

这种提示会诱发：

- 未证明文件覆盖就开始总结；
- 抽样阅读后声称全仓审查；
- 把 CI 结果当作代码正确性的充分证明；
- 将风格建议与真实阻断问题混在一起；
- 忘记 migration、PowerShell、Terraform 或文档一致性；
- 在一个超长答案里丢失 finding 的证据链。

本指南要求至少执行十二轮。每轮只处理一个明确审查域，并把结果写入统一 ledger。

## 3. 审查角色和硬性行为规则

把下面内容放入 ChatGPT Project instructions；如果不用 Project，就作为第一条
消息发送。

```text
你是 Cloud Governance X 阶段 1 的独立高级审查员。

你的任务是验证证据，不是帮助作者证明预设结论。仓库作者已声明 Day 12～19 的
工程门禁完成，但 Independent Acceptance 尚未签收。

硬性规则：

1. 只审查实际读取到的文件。不得声称看过未打开、无法解析或未上传的文件。
2. 每个 finding 必须给出文件路径、相关符号或配置键、证据和影响。
3. 能给行号时给行号；压缩包行号不稳定时，必须给出可搜索的类名、函数名或原文。
4. 区分“仓库静态事实”“CI 报告声称”“你实际执行的命令”和“合理推断”。
5. 未实际执行命令时，禁止写“测试已通过”；只能写“阶段报告记录为通过”。
6. 不把缺少后续阶段能力当作阶段 1 实现缺陷，但要检查其风险是否被正确保留。
7. 优先寻找会导致错误行为、漏检、绕过、数据损坏、权限扩大、挂死、资源遗留或
   错误签收的问题。纯风格建议不得标为阻断。
8. 主动寻找反例和 negative path，不只验证 happy path。
9. 不进行代码修改。发现问题后提出最小修复方向和验证建议即可。
10. 如果上下文不足，输出 NEED_MORE_EVIDENCE 和精确所需文件，不要猜测。
11. 每轮维护 Review Ledger，禁止遗漏之前尚未关闭的 finding。
12. 最终只能输出 ACCEPT、CONDITIONAL_ACCEPT 或 REJECT，不得使用模糊结论。

严重度：

- Critical：可能导致 secret 泄漏、不可逆数据破坏、门禁可完全绕过，或签收结论
  建立在明显虚假证据上。
- High：真实行为错误、跨平台 CI 不可靠、架构边界可绕过、migration/进程/权限
  失败，足以阻止进入 Day 20。
- Medium：不会立即破坏阶段基础，但存在明确设计、覆盖、文档或可维护性风险；
  必须修复或由 Owner 书面接受并指定后续阶段。
- Low：局部改进，不影响阶段签收。
- Note：观察或未来建议，不是 finding。

Finding 状态：

- Open
- Needs evidence
- Accepted risk
- Fixed, awaiting verification
- Closed

证据强度：

- E0：无证据或仅猜测
- E1：文档声明
- E2：静态代码证据
- E3：自动化测试或 CI 输出
- E4：真实数据库、云资源、进程或保护规则的运行证据

任何 High/Critical finding 不能只凭 E0/E1 建立，也不能只凭作者一句说明关闭。
```

## 4. 统一 Review Ledger

每一轮都要求模型追加以下表格，不要改变列名：

| ID | Domain | Severity | Confidence | Evidence | Location | Finding | Impact | Required action | Verification | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |

字段约束：

- `ID`：使用 `P1-001` 连续编号；
- `Domain`：Scope、Toolchain、StaticGate、Architecture、API、DI、Worker、
  Migration、CI、Governance、Regression、Documentation；
- `Severity`：Critical/High/Medium/Low/Note；
- `Confidence`：High/Medium/Low；
- `Evidence`：E0～E4，可组合，例如 `E2+E3`；
- `Location`：文件路径和符号；
- `Finding`：只陈述一个问题；
- `Impact`：描述真实失败方式；
- `Required action`：最小必要动作；
- `Verification`：如何证明修复有效，必须含 negative path；
- `Status`：初始通常为 Open 或 Needs evidence。

### 4.1 Finding 质量示例

不合格：

```text
代码可以更优雅，建议重构。
```

合格：

```text
P1-00X / High / E2
scripts/Test-DatabaseMigration.ps1 的 API 启动路径通过 dotnet run 产生包装进程；
脚本停止包装进程后，Linux runner 上实际 API 子进程仍可能存活，使 WaitForExit
阻塞到 job timeout。建议直接执行已构建 DLL，并在 CI 中验证 job 正常结束且无
orphan process。
```

### 4.2 反对意见记录

如果作者不同意 finding，不删除原 finding，增加：

```text
Owner response:
Reviewer reassessment:
Final disposition:
```

最终报告必须保留争议轨迹。

## 5. 材料完整性检查

开始任何代码判断前，执行“第 0 轮”。

### 第 0 轮提示词：建立可审查基线

```text
请先不要进行代码质量评价，也不要输出阶段签收结论。

任务：

1. 递归读取我上传的仓库。
2. 输出仓库根目录、你能读取的总文件数和各顶层目录文件数。
3. 明确确认以下关键路径是否存在并可读取：
   - .editorconfig
   - .github/workflows/ci.yml
   - .github/PULL_REQUEST_TEMPLATE.md
   - .github/CODEOWNERS
   - Directory.Build.props
   - global.json
   - FinOpsPlatform.slnx
   - construction/02-★★★-day8-production-roadmap.md
   - construction/04-★★★-phase-1-independent-review-guide.md
   - docs/phase-1/stage-1-gate-report.md
   - docs/phase-1/engineering-governance.md
   - scripts/Test-RepositoryStatic.ps1
   - scripts/Test-DatabaseMigration.ps1
   - src/FinOps.Tests/Architecture/LayerDependencyTests.cs
   - src/FinOps.Migrator/Program.cs
4. 列出所有无法读取、编码异常、被截断或疑似遗漏的文件。
5. 记录我提供的 main commit SHA；如果我没提供，标记 NEED_MORE_EVIDENCE。
6. 建立空的 Review Ledger。

通过条件：

- 关键隐藏文件可读；
- src、scripts、terraform、docs、construction、.github 均被识别；
- 你没有在本轮评价实现正确性。

如果不满足，请只输出 INGESTION_FAILED 和修复上传方式。
```

### 第 0 轮 Owner 核对

Owner 必须人工确认：

- 模型没有漏掉 `.github`；
- 模型没有把 `tmp/` 当成正式证据；
- 模型知道源码包不含 `.git` 历史；
- commit SHA 与 GitHub `main` 一致；
- 模型没有声称运行过任何命令。

## 6. 十二轮全面 Review

每一轮都使用同一个会话或 Project，并要求：

```text
先读取指定文件，后回答问题；将新 finding 追加到 Review Ledger；不得关闭其他轮
finding；最后列出本轮实际读取的文件。
```

### 第 1 轮：范围、事实与阶段边界

优先读取：

- `README.md`
- `outline.md`
- `construction/01-★★★-construction-plan.md`
- `construction/02-★★★-day8-production-roadmap.md`
- `docs/phase-0/current-capability-baseline.md`
- `docs/phase-0/production-gap-register.md`
- `docs/phase-0/risk-register.md`
- `docs/phase-1/stage-1-gate-report.md`

提示词：

```text
执行第 1 轮：Scope and Claims Review。

检查：

1. README 对 Day 1～19 的完成表述是否与代码和阶段报告一致。
2. 是否把 Azure 数据底座夸大为多云生产平台。
3. Phase 1 Passed/EngineeringGatePassed 与 Independent Acceptance 是否被区分。
4. 后续身份、tenant、RBAC、调度、部署、备份、安全能力是否仍明确标为未完成。
5. risk register 和 production gap 是否错误关闭了仍存在的风险。
6. Day 20 的前置条件是否明确依赖本次独立 review。
7. 找出互相矛盾、过期或使用未来时态描述已实现事实的内容。

输出：

- Current truth：最多 15 条；
- Contradictions；
- Missing evidence；
- Review Ledger 增量；
- 本轮实际读取文件。
```

阻断标准：

- 把未实现 tenant/auth 能力写成已完成；
- 阶段报告以不存在的 CI 或 branch protection 为依据；
- 关键剩余风险被标记 Closed 且无证据；
- 审查对象 commit 不明确。

### 第 2 轮：Day 12 编译、Analyzer 与格式策略

读取：

- `.editorconfig`
- `Directory.Build.props`
- `global.json`
- `dotnet-tools.json`
- 全部 `.csproj`
- Day 12 相关 README 和配置指南段落

提示词：

```text
执行第 2 轮：Toolchain and Compiler Policy Review。

逐项验证：

1. SDK 选择、rollForward 和 target framework 是否一致。
2. Nullable、ImplicitUsings、TreatWarningsAsErrors、analyzer 和 code-style
   enforcement 是否真正作用于全部项目。
3. .editorconfig 规则是否会在 build 或 format 中生效，是否存在只写不执行的规则。
4. test、API、Worker、Migrator 是否意外使用不同编译策略。
5. 是否存在会造成平台差异、纯样式大 churn 或隐藏行为 diff 的规则。
6. package/tool 版本是否固定，哪些只是可见但不阻断。
7. 设计一个最小格式违规和一个最小 analyzer 违规，说明预期由哪个命令阻断。

不要因为“配置看起来标准”而判定通过。请沿脚本和 CI 找到它们被调用的位置。
```

必须特别检查：

- `.editorconfig` 是否被 `dotnet format` 消费；
- warnings as errors 是否覆盖所有生产项目；
- 测试项目的特殊规则是否有合理理由；
- SDK 配置在 GitHub runner 上是否可满足。

### 第 3 轮：Day 13 单一静态门禁

读取：

- `scripts/Test-RepositoryStatic.ps1`
- `scripts/Test-GitHubActions.ps1`
- `.gitignore`
- `.github/workflows/ci.yml`
- `terraform/azure/*.tf`
- 所有被脚本解析的 JSON/YAML/XML/Markdown 配置

提示词：

```text
执行第 3 轮：Static Gate Adversarial Review。

不要只描述脚本步骤。把脚本当成安全边界，寻找绕过。

检查：

1. 候选文件集合是否包含 tracked 和未 tracked、未 ignored 文件。
2. secret scan 的模式、排除和自扫描处理是否存在明显漏报/误报。
3. JSON、XML、YAML、PowerShell、Markdown 检查是否真的覆盖仓库相应文件。
4. 子命令失败后主脚本是否最终返回非零。
5. dotnet restore/format/build/test 的顺序与 --no-restore/--no-build 是否一致。
6. vulnerable、deprecated、outdated 的阻断政策是否与 ADR-0018 一致。
7. Terraform init 是否禁用 backend，是否会修改 lockfile、state 或工作区。
8. 工作树不变检查在空输出、Windows/Linux、换行和未跟踪文件下是否可靠。
9. actionlint 下载是否校验版本、平台、架构和 SHA256。
10. 网络下载、工具缓存或临时目录失败时是否明确失败。

至少提出 10 个恶意或异常 probe，并说明：

- 修改什么；
- 应由哪个 step 发现；
- 预期退出码；
- 是否存在绕过可能。
```

建议 probes：

- 新增未 git add 的非法 JSON；
- 新增 ignored 与未 ignored secret 文件；
- PowerShell parse error；
- Markdown 断链；
- YAML 合法但 GitHub Actions 语义非法；
- 格式违规；
- 编译 warning；
- 单元测试失败；
- Terraform fmt 失败；
- Terraform validate 失败；
- verification 修改工作树；
- actionlint checksum 不匹配。

### 第 4 轮：Day 14 架构边界

读取：

- `src/FinOps.Tests/Architecture/LayerDependencyTests.cs`
- 全部 `.csproj`
- `FinOpsPlatform.slnx`
- `docs/adr/ADR-0001-module-boundaries-and-architecture-tests.md`
- API、Worker、Migrator 的入口和 DI 文件

提示词：

```text
执行第 4 轮：Architecture Enforcement Review。

建立期望依赖图和实际依赖图，然后逐条回答：

1. Domain 是否可能通过 package、project reference、source file 或 transitive
   dependency 依赖外层。
2. Application 是否可能引用 Infrastructure、host、EF、Azure SDK 或 Npgsql。
3. Azure SDK、EF implementation 和 Npgsql 的允许范围是否与 ADR 一致。
4. API、Worker、Migrator 的项目引用是否被精确约束。
5. 测试读取的是当前真实项目和程序集，还是可能检查错误路径或旧 build output。
6. Windows/Linux 路径和扩展名处理是否一致。
7. 新增 csproj、改名项目或新增宿主是否能绕过规则。
8. 禁止 API/Worker 自动 migration 的源码扫描是否可能被别名、静态导入、
   helper method 或不同调用形式绕过。
9. 反射级引用和 csproj/package 级引用之间有哪些盲区。
10. 规则是否过度严格，可能阻止合理的阶段 2 tenant 设计。

为每条架构规则给出：

- Protected invariant；
- Positive evidence；
- Negative probe；
- Known blind spot。
```

阻断 finding 示例：

- architecture tests 只检查旧输出或本机绝对路径；
- 新文件/新项目可绕过；
- Domain/Application 实际存在反向引用；
- Migrator 边界未被测试。

### 第 5 轮：Day 15 API Endpoint 模块化与契约兼容

读取：

- `src/FinOps.Api/Program.cs`
- `src/FinOps.Api/Endpoints/*.cs`
- `src/FinOps.Tests/Api/EndpointRouteTests.cs`
- 相关 Application 接口/DTO
- 调用 API 的 E2E 脚本

提示词：

```text
执行第 5 轮：API Composition and Route Compatibility Review。

检查重构前后应保持的契约：

1. 所有 route、HTTP method、参数、默认值和 response shape。
2. health/live 与 readiness 的分离。
3. Resources、Costs、ETL、Cloud、Health module 是否都由 composition root 挂载。
4. route tests 是验证真实 endpoint data source，还是只验证手写常量。
5. 重复 route、遗漏 route、错误 method、默认日期变化能否被测试发现。
6. endpoint module 是否包含不应存在的业务逻辑或 Infrastructure 类型。
7. ProblemDetails/异常行为是否因拆分改变。
8. 当前匿名管理 API 是否被诚实保留为阶段 2 风险。
9. E2E 脚本中的 URL 是否与 route tests 和代码一致。

输出一张 route inventory：

Method | Path | Module | Application dependency | Auth state | Test evidence
```

### 第 6 轮：Day 16 DI 拆分与生命周期

读取：

- `src/FinOps.Infrastructure/DependencyInjection.cs`
- `ApplicationUseCaseServiceCollectionExtensions.cs`
- `Azure/AzureServiceCollectionExtensions.cs`
- `Persistence/PostgreSqlServiceCollectionExtensions.cs`
- `HealthChecks/PostgreSqlHealthCheckExtensions.cs`
- API/Worker/Migrator `Program.cs`
- `DependencyInjectionTests.cs`

提示词：

```text
执行第 6 轮：Dependency Injection and Lifetime Review。

为每个关键服务列出：

Service | Implementation | Lifetime | Host consumers | External resource held

重点检查：

1. DbContext factory、Repository、Application service、Provider 的 scope 是否兼容。
2. Singleton 是否捕获 Scoped/Transient dependency。
3. HttpClient 是否通过 factory 管理，是否被 singleton 错误持有。
4. TokenCredential 与 ArmClient 生命周期是否合理。
5. API 和 Worker 是否使用同一注册入口而无重复/漂移。
6. Health check 是否只在 API 注册，是否符合职责。
7. Migrator 是否只注册 migration 所需依赖，避免 Azure/业务用例。
8. Add... 方法重复调用时是否造成重复 handler 或重复核心服务。
9. ValidateOnBuild/ValidateScopes 测试是否真的解析关键服务图。
10. 配置缺失、非法 port/timeout、空 credential 配置时失败是否清楚。

设计至少三个 captive dependency 或 duplicate registration negative probes。
```

### 第 7 轮：Day 17 Worker Job 契约与进程生命周期

读取：

- `src/FinOps.Worker/*.cs`
- `src/FinOps.Worker/Jobs/*.cs`
- `src/FinOps.Tests/Worker/WorkerJobTests.cs`
- Worker appsettings
- Worker 相关 E2E 脚本

提示词：

```text
执行第 7 轮：Worker Dispatch and Process Lifecycle Review。

检查：

1. Job name 是否大小写策略明确。
2. 未知 Job 是否稳定失败并返回非零退出码。
3. 重复 Handler 名称是否在执行前失败。
4. 每个 Handler 是否只承担一个用例，不形成万能 Handler。
5. scope 创建和释放是否覆盖成功、失败、取消。
6. 宿主 cancellation 与业务 OperationCanceledException 是否被正确区分。
7. 普通异常是否设置进程退出码并停止宿主。
8. StopApplication 是否保证任务完成后退出，又不会提前退出。
9. `Environment.ExitCode` 是否能从真实 `dotnet <Worker.dll>` 传播。
10. 测试是否只用 stub 验证对象，真实进程退出由哪个门禁覆盖。
11. Resources/Costs 的配置和日志字段是否保持兼容。
12. 一次性 Worker 的当前限制是否被误写成可靠 scheduler。

输出 Worker 状态机：

Start → Resolve Job → Execute → Success/Cancel/Failure → Exit code → Host stop
```

### 第 8 轮：Day 18 独立 Migration Host

读取：

- `src/FinOps.Migrator/*`
- `scripts/Invoke-DatabaseMigration.ps1`
- `scripts/Test-DatabaseMigration.ps1`
- API/Worker 入口
- EF migrations、DbContext 和 PostgreSQL 注册
- ADR-0002
- migration 相关 E2E 调用点

提示词：

```text
执行第 8 轮：Migration Ownership and Database Permission Review。

必须逐场景审查：

1. 空库升级；
2. 已升级库重复执行；
3. 两个同库 Migrator 并发；
4. 不同数据库并发；
5. 连接失败；
6. 配置/DI/Host build 失败；
7. migration SQL 失败；
8. API/Worker 使用无 schema CREATE 权限的 runtime role；
9. API/Worker 是否仍可能间接调用 Migrate/MigrateAsync；
10. 清理失败是否被报告；
11. 日志是否泄露 password/connection string；
12. process tree 在 Windows/Linux 是否会残留；
13. exit code 是否精确传播；
14. advisory lock 的 scope、获取方式和释放方式；
15. rollback/PITR 未实现是否被明确保留为后续风险。

特别审查 Test-DatabaseMigration.ps1：

- 是否直接执行已构建 DLL；
- NoBuild 前置条件是否检查；
- API readiness 超时是否有限；
- API process 是否可停止且 WaitForExit 不挂死；
- Worker Costs 是否在 forced sample 下完全离线；
- test database/role/log/process 是否 finally 清理。

输出：

- Scenario matrix；
- Identity/permission matrix；
- Process lifecycle matrix；
- 未覆盖的生产 migration 风险。
```

### 第 9 轮：Day 19 CI、PR 契约与 GitHub 治理

读取：

- `.github/workflows/ci.yml`
- `.github/PULL_REQUEST_TEMPLATE.md`
- `.github/CODEOWNERS`
- `docs/phase-1/engineering-governance.md`
- `docs/phase-1/stage-1-gate-report.md`
- `scripts/Test-GitHubActions.ps1`
- ADR template

提示词：

```text
执行第 9 轮：CI and Merge Contract Review。

检查：

1. pull_request、main push、workflow_dispatch 触发是否正确。
2. workflow permissions 是否最小。
3. action 是否固定 immutable SHA，版本注释是否对应。
4. .NET/Terraform/tool versions 是否明确。
5. CI 是否复用仓库脚本，而不是复制逻辑。
6. 两个 required check name 是否稳定且与 branch protection 证据一致。
7. timeout 和 concurrency 是否可能取消必要证据或留下资源。
8. fork PR、并行 run、重复 push 下的行为。
9. Database migration job 是否依赖 runner 隐含软件。
10. PR template 是否要求 negative test、migration 和文档影响。
11. CODEOWNERS 当前只有单 Owner 的限制是否诚实说明。
12. engineering-governance 是否仍错误写 branch protection 为 deferred。
13. stage report 中 run ID、commit、测试数和结论是否互相一致。
14. CI 全绿是否被错误解释成 production readiness。

区分：

- Workflow configuration evidence；
- Branch protection external evidence；
- Reported historical evidence；
- 仅凭源码无法重新证明的 GitHub 设置。

无法从上传文件验证的 branch protection 必须标记 NEED_MORE_EVIDENCE，要求 Owner
提供 GitHub 设置截图或 API 输出，不能假设存在。
```

### 第 10 轮：Day 1～7 回归与跨域兼容

阶段 1 主要是治理和重构，但必须确认旧能力没有被破坏。

读取：

- Domain/Application 核心实现；
- Azure Providers；
- PostgreSQL Repositories；
- API endpoint；
- Worker handlers；
- 现有 39 项 tests；
- 六个 Azure/Terraform E2E；
- README、Azure、data model 文档。

提示词：

```text
执行第 10 轮：Day 1-7 Regression Review after Phase 1 Refactoring。

沿真实数据流审查：

A. Azure subscription API
B. Resource inventory Worker
C. Resource ETL admin API and history
D. Cost sync Worker/admin API
E. Cost daily/service/resource-group queries
F. PostgreSQL readiness
G. Terraform lifecycle

对每条链输出：

Entry → Application service → Provider/Repository → Domain/DTO → Persistence/API

检查：

1. route/module 拆分是否改变入口。
2. DI 拆分是否改变实现或 lifetime。
3. Worker handler 是否改变 job name、参数或退出行为。
4. 移除自动 migration 后，E2E 是否在宿主前显式运行 Migrator。
5. forced sample 是否仍带 provenance，且不会触碰 Azure identity。
6. Resource/Cost Upsert identity、FirstSeen/LastSeen、币种隔离是否未改变。
7. ETL Failed/Succeeded 记录是否保持。
8. API/Worker 空库失败是否符合新 migration 契约。
9. 哪些真实 Azure E2E 在 Phase 1 没有重跑，为什么。
10. 当前 39 tests 对每条链覆盖什么、不覆盖什么。

不要因为编译通过就判定行为兼容。
```

### 第 11 轮：文档、ADR、风险和证据交叉审计

读取：

- `README.md`
- `docs/02-★★★-configuration-guide.md`
- `docs/04-★★★-azure-integration.md`
- `docs/05-★★★-data-model.md`
- `docs/adr/*.md`
- `docs/phase-0/*.md`
- `docs/phase-1/*.md`
- `construction/*.md`

提示词：

```text
执行第 11 轮：Documentation and Evidence Consistency Review。

建立 Claim-Evidence Matrix：

Claim | Source document | Code evidence | Test/CI evidence | Contradiction | Verdict

至少覆盖：

- 项目数量和依赖方向；
- endpoint 模块；
- DI 模块；
- Worker handler registry；
- independent Migrator；
- API/Worker 不自动 migration；
- advisory lock；
- restricted runtime DB role；
- analyzer/format gate；
- architecture tests；
- required checks；
- branch protection；
- 测试数量；
- forced sample 行为；
- 已知 dependency/tool drift；
- 后续阶段未完成能力。

检查 ADR 状态是否 Accepted、决定是否与实现一致、verification 条件是否真正满足。
检查 risk/gap 中的状态变化是否有证据，避免 Open/Mitigated/Closed 混淆。
```

### 第 12 轮：对抗性总审查与签收建议

这一轮不能新增阅读范围，必须基于前十一轮 ledger 和已读源码。

提示词：

```text
执行第 12 轮：Adversarial Synthesis and Acceptance Recommendation。

第一步：攻击自己的前十一轮结论。

请列出至少 15 个“如果我们错了，会错在哪里”的反例，包括：

- 没读到文件；
- 把文档声明当运行证据；
- 测试只验证实现细节；
- Windows 通过但 Linux 失败；
- CI name 与 branch protection 漂移；
- 新文件绕过架构/secret/parse gate；
- wrapper process/orphan process；
- migration lock 或权限误判；
- forced sample 仍访问外部依赖；
- cleanup 失败被 finally 隐藏；
- E2E 未运行却被写成通过；
- 风险状态被过度关闭；
- route/DI/Worker 行为在重构中改变；
- Terraform init 修改仓库；
- dependency report 被误当 dependency policy 完成。

第二步：整理最终 Review Ledger。

- 合并重复 finding；
- 保留争议；
- 不得静默降低严重度；
- 标记证据不足项；
- 给每个 Open finding 指定阻断与否。

第三步：输出决策，只能三选一：

ACCEPT
条件：无 Open Critical/High；无未解释 Medium；证据链完整；进入 Day 20 不会建立
在已知不可信工程基础上。

CONDITIONAL_ACCEPT
条件：无 Critical/High；仅有明确不阻断的 Medium/Low；每项有 Owner、处理 Day、
到期条件和风险接受理由。

REJECT
条件：存在 Open Critical/High；关键证据缺失；模型没有完成文件覆盖；CI/架构/
migration 契约存在实质疑问；或报告结论与代码冲突。

最终报告必须包含：

1. Executive decision
2. Reviewed baseline
3. Files actually reviewed
4. Files not reviewed
5. Phase 1 requirement matrix
6. Final Review Ledger
7. Negative-test matrix
8. Residual risks
9. Required fixes before Day 20
10. Conditional backlog
11. Owner sign-off block
12. Machine-review limitations

禁止使用“总体不错”“基本可以”“建议继续完善”作为最终结论。
```

## 7. 阶段 1 要求矩阵

最终 reviewer 必须填完，不允许空白：

| Day | Requirement | Code evidence | Automated evidence | Negative evidence | Residual risk | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 12 | analyzer、format、统一编译策略 |  |  |  |  |  |
| 13 | 单一静态门禁、失败非零、跨平台 |  |  |  |  |  |
| 14 | 可执行架构边界 |  |  |  |  |  |
| 15 | endpoint 模块化且契约兼容 |  |  |  |  |  |
| 16 | DI 拆分与生命周期正确 |  |  |  |  |  |
| 17 | Worker handler registry 与退出语义 |  |  |  |  |  |
| 18 | 独立 migration、并发、权限、清理 |  |  |  |  |  |
| 19 | CI、PR、ADR、责任边界、合并阻断 |  |  |  |  |  |

Verdict 只允许：

- Verified
- Partially verified
- Not verified
- Contradicted

任何 `Contradicted` 默认阻止签收；`Not verified` 必须由 Owner 决定补证据还是
拒绝签收。

## 8. 必须独立验证的 Negative Tests

reviewer 不一定能在 ChatGPT 沙箱中运行 .NET、Docker、Terraform 或 GitHub
Actions，但必须审查这些 negative tests 的设计是否可信，并明确哪些只能由 Owner
在真实仓库执行。

| ID | 故障注入 | 预期阻断 |
| --- | --- | --- |
| N01 | C# 格式违规 | `dotnet format` 或 build 失败 |
| N02 | Analyzer 违规 | build 失败 |
| N03 | 未跟踪非法 JSON | static gate 失败 |
| N04 | GitHub workflow 非法 runner label | actionlint 失败 |
| N05 | Domain 反向引用 Infrastructure | architecture test 失败 |
| N06 | Application 引入 Azure SDK | architecture test 失败 |
| N07 | 从 endpoint mounting 移除 Costs | route test 失败 |
| N08 | 移除关键 DI 注册 | DI graph test 失败 |
| N09 | 重复 Worker Job name | dispatcher 构造失败 |
| N10 | 未知 Worker Job | 进程非零 |
| N11 | Worker handler 抛异常 | 进程非零且宿主停止 |
| N12 | API/Worker 新增 MigrateAsync | architecture test 失败 |
| N13 | 同库 migration lock 已占用 | 第二个 Migrator 返回 1 |
| N14 | 数据库端口不可连接 | Migrator 返回 1 |
| N15 | runtime role 无 schema CREATE | API/Worker 仍可运行 |
| N16 | forced sample + 无 Azure identity | Costs Worker 离线成功 |
| N17 | API 进程停止 | 不遗留 orphan process |
| N18 | workflow check pending | PR 显示 blocked |
| N19 | required check failed | PR 不可合并 |
| N20 | verification 生成工作树变更 | static gate 失败 |

最终报告必须标记每项为：

- Observed directly
- Supported by committed test
- Reported by CI evidence
- Not independently verified

## 9. 网页版不能做什么

即使使用高级模型，也必须承认以下限制。

### 9.1 上传源码不等于真实 Git checkout

ZIP 通常缺少：

- `.git` 历史；
- branch protection 设置；
- GitHub check API；
- ignored 本地文件；
- 本地 Docker 状态；
- Azure subscription 状态。

因此模型不能单独证明：

- 某个 commit 确实是 `main`；
- branch protection 当前仍启用；
- PR 当时真的被阻断；
- Azure 资源已经清理；
- 本机没有测试数据库；
- CI 日志没有被截取或转述错误。

这些项目需要 Owner 提供 GitHub URL、截图、API 输出或本地命令结果。

### 9.2 能运行 Python 不等于能运行项目

即使网页会话提供数据分析环境，也不能假设它拥有：

- .NET 10 SDK；
- Docker daemon；
- PowerShell 7；
- Terraform；
- Azure CLI 登录；
- PostgreSQL；
- GitHub 管理权限。

模型必须逐项报告实际可用工具。没有运行环境时，只做静态审查，禁止伪造执行
结果。

### 9.3 模型结论不是安全认证

本次 review 是工程决策辅助，不是：

- 渗透测试；
- 合规认证；
- 法律审计；
- 财务审计；
- 生产 readiness certification。

## 10. Owner 补证据提示词

当 reviewer 输出 NEED_MORE_EVIDENCE 时，用下面格式补充，避免把一大段日志无
结构地粘贴进去：

```text
Evidence request ID:
Question being answered:
Source:
Commit SHA:
Command or GitHub URL:
Started at:
Finished at:
Exit code / conclusion:
Relevant excerpt:
Cleanup evidence:
Sensitive values removed: yes/no
```

不要上传完整环境变量、token、connection string 或未脱敏日志。

## 11. Finding 修复后的复审流程

如果最终为 REJECT 或 CONDITIONAL_ACCEPT：

1. 将 findings 原样保存；
2. 每个修复建立独立 branch/PR；
3. PR 描述引用 finding ID；
4. 增加能先失败、修复后通过的 negative test；
5. 通过 `Static verification` 和必要的 `Database migration`；
6. 合并后重新下载最新 `main`；
7. 新会话只执行“修复复审”，不要让模型重写原始 finding；
8. 更新 ledger 状态为 `Fixed, awaiting verification`；
9. reviewer 验证后才改为 `Closed`；
10. 所有阻断项关闭后重新执行第 12 轮。

复审提示词：

```text
这是 finding <ID> 的修复版本。

请先复述原 finding、原证据和原失败方式，然后审查：

1. 修复是否处理根因而不是压掉测试；
2. 是否引入更宽权限、更弱检查或新的平台假设；
3. negative test 是否在旧实现上会失败；
4. GitHub-hosted required checks 是否在新 commit 上通过；
5. 文档和风险状态是否同步。

只允许输出：

- CLOSED
- STILL_OPEN
- REGRESSION_INTRODUCED
- NEED_MORE_EVIDENCE
```

## 12. 最终签收模板

只有 reviewer 给出 `ACCEPT`，或 Owner 明确接受符合条件的
`CONDITIONAL_ACCEPT`，才填写：

```text
# Phase 1 Independent Acceptance

Repository:
Branch:
Commit SHA:
Review started:
Review completed:
Reviewer surface/model:
Review guide version:

Decision:
ACCEPT / CONDITIONAL_ACCEPT / REJECT

Critical open:
High open:
Medium open:
Low open:

Required checks:
- Static verification:
- Database migration:

Independent review report:
<保存位置或链接>

Accepted residual risks:
- ID:
  Owner:
  Target Day:
  Expiry/revisit trigger:

Owner decision:

Owner:
Date:

Authorization:
- [ ] Phase 1 accepted
- [ ] Day 20 may start
```

签收规则：

- `Critical open > 0`：必须 REJECT；
- `High open > 0`：必须 REJECT；
- `Medium open > 0`：默认不签收，除非每项都有书面风险接受；
- `Needs evidence` 指向核心门禁：不得签收；
- commit SHA 不一致：不得签收；
- reviewer 未列出实际读取文件：不得签收；
- reviewer 只做了一轮概括性 review：不得签收。

## 13. 建议保存的最终产物

网页版 review 完成后，保存以下文件到本地 `tmp/phase-1-independent-review/`：

```text
00-ingestion-report.md
01-scope-review.md
02-toolchain-review.md
03-static-gate-review.md
04-architecture-review.md
05-api-review.md
06-di-review.md
07-worker-review.md
08-migration-review.md
09-ci-governance-review.md
10-regression-review.md
11-documentation-evidence-review.md
12-final-acceptance-report.md
review-ledger.csv
owner-evidence-index.md
```

这些材料默认属于本地 evidence，不自动提交 Git。若最终需要把签收结果纳入仓库，
应另写一份脱敏、精简、可长期维护的：

```text
docs/phase-1/independent-acceptance-report.md
```

不要提交：

- ChatGPT 原始会话导出中的个人信息；
- token；
- 未脱敏日志；
- 本地绝对路径；
- 无法长期验证的临时截图；
- `tmp/` 全量输出。

## 14. 进入 Day 20 的唯一允许路径

```text
Day 19 Engineering Gate Passed
    ↓
下载并固定 main commit
    ↓
第 0 轮确认材料完整
    ↓
第 1～11 轮逐域审查
    ↓
第 12 轮对抗性汇总
    ↓
修复所有 Critical/High
    ↓
处理或接受 Medium
    ↓
Owner 填写 Independent Acceptance
    ↓
更新阶段报告
    ↓
才允许启动 Day 20
```

任何以下捷径都不允许：

- “CI 已经绿了，所以直接进入 Day 20”；
- “ChatGPT 说总体不错，所以签收”；
- “没有发现问题”等同于读取了全部文件；
- “未来会补测试”用于关闭当前 High finding；
- “这是学习项目”用于接受 secret、数据损坏、权限或 migration 风险；
- 为追赶 Day 编号而跳过独立 review。

## 15. 最终原则

```text
全面 review 不是再写一遍项目总结。

它必须证明：

审查对象固定，
文件覆盖可见，
声明能映射到代码，
代码能映射到测试，
测试能映射到失败方式，
外部证据与仓库事实一致，
剩余风险被明确保留，
最终决策可以被复查。
```

高级模型可以提高阅读和推理效率，但不能替代证据、真实运行和 Owner 责任。
