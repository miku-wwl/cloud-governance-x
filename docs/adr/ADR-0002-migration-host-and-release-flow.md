# ADR-0002: Migration Host 与发布流程

## 状态

Accepted - 项目 Owner 于 2026-06-18 批准。

## 背景

Day 18 之前，API 和 Worker 在 startup 时调用 `MigrateAsync`。这对本地学习基线有用，但不适合生产：

- 多个 API 或 Worker 实例可能竞争执行 schema change；
- runtime identity 需要 DDL 权限；
- application startup failure 会和 migration 行为耦合；
- 没有明确的 migration approval、证据或 rollback path。

相关风险：

- RISK-0003：API/Worker 自动 migration。
- RISK-0012：无 staging 和 artifact promotion chain。
- RISK-0013：无 CI/CD 自动门禁。

## 决策

Stage 1 移除 API 和 Worker startup 中的自动 migration，并增加专用 migration executable path。

实现是一个小型 `FinOps.Migrator` console project，必须：

1. 引用 `FinOps.Infrastructure`；
2. 使用与 API/Worker 相同形状的 PostgreSQL 配置；
3. 获取目标数据库的 PostgreSQL advisory lock；
4. 应用 EF Core migrations 后退出；
5. 成功返回 `0`，migration 失败返回 `1`；
6. 记录数据库目标、pending migration 数量、已应用 migration 名称和耗时，但不记录 connection string 或 password。

本地开发可以在 API 或 Worker startup 前手工运行 migrator，或通过脚本运行。未来 CI/CD 会在部署应用实例前，将同一个 migrator 作为 release step 执行。

## 考虑过的替代方案

### 所有环境继续使用 startup migration

Rejected。它能简化本地启动，但无法关闭 RISK-0003，并迫使应用 runtime identity 保留 schema-change 权限。

### 只使用 `dotnet ef database update`

Rejected as primary release path。它适合开发，但项目自有 migrator 给 release pipeline 一个稳定 executable，并允许项目加入 logging、validation 和 environment checks。

### 只在 CI 中运行 migration，不创建 migrator

Deferred。当时 CI/CD 尚未存在。migrator 先创建可复用单元，未来 CI/CD 可以编排它。

## 后果

- API 和 Worker startup 更简单、更安全。
- 本地 setup 增加一个显式 migration step。
- Stage 1 必须更新脚本和文档，使空数据库仍可重复准备。
- 类生产部署可以使用不同数据库 identity：migrator 拥有 DDL 权限，API/Worker 只拥有运行所需权限。
- 同一数据库的并发 FinOps migrator 会在应用 schema change 前失败；部署并发控制仍由外层 release guard 负责。
- Migration rollback 仍是受控 release concern；生产中不会自动执行 EF `Down`。

## Stage 1 实施挂钩

- Day 18 负责实现。
- 新增 `src/FinOps.Migrator/FinOps.Migrator.csproj`。
- 将项目加入 `FinOpsPlatform.slnx`。
- 从 `src/FinOps.Api/Program.cs` 和 `src/FinOps.Worker/Worker.cs` 移除 `MigrateAsync` 调用。
- 新增或更新脚本，使本地验证可以执行：

```powershell
dotnet run --project src/FinOps.Migrator
dotnet run --project src/FinOps.Api --urls http://localhost:5000
$env:Etl__Job = "Resources"
dotnet run --project src/FinOps.Worker
```

## 验证

Stage 1 未满足以下条件前不能视为完成：

- 空本地数据库可由 `FinOps.Migrator` 完成 migration。
- 重复运行 `FinOps.Migrator` 是幂等的。
- 两个并发 FinOps migrator 不能同时对同一数据库应用 migration。
- API 和 Worker 在 migration 后可成功启动。
- API 和 Worker 不调用 `Database.MigrateAsync`。
- migration 失败返回非零退出码。
- 文档明确说明生产 migration 是独立 release step。
