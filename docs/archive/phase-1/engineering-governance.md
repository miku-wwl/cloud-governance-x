# Phase 1 工程治理

## 目的

Phase 1 把本地工程规则转成可重复的 merge contract。CI 不表示系统生产可用；它的作用是防止已知格式、架构、依赖、secret、Terraform、测试和 migration 回归在无人察觉的情况下进入 main branch。

## Required CI checks

`.github/workflows/ci.yml` 发布两个 check name：

- `Static verification`
- `Database migration`

仓库 branch protection 要求 pull request 合入 `main` 前这两个 check 均通过。配置使用 strict/up-to-date mode，适用于 administrator，并禁止 force push 和 branch deletion。branch protection 仍是仓库 Owner 的外部设置；workflow 不会自动创建或保持它。

阶段报告记录的是 2026-06-18 捕获的证据；后续 commit 的独立 review 必须重新获取当时的外部证据，不能把历史快照当作新 SHA 的证明。

## 验证入口

| 入口 | 职责 |
| --- | --- |
| `scripts/Test-RepositoryStatic.ps1` | candidate file、secret、syntax、actionlint、dependency、format、build、test 和 Terraform 检查 |
| `scripts/Test-DatabaseMigration.ps1` | 空库、重复、并发、失败 migration 路径，以及受限 runtime database identity |
| Azure/Terraform E2E scripts | 显式外部资源验证；Phase 1 CI 中不自动运行 |

CI 必须复用仓库自有脚本，不在 workflow YAML 中复制规则。

## 责任边界

| 区域 | 主要责任 | 必审重点 |
| --- | --- | --- |
| Domain/Application | Application Owner | 业务不变量、port、未来 tenant-safe 演进 |
| Infrastructure/PostgreSQL | Data Owner / Platform SRE | mapping、migration、credential、permission、failure behavior |
| API/Worker/Migrator | Application Owner / Platform SRE | composition、lifecycle、exit code、release ordering |
| Terraform | Cloud Provider Owner | provider lock、state safety、destructive impact、identity |
| CI/scripts/dependencies | Platform SRE / Security Owner | least privilege、pinned tools、secret 和 supply-chain gate |
| ADR/risk/gap documents | Decision owner | fact、acceptance status、evidence、remaining risk |

初版 CODEOWNERS 在更多 maintainer 和团队出现前，将所有区域映射到当前项目 Owner。

## Pull request contract

每个 pull request 必须：

1. 只有一个可 review 的目的；
2. 标识 runtime、schema、identity、data、deployment 和 rollback 影响；
3. 在适用时通过两个 required CI checks；
4. 新增或修改 gate 时包含 negative test；
5. 事实变化时更新 ADR、risk、gap、README 和运行文档；
6. 排除 credential、state、plan、log、本地 evidence 和 generated output。

## 外部 action 和工具策略

- GitHub Actions 固定到 immutable commit SHA，并在注释中记录 review 过的 release version。
- .NET 从 `global.json` 选择。
- Terraform CLI 和 actionlint 版本必须明确。
- `terraform init -upgrade` 绝不属于自动 gate。
- vulnerable NuGet package 阻断 CI；deprecated 和 outdated package 保持可见，但升级前需要聚焦 review。

## 延期控制项

以下内容不属于 Day 19 gate：

- signed commits；
- Dependabot 或 Renovate；
- SBOM、container、license-policy、provenance 和 historical secret scanning；
- deployment、staging、release approval、rollback、backup 和 PITR。

这些内容由后续 phase 跟踪，不能从 Phase 1 workflow 全绿推断它们已经完成。
