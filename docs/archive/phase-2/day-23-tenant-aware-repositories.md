# Day 23 Tenant-aware Repository 评审

日期：2026-06-20
状态：Accepted

## 1. 目标

让资源、成本和 ETL 持久化路径依赖 可信 TenantContext 和租户所属 CloudAccount，降低跨租户读写和 IDOR 风险。

## 2. 实现范围

Day 23 完成：

- resources、costs、ETL runs 上新增 nullable expand-only `tenant_id`；
- repository read/write 要求 TenantContext；
- tenant-aware unique index；
- resource 和 cost row 的 CloudAccount composite foreign key；
- tenant-scoped ETL ID update；
- E2E Tenant、ProviderConnection、CloudAccount fixture；
- PostgreSQL Tenant A/B repository integration。

Day 23 不做：

- historical NULL-row backfill；
- OIDC；
- RBAC；
- 更细 account 范围；
- PostgreSQL RLS。

## 3. 关键设计

schema 保持 expand-only，以兼容遗留 NULL 行；新的 repository 路径必须在 TenantContext 下执行。

CloudAccount composite foreign key 负责保证 resource/cost row 属于当前 Tenant 的 account。tenant-aware unique index 避免不同 Tenant 的业务数据互相冲突。

## 4. 验证证据

tracked report 记录 `Status: Accepted`。本地 closeout 记录：

- static verification：通过；
- build：通过；
- tests：69 passed；
- migration Up/Down/reapply：通过；
- Tenant A/B integration：通过；
- cross-tenant CloudAccount rejection：通过；
- cleanup：通过。

## 5. Review 结论

Accepted。核心 repository 已经 tenant-aware。

## 6. 遗留风险

历史 NULL-row backfill 留给 Day 24。OIDC、RBAC、更细 account 范围和 RLS 留给后续工作。

## 7. 相关链接

- [docs/days/day-23.md](../../days/day-23.md)
- [ADR-0003](../adr/ADR-0003-organization-tenant-cloud-account-model.md)
