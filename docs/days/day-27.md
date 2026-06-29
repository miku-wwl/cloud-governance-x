# Day 27 - 权限与范围 RBAC

## 1. 目标
在 Day 20-26 的身份与租户基础上定义并实现权限与范围 RBAC，解决“已认证但未授权”的核心生产风险。

## 2. 前置条件
依赖 Day 20 租户模型、Day 21 tenancy schema、Day 22 TenantContext、Day 23 租户感知 repository、Day 25 OIDC Bearer authentication、Day 26 Microsoft Entra 开发集成，以及当前 Phase 2 的身份/租户/RBAC 规划。

## 3. 施工范围
允许定义权限词汇表、当前 API/Worker 所需角色或授权模型、tenant/CloudAccount/平台范围 evaluation、允许/拒绝矩阵测试、与已认证的 `iss`/`sub` 和可信 TenantContext 的集成，以及 current-state、risk、production gap 文档更新。不允许一次性完成所有端点保护、全局 401/403 Problem Details、追加式审计持久化、PostgreSQL RLS 或 React 前端授权。

## 4. 设计决策
RBAC 必须以可信 TenantContext 和认证身份为输入，区分 tenant、CloudAccount 和平台范围。Day 27 的重点是权限模型和范围评估，不把 Day 28 端点保护或 Day 29 审计存储偷偷合并进来。

## 5. 实现摘要
Planned。尚未开始代码实现；预期会涉及授权模型、策略/服务、测试矩阵、必要 migration 或 seed 数据，以及文档状态更新。

## 6. 验证证据
Planned。验收至少需要本地静态门禁、授权 allow/deny 矩阵测试、负向授权路径验证，以及与真实/测试认证身份和 TenantContext 的集成证据。

## 7. Review 结论
Validation。Day 27 应保持 Validation，直到负向授权路径被 review 并接受。它本身不关闭 Phase 2。

## 8. 遗留风险
完整端点保护、稳定 401/403 契约、追加式审计、PostgreSQL RLS、前端授权和生产身份治理仍留给后续 Day。

## 9. 相关链接
- [construction/current-playbook.md](../../construction/current-playbook.md)
- [docs/current-state.md](../current-state.md)
- [docs/roadmap.md](../roadmap.md)
- [day-26.md](day-26.md)
