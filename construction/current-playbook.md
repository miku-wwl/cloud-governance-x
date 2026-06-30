# 当前施工手册

- 当前里程碑：M4 - RBAC、端点保护与审计
- 当前位置：M4 / 历史 Phase 2，Day 29 Accepted 之后
- 当前施工单元：Day 30 - Phase 2 安全门禁

本文只描述当前施工单元。工程总规划见
[engineering-plan.md](engineering-plan.md)。

## 1. 开工规则

开始实现新施工单元前，必须先完成：

1. 阅读 [outline.md](../outline.md)；
2. 阅读 [docs/current-state.md](../docs/current-state.md)；
3. 阅读 [docs/roadmap.md](../docs/roadmap.md)；
4. 阅读 [engineering-plan.md](engineering-plan.md)；
5. 确认当前施工单元：Day 30 - Phase 2 安全门禁；
6. 检查风险登记和生产差距登记；
7. 确认 working tree 状态，不覆盖无关用户修改；
8. 只实现当前施工单元，除非 Owner 明确改变范围。

## 2. Day 30 目标

Day 30 要执行 Phase 2 安全门禁，复核 tenant escape、IDOR、RBAC、端点保护和审计闭环，判断 Phase 2 是否可以出关。

预期范围：

- 复核 Day 20-29 的身份、租户、RBAC、端点保护和审计证据；
- 增加 tenant escape / IDOR / missing tenant / wrong role 的组合验证；
- 检查所有业务端点都有 permission 或 explicit anonymous 理由；
- 确认审计成功/失败路径可追踪；
- 更新 [docs/current-state.md](../docs/current-state.md)、Day 30 胶囊
  以及相关风险/生产差距文档。Day 30 胶囊应在 Day 30 正式开工时创建。

## 3. Day 30 非目标

Day 30 不应宣称：

- PostgreSQL RLS 已实现；
- React 或浏览器授权体验已存在。
- M5 生产数据模型已开始或完成。

这些内容分别留给 M5 或后续阶段。

## 4. 设计边界

Day 30 的安全门禁必须满足：

- 不用“已完成很多 Day”替代安全证据；
- 每个通过项都必须有自动化测试或明确人工证据；
- 未关闭的风险必须写清楚是 Phase 2 阻断项、后续阶段风险还是接受风险；
- 不得把 Day29 Accepted 直接写成 M4 或历史 Phase2 Accepted。

## 5. 验证要求

最低验证：

```powershell
./scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit -SkipDependencyOutdated
```

如果新增数据库 schema 或 seed 数据，还必须执行：

```powershell
./scripts/Test-DatabaseMigration.ps1
```

Day 30 必须补充聚焦测试：

- tenant escape 被拒绝；
- IDOR 被拒绝；
- 业务端点无匿名绕过；
- 错误 role 被拒绝且有审计；
- 允许路径有审计；
- Phase 2 gate 报告清楚列出 Accept / Validation / Blocked。

## 6. 出关规则

Day 30 默认保持 `Validation`，直到 Owner 接受：

- Phase 2 安全门禁；
- tenant escape / IDOR / RBAC / endpoint / audit 证据；
- 风险与生产差距状态；
- 是否生成 [docs/milestones/milestone-4.md](../docs/milestones/)。

Day 30 是 Phase 2 出关判断点；未通过时保持 Validation 或 Blocked，不进入 M5。
