# Azure Day 2 基础设施

此 Terraform root 创建后续 Day 使用的最小 Azure 开发基础设施：

- Resource Group
- Storage Account
- Service Bus Namespace
- Service Bus Queue

## 1. 前置条件

运行 Terraform 前先用 Azure CLI 登录：

```powershell
az login
az account show
```

## 2. 手工运行

```powershell
Push-Location terraform/azure
terraform init
terraform fmt -check
terraform validate
terraform plan -out day2.tfplan
terraform apply day2.tfplan
terraform output
terraform destroy
Pop-Location
```

## 3. 完整生命周期脚本

从仓库根目录执行完整 create、verify、destroy 闭环：

```powershell
./scripts/Test-AzureTerraformLifecycle.ps1
```

首次使用前需要先运行：

```powershell
terraform -chdir=terraform/azure init
```

## 4. 文件与状态边界

本地 state、plan、变量覆盖和证据文件不提交 Git。生命周期脚本在验证完成后会删除自身运行产物。

该 Terraform 配置只用于开发和学习闭环。生产或团队环境仍需要远程 state、locking、环境隔离、secret store、变更审批和销毁保护。
