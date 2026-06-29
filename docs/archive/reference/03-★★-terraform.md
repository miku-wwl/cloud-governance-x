# 03 ★★ Terraform

## Day 2 Azure 基础设施

Azure Terraform root 位于 `terraform/azure`。它刻意保持小规模、低成本，用于在应用代码开始消费 Azure SDK 前，演示完整基础设施生命周期。

部署会创建：

| 资源 | 配置 | 后续用途 |
| --- | --- | --- |
| Resource Group | 一个隔离 demo scope | 生命周期和资源清单 |
| Storage Account | StorageV2、Standard LRS | ETL artifact 和未来 state 选项 |
| Service Bus Namespace | Basic | 计划中的治理事件传输 |
| Service Bus Queue | `governance-events` | anomaly 和 compliance event |
| Log Analytics | 可选，默认关闭 | 未来运行日志 |

## 治理标签

Terraform 会给所有可打标签资源添加：

- `owner`
- `environment`
- `cost-center`
- `managed-by=terraform`
- `project=cloud-governance-x`

前三个标签会在后续 compliance 工作中变成必需治理标签。

## 认证

本地开发使用当前 Azure CLI identity：

```powershell
az login
az account show
```

仓库不保存 Azure credential。

## 生命周期验证

可重复验证脚本会执行完整 Day 2 acceptance flow：

```powershell
./scripts/Test-AzureTerraformLifecycle.ps1
```

脚本会初始化 provider、验证格式、创建保存的 plan、apply、通过 Azure CLI 查询 Azure Resource Manager、验证预期资源类型、destroy 部署，并确认 Resource Group 不再存在。验证 destroy 后，默认删除 provider cache、本地 state、保存的 plan 和证据文件。需要本地 JSON 证据用于排错或 demo 时，可以使用 `-KeepEvidence`。

只有当资源确实需要留给下一次非生产开发会话时，才使用 `-KeepResources`。保留资源可能继续产生 Azure 费用：

```powershell
./scripts/Test-AzureTerraformLifecycle.ps1 -KeepResources
```

Log Analytics 默认关闭，因为 ingestion 可能产生持续费用：

```powershell
./scripts/Test-AzureTerraformLifecycle.ps1 -EnableLogAnalytics
```

当前 root 要求 Terraform `>= 1.9.0`，并依赖已提交的 `.terraform.lock.hcl` 固定精确 provider 版本。2026-06-18 复核时，本地 Terraform CLI 为 `1.14.0`，而 `1.15.6` 已可用。这是升级候选，不是无 review 运行 `terraform init -upgrade`、变更 state 或 provider lock 的理由。升级必须经过 release note 检查和 plan review。
