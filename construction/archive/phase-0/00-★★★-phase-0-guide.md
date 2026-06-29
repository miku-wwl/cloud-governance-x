# 00 ★★★ 阶段 0：Day 8～11 施工总指南

> 文档性质：施工与学习手册，不是阶段完成报告。
>
> 对应范围：`construction/01-★★★-construction-plan.md` 阶段 0、
> `construction/02-★★★-day8-production-roadmap.md` Day 8～11。
>
> 当前状态：Day 1～7 已形成开发基线；阶段 0 仍处于 `Validation`。

## 1. 阶段 0 要解决什么问题

Day 1～7 已经证明代码可以连接 PostgreSQL 和 Azure，并完成资源、成本与 ETL
闭环。但“曾经运行成功”还不足以成为生产化改造的可靠起点。

阶段 0 要把现状转化为四类可审查事实：

1. **能力事实**：当前究竟实现了什么、受什么限制、哪些尚未实现；
2. **行为证据**：代码、数据库、Terraform 和真实 Azure 链路能否重复运行；
3. **架构事实**：组件、部署、数据流、身份和信任边界究竟如何连接；
4. **治理事实**：已知风险、敏感数据、依赖许可证和待决架构问题由谁处理。

阶段 0 不负责增加前端、认证、多租户、调度或新 Provider。它负责回答：

```text
我们现在拥有什么？
这些能力如何被证明？
它们为什么还不能直接进入生产？
后续生产化改造要优先关闭哪些风险？
```

## 2. Day 8～11 的依赖顺序

```text
Day 8：建立能力真值
    ↓
Day 9：重新验证这些能力
    ↓
Day 10：把已验证行为画成当前架构与数据流
    ↓
Day 11：基于事实建立风险治理并执行阶段门禁
```

不能交换的原因：

- 没有 Day 8 的能力边界，Day 9 不知道哪些声明需要证明；
- 没有 Day 9 的运行证据，Day 10 可能把过时或不可运行行为画进架构；
- 没有 Day 10 的信任边界，Day 11 的安全和数据风险容易漏项；
- Day 11 是阶段出关日，只能汇总前面三天的事实，不能临时补造证据。

## 3. 详细手册

| Day | 手册 | 核心产物 |
| --- | --- | --- |
| Day 8 | [`01-★★★-day8-capability-baseline.md`](01-★★★-day8-capability-baseline.md) | 当前能力真值表、生产差距表、文档事实统一 |
| Day 9 | [`02-★★★-day9-baseline-verification.md`](02-★★★-day9-baseline-verification.md) | 自动化与真实 E2E 基线验收、清理证据 |
| Day 10 | [`03-★★★-day10-architecture-data-flow.md`](03-★★★-day10-architecture-data-flow.md) | 当前组件图、部署图、数据流图、信任边界 |
| Day 11 | [`04-★★★-day11-risk-and-stage-gate.md`](04-★★★-day11-risk-and-stage-gate.md) | 风险登记、数据分类、依赖许可证、ADR backlog、出关结论 |

## 4. 计划产物与存放规则

### 4.1 永久文档

实际执行 Day 8～11 时，建议在以下目录形成长期事实：

```text
docs/phase-0/
├── current-capability-baseline.md
├── baseline-verification-summary.md
├── current-architecture.md
├── risk-register.md
├── data-classification.md
├── dependency-license-inventory.md
├── adr-backlog.md
└── stage-0-gate-report.md
```

这些文件进入 Git，因为后续阶段需要持续引用和更新。

### 4.2 临时证据

原始命令输出、截图说明、临时查询和日志放入：

```text
tmp/
├── day08-closeout-report.md
├── day09-closeout-report.md
├── day10-closeout-report.md
├── day11-closeout-report.md
└── phase-0-evidence/
```

`tmp/` 已被 `.gitignore` 忽略，不允许提交。长期文档只记录经过整理的结论和
证据索引，不复制包含 token、完整连接字符串或敏感云数据的原始输出。

## 5. 统一状态

每个 Day 使用以下状态：

| 状态 | 含义 |
| --- | --- |
| `NotStarted` | 尚未开始 |
| `Implementation` | 正在形成文档、脚本或证据 |
| `Validation` | 主要产物已形成，正在自动或人工验收 |
| `ReadyForReview` | 验收完成，等待人工 review |
| `Complete` | 人工 review 通过且无阻断项 |
| `Blocked` | 存在无法继续的外部或前置阻断 |

`Complete` 不能只由 Codex 自行宣布。至少需要：

- 交付物存在；
- 自动验收有结果；
- 人工验证完成；
- 清理结果明确；
- 遗留问题已进入风险登记；
- 用户 review 后同意进入下一 Day。

## 6. 每个 Day 的证据最小结构

`tmp/dayNN-closeout-report.md` 至少包含：

```markdown
# Day NN 闭环报告

## 1. 基本信息
- 日期：
- Commit 起点：
- 分支：
- 执行者：
- Azure subscription：
- 环境：

## 2. 本日范围
- 已完成：
- 明确未做：

## 3. 文件变化
- 新增：
- 修改：
- 删除：

## 4. 自动验收
| 命令 | 结果 | 证据位置 | 备注 |

## 5. 人工验证
| 检查项 | 结果 | 观察 | 证据位置 |

## 6. 清理
| 对象 | 预期 | 实际 |

## 7. 风险与限制
| ID | 描述 | 严重度 | 后续处理 |

## 8. Review 结论
- 状态：
- 阻断项：
- 是否允许进入下一 Day：
```

## 7. 阶段 0 共同约束

### 7.1 不新增业务功能

允许：

- 修正文档错误；
- 修复阻断基线验收的缺陷；
- 增加只为可重复取证所需的最小脚本或检查；
- 清理真实垃圾文件；
- 增加阶段 0 永久文档。

不允许：

- 提前实现 Day 12 以后的架构重构；
- 新增认证、多租户、调度、前端或 AWS；
- 为了让测试变绿而降低断言；
- 用 sample 数据冒充真实 Azure 数据；
- 为了“仓库干净”删除仍有生产或学习价值的文件。

### 7.2 事实优先级

判断当前行为时按以下顺序取证：

```text
真实运行结果
    >
自动化测试
    >
源代码与 migration
    >
配置和脚本
    >
README 与专题文档
    >
历史计划和口头描述
```

文档与代码冲突时，先记录冲突，再判断应修文档还是修代码，不能默认文档正确。

### 7.3 安全边界

- 不把 `az account get-access-token` 的 token 写入报告；
- 不记录完整 connection string、client secret 或 access key；
- Azure subscription ID、tenant ID、资源 ID 和成本数据按内部或机密信息处理；
- 截图和日志进入报告前必须脱敏；
- 未知 Azure 资源只登记和确认，不自动删除；
- 清理命令只能作用于本次脚本明确创建的资源。

## 8. 阶段 0 总验收矩阵

| 门禁 | Day | 通过标准 |
| --- | --- | --- |
| 能力边界准确 | 8 | 所有主要能力标记为已实现、受限、未实现或生产禁止 |
| 文档事实一致 | 8 | README、纲领、计划和旧评估不再互相冒充当前事实 |
| 本地工程可重复 | 9 | restore、build、test、PostgreSQL 和健康检查成功 |
| Azure 链路可重复 | 9 | 六个 E2E 有明确结果，真实成本与 sample 路径分开记录 |
| 清理可证明 | 9 | 无测试数据库、监听端口、Terraform state 和未知临时 Resource Group 遗留 |
| 当前架构可解释 | 10 | 组件、部署和数据流图与代码一致 |
| 信任边界明确 | 10 | 每条外部数据流有身份、协议、数据和当前控制说明 |
| 风险可治理 | 11 | 每个风险有严重度、Owner、目标阶段和处理策略 |
| 数据分类存在 | 11 | 成本、标签、身份、凭据、日志和导出有分类与控制要求 |
| 依赖许可证可追踪 | 11 | 直接依赖、关键传递依赖、Terraform Provider 和容器镜像已登记 |
| ADR backlog 完整 | 11 | 后续关键设计决策有编号、触发阶段和状态 |
| 仓库安全整洁 | 11 | Git 干净、secret 检查完成、无不应提交的运行产物 |

## 9. 阶段 0 出关条件

只有以下项目全部有结论，阶段 0 才能从 `Validation` 进入 `Complete`：

- [ ] Day 8 人工 review 通过；
- [ ] Day 9 自动与人工验收通过；
- [ ] Day 10 图与代码交叉 review 通过；
- [ ] Day 11 风险和治理材料 review 通过；
- [ ] 所有失败都有原因和后续处理，不存在“先忽略”；
- [ ] 没有未知 Azure 测试资源；
- [ ] 没有 `finops_day*` 测试数据库遗留；
- [ ] 没有测试 API 端口或后台 `dotnet` 进程遗留；
- [ ] 没有 Terraform state、plan、`.terraform/` 或日志进入 Git；
- [ ] 没有已知 secret 存在于当前 Git 跟踪内容；
- [ ] 阶段 1 的输入和阻断项明确。

## 10. 阶段完成后的学习成果

完成阶段 0 后，应当能独立讲清：

1. Day 1～7 每一条能力的真实证据是什么；
2. 为什么现有系统只能称为开发基线；
3. API、Worker、Application、Domain、Infrastructure 的依赖关系；
4. 资源和成本数据如何从 Azure 进入 PostgreSQL；
5. Terraform 与运行时 ETL 为什么是两条不同控制链；
6. sample fallback 为什么是生产风险；
7. 当前最严重的安全、数据、可靠性和交付风险；
8. 为什么阶段 1 必须先建立架构和工程门禁。
