# Day 29 - 追加式审计

## 1. 目标
建立授权决策的追加式审计模型，解决 Day 28 之后“端点已执行授权但授权成功/失败不可追责”的工程风险。

## 2. 前置条件
依赖 Day 27 RBAC 权限与范围模型、Day 28 端点保护与 401/403 行为，以及 RISK-0001、GAP-001、GAP-015。

## 3. 施工范围
允许新增授权审计领域实体、Application 审计接口、Infrastructure EF 持久化、数据库 migration、端点授权 filter 的审计写入、端到端测试和相关状态/风险文档。不允许实现通用业务审计中心、审计查询 API、前端审计页面、PostgreSQL RLS、rate limit、完整 Problem Details 或 Day 30 Phase 2 安全门禁。

## 4. 设计决策
Day 29 把审计点放在 API endpoint authorization filter，而不是放在业务 handler。这样可以覆盖匿名拒绝、缺 TenantContext 拒绝、RBAC 拒绝和 RBAC 允许四类授权决策。

审计事件采用追加式表 `authorization_audit_events`，记录：

| 字段类别 | 内容 |
| --- | --- |
| actor | `actor_issuer`、`actor_subject` |
| tenant / scope | `tenant_id`、`cloud_account_id`、`scope_kind` |
| action | `permission`、`http_method`、`path` |
| result | `is_allowed`、`status_code`、`reason` |
| trace | `correlation_id`、`occurred_at` |
| 高权限标记 | `is_high_privilege` |

审计字段不记录 token、Authorization header、secret、provider raw payload 或完整请求体。Day 29 的“不可普通修改”通过模型边界和无更新路径约束，不在本 Day 实现数据库级 append-only trigger。

## 5. 实现摘要
已完成 Day 29 追加式授权审计：

- 新增 `AuthorizationAuditEvent` 领域实体；
- 新增 `IFinOpsAuthorizationAuditSink` 和默认 no-op sink；
- 新增 Infrastructure `AuthorizationAuditSink`，通过 `FinOpsDbContext` 追加写入审计事件；
- 新增 EF 配置和 `AddAuthorizationAuditEvents` migration；
- `FinOpsEndpointAuthorizationExtensions` 在 401、403 和允许路径追加审计；
- 高权限 permission 记录 `is_high_privilege`；
- API TestServer 端到端测试断言授权成功、缺 TenantContext、RBAC deny 都会产生审计 entry；
- DI、模型配置和审计实体测试已覆盖。

## 6. 验证证据
已完成验证：

- `dotnet build FinOpsPlatform.slnx`：成功，0 warning，0 error；
- `dotnet test FinOpsPlatform.slnx --no-restore`：111 passed，1 skipped；
- `./scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit -SkipDependencyOutdated`：通过；
- `./scripts/Test-DatabaseMigration.ps1`：通过；
- TestServer 集成测试覆盖：
  - Operator 调用 `POST /api/admin/sync/azure/costs` 成功并产生 allowed audit；
  - 已认证但缺 TenantContext 返回 403 并产生 denied audit；
  - Auditor 调用 `POST /api/admin/sync/azure/resources` 返回 403 并产生 denied audit；
- EF migration 已生成：
  - `20260630014542_AddAuthorizationAuditEvents`；
  - 新表 `authorization_audit_events`；
  - tenant/time 和 high-privilege/time 索引；
- 数据库 migration gate 已验证空库 migration、幂等 rerun、并发拒绝、不同库隔离、Down/reapply、legacy backfill 和 Worker 失败路径。

## 7. Review 结论
Accepted。Day 29 的代码施工和端到端授权审计验证已完成；Owner 已批准进入 Day 30。Day 29 不关闭 M4，也不关闭历史 Phase 2。

## 8. 遗留风险
Day 29 只完成授权决策追加式审计，不完成完整审计产品能力。

明确留给后续 Day：

- Day 30：执行 tenant escape、IDOR、RBAC、端点保护和审计 gate，判断 Phase 2 是否出关；
- 后续 API hardening：审计查询 API、Problem Details、rate limit、分页、稳定错误码；
- 后续数据与安全阶段：数据库级 append-only trigger、审计 retention、审计导出、前端审计页面和 RLS。

## 9. 相关链接
- [construction/current-playbook.md](../../construction/current-playbook.md)
- [docs/current-state.md](../current-state.md)
- [docs/roadmap.md](../roadmap.md)
- [day-28.md](day-28.md)
