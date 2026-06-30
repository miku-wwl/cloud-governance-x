# Day 21 - 租户领域模型与 Schema

## 1. 目标
按 ADR-0003 实现 expand-only tenancy foundation，解决系统缺少业务 Tenant、CloudAccount 和 Membership 基础模型的风险，同时避免直接回填旧数据造成不可控迁移。

## 2. 前置条件
依赖 Day 20 租户模型评审和 [ADR-0003](../archive/adr/ADR-0003-organization-tenant-cloud-account-model.md)。

## 3. 施工范围
允许新增 Organization、Tenant、ProviderConnection、CloudAccount、Membership Domain models、EF Core configuration、expand-only PostgreSQL migration、tenant-owned uniqueness 和 composite relationship invariants。不允许 backfill legacy rows 或实现 TenantContext、OIDC、RBAC、审计、RLS。

## 4. 设计决策
采用 expand-only schema 先建立租户结构，不立即修改历史 NULL rows；通过复合外键和唯一约束保护 Tenant 与 Provider/CloudAccount 关系。

## 5. 实现摘要
新增租户相关 Domain 模型、EF 配置、数据库 migration、业务 Tenant 与 Azure directory 分离测试，以及跨租户关系负例约束。

## 6. 验证证据
本地 closeout 记录 `scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit` 通过、build 0 warning/0 error、52 tests passed、`scripts/Test-DatabaseMigration.ps1` 通过，并验证跨租户引用、Provider mismatch、重复 membership、全局 Provider account identity 和 restricted delete 等 PostgreSQL negative constraints。证据包括本文、[ADR-0003](../archive/adr/ADR-0003-organization-tenant-cloud-account-model.md) 和 `tmp/day21-closeout-report.md`。

## 7. Review 结论
Validation。实现已合并，但源 day report 仍保留 Validation wording，等待人工 review 口径确认。

## 8. 遗留风险
可信 TenantContext、租户感知 repository、legacy backfill、OIDC、RBAC、审计 和 RLS 留给后续 Day。

## 9. 相关链接
- Commit: `6284c4c` - `feat: add day 21 tenancy foundation`
- PR: `#9` - `feat/day21-tenancy-foundation`
