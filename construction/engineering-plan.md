# Cloud Governance X 工程规划总纲

本文是当前权威工程规划。旧的
`construction/archive/02-★★★-day8-production-roadmap.md` 只作为历史材料保留，
不再作为施工依据。

## 1. 当前结论

截至 2026-06-29：

- 当前历史 Phase：**Phase 2 - 身份、租户、RBAC 与审计**；
- 当前权威里程碑：**M4 - RBAC、端点保护与审计**；
- 最新已实现 Day：**Day 28 - 端点保护与授权错误契约**；
- 最新已接受 Day：**Day 28 - 端点保护与授权错误契约**；
- 当前施工单元：**Day 29 - 追加式审计**；
- Phase 2 要到 **Day 30 安全门禁** 后才判断是否出关；
- 下一里程碑是 **M5 - 生产数据模型**，从 **Day 31** 开始；
- 当前权威总规划是 **M0-M10 共 11 个里程碑，Day 1-148 共 148 个施工单元**。

Day 是可验收施工单元，不等于自然日，也不代表生产成熟度百分比。任何 Day 未通过
review 时，优先修复当前 Day，不用创建新 Day 掩盖阻断问题。

## 2. 文档权威关系

发生冲突时按以下顺序处理：

1. 生产安全与数据正确性；
2. [outline.md](../outline.md)；
3. [docs/current-state.md](../docs/current-state.md)；
4. [docs/roadmap.md](../docs/roadmap.md)；
5. 本文；
6. [current-playbook.md](current-playbook.md)；
7. [docs/days/](../docs/days/) 中的 Day 胶囊；
8. [archive/](archive/) 中的历史施工材料。

`current-playbook.md` 可以比本文更细，但不能扩大本文和 roadmap 明确排除的范围。

## 3. 11 个里程碑

| 里程碑 | Day 范围 | 状态 | 工程目标 |
| --- | --- | --- | --- |
| M0 开发基线 | Day 1-7 | Complete | 建立本地 Azure、PostgreSQL、API、Worker 数据底座 |
| M1 基线治理 | Day 8-11 | Complete | 建立事实基线、风险、架构和 Phase 0 gate |
| M2 工程基础 | Day 12-19 | Accepted | 建立静态门禁、架构测试、模块化和独立 Migration Host |
| M3 身份与租户基础 | Day 20-26 | Accepted, phase open | 建立 Tenant、TenantContext、租户感知 repository、OIDC 和 Entra 开发集成 |
| M4 RBAC、端点保护与审计 | Day 27-30 | Active，Day 28 Accepted | 关闭“已认证但未授权”和缺失审计的 Phase 2 核心风险 |
| M5 生产数据模型 | Day 31-40 | Next | 建立 lineage、资源生命周期、成本语义、数据质量和 migration 演练 |
| M6 可靠 ETL 平台 | Day 41-50 | Not started | 调度、lease、retry、checkpoint、backfill 和运维控制 |
| M7 Release A 平台基础 | Day 51-59 | Not started | telemetry、容器、环境、CI/CD、备份恢复和发布门禁 |
| M8 Azure 生产能力 | Day 60-127 | Not started | 生产 Azure Provider、FinOps、治理 workflow、API、frontend、事件、SLO 和平台发布 |
| M9 多云能力 | Day 128-136 | Not started | AWS Provider 和 Azure/AWS 统一契约 |
| M10 系统加固与上线 | Day 137-148 | Not started | 安全、供应链、性能、韧性、DR、Go/No-Go、canary 和运营接管 |

历史 Phase 与新里程碑的关系：

- Phase 0 对应 M1；
- Phase 1 对应 M2；
- Phase 2 覆盖 M3 和 M4；
- Day 31 之后按 M5-M10 管理，不再继续扩散旧 Phase 编号。

## 4. 每个 Day 的执行契约

每个 Day 开工前必须确认：

- 当前 Day 胶囊存在，且使用固定 9 段结构；
- 上一依赖 Day 或阶段门没有阻断项；
- 工作树状态已检查，不覆盖无关修改；
- 本 Day 的风险、差距、ADR 或阶段目标已明确；
- 本 Day 的 non-goals 已写清楚。

每个 Day 收尾必须至少更新：

- 对应 [docs/days/day-x.md](../docs/days/)；
- 受影响的 current-state、roadmap、risk/gap 或 ADR；
- 必要的运行说明或施工手册；
- closeout 证据，长期有效结论不能只留在 `tmp/`。

默认最低验证：

```powershell
./scripts/Test-RepositoryStatic.ps1 -SkipTerraformInit -SkipDependencyOutdated
```

如果改动包含数据库 schema：

```powershell
./scripts/Test-DatabaseMigration.ps1
```

如果改动包含 Azure、Entra、Terraform 或真实 Provider 行为，必须补充对应 E2E 或手工证据，
并记录清理结果。

## 5. 当前施工窗口

当前只允许实际展开 M4，并准备 M5。M6 以后虽然有 Day 总表，但在 M5 出关前不得
把远期 Day 当作承诺范围。

| Day | 施工目标 | 出关证据 |
| --- | --- | --- |
| Day 27 | 权限与范围 RBAC | RBAC 模型、范围评估、allow/deny matrix 和负向授权路径已完成自动化验证并接受 |
| Day 28 | 端点保护与授权错误契约 | 现有业务端点已绑定 RBAC permission，health 保持 anonymous，基础 401/403 已验证 |
| Day 29 | 追加式审计 | 高权限动作成功/失败都有不可篡改审计，审计字段不泄漏敏感值 |
| Day 30 | Phase 2 安全门禁 | tenant escape、IDOR、RBAC、端点保护、审计和真实 Entra 闭环通过 |

## 6. Day 施工总表

### M0 - 开发基线，Day 1-7

| Day | 施工主题 | 验收重点 |
| --- | --- | --- |
| Day 1 | 项目基础骨架、solution、API、Worker、PostgreSQL 本地环境 | 本地 build/test、健康检查和目录边界成立 |
| Day 2 | Azure Terraform 生命周期 | Resource Group、Storage、Service Bus 可创建、核验和销毁 |
| Day 3 | Azure SDK 集成 | `DefaultAzureCredential` 读取订阅，Azure SDK 不泄漏到 Application |
| Day 4 | Azure Resource Graph 资源清单 | 资源分页读取、归一化存储、幂等写入 |
| Day 5 | 资源 ETL 运行历史 | API/Worker 触发资源同步，成功失败均可追踪 |
| Day 6 | Azure Cost POC | 成本 API 可读取或明确 fallback，成本语义边界清楚 |
| Day 7 | Azure Cost ETL | 成本数据入库、查询 API、样例数据来源可追溯 |

### M1 - 基线治理，Day 8-11

| Day | 施工主题 | 验收重点 |
| --- | --- | --- |
| Day 8 | 当前能力真值表和生产禁止项 | 文档不夸大能力，旧 30 天承诺退役 |
| Day 9 | 重跑 Day 1-7 自动化与真实 E2E | build/test、六条 E2E、清理证据完整 |
| Day 10 | 架构图、部署图、数据流和 trust boundary | 图与代码配置一致，敏感数据流可见 |
| Day 11 | 风险登记、数据分类、依赖许可证、ADR backlog、Phase 0 gate | 风险有 Owner/严重度/阶段，Phase 0 有明确结论 |

### M2 - 工程基础，Day 12-19

| Day | 施工主题 | 验收重点 |
| --- | --- | --- |
| Day 12 | `.editorconfig`、analyzer、format 和统一编译策略 | 干净 restore/build 通过，刻意违规可失败 |
| Day 13 | 单一静态验证入口 | JSON/YAML/XML/PowerShell/Markdown/Terraform/build/test/secret 检查可重复 |
| Day 14 | architecture tests | Domain/Application/Infrastructure/API/Worker 边界被自动约束 |
| Day 15 | API endpoint 模块化 | 路由和响应兼容，endpoint 从 `Program.cs` 拆分 |
| Day 16 | Infrastructure DI 拆分 | API/Worker 启动、DI 验证、生命周期和重复注册测试通过 |
| Day 17 | Worker Job Handler 注册表 | Resources/Costs/unknown/cancel/failure 进程语义可测 |
| Day 18 | 独立 Migration Host | API/Worker 不再 startup migration，空库/重复/并发/失败路径可测 |
| Day 19 | CI、PR 模板、ADR 模板、责任边界和 Phase 1 gate | required checks 阻断合并，Phase 1 证据完整 |

### M3 - 身份与租户基础，Day 20-26

| Day | 施工主题 | 验收重点 |
| --- | --- | --- |
| Day 20 | tenancy ADR 和核心模型 | Organization/Tenant/CloudAccount/Membership/scope 边界清楚 |
| Day 21 | tenant 基础 Domain、EF configuration 和 migration | tenant-owned uniqueness、外键、删除和数据库负例通过 |
| Day 22 | 可信 TenantContext | 客户端伪造 tenant 被拒绝，HTTP 和 Worker 都显式建上下文 |
| Day 23 | tenant-aware repository | tenant A/B 数据隔离，所有读写都有 tenant 条件 |
| Day 24 | legacy tenant backfill | 旧数据可重复迁入开发 tenant，不删除重来 |
| Day 25 | OIDC JWT Bearer 验证 | 无 token、过期、issuer、audience、签名错误和匿名 health 边界可测 |
| Day 26 | Microsoft Entra 开发集成 | 真实 Entra token、本地 public client、JWKS/metadata、无 secret 配置 |

### M4 - RBAC、端点保护与审计，Day 27-30

| Day | 施工主题 | 验收重点 |
| --- | --- | --- |
| Day 27 | 权限与范围 RBAC | admin/operator/analyst/auditor/owner 的允许与拒绝矩阵通过 |
| Day 28 | 现有 API 授权与稳定错误 | `/api/admin` 不可匿名，所有端点有 policy 或 anonymous 理由 |
| Day 29 | 追加式审计 | actor、tenant、action、target、result、correlation 可追踪且不可普通修改 |
| Day 30 | Phase 2 安全门禁 | tenant escape、IDOR、后台 tenant 丢失、RBAC 和审计 E2E 通过 |

### M5 - 生产数据模型，Day 31-40

| Day | 施工主题 | 验收重点 |
| --- | --- | --- |
| Day 31 | Raw/Normalized/Derived/Operational 数据分层 ADR | 每类数据归层，source、job、schema/parser version 和 raw reference 明确 |
| Day 32 | ingestion metadata 和 raw payload reference | raw 与 normalized 可追溯，敏感 payload 分类和访问受控 |
| Day 33 | 资源 lifecycle：scan run、active/inactive/deleted、关系 | 完整成功扫描才失活，部分失败不误删 |
| Day 34 | 成本语义：cost type、charge type、账期、币种、修订 | 多币种不混加，负成本、refund、迟到修订可测 |
| Day 35 | ETL operational model | JobDefinition、Run、Attempt、Checkpoint、heartbeat 和 error category 可测 |
| Day 36 | Rule、Finding、Waiver、Event、Outbox、Inbox、Notification 基础 schema | 指纹、版本、状态历史和 tenant 边界清楚 |
| Day 37 | 数据质量规则和 retention 骨架 | 缺字段、重复、错误日期、币种、tenant/account mismatch 可检测 |
| Day 38 | expand/contract、backfill、schema compatibility | 老版本兼容，backfill 可恢复，无静默截断 |
| Day 39 | 接近 staging 数据量的 migration 与恢复演练 | 时长、锁、恢复点和 reconciliation 有证据 |
| Day 40 | M5 数据门禁 | 数据语义、lineage、质量、恢复和 data dictionary 全部有结论 |

### M6 - 可靠 ETL 平台，Day 41-50

| Day | 施工主题 | 验收重点 |
| --- | --- | --- |
| Day 41 | Job Handler、JobDefinition、参数 schema、版本和并发策略 | 非法参数、未知版本和不支持 Job 失败可诊断 |
| Day 42 | schedule、due-job scan、manual trigger、pause/resume、backfill | 时区、重复扫描、禁用任务和授权审计可测 |
| Day 43 | 队列 ADR 与 enqueue/dequeue 基础能力 | 并发消费、重复投递和数据库暂不可用测试 |
| Day 44 | lease、heartbeat、lease expiry、distributed lock | 双 Worker 争抢、崩溃、租约过期接管可测 |
| Day 45 | idempotency key、checkpoint、continuation token、partition | 中断续跑和重复消息不重复写 |
| Day 46 | timeout、cancellation、retry、jitter、错误分类 | 429、5xx、超时、取消、永久错误路径可测 |
| Day 47 | partial success、quarantine、dead-letter、operator replay | 部分失败可追踪，重放有审计且不绕过次数限制 |
| Day 48 | 受保护 Job 管理 API | 查询、触发、取消、重试、回放均有 RBAC、tenant scope 和审计 |
| Day 49 | ETL 指标 | queue delay、duration、records、retry、freshness、backlog 可采集 |
| Day 50 | M6 可靠性门禁 | 双 Worker、崩溃、续跑、重复、取消、DB/Provider 故障 E2E 通过 |

### M7 - Release A 平台基础，Day 51-59

| Day | 施工主题 | 验收重点 |
| --- | --- | --- |
| Day 51 | OpenTelemetry 基线 | correlation 跨 API、Worker、Migration、DB 和外部调用可追踪 |
| Day 52 | development dashboard 与基础告警 | 故意失败能触发指标和告警，告警有 Owner 和入口 |
| Day 53 | API/Worker/Migration Host 生产容器 | non-root、多阶段、健康检查、优雅关闭、只读文件系统 |
| Day 54 | development 基础设施、remote state、locking、secret store | state 加密和锁验证，环境可重建 |
| Day 55 | 最小 staging 环境 | staging 部署、身份、数据库、队列和遥测闭环 |
| Day 56 | artifact、container image、SBOM 和扫描 | artifact 可追溯到 commit，高危扫描阻断 |
| Day 57 | CD 到 development 和 staging promotion | 同一 artifact 晋级，smoke、migration gate 和回滚演练 |
| Day 58 | 备份、PITR 和首次恢复演练 | 独立环境恢复，RPO/RTO 实测，核心数据一致 |
| Day 59 | Release A 全局门禁 | 身份、tenant、数据、ETL、CI/CD、遥测、备份和回滚证据包完整 |

### M8 - Azure 生产能力，Day 60-127

| Day | 施工主题 | 验收重点 |
| --- | --- | --- |
| Day 60 | tenant-aware Azure connection 和 subscription onboarding | onboard/suspend/reconnect/offboard，连接不保存明文 secret |
| Day 61 | Managed Identity 或 Workload Identity | 不依赖开发机 Azure CLI，凭据轮换和权限撤销可测 |
| Day 62 | permission preflight、capability discovery、connection health | 缺权限能指出 scope/action，能力差异不伪装成功 |
| Day 63 | Resource Graph 多 subscription、分页、限流、重试、query version | 大结果分页、429、单订阅失败和取消可测 |
| Day 64 | full-scan、一致性、关系、inactive、partial failure | 部分扫描不误失活，关系与 lifecycle E2E 通过 |
| Day 65 | Cost Management 生产语义，移除生产 sample fallback | actual/amortized、动态列、币种、空账单和错误响应可测 |
| Day 66 | 成本 backfill、迟到修订、重算、source reconciliation | 同一账期重跑安全，修订可追溯 |
| Day 67 | Azure Policy Insights | definition、assignment、state、exemption 来源清楚 |
| Day 68 | Azure Monitor 批量指标 | missing metric、窗口、限流和批量查询可测 |
| Day 69 | Azure Provider contract 与 resilience | credential 过期、429、5xx、partial、empty、malformed response 全覆盖 |
| Day 70 | Azure staging E2E | 多订阅资源、成本、Policy、Monitor、freshness、runbook 和清理证据 |
| Day 71 | 成本归因模型 | direct、relationship-derived、rule-allocated、unallocated 有依据和 confidence |
| Day 72 | subscription/service/RG/tag/owner/environment/cost-center 归因 | 标签变化、冲突、缺失和 shared cost 可测 |
| Day 73 | budget、threshold、owner、notification event | 月/季边界、币种、阈值重复和审计可测 |
| Day 74 | 趋势、period comparison、forecast 基线 | 账期边界、修订重算、稀疏数据可测 |
| Day 75 | 第一版异常检测和 backtest | 负成本、零转非零、稀疏数据、误报反馈可测 |
| Day 76 | 成本数据质量和 reconciliation | 缺数据、重复、异常突变和汇率缺失可见 |
| Day 77 | FinOps 查询 API 与 lineage 解释 | 指标可追溯 source/job/rule，多币种隔离 |
| Day 78 | FinOps staging E2E | 归因、预算、趋势、异常、质量、重算证据完整 |
| Day 79 | Rule definition、version、parameter、scope、severity、effective date | 规则版本不可覆盖历史，非法参数失败 |
| Day 80 | evaluator、dry-run、影响预览、required-tags 规则 | dry-run 不写 finding，真实执行可重复 |
| Day 81 | finding fingerprint、evidence、first/last seen、状态历史 | 去重、重开、解决、规则升级可测 |
| Day 82 | waiver、approver、scope、expiry、renewal | 过期后恢复，越权豁免失败，完整审计 |
| Day 83 | Owner、assignment、comment、evidence workflow | 权限、并发更新和历史记录可测 |
| Day 84 | recommendation、manual task、ticket 契约、remediation approval 骨架 | 默认只读建议，审批和执行身份边界明确 |
| Day 85 | Azure Policy finding 与平台规则映射 | 来源隔离，云端 exemption 映射可测 |
| Day 86 | Compliance、Finding、Waiver、Remediation API/报告 | 分页、tenant、RBAC、审计和导出边界可测 |
| Day 87 | 合规闭环 staging E2E | 发现、分配、豁免、到期、整改建议、解决、重开全链路 |
| Day 88 | `/api/v1`、API 分类、稳定 endpoint module | OpenAPI 可生成，旧路由迁移或兼容策略明确 |
| Day 89 | Problem Details、error code、correlation、日志脱敏 | 失败响应 schema 一致，不泄漏 stack、SQL、token |
| Day 90 | pagination、filter/sort 白名单、query limits、UTC 日期语义 | 大结果不能无界返回，非法过滤稳定失败 |
| Day 91 | rate limit、idempotency key、optimistic concurrency、ETag | 重复写、并发写、限流和重试可测 |
| Day 92 | query/operator/admin/internal/webhook/export 权限与审计矩阵 | 每类 API 的 auth、scope、limit 和 audit 清楚 |
| Day 93 | async export job、对象存储、过期、下载授权 | 大导出异步，URL 不含 secret，跨 tenant 下载失败 |
| Day 94 | OpenAPI contract、breaking-change、客户端兼容测试 | 实现与 OpenAPI 一致，破坏性变更阻断 CI |
| Day 95 | API 安全、负载、tenant、错误和 staging E2E | p95 达标，越权与大查询被阻断 |
| Day 96 | React + TypeScript 工程基础 | lint/test/build/router/query state，配置不打包 secret |
| Day 97 | OIDC PKCE、token lifecycle、typed API client、tenant/account selector | 登录、登出、过期、刷新、未授权和 tenant 切换可测 |
| Day 98 | design shell、权限导航、loading/empty/stale/partial/error | 不同角色菜单与操作，错误能定位后端 trace |
| Day 99 | Overview 和 Cost 页面 | 多币种、筛选、预算、趋势、异常和 freshness 展示正确 |
| Day 100 | Resources 和 Provider Connections 页面 | lifecycle、关系、连接健康、权限诊断和分页验证 |
| Day 101 | Compliance、Findings、Waivers、Remediation 页面 | 权限动作、状态流、证据、豁免到期和审计可见 |
| Day 102 | Anomalies、ETL Runs、Audit、Platform Operations 页面 | 重试/取消需授权，审计不可编辑，大表分页 |
| Day 103 | accessibility、键盘、响应式、虚拟化、CSP、安全 header | 基础 WCAG、XSS/CSP、超大表和慢网状态可测 |
| Day 104 | 浏览器 E2E、角色矩阵和前端出关 | 登录到核心治理流程闭环，无越权 |
| Day 105 | Governance Event contract | schema version、correlation、causation、tenant/provider/subject 必填 |
| Day 106 | transactional outbox 和 reliable publisher | DB 提交后事件最终发布，重复发布有稳定 event ID |
| Day 107 | Azure Service Bus topology | Terraform、网络、身份、队列、重试、dead-letter 和费用 review |
| Day 108 | inbox、幂等 consumer、handler version、处理历史 | 重复、乱序、consumer crash 和并发可测 |
| Day 109 | retry、poison、dead-letter 查询、诊断、replay | 最大重试不被绕过，重放有授权审计 |
| Day 110 | 告警 dedupe、aggregation、suppression、maintenance、ack、resolution | 告警风暴、静默窗口和恢复可测 |
| Day 111 | 通知适配器 | 至少一个真实渠道，429/5xx/模板失败/重试/delivery audit 可测 |
| Day 112 | 事件到告警到通知 staging E2E | outbox、bus、inbox、告警、通知、死信和重放证据 |
| Day 113 | trace propagation | API、Worker、DB、Service Bus、Provider 可端到端追踪 |
| Day 114 | API/runtime/DB/queue/Job/Provider/quality/freshness 指标 | 名称、单位、维度和 cardinality review |
| Day 115 | availability、latency、ETL、freshness、event、dead-letter SLI/SLO | SLO 可计算，外部云延迟与平台责任分开 |
| Day 116 | executive、API、Worker、DB、queue、Provider、tenant hotspot、release dashboard | Dashboard 可回答健康、影响范围和变化时间 |
| Day 117 | 可行动告警、路由、严重度、maintenance、test-alert | 每个告警有 Owner、阈值理由和 runbook |
| Day 118 | 关键 runbook 和 staging incident drill | 非作者可按 runbook 定位和恢复 |
| Day 119 | observability 门禁 | SLO、Dashboard、告警、trace、runbook 和演练证据完整 |
| Day 120 | 容器 hardening、limits、graceful shutdown、probe | non-root、read-only、信号处理、扫描和资源限制可测 |
| Day 121 | 目标运行平台 ADR 与部署单元 | API、Worker、Migration、Scheduler 可独立部署和扩缩 |
| Day 122 | TLS、入口、网络策略、workload identity、secret integration | 网络拒绝、secret rotation、证书和身份可测 |
| Day 123 | HPA、PDB、anti-affinity、rolling/canary、graceful drain | 扩缩、节点维护、pod kill 和 rollout 验证 |
| Day 124 | Terraform modules、环境 state、drift、policy check、生产保护 | plan artifact、apply approval、drift 检测和 state restore |
| Day 125 | CI 扩展：integration、contract、migration、frontend、安全、IaC、container、license、provenance | 所有门禁可重复，失败阻断 artifact 晋级 |
| Day 126 | CD 扩展：promotion、migration、smoke、canary、rollback、release notes、审计 | 同一 artifact 晋级，应用和 migration 回滚演练 |
| Day 127 | Azure Production Release B 门禁 | Azure 生产能力证据包完整，才允许首批 Azure tenant |

### M9 - 多云能力，Day 128-136

| Day | 施工主题 | 验收重点 |
| --- | --- | --- |
| Day 128 | AWS 账号、Organizations、Cost Explorer/CUR、Config、权限预检、AWS Provider ADR | 功能可用性、费用、区域和账号边界清楚 |
| Day 129 | IAM Role、STS AssumeRole、external ID、connection health | 短期凭据、轮换、错误信任策略和最小权限可测 |
| Day 130 | Organization/account/region discovery 和 onboarding | global/region 语义、suspend/offboard、partial account failure |
| Day 131 | Resource Explorer、Tagging API、分页限流 inventory | 多 region、global resource、覆盖缺口和 429 可测 |
| Day 132 | EC2、EBS、EIP、S3、ELB、RDS 等专用适配和 lifecycle | identity、关系、标签、删除和 unsupported diagnostics |
| Day 133 | Cost Explorer/CUR 生产成本链路 | unblended/amortized、credit/refund、迟到数据和币种可测 |
| Day 134 | AWS Config 和 CloudWatch | Config 来源隔离、missing metrics、限流和权限可测 |
| Day 135 | Azure/AWS 查询、归因、finding、事件和 Dashboard 统一 | 一个 Provider 故障不阻塞另一个，特有语义可追溯 |
| Day 136 | 双云 staging E2E 和 Release C 门禁 | 双云 onboarding、资源、成本、合规、指标、事件、SLO 和 runbook 证据 |

### M10 - 系统加固与上线，Day 137-148

| Day | 施工主题 | 验收重点 |
| --- | --- | --- |
| Day 137 | threat model、资产、攻击者、trust boundary、STRIDE | 每项威胁有预防、检测、响应或明确接受 |
| Day 138 | OWASP API、IDOR、SSRF、injection、auth bypass、tenant escape、export leakage | 无未接受 Critical/High |
| Day 139 | Azure/AWS 最小权限、secret rotation、break-glass、incident response | 权限差距、凭据轮换和应急访问演练 |
| Day 140 | SAST、DAST、dependency、container、IaC、SBOM、provenance、license 门禁 | artifact 可验证，风险接受有 Owner 和到期时间 |
| Day 141 | 容量模型和负载数据集 | tenant、account、resource、cost row、finding、event、user、retention 目标明确 |
| Day 142 | API、aggregation、export、ETL、bulk upsert、queue、Dashboard 负载测试 | p50/p95/p99、throughput、资源使用、索引和容量结论 |
| Day 143 | DB/queue/Provider/network/Worker/API/bad deployment/bad rule 故障注入 | 无 silent data loss，自动恢复和告警符合预期 |
| Day 144 | PostgreSQL PITR、对象存储、Terraform state、配置、队列、环境重建 DR 演练 | RPO/RTO 实测，恢复后 reconciliation |
| Day 145 | 正式 Go/No-Go | 产品、工程、数据、运行、安全、性能和 DR 证据汇总 |
| Day 146 | 内部 tenant 发布 | 限制账号、用户和只读能力，观察 SLO、告警、数据质量和支持 |
| Day 147 | canary tenant 发布 | onboarding、运行观察、扩大或回滚决策有记录 |
| Day 148 | 稳定期 review 和运营接管 | on-call、运营日历、access review、容量、成本、漏洞、DR 和复盘节奏生效 |

## 7. 计划调整规则

允许调整计划的情况：

- review 发现设计模型不成立；
- 安全、数据正确性或生产门禁要求改变；
- 真实 Provider、数据库、身份平台行为与假设不一致；
- 当前 Day 暴露出必须先解决的前置风险。

不允许的调整：

- 为了显得进度快而把阻断问题挪到后续 Day；
- 用“已经完成很多 Day”替代阶段门证据；
- 把样例 fallback、匿名端点或本地开发身份包装成生产能力；
- 在没有 Owner 接受的情况下关闭 Critical/High 风险。

调整后必须更新：

- 本文；
- [docs/roadmap.md](../docs/roadmap.md)；
- 相关 Day 胶囊；
- 风险或生产差距登记。
