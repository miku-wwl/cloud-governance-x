# Day 2 - Azure Terraform 生命周期

## 1. 目标
建立可重复创建、核验和销毁的 Azure Terraform 开发生命周期，解决云资源只能手工创建、无法复验和清理的工程风险。

## 2. 前置条件
依赖 Day 1 的项目基础骨架和本地脚本运行环境。后续 Day 9 和 Phase 0 对该生命周期进行基线复验。

## 3. 施工范围
允许创建开发用 Resource Group、Storage Account、Service Bus Namespace 和 Queue，并建立标签约定和验收脚本。不允许引入生产远程 state、环境提升策略、云策略门禁或生产保护承诺。

## 4. 设计决策
Terraform 资源限定为开发闭环资源，强调 create/verify/destroy 的可重复性。状态管理继续使用本地 state，身份使用本地开发身份。

## 5. 实现摘要
新增 Azure Resource Group、Storage Account、Service Bus Namespace、Queue、治理标签和 Terraform 生命周期测试脚本。

## 6. 验证证据
Day 9 重新运行 Day 2 E2E，记录创建并验证 5 个资源，随后销毁资源，确认没有残留 Resource Group 或 state 产物。证据包括 [terraform/azure/README.md](../../terraform/azure/README.md)、[03-★★-terraform.md](../archive/reference/03-★★-terraform.md)、[baseline-verification-summary.md](../archive/phase-0/baseline-verification-summary.md) 和 [Test-AzureTerraformLifecycle.ps1](../../scripts/Test-AzureTerraformLifecycle.ps1)。

## 7. Review 结论
Accepted。作为开发基线完成，并被 Phase 0 基线复验接受。

## 8. 遗留风险
仍使用本地 state 和开发身份；远程 state、环境隔离、策略门禁、生产销毁保护和 least privilege 身份留给后续平台阶段。

## 9. 相关链接
- Commit: `8736988` - `feat: complete day 2 Azure Terraform lifecycle`
- [terraform/azure/README.md](../../terraform/azure/README.md)
- [docs/archive/reference/03-★★-terraform.md](../archive/reference/03-★★-terraform.md)
- [scripts/Test-AzureTerraformLifecycle.ps1](../../scripts/Test-AzureTerraformLifecycle.ps1)