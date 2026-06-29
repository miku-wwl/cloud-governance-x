# 05 ★★★ 数据模型

## `cloud_resources`

Day 4 引入归一化 cloud resource inventory 表。Azure Resource Graph 数据在持久化前映射到该模型，使后续 AWS inventory 可以复用同一 schema。

| Column | PostgreSQL type | 用途 |
| --- | --- | --- |
| `id` | `uuid` | 内部主键 |
| `provider` | `varchar(32)` | 云 Provider，当前为 `Azure` |
| `account_id` | `varchar(128)` | Azure subscription ID |
| `resource_id` | `varchar(2048)` | 原始云资源 ID |
| `resource_id_normalized` | `varchar(2048)` | 大小写归一化 identity key |
| `resource_name` | `varchar(512)` | 显示名称 |
| `resource_type` | `varchar(512)` | Provider resource type |
| `region` | `varchar(128)` | Azure location |
| `resource_group` | `varchar(256)` | Azure resource group |
| `tags_json` | `jsonb` | Provider tags |
| `first_seen_at` | `timestamptz` | 第一次成功 inventory observation |
| `last_seen_at` | `timestamptz` | 最近一次 inventory observation |

`(provider, resource_id_normalized)` 上的唯一索引是数据库层面的幂等保证。Azure resource ID 大小写不敏感，因此保留原始 ID 用于显示，同时使用归一化值进行匹配。

## Migration

EF Core migration 位于：

```text
src/FinOps.Infrastructure/Persistence/Migrations/
```

恢复仓库本地 EF tool 并查看 migration：

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations list `
  --project src/FinOps.Infrastructure `
  --startup-project src/FinOps.Infrastructure
```

专用 `FinOps.Migrator` 会在 API 或 Worker startup 前应用 pending migration。API 和 Worker 不再修改 schema。本地和 release workflow 必须显式运行 migrator；生产环境可以只把 DDL 权限授予 migration identity。Migrator 还会为目标数据库获取 PostgreSQL advisory lock；如果另一个 FinOps migrator 已持有该锁，本次执行会失败。

## `etl_job_runs`

Day 5 记录每次正式 ETL 执行的生命周期。Worker 和手工 API trigger 通过同一个 application service 写入。

| Column | PostgreSQL type | 用途 |
| --- | --- | --- |
| `id` | `uuid` | Job run identifier |
| `job_name` | `varchar(128)` | 稳定 ETL 名称，例如 `azure-resource-sync` |
| `provider` | `varchar(32)` | 云 Provider |
| `started_at` | `timestamptz` | 执行开始时间 |
| `finished_at` | `timestamptz` | 终止完成时间 |
| `status` | `varchar(32)` | `Running`、`Succeeded` 或 `Failed` |
| `records_processed` | `integer` | 本次处理的 resource row 数量 |
| `error_message` | `varchar(4000)` | 不含 stack trace 的失败摘要 |

`(job_name, started_at desc)` 上的索引用于查询最近执行历史。ETL run update 使用独立 `DbContext`，因此 inventory persistence failure 不会阻止失败状态被记录。

## `cloud_cost_daily`

Day 6 保存 Azure Cost Management 日聚合，按 service 和 resource group 分组。`raw_json.source` 区分真实行和 fallback 行。

| Column | PostgreSQL type | 用途 |
| --- | --- | --- |
| `id` | `uuid` | 内部主键 |
| `provider` | `varchar(32)` | 云 Provider |
| `account_id` | `varchar(128)` | Azure subscription ID |
| `usage_date` | `date` | cost usage date |
| `service_name` | `varchar(256)` | Azure service dimension |
| `resource_group` | `varchar(256)` | Resource group 或 `(unassigned)` |
| `cost` | `numeric(20,8)` | 聚合 pretax cost |
| `currency` | `varchar(16)` | billing currency |
| `raw_json` | `jsonb` | 归一化行和 provenance |

唯一 identity 为 `(provider, account_id, usage_date, service_name, resource_group, currency)`，因此重复查询会更新已有聚合。
