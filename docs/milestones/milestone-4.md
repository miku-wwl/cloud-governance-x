# M4 - RBAC、端点保护与审计完工报告

## 1. 阶段范围

M4 覆盖 Day 27-30，历史上属于 Phase 2 的后半段。目标是完成权限与范围 RBAC、业务端点保护、授权错误基础契约、追加式授权审计和安全门禁。

对应 Day：

- Day 27 - 权限与范围 RBAC；
- Day 28 - 端点保护与授权错误契约；
- Day 29 - 追加式授权审计；
- Day 30 - M4 安全门禁。

## 2. 阶段目标

M4 要解决的问题是：认证和 TenantContext 已存在后，业务端点必须真正执行授权，授权结果必须可追踪，tenant escape、IDOR、wrong role 和缺失上下文不能绕过后端控制。

本阶段必须回答：

- role / permission / scope 矩阵是否可验证；
- 现有业务端点是否都有 permission 或 explicit anonymous 理由；
- 匿名、缺 TenantContext 和 wrong role 是否 fail closed；
- 授权 allow / deny 是否有追加式审计；
- M4 是否具备进入 M5 的安全门禁证据。

## 3. 完工结论

结论：**ACCEPT**

签发信息：

- 审查日期：2026-06-30；
- 正式签发日期：2026-07-01；
- Owner 决策：`M4 - RBAC、端点保护与审计：ACCEPT`；
- 后续授权：正式进入 `M5 - 生产数据模型 / Day31`。

M4 acceptance 不代表生产可用，不代表 RLS、完整审计产品、rate limit、Problem Details、staging、备份或生产运行身份已经完成。

## 4. 关键交付物

- `FinOpsPermission`、`FinOpsAuthorizationScope` 和 RBAC 评估服务；
- Membership role 到 permission 的 allow / deny matrix；
- 现有业务端点 `RequireFinOpsPermission` filter；
- 业务端点 anonymous / authenticated / forbidden 行为；
- `AuthorizationAuditEvent`、audit sink 和 `authorization_audit_events` migration；
- 授权 allow / deny 审计；
- tenant escape、query string tenant spoofing、RBAC deny handler bypass 和 endpoint protection gate 测试；
- Milestone 文档体系从 `docs/phase/` 迁移到 `docs/milestones/`。

## 5. 验证证据

永久证据入口：

- [day-27.md](../days/day-27.md)
- [day-28.md](../days/day-28.md)
- [day-29.md](../days/day-29.md)
- [day-30.md](../days/day-30.md)

Day30 本地 gate 证据：

- `dotnet build FinOpsPlatform.slnx`：0 warning，0 error；
- `dotnet test FinOpsPlatform.slnx --no-restore`：113 passed，1 skipped；
- `./scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit -SkipDependencyOutdated`：通过；
- 未拥有 tenant header 被 403 拒绝；
- query string tenant 不能建立 TenantContext；
- wrong role 返回 403；
- RBAC deny 时 high-privilege handler call count 为 0；
- allow / deny 授权路径有 audit entry；
- 所有 `/api/` 业务端点带 authorization metadata。

## 6. Review 结论

Day30 QA、项目经理和软件总工复核均未发现 M4 范围内阻断项。Owner 已批准 Day30，并于 2026-07-01 正式签发：

`M4 - RBAC、端点保护与审计：ACCEPT。授权进入 M5 - 生产数据模型 / Day31。`

接受边界：

- Accept M4；
- 不 Accept 生产可用；
- 不 Accept RLS；
- 不 Accept 完整审计产品；
- 不 Accept rate limit / Problem Details / API hardening；
- 不 Accept M5 数据模型已完成。

## 7. 带入后续阶段的风险

- M5 必须建立数据分层、lineage、resource lifecycle、成本语义和数据质量；
- 后续 API hardening 必须完成 Problem Details、错误码、pagination、rate limit 和 OpenAPI contract；
- 后续安全加固必须评估 PostgreSQL RLS、数据库级 append-only 审计约束、审计 retention 和审计查询；
- 后续生产化必须处理 staging、workload identity、备份恢复、SLO、observability 和 CI/CD promotion。

## 8. 后续影响

M4 完成后，项目可以从身份、租户、RBAC 和审计基础建设转入 M5 - 生产数据模型。Day31 应从 Raw / Normalized / Derived / Operational 数据分层 ADR 开始。
