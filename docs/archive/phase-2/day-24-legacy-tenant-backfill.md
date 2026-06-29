# Day 24 Legacy Tenant Backfill 评审

日期：2026-06-20
状态：Validation

## 1. 目标

为 Day 1-7 遗留的 `tenant_id = NULL` 行提供受控 development-only backfill 路径，避免用直接 SQL 或删除重建方式破坏历史数据。

## 2. 实现范围

Day 24 完成：

- 独立 Migrator operation；
- dry-run 默认；
- 显式 `-Apply`；
- 必需 Organization ID 和 Tenant ID；
- writer-stop acknowledgement；
- NOWAIT lock 和 advisory lock；
- apply 前后的 row-count confirmation；
- Provider normalization 和 collision check；
- 受控创建 ProviderConnection 和 CloudAccount；
- completion marker；
- post-backfill NULL-write constraint；
- production environment rejection。

Day 24 不做：

- 生产大数据量迁移演练；
- restore rehearsal；
- OIDC；
- RBAC；
- 端点授权；
- RLS；
- 审计。

## 3. 关键设计

backfill 默认只 dry-run。真正 apply 时必须显式确认，并要求调用者确认 writer 已停止。

回填必须可重复验证，并在完成后阻止继续写入无 tenant 数据。completion marker 用于防止完成后走危险 down path。

## 4. 验证证据

tracked report 记录数据库门禁覆盖：

- dry-run；
- apply；
- second apply；
- collision failure；
- active-writer failure；
- stale count failure；
- production environment rejection；
- post-backfill NULL write rejection；
- completion marker 后 Down rejection。

相关脚本：

- [scripts/Invoke-DevelopmentTenantBackfill.ps1](../../../scripts/Invoke-DevelopmentTenantBackfill.ps1)

## 5. Review 结论

Validation。实现已完成，但源报告保持 Validation。该工具仍是 development-only，不得作为生产迁移方案使用。

## 6. 遗留风险

EF model 仍保持 nullable 兼容；large-data timing、lock duration、restore rehearsal、OIDC、RBAC、端点授权、RLS 和审计 留给后续 Day。

## 7. 相关链接

- [docs/days/day-24.md](../../days/day-24.md)
- [ADR-0003](../adr/ADR-0003-organization-tenant-cloud-account-model.md)
