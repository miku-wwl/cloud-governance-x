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
已实现 Day 27 RBAC 核心模型：

- 在 Domain 层新增 `MembershipRole`，覆盖 `Owner`、`Administrator`、`Operator`、`Analyst`、`Auditor`；
- 为 `Membership` 增加 `Role` 和 `Activate` 行为；
- 新增 migration `20260629085216_AddMembershipRoles`，为 `memberships` 增加 `role` 字段，既有记录默认 `Auditor`；
- 在 Application 层新增 `FinOpsPermission`、`FinOpsAuthorizationScope`、`FinOpsAuthorizationDecision` 和 `IFinOpsAuthorizationService`；
- 授权范围明确区分 tenant、CloudAccount 和 platform；
- `HttpTenantContextMiddleware` 通过 active Membership 解析真实 role，并写入可信 TenantContext；
- `TenantMembershipResolver` 支持 active membership role 查询和 active CloudAccount scope 校验；
- 测试覆盖角色权限矩阵、tenant scope、CloudAccount scope、platform scope、缺失 TenantContext、inactive/unknown Membership、跨 tenant target 和 BackgroundJob 不绕过 RBAC；
- 修正 `Test-DatabaseMigration.ps1` 的 migration 定位逻辑，避免 Day27 新 migration 破坏 Day24 backfill rollback 回归。

## 6. 验证证据
已完成验证：

- `dotnet build FinOpsPlatform.slnx`：成功，0 warning，0 error；
- `dotnet test FinOpsPlatform.slnx --no-restore`：101 passed，1 skipped；
- `./scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit -SkipDependencyOutdated`：通过，包含 whitespace、secret scan、JSON/XML/YAML、workflow、PowerShell、Markdown links、restore、vulnerable/deprecated package report、format、build、test、Terraform fmt/validate；
- `./scripts/Test-DatabaseMigration.ps1`：通过，覆盖空库 migration、幂等重跑、并发 migration 拒绝、不同数据库隔离、Day24 backfill Down/reapply、tenant-aware core Down/reapply、backfill 控制、API/Worker migration ownership 和失败路径。

验证过程说明：

- 第一次数据库迁移回归因 Docker Desktop 未运行失败；启动 Docker daemon 后重跑；
- 第二次发现 Day24 脚本按“最新 migration”推导 rollback 的旧假设，被 Day27 新 migration 打破；
- 修复脚本为按 migration 名称和 history index 定位后，最终端到端通过。

## 7. Review 结论
Accepted。Day 27 的代码施工和端到端验证已完成，RBAC 模型、范围评估、allow/deny matrix 和负向路径已有自动化证据；Owner 已批准进入 Day 28。Day 27 不关闭 Phase 2。

## 8. 遗留风险
Day 27 只完成 RBAC 模型和授权评估服务，不宣称所有业务端点已经绑定授权 policy。

明确留给后续 Day：

- Day 28：把现有业务端点绑定授权 policy，稳定 401/403 契约；
- Day 29：追加式审计模型和高权限 action record；
- Day 30：tenant escape、IDOR、RBAC、端点保护和审计 gate；
- 后续阶段：PostgreSQL RLS、前端授权、生产 Provider identity 和生产运行治理。

## 9. 相关链接
- [construction/current-playbook.md](../../construction/current-playbook.md)
- [docs/current-state.md](../current-state.md)
- [docs/roadmap.md](../roadmap.md)
- [day-26.md](day-26.md)
