# Day 20 - 租户模型评审

## 1. 目标
在实现 schema 或 runtime 行为前定义业务租户词汇和安全模型，解决 Azure directory tenant 与业务 Tenant 混淆、隔离边界不清的生产风险。

## 2. 前置条件
依赖 Phase 1 出关和 Phase 2 身份/租户/RBAC/审计 规划。关联 [ADR-0003](../archive/adr/ADR-0003-organization-tenant-cloud-account-model.md)。

## 3. 施工范围
允许定义 Organization、Tenant、Membership、ProviderConnection、CloudAccount、身份来源路径、范围层级、shared-schema 隔离要求和 Day 21-30 negative-test map。不允许实现 Domain entities、EF schema、TenantContext、OIDC、RBAC 或 审计存储。

## 4. 设计决策
业务 Tenant 与 Azure directory tenant 明确分离；租户/账号范围层级先形成 ADR，再进入 schema 和 runtime 实现。

## 5. 实现摘要
新增 Phase 2 tenancy model 文档、ADR-0003 决策、身份来源分类、范围层级、shared schema 隔离要求和后续 Day 实现地图。

## 6. 验证证据
closeout report 记录 `Status: Complete`，44 tests passed，static verification 在修正 whitespace 后通过。证据包括本文、[ADR-0003](../archive/adr/ADR-0003-organization-tenant-cloud-account-model.md) 和忽略文件 `tmp/day20-closeout-report.md`。

## 7. Review 结论
Accepted。ADR-0003 已接受，Day 21 可以开始实现 tenancy foundation。

## 8. 遗留风险
Domain entities、EF configuration、migration、TenantContext、OIDC、RBAC、审计存储 和 RLS 均留给后续 Day。

## 9. 相关链接
- Commit: `5f32751` - `docs: define phase 2 tenancy model`
- PR: `#8` - `docs/day20-tenancy-decision`
- [docs/archive/adr/ADR-0003-organization-tenant-cloud-account-model.md](../archive/adr/ADR-0003-organization-tenant-cloud-account-model.md)
