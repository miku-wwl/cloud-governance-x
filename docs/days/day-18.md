# Day 18 - 独立 Migration Host

## 1. 目标
将数据库 schema migration 从 API/Worker startup 中移除，建立独立 migration 路径，解决运行时宿主抢跑改库和发布边界不清的生产风险。

## 2. 前置条件
依赖 Phase 1 工程基线和 [ADR-0002](../archive/adr/ADR-0002-migration-host-and-release-flow.md)。

## 3. 施工范围
允许新增 `FinOps.Migrator`、migration 调用脚本、migration 测试脚本、PostgreSQL advisory lock 和空库/重复/并发/失败连接/受限角色检查。不允许宣称生产发布编排、回滚演练或大数据量迁移验证完成。

## 4. 设计决策
API 和 Worker 不再负责自动应用 schema migration；数据库变更必须通过独立 host 或发布步骤显式执行，并用 advisory lock 阻止同库并发迁移。

## 5. 实现摘要
新增 `FinOps.Migrator`、[Invoke-DatabaseMigration.ps1](../../scripts/Invoke-DatabaseMigration.ps1)、[Test-DatabaseMigration.ps1](../../scripts/Test-DatabaseMigration.ps1)、advisory lock 行为和多种 migration gate 测试。

## 6. 验证证据
Phase 1 final acceptance 验证空库 migration、幂等 rerun、同库并发拒绝、不同库隔离、失败退出码和无 DDL runtime role 行为。证据包括 [src/FinOps.Migrator](../../src/FinOps.Migrator)、[Test-DatabaseMigration.ps1](../../scripts/Test-DatabaseMigration.ps1)、[ADR-0002](../archive/adr/ADR-0002-migration-host-and-release-flow.md) 和 [independent-acceptance-report.md](../archive/phase-1/independent-acceptance-report.md)。

## 7. Review 结论
Accepted。Phase 1 独立验收通过。

## 8. 遗留风险
生产 release orchestration、migration approval、roll-forward/rollback rehearsal 和 staging data-volume tests 留给后续平台工作。

## 9. 相关链接
- Commit: `650e902` - `refactor: isolate database migrations`
- Commit: `e646913` - migration hardening
- [docs/archive/adr/ADR-0002-migration-host-and-release-flow.md](../archive/adr/ADR-0002-migration-host-and-release-flow.md)