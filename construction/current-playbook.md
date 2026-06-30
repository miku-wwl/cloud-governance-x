# 当前施工手册

- 当前里程碑：M4 - RBAC、端点保护与审计
- 当前位置：Phase 2，Day 28 Accepted 之后
- 当前施工单元：Day 29 - 追加式审计

本文只描述当前施工单元。工程总规划见
[engineering-plan.md](engineering-plan.md)。

## 1. 开工规则

开始实现新施工单元前，必须先完成：

1. 阅读 [outline.md](../outline.md)；
2. 阅读 [docs/current-state.md](../docs/current-state.md)；
3. 阅读 [docs/roadmap.md](../docs/roadmap.md)；
4. 阅读 [engineering-plan.md](engineering-plan.md)；
5. 确认当前施工单元：Day 29 - 追加式审计；
6. 检查风险登记和生产差距登记；
7. 确认 working tree 状态，不覆盖无关用户修改；
8. 只实现当前施工单元，除非 Owner 明确改变范围。

## 2. Day 29 目标

Day 29 要建立追加式审计模型和高权限 action record，关闭“授权已执行但不可追责”的核心缺口。

预期范围：

- 定义审计事件模型；
- 覆盖 actor、tenant、action、target、result、correlation 和时间戳；
- 将高权限 admin/sync/ETL/query 授权结果接入审计写入路径；
- 覆盖成功和失败路径；
- 更新 [docs/current-state.md](../docs/current-state.md)、Day 29 胶囊
  以及相关风险/生产差距文档。Day 29 胶囊应在 Day 29 正式开工时创建。

## 3. Day 29 非目标

Day 29 不应宣称：

- PostgreSQL RLS 已实现；
- React 或浏览器授权体验已存在。
- Phase 2 安全门禁已完成。

这些内容分别留给 Day 30 或后续阶段。

## 4. 设计边界

Day 29 的审计模型必须满足：

- 审计事件只追加，不允许普通业务路径修改历史；
- actor、tenant、action、target、result 和 correlation 必须可追踪；
- 成功和失败都要有审计路径；
- 审计字段不得记录 token、secret 或敏感 raw payload；
- 审计服务不能破坏 Domain、Application、Infrastructure、API、Worker 的依赖方向。

## 5. 验证要求

最低验证：

```powershell
./scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit -SkipDependencyOutdated
```

如果新增数据库 schema 或 seed 数据，还必须执行：

```powershell
./scripts/Test-DatabaseMigration.ps1
```

Day 29 必须补充聚焦测试：

- 授权成功的高权限动作写入审计；
- 授权失败的高权限动作写入审计；
- 审计记录包含 actor、tenant、action、target、result 和 correlation；
- 普通业务路径不能修改审计历史；
- 审计字段不包含 token、secret 或 raw provider payload。

## 6. 出关规则

Day 29 默认保持 `Validation`，直到 Owner 接受：

- 审计事件模型；
- 高权限 action record；
- 成功/失败审计路径；
- 审计字段脱敏边界；
- 文档和风险更新。

Day 29 不关闭 Phase 2。Phase 2 必须等 Day 30 安全门禁后再判断是否出关。
