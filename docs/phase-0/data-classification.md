# 数据分类与处理要求

## 1. 分类模型

| 等级 | 定义 | 最低处理要求 |
| --- | --- | --- |
| Public | 可公开且泄漏无明显损害 | 保证完整性，发布前 review |
| Internal | 仅项目或团队内部 | 受控仓库/系统访问，不公开索引 |
| Confidential | 泄漏会暴露组织、资产、财务或运行信息 | 最小权限、传输加密、审计、受控导出 |
| Restricted | 可直接授予访问或造成重大损害 | secret store、短期凭据、禁止普通日志/Git、轮换 |

公开仓库中的源码和文档按 Public 管理，但其中不能包含 Internal 以上的真实环境
值。Azure tenant ID 不是本项目的业务 tenant，也不能作为业务隔离边界。

## 2. 当前与规划数据

| 数据 | 等级 | Owner | 来源 | 当前存储 | 传输 | 当前日志行为 | Git 风险 | 访问主体 | Retention | 备份要求 | 导出要求 | 删除要求 | 目标生产控制 | 当前差距 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 源码与公共文档 | Public | Application Owner | 开发者 | Git/GitHub | SSH/HTTPS | N/A | 可能误提交敏感值 | 仓库读者 | 永久历史 | Git 远端 | 发布 review | 按 Git 策略 | branch protection、签名与扫描 | 当前无 CI 门禁 |
| 非敏感配置结构 | Internal | Platform Architect | appsettings/compose/Terraform | Git | Git、文件系统 | 键名可能出现 | 值被误填为真实配置 | 开发者 | 跟随代码 | Git 远端 | 禁止包含环境 secret | 删除真实值并轮换 | schema 校验、环境分层 | 仅开发默认值 |
| Azure subscription/tenant ID | Confidential | Cloud Provider Owner | Azure ARM/CLI | 内存、日志/证据可能出现 | HTTPS | 当前脚本可能输出 ID | 不得进入永久公共报告 | 应用身份、开发者 | 待定 | 按运行证据策略 | 必须脱敏或授权 | 账号 offboard 后清理 | tenant/account registry、日志脱敏 | 无正式 retention |
| 资源名称、ID、区域、资源组和 tags | Confidential | Data Owner | Resource Graph | `cloud_resources` | HTTPS、Npgsql | ETL 数量为主，错误可能含 ID | 测试证据不得提交完整 payload | API/Worker、DB 用户 | 待定 | 生产需要 PITR | tenant scope、审计、水印 | inactive 后按策略删除 | 加密、RLS、lineage、retention | 无 tenant 和删除语义 |
| 成本金额、币种与维度 | Confidential | FinOps Product Owner | Cost Management | `cloud_cost_daily` | HTTPS、Npgsql | 数量和错误；不应记录明细 | 报告不得提交真实明细 | API/Worker、DB 用户 | 待定，需覆盖账期修订 | 生产需要 PITR | 授权、审计、用途限制 | 依法规和账期策略 | RLS、lineage、reconciliation | 无 tenant、retention |
| Cost `raw_json` | Confidential | Data Owner | Cost Provider | PostgreSQL jsonb | HTTPS、Npgsql | 当前不主动完整记录 | dump/导出泄漏风险 | 应用与 DB 用户 | 应短于或等于财务保留策略 | 与成本表一致 | 默认禁止原样导出 | 按 lineage/法规删除 | allowlist、schema version、加密 | 原始维度边界未定义 |
| ETL 运行状态与错误 | Confidential | Platform SRE | 应用与 Provider | `etl_job_runs`、控制台日志 | Npgsql、stdout | 保存最长 4000 字符错误摘要 | 日志/临时报告可能误提交 | 运维与应用 | 待定 | 生产审计需备份 | 脱敏后受控导出 | 到期清理 | 结构化错误码、脱敏、审计 | 可能含 scope/SDK 详情 |
| 本地数据库开发口令 | Internal，仅限无复用示例 | Security Owner | Git 中固定示例 | appsettings/compose/.env.example | 本机 TCP | 不应记录 connection string | 已跟踪但明确为不可复用示例 | 本地开发者 | 随环境 | 无 | 禁止导出 | 环境销毁时删除 | 生产改 Restricted + secret store | 无环境启动硬门禁 |
| 生产数据库密码/连接凭据 | Restricted | Security Owner | 未来 secret store | 当前不存在 | TLS | 严禁记录 | 严禁进入 Git | workload identity/受限应用 | 按轮换策略 | secret store HA | 禁止导出 | 轮换并撤销 | 短期凭据或托管身份 | 尚未实现 |
| Azure access token/服务凭据 | Restricted | Security Owner | Azure Identity | 当前只在进程内存/CLI cache | HTTPS | 严禁记录 token | 严禁进入 Git、tmp 和报告 | Azure CLI 用户、未来 workload | token 生命周期 | 不备份 token | 禁止导出 | 到期/撤销/轮换 | workload identity、最小 RBAC | 当前依赖开发者 CLI |
| Terraform state/plan | Confidential；含 secret 时 Restricted | Cloud Provider Owner | Terraform | 当前本地文件，运行后清理 | 文件系统、Azure HTTPS | 命令可能显示资源属性 | `.gitignore` 已排除 | Terraform 执行者 | 环境生命周期 + 审计策略 | 生产需加密备份 | 仅受控运维访问 | 环境销毁后按政策保留/删除 | remote state、locking、审计 | 本地 state，无集中恢复 |
| `tmp/`、TEMP 与 E2E 日志 | Confidential | Platform SRE | 验收脚本 | 本地文件系统 | OS 文件 I/O | 可能含资源/订阅 ID | `tmp/` 被 Git 忽略 | 本机执行者 | 当前人工清理，生产待定 | 不作为唯一永久证据 | 脱敏摘要进入 docs | 验收后清理原始敏感输出 | 集中证据库、TTL、脱敏 | 无自动 TTL |
| 审计日志 | Confidential | Security Owner | 未来 API/Job/运维操作 | 当前尚不存在 | 未来 TLS | 应记录 actor/action/target，不记 secret | 禁止提交原始生产审计 | 安全与授权运维 | 法规与调查周期待定 | 不可篡改备份 | 受控调查导出 | 依法保留/删除 | 不可篡改、查询审计、tenant scope | 尚未实现 |
| 导出文件 | Confidential | Data Owner | 未来查询/导出 Job | 当前尚不存在 | 未来 HTTPS/对象存储 | 只记元数据 | 禁止进入 Git | 授权用户 | 短 TTL | 通常不长期备份 | 授权、过期、水印、审计 | 到期自动删除 | tenant scope、短期 URL、审计 | 尚未实现 |

## 3. 当前处理规则

1. 永久文档只记录结论、命令和脱敏摘要，不复制 token、完整连接字符串、真实
   成本明细或完整 Azure payload。
2. `tmp/` 是本地证据区，不是安全存储；Git 忽略不能替代访问控制和保留策略。
3. 本地固定数据库口令只能作为无复用的开发示例。任何共享、staging 或生产环境
   中的同值凭据均视为配置错误。
4. Terraform state、数据库备份和导出必须按其包含的最高等级数据分类。
5. 在阶段 3 完成数据分层和 tenancy ADR 前，所有 retention 数值保持“待决”，
   不虚构合规期限。

## 4. 关闭条件

阶段 3 应为每类持久数据批准 retention、删除、lineage 和 tenant 规则。Release A
发布前必须证明 Confidential 数据最小权限、加密、审计和备份恢复；Restricted
数据必须由 secret/workload identity 机制管理并完成轮换演练。
