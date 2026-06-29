# Cloud Governance X 项目纲领

本文是项目稳定纲领，只记录目标、生产原则和硬边界。当前状态、路线和施工细节不在
本文展开。

当前状态见：

- [docs/current-state.md](docs/current-state.md)

里程碑规划见：

- [docs/roadmap.md](docs/roadmap.md)

按 Day 回顾历史见：

- [docs/days/README.md](docs/days/README.md)

## 1. 项目使命

Cloud Governance X 是面向组织级云环境的多云 FinOps 与资源治理平台。

平台长期要帮助平台团队、FinOps 团队、安全团队和资源 Owner 回答：

- 组织拥有哪些云账号、订阅和资源；
- 这些资源归属于哪个 owner、tenant、environment 和 cost center；
- 成本在哪里产生，是否可归因、可解释、可复核；
- 哪些资源、标签、策略、权限或配置存在治理风险；
- 哪些治理发现需要确认、豁免或整改；
- 每个结论来自哪些证据、规则版本和源数据；
- 平台自身是否安全、可观测、可恢复。

## 2. 产品形态

长期产品是生产级平台，核心形态包括：

- .NET 服务和 Worker；
- PostgreSQL；
- Terraform 管理的基础设施；
- Azure 和 AWS Provider adapter；
- API 和授权边界成熟后的 React 前端；
- 可靠事件、审计和通知流。

当前仓库还不是最终生产产品。当前事实以
[docs/current-state.md](docs/current-state.md) 为准。

## 3. 生产级定义

生产级表示系统可以在真实组织中反复运行，并具备安全性、数据正确性、可观测性和
恢复证据。

生产级不等于“功能很多”，也不等于“演示成功一次”。

生产级必须具备以下证据：

- 每个非匿名入口都有标准认证和授权；
- tenant 隔离覆盖读、写、索引、任务、缓存 key 和存储路径；
- 高权限和治理操作有 追加式审计；
- 云凭据不以明文存在于代码、日志、配置文件或数据库字段；
- Provider 访问使用 least-privilege managed identity、workload identity 或短期凭据；
- CI/CD 可重复，artifact 不可变，migration 有门禁；
- 数据具备 lineage、质量检查和正确成本语义；
- ETL 具备 lease、retry、idempotency 和 checkpoint；
- 日志、指标、trace、SLO、告警和 runbook 可用；
- 备份、恢复和灾难恢复经过演练；
- 安全、依赖、secret、容器和 IaC 门禁可重复运行；
- 负载、失败和恢复测试达到目标规模。

## 4. 架构原则

默认架构是 modular monolith，并保留独立 Worker。只有当某个模块有明确独立部署或
独立扩缩证据时，才考虑拆成独立服务。

强制边界：

- Domain 不依赖 Application、Infrastructure、宿主项目、EF Core 或云 SDK。
- Application 不依赖 Infrastructure、ASP.NET、EF Core 或云 SDK。
- API 负责 HTTP、认证/授权、输入输出契约和 composition。
- Worker 负责后台执行控制和 job 生命周期。
- Infrastructure 负责数据库、云 SDK、queue、object storage 和外部 adapter 实现。
- Migration 通过专用 host 或 release step 执行，不在 API/Worker startup 自动执行。
- Provider 差异必须隐藏在小而明确的 capability interface 后面。

## 5. 数据原则

核心数据必须带 tenant、provider 和 account scope。

生产数据必须支持：

- source 和 ingestion lineage；
- raw、normalized、derived、operational 分层；
- 原始币种和明确成本语义；
- 没有汇率证据时禁止跨币种汇总；
- 资源生命周期语义；
- rule 和 finding versioning；
- 持久化的失败、retry 和运行状态；
- retention 和 deletion policy。

sample data、test data、inferred data 和真实 Provider data 必须清楚区分。
sample fallback 不能掩盖生产 Provider failure。

## 6. 安全原则

安全是模型的一部分，不是事后补丁。

强制规则：

- TenantContext 必须来自可信认证和 membership 检查，不能来自任意客户端输入；
- 每个非平台范围都必须包含租户边界；
- 跨租户平台操作需要单独权限、明确目标、reason 和审计；
- 高风险 remediation 默认 dry-run，并需要 approval；
- secret 不能提交，也不能写入日志；
- 生产管理端点不能匿名。

## 7. 交付原则

项目当前采用 milestone + gate 规划。

Day 编号只作为 review capsule，不作为长期排期表，也不代表成熟度百分比。

规划规则：

- 只展开当前 milestone 和下一个 milestone；
- 每个 milestone 用证据关闭，不用乐观判断关闭；
- 历史计划只保留审计价值，不能覆盖当前事实；
- review 不通过时修复当前单元，不创建表面上的新 Day；
- 不用本地-only 证据宣称生产 ready。

## 8. 文档规则

所有项目文档默认使用中文撰写。只有代码标识符、产品名称、命令名称、协议名称、
错误/状态字面量和来源引用可以保留英文。

每个 `docs/days/day-x.md` 文件必须使用以下固定结构：

1. `# Day X - 标题`
2. `## 1. 目标`
3. `## 2. 前置条件`
4. `## 3. 施工范围`
5. `## 4. 设计决策`
6. `## 5. 实现摘要`
7. `## 6. 验证证据`
8. `## 7. Review 结论`
9. `## 8. 遗留风险`
10. `## 9. 相关链接`

## 9. 禁止捷径

生产环境明确禁止：

- 匿名 management API；
- 在仓库或数据库明文字段中保存真实 secret；
- API/Worker startup 抢跑执行 migration；
- sample data 掩盖 Provider failure；
- 没有租户范围的跨租户查询或唯一索引；
- 无边界的大结果 API；
- 没有 lease 或 idempotency 的并发 ETL；
- 只写日志、不持久化失败状态的错误处理；
- 没有审计的 waiver、remediation 或平台操作；
- 没有 approval 和 rollback 证据的自动 destructive remediation；
- 没有恢复演练就宣称 disaster recovery；
- Azure 和 AWS 都未通过 Provider gate 前宣称多云生产能力。

## 10. 当前文档决策

当前文档体系按以下职责组织：

- 本纲领记录原则；
- current-state 记录当前事实；
- roadmap 记录里程碑规划；
- Day 胶囊记录 review 历史；
- ADR、risk register 和 production gap register 记录长期决策与开放风险。

旧 100+ Day 路线只保留历史上下文。
