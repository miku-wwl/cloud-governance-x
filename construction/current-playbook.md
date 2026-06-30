# 当前施工手册

- 当前里程碑：M5 - 生产数据模型
- 当前位置：M4 已正式签发；`M4 - RBAC、端点保护与审计：ACCEPT`
- 当前施工单元：Day 31 - 数据分层 ADR

M4 签发后，项目已授权进入 M5 / Day31。本文只描述当前施工单元。工程总规划见
[engineering-plan.md](engineering-plan.md)。

## 1. 开工规则

开始实现新施工单元前，必须先完成：

1. 阅读 [outline.md](../outline.md)；
2. 阅读 [docs/current-state.md](../docs/current-state.md)；
3. 阅读 [docs/roadmap.md](../docs/roadmap.md)；
4. 阅读 [engineering-plan.md](engineering-plan.md)；
5. 确认当前施工单元：Day 31 - 数据分层 ADR；
6. 检查风险登记和生产差距登记；
7. 确认 working tree 状态，不覆盖无关用户修改；
8. 只实现当前施工单元，除非 Owner 明确改变范围。

## 2. Day 31 目标

Day 31 要建立生产数据模型的分层 ADR，明确 Raw / Normalized / Derived / Operational 数据职责、source、job、schema/parser version、raw reference、敏感数据边界和后续 migration/backfill 约束。

预期范围：

- 起草数据分层 ADR；
- 定义当前资源、成本、ETL、授权审计数据归属；
- 明确 raw payload/reference、schema version、parser version 和 lineage 字段；
- 明确不在 Day31 实现大规模 schema 改造或 backfill；
- 更新 [docs/current-state.md](../docs/current-state.md)、Day 31 胶囊以及相关风险/生产差距文档。

## 3. Day 31 非目标

Day 31 不应宣称：

- 完整生产数据模型已实现；
- 大规模 migration 或 backfill 已完成；
- resource lifecycle、成本语义、数据质量和 retention 已完成。

这些内容分别留给 M5 后续 Day。

## 4. 设计边界

Day 31 的设计边界必须满足：

- 不把样例 raw payload 当作生产 lineage；
- 不把 raw JSON 直接暴露给查询 API；
- 每类数据必须有 owner、source 和 retention 问题记录；
- 当前 ADR 必须能指导 Day32-40 的 schema 和 migration。

## 5. 验证要求

最低验证：

```powershell
./scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit -SkipDependencyOutdated
```

如果新增数据库 schema 或 seed 数据，还必须执行：

```powershell
./scripts/Test-DatabaseMigration.ps1
```

Day 31 必须补充聚焦验证：

- ADR 与当前 schema / repository / ETL 事实一致；
- Markdown local links 通过；
- static verification 通过。

## 6. 出关规则

Day 31 默认保持 `Validation`，直到 Owner 接受：

- 数据分层 ADR；
- 当前数据实体归层；
- 后续 Day32-40 输入；
- 风险与生产差距更新。

Day 31 是 M5 开工设计 Day，不应直接实现后续 Day 的 schema 改造。
