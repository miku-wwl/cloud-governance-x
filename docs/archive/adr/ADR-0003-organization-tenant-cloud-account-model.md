# ADR-0003: Organization、Tenant、CloudAccount 与范围模型

## 状态

Accepted

## 日期

2026-06-19

## Owner

- 决策 Owner：Platform Architect
- Reviewer：Security Owner、Data Owner、Application Owner、Platform SRE

## 背景

Cloud Governance X 当前保存 Azure resource、cost 和 ETL 数据时，没有业务 tenant 边界。
Azure SDK 对象会暴露 Azure tenant ID，但该标识描述的是 Microsoft Entra directory，
不是平台的客户、组织或数据隔离边界。

Phase 2 必须先建立稳定模型，再添加 schema、trusted tenant context、OIDC、RBAC 和 audit。
该决策必须支持：

- 一个 organization 下有一个或多个隔离 workspace；
- Azure subscriptions 和未来 AWS accounts；
- 多个 Provider credential 或 workload identity；
- human 和 service identity；
- tenant、account 和未来 resource-level authorization scope；
- 无法从 ambient HTTP state 推断 tenant 的后台 job；
- 明确受控的 platform-administrator 路径；
- 未来 RLS 或更强物理隔离，不改变业务 identity。

本决策关联 RISK-0001、RISK-0002、RISK-0018，以及 GAP-001、GAP-002、GAP-005。
它本身不关闭这些风险；实现和隔离测试仍需持续到 Day 30。

## 决策

### 业务层级

标准层级为：

```text
Organization
    └── Tenant
          ├── Membership
          ├── ProviderConnection
          └── CloudAccount
                └── 更细授权 scope
```

所有内部标识符都是 opaque UUID。显示名称和外部 Provider 标识只是属性，不能作为授权 key。

### Organization

`Organization` 表示拥有一个或多个 platform tenant 的客户、法人或管理实体。

- 一个 Organization 可以包含多个 Tenant。
- Organization 不是常规 row-level data isolation key。
- 加入某个 Tenant 不会隐含获得 organization-wide access。
- 初始部署可以只有一个 Organization 和一个 Tenant，但代码和 schema 不能特殊处理这个基数。

最小计划属性：

- `id`
- `display_name`
- `status`
- `created_at`
- `updated_at`

### Tenant

`Tenant` 是主要 security、authorization 和 data-isolation boundary。
每个属于客户 workspace 的 operational 或 business row 最终都必须直接携带 `tenant_id`，
或通过 tenant-bound aggregate 和 composite foreign key 间接携带。

- 一个 Tenant 只属于一个 Organization。
- Tenant 不能包含另一个 Tenant。
- Tenant identity 绝不能来自 query string、route value、任意 header、Azure tenant ID 或 cloud account ID。
- Suspension 拒绝新的 business operation，同时保留数据用于 audit 和受控恢复。
- Decommissioning 是显式生命周期，不是 hard cascade delete。

最小计划属性：

- `id`
- `organization_id`
- `slug`
- `display_name`
- `status`
- `created_at`
- `updated_at`

tenant slug 在 Organization 内唯一。授权和 join 使用 UUID，不使用 slug。

### CloudAccount

`CloudAccount` 是平台归一化后的 Provider account 边界：

- Azure：一个 Subscription；
- AWS：一个 AWS Account；
- 未来 Provider：等价的 billable/resource ownership account。

Azure tenant/directory 是 Provider metadata，可以记录为 `provider_directory_id`，但它不是 `tenant_id`。

CloudAccount 必须：

- 同一时刻只属于一个业务 Tenant；
- 拥有 Provider 和不可变的 normalized external account ID；
- 引用用于访问它的 ProviderConnection；
- 携带 onboarding 和 operational status；
- 不包含 credential；
- 不能被静默转移给另一个 Tenant。

账号在 Tenant 间移动需要未来显式 offboard/transfer workflow，并具备授权、审计和数据 reconciliation。
Day 21 不得把 transfer 实现为不受限制的 `tenant_id` update。

最小计划属性：

- `id`
- `tenant_id`
- `provider`
- `external_account_id`
- `provider_directory_id`
- `provider_connection_id`
- `display_name`
- `status`
- `environment`
- `created_at`
- `updated_at`

活动 identity 由 `(provider, external_account_id)` 全局唯一标识。每个 tenant-owned foreign key
和 uniqueness rule 也必须保留 tenant boundary。

### ProviderConnection

`ProviderConnection` 表示访问某个 Provider 所需的配置和 identity binding。

- 它只属于一个 Tenant。
- 它可以服务该 Tenant 下的一个或多个 CloudAccount。
- 它保存非 secret metadata、capability state 和 secret/workload identity reference。
- 它绝不保存 access token、client secret 或 raw credential。
- CloudAccount 不能引用另一个 Tenant 的 ProviderConnection。

最小计划属性：

- `id`
- `tenant_id`
- `provider`
- `display_name`
- `credential_reference`
- `status`
- `last_validated_at`
- `created_at`
- `updated_at`

`credential_reference` 是未来 secret store 或 workload identity 配置的不透明定位符。
本地 Azure CLI identity 仍是 development adapter，不能变成持久化 tenant identity。

### Membership 和 identity subject

`Membership` 授予外部 identity subject 对某个 Tenant 的访问关联。

稳定外部 subject key 是：

```text
issuer + subject
```

Email、display name 和 Entra object display attribute 都是可变 profile data，不能作为 authorization key。

Membership 支持 human 和 service subject。它只建立 tenant association 和 lifecycle；
Day 27 将定义 role 和 permission assignment。一个 user 可以拥有多个 Tenant 的 Membership，
但每个 request 或 job 只针对一个显式 effective Tenant 执行。

最小计划属性：

- `id`
- `tenant_id`
- `issuer`
- `subject`
- `subject_type`
- `display_name`
- `status`
- `created_at`
- `updated_at`

活动 identity 由 `(tenant_id, issuer, subject)` 唯一标识。

### 范围模型

授权范围是带类型的引用，不是任意 string prefix。

初始层级为：

```text
Tenant
    └── CloudAccount
          └── Provider-specific resource scope（未来）
```

标准 scope 表示包含：

- `scope_type`
- `tenant_id`
- 可选 `scope_id`

规则：

- 每个非 平台范围 都包含 `tenant_id`；
- CloudAccount scope 必须解析到同一 Tenant 中的 account；
- 更窄 scope 不能授予父级之外的访问；
- Provider-native resource ID 只是 target 或 metadata，不能单独作为可信 scope；
- wildcard scope 不能用 null tenant 表示；
- 缺少 trusted scope 是 authorization failure。

Day 27 可以在不改变 tenant identity model 的前提下，把 permission assignment 加到这些 typed scope 上。

### 可信 identity 与 tenant selection

human HTTP request 从已验证 OIDC token 获取 identity。effective tenant 必须从 authenticated subject 的 active
Membership 中选择，并在服务端验证。

客户端只能用 opaque tenant identifier 请求选择 tenant；服务端必须验证 membership 或 platform-level authority。
route、query、header 或 body 中的值绝不能自行产生 authority。

background work 没有 ambient user identity。每个 job definition/message 必须携带服务端创建的 tenant 和 account
scope。Worker 在解析 Repository 或 Provider 前，必须拒绝缺失或不一致的 tenant context。

### Platform administrator 路径

cross-tenant platform administration 是单独的 platform-level grant，不是 Tenant Membership，也不是隐式 wildcard。

每个 cross-tenant operation 必须：

1. 认证 platform subject；
2. 要求专用 platform permission；
3. 指定一个明确 target Tenant；
4. 记录 reason、correlation ID 和 operation result；
5. 对该 target 仍通过正常 tenant-bound Repository 和 Provider 检查；
6. 产生 追加式审计证据。

常规数据查询不得使用 platform-wide scope。未来引入 break-glass access 时，需要单独 approval、短时长和 audit。

### 隔离策略

初始实现使用单 PostgreSQL database 和 shared schema，并显式携带 `tenant_id`。

强制不变量：

- tenant-owned primary lookup path 包含 `tenant_id`；
- tenant-owned unique index 包含 `tenant_id`，除非有文档说明的全局 Provider identity 需要额外全局约束；
- tenant-owned relationship 使用 composite key 或等价 validation，防止 cross-tenant reference；
- Application 和 Repository contract 要求 tenant context；
- cache key、job message、object path 和 audit record 包含 tenant；
- 生产执行中不存在 default 或 empty Tenant。

PostgreSQL Row Level Security 推迟到 ADR-0005，作为 defense in depth。RLS 不能替代显式 Application 和 Repository tenant boundary。

### 生命周期

计划的生命周期状态刻意保持很小：

| Aggregate | States |
| --- | --- |
| Organization | Active, Suspended, Decommissioning |
| Tenant | Active, Suspended, Decommissioning |
| CloudAccount | Pending, Active, Suspended, Disconnected |
| ProviderConnection | Pending, Active, Degraded, Revoked |
| Membership | Invited, Active, Suspended, Revoked |

状态迁移由对应 Day 实现和测试。hard delete 不是 tenant-owned operational data 的常规生命周期操作。

## 考虑过的替代方案

### 把 Azure tenant 当作业务 Tenant

Rejected。一个业务客户可能使用多个 Entra directory，一个 directory 也可能包含属于不同业务 workspace 的 subscription。
该方案也不支持 AWS。

### 只用 CloudAccount 作为 tenant boundary

Rejected。Membership、organization-wide policy、audit、shared cost allocation 和 multi-account view 都需要 Provider account
之上的稳定业务 workspace。

### 每个 Tenant 一个 schema

Deferred。在 domain model 稳定前，这会增加 migration、connection 和运维复杂度。逻辑模型必须保持与该选项兼容，以便未来支持更强隔离层级。

### 每个 Tenant 一个 database

Deferred。它提供更强隔离，但会增加 provisioning、migration、backup 和 cross-tenant operation 成本。未来可为大型或受监管客户引入，而不改变公开业务标识。

### 直接把 role 存在 Membership 上

Deferred to Day 27。单个 role column 无法表达 platform permission、account scope、未来 custom role 或独立 service permission。

### 信任 `X-Tenant-Id` header

Rejected。client-controlled tenant context 会直接形成 tenant-escape 路径。任何 tenant selector 都只是请求使用服务端已经验证过的 authority。

## 后果

收益：

- 业务 tenancy 与 Provider 解耦；
- Azure 和 AWS account 共享一个稳定模型；
- tenant escape 有明确 schema、Application 和 authorization control；
- OIDC、background jobs、RBAC 和 audit 共享 subject/scope 语言；
- 未来仍可引入更强隔离层级。

成本和义务：

- 每个现有 core table 和 Repository 都必须 tenant-aware；
- Day 24 必须把当前数据 backfill 到受控 development Tenant；
- account onboarding 和 transfer 需要显式 workflow；
- cross-tenant support operation 需要专用 audit 和 authorization；
- shared-schema isolation 需要大量 negative testing 和后续 RLS 评估。

新风险：

- 所有 repository 迁移完成前，遗漏 tenant predicate 可能暴露数据；
- 错误 account-to-connection validation 可能串用 credential 或 scope；
- platform administrator privilege 如果不保持隔离和审计，可能变成 ambient bypass。

## 实施挂钩

- Day 21：
  - 新增 Organization、Tenant、CloudAccount、ProviderConnection 和 Membership Domain models；
  - 新增 EF configuration 和 expand-only migration；
  - 新增 tenant-aware composite key 和 relationship tests。
- Day 22：
  - 新增可信 HTTP 和后台 `TenantContext`；
  - 拒绝缺失、伪造和不一致的 tenant selection。
- Day 23：
  - 所有 Repository contract 和 query 都要求 tenant；
  - 新增 tenant A/B isolation 和 IDOR tests。
- Day 24：
  - 创建一个明确的 development Organization/Tenant；
  - backfill 现有行，不删除或静默重分类数据。
- Day 25-28：
  - 将 OIDC subject 绑定到 Membership；
  - 实现 typed 权限/范围检查；
  - 保护每个 endpoint。
- Day 29：
  - 为 membership、connection、account 和 cross-tenant administration 增加 追加式审计。
- Day 30：
  - 执行 tenant escape、RBAC、service identity 和 audit E2E。

长期文档影响：

- `docs/archive/phase-0/adr-backlog.md`
- `docs/archive/phase-0/risk-register.md`
- `docs/archive/phase-0/production-gap-register.md`
- `docs/days/day-20.md`

## 验证

Day 20 验证：

- 六个必需概念都有明确 ownership 和 cardinality；
- Azure tenant 和业务 Tenant 已区分；
- human、service 和 background identity source 已识别；
- account 和 tenant scope rule 可执行；
- platform administration 是显式、target-bound 且可审计的；
- 未来实现和 negative tests 已映射到 Day 21-30。

后续必需 negative tests：

- 没有 Membership 的 client-supplied tenant 被拒绝；
- 缺失 TenantContext 时 fail closed；
- tenant A 不能引用 tenant B 的 CloudAccount 或 ProviderConnection；
- 重复 Provider account onboarding 被拒绝；
- background job 没有 tenant/account scope 时不能启动；
- 普通 Tenant administrator 不能使用 平台范围；
- platform administrator 必须指定并审计一个 target Tenant；
- 不能创建 cross-tenant foreign key 或 unique identity；
- suspended/revoked Membership 和 ProviderConnection 不能被使用。

## 重新审视触发条件

以下情况需要 review 或替代本 ADR：

- 需要 schema-per-tenant 或 database-per-tenant；
- 客户需要 region/data-residency isolation；
- 实现 CloudAccount 在 Tenant 间 transfer；
- 某个 Provider 无法自然映射到 account model；
- delegated administration 需要 organization-level policy inheritance；
- PostgreSQL RLS 设计改变 enforcement model；
- platform administration 变成独立 service 或 control plane。
