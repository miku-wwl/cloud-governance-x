# Day 25 OIDC Bearer 认证评审

日期：2026-06-21
状态：Validation

## 1. 目标

为 API 建立 provider-independent JWT Bearer validation 管道，使用标准 OIDC/JWT 机制验证外部 token，而不是自建用户名密码或 token 签发系统。

## 2. 实现范围

Day 25 完成：

- `Authentication:Oidc` 配置；
- 可选 JWT Bearer authentication；
- issuer、audience、signature、expiration 和 lifetime validation；
- 本地默认关闭；
- 保留原始 OIDC `iss` 和 `sub` claims；
- root、health、live endpoints 显式 anonymous；
- 使用 ephemeral RSA key 和 static OIDC metadata 的 in-memory token tests。

Day 25 不做：

- 业务端点授权策略；
- RBAC；
- 完整端点保护；
- 稳定 401/403 契约；
- 审计。

## 3. 关键设计

API 只验证 identity provider 签发的 token，不签发 token，也不实现密码系统。认证是否启用由配置控制，生产或共享环境必须显式配置 Authority、Audience 和 HTTPS metadata。

`iss/sub` 是后续 Tenant Membership 映射的稳定输入；email 或显示名称不能作为授权 key。

## 4. 验证证据

tracked report 记录：

- local build：0 warning，0 error；
- tests：82 passed；
- 1 个 PostgreSQL integration test 因未启用 opt-in database environment 被 skipped；
- token 负向测试覆盖 issuer、audience、signature、expiration 和 metadata 行为。

## 5. Review 结论

Validation。实现已合并，但端点授权仍留给 Day 27-28。

## 6. 遗留风险

业务端点尚未绑定授权策略。RBAC、完整端点保护、稳定 401/403 响应和审计仍未完成。

## 7. 相关链接

- [docs/days/day-25.md](../../days/day-25.md)
- [src/FinOps.Api/Authentication](../../../src/FinOps.Api/Authentication)
- [src/FinOps.Tests/Api/OidcBearerAuthenticationTests.cs](../../../src/FinOps.Tests/Api/OidcBearerAuthenticationTests.cs)
