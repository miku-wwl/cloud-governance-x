# M3 - 身份与租户基础完工报告

## 1. 阶段范围

M3 覆盖 Day 20-26，历史上属于 Phase 2 的前半段。目标是建立业务 tenant、可信 TenantContext、tenant-aware data access、OIDC Bearer authentication 和 Microsoft Entra 开发身份闭环。

对应 Day：

- Day 20 - 租户模型与 ADR；
- Day 21 - tenant schema；
- Day 22 - 可信 TenantContext；
- Day 23 - tenant-aware repository；
- Day 24 - legacy tenant backfill；
- Day 25 - OIDC Bearer authentication；
- Day 26 - Microsoft Entra 开发集成。

## 2. 阶段目标

M3 要解决的问题是：系统必须从“单租户开发数据底座”升级为有业务租户边界、可信身份来源和可验证数据隔离的工程基础。

本阶段必须回答：

- tenant、organization、provider connection、cloud account 和 membership 的边界是否清楚；
- HTTP 和 Worker 是否都显式建立可信 TenantContext；
- 新数据读写是否 tenant-aware；
- OIDC token 是否能转成服务端验证过的 Membership / TenantContext；
- 真实 Microsoft Entra 开发身份是否能完成本地 API 调用。

## 3. 完工结论

结论：**ACCEPT**

签发信息：

- 审查日期：2026-06-30；
- Owner 决策：Day30 gate 已复核 M3 输入，M3 接受；
- 后续授权：允许 M4 gate 结果一起支撑进入 M5。

M3 acceptance 只接受身份与租户基础，不证明系统已经生产可用。

## 4. 关键交付物

- ADR-0003 Organization / Tenant / CloudAccount model；
- Organization、Tenant、ProviderConnection、CloudAccount、Membership 领域模型；
- tenant-owned uniqueness、composite FK 和 restricted delete；
- scoped TenantContext、HTTP membership selection 和 Worker explicit tenant；
- tenant-aware resource、cost 和 ETL repository；
- development-only legacy tenant backfill；
- OIDC JWT Bearer validation；
- Microsoft Entra public client、delegated token、本地 API E2E。

## 5. 验证证据

永久证据入口：

- [day-20.md](../days/day-20.md)
- [day-21.md](../days/day-21.md)
- [day-22.md](../days/day-22.md)
- [day-23.md](../days/day-23.md)
- [day-24.md](../days/day-24.md)
- [day-25.md](../days/day-25.md)
- [day-26.md](../days/day-26.md)
- [ADR-0003](../archive/adr/ADR-0003-organization-tenant-cloud-account-model.md)

关键验证结论：

- tenant schema migration 和 negative constraints 已验证；
- TenantContext 缺失时 fail closed；
- tenant-aware repository 新读写隔离；
- OIDC issuer、audience、signature、scope 和 JWKS 验证路径成立；
- Day26 真实 Entra token、本地 membership mapping 和 API 调用已验证；
- Day30 gate 复核 M3 与 M4 的组合安全证据。

## 6. Review 结论

Day30 gate 未发现 M3 范围内阻断 M4 出关的 Critical 缺陷。M3 接受。

## 7. 带入后续阶段的风险

- PostgreSQL RLS 尚未实现；
- 环境级 backfill 和生产数据迁移证据仍需后续 Milestone；
- Provider runtime identity 仍未生产化；
- 数据 lineage、resource lifecycle、cost semantics 和 retention 留给 M5。

## 8. 后续影响

M3 为 M4 的 RBAC、端点保护和授权审计提供身份、租户和数据边界基础。M3 接受后，项目可以把后续重点转向 M5 生产数据模型。
