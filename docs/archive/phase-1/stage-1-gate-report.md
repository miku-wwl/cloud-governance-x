# Phase 1 阶段门禁报告

日期：2026-06-18
范围：Day 12～19
状态：`EngineeringGatePassed`
独立验收：`ACCEPT`

2026-06-19，独立端到端审查基于
`main@2062b0fe835bf30888ad412e68bd35092f25d9b7` 完成。审查未发现仍打开的
Phase 1 Critical、High、Medium 或 Low finding。正式决策和遗留风险边界记录在
[`independent-acceptance-report.md`](independent-acceptance-report.md)。

本报告保存下列 commit 和 workflow run 的证据。37、39 等历史测试数量绑定当时
的 workflow run，测试套件增长后不回写这些历史数字。当前工作基线有 44 个可执行
测试；新的 commit 仍必须拥有自己的两项 CI 结果和当前 branch protection 证据，
才能进入独立验收。

2026-06-19 重新检查 repository protection API：`main` 仍要求
`Static verification` 和 `Database migration`，strict mode 与 administrator
enforcement 已启用，force push 与 branch deletion 已禁用。这确认了当时的仓库
设置，但不能替代下一个 commit 所需的 CI run。

## 已实现控制

- 共享 analyzer、formatting 和 compilation baseline；
- 单一仓库静态验证入口；
- 可执行 architecture 与 infrastructure package 边界；
- 模块化 API endpoint、DI 和 Worker Job 组合；
- 带 advisory lock 的专用数据库 Migrator；
- 可重复 database migration 与 restricted-role 回归；
- GitHub Actions CI、pull-request template、ADR template、CODEOWNERS 和责任边界。

## 必需证据

Phase 1 工程门禁只有在以下条件满足时才算完成：

- `Static verification` 在干净 GitHub-hosted runner 上通过；
- `Database migration` 在干净 GitHub-hosted runner 上通过；
- format、architecture、test 或 migration 故障会让对应 job 失败；
- actionlint 接受已提交 workflow；
- 本地与 CI 验证不会留下仓库 artifact；
- `main` branch protection 要求合并前通过两个 CI check；
- ADR-0001、ADR-0002 和 ADR-0018 已接受；
- risk、gap、baseline、README 和 configuration 文档与最终仓库状态一致。

## GitHub 托管证据

前两个 workflow run 暴露了真实跨平台与离线执行缺陷，而不是直接完成门禁：

- run `27747051177` 在 commit `029b048` 上两个 job 均失败。失败证明 workflow
  会阻断无效 PowerShell 路径假设和 Linux-specific architecture-test 路径不匹配；
- run `27747251543` 在 commit `c52f9f5` 上通过 `Static verification`，包括 build
  和当时全部 37 个测试，运行环境为 `ubuntu-24.04`；
- 同一 run 的 `Database migration` 在 20 分钟超时处被取消。空库、重复运行、并发、
  failure-exit 和 restricted-role API 检查已经通过，但 restricted-role Costs Worker
  在 `AzureCost__ForceSampleData=true` 时仍等待 Azure authentication。

取消的根因是 `AzureCostProvider` 在尊重 forced sample mode 前先枚举 Azure
subscriptions。整改后，forced sample data 会在构造任何 Azure request path 之前返回；
回归测试使用一个“只要被触碰就失败”的 credential。

第一次整改 run 又暴露了第二个 Linux-only process-boundary 缺陷：
`Start-Process dotnet run` 会停止 wrapper process，却留下实际 API child process，
导致 migration script 无限等待。门禁现在直接运行已构建的 API 与 Worker DLL。

Pull request `#2` 证明了完整 merge contract：

- required checks 完成前，GitHub 将 PR 标记为 `BLOCKED`；
- workflow run `27750735956` 在 commit `ecf9bba` 上通过 `Static verification` 和
  `Database migration`；
- 同一 run 通过全部 39 个测试、formatting、build、actionlint、dependency、
  Terraform、migration、concurrency、failure-exit 和 restricted-role checks；
- 两个 required checks 成功后，GitHub 将 PR 状态改为 `CLEAN`；
- PR `#2` 以 commit `a7d09f5` 合并。

## 本地整改证据

被取消的 workflow 后，本地验证结果如下：

- `Test-RepositoryStatic.ps1` 通过；
- actionlint 1.7.12 接受 workflow；
- 非法 runner-label probe 按预期使 actionlint 失败；
- build 以 0 warnings、0 errors 完成；
- forced-sample offline 回归测试通过；
- 全部 39 个测试通过；
- direct host-process 修复后，`Test-DatabaseMigration.ps1 -NoBuild` 在 22 秒内通过，
  包括 GitHub Actions 中超时的 restricted-role Costs Worker 场景；
- Terraform fmt/init/validate 通过，且不改变 Git status；
- 所有 migration 测试数据库、角色、进程和临时日志均已清理。

Day 12～18 的早期 closeout 证据也记录了 format、architecture、route、DI、
migration 和 test 的刻意失败路径均返回非零结果。这些本地文件按 `.gitignore`
排除；本报告是提交到仓库的阶段摘要。

## 关闭确认

Phase 1 工程门禁于 2026-06-18 关闭：

- `main` branch protection 要求 `Static verification` 和 `Database migration`；
- required checks 使用 strict/up-to-date mode；
- administrators 不能绕过 protection；
- force push 和 branch deletion 已禁用；
- 受保护 PR 在 checks pending 时被阻止，只有两个 checks 均通过后才可合并；
- GitHub-hosted 与本地门禁都没有留下 tracked repository artifact。

最终 Owner acceptance 与工程门禁分离。Day 20 启动前，固定的 `main` commit
必须完成 `construction/04-★★★-phase-1-independent-review-guide.md` 定义的独立审查。
审查必须产生完整 ledger，关闭所有 Critical/High finding，处理 Medium finding，
并记录 Owner 决策。

## 剩余风险

绿色 Phase 1 gate 不提供 authentication、tenant isolation、production identities、
deployment approval、backup、PITR、reliable scheduling、staging、SLO、container
scanning、SBOM 或 provenance。这些控制仍分配给后续阶段。
