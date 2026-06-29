# Day 4 - Azure 资源清单

## 1. 目标
将 Azure Resource Graph 中的资源清单同步到 PostgreSQL，解决治理平台没有资源事实表、无法做后续成本和治理关联的生产风险。

## 2. 前置条件
依赖 Day 1 工程骨架、Day 3 Azure SDK 集成、本地 PostgreSQL 和 Azure 资源读取权限。

## 3. 施工范围
允许实现 Resource Graph 查询、资源 DTO 映射、`cloud_resources` 表、repository 行为、Worker 同步任务和幂等 upsert。不允许宣称资源生命周期、删除语义、checkpoint 或关系模型已经生产完整。

## 4. 设计决策
以 `provider` 和 normalized resource identity 作为幂等写入关键，保留 `FirstSeenAt` 和 `LastSeenAt` 以支持后续生命周期语义扩展。

## 5. 实现摘要
新增 Resource Graph 查询路径、资源归一化映射、`cloud_resources` 持久化、Worker resource sync job 和重复同步不插入重复资源的 upsert 行为。

## 6. 验证证据
Day 9 重新运行资源清单 E2E：创建临时 Azure 资源，同步 4 个资源，重复运行 Worker，并验证无重复数据。证据包括 [04-★★★-azure-integration.md](../archive/reference/04-★★★-azure-integration.md)、[05-★★★-data-model.md](../archive/reference/05-★★★-data-model.md)、[baseline-verification-summary.md](../archive/phase-0/baseline-verification-summary.md) 和 [Test-AzureResourceInventory.ps1](../../scripts/Test-AzureResourceInventory.ps1)。

## 7. Review 结论
Accepted。作为开发基线完成，并经过 Day 1～4 审计。

## 8. 遗留风险
未完成 checkpoint、inactive/deleted 语义、资源关系建模、Provider 生产身份和大规模同步可靠性。

## 9. 相关链接
- Commit: `069ebc0` - `feat: complete day 4 Azure resource inventory`
- Commit: `1d65f1b` - `chore: audit day 1-4 implementation`
- [scripts/Test-AzureResourceInventory.ps1](../../scripts/Test-AzureResourceInventory.ps1)
