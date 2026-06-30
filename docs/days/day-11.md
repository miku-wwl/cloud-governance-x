# Day 11 - 风险登记与 Phase 0 出关

## 1. 目标
围绕 Day 1-7 基线建立风险、数据分类、依赖、ADR 和阶段门证据，解决项目进入工程加固前缺少治理账本的问题。

## 2. 前置条件
依赖 Day 8 能力基线、Day 9 复验证据、Day 10 架构快照和 Phase 0 出关要求。

## 3. 施工范围
允许创建 risk register、production gap register、data classification、dependency/license inventory、ADR backlog 和 Phase 0 gate report。不允许把 Phase 0 通过解释为生产风险关闭。

## 4. 设计决策
通过阶段门把“理解了风险并可进入下一阶段”和“生产风险已关闭”分开处理。

## 5. 实现摘要
新增风险登记、生产缺口登记、数据分类、依赖许可证清单、ADR backlog 和 Phase 0 stage gate report。

## 6. 验证证据
Owner 于 2026-06-18 将 Phase 0 签为 `Complete`，允许 Phase 1 / Day 12 启动。证据包括 [stage-0-gate-report.md](../archive/phase-0/stage-0-gate-report.md)、[risk-register.md](../archive/phase-0/risk-register.md)、[production-gap-register.md](../archive/phase-0/production-gap-register.md)、[data-classification.md](../archive/phase-0/data-classification.md)、[dependency-license-inventory.md](../archive/phase-0/dependency-license-inventory.md) 和 [adr-backlog.md](../archive/phase-0/adr-backlog.md)。

## 7. Review 结论
Accepted。Phase 0 完成，授权进入 Phase 1。

## 8. 遗留风险
生产认证、授权、租户隔离、CI/CD、审计、调度、备份和可观测性等风险仍未关闭。

## 9. 相关链接
- Commit: `9175660` - `docs: add phase 0 governance review materials`
- Commit: `1f5382b` - `docs: sign off phase 0 gate`
- [docs/archive/phase-0/stage-0-gate-report.md](../archive/phase-0/stage-0-gate-report.md)
