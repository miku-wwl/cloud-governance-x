# Day 24 - 历史租户回填

## 1. 目标
为 Day 1-7 遗留的 `tenant_id = NULL` 行提供受控开发回填路径，解决新租户约束与旧数据并存时无法进入严格租户模型的风险。

## 2. 前置条件
依赖 Day 21 tenancy schema、Day 22 TenantContext 和 Day 23 租户感知 repository。

## 3. 施工范围
允许新增 development-only backfill operation、dry-run 默认、显式 `-Apply`、Organization/Tenant ID 参数、writer-stop acknowledgement、NOWAIT locks、advisory lock、apply row-count confirmation、Provider normalization/collision checks、ProviderConnection/CloudAccount controlled creation、completion marker 和 post-backfill NULL-write constraints。不允许把它作为生产大数据迁移方案。

## 4. 设计决策
回填必须默认 dry-run，应用时要求显式确认并阻断活动 writer；通过 completion marker 和 NULL-write rejection 防止回填后继续写入无租户数据。

## 5. 实现摘要
新增 Migrator legacy backfill operation、[Invoke-DevelopmentTenantBackfill.ps1](../../scripts/Invoke-DevelopmentTenantBackfill.ps1)、锁定和碰撞检查、受控 Provider/CloudAccount 创建、完成标记和 post-backfill constraints。

## 6. 验证证据
tracked report 记录覆盖 dry-run、apply、second apply、collision failure、active-writer failure、stale count failure、production environment rejection、post-backfill NULL write rejection 和 completion marker 后 Down rejection。证据包括 [day-24-legacy-tenant-backfill.md](../archive/phase-2/day-24-legacy-tenant-backfill.md) 和 [Invoke-DevelopmentTenantBackfill.ps1](../../scripts/Invoke-DevelopmentTenantBackfill.ps1)。

## 7. Review 结论
Validation。实现已完成，源 day report 仍保留 Validation 状态。

## 8. 遗留风险
EF model 仍保持 nullable 兼容；大数据 timing、lock duration、restore rehearsal、OIDC、RBAC、端点授权、RLS 和审计 留给后续 Day。

## 9. 相关链接
- Commit: `0d5f98f` - `feat: add controlled legacy tenant backfill`
- [docs/archive/phase-2/day-24-legacy-tenant-backfill.md](../archive/phase-2/day-24-legacy-tenant-backfill.md)
- [scripts/Invoke-DevelopmentTenantBackfill.ps1](../../scripts/Invoke-DevelopmentTenantBackfill.ps1)