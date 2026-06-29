# Day 21 租户领域模型与 Schema 评审

日期：2026-06-19
决策来源：ADR-0003
状态：Validation

## 1. 目标

按 ADR-0003 实现 expand-only tenancy foundation，加入 Organization、Tenant、ProviderConnection、CloudAccount 和 Membership 的领域模型、EF 配置和数据库 schema。

## 2. 实现范围

Day 21 完成：

- 新增租户相关 Domain model；
- 新增 EF Core configuration；
- 新增 expand-only PostgreSQL migration；
- 建立 tenant-owned uniqueness 和 relationship invariant；
- 区分业务 Tenant 与 Azure directory tenant；
- 增加 schema 和 model 负向测试。

Day 21 不做：

- legacy data backfill；
- 可信 TenantContext；
- 租户感知 repository；
- OIDC；
- RBAC；
- 审计；
- PostgreSQL RLS。

## 3. 关键设计

本 Day 使用 expand-only migration，先引入新的租户结构，不直接修改 Day 1-7 遗留数据。

关键约束：

- `Tenant` 属于 `Organization`；
- `ProviderConnection` 属于 `Tenant`；
- `CloudAccount` 同时属于 `Tenant` 和 `ProviderConnection`；
- `Membership` 使用 `(tenant_id, issuer, subject)` 唯一标识 active identity；
- `CloudAccount` 的 `(provider, external_account_id)` 保持全局 active identity；
- composite key 或等价约束必须阻止跨 tenant relationship。

## 4. 验证证据

本地 closeout 记录：

- `scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit`：通过；
- build：0 warning，0 error；
- tests：52 passed；
- `scripts/Test-DatabaseMigration.ps1`：通过；
- PostgreSQL negative constraints 覆盖 cross-tenant reference、Provider mismatch、duplicate membership、global Provider account identity 和 restricted delete。

## 5. Review 结论

实现已合并，但源报告保持 `Validation`。后续 Day 22-24 已继续在该基础上实现 TenantContext、租户感知 repository 和 legacy backfill。

## 6. 遗留风险

- 可信 TenantContext 留给 Day 22；
- 租户感知 repository 留给 Day 23；
- legacy backfill 留给 Day 24；
- OIDC、RBAC、审计和 RLS 留给后续 Day。

## 7. 相关链接

- [ADR-0003](../adr/ADR-0003-organization-tenant-cloud-account-model.md)
- [docs/days/day-21.md](../../days/day-21.md)
