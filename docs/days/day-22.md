# Day 22 - 可信 TenantContext

## 1. 目标
为 HTTP 请求和 Worker job 建立可信租户执行上下文，解决系统可能信任任意客户端 tenant input 的隔离风险。

## 2. 前置条件
依赖 Day 21 tenancy schema、Membership 模型和 Phase 2 对 trusted tenant context 的要求。

## 3. 施工范围
允许实现 scoped TenantContext、读/初始化接口分离、HTTP 基于 已认证的 `iss`/`sub` 和 Active Membership 选择租户、Worker job 显式 tenant ID、缺失/未知/停用 Tenant 拒绝，以及 E2E 脚本 development tenant 参数。不允许实现 repository tenant filtering、legacy backfill、OIDC validation、RBAC 或审计。

## 4. 设计决策
TenantContext 必须 fail-closed；HTTP tenant selection 不能信任任意请求参数，必须来自认证身份和 active membership。Worker job 必须显式携带 tenant ID，并拒绝无效租户。

## 5. 实现摘要
新增 scoped TenantContext、read/init 接口、HTTP membership selection、Worker tenant validation 和 E2E tenant fixture 更新。

## 6. 验证证据
Day report 记录 final decision `ACCEPT`。本地 closeout 记录 static verification、build、62 tests、Terraform validation、database/Worker process verification 均通过。证据包括本文和 `tmp/day22-closeout-report.md`。

## 7. Review 结论
Accepted。可信 TenantContext 建立完成。

## 8. 遗留风险
Repository tenant filtering、legacy-data backfill、OIDC validation、RBAC 和审计 仍未完成。

## 9. 相关链接
- Commit: `eb9006d` - `feat: establish trusted tenant context`
- PR: `#10` - `feat/day22-trusted-tenant-context`
