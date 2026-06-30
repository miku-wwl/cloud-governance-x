# Day 9 - 基线复验

## 1. 目标
重新运行 Day 1-7 的自动化、本地 PostgreSQL、Terraform 和 Azure E2E 检查，解决早期能力缺少可复验基线证据的问题。

## 2. 前置条件
依赖 Day 1-7 开发基线、Azure CLI 登录、Terraform、本地 PostgreSQL 和相关验收脚本。

## 3. 施工范围
允许记录永久 baseline verification summary、分类型 pass/fail 证据、清理证据和禁用 fallback 的真实成本检查。不允许把开发环境复验扩展解释为生产验收。

## 4. 设计决策
基线复验不仅记录成功命令，也记录清理结果，避免临时数据库、端口、Terraform 产物或 Azure 测试资源残留。

## 5. 实现摘要
整理自动化工具、build/test、API health、Terraform、Azure E2E 和 cleanup 的复验结果；加入严格真实成本路径。

## 6. 验证证据
记录结果包括 build 0 warning/0 error、测试通过、6 个 Azure/Terraform E2E 脚本通过、严格真实成本返回 28 行、cleanup 检查通过。证据包括 [baseline-verification-summary.md](../archive/phase-0/baseline-verification-summary.md) 和 `tmp/phase-0-evidence/day09/` 原始输出。

## 7. Review 结论
Accepted。Phase 0 gate 接受该复验证据。

## 8. 遗留风险
复验范围不覆盖 staging、生产身份、备份、高可用、租户隔离或安全测试。

## 9. 相关链接
- Commit: `3c5dbd4` - `docs: complete day 9 baseline verification`
- [docs/archive/phase-0/baseline-verification-summary.md](../archive/phase-0/baseline-verification-summary.md)
