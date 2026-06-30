# Day 30 - M4 安全门禁

## 1. 目标
执行 M4 / 历史 Phase 2 的安全门禁，复核 Day 20-29 建立的身份、租户、RBAC、端点保护和授权审计是否形成闭环，解决“完成多个 Day 但没有集中 gate 结论”的工程风险。

## 2. 前置条件
依赖 Day 20 租户模型、Day 22 可信 TenantContext、Day 23 tenant-aware repository、Day 25 OIDC Bearer、Day 26 Entra 开发身份、Day 27 RBAC、Day 28 端点保护、Day 29 追加式授权审计，以及 RISK-0001、RISK-0002、GAP-001、GAP-002、GAP-015。

## 3. 施工范围
允许补充 tenant escape、IDOR、missing tenant、wrong role、endpoint authorization metadata 和 audit gate 测试；允许更新 Day 胶囊、current-state、roadmap、current-playbook、risk/gap 和 milestone 索引。不允许实现 PostgreSQL RLS、React 前端、完整审计查询 API、rate limit、完整 Problem Details、M5 生产数据模型或生成已接受的 Milestone 完工报告。

## 4. 设计决策
Day 30 是 gate，不是新功能 Day。门禁采用“现有控制 + 聚焦补测 + 文档结论”的方式，而不是重写身份或租户模型。

Gate 判断矩阵：

| Gate 项 | 判定方式 |
| --- | --- |
| tenant escape | 未拥有 tenant header 必须在进入 endpoint 前被拒绝 |
| IDOR / 客户端伪造 tenant | query string tenant 不能建立可信 TenantContext |
| RBAC deny | 错误 role 必须返回 403，且不能执行 handler |
| endpoint protection | 所有 `/api/` 业务端点必须有 authorization metadata |
| audit | allowed / denied 授权路径必须能产生审计记录 |
| repository tenant boundary | tenant-aware repository 缺失 TenantContext 必须 fail closed |

Day 30 不把“授权决策审计”扩大成“业务操作最终结果审计”。Day 29 的审计仍只证明授权 allow/deny，不证明 handler 后续执行成功。

## 5. 实现摘要
已完成 Day 30 gate 补强：

- TestServer 集成测试新增未拥有 tenant header 拒绝路径；
- TestServer 集成测试新增 query string tenant 不建立 authority 路径；
- RBAC deny 测试新增 handler 未执行断言；
- 高权限同步 stub 增加 call count，用于证明 deny 不穿透到业务操作；
- 保留所有业务端点 authorization metadata 测试；
- 保留匿名业务请求 401、缺 TenantContext 403、错误 role 403 和授权成功审计测试；
- 更新 Day30、current-state、roadmap、current-playbook、risk/gap 和 milestone 索引。

## 6. 验证证据
已完成验证：

- `dotnet build FinOpsPlatform.slnx`：成功；
- `dotnet test FinOpsPlatform.slnx --no-restore`：113 passed，1 skipped；
- `./scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit -SkipDependencyOutdated`：通过；
- 新增 TestServer gate 覆盖：
  - 未拥有 tenant header 被 `HttpTenantContextMiddleware` 403 拒绝；
  - query string tenant 不能建立 TenantContext，业务端点返回 403；
  - Auditor 调用 high-privilege resource sync 返回 403；
  - RBAC deny 时 resource sync handler call count 为 0；
  - allow / deny 授权路径有审计 entry；
- 既有 gate 覆盖：
  - 所有 `/api/` 业务端点带 authorization metadata；
  - health endpoint explicit anonymous；
  - repository 缺失 TenantContext fail closed；
  - RBAC 跨 tenant scope deny；
  - BackgroundJob TenantContext 不能绕过 HTTP-user RBAC。

## 7. Review 结论
Accepted。Day 30 的 M4 / 历史 Phase 2 安全门禁施工、本地验证和 Owner review 已完成；Owner 批准 M4 出关并允许进入 M5 / Day31。Day 30 接受不代表生产可用，也不代表 RLS、完整审计产品、rate limit 或 API hardening 已完成。

## 8. 遗留风险
Day 30 gate 未发现当前范围内阻断 M4 acceptance 的 Critical 缺陷，但以下内容仍明确留给后续 Milestone：

- M5：生产数据模型、lineage、资源生命周期、成本语义和数据质量；
- 后续 API hardening：Problem Details、稳定错误码、pagination、rate limit 和 OpenAPI contract；
- 后续安全加固：PostgreSQL RLS、数据库级 append-only 审计约束、审计 retention、审计查询 API、前端权限体验；
- 后续生产化：staging、workload identity、备份恢复、SLO、observability、CI/CD promotion 和生产 Provider identity。

## 9. 相关链接
- [construction/current-playbook.md](../../construction/current-playbook.md)
- [construction/engineering-plan.md](../../construction/engineering-plan.md)
- [docs/current-state.md](../current-state.md)
- [docs/roadmap.md](../roadmap.md)
- [docs/milestones/README.md](../milestones/README.md)
- [day-29.md](day-29.md)
