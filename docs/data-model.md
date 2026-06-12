# Data Model

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

The Worker applies pending migrations before resource synchronization.
