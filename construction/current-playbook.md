# 当前施工手册

- 当前里程碑：M4 - RBAC、端点保护与审计
- 当前位置：Phase 2，Day 26 Accepted 之后
- 当前施工单元：Day 27 - 权限与范围 RBAC

本文只描述当前施工单元。工程总规划见
[engineering-plan.md](engineering-plan.md)。

## 1. 开工规则

开始实现新施工单元前，必须先完成：

1. 阅读 [outline.md](../outline.md)；
2. 阅读 [docs/current-state.md](../docs/current-state.md)；
3. 阅读 [docs/roadmap.md](../docs/roadmap.md)；
4. 阅读 [engineering-plan.md](engineering-plan.md)；
5. 阅读对应 Day 胶囊：[docs/days/day-27.md](../docs/days/day-27.md)；
6. 检查风险登记和生产差距登记；
7. 确认 working tree 状态，不覆盖无关用户修改；
8. 只实现当前施工单元，除非 Owner 明确改变范围。

## 2. Day 27 目标

Day 27 要实现权限与范围 RBAC，关闭“已认证但未授权”的核心缺口。

预期范围：

- 定义权限词汇表；
- 定义 tenant、CloudAccount、平台范围；
- 设计当前 API/Worker 所需的 role、grant 或等价授权模型；
- 将 已认证的 `iss/sub`、Membership 和 可信 TenantContext 接入授权评估；
- 覆盖 administrator、operator、analyst、auditor、owner 等角色或主体的 allow/deny matrix；
- 更新 [docs/current-state.md](../docs/current-state.md)、[docs/days/day-27.md](../docs/days/day-27.md)
  以及相关风险/生产差距文档。

## 3. Day 27 非目标

Day 27 不应宣称：

- 所有现有端点都已经受保护；
- 全局 401/403 Problem Details 已完成；
- 追加式审计存储已完成；
- PostgreSQL RLS 已实现；
- React 或浏览器授权体验已存在。

这些内容分别留给 Day 28、Day 29 或后续阶段。

## 4. 设计边界

Day 27 的授权模型必须满足：

- 不信任客户端传入的任意租户或范围；
- 授权输入来自认证主体、Membership、TenantContext 和受控目标范围；
- 租户范围与 CloudAccount 范围必须显式区分；
- 平台范围必须是独立高权限路径，不能由普通租户权限隐式获得；
- deny path 必须和 allow path 一样有测试；
- 授权服务不能破坏 Domain、Application、Infrastructure、API、Worker 的依赖方向。

## 5. 验证要求

最低验证：

```powershell
./scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit -SkipDependencyOutdated
```

如果新增数据库 schema 或 seed 数据，还必须执行：

```powershell
./scripts/Test-DatabaseMigration.ps1
```

Day 27 必须补充聚焦测试：

- 权限到 role/grant 的正向映射；
- 租户范围 allow/deny；
- CloudAccount 范围 allow/deny；
- 平台范围 allow/deny；
- 缺失 TenantContext、未知 Membership、inactive Membership、跨 tenant target 的拒绝路径；
- API 或 Worker 最小 harness 中的授权集成路径。

## 6. 出关规则

Day 27 默认保持 `Validation`，直到 Owner 接受：

- RBAC 模型；
- 范围评估边界；
- allow/deny matrix；
- 负向授权路径；
- 文档和风险更新。

Day 27 不关闭 Phase 2。Phase 2 必须等 Day 30 安全门禁后再判断是否出关。
