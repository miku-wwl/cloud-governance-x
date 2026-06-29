# Day 26 Microsoft Entra 开发集成评审

日期：2026-06-21
状态：Accepted
决策来源：[ADR-0004](../adr/ADR-0004-entra-and-development-identity.md)

## 1. 目标

使用真实 Microsoft Entra ID token 调用本地 API，同时保持 API caller identity 与 Azure Provider runtime identity 分离。

## 2. 实现范围

Day 26 完成：

- 可重复的 development App Registration 初始化脚本；
- API App Registration 暴露 delegated `access_as_user`；
- public local development client 使用 Device Code Flow；
- cleanup 脚本默认 dry-run，并要求精确 Tenant 确认；
- 真实 token E2E 脚本；
- OIDC metadata 和 JWKS 证据；
- signing-key rollover regression；
- metadata failure regression。

Day 26 不做：

- 将委托 scope 强制为授权策略；
- 业务端点全面保护；
- Azure Provider runtime identity 替换；
- staging/production identity 方案；
- 审计。

## 3. 关键设计

开发身份分为：

- API caller identity：使用 Entra public client + Device Code Flow 获取 delegated access token；
- Azure Provider runtime identity：仍使用本地 `DefaultAzureCredential` 和 Azure CLI 开发身份。

App Registration 是 Entra directory object，不属于 Resource Group。清理必须通过显式脚本完成，且默认 dry-run。

## 4. 验证证据

tracked report 记录真实 token 证据：

- tenant-specific issuer；
- API audience；
- 委托 scope；
- signed JWT `kid` 存在于 Microsoft Entra JWKS；
- 两个 App Registration 均无 credential；
- Active Membership 存在前返回 403；
- token `iss/sub` 成功映射 Membership；
- 本地 API 接受真实 token 并建立 TenantContext；
- tenant-aware cost endpoint 返回 200；
- 临时 PostgreSQL 数据库清理完成。

当前文档迁移期间的本地快照：

- `dotnet test FinOpsPlatform.slnx --no-restore`：84 passed，1 skipped。

## 5. Review 结论

Accepted。Day 1-Day 26 工程交付审计未发现当前范围内的阻断性 Critical 缺陷；Day 26 的真实 Microsoft Entra 开发身份集成可以作为 Day 27 RBAC 的前置输入。委托 scope 仍未变成后端授权策略，该事项属于 Day 27-Day 28 的后续施工范围，不作为 Day 26 阻断缺陷。

## 6. 遗留风险

业务端点在 Day 28 前仍不能视为完整受保护；Day 27 需要实现 RBAC，Day 28 需要把 policy 应用到端点。Azure Provider runtime identity 仍使用本地开发 credential chain。

## 7. 相关链接

- [docs/days/day-26.md](../../days/day-26.md)
- [ADR-0004](../adr/ADR-0004-entra-and-development-identity.md)
- [scripts/Initialize-DevelopmentEntraIdentity.ps1](../../../scripts/Initialize-DevelopmentEntraIdentity.ps1)
- [scripts/Test-EntraOidcIntegration.ps1](../../../scripts/Test-EntraOidcIntegration.ps1)
- [scripts/Remove-DevelopmentEntraIdentity.ps1](../../../scripts/Remove-DevelopmentEntraIdentity.ps1)
