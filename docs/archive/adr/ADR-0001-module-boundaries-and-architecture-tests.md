# ADR-0001: 模块边界与架构测试

## 状态

Accepted - 项目 Owner 于 2026-06-18 批准。

## 背景

当前代码库是 modular monolith，包含 7 个项目：

- `FinOps.Domain`
- `FinOps.Application`
- `FinOps.Infrastructure`
- `FinOps.Migrator`
- `FinOps.Api`
- `FinOps.Worker`
- `FinOps.Tests`

预期依赖方向是：

```text
Api / Worker -> Infrastructure -> Application -> Domain
```

在本决策前，该方向主要依靠 project reference 和 review 纪律维护，而不是自动化架构测试。Stage 1 要求核心边界被破坏时 CI 必须失败。

相关风险：

- RISK-0013：无 CI/CD 自动门禁。
- RISK-0022：无自动 secret、dependency、container 或 IaC 门禁。

## 决策

Stage 1 保持 modular monolith，并在 `FinOps.Tests` 中用 reflection-based architecture tests 执行边界约束。

第一批架构规则：

1. `FinOps.Domain` 不得引用 Application、Infrastructure、API、Worker、EF Core、ASP.NET Core、Azure SDK、Npgsql 或 hosting package。
2. `FinOps.Application` 可以引用 Domain，但不得引用 Infrastructure、API、Worker、EF Core、ASP.NET Core、Azure SDK、Npgsql 或 hosting package。
3. Azure SDK 和 Npgsql 实现依赖只能出现在 `FinOps.Infrastructure`。
4. `FinOps.Api` 和 `FinOps.Worker` 可以组合 Application 和 Infrastructure，但业务用例保留在 Application。
5. `FinOps.Migrator` 可以引用 Infrastructure，但必须保持为专用 migration executable。
6. `FinOps.Tests` 可以引用生产项目和 test-only library。

Stage 1 初始不引入新的外部 architecture-test package。如果 reflection 测试变得噪声过大或能力不足，后续可通过 ADR-0018 评估专用库。

## 考虑过的替代方案

### 只依靠 review

Rejected。review 仍然必要，但无法提供可重复 CI 证据，也容易漏掉意外 package 或 project reference 漂移。

### 立即引入专用 architecture-test package

Deferred。专用包可以改善体验，但在第一批规则尚未稳定前引入工具会扩大依赖面。第一阶段可以先使用 assembly metadata 和 project/package 文件检查。

### 拆成 microservices

Stage 1 拒绝。当前系统尚未具备生产 identity、tenant isolation、reliable jobs 或 deployment automation。过早拆分服务会在 monolith 边界稳定前增加运维复杂度。

## 后果

- 架构规则变成可执行检查，并可阻断 CI。
- 第一批规则聚焦 project 和 package 边界，不覆盖未来所有 domain module。
- Identity、Tenancy、Costs、Inventory、Compliance、Findings、Events、Audit、Operations 等名称先作为逻辑模块目标保留，等代码增长到需要明确 namespace 和测试时再落地。
- 任何有意突破边界的例外，都必须先更新本 ADR 或创建后续 ADR。

## Stage 1 实施挂钩

- Day 12：新增 `.editorconfig` 和 analyzer baseline，不改变 runtime 行为。
- Day 13：创建统一静态验证入口，目标脚本为 `scripts/Test-RepositoryStatic.ps1`。
- Day 14：在 `src/FinOps.Tests/Architecture/` 下新增架构测试。
- Day 15-17：在拆分代码时保持 route、DI 和 Worker 行为不变。
- Day 19：CI 必须运行静态验证入口和架构测试。

## 验证

Stage 1 未满足以下条件前不能视为完成：

- `dotnet test` 的正常运行包含架构测试。
- 故意引入反向依赖时，架构测试失败。
- 架构测试失败时，静态验证脚本失败。
- README 或 review 文档能把规则映射回本 ADR。
