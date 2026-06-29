# ADR-0018: 依赖与工具链治理

## 状态

Accepted - 项目 Owner 于 2026-06-18 批准。

## 背景

2026-06-18 文档 review 发现：

- 配置的 NuGet source 当前未报告 vulnerability；
- `xunit 2.9.3` 仍标记为 Legacy；
- 多个 NuGet package 有可用新版本；
- 本地 Terraform CLI 为 `1.14.0`，当时 `1.15.6` 已可用；
- 仓库没有已提交的 dependency update policy 或可重复 dependency gate。

相关风险：

- RISK-0013：无 CI/CD 自动门禁。
- RISK-0022：无自动 secret、dependency、container 或 IaC 门禁。
- RISK-0023：xUnit v2 package 标记为 Legacy。
- RISK-0027：dependency 和 toolchain version drift。

## 决策

Stage 1 先建立保守、可重复的 dependency 和 toolchain gate，再进行大范围升级。

第一版 gate 作为 `scripts/Test-RepositoryStatic.ps1` 的一部分实现，并检查：

1. `dotnet --version`
2. `dotnet tool restore`
3. `dotnet list FinOpsPlatform.slnx package --vulnerable --include-transitive`
4. `dotnet list FinOpsPlatform.slnx package --deprecated`
5. `dotnet list FinOpsPlatform.slnx package --outdated`
6. `terraform -chdir=terraform/azure version`
7. `terraform -chdir=terraform/azure fmt -check`
8. `terraform -chdir=terraform/azure init -backend=false -input=false`
9. `terraform -chdir=terraform/azure validate`

Stage 1 将 vulnerable package 视为阻断项。deprecated 和 outdated package 会报告并跟踪；
只有 ADR、risk 或 stage gate 明确要求时才变成阻断项。这样可以避免项目在缺少 CI 证据和 migration
覆盖前进行大范围意外升级。

第一个升级目标仍是 xUnit v2 到 xUnit v3，但迁移必须作为聚焦变更完成，并保证全部测试通过，不能混入静态门禁工作。

## 考虑过的替代方案

### 立即升级所有 outdated dependency

Rejected。它会把治理变更和行为变更混在一起，使回归更难归因。

### 到 Stage 14 前忽略 outdated dependency

Rejected。Stage 14 是完整供应链门禁，但 Stage 1 需要足够可见性，避免已知漂移静默累积。

### 先引入 Dependabot 或 Renovate

Deferred。自动 PR 在 CI 可靠后很有用。在此之前，自动更新会制造噪声，却没有强制回归证据。

## 后果

- Stage 1 获得可重复 dependency 可见性，但不强制不安全的大范围升级。
- 仓库可以区分 vulnerability blocking 和 maintenance drift。
- CI 和 Owner review policy 建立后，仍可引入 Dependabot 或 Renovate。
- Terraform provider lock 变化必须保持人工 review。`terraform init -upgrade` 不能作为自动静态检查运行。

## Stage 1 实施挂钩

- Day 12 记录 formatting 和 analyzer baseline。
- Day 13 新增 `scripts/Test-RepositoryStatic.ps1`，并把 dependency checks 纳入其中。
- Day 19 CI 调用同一脚本。
- RISK-0023 在 xUnit v3 migration 完成前保持 open。
- RISK-0027 在 gate 存在且升级策略写入 CI/review 文档前保持 open。

## 验证

Stage 1 未满足以下条件前不能视为完成：

- 静态验证脚本打印 dependency 和 toolchain 结果。
- CLI 报告 vulnerable package 时，脚本以非零退出。
- deprecated 和 outdated package 输出保留在日志中，或汇总到 closeout note。
- Terraform validation 不向 Git 写入 state 或 plan。
- 文档说明 provider lock update 需要显式 review。
