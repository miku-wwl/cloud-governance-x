# 阶段 0 Gate 报告

## 1. 当前结论

- 执行日期：2026 年 6 月 14 日
- 基线分支：`main`
- Day 10 Commit：`d3d760e`
- 阶段状态：`Validation`
- 自动/机械门禁：通过，存在已登记工具缺口
- 最终人工门禁：等待项目 Owner review
- 是否允许开始 Day 12：**暂不允许**

本报告不使用“基本通过”。当前没有产品测试失败、未知 Azure 资源、测试数据库、
端口、Terraform 运行产物或疑似真实 secret 阻断；保持 `Validation` 的唯一 gate
原因是 Day 11 风险、分类、供应链与 ADR 材料尚未由人工 reviewer 明确批准。

## 2. 证据索引

| 证据 ID | 门禁 | 结论 | 永久文档 | 原始证据 | Commit/基线 |
| --- | --- | --- | --- | --- | --- |
| EVD-0001 | 能力边界 | Passed | `current-capability-baseline.md` | `tmp/phase-0-evidence/` | `6ce8e25` |
| EVD-0002 | build/test | Passed | `baseline-verification-summary.md` | `tmp/phase-0-evidence/day09/` | `6ce8e25` |
| EVD-0003 | 六个 Azure E2E | Passed | `baseline-verification-summary.md` | Day 9 E2E logs | `6ce8e25` |
| EVD-0004 | 严格真实成本 | Passed | `baseline-verification-summary.md` | Day 9 strict cost log | `6ce8e25` |
| EVD-0005 | Day 9 清理 | Passed | `baseline-verification-summary.md` | Day 9 cleanup output | `3c5dbd4` |
| EVD-0006 | 当前架构与 trust boundary | PassedMechanical / ReviewPending | `current-architecture.md` | `tmp/phase-0-evidence/day10/` | `d3d760e` |
| EVD-0007 | 风险登记 | PassedMechanical / ReviewPending | `risk-register.md` | Day 11 review notes | `d3d760e` 后工作树 |
| EVD-0008 | 数据分类 | PassedMechanical / ReviewPending | `data-classification.md` | Day 11 review notes | `d3d760e` 后工作树 |
| EVD-0009 | 依赖许可证 | PassedWithGaps | `dependency-license-inventory.md` | Day 11 package JSON/lock | `d3d760e` 后工作树 |
| EVD-0010 | secret 检查 | GapRegistered | 本报告 §4 | Day 11 grep output | `d3d760e` 后工作树 |
| EVD-0011 | Azure/DB/端口/仓库遗留 | Passed | 本报告 §5 | Day 11 audit output | `d3d760e` 后工作树 |
| EVD-0012 | ADR backlog | PassedMechanical / ReviewPending | `adr-backlog.md` | Day 11 review notes | `d3d760e` 后工作树 |

Day 11 NuGet JSON SHA-256：
`ADE91A0176CF52CCAF75DE841D317F84377D6B03B6E8F658C624AC74284AF239`。
Terraform lock SHA-256：
`05D93F0323FB2C7931B475C99EB3D739D383BE25619D1B949DC82558CAB64C0F`。

## 3. 自动与静态检查

| 命令/检查 | 日期 | 结果摘要 | 已知限制 |
| --- | --- | --- | --- |
| `dotnet test FinOpsPlatform.slnx --no-restore` | 2026-06-14 | 19/19 passed | 单元测试为主 |
| `dotnet list ... --include-transitive` | 2026-06-14 | 直接与传递包已解析 | 不是 SBOM |
| `dotnet list ... --vulnerable` | 2026-06-14 | 当前 NuGet 源未报告漏洞 | 仅当前 advisory 快照 |
| `dotnet list ... --deprecated` | 2026-06-14 | xUnit v2 命中 Legacy | 已登记 RISK-0023 |
| `terraform init -backend=false` + `providers` | 2026-06-14 | azurerm 4.77.0、random 3.9.0 | 插件缓存随后删除 |
| 敏感词 `rg` 与 `git grep` | 2026-06-14 | 命中均为示例、字段名或文档 | 不覆盖高熵和完整历史 |
| `git diff --check` | 2026-06-14 | Passed | 最终提交前需重跑 |

执行者：Codex，运行环境为用户当前 Windows 开发机。原始输出位于被 Git 忽略的
`tmp/phase-0-evidence/`，不作为唯一永久证据。

### 3.1 2026-06-18 文档复核补充

本次复核没有重新执行真实 Azure E2E，也没有升级依赖或 Terraform lock file；
只刷新会随时间变化的工具和依赖事实：

| 命令/检查 | 结果摘要 | Gate 影响 |
| --- | --- | --- |
| `dotnet --version` | `10.0.300`，符合 `global.json` | 无变化 |
| `dotnet list package --vulnerable --include-transitive` | 当前 NuGet 源未报告漏洞 | 无变化；仍需阶段 14 SCA/SBOM |
| `dotnet list package --deprecated` | `xunit 2.9.3` 仍为 Legacy | RISK-0023 保持 Open |
| `dotnet list package --outdated` | EF Core Design、Hosting、Test SDK、xUnit runner、coverlet 有更新 | 新增 RISK-0027 |
| `terraform -chdir=terraform/azure version` | 本机 CLI `1.14.0`，提示 `1.15.6` 可用；Provider lock 仍为 azurerm 4.77.0、random 3.9.0 | 新增 RISK-0027；不自动升级 |

阶段结论仍为 `Validation`。复核没有发现新的产品测试失败或疑似真实 secret，
但进一步确认阶段 1 必须建立固定的依赖和工具链门禁。

## 4. Secret 检查结论

本机没有已批准的 `gitleaks`，因此没有运行自动 secret scanner，也没有声称 Git
历史已完成扫描。当前 tracked 内容的敏感词命中分类如下：

| 命中类型 | 结论 |
| --- | --- |
| `finops_dev_password` | 明确标注的本地不可复用示例；生产禁止 |
| 测试字符串 `secret` | 单元测试输入，不是凭据 |
| `Password`/connection string builder | 配置字段和代码结构 |
| token/secret/access key 文档文字 | 安全规则和施工说明 |
| Azure `TokenCredential`/Bearer header | 认证代码，不含 token 值 |
| Terraform `shared_access_key_enabled` | Azure Storage 功能开关，不是 access key |

结论为 `GapRegistered`：当前未发现疑似真实 secret，但缺少自动高熵、规则库和
完整 Git 历史扫描。RISK-0022 要求阶段 1 固化 scanner；若未来发现真实 secret，
必须先轮换和撤销，再处理 Git 历史。

## 5. 遗留与清理审计

| 对象 | 预期 | 实际 | 结果 |
| --- | --- | --- | --- |
| Azure owner=`cloud-governance-x` Resource Group | 无未知测试资源 | `[]` | Passed |
| PostgreSQL `finops_day*` | 无测试数据库 | 查询无行 | Passed |
| 端口 5000/5103/5105/5106/5107/5108 | 无监听 | 全部 False | Passed |
| Terraform `.terraform/` | 取证后删除 | 已删除 | Passed |
| `*.tfstate*` / `*.tfplan` | 无遗留 | 未发现 | Passed |
| Git tracked 运行产物 | 无 | 只有预期文档变更 | Passed |
| `tmp/` | Git 忽略 | `!! tmp/` | Passed |
| `bin/obj` | Git 忽略 | 仅 ignored build output | Passed |

PostgreSQL Compose 容器和 named volume 是正常本地开发依赖，不属于测试遗留。
未知资源只审计不自动删除。

## 6. Go/No-Go Checklist

| 门禁 | 结论 | 说明 |
| --- | --- | --- |
| Day 8 能力基线人工 review | PassedByProgression | 用户已要求提交并继续 Day 9 |
| Day 9 build/test | Passed | 永久总结有结果 |
| Day 9 六个 E2E 分类 | Passed | 6/6 Passed |
| strict 真实成本证据 | Passed | fallback=false，28 行真实成本 |
| Day 9 清理 | Passed | Azure/DB/端口/state 均清理 |
| Day 10 图与代码一致 | PassedMechanical | 9 图渲染、代码反查和用户要求继续 Day 11 |
| trust boundary 控制与缺口 | PassedMechanical | 当前架构 §12 |
| 风险 Owner/严重度/目标阶段 | PassedMechanical / ReviewPending | 27 条风险 |
| 数据分类覆盖 | PassedMechanical / ReviewPending | 成本、资源、身份、凭据、日志、state、导出 |
| 依赖和供应链登记 | PassedWithGaps | 直接依赖完成；正式 scanner 延后 |
| secret 检查真实结论 | GapRegistered | 无疑似 secret；无 gitleaks/历史自动扫描 |
| ADR 覆盖阶段 1～4 | PassedMechanical / ReviewPending | ADR-0001/0002/0018 已形成候选决策；ADR-0003～0009 保持队列 |
| Git 无运行产物 | Passed | state/plan/tmp 未跟踪 |
| 无未知 Azure 测试资源 | Passed | owner 标签查询 `[]` |
| 无测试 DB/端口 | Passed | 无 `finops_day*`，测试端口无监听 |
| Day 11 人工 review | Pending | 需要项目 Owner 明确结论 |

`PassedByProgression` 表示用户通过“提交并开始下一 Day”的明确动作接受前一 Day
继续施工，不等于独立安全审计签字。

## 7. 阶段结论与下一步边界

当前有效结论是 `Validation`，不是 `Complete` 或 `Blocked`。没有外部阻断，
但 Day 11 人工 review 是阶段 0 的强制门禁。

人工 reviewer 应重点确认：

1. Critical 风险的 Owner 与目标阶段是否合理；
2. 本地开发口令能否继续作为公开示例；
3. 数据分类、retention 待决项是否完整；
4. xUnit Legacy、依赖漂移、容器 digest 和 scanner 缺口是否可带入阶段 1；
5. ADR-0001/0002/0018 的候选决策是否可接受为阶段 1 起点；
6. 是否明确接受“阶段 0 完成不等于可生产部署”。

只有 reviewer 明确批准后，阶段状态才能改为 `Complete` 并允许开始 Day 12。
若 reviewer 不批准，应保持 `Validation` 并把具体异议加入风险或 ADR，不删除
失败证据。
