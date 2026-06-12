项目二：多云 FinOps 成本治理与资源合规管控平台
 
项目定位
 
证明 .NET后端工程能力 + 云资源治理SRE能力 + 事件驱动架构经验，是ASB、Azure SRE、云平台治理岗位强力加分项目。
承接项目一的基础设施交付，完成资源交付后的成本管控、闲置治理、合规巡检、异常告警闭环。
 
核心技术栈
 
.NET WebAPI、React+TypeScript、Python、PostgreSQL、Azure Cost Management、AWS Cost Explorer、Azure Policy、AWS Config、Prometheus、Grafana、Azure Service Bus
 
核心工程落地
 
P0 核心底座：多云账单ETL与统一可视化
 
- 对接Azure Cost Management API、AWS Cost Explorer API拉取全量账单数据
- 结合Azure Resource Graph、AWS Resource Explorer完成双云资源盘点
- 自研ETL归一化数据模型，统一双云成本与资源数据结构入库PostgreSQL
- 开发前后端分离可视化平台，实现成本环比分析、服务占比、资源清单、合规率大盘
 
P0 多云合规治理体系
 
- 基于Azure Policy(Push评估)、AWS Config(Pull轮询评估)搭建双云合规扫描体系
- 自动扫描资源标签合规、权限合规、网络合规，输出标准化风险整改报告
- 精通双云合规引擎机制差异、修复策略、生产落地限制
 
P1 成本标签治理与闲置资源检测
 
- 强制管控cost-center、environment、owner核心业务标签，实现成本分账、归属治理
- 自研可配置闲置资源规则引擎，基于CPU、内存、网络流量多维度指标，检测长期低负载节点、闲置PVC、孤立公网IP、废弃负载均衡
- 基于Prometheus、云监控指标交叉验证，输出安全可控的资源优化清单
 
P2 智能成本异常检测 & 事件驱动告警
 
- 基于3σ滑动窗口统计学算法实现智能成本异常识别，优于传统静态阈值告警
- 对接Azure Service Bus实现事件驱动异步告警，完成异常检测→消息投递→业务通知闭环
- 结合底层高并发消息架构认知，对比交易级低延迟队列与云原生可靠消息队列的场景差异
 
P2 容器镜像轻量化治理
 
- 基于多阶段构建、distroless极简镜像完成业务镜像瘦身优化
- 通过dive、docker history量化优化收益，形成容器交付层成本与稳定性优化方案
 