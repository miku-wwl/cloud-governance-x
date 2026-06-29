# Phase 1 第三版整改与复审说明

日期：2026-06-19
仓库：`miku-wwl/cloud-governance-x`
Pull request：[#6](https://github.com/miku-wwl/cloud-governance-x/pull/6)
整改实现提交：`5af852ee71233f3c7a68e61f314a83c483fb1c47`
对比基线：`4d2761a21f2fe474f9b83822615ff5aeaff77d6e`

## 1. 提交给专家组的结论

第二版复审指出的两个 High 均已复现、修复，并增加可执行回归：

1. P1-001 不再使用源码正则判断 migration 所有权，改为检查编译程序集的
   metadata/IL。
2. P1-011 的 Markdown 检查已排除 fenced code，并加入 PowerShell 与 C#
   代码块回归 fixture。

同时完成文档事实更新、Worker handler 真实进程失败路径，以及 migration
cleanup 保留原始异常的修复。

本报告不自行宣布 Phase 1 accepted。请专家组基于本提交和下列证据进行第三次
独立复审。

## 2. GitHub 与本地证据

整改实现提交的 GitHub Actions：

- workflow run：
  [27764950828](https://github.com/miku-wwl/cloud-governance-x/actions/runs/27764950828)
- `Static verification`：
  [通过，1m09s](https://github.com/miku-wwl/cloud-governance-x/actions/runs/27764950828/job/82149000008)
- `Database migration`：
  [通过，1m06s](https://github.com/miku-wwl/cloud-governance-x/actions/runs/27764950828/job/82149000094)
- GitGuardian Security Checks：通过

本地完整验证：

- `scripts/Test-RepositoryStatic.ps1`：通过
- .NET build：0 warnings，0 errors
- .NET tests：44 passed，0 failed，0 skipped
- Terraform fmt/init/validate：通过
- `scripts/Test-DatabaseMigration.ps1`：通过
- `git diff --check`：通过

2026-06-19 通过 GitHub repository API 重新检查 `main`：

- required checks：`Static verification`、`Database migration`
- strict/up-to-date：启用
- administrator enforcement：启用
- force push：禁用
- branch deletion：禁用

直接 push `main` 被 GitHub 以 `GH006` 拒绝，并明确提示两个 required checks
尚未产生；因此整改通过受保护 PR #6 提交。

## 3. High findings

### P1-001：migration 架构门禁

原实现只用正则扫描源码中的 `MigrateAsync(...)`，确实无法发现方法组和委托
别名。该 finding 成立。

整改后，架构测试读取 Domain、Application、Infrastructure、API 和 Worker
编译程序集，检查：

- EF Core schema API 的 member reference：
  `Migrate`、`MigrateAsync`、`EnsureCreated`、`EnsureCreatedAsync`
- IL 中上述 schema API 名称的 reflection string

因此普通直接调用、静态扩展调用和方法组/委托别名都会在编译产物中留下
member reference，不再依赖源码写法。

新增两个可执行 negative fixtures：

- `Database_schema_api_scanner_detects_method_group_aliases`
- `Database_schema_api_scanner_detects_reflection_method_names`

边界说明：任何静态门禁都不能声称识别运行时拼接字符串、外部配置或动态加载
产生的任意反射行为。本次门禁覆盖编译产物中的 schema API 引用及常见的固定
方法名反射；动态代码执行仍属于人工审查和安全治理边界。

建议状态：**Fixed and verified**。

### P1-011：Markdown fenced-code false positive

该 finding 成立。原 reference-style link 正则会把 fenced code 中的
`[void][scriptblock]` 解释成 Markdown reference。

整改内容：

- 同时识别反引号和波浪线 fence；
- 支持至少三个 fence 字符及更长 closing fence；
- reference definition、inline link 和 reference usage 均只扫描 prose；
- 保留原文件行号用于错误报告；
- 允许 Markdown 空行，不触发 PowerShell parameter binding failure。

新增回归 fixture：

- PowerShell：`[void][scriptblock]::Create(`
- C#：`[Fact][Trait]`
- prose 中的断裂 reference 仍必须被检测。

当前仓库的 `Markdown local links` 和完整 `Static verification` 均已通过。

建议状态：**Fixed and verified**。

## 4. 其他 ledger disposition

| ID | 第三版结果 | 建议状态 |
| --- | --- | --- |
| P1-002 | 普通未知 YAML 继续默认拒绝；完整静态门禁通过 | Fixed and verified |
| P1-003 | cleanup failure 会导致验证失败；数据库回归通过 | Fixed and verified |
| P1-004 | E2E 直接运行 DLL；CI migration job 通过 | Fixed and verified |
| P1-005 | 当前架构、路由模块、测试数和保护规则口径已更新 | Fixed and verified |
| P1-006 | 当前保护规则已重新取证；整改 SHA 的两个 required checks 已通过 | Fixed and verified |
| P1-007 | route surface、默认 days/take、响应字段和非法 binding 有自动测试 | Dispositioned; 后续契约扩展不阻断 Phase 1 |
| P1-008 | 新增 handler 失败的真实 Worker 进程退出码验证；unknown Job 继续覆盖 | Fixed and verified |
| P1-009 | 第二数据库 lock isolation、NoBuild artifact 和完整 migration CI 均通过 | Fixed and verified |
| P1-010 | DI 幂等测试包含在 44 个通过测试中 | Fixed and verified |
| P1-011 | fenced code 排除及回归 fixture 已通过 | Fixed and verified |
| P1-012 | cleanup 不再覆盖主验证异常；只在无主异常时抛 cleanup failure | Fixed and verified |

## 5. 对第二版评审意见的校正

以下意见正确：

- P1-001 方法组/委托别名可绕过源码正则。
- P1-011 会误报现有 fenced code。
- 当前待签收 SHA 必须有对应 CI，而不能引用旧提交的 CI。
- 当前架构和能力文档存在事实漂移。
- Worker handler exception 值得增加真实进程路径。

以下表述需要校正：

1. “所有 reflection 都必须由静态门禁识别”不是可证明的有限要求。固定方法名
   reflection 可以检查；运行时拼接、外部配置与动态加载不能由普通 IL 扫描
   完全封堵。
2. Stage report 中的 37/39 tests 是绑定特定历史 workflow run 的证据，不应
   改写成当前数量。报告现已明确标注它们是历史计数，同时记录当前为 44。
3. “多处仍写 branch protection 未启用”不能一概而论。部分内容属于历史报告
   或风险触发条件；当前事实文档已更新，外部保护规则也已于 2026-06-19
   重新验证。

## 6. 建议第三次复审重点

请专家组重点复核：

1. 对程序集 metadata/IL 的 migration API 检查是否满足 Phase 1 所有权边界。
2. 两个 migration negative fixtures 是否真实证明 alias 和固定字符串
   reflection 可被发现。
3. Markdown scanner 是否正确跳过 backtick/tilde fenced code，同时继续阻断
   prose 中的断链。
4. PR #6 的两个 required checks 是否与 branch protection context 完全一致。
5. 当前事实文档与历史证据是否已清晰分离。

## 7. 请求的评审决定

基于已关闭的两个 High、通过的本地与 GitHub 门禁、当前保护规则证据，建议：

- Critical open：0
- High open：0
- P1-006 evidence：已提供
- Phase 1：请专家组进行第三次独立复审并给出最终 acceptance
- Day 20：仅在专家组和 Owner 正式签收后开始
