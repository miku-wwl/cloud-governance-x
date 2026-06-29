# Day 22 可信 TenantContext 评审

日期：2026-06-20
状态：Accepted

## 1. 目标

建立 HTTP 请求和 Worker job 的可信 tenant execution context，避免系统信任任意 客户端提供的 tenant input。

## 2. 实现范围

Day 22 完成：

- scoped TenantContext；
- read interface 和 initialization interface 分离；
- HTTP 请求根据已认证 `iss/sub` 和 Active Membership 选择 Tenant；
- Worker job request 显式携带 tenant ID；
- Worker 拒绝缺失、未知或 inactive Tenant；
- E2E 脚本补充 explicit development tenant。

Day 22 不做：

- repository tenant filtering；
- legacy data backfill；
- OIDC token validation；
- RBAC；
- 审计。

## 3. 关键设计

TenantContext 必须 fail closed。客户端可以请求选择 Tenant，但 authority 必须来自服务端验证过的 Membership。

Worker 没有 ambient HTTP identity，因此 job 必须显式携带服务端创建的 租户范围。缺失或不一致时，Worker 必须在解析 repository 或 provider 前失败。

## 4. 验证证据

归档报告记录 final decision 为 `ACCEPT`。本地 closeout 记录：

- static verification：通过；
- build：通过；
- tests：62 passed；
- Terraform validation：通过；
- database/Worker process verification：通过。

## 5. Review 结论

Accepted。可信 TenantContext 建立完成，可作为 Day 23 repository 租户过滤的前置。

## 6. 遗留风险

repository tenant filtering、legacy-data backfill、OIDC validation、RBAC 和审计 仍未完成。

## 7. 相关链接

- [docs/days/day-22.md](../../days/day-22.md)
- [ADR-0003](../adr/ADR-0003-organization-tenant-cloud-account-model.md)
