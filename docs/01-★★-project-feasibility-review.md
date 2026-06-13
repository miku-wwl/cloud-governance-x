# 01 ★★ 项目整体合理性与工期可行性评估

## 1. 评估范围

本报告基于以下内容进行评估：

- `outline.md` 中的项目定位和求职目标；
- `construction-plan.md` 中的 30 天建设计划；
- 当前已经完成并真实闭环的 Day 1～7 工程；
- 现有代码、数据库迁移、自动化测试、Terraform 和端到端脚本；
- Day 8～30 尚未实现的 Azure 治理、前端、消息系统和 AWS 接入。

评估日期：2026 年 6 月 14 日。

## 2. 总体结论

### 2.1 项目方向合理

这个项目适合作为云平台、Azure SRE、FinOps、云治理和 .NET 后端岗位的综合
作品集。它不是单纯展示 CRUD，而是覆盖了：

- Terraform 管理云资源生命周期；
- 云 API 和 SDK 集成；
- 成本与资源 ETL；
- PostgreSQL 归一化数据模型；
- Web API 与 Worker 两种宿主；
- 幂等写入、执行历史和失败记录；
- 合规规则、异常检测和消息队列的后续扩展；
- Azure 与 AWS 的统一治理目标。

这些能力之间有真实业务联系，能够形成“资源发现、成本分析、合规检查、风险
发现、事件通知、整改建议”的完整故事，而不是无关技术的简单堆叠。

### 2.2 当前 Day 1～7 的实现路径正确

Day 1～7 已建立可靠的数据底座：

- 61 个非 migration C# 源文件；
- 10 个测试源文件；
- 3 次 EF Core 数据库迁移；
- 19 个自动化测试；
- 6 个真实端到端 PowerShell 验收脚本；
- Azure Terraform 创建、核验、销毁闭环；
- Azure CLI 与 `DefaultAzureCredential` 认证；
- Azure Resource Graph 资源清单；
- Azure Cost Management 真实成本数据；
- 资源和成本 ETL 执行历史；
- 重复同步幂等性；
- PostgreSQL、API、Worker 和 Azure 的联合验证。

现有依赖方向基本正确：

```text
Api / Worker
      ↓
Infrastructure
      ↓
Application
      ↓
Domain
```

Application 已经定义 `ICloudResourceInventoryProvider`、
`ICloudCostProvider` 和 `ICloudComplianceProvider` 等云厂商无关接口，
Azure SDK 也被限制在 Infrastructure 层。这个基础能够支撑后续 AWS 接入。

### 2.3 30 天计划只能按“作品集 MVP”理解

如果目标是：

```text
单人、全职投入、能够现场演示、核心链路真实、非核心能力允许简化
```

那么 30 天计划具有可行性，但风险较高，需要严格控制范围。

如果目标是：

```text
生产可部署、多租户、完整安全、稳定调度、全量云资源覆盖、
真实 Azure Policy + AWS Config、完善监控告警和高可用
```

那么 30 天明显不可行。此类目标通常需要多人和数月建设。

## 3. 项目设计中合理的部分

### 3.1 先 Azure 后 AWS

Day 1～20 先打通 Azure，再在 Day 21～30 接入 AWS，这个顺序正确。双云同时
起步会让认证、账单维度、资源模型、错误处理和测试问题一起出现，很难判断问题
来自公共设计还是具体 Provider。

### 3.2 先数据底座后 Dashboard

先完成资源和成本 ETL，再建设前端，避免 Dashboard 依赖假数据和频繁变化的
接口。Day 1～7 已经证明这种顺序有效。

### 3.3 POC 后正式化

资源和成本都采用了“先验证云 API，再加入数据库、幂等性、执行历史和失败
记录”的两阶段方式。它让外部 API 风险与内部工程风险分开，是合理的迭代方法。

### 3.4 统一数据表

Azure 和未来 AWS 共用 `cloud_resources`、`cloud_cost_daily` 和统一 finding
模型的方向合理。`provider` 和 `account_id` 作为归一化维度，有利于统一查询
和 Dashboard 筛选。

### 3.5 真实闭环优先

当前脚本不是只检查“命令返回 0”，而是验证了：

- Azure 资源真实存在后再销毁；
- 数据库记录数量和唯一性；
- Worker 与 API 两个入口；
- 失败状态是否持久化；
- 成本聚合 API 总额是否一致；
- 临时资源和测试数据库是否清理。

这种验收方式应继续保持，它是当前项目最有价值的工程特征之一。

## 4. 当前设计需要调整的部分

## 4.1 Provider Adapter 不应等到 Day 19

原计划在 Day 19 才正式引入 `ICloudProviderAdapter`。这一天太晚，因为在此
之前已经要实现：

- 成本归因；
- 标签合规；
- Compliance API；
- Dashboard provider filter；
- finding 和异常检测。

如果这些能力继续直接使用 Azure 概念，Day 19 会变成大规模重构。

当前已经有 Provider 无关的小接口，因此不需要马上增加一个过大的总接口。
建议从 Day 8 开始完成以下整理：

- 所有应用服务显式接受或返回 `provider`；
- 避免服务名、Job 名和日志写死 Azure；
- DI 支持按 Provider 选择多个实现；
- Dashboard 和查询接口从第一版就保留 Provider 筛选；
- 保持资源、成本、合规三个小接口，不强制合并成“万能 Adapter”。

结论：Adapter 思想应前置，但不建议为了形式增加一个承担所有职责的巨型接口。

## 4.2 Day 8 的成本归因能力被当前成本粒度限制

当前成本数据按以下维度存储：

```text
日期 + 服务 + Resource Group + 币种
```

它可以回答哪个服务、哪个 Resource Group 花费较高，但不能直接精确回答：

- 单个资源花了多少钱；
- 某个 `cost-center` 标签产生了多少成本；
- 缺少标签的资源对应多少“无法归属成本”。

因为资源标签来自 Resource Graph，而成本表目前没有资源 ID 和标签维度。

Day 8 必须先定义成本归因的准确边界：

1. 第一版只做 Resource Group 级归因；
2. 用资源清单统计每个 Resource Group 的标签覆盖情况；
3. 将“成本无法归属”定义为该 Resource Group 缺少统一归属标签，而不是伪造
   单资源成本；
4. 如果 Azure Cost API 能稳定返回 ResourceId 或 Tag 维度，再增加更细粒度
   数据表或 attribution 表。

报告和 Dashboard 必须区分“真实账单维度”和“推导归因结果”。

## 4.3 资源删除和失效状态尚未建模

当前资源同步会更新已发现资源的 `LastSeenAt`，但 Azure 中已经删除的资源仍会
留在数据库。后续合规率和资源数量会因此失真。

建议在 Day 8～9 增加：

- `is_active` 或 `lifecycle_status`；
- `last_seen_at` 与本次同步时间比较；
- 只有一次完整成功扫描后，才能把未出现资源标记为 inactive；
- 查询接口默认只统计 active 资源；
- 保留历史记录，而不是物理删除。

这是标签合规和双云资源统计开始前必须处理的数据语义。

## 4.4 API 和 Worker 的组织方式需要在功能增长前拆分

当前 Minimal API 全部集中在 `Program.cs`，Day 1～7 规模下可以接受。Day 11
之后将加入 Compliance、Findings、Events、Anomalies 和导出接口，继续堆在
一个文件中会降低可读性和测试性。

建议按功能拆分 endpoint registration：

```text
Endpoints/
├── CostEndpoints.cs
├── ResourceEndpoints.cs
├── ComplianceEndpoints.cs
├── EtlEndpoints.cs
└── EventEndpoints.cs
```

Worker 也应从 `if Resources / else Costs` 逐步改为任务处理器注册表，避免每加
一个任务就修改主 Worker。

## 4.5 管理接口目前没有认证授权

以下接口可以触发真实云 API 和数据库写入：

```text
POST /api/admin/sync/azure/resources
POST /api/admin/sync/azure/costs
```

本地学习阶段可以接受，但不能把它描述为生产安全设计。若应用会暴露到公网，
Azure MVP 冻结前至少需要：

- 明确只绑定本地地址，或增加简单认证；
- 区分只读 API 与管理 API；
- 限制并发同步；
- 避免两个请求同时执行同一 ETL；
- 对输入范围设置上限。

完整 RBAC 可以后补，但风险必须在文档中明确。

## 4.6 自动迁移适合学习环境，不适合直接照搬到生产

API 和 Worker 启动时都会自动执行 migration。对当前端到端学习项目很方便，
但多实例生产环境可能发生并发迁移或权限过大。

建议 Day 18 Docker 整理时增加独立 migration 命令，并在文档中区分：

- 本地开发：允许宿主启动时迁移；
- CI/CD 或生产：部署前单独执行迁移。

## 4.7 ETL 还缺少调度和并发控制

现有 ETL 是一次性 Worker 或手工 API 触发，尚未实现：

- 定时调度；
- 同一 Job 的互斥执行；
- 超时与取消策略；
- 分页断点；
- 大批量写入；
- 指标和结构化追踪。

作品集 MVP 不必全部实现，但至少应在 Day 17 前加入数据库锁或 Job 互斥，
避免重复触发造成并发 Upsert 冲突。

## 4.8 `outline.md` 和 30 天计划存在范围冲突

项目大纲还包含：

- Python；
- Prometheus 和 Grafana；
- CPU、内存、网络指标；
- PVC、负载均衡等闲置检测；
- distroless 镜像治理；
- Azure Policy 和 AWS Config 的完整机制。

而 30 天计划明确使用 C# 完成异常检测，并把 Kubernetes 深度治理、复杂 RBAC
等内容列为后补。

必须以 `construction-plan.md` 的 MVP 边界为准。否则项目会同时变成 FinOps、
CMDB、CSPM、监控平台、容器治理平台和消息平台，30 天一定失控。

建议把 Prometheus/Grafana、深度闲置检测和镜像治理定义为第二阶段，不纳入
Day 1～30 的完成承诺。

## 5. 当前项目表述需要更准确

README 当前第一句称项目“基于 .NET 10、Terraform、PostgreSQL 和 React
构建”，但仓库尚未创建 React 前端；AWS 也尚未接入。

在 Day 13 和 Day 24 完成前，建议区分：

```text
项目目标：多云 FinOps 与资源治理平台。
当前阶段：已完成 Azure 资源和成本数据底座。
```

面试和简历只能陈述已经能够现场演示的能力。未来规划可以说明，但不能把计划中
的 Azure Policy、AWS Config、Prometheus 或双云 Dashboard 写成已完成事实。

## 6. 工期可行性分析

## 6.1 不同投入方式的结论

| 投入方式 | 30 天可行性 | 结论 |
| --- | --- | --- |
| 每天 6～8 小时，全职开发 | 中等偏高 | 严格限制为作品集 MVP 时可行 |
| 每天 3～4 小时，稳定兼职 | 中等偏低 | 建议延长到 40～45 天 |
| 每天 1～2 小时，边学边做 | 低 | 建议按 8～12 周安排 |
| 生产级交付标准 | 不可行 | 需要多人和数月建设 |

Day 1～7 已经完成真实闭环，降低了数据库、Azure 认证和 ETL 的基础风险。
但剩余 23 天的功能种类更多，不能简单认为完成 7 天就等于完成了 23%：

- 前 7 天主要建立一条 Azure 数据链路；
- 后 13 天需要合规、前端、算法、消息系统和产品化；
- 最后 10 天还要处理 AWS 的认证、账单延迟和服务差异。

后半段的上下文切换和集成复杂度明显更高。

## 6.2 Azure Day 8～20

Azure MVP 在 13 天内完成是可能的，但需要满足：

- 标签合规优先使用本地规则引擎；
- Azure Policy 接入失败时立即使用 Policy-style 保底；
- Dashboard 只做必要页面和图表，不追求复杂设计系统；
- 异常检测只实现可解释的 3σ 基线；
- Service Bus 只做一个事件类型的可靠闭环；
- 不在此阶段加入 Prometheus、Grafana 和 Kubernetes 治理；
- Day 20 只修复阻塞演示的问题，不继续增加功能。

高风险日是 Day 13～17。五天内同时完成 React、过滤器、异常算法、Service Bus
生产消费和失败处理，几乎没有缓冲。任何一个外部依赖问题都会挤压 Day 20。

## 6.3 AWS Day 21～30

十天接入 AWS 的技术方向可行，但前提是 AWS 范围很小：

- 资源盘点优先 S3、EC2、EBS、EIP；
- 使用 Resource Groups Tagging API 加少量服务专用 API；
- 成本只做 daily + service；
- 合规优先复用标签规则；
- AWS Config 使用 Config-style 本地规则作为保底；
- 不承诺覆盖所有 AWS 资源类型。

AWS Cost Explorer 可能需要提前启用，并存在成本数据延迟。IAM 权限、区域差异、
全局资源和 Tagging API 覆盖不完整也会消耗时间。因此 AWS 账号、预算和 Cost
Explorer 必须在 Day 21 之前完成预检，不能等到当天才发现不可用。

## 6.4 推荐工期

最稳妥的建议是：

```text
30 天：完成严格范围的可演示 MVP。
36～40 天：完成更稳定的作品集版本，并保留测试和修复缓冲。
```

如果必须坚持 30 天，应删除或降级以下内容：

- 真实 Azure Policy 接入；
- 真实 AWS Config 接入；
- Prometheus/Grafana；
- 深度闲置资源检测；
- 容器镜像治理专项；
- 复杂认证和 RBAC；
- 多种消息事件和通知渠道；
- 高级前端视觉效果。

## 7. 推荐的 Day 8～30 调整版关键路径

### Day 8：数据语义和 Provider 前置整理

- Provider 参数贯穿应用服务和查询；
- 资源 active/inactive 生命周期；
- 定义 Resource Group 级成本归因边界；
- 为后续筛选补充数据库索引。

### Day 9：成本归因

- Resource Group、Service、Environment、Cost Center 视图；
- 明确真实维度与推导结果；
- 无法归属原因分类。

### Day 10～12：合规闭环

- Day 10：标签规则引擎和 finding 数据模型；
- Day 11：Compliance / Finding API；
- Day 12：整改报告和 Policy-style finding。

真实 Azure Policy 作为加分项，不阻塞主线。

### Day 13～15：Dashboard

- Day 13：React 工程、API client、Overview；
- Day 14：Cost、Resources、Compliance 页面；
- Day 15：过滤器、加载/错误状态和基础前端测试。

将原 Day 15 的异常检测顺延一天，给前端完整三天。

### Day 16～18：异常与事件

- Day 16：3σ 异常检测和可重复样例；
- Day 17：Service Bus 发布、消费和数据库状态；
- Day 18：重试、失败记录、互斥执行和 dead-letter 说明。

### Day 19：运行与安全整理

- Dockerfile 和 Compose；
- 独立 migration 方式；
- 管理接口暴露边界；
- 配置和密钥说明；
- 一键启动路径。

Provider 抽象不再集中到 Day 19，因为相关整理已从 Day 8 开始持续完成。

### Day 20：Azure MVP 冻结

- 只做完整彩排、修复、清理和证据保存；
- 禁止新增功能；
- 固化 Azure 演示脚本。

### Day 21～24：AWS 数据链路

- Day 21：账号、权限、Cost Explorer 预检和 Terraform；
- Day 22：AWS SDK 身份验证和基础资源读取；
- Day 23：资源 ETL；
- Day 24：成本 ETL。

### Day 25～28：统一治理

- Day 25：双云查询和 Dashboard 对比；
- Day 26：复用标签合规；
- Day 27：AWS Config-style finding；
- Day 28：双云异常进入同一事件管道。

### Day 29～30：冻结与表达

- Day 29：架构图、数据流、运行文档和演示材料；
- Day 30：完整彩排、简历 bullet、面试讲稿和最终清理。

## 8. 必须坚持的工程原则

后续开发建议继续执行以下标准：

1. 每天交付必须包含代码、自动化测试、真实手工验收和清理。
2. 外部云 API 必须准备明确标记的 fallback，但不能把样例伪装成真实数据。
3. Azure/AWS SDK 类型不能泄漏到 Application 和 API 契约。
4. 数据幂等性必须由数据库唯一约束兜底，不能只依赖内存判断。
5. finding、event 和 ETL 都必须有可追踪状态，失败不能只存在日志中。
6. 临时云资源必须在验收结束后销毁。
7. 文档必须区分“已经实现”“保底实现”和“未来规划”。
8. Day 20 和 Day 30 必须冻结功能，给演示稳定性留出时间。

## 9. 建议的完成标准

### 30 天 MVP 可以承诺

- Azure 和 AWS 少量核心资源的 Terraform 生命周期；
- 双云资源与成本 ETL；
- 统一 PostgreSQL 模型；
- 标签合规和 Policy/Config-style finding；
- 基础成本归因；
- React 统一 Dashboard；
- 3σ 成本异常；
- Azure Service Bus 单一告警闭环；
- 整改报告；
- 可重复演示脚本和测试。

### 30 天内不应承诺

- 企业级多租户；
- 完整 RBAC；
- 所有 Azure/AWS 资源类型；
- 完整 Azure Policy 和 AWS Config 覆盖；
- 精确单资源成本分摊；
- 大规模性能和高可用；
- Kubernetes 深度治理；
- Prometheus/Grafana 完整观测平台；
- 自动执行资源删除或成本整改。

## 10. 最终判断

项目的业务故事、技术选型和当前实现总体合理，值得继续建设。Day 1～7 已经
完成了最重要的真实性验证：云资源、资源清单、成本数据、数据库和应用代码能够
形成实际闭环。

原 30 天计划的问题不在方向，而在范围和顺序：

- Provider 思想需要提前；
- 成本归因必须尊重现有数据粒度；
- 资源生命周期必须在合规前补齐；
- 前端至少需要三天连续投入；
- AWS 账号和 Cost Explorer 必须提前预检；
- 生产级能力必须明确排除在 30 天 MVP 之外。

在每天 6～8 小时、严格执行范围控制并保留 fallback 的情况下，30 天可以完成
一套有说服力的多云 FinOps 作品集 MVP。若希望代码、测试、前端体验和演示稳定
性都达到更从容的水平，建议把总工期调整为 36～40 天。
