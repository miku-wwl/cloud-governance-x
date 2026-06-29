# Day 6 - Azure 成本 POC

## 1. 目标
验证 Azure Cost Management 数据可以被查询并以日粒度聚合入库，解决平台没有成本事实来源的产品风险。

## 2. 前置条件
依赖 Day 3 Azure SDK 集成、Day 5 ETL 基础、本地数据库和 Azure Cost Management 可读权限。

## 3. 施工范围
允许实现成本查询、按 service/resource group/currency 聚合的日成本持久化，以及本地学习用 sample fallback。不允许在生产使用 sample fallback 或宣称完整 FinOps 成本语义。

## 4. 设计决策
成本数据先以日聚合形式落库，保留 currency 字段，避免无证据跨币种聚合。sample fallback 只用于本地学习，不能遮蔽真实 Provider 失败。

## 5. 实现摘要
新增 Azure Cost Management 查询路径、日成本持久化、按 service/resource group/currency 的聚合模型和本地 sample fallback 行为。

## 6. 验证证据
Day 9 重新运行成本检查，并在禁用 fallback 的严格模式下获得 28 行真实 Azure 成本数据。证据包括 [04-★★★-azure-integration.md](../archive/reference/04-★★★-azure-integration.md)、[05-★★★-data-model.md](../archive/reference/05-★★★-data-model.md)、[baseline-verification-summary.md](../archive/phase-0/baseline-verification-summary.md) 和 [Test-AzureCostPoc.ps1](../../scripts/Test-AzureCostPoc.ps1)。

## 7. Review 结论
Accepted。作为开发基线完成。

## 8. 遗留风险
sample fallback 生产禁用；尚未覆盖 amortized/unblended、charge type、billing period、revision history、resource-level precision 和 currency conversion。

## 9. 相关链接
- Commit: `be28bd6` - `feat: complete day 6 Azure cost POC`
- [scripts/Test-AzureCostPoc.ps1](../../scripts/Test-AzureCostPoc.ps1)