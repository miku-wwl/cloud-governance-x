# Day 17 - Worker Job Handler 注册表

## 1. 目标
用显式 handler registry 替代 Worker job `if/else` 选择，解决后台任务分派逻辑扩展困难、退出码和异常路径不清晰的问题。

## 2. 前置条件
依赖 Day 12 工程基线、现有 Worker Resources/Costs job 和 Phase 1 Worker 模块化目标。

## 3. 施工范围
允许新增 `IWorkerJobHandler`、handler registry、dispatcher、Resources/Costs handlers，以及 unknown、duplicate、cancel、failure 和真实进程退出码测试。不允许把一次性 Worker 模型升级为完整调度平台。

## 4. 设计决策
任务名通过 registry 显式解析；重复注册、未知任务、取消和失败必须有确定行为，真实进程退出码需被测试覆盖。

## 5. 实现摘要
新增 Worker job handler 抽象、注册表、dispatcher、Resources/Costs handlers，以及异常路径和进程行为测试。

## 6. 验证证据
Phase 1 final acceptance 验证大小写不敏感 dispatch、duplicate/unknown/cancel/failure tests 和 real process probes。证据包括 [src/FinOps.Worker/Jobs](../../src/FinOps.Worker/Jobs)、[WorkerJobTests.cs](../../src/FinOps.Tests/Worker/WorkerJobTests.cs) 和 [independent-acceptance-report.md](../archive/phase-1/independent-acceptance-report.md)。

## 7. Review 结论
Accepted。Phase 1 独立验收通过。

## 8. 遗留风险
仍是一 shot Worker；scheduler、lease、retry、checkpoint 和 operator controls 留给可靠 ETL 平台阶段。

## 9. 相关链接
- Commit: `5d8315b` - `refactor: add worker job handler registry`
- [src/FinOps.Worker/Jobs](../../src/FinOps.Worker/Jobs)