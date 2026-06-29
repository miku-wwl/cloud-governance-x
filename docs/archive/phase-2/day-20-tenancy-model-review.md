# Day 20 租户模型评审

日期：2026-06-19
Phase：2 - 身份、租户、RBAC 与审计
决策：ADR-0003 Accepted
实施范围：只做设计，不修改 Domain、EF 或 migration。

## 1. 结论

Day 20 建立 Day 21-30 所需的业务和安全词汇。

已接受模型：

```text
Organization
    └── Tenant（主要安全和数据边界）
          ├── Membership（issuer + subject）
          ├── ProviderConnection（credential reference，不保存 credential）
          └── CloudAccount（Azure Subscription / AWS Account）
                └── typed narrower scopes
```

Azure tenant/directory ID 是 Provider metadata，不是 Cloud Governance X 的业务 Tenant，绝不能作为 `tenant_id`。

## 2. 模型评审

| 概念 | 所属关系 | 安全含义 | 基数 | 关键不变量 |
| --- | --- | --- | --- | --- |
| Organization | 顶层客户或管理实体 | 不隐含 row-level access | 1:N Tenants | 某个 Tenant 的 membership 不授予 Organization-wide access |
| Tenant | 属于一个 Organization | 主要授权和数据隔离边界 | 1:N accounts/connections/memberships | 每个 tenant-owned operation 都有一个 trusted tenant |
| CloudAccount | 属于一个 Tenant 和一个 ProviderConnection | 归一化 Provider account 范围 | 同一时刻一个 active Tenant | Azure Subscription/AWS Account 不能静默转移 |
| ProviderConnection | 属于一个 Tenant | Provider identity/configuration boundary | 1:N CloudAccounts | 只保存 credential reference；account 和 connection tenant 必须匹配 |
| Membership | 属于一个 Tenant 和一个 external subject | 允许 subject 参与某个 Tenant | subject 可加入多个 Tenant | issuer + subject 是稳定身份；email 不是 |
| 范围 | 属于一个 Tenant，可选 account/resource | 把权限限定到 typed target | 层级结构 | 缺失 tenant 不能表示 wildcard |

## 3. Identity source 评审

| 执行路径 | Identity source | Tenant source | 拒绝来源 |
| --- | --- | --- | --- |
| Human HTTP | 已验证 OIDC issuer + subject | 服务端验证后的 active Membership | 任意 header/query/route/body |
| Service HTTP | 已验证 workload/service subject | 显式 service grant 和 target Tenant | 共享 secret 或 client claim alone |
| Background Job | 服务端创建的 job definition/message | 持久化 tenant/account 范围 | ambient HTTP state、default tenant、Provider account inference |
| Platform administration | 独立 platform-level grant | 一个明确 target Tenant | 隐式 all-tenant wildcard |
| Local development | 受控开发身份 adapter | seeded development Tenant | Azure CLI tenant ID 作为业务 tenant |

## 4. 范围与授权评审

初始范围层级：

```text
Tenant -> CloudAccount -> 未来 Provider resource 范围
```

授权同时评估权限和范围。Day 20 不选择最终 role 名称，也不实现 policy；Day 27 负责这些细节。

已固定规则：

1. 每个非平台范围都包含 `tenant_id`。
2. account 范围通过该 Tenant 拥有的 CloudAccount 解析。
3. Provider-native ID 不单独授予访问权限。
4. 缺失 tenant context 是失败，不是 global query。
5. 跨租户平台操作需要单独权限、明确目标、reason、correlation 和追加式审计。

## 5. 隔离边界评审

初始存储使用共享 PostgreSQL database 和 schema。这是逻辑隔离决策，不代表已经完成物理隔离。

必需实现控制：

- tenant-owned core row 携带 `tenant_id`；
- tenant-aware unique index；
- composite relationship 或等价检查阻止 cross-tenant join；
- Application 和 Repository contract 要求 tenant；
- cache key、job、object path 和审计中包含 tenant；
- 生产环境不存在 empty/default Tenant；
- PostgreSQL RLS 作为 defense in depth 单独评估。

## 6. 生命周期与破坏性操作

Organization、Tenant、CloudAccount、ProviderConnection 和 Membership 使用显式 status transition。
Tenant offboarding 和 CloudAccount transfer 不是直接 delete/update 操作。

Day 21 必须使用 expand-only schema change。Day 20 或 Day 21 不给现有 resource、cost 和 ETL 行伪造 tenant。
Day 24 负责把旧数据可重复 backfill 到明确 development Tenant。

## 7. 威胁评审

| 威胁 | Day 20 控制 | 验证 Owner |
| --- | --- | --- |
| Azure tenant 与业务 Tenant 混淆 | 明确区分概念和字段名 | Day 21 model tests |
| 伪造 tenant selector | selector 只请求服务端已验证的 Membership authority | Day 22 |
| cross-tenant account/connection reference | same-tenant relationship invariant | Day 21/23 |
| background job 丢失 tenant | job 必须携带服务端创建的 tenant/account 范围 | Day 22/30 |
| normal admin 变成 platform admin | 独立 platform-level grant | Day 27/30 |
| support operator 执行 global query | 显式 one-target operation 和审计 | Day 27/29/30 |
| credential 存入数据库 | ProviderConnection 只保存 opaque reference | Day 21/26 |
| account 在 tenant 间静默转移 | transfer 需要未来受控 workflow | 后续 ADR/workflow |

## 8. 替代方案结论

| 替代方案 | 结论 | 原因 |
| --- | --- | --- |
| Azure tenant 等于业务 Tenant | Rejected | Provider-specific，且不是正确客户边界 |
| 只用 CloudAccount 做 tenancy | Rejected | 不能表达 membership、shared policy 或 multi-account workspace |
| 每个 Tenant 一个 schema | Deferred | migration/operations 复杂度过早 |
| 每个 Tenant 一个 database | Deferred | 更强隔离层未来可加入，不改变 ID |
| Membership 上直接放 role column | Deferred | Day 27 需要 权限 + 类型化范围 |
| 信任 `X-Tenant-Id` | Rejected | 直接 tenant-escape 风险 |

## 9. Day 20 验收清单

- [x] Organization 已定义。
- [x] Tenant 被定义为主要隔离边界。
- [x] CloudAccount 映射 Azure Subscription 和未来 AWS Account。
- [x] ProviderConnection 不保存 credential material。
- [x] Membership 使用稳定 issuer + subject identity。
- [x] Tenant/account 范围层级已定义。
- [x] human、service 和 background identity source 已定义。
- [x] Azure tenant 和业务 Tenant 明确分离。
- [x] platform administrator path 明确且可审计。
- [x] shared-schema isolation 要求已定义。
- [x] Day 21-30 实现和 negative tests 已映射。
- [x] 未把 Day 21 schema 或 migration 工作拉入 Day 20。

## 10. 决策

**Day 20 Complete**

ADR-0003 已接受。Day 21 可以开始实现该模型定义的 Domain、EF configuration 和 expand-only migration。
