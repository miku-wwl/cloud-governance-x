# Day 16 - Infrastructure DI 拆分

## 1. 目标
把 Infrastructure service registration 拆成更清晰的宿主适配模块，解决依赖注册职责混杂和 lifetime 行为不易验证的问题。

## 2. 前置条件
依赖 Day 12 工程基线、现有 Infrastructure 依赖注册和 Phase 1 模块化目标。

## 3. 施工范围
允许拆分 application use-case、PostgreSQL、Azure、health-check 注册，并增加 DI idempotence 和 validation tests。不允许引入生产 secret management、部署配置验证或环境晋级能力。

## 4. 设计决策
通过小型扩展方法表达不同注册边界，同时用 `ValidateOnBuild`、`ValidateScopes` 和重复调用测试保护 DI 行为。

## 5. 实现摘要
拆分 Application use-case registration、PostgreSQL registration、Azure registration、health-check registration，并新增 DI 幂等与验证测试。

## 6. 验证证据
Phase 1 final acceptance 验证 split registrations、`ValidateOnBuild`/`ValidateScopes` 和 duplicate-call tests 通过。证据包括 [DependencyInjection.cs](../../src/FinOps.Infrastructure/DependencyInjection.cs)、[ApplicationUseCaseServiceCollectionExtensions.cs](../../src/FinOps.Infrastructure/ApplicationUseCaseServiceCollectionExtensions.cs)、[DependencyInjectionTests.cs](../../src/FinOps.Tests/Infrastructure/DependencyInjectionTests.cs) 和 [independent-acceptance-report.md](../archive/phase-1/independent-acceptance-report.md)。

## 7. Review 结论
Accepted。Phase 1 独立验收通过。

## 8. 遗留风险
生产 secret management、部署配置验证、环境 promotion 和配置漂移治理仍未完成。

## 9. 相关链接
- Commit: `2488310` - `refactor: split infrastructure dependency injection`
- [src/FinOps.Tests/Infrastructure/DependencyInjectionTests.cs](../../src/FinOps.Tests/Infrastructure/DependencyInjectionTests.cs)