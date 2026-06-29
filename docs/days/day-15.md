# Day 15 - API Endpoint 模块化

## 1. 目标
从 `Program.cs` 中拆分端点注册，解决 API 入口过度集中、后续授权和路由治理难以审查的工程问题。

## 2. 前置条件
依赖 Day 12 工程基线和现有 API 路由行为。关联 Phase 1 架构整理目标。

## 3. 施工范围
允许拆分 Resources、Costs、ETL、Cloud、Health 端点模块，并增加路由清单和兼容性测试。不允许改变既有 HTTP contract 或宣称 API versioning、OpenAPI 治理、分页、授权和生产错误模型完成。

## 4. 设计决策
通过端点模块保留现有路由行为，同时把不同领域的 HTTP 注册从启动文件中拆开，为后续端点授权做准备。

## 5. 实现摘要
新增 Resources、Costs、ETL、Cloud、Health 端点模块，以及路由清单和兼容性测试。

## 6. 验证证据
Phase 1 最终验收确认端点模块、路由清单、binding defaults 和关键响应形状覆盖通过。证据包括 [src/FinOps.Api/Endpoints](../../src/FinOps.Api/Endpoints)、[EndpointRouteTests.cs](../../src/FinOps.Tests/Api/EndpointRouteTests.cs) 和 [independent-acceptance-report.md](../archive/phase-1/independent-acceptance-report.md)。

## 7. Review 结论
Accepted。Phase 1 独立验收通过。

## 8. 遗留风险
API versioning、完整 OpenAPI 治理、分页、授权和稳定 production error contract 仍在后续 API 工作中处理。

## 9. 相关链接
- Commit: `9c486f5` - `refactor: split api endpoint modules`
- [src/FinOps.Api/Endpoints](../../src/FinOps.Api/Endpoints)
