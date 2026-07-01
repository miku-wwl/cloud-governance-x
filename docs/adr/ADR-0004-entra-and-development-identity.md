# ADR-0004: Microsoft Entra 与开发身份

日期：2026-06-21
状态：Accepted
Owner：Security Owner

## 背景

Day 25 已加入 provider-independent JWT Bearer validation，但 API 仍缺少真实 identity provider
和可重复的本地开发 token flow。当前 Azure CLI user identity 适合 Azure SDK 开发，但 Azure Resource
Manager token 不是 FinOps API 的 access token。

App Registration 是 Microsoft Entra directory object。它们不是 Azure Resource Manager resource，
不属于 subscription 或 Resource Group，也不会在 Resource Group 删除时被自动删除。

## 决策

开发环境使用两个 single-tenant Microsoft Entra App Registration：

1. `cloud-governance-x-api-dev`
   - 表示 FinOps API；
   - 使用 Microsoft identity platform v2 access token；
   - 暴露 `api://<api-client-id>/access_as_user`；
   - 没有 redirect URI、credential、certificate 或 client secret。
2. `cloud-governance-x-local-dev-client`
   - 表示 operator 的本地命令行会话；
   - 是 public client；
   - 使用 OAuth 2.0 Device Code flow；
   - 只获得 delegated `access_as_user` permission；
   - 没有 client secret。

API 验证 tenant-specific v2 issuer、API client ID audience、signature 和 lifetime。
Tenant authority 仍独立通过 token 的 `iss`、`sub` 与 Active Membership 匹配建立。

开发 App Registration 由 review 过的脚本创建和删除。它们的 object ID 和 application ID 可以作为非 secret
证据保存。access token、refresh token 和 device code 绝不能提交，也不能写入普通日志。

## 后果

- 本地开发证明了后续部署环境同样会使用的 external-token trust boundary；
- client ID 泄漏不会认证应用，因为 client ID 是公开标识；
- Device Code flow 需要交互用户，不能静默变成生产 service identity；
- App Registration 会在 Resource Group 删除后继续存在，因此需要显式 Entra cleanup operation；
- Day 27 可以在不改变 token acquisition boundary 的前提下评估 delegated scope 和 application role；
- staging 和 production 必须使用独立 registration 和 workload identity 决策，不能复用 development public client。

## 被拒绝的替代方案

### 复用 Azure CLI application 作为产品 client

Rejected。Azure CLI 是 Microsoft-owned first-party client，不能给本项目明确、可 review 的 client registration 或 permission lifecycle。

### 为本地开发使用 client secret

Rejected。public developer client 无法保守 secret；引入 secret 会制造不必要的存储和轮换风险。

### 把 Entra object 放入现有 AzureRM Terraform root

Day 26 拒绝。现有 Terraform root 管理 subscription-scoped Azure resource。Entra directory object
有不同 ownership、permission 和 lifecycle。未来在 remote state 和 environment ownership 设计完成后，可以用专用
`azuread` root 替代当前 Graph 脚本。

### API 和本地 client 使用同一个 registration

Rejected。resource-server audience/scope ownership 和 client token acquisition 是不同职责，需要独立生命周期和 review。
