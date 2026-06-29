# Day 13 - 仓库静态门禁

## 1. 目标
建立一个本地统一静态验证入口，解决仓库格式、配置、链接、secret pattern、Terraform、build/test 等问题只能分散检查的风险。

## 2. 前置条件
依赖 Day 12 格式和 Analyzer 基线。关联 Phase 1 required checks 和工程治理。

## 3. 施工范围
允许新增 `scripts/Test-RepositoryStatic.ps1`，覆盖配置解析、Markdown 链接、secret pattern、垃圾文件、dotnet restore/format/build/test、Terraform 静态验证和工作区不变性检查。不允许把它视为完整 SAST、SBOM、container 或 IaC 供应链门禁。

## 4. 设计决策
所有本地和 CI 静态检查尽量复用同一脚本，减少“本地过、CI 不过”或“CI 规则漂移”的风险。

## 5. 实现摘要
新增 repository static verification script，包含 JSON/YAML/XML/PowerShell/Markdown 检查、secret-pattern 和 garbage-file 检查、dotnet restore/format/build/test、Terraform validate，以及验证门禁不修改工作区。

## 6. 验证证据
首次实现产生 review findings；Phase 1 remediation 关闭 Markdown false positive 和其他 gate 问题。最终验收验证本地和 GitHub Actions 均可运行。证据包括 [Test-RepositoryStatic.ps1](../../scripts/Test-RepositoryStatic.ps1)、[third-review-remediation-report.md](../archive/phase-1/third-review-remediation-report.md) 和 [independent-acceptance-report.md](../archive/phase-1/independent-acceptance-report.md)。

## 7. Review 结论
Accepted。Phase 1 独立验收确认门禁有效。

## 8. 遗留风险
完整 SAST/SBOM/container/IaC 供应链安全门禁留给后续安全和依赖治理阶段。

## 9. 相关链接
- Commit: `c1e4065` - `build: add repository static verification gate`
- Commit: `2062b0f` - Phase 1 review fixes
- [scripts/Test-RepositoryStatic.ps1](../../scripts/Test-RepositoryStatic.ps1)