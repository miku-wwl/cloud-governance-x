# Day 26 - Microsoft Entra 开发集成

## 1. 目标
使用真实 Microsoft Entra ID token 调用本地 API，同时保持 API caller identity 与 Azure Provider runtime identity 分离，解决认证链路只停留在内存 token 测试的风险。

## 2. 前置条件
依赖 Day 25 OIDC Bearer authentication、Day 22 TenantContext 和 [ADR-0004](../adr/ADR-0004-entra-and-development-identity.md)。

## 3. 施工范围
允许新增可重复开发 App Registration 初始化脚本、API 委托 `access_as_user` scope、本地 public client Device Code flow、dry-run 默认 cleanup、真实 token E2E、OIDC metadata/JWKS 证据、signing-key rollover 和 metadata failure regression。不允许把委托 scope 当作已执行授权策略，也不允许改变 Azure Provider runtime identity。

## 4. 设计决策
开发身份分为 API caller identity 和 Azure Provider identity；Entra App Registration 是目录对象，不随 Resource Group 自动清理；cleanup 必须 dry-run 默认并要求确认 Tenant。

## 5. 实现摘要
新增 [Initialize-DevelopmentEntraIdentity.ps1](../../scripts/Initialize-DevelopmentEntraIdentity.ps1)、[Test-EntraOidcIntegration.ps1](../../scripts/Test-EntraOidcIntegration.ps1)、[Remove-DevelopmentEntraIdentity.ps1](../../scripts/Remove-DevelopmentEntraIdentity.ps1)、真实 token E2E、JWKS/metadata 验证和回归测试。

## 6. 验证证据
tracked report 记录真实 token 证据：tenant-specific issuer、API audience、委托 scope、signed JWT `kid` 存在于 Microsoft Entra JWKS、两个 app registration 均无 credentials、Active Membership 前为 403、`iss/sub` membership mapping 后本地 API 接受真实 token 并建立 TenantContext、tenant-aware cost endpoint 返回 200、临时 PostgreSQL 数据库清理完成。当前文档迁移快照还记录 `dotnet test FinOpsPlatform.slnx --no-restore`: 84 passed, 1 skipped。

## 7. Review 结论
Accepted。Day 1-Day 26 工程交付审计未发现当前范围内的阻断性 Critical 缺陷，Day 26 的真实 Microsoft Entra 开发身份集成可以作为 Day 27 RBAC 的前置输入。

## 8. 遗留风险
Day 26 未把委托 scope 强制为授权策略；业务端点在 Day 28 前仍不能视为完整受保护；Azure Provider runtime identity 仍使用本地开发 credential chain。

## 9. 相关链接
- Commit: `2189cde` - `feat: integrate Microsoft Entra development identity`
- PR: `#12` - `feat/day26-entra-development-integration`
- [docs/adr/ADR-0004-entra-and-development-identity.md](../adr/ADR-0004-entra-and-development-identity.md)
