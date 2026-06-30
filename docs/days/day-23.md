# Day 23 - 租户感知 Repository

## 1. 目标
让资源、成本和 ETL 持久化依赖可信 TenantContext 和租户所属 CloudAccount 关系，解决核心数据读写可能跨租户泄露的生产风险。

## 2. 前置条件
依赖 Day 21 tenancy schema 和 Day 22 可信 TenantContext。

## 3. 施工范围
允许在 resources、costs、etl runs 上新增 nullable expand-only `tenant_id`，实现 TenantContext-required repository reads/writes、tenant-aware unique indexes、CloudAccount composite foreign keys、tenant-scoped ETL ID updates、E2E Tenant/ProviderConnection/CloudAccount fixtures 和 Tenant A/B integration。不允许回填历史 NULL rows、实现 OIDC、RBAC、更细 account 范围或 RLS。

## 4. 设计决策
先用 nullable expand-only 列兼容历史数据，同时让新 repository 路径必须在 TenantContext 下运行；通过 composite FK 约束资源/成本行必须属于当前租户的 CloudAccount。

## 5. 实现摘要
新增 租户感知 repository 行为、tenant-aware unique indexes、CloudAccount composite foreign keys、ETL 租户范围 更新、E2E fixture 和 PostgreSQL Tenant A/B 集成测试。

## 6. 验证证据
tracked report 记录 `Status: Accepted`。本地 closeout 记录 static verification、build、69 tests、migration Up/Down/reapply、Tenant A/B integration、cross-tenant CloudAccount rejection 和 cleanup 均通过。证据包括本文和 `tmp/day23-closeout-report.md`。

## 7. Review 结论
Accepted。核心 repository 已租户感知。

## 8. 遗留风险
历史 NULL-row backfill 留给 Day 24；OIDC、RBAC、更细 account 范围和 RLS 留给后续工作。

## 9. 相关链接
- Commit: `f1840f8` - `feat: make core repositories tenant aware`
