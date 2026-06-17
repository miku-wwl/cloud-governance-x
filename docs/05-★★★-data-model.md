# 05 ★★★ Data Model

## `cloud_resources`

Day 4 introduces the normalized cloud resource inventory table. Azure Resource
Graph data is mapped into this model before persistence, so later AWS inventory
can reuse the same schema.

| Column | PostgreSQL type | Purpose |
| --- | --- | --- |
| `id` | `uuid` | Internal primary key |
| `provider` | `varchar(32)` | Cloud provider, currently `Azure` |
| `account_id` | `varchar(128)` | Azure subscription ID |
| `resource_id` | `varchar(2048)` | Original cloud resource ID |
| `resource_id_normalized` | `varchar(2048)` | Case-normalized identity key |
| `resource_name` | `varchar(512)` | Display name |
| `resource_type` | `varchar(512)` | Provider resource type |
| `region` | `varchar(128)` | Azure location |
| `resource_group` | `varchar(256)` | Azure resource group |
| `tags_json` | `jsonb` | Provider tags |
| `first_seen_at` | `timestamptz` | First successful inventory observation |
| `last_seen_at` | `timestamptz` | Most recent inventory observation |

The unique index on `(provider, resource_id_normalized)` is the database-level
idempotency guarantee. Azure resource IDs are case-insensitive, so the original
ID is retained for display while a normalized value is used for matching.

## Migration

EF Core migrations live under:

```text
src/FinOps.Infrastructure/Persistence/Migrations/
```

Restore the repository-local EF tool and inspect migrations:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations list `
  --project src/FinOps.Infrastructure `
  --startup-project src/FinOps.Infrastructure
```

The API and Worker currently apply pending migrations during startup. The Worker
does this before either resource or cost synchronization. This startup migration
pattern is convenient for the local baseline, but it is explicitly listed as a
production gap because production should use a separate, controlled migration
step.

## `etl_job_runs`

Day 5 records the lifecycle of every formal ETL execution. The Worker and the
manual API trigger write through the same application service.

| Column | PostgreSQL type | Purpose |
| --- | --- | --- |
| `id` | `uuid` | Job run identifier |
| `job_name` | `varchar(128)` | Stable ETL name, such as `azure-resource-sync` |
| `provider` | `varchar(32)` | Cloud provider |
| `started_at` | `timestamptz` | Execution start time |
| `finished_at` | `timestamptz` | Terminal completion time |
| `status` | `varchar(32)` | `Running`, `Succeeded`, or `Failed` |
| `records_processed` | `integer` | Resource rows handled by the run |
| `error_message` | `varchar(4000)` | Failure summary without stack trace |

An index on `(job_name, started_at desc)` supports recent execution history.
ETL run updates use a separate `DbContext` so an inventory persistence failure
does not prevent the failure status from being recorded.

## `cloud_cost_daily`

Day 6 stores Azure Cost Management daily aggregates grouped by service and
resource group. `raw_json.source` distinguishes real and fallback rows.

| Column | PostgreSQL type | Purpose |
| --- | --- | --- |
| `id` | `uuid` | Internal primary key |
| `provider` | `varchar(32)` | Cloud provider |
| `account_id` | `varchar(128)` | Azure subscription ID |
| `usage_date` | `date` | Cost usage date |
| `service_name` | `varchar(256)` | Azure service dimension |
| `resource_group` | `varchar(256)` | Resource group or `(unassigned)` |
| `cost` | `numeric(20,8)` | Aggregated pretax cost |
| `currency` | `varchar(16)` | Billing currency |
| `raw_json` | `jsonb` | Normalized row and provenance |

The unique identity is `(provider, account_id, usage_date, service_name,
resource_group, currency)`, so repeated queries update existing aggregates.
