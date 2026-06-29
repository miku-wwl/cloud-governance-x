# Day 8 - 能力基线

## 1. 目标
冻结 Day 1-7 实际能力，清除“已有能力”和“长期愿景”混淆，解决项目状态叙述过度膨胀的治理风险。

## 2. 前置条件
依赖 Day 1-7 开发基线，以及 Phase 0 要求对当前能力进行事实审计。

## 3. 施工范围
允许整理当前能力基线、生产禁止清单、Day 1-7 证据入口和当前/未来能力分界。不允许新增产品功能或把文档整理当作生产能力提升。

## 4. 设计决策
将已实现的 Azure 数据底座与计划中的生产级多云平台明确分离，后续文档以事实和证据为准。

## 5. 实现摘要
新增 current capability baseline、production-prohibited behavior list、Day 1-7 evidence entrypoints，以及 implemented/planned 能力分界。

## 6. 验证证据
Day 8 输出后来被 Phase 0 gate 接受。证据包括 [current-capability-baseline.md](../archive/phase-0/current-capability-baseline.md)、[01-★★★-day8-capability-baseline.md](../../construction/archive/phase-0/01-★★★-day8-capability-baseline.md) 和 [stage-0-gate-report.md](../archive/phase-0/stage-0-gate-report.md)。

## 7. Review 结论
Accepted。Phase 0 接受该能力基线作为后续治理基础。

## 8. 遗留风险
没有新增认证、租户隔离或运行控制；所有生产风险仍需后续 Day 关闭。

## 9. 相关链接
- Commit: `6ce8e25` - `docs: complete day 8 capability baseline`
- [docs/archive/phase-0/current-capability-baseline.md](../archive/phase-0/current-capability-baseline.md)
- [construction/archive/phase-0/01-★★★-day8-capability-baseline.md](../../construction/archive/phase-0/01-★★★-day8-capability-baseline.md)