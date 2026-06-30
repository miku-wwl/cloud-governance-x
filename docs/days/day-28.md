# Day 28 - 端点保护与授权错误契约

## 1. 目标
把 Day 27 的 RBAC 授权模型应用到现有业务端点，解决“授权模型已存在但 API 仍未显式保护”的工程风险。

## 2. 前置条件
依赖 Day 25 OIDC Bearer authentication、Day 26 Microsoft Entra 开发集成、Day 27 权限与范围 RBAC，以及 RISK-0001 / GAP-001。

## 3. 施工范围
允许为现有业务端点建立授权 policy 映射，区分 anonymous health endpoint、query endpoint、admin sync endpoint 和 ETL run endpoint；允许接入 `IFinOpsAuthorizationService`、补充端点级 allow/deny 测试、稳定 401/403 行为并更新 current-state、risk、production gap 文档。不允许实现追加式审计持久化、PostgreSQL RLS、React 授权体验或生产 Provider identity。

## 4. 设计决策
Day 28 只负责端点保护和授权错误契约。没有显式 anonymous 理由的业务端点默认拒绝匿名；端点授权基于已认证主体、可信 TenantContext、Membership role 和 Day 27 的 RBAC 授权服务，不信任客户端直接声明的任意权限或租户。

当前映射：

| 端点 | 权限 |
| --- | --- |
| `GET /api/cloud/azure/subscriptions` | `ResourceRead` |
| `POST /api/admin/sync/azure/resources` | `ResourceSync` |
| `GET /api/costs/daily` | `CostRead` |
| `GET /api/costs/by-service` | `CostRead` |
| `GET /api/costs/by-resource-group` | `CostRead` |
| `POST /api/admin/sync/azure/costs` | `CostSync` |
| `GET /api/admin/etl-runs` | `EtlRunRead` |

`/`、`/health` 和 `/health/live` 保持 explicit anonymous。

## 5. 实现摘要
已实现 Day 28 端点保护：

- 新增 API 层 `RequireFinOpsPermission` endpoint filter；
- 业务端点统一先要求认证，再要求可信 TenantContext，再调用 `IFinOpsAuthorizationService`；
- 无认证返回 401；
- 已认证但无 TenantContext 或无权限返回 403；
- 现有业务端点均绑定 Day 27 permission；
- health endpoint 保持 `AllowAnonymous`；
- route 测试新增 business route authorization metadata 断言；
- route 测试新增匿名拒绝、无 TenantContext 拒绝、无权限 role 拒绝；
- TestServer 集成测试覆盖 E2E identity、TenantContext middleware、UseAuthorization、endpoint filter 和 handler 的完整路径。

## 6. 验证证据
已完成验证：

- `dotnet build FinOpsPlatform.slnx`：成功，0 warning，0 error；
- `dotnet test FinOpsPlatform.slnx --no-restore`：108 passed，1 skipped；
- TestServer 集成测试覆盖：
  - E2E identity + active role + tenant header 可以访问授权端点；
  - 已认证但缺少 TenantContext 返回 403；
  - role 不具备 permission 返回 403；
- route-level 测试覆盖：
  - 所有 `/api/` 业务端点带 authorization metadata；
  - health routes 保持 anonymous；
  - 匿名业务端点返回 401；
  - 既有 response shape 和 invalid binding 行为保持兼容。

## 7. Review 结论
Accepted。Day 28 的代码施工和端点级授权验证已完成，现有业务端点已绑定 RBAC permission；Owner 已批准进入 Day 29。Day 28 不关闭 Phase 2。

## 8. 遗留风险
Day 28 只完成端点保护和基础 401/403 行为，不完成审计持久化。

明确留给后续 Day：

- Day 29：追加式审计模型和高权限 action record；
- Day 30：tenant escape、IDOR、RBAC、端点保护和审计 gate；
- 后续阶段：PostgreSQL RLS、前端授权、生产 Provider identity、rate limit、Problem Details 细化和生产运行治理。

## 9. 相关链接
- [construction/current-playbook.md](../../construction/current-playbook.md)
- [docs/current-state.md](../current-state.md)
- [docs/roadmap.md](../roadmap.md)
- [day-27.md](day-27.md)
