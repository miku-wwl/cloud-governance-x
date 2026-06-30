# Day 25 - OIDC Bearer 认证

## 1. 目标
接入 provider-independent ASP.NET Core JWT Bearer 验证，解决 API 缺少标准认证管道、TenantContext 无法绑定真实身份的风险。

## 2. 前置条件
依赖 Day 20-24 tenancy foundation，特别是 Membership 与 TenantContext。关联 Phase 2 identity 工作。

## 3. 施工范围
允许新增 `Authentication:Oidc` 配置、可选 JWT Bearer authentication、issuer/audience/signature/expiration/lifetime validation、默认关闭的本地配置、保留原始 OIDC `iss`/`sub` claims、匿名 root/health endpoints 和内存 token tests。不允许给业务端点绑定授权策略或实现 RBAC/审计。

## 4. 设计决策
使用标准 JWT Bearer 验证，不自建 identity system；认证能力默认关闭，本地和测试可显式启用；`iss`/`sub` 原始 claim 保留给 Tenant Membership 校验。

## 5. 实现摘要
新增 OIDC authentication options、JWT Bearer pipeline、claim preservation、anonymous endpoint rules、ephemeral RSA key token tests 和 static OIDC metadata tests。

## 6. 验证证据
tracked report 记录本地 build 0 warning/0 error，82 tests passed，1 个 PostgreSQL integration test 因 opt-in database 环境未启用被 skipped。证据包括本文、[src/FinOps.Api/Authentication](../../src/FinOps.Api/Authentication) 和 [OidcBearerAuthenticationTests.cs](../../src/FinOps.Tests/Api/OidcBearerAuthenticationTests.cs)。

## 7. Review 结论
Validation。实现已合并，源 day report 仍保留 Validation 状态。

## 8. 遗留风险
业务端点尚未绑定授权策略；RBAC、完整端点保护、稳定 401/403 契约和审计留给 Day 27-29。

## 9. 相关链接
- Commit: `c405220` - `feat: add OIDC bearer authentication`
- PR: `#11` - `feat/day25-oidc-bearer-authentication`
