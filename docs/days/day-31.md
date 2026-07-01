# Day 31 - 数据分层 ADR

## 1. 目标

Day31 是 M5 - 生产数据模型的开工设计 Day。目标是建立 Raw / Normalized / Derived / Operational 四层数据模型 ADR，明确当前资源、成本、ETL、授权审计和主数据实体的归层，约束后续 Day32-40 的 lineage、raw reference、schema/parser version、migration、backfill 和 retention 工作。

本 Day 要解决的工程问题是：在开始生产数据模型 schema 改造前，先统一数据事实口径，避免把开发阶段的 `RawJson`、即时投影或 operational audit 误当成可生产审计和可财务追溯的数据模型。

## 2. 前置条件

- M4 - RBAC、端点保护与审计已在 2026-07-01 正式签发 ACCEPT；
- Day30 已完成安全门禁，授权进入 M5 / Day31；
- ADR-0003 已建立 Organization / Tenant / CloudAccount 模型；
- 风险登记中仍存在资源生命周期、成本语义、raw JSON 泄漏、ETL operational metadata、retention 和 Terraform local state 等开放风险；
- 当前代码中已有 `CloudResource`、`CloudCostDaily`、`EtlJobRun`、`AuthorizationAuditEvent` 及 tenant-aware repository 基础。

## 3. 施工范围

允许范围：

- 新增生产数据分层 ADR；
- 梳理当前实体归层和后续字段输入；
- 更新 Day31 胶囊、Day 索引、current-state 和路线图；
- 执行静态验证、数据库 migration 验证和 Azure Terraform 生命周期闭环。

不允许范围：

- 不新增或修改数据库 migration；
- 不执行 legacy backfill；
- 不把 Day32-40 的 schema、resource lifecycle、成本语义、数据质量或 retention 工作提前并入 Day31；
- 不宣称 M5 或生产数据模型已经完成。

## 4. 设计决策

Day31 新增 [ADR-0005](../adr/ADR-0005-data-layering-and-lineage.md)，将生产数据模型分为四层：

| 层 | 职责 | 当前代表对象 | Day31 决策 |
| --- | --- | --- | --- |
| Raw | provider 原始事实保存或引用 | 后续 raw payload store / raw reference | 不暴露给查询 API，必须带敏感级别、hash、source、retention |
| Normalized | 平台标准业务事实 | `CloudResource`、`CloudCostDaily` | 必须逐步补 tenant/account/provider/source/run/schema/parser/raw reference |
| Derived | 聚合、规则结果、finding、报表事实 | 当前主要是即时查询投影 | 后续必须带规则版本和上游 lineage |
| Operational | 系统运行、安全审计和控制面事实 | `EtlJobRun`、`AuthorizationAuditEvent` | 不作为财务或资源事实口径 |

关键取舍：

- `CloudCostDaily.RawJson` 只作为过渡兼容字段，不作为生产 raw 存储方向；
- `CloudResource.TagsJson` 是 normalized attribute bag，不等于完整 raw payload；
- `AuthorizationAuditEvent` 是授权 operational audit，不等于业务数据 lineage；
- nullable `TenantId` 兼容形态必须在后续 migration/backfill gate 中收敛；
- Day31 不改 schema，避免在 ADR 未冻结前引入不可逆迁移。

## 5. 实现摘要

- 新增 `docs/adr/ADR-0005-data-layering-and-lineage.md`；
- 新增本 Day 多合一胶囊 `docs/days/day-31.md`；
- 更新 `docs/days/README.md`，把 Day31 纳入 Day 胶囊索引；
- 更新 `docs/current-state.md` 与 `docs/roadmap.md`，记录 M5 已进入 Day31 validation，最新 Owner accepted Day 仍为 Day30；
- 保持生产代码和数据库 migration 不变。

## 6. 验证证据

本地验证结果：

- `dotnet build FinOpsPlatform.slnx`：通过，0 warning，0 error；
- `dotnet test FinOpsPlatform.slnx --no-restore`：通过，109 passed，1 skipped；
- `./scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit -SkipDependencyOutdated`：首次失败，原因是本地 Terraform provider cache 缺少 `azurerm` / `random` 插件且该命令跳过了 init；未发现 Day31 文档或代码问题；
- `./scripts/Test-RepositoryStatic.ps1 -SkipDependencyOutdated`：通过，包含 Git diff whitespace、候选垃圾文件、secret pattern、JSON/XML/YAML、GitHub Actions、PowerShell parse、Markdown local links、dotnet restore/build/test、Terraform fmt/init/validate；
- `./scripts/Test-DatabaseMigration.ps1`：通过，空库 8 个 migration 应用成功，幂等 rerun 0 pending，legacy backfill、并发 migration、非法回滚、约束和 Worker 负向用例均按脚本预期执行；
- `./scripts/Test-AzureTerraformLifecycle.ps1 -NamePrefix finops31 -Environment day31 -Owner cloud-governance-x -CostCenter learning`：通过，真实执行 Terraform init / fmt / validate / plan / apply / Azure 资源验证 / Service Bus queue 验证 / destroy / state 清空 / resource group 删除检查。

Azure 生命周期实测资源：

- 订阅：`Azure for Students`；
- 资源组：`rg-finops31-day31-bk9kkm`；
- Storage Account：`stfinops31day31bk9kkm`；
- Service Bus Namespace：`sb-finops31-day31-bk9kkm`；
- Service Bus Queue：`governance-events`；
- apply 后验证：发现 2 个顶层 Azure 资源，queue 名称匹配；
- destroy 后验证：Terraform state 为空，资源组不存在，本地 Terraform runtime artifacts 已清理。

## 7. Review 结论

Validation，建议 Owner 签发 ACCEPT。

Day31 的 ADR、实体归层、后续 Day32-40 输入和部署-验证-销毁闭环均已形成。Day31 不自签最终 ACCEPT，Owner 可基于 QA 报告和项目经理报告签发。

## 8. 遗留风险

- Day32 必须把 raw payload reference、ingestion metadata、schema version 和 parser version 落到 schema 方案；
- Day33 必须关闭资源 inactive/deleted 生命周期语义缺口；
- Day34 必须处理成本语义、账期、币种、负成本、refund、迟到修订和 reconciliation；
- Day35/Day42 必须补 ETL attempt、checkpoint、heartbeat、trigger、lease 和 recovery；
- Day37 必须补数据质量和 retention 骨架；
- Day38/39 必须完成 expand/contract、backfill、staging-like migration 和 rollback rehearsal；
- RLS、生产 Provider identity、远端 Terraform state、备份恢复、SLO 和 staging 仍不属于 Day31 完成项。

## 9. 相关链接

- [ADR-0005: 生产数据分层、Lineage 与 Raw Reference](../adr/ADR-0005-data-layering-and-lineage.md)
- [M4 完工报告](../milestones/milestone-4.md)
- [当前状态](../current-state.md)
- [工程规划](../../construction/engineering-plan.md)
- [当前施工手册](../../construction/current-playbook.md)
- [生产差距登记](../archive/phase-0/production-gap-register.md)
- [风险登记](../archive/phase-0/risk-register.md)

