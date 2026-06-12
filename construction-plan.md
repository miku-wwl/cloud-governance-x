---

# 总体技术栈

```text
Backend API：ASP.NET Core Web API / .NET 10
Worker：.NET 10 Worker Service
ORM：Entity Framework Core
Database：PostgreSQL
Frontend：React + TypeScript
IaC：Terraform
Queue：Azure Service Bus
Cloud APIs：
  - Azure Cost Management
  - Azure Resource Graph
  - Azure Policy / Policy-style Compliance
  - AWS Cost Explorer
  - AWS Resource Explorer / Tagging API
  - AWS Config / Config-style Compliance
```

项目定位改成：

```text
基于 .NET 10、Terraform、PostgreSQL 和 React 构建的多云 FinOps 与资源治理平台。
```

---

# 项目目录建议

```text
multi-cloud-finops-platform/
├── src/
│   ├── FinOps.Api/              # .NET 10 WebAPI
│   ├── FinOps.Domain/           # 核心实体
│   ├── FinOps.Application/      # Use Cases / Interfaces
│   ├── FinOps.Infrastructure/   # Azure/AWS SDK、EF Core、Service Bus
│   ├── FinOps.Worker/           # ETL Worker / Alert Worker
│   └── FinOps.Tests/
│
├── frontend/
│   └── finops-dashboard/        # React + TypeScript
│
├── terraform/
│   ├── azure/
│   └── aws/
│
├── database/
│   └── migrations/
│
└── docs/
    ├── architecture.md
    ├── data-model.md
    ├── azure-integration.md
    ├── aws-integration.md
    ├── terraform.md
    ├── demo-script.md
    └── interview-notes.md
```

`.csproj` 统一目标：

```xml
<TargetFramework>net10.0</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```

---

# 30 天总工期规划

## Day 1～20：Azure-only 完整闭环

目标是先把 Azure 打穿，不要一开始双云一起乱。

---

## Day 1：.NET 10 项目骨架 + 基础架构设计

### 目标

搭好 .NET 10 后端骨架，不写复杂业务。

### 交付

```text
- .NET 10 WebAPI
- .NET 10 Worker Service
- Clean Architecture 基础目录
- PostgreSQL Docker Compose
- Health Check API
- README v0.1
```

### 命令示例

```bash
dotnet new sln -n FinOpsPlatform

dotnet new webapi -n FinOps.Api -f net10.0
dotnet new classlib -n FinOps.Domain -f net10.0
dotnet new classlib -n FinOps.Application -f net10.0
dotnet new classlib -n FinOps.Infrastructure -f net10.0
dotnet new worker -n FinOps.Worker -f net10.0
dotnet new xunit -n FinOps.Tests -f net10.0
```

### 当天验收

```text
dotnet build 成功
API 能启动
Worker 能启动
PostgreSQL 能连接
```

---

## Day 2：Terraform Azure 基础设施

### 目标

Terraform 从第一天就纳入项目，不要后补。

### Azure 创建

```text
Resource Group
Storage Account
Service Bus Namespace
Service Bus Queue
Log Analytics Workspace，可选
Demo Tags
```

### Terraform 目录

```text
terraform/azure/
├── providers.tf
├── main.tf
├── variables.tf
├── outputs.tf
└── terraform.tfvars.example
```

### 验收

```bash
terraform init
terraform plan
terraform apply
az resource list
terraform destroy
```

这一天重点不是资源多，而是证明你会用 Terraform 管理云资源生命周期。

---

## Day 3：.NET 10 接 Azure SDK + 认证

### 目标

后端能连接 Azure。

### 推荐认证方式

本地开发先用：

```text
Azure CLI login + DefaultAzureCredential
```

后续可以再扩展：

```text
Service Principal
Managed Identity
```

### .NET 接口设计

```csharp
public interface ICloudResourceInventoryProvider
{
    Task<IReadOnlyList<CloudResourceDto>> GetResourcesAsync();
}

public interface ICloudCostProvider
{
    Task<IReadOnlyList<CloudCostDailyDto>> GetDailyCostsAsync(DateOnly from, DateOnly to);
}

public interface ICloudComplianceProvider
{
    Task<IReadOnlyList<ComplianceFindingDto>> GetComplianceFindingsAsync();
}
```

### 验收

```text
.NET 10 能读取 Azure subscription 信息
Infrastructure 层有 Azure Provider 初版
API 层不直接调用 Azure SDK
```

---

## Day 4：Azure Resource Graph POC

### 目标

拉 Azure 资源清单。

### 查询方向

```kusto
Resources
| project id, name, type, location, resourceGroup, tags
```

### 数据库表

```text
cloud_resources
```

字段：

```text
provider
account_id
resource_id
resource_name
resource_type
region
resource_group
tags_json
first_seen_at
last_seen_at
```

### 验收

```text
.NET Worker 能拉 Azure resources
能写入 PostgreSQL
重复运行不会重复插入
```

---

## Day 5：Azure Resource ETL 正式化

### 目标

把 POC 变成正式 ETL Job。

### 增加能力

```text
Upsert
LastSeenAt 更新
ETL Job Run 记录
错误日志
records_processed 统计
```

### 新表

```text
etl_job_runs
```

字段：

```text
job_name
provider
started_at
finished_at
status
records_processed
error_message
```

### 验收

```text
可以手动触发 Azure Resource Sync
可以看到 ETL 执行历史
失败时有错误记录
```

---

## Day 6：Azure Cost Management POC

### 目标

拉 Azure 成本数据。

### 第一版只做

```text
最近 7 天 daily cost
按 Service 聚合
按 Resource Group 聚合
```

### 表

```text
cloud_cost_daily
```

字段：

```text
provider
account_id
usage_date
service_name
resource_group
cost
currency
raw_json
```

### 注意

Azure 成本数据可能有延迟。学生订阅也可能遇到数据粒度限制。所以你要准备 sample data fallback，别因为当天成本数据为空就卡死。

---

## Day 7：Azure Cost ETL 正式化

### 目标

成本数据进入正式 ETL。

### API

```text
POST /api/admin/sync/azure/costs
GET /api/costs/daily
GET /api/costs/by-service
GET /api/costs/by-resource-group
```

### 验收

```text
成本数据能入库
重复执行不会重复插入
API 能返回成本趋势
API 能返回服务成本占比
```

---

## Day 8：Azure 成本 + 资源关联

### 目标

做第一版成本归因。

不要一开始追求精确到单个资源，先做：

```text
Resource Group 维度
Service Name 维度
Tag 维度
Environment 维度
Cost Center 维度
```

### 验收问题

你要能回答：

```text
哪个 Resource Group 花钱最多？
哪个 Service 花钱最多？
哪些资源缺少 cost-center，导致成本无法归属？
```

这一天开始，项目真正有 FinOps 味道。

---

## Day 9：标签合规规则引擎

### 目标

先做自研规则，不依赖 Azure Policy 卡进度。

### 强制标签

```text
owner
environment
cost-center
```

### 输出表

```text
tag_compliance_findings
```

字段：

```text
provider
resource_id
resource_name
missing_tags
severity
status
recommendation
created_at
```

### 验收

```text
能扫描所有 Azure 资源
能找出缺失标签资源
能生成整改建议
```

---

## Day 10：Azure Policy 接入或 Policy-style 模拟

### 目标

体现你理解 Azure Policy，但不被它拖死。

### 两条路线

优先：

```text
读取 Azure Policy compliance state
```

保底：

```text
用自研规则引擎模拟 Policy-style compliance finding
```

### 验收

```text
Tag Finding 和 Policy Finding 分开
Dashboard 能展示 Policy-style 风险项
```

面试时可以说：

> 第一版实现了本地 compliance evaluator，同时预留 Azure Policy Provider 接口，后续可以直接接 Policy Insights。

这个说法很稳。

---

## Day 11：Compliance API

### 目标

让前端能展示合规数据。

### API

```text
GET /api/compliance/summary
GET /api/compliance/tags
GET /api/compliance/policies
GET /api/findings/open
```

### Dashboard 指标

```text
总资源数
合规资源数
不合规资源数
标签合规率
高风险 finding 数量
```

---

## Day 12：整改报告生成

### 目标

从“看板”升级为“治理平台”。

### 报告格式

```text
Resource
Provider
Violation
Severity
Recommendation
Owner
Status
```

### 输出

```text
docs/demo-remediation-report.md
```

### 验收

```text
能导出一份 Markdown 整改报告
报告内容来自真实 finding 数据
```

---

## Day 13：React Dashboard v1

### 页面

```text
Overview
Cost
Resources
Compliance
Findings
ETL Runs
```

### 图表

```text
Daily Cost Trend
Cost by Service
Resource Count by Type
Tag Compliance Rate
Open Findings
```

### 验收

```text
前端能调用 .NET 10 API
能展示真实数据库数据
```

---

## Day 14：Dashboard 过滤器

### 增加筛选

```text
provider
resource group
service name
environment
cost-center
finding status
```

### 验收

```text
能筛 Azure 资源
能筛缺失标签资源
能按 environment 看成本
```

---

## Day 15：.NET 10 实现 3σ 成本异常检测

### 目标

不要引入 Python，直接用 C# 实现。

### 规则

```text
取最近 N 天成本
计算 mean
计算 stddev
如果 today_cost > mean + 3 * stddev
生成 anomaly finding
```

### 表

```text
cost_anomaly_findings
```

字段：

```text
provider
scope
service_name
usage_date
actual_cost
baseline_mean
baseline_stddev
threshold
severity
status
```

### 验收

```text
能用 sample data 制造异常
能生成 anomaly finding
```

不要叫 AI，叫：

```text
statistical anomaly detection
```

---

## Day 16：Azure Service Bus 告警闭环

### 目标

异常 finding 投递到 Service Bus。

### 流程

```text
Anomaly Detector
    ↓
Governance Event
    ↓
Azure Service Bus Queue
    ↓
.NET 10 Worker Consumer
    ↓
Processed / Failed
```

### 表

```text
governance_events
```

字段：

```text
event_type
provider
severity
payload_json
status
created_at
processed_at
```

### 验收

```text
.NET 10 能 publish 到 Service Bus
.NET 10 Worker 能 consume
消费结果能回写数据库
```

---

## Day 17：Worker 错误处理 + Dead-letter 思维

### 目标

让告警系统像生产系统。

### 增加

```text
retry count
failed status
dead-letter queue 设计说明
worker logs
event processing history
```

### 验收

```text
失败事件不会静默丢失
Dashboard 能看到事件状态
Worker 日志清楚
```

---

## Day 18：Docker / 本地运行整理

### 目标

让项目更像完整工程。

### 建议

```text
docker-compose.yml
PostgreSQL container
Backend container，可选
Frontend container，可选
```

### 注意

.NET 10 镜像要用对应版本，例如：

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
FROM mcr.microsoft.com/dotnet/sdk:10.0
```

### 验收

```text
本地可以一键启动数据库
README 里有完整启动步骤
```

---

## Day 19：Provider Adapter 抽象

这是 AWS 扩展前最关键的一天。

### 目标

把 Azure-only 代码改成 provider-agnostic。

### 抽象接口

```csharp
public interface ICloudProviderAdapter
{
    string ProviderName { get; }

    Task<IReadOnlyList<CloudResourceDto>> GetResourcesAsync();

    Task<IReadOnlyList<CloudCostDailyDto>> GetDailyCostsAsync(
        DateOnly from,
        DateOnly to);

    Task<IReadOnlyList<ComplianceFindingDto>> GetComplianceFindingsAsync();
}
```

### Azure 实现

```text
AzureCloudProviderAdapter
AzureResourceInventoryProvider
AzureCostProvider
AzureComplianceProvider
```

### 验收

```text
Application 层不依赖 Azure SDK
数据库支持 provider = Azure / AWS
Dashboard 支持 provider filter
```

这一天做不好，后面 AWS 会变成硬拼代码。

---

## Day 20：Azure MVP Demo 冻结

### 目标

冻结 Azure 版本。

### 必须能演示

```text
Terraform 创建 Azure 资源
.NET 10 Worker 拉 Azure 资源
.NET 10 Worker 拉 Azure 成本
Dashboard 展示成本趋势
Dashboard 展示资源清单
Dashboard 展示标签合规
3σ 检测生成异常
Service Bus 完成告警闭环
整改报告生成
```

Day 20 结束，你已经有一个完整 Azure FinOps 项目。

---

# Day 21～30：AWS 接入统一治理平台

这 10 天不是重做一遍 AWS 平台，而是把 AWS 接到同一套 .NET 10 Provider Adapter 里。

---

## Day 21：Terraform AWS 基础资源

### 创建少量资源

```text
S3 Bucket
EC2，可选
EBS，可选
EIP，可选
IAM Policy / Role
统一 Tags
```

### Terraform 目录

```text
terraform/aws/
├── providers.tf
├── main.tf
├── variables.tf
├── outputs.tf
└── terraform.tfvars.example
```

### 验收

```text
terraform apply 成功
AWS Console 能看到资源
资源带 owner/environment/cost-center tags
terraform destroy 能清理
```

---

## Day 22：.NET 10 接 AWS SDK

### 新增实现

```text
AwsCloudProviderAdapter
AwsResourceInventoryProvider
AwsCostProvider
AwsComplianceProvider
```

### 先验证

```text
GetCallerIdentity
List S3 Buckets
List tagged resources
```

### 验收

```text
.NET 10 能连接 AWS
能读取至少一类 AWS 资源
AWS 资源能进入 cloud_resources
```

---

## Day 23：AWS 资源盘点 ETL

### 优先资源类型

```text
S3
EC2
EBS
EIP
Load Balancer，可选
RDS，可选
```

### 入库

继续用同一张表：

```text
cloud_resources
```

### 验收

```text
Dashboard 资源列表里同时出现 Azure 和 AWS
Provider filter 可用
```

---

## Day 24：AWS Cost Explorer ETL

### 目标

AWS 成本进入同一张成本表。

### 第一版

```text
按天成本
按 Service 聚合
按 Account / Region 聚合，可选
按 Tag 聚合，可选
```

### 入库

```text
cloud_cost_daily
```

### 验收

```text
cloud_cost_daily 里同时有 Azure 和 AWS
Cost Dashboard 可以筛 provider
```

---

## Day 25：Azure vs AWS 成本和资源对比

### Dashboard 增加

```text
Azure vs AWS Daily Cost
Azure vs AWS Cost by Service
Azure vs AWS Resource Count
Azure vs AWS Tag Compliance Rate
```

### 验收

你要能回答：

```text
Azure 和 AWS 哪边资源更多？
哪边成本更高？
哪边标签合规率更低？
哪些资源无法成本归属？
```

---

## Day 26：AWS 标签合规复用

### 目标

同一套标签规则跑 Azure + AWS。

### 规则仍然是

```text
owner
environment
cost-center
```

### 验收

```text
同一个 Tag Compliance Dashboard 显示双云结果
同一套规则引擎输出 Azure/AWS findings
```

这就是项目的核心价值：

```text
不是两套平台，而是一套统一治理规则。
```

---

## Day 27：AWS Config-style 合规

### 理想版

接 AWS Config compliance result。

### 保底版

自定义 AWS Config-style 规则：

```text
S3 bucket should not be public
EBS volume should be attached
EIP should be associated
EC2 should have required tags
```

### 验收

```text
policy_compliance_findings 表支持 Azure Policy-style 和 AWS Config-style finding
```

---

## Day 28：多云异常检测 + Service Bus 告警

### 目标

Azure 和 AWS 成本异常都进入同一个告警管道。

### 流程

```text
Azure Cost Data ─┐
                 ├── .NET 10 Anomaly Detector ── Governance Event ── Service Bus
AWS Cost Data ───┘
```

### 验收

```text
Azure 异常能生成事件
AWS 异常能生成事件
同一个 .NET Worker 消费两边事件
Dashboard 显示 provider 字段
```

---

## Day 29：文档、架构图、演示材料

### 必须整理

```text
README.md
docs/architecture.md
docs/data-model.md
docs/terraform.md
docs/azure-integration.md
docs/aws-integration.md
docs/provider-adapter.md
docs/finops-rules.md
docs/demo-script.md
docs/interview-notes.md
```

### 架构图至少三张

```text
System Architecture
ETL Data Flow
Provider Adapter Design
```

---

## Day 30：简历 bullet + 面试讲稿 + Demo Rehearsal

### 英文简历 bullet

```text
Built a multi-cloud FinOps and governance platform using .NET 10, Terraform, PostgreSQL and React, integrating Azure Cost Management, Azure Resource Graph, Azure Policy-style compliance checks, AWS Cost Explorer, AWS resource inventory APIs and AWS Config-style findings into a unified provider-agnostic governance model.

Implemented .NET 10 WebAPI and Worker services for automated Azure/AWS cost and resource ETL, normalizing cloud-specific billing and inventory data into PostgreSQL and exposing dashboards for cost trends, service-level spend, resource inventory, tag compliance and remediation findings.

Designed a reusable governance rule engine to detect missing cost allocation tags, idle resource risks and policy violations across Azure and AWS, producing standardized findings with severity, ownership and remediation recommendations.

Implemented statistical cost anomaly detection using a sliding-window 3σ algorithm and an event-driven alerting pipeline with Azure Service Bus and .NET 10 Worker services, supporting asynchronous alert processing, retry handling and operational visibility.
```

### 中文面试讲法

```text
这个项目不是简单成本看板，而是一个多云治理平面。我用 Terraform 创建 Azure 和 AWS 的测试资源，用 .NET 10 WebAPI 和 Worker Service 对接 Azure Cost Management、Azure Resource Graph、AWS Cost Explorer 和资源盘点 API，把双云成本、资源和合规数据归一化进入 PostgreSQL。上层通过统一规则引擎做标签合规、成本归因、异常检测和整改建议，最后用 Azure Service Bus 实现事件驱动告警闭环。重点是 Azure 和 AWS 不是两套孤立系统，而是通过 Provider Adapter 接入同一套治理模型。
```

---

# .NET 10 版本下的功能优先级

## 必须完成

```text
.NET 10 WebAPI
.NET 10 Worker Service
PostgreSQL
React Dashboard
Terraform Azure
Terraform AWS
Azure Resource Inventory
Azure Cost ETL
Azure Tag Compliance
Azure Service Bus Alert
AWS Resource Inventory
AWS Cost ETL
AWS Tag Compliance
Provider Adapter
统一 Dashboard
```

## 尽量完成

```text
Azure Policy compliance
AWS Config compliance
3σ cost anomaly detection
remediation report
ETL job history
retry / dead-letter 设计
Dockerfile
基础测试
```

## 可以后补

```text
AKS 深度治理
EKS 深度治理
PVC 闲置检测
Pod requests/limits 浪费分析
镜像瘦身治理
Teams / Email 真通知
多租户
复杂 RBAC
```

---

# 最终 30 天版本一句话

```text
Day 1～20：用 .NET 10 + Terraform 打穿 Azure FinOps 治理闭环；
Day 21～30：通过 Provider Adapter 接入 AWS 成本、资源和合规数据；
最终形成一个 .NET 10 驱动的多云 FinOps 与资源合规治理平台。
```
