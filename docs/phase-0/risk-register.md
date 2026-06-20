# 阶段 0 风险登记册

## 1. 文档定位

- 登记日期：2026 年 6 月 14 日
- 事实基线：Day 8～10 永久文档与 Day 9 运行证据
- 当前状态：`ReadyForReview`
- 适用范围：当前 local 开发基线及后续生产化施工

严重度使用 Day 11 施工手册中的 Impact × Likelihood 矩阵。Owner 是责任角色；
单人项目中可以由同一人承担多个角色，但关闭风险时必须以对应角色作出结论。

## 2. 风险登记

| ID | 标题 | 类别 | 事实证据 | 触发场景 | 影响 | Likelihood | Impact | Severity | Owner | Treatment | 目标阶段 | 验证方式 | 状态 | 接受期限 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| RISK-0001 | 匿名管理与查询 API | Security | Day 25 已接入 OIDC JWT Bearer 验证和负向测试，但现有业务 endpoint 尚未绑定授权 policy；GAP-001 | API 被非本机或非预期主体访问 | 成本泄漏、云枚举、未授权 ETL 和写库 | Medium | Critical | Critical | Security Owner | Avoid | 阶段 2、8 | Day 27～30 RBAC、API policy、审计和匿名拒绝 E2E | Open | N/A |
| RISK-0002 | 无业务 tenant 隔离 | Security/Data | Day 23 已隔离新读写；Day 24 backfill 建立受控迁移和数据库护栏；Day 25 已验证 OIDC `iss/sub` 到 Membership/TenantContext 的真实请求管道；环境执行证据、RBAC、全 endpoint 授权和 RLS 仍未完成；GAP-002 | 引入第二组织或账号边界 | 跨组织数据越界和错误操作 | Low | Critical | High | Platform Architect | Avoid | 阶段 2～3 | tenant escape、IDOR、环境 backfill、RBAC、RLS 与后台上下文测试 | Mitigated | Day 26～30 完成剩余控制 |
| RISK-0003 | API/Worker 自动 migration | Reliability/Operations | Day 18 新增独立 `FinOps.Migrator`、同库 advisory lock 和可重复数据库回归脚本；API/Worker 无 migration API；无 DDL runtime role 启动通过 | 发布遗漏 Migrator、目标配置错误或 migration 本身失败 | schema 未升级或错误目标被修改，导致业务宿主失败 | Low | High | Medium | Platform SRE | Mitigate | 阶段 12 | CI/CD 显式先运行 Migrator，绑定并确认环境目标，失败阻断部署；生产身份隔离和回滚演练 | Mitigated | `Test-DatabaseMigration.ps1` 覆盖空库、幂等、并发拒绝、失败退出码和受限权限 |
| RISK-0004 | ETL 无可靠调度 | Operations | 仅手工 POST 或一次性 Worker；GAP-006 | 需要持续采集和 freshness SLO | 数据过期、漏跑且依赖人工发现 | High | Medium | High | Application Owner | Mitigate | 阶段 4 | Scheduler E2E、漏跑告警、freshness 指标 | Open | N/A |
| RISK-0005 | ETL 无租约和并发互斥 | Reliability | 无 lease/heartbeat；GAP-006 | API 与 Worker 并发触发同一 scope | 重复调用、竞争写入、费用与状态混乱 | Medium | High | High | Application Owner | Mitigate | 阶段 4 | 并发触发测试只允许一个 active run | Open | N/A |
| RISK-0006 | 成本 sample fallback 掩盖真实故障 | Data/Cost | appsettings 默认 fallback=true；GAP-004 | 权限、空账单或 Provider 故障 | 样例数据被误当财务事实 | Medium | Critical | Critical | FinOps Product Owner | Avoid | 阶段 5～6 | 生产配置无法启用 sample，provenance 断言和部署门禁 | Open | N/A |
| RISK-0007 | 资源删除和失活语义缺失 | Data | 仅 First/LastSeen；GAP-007 | 云资源被删除或移出 scope | 清单把历史资源当作当前资源 | High | High | Critical | Data Owner | Mitigate | 阶段 3、5 | full-scan、scan ID、inactive/deleted E2E | Open | N/A |
| RISK-0008 | 成本粒度和账单语义有限 | Data/Cost | 仅日、服务、资源组、币种；GAP-008/019 | 用于精确归因、预算或对账 | 财务结论失真或无法追溯修订 | High | High | Critical | FinOps Product Owner | Mitigate | 阶段 3、6 | 账单语义 ADR、lineage、重算与 reconciliation | Open | N/A |
| RISK-0009 | 无 outbox/inbox 可靠事件协议 | Reliability | 当前无事件发布消费；GAP-014 | 后续引入治理事件和异步处理 | 数据提交与消息状态不一致、重复消费 | Medium | High | High | Platform Architect | Mitigate | 阶段 10 | 故障注入下的 outbox/inbox 幂等测试 | Open | N/A |
| RISK-0010 | 无统一 telemetry、告警和 SLO | Operations | 只有日志与 health；GAP-011 | Provider 变慢、ETL 卡住或数据过期 | 故障发现晚、无法量化可靠性 | High | High | Critical | Platform SRE | Mitigate | 阶段 11 | trace/metric/log 联动、告警与 SLO 演练 | Open | N/A |
| RISK-0011 | 无备份、PITR 和恢复演练 | Reliability/Data | Compose 单卷；GAP-013 | 数据损坏、误删或数据库故障 | 无法恢复生产数据 | Medium | Critical | Critical | Platform SRE | Avoid | Release A、阶段 15 | PITR 恢复演练达到批准的 RPO/RTO | Open | N/A |
| RISK-0012 | 无 staging 和制品晋级链 | Delivery | 当前只有 local；GAP-012 | 直接向生产发布 | 环境差异、回滚与验收不可证明 | High | High | Critical | Platform SRE | Mitigate | Release A、阶段 12 | 同一制品 development→staging 晋级与回滚 | Open | N/A |
| RISK-0013 | 无 CI/CD 自动门禁 | Delivery | Day 19 新增 GitHub Actions 静态与数据库门禁、PR 模板和 CODEOWNERS；GAP-012/022 | branch protection 未启用或后续部署绕过 CI | 缺陷、secret 或架构违规进入主干 | Low | High | Medium | Application Owner | Mitigate | 阶段 1、12 | `Static verification` 与 `Database migration` 在干净 runner 通过；启用 required checks | Mitigated | Day 19 GitHub Actions 运行证据 |
| RISK-0014 | Terraform 使用本地 state | Operations/Security | 无 backend；GAP-009 | 团队或环境变更 | 无锁、审计、恢复和环境隔离 | High | High | Critical | Cloud Provider Owner | Avoid | 阶段 12 | 加密 remote state、locking、身份和恢复测试 | Open | N/A |
| RISK-0015 | Azure E2E 产生真实费用 | Cost | Day 9 创建 Storage/Service Bus 等资源 | 重复测试或资源规格扩大 | 超预算或不可预期费用 | Medium | Medium | Medium | FinOps Product Owner | Mitigate | 持续治理 | 预算、资源 TTL、费用告警与测试配额 | Open | N/A |
| RISK-0016 | E2E 异常遗留 Azure 资源 | Cost/Operations | 脚本依赖 finally/destroy；Day 9 清理通过 | 进程中断、权限变化或 destroy 失败 | 持续费用和资源污染 | Medium | High | High | Cloud Provider Owner | Mitigate | 阶段 1、12 | 独立遗留扫描、TTL 清理与失败演练 | Open | N/A |
| RISK-0017 | 默认开发口令、开放 Host 和数据库端口 | Security | appsettings/compose、`AllowedHosts=*`；GAP-010 | 配置被用于共享或公网环境 | 未授权数据库/API 访问 | Medium | High | High | Security Owner | Avoid | 阶段 1～2、12 | 环境启动校验、secret store、网络拒绝测试 | Open | N/A |
| RISK-0018 | Azure CLI 用户身份作为运行身份 | Security/Operations | `DefaultAzureCredential` 使用本地 CLI；GAP-005 | 用户会话过期、订阅切换或权限过大 | 采集失败、越权和不可审计身份使用 | High | High | Critical | Security Owner | Avoid | 阶段 2、5 | workload identity、最小 RBAC 与轮换演练 | Open | N/A |
| RISK-0019 | Provider 超时、重试和错误分类不统一 | Reliability | Cost 固定 timeout；无统一策略；GAP-017 | 限流、暂时故障或部分失败 | 雪崩重试、漏数或错误降级 | High | High | Critical | Cloud Provider Owner | Mitigate | 阶段 4～7 | 限流/超时/永久错误故障注入测试 | Open | N/A |
| RISK-0020 | 无数据 retention 与删除策略 | Data/Compliance | 三表和证据无保留期；GAP-021 | 数据长期增长、租户删除或合规请求 | 过度保留、无法证明删除 | High | High | Critical | Data Owner | Mitigate | 阶段 3 | retention policy、删除作业和审计证据 | Open | N/A |
| RISK-0021 | 错误、日志和 raw JSON 可能泄漏敏感元数据 | Security/Data | 保存 Provider 原始 JSON 和错误摘要 | SDK/HTTP 错误含 scope、资源或组织信息 | 内部/机密数据进入日志和导出 | Medium | High | High | Security Owner | Mitigate | 阶段 3、8、11 | 日志脱敏、payload allowlist 与泄漏测试 | Open | N/A |
| RISK-0022 | 无自动 secret、依赖、容器和 IaC 门禁 | Security/Delivery | Day 19 CI 已运行候选文件 secret 模式、NuGet vulnerability、actionlint 和 Terraform 静态检查；GAP-022 | 历史 secret、恶意依赖、容器或制品风险未被现有模式覆盖 | 供应链风险和 secret 泄漏未被及时阻断 | Medium | High | High | Security Owner | Mitigate | 阶段 14 | 历史 scanner、SBOM、license、container、IaC 和 provenance 门禁 | Mitigated | Day 19 CI 静态门禁证据 |
| RISK-0023 | xUnit v2 元包已标记 Legacy | Delivery | NuGet deprecated 查询命中 `xunit 2.9.3` | SDK/runner 演进或后续升级 | 测试栈维护成本和兼容风险 | Medium | Medium | Medium | Application Owner | Mitigate | 阶段 1 | xUnit v3 迁移，全部测试与 CI 通过 | Open | N/A |
| RISK-0024 | PostgreSQL 镜像未在 Compose 固定 digest | Security/Delivery | `postgres:18-alpine`，本机解析到一个 digest | tag 后续指向新镜像 | 本地环境不可完全复现，供应链变化未审查 | Medium | Medium | Medium | Platform SRE | Mitigate | 阶段 12、14 | 生产镜像 digest、签名和漏洞扫描门禁 | Open | N/A |
| RISK-0025 | 单 PostgreSQL 实例是数据与审计单点 | Reliability | Compose 单容器单卷 | PostgreSQL 停止或磁盘损坏 | API/ETL 不可用，Failed 状态也可能无法保存 | Medium | Critical | Critical | Platform SRE | Mitigate | Release A、阶段 12、15 | HA、故障转移、备份恢复和应用降级演练 | Open | N/A |
| RISK-0026 | API 错误契约与限流未生产化 | Security/Reliability | Minimal API 集中，失败可为通用 500；GAP-016 | 参数错误、Provider 错误或高频调用 | 客户端误判、内部信息暴露、资源耗尽 | High | Medium | High | Application Owner | Mitigate | 阶段 8 | 稳定 Problem Details、错误码、分页和限流测试 | Open | N/A |
| RISK-0027 | 依赖和工具链版本漂移未受控 | Delivery/Security | Day 19 CI 固定 .NET/Terraform/actionlint 和外部 Action SHA，并报告 NuGet vulnerable/deprecated/outdated | 报告未自动创建升级 PR，部分依赖仍有可用更新 | 漏掉安全/兼容修复，或升级时引入未经验证的行为变化 | Medium | Medium | Medium | Application Owner / Platform SRE | Mitigate | 阶段 14 | 聚焦升级 PR、回归测试、Dependabot/Renovate 和 SBOM/SCA | Mitigated | ADR-0018 与 Day 19 CI 证据 |

## 3. 当前最高风险

当前 Critical 风险不表示阶段 0 必须立即实现全部生产控制，而表示后续施工不得
绕过它们。阶段 1 优先处理工程门禁和 migration；阶段 2～3 优先处理身份、
tenant 与数据边界；生产发布前必须关闭备份、staging、state、可观测性和单点。

任何风险降级、关闭或接受都必须附新的测试或运行证据。`Accepted` 必须由有权
Owner 给出期限；本初稿没有把任何生产风险标记为 Accepted。
