# 当前施工手册

- 当前里程碑：M4 - RBAC、端点保护与审计
- 当前位置：Phase 2，Day 27 Accepted 之后
- 当前施工单元：Day 28 - 端点保护与授权错误契约

本文只描述当前施工单元。工程总规划见
[engineering-plan.md](engineering-plan.md)。

## 1. 开工规则

开始实现新施工单元前，必须先完成：

1. 阅读 [outline.md](../outline.md)；
2. 阅读 [docs/current-state.md](../docs/current-state.md)；
3. 阅读 [docs/roadmap.md](../docs/roadmap.md)；
4. 阅读 [engineering-plan.md](engineering-plan.md)；
5. 确认当前施工单元：Day 28 - 端点保护与授权错误契约；
6. 检查风险登记和生产差距登记；
7. 确认 working tree 状态，不覆盖无关用户修改；
8. 只实现当前施工单元，除非 Owner 明确改变范围。

## 2. Day 28 目标

Day 28 要把 Day 27 RBAC 授权模型应用到现有业务端点，关闭“模型存在但端点未保护”的核心缺口。

预期范围：

- 为现有业务端点建立授权 policy 映射；
- 区分 anonymous health、query、admin sync、ETL run 查询等端点意图；
- 将 `IFinOpsAuthorizationService` 接入 API 最小授权路径；
- 稳定无 token、无 TenantContext、无权限、跨 tenant target 的 401/403 行为；
- 覆盖端点级正向和负向测试；
- 更新 [docs/current-state.md](../docs/current-state.md)、Day 28 胶囊
  以及相关风险/生产差距文档。Day 28 胶囊应在 Day 28 正式开工时创建。

## 3. Day 28 非目标

Day 28 不应宣称：

- 追加式审计存储已完成；
- PostgreSQL RLS 已实现；
- React 或浏览器授权体验已存在。

这些内容分别留给 Day 29 或后续阶段。

## 4. 设计边界

Day 28 的端点保护必须满足：

- 不信任客户端传入的任意租户或范围；
- 授权输入来自认证主体、Membership、TenantContext 和受控目标范围；
- 没有显式 anonymous 理由的业务端点默认拒绝匿名；
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

Day 28 必须补充聚焦测试：

- 匿名访问业务端点被拒绝；
- health/live 等明确 anonymous 端点仍可访问；
- 无 TenantContext、未知 Membership、inactive Membership、跨 tenant target 被拒绝；
- 不同 role 调用 query/admin/sync/ETL endpoint 的 allow/deny 行为；
- 401/403 行为稳定且不泄漏内部异常。

## 6. 出关规则

Day 28 默认保持 `Validation`，直到 Owner 接受：

- 端点授权 policy 清单；
- anonymous endpoint 白名单；
- 401/403 行为；
- 端点级负向授权路径；
- 文档和风险更新。

Day 28 不关闭 Phase 2。Phase 2 必须等 Day 30 安全门禁后再判断是否出关。
