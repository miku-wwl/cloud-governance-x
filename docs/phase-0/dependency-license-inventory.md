# 依赖、许可证与供应链清单

## 1. 取证范围与限制

- 取证日期：2026 年 6 月 14 日
- 基线 Commit：`d3d760e`
- NuGet 来源：`dotnet list ... --include-transitive` 与本机包内 `.nuspec`
- Terraform 来源：`providers.tf`、`.terraform.lock.hcl` 和临时 `terraform init`
- 容器来源：本机 `docker image inspect postgres:18-alpine`
- 当前结论：已登记直接依赖和关键供应链对象；正式组织许可证批准、SBOM、
  容器漏洞扫描与持续监控仍属于阶段 14

许可证字段表示上游包声明，不等于本项目已经完成法律审查。

## 2. .NET 直接依赖

| 包 | 解析版本 | 用途 | 上游声明 | 证据来源 | 当前结论 |
| --- | --- | --- | --- | --- | --- |
| Azure.Identity | 1.21.0 | `DefaultAzureCredential` | MIT | 包内 `.nuspec` | 已登记 |
| Azure.ResourceManager | 1.14.0 | ARM client 与订阅 | MIT | 包内 `.nuspec` | 已登记 |
| Azure.ResourceManager.ResourceGraph | 1.1.0 | 资源清单查询 | MIT | 包内 `.nuspec` | 已登记 |
| Microsoft.EntityFrameworkCore.Design | 10.0.4 | migration 设计时工具 | MIT | 包内 `.nuspec` | 已登记 |
| Npgsql | 10.0.3 | PostgreSQL 驱动 | PostgreSQL | 包内 `.nuspec` | 已登记 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.2 | EF Core PostgreSQL Provider | PostgreSQL | 包内 `.nuspec` | 已登记 |
| Microsoft.Extensions.Hosting | 10.0.8 | Worker 宿主 | MIT | 包内 `.nuspec` | 已登记 |
| Microsoft.NET.Test.Sdk | 17.14.1 | 测试宿主 | MIT | 包内 `.nuspec` | 已登记 |
| xunit | 2.9.3 | 测试框架元包 | Apache-2.0 | 包内 `.nuspec` | NuGet 标记 Legacy，RISK-0023 |
| xunit.runner.visualstudio | 3.1.4 | 测试适配器 | Apache-2.0 | 包内 `.nuspec` | 已登记 |
| coverlet.collector | 6.0.4 | 覆盖率采集 | MIT | 包内 `.nuspec` | 已登记 |

`FinOps.Application` 和 `FinOps.Domain` 没有直接 NuGet 包。API 主要通过
Infrastructure 项目引用获得传递依赖。

## 3. 关键传递依赖族

| 依赖族 | 当前关键版本 | 用途 | 许可证证据 | 备注 |
| --- | --- | --- | --- | --- |
| Azure.Core | 1.53.0 | Azure SDK pipeline/credential 基础 | `.nuspec`: MIT | Azure SDK 核心 |
| Microsoft.EntityFrameworkCore | 10.0.4 | ORM、migration、查询 | `.nuspec`: MIT | 含 Relational/Abstractions |
| Microsoft.Identity.Client | 4.83.1 | Azure Identity 认证链 | 包 metadata 待正式 scanner 汇总 | 阶段 14 纳入完整 SBOM |
| Microsoft.Extensions.* | 10.0.3～10.0.8 | Hosting、DI、Logging、Options | 包 metadata 待完整汇总 | Worker 依赖族 |
| Newtonsoft.Json | 13.0.3 | EF Design 传递工具依赖 | `.nuspec` 同系列声明 MIT | 非运行时业务 JSON 主路径 |
| Roslyn/MSBuild 工具族 | 5.0.0 / 18.0.2 | EF Design 构建分析 | 包 metadata 待完整汇总 | 主要是设计时依赖 |
| xunit.* v2 | 2.0.3～2.9.3 | 测试框架传递组件 | Apache-2.0 系列 | 已标记 Legacy |

完整机器可读解析结果保存在被忽略的
`tmp/phase-0-evidence/day11-dotnet-packages.json`。本轮 `--vulnerable
--include-transitive` 未发现 NuGet 已知漏洞；这只是 2026 年 6 月 14 日基于
当前 NuGet 源的快照，不是持续安全保证。

## 4. .NET SDK 与本地工具

| 对象 | 版本 | 来源/用途 | 许可证状态 | 更新策略 |
| --- | --- | --- | --- | --- |
| .NET SDK | 10.0.300 | `global.json` 固定构建基线 | MIT；下载包的 ThirdPartyNotices 仍需保留，[官方 LICENSE](https://github.com/dotnet/sdk/blob/main/LICENSE.TXT) | 同 major/feature band 受控升级 |
| dotnet-ef | 10.0.4 | `dotnet-tools.json` 本地 manifest | MIT，[EF Core 官方 LICENSE](https://github.com/dotnet/efcore/blob/main/LICENSE.txt) | 与 EF Core 版本对齐 |

## 5. Terraform

| 对象 | Constraint | Locked/Installed | 完整性 | 上游许可证 | 当前结论 |
| --- | --- | --- | --- | --- | --- |
| `hashicorp/azurerm` | `~> 4.0` | 4.77.0 | lock file `h1/zh` checksums | MPL-2.0，[官方 LICENSE](https://github.com/hashicorp/terraform-provider-azurerm/blob/main/LICENSE) | 已登记 |
| `hashicorp/random` | `~> 3.6` | 3.9.0 | lock file `h1/zh` checksums | MPL-2.0，[官方 metadata](https://github.com/hashicorp/terraform-provider-random/blob/main/.copywrite.hcl) | 已登记 |
| Terraform CLI | `>= 1.9.0` | 1.14.0 | 本机二进制版本 | BUSL-1.1，[官方 LICENSE](https://github.com/hashicorp/terraform/blob/main/LICENSE) | 使用权与组织政策待正式批准 |

Provider 插件仅为取证临时初始化，`terraform/azure/.terraform/` 已在取证后删除。
lock file 保留在 Git；更新必须 review constraint、lock diff、release notes 和
plan，不使用无审查的自动大版本升级。

## 6. 容器镜像

| 镜像 | 当前引用 | 本机解析 digest | 上游/许可证 | 扫描状态 | 用途与限制 |
| --- | --- | --- | --- | --- | --- |
| PostgreSQL | `postgres:18-alpine` | `sha256:96d56f7f57c6aacd1fcb908bc83b345ec5f83231ee486dd66a1baadce274db88` | Docker Official Image packaging 为 MIT；PostgreSQL 本体使用 PostgreSQL License，[上游仓库](https://github.com/docker-library/postgres) | `NotRun` | 仅 local；Compose 未固定 digest，RISK-0024 |

当前没有 API/Worker 生产镜像。阶段 12～14 必须建立固定 digest、签名、
provenance、漏洞扫描、基础镜像支持周期和重建策略。

## 7. 外部工具

| 工具 | 当前版本 | 角色 | 许可证/使用条款状态 | 当前缺口 |
| --- | --- | --- | --- | --- |
| Azure CLI | 2.86.0 | 登录与 Azure E2E | MIT，[官方 LICENSE](https://github.com/Azure/azure-cli/blob/dev/LICENSE)；发行包第三方依赖仍需扫描 | 无版本自动门禁 |
| Docker Engine/Desktop | Engine 29.4.3、Desktop 4.74.0 | 本地容器运行 | Moby Engine 为 Apache-2.0，[官方 LICENSE](https://github.com/moby/moby/blob/master/LICENSE)；Desktop 受 [Docker Subscription Service Agreement](https://docs.docker.com/subscription/desktop-license/) 约束 | 组织使用资格待确认；无容器扫描 |
| Docker Compose | 5.1.4 | 本地 PostgreSQL 编排 | 随 Docker 工具链审查 | 无生产用途 |
| PowerShell | 7.6.2 | E2E 与施工脚本 | MIT，[官方 LICENSE](https://github.com/PowerShell/PowerShell/blob/master/LICENSE.txt)；第三方 notices 待阶段 14 汇总 | 无脚本签名策略 |
| Git | 当前开发机安装 | 源码版本控制 | 待组织工具清单统一管理 | 无 branch protection 证据 |

## 8. 供应链结论

1. 直接 NuGet、关键传递依赖、Terraform Provider、CLI 工具和 PostgreSQL 镜像
   已进入可追踪清单。
2. NuGet 当前未报告已知漏洞；`xunit 2.9.3` 已弃用/Legacy。
3. 未运行正式 license scanner、SBOM、容器漏洞扫描、签名或 provenance 验证。
4. 阶段 1 建立可重复 dependency/secret 门禁；阶段 14 完成组织认可的许可证、
   SBOM、SCA、镜像和 IaC 供应链总门禁。
