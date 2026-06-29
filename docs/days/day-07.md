# Day 7 - Azure 成本 ETL

## 1. 目标
把成本 POC 升级为可重复 ETL 路径和查询 API，解决成本数据只能一次性验证、无法查询和追踪执行历史的问题。

## 2. 前置条件
依赖 Day 5 ETL run 记录、Day 6 成本 POC、PostgreSQL 和 Worker 入口。

## 3. 施工范围
允许实现 Cost Worker job、cost sync service、日成本/service/resource group 查询 API、幂等 upsert 和 ETL run tracking。不允许宣称预算、归因、异常检测、生产调度或完整 FinOps 能力。

## 4. 设计决策
成本 ETL 与资源 ETL 共用运行记录思想，查询 API 按 currency 保持总额一致性，重复运行不得产生重复行。

## 5. 实现摘要
新增成本 Worker job、成本同步服务、日成本和聚合查询 API、成本幂等写入和 ETL 运行历史。

## 6. 验证证据
Day 9 重新运行 Day 7 E2E 和严格真实成本检查，验证重复运行无重复行，并验证 daily、service、resource-group 查询按 currency 汇总一致。证据包括 [README.md](../../README.md)、[04-★★★-azure-integration.md](../archive/reference/04-★★★-azure-integration.md)、[05-★★★-data-model.md](../archive/reference/05-★★★-data-model.md)、[baseline-verification-summary.md](../archive/phase-0/baseline-verification-summary.md) 和 [Test-AzureCostEtl.ps1](../../scripts/Test-AzureCostEtl.ps1)。

## 7. Review 结论
Accepted。完成 Day 1-7 开发数据底座，并经过配置文档 review。

## 8. 遗留风险
仍是本地/开发数据底座；生产 FinOps 语义、预算、归因、异常检测、可靠调度和运营治理未完成。

## 9. 相关链接
- Commit: `5986d2f` - `feat: complete day 7 Azure cost ETL`
- Commit: `7b25d41` - `chore: audit day 1-7 and document configuration`
- [scripts/Test-AzureCostEtl.ps1](../../scripts/Test-AzureCostEtl.ps1)