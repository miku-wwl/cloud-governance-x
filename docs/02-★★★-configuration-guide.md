# 02 ★★★ 配置文件说明

本文详细解释 Day 1～7 使用的 JSON、YAML、SLNX、MSBuild、环境变量、
项目文件和 Terraform 配置。

## 为什么 JSON 文件中没有注释

标准 JSON 语法不支持注释。加入 `//`、`/* */` 或人为添加 `_comment`
字段，都会导致文件无法解析，或者把纯说明文字错误地放进应用配置模型。

因此，JSON 文件保持严格合法，字段含义统一在本文说明。YAML、XML、HCL
和 `.env.example` 本身支持注释，所以也在对应文件中加入了中文说明。

## .NET 配置覆盖顺序

.NET 按以下顺序加载配置，越靠后的来源优先级越高：

1. `appsettings.json`
2. `appsettings.{Environment}.json`，如果文件存在
3. 环境变量
4. 命令行参数

环境变量使用双下划线 `__` 表示嵌套层级。例如：

```powershell
$env:PostgreSql__Database = "finops_day7"
```

API 和 Worker 原来的 `appsettings.Development.json` 已经删除，因为其中的
日志配置与基础 `appsettings.json` 完全相同，没有产生任何覆盖效果。

如果 IDE 仍显示这些文件的标签页，那只是编辑器保存的旧标签页状态，并不表示
文件仍存在。关闭标签页或刷新文件树即可。

## 根目录配置

### 静态门禁入口

阶段 1 的本地静态门禁入口是：

```powershell
./scripts/Test-RepositoryStatic.ps1
```

该脚本由 Day 13 引入，用于把 JSON、YAML、XML、PowerShell、Markdown、
GitHub Actions workflow、Terraform、格式、依赖、secret、垃圾文件、build 和
test 检查串成一个可重复入口。任何阻断性子检查失败时，脚本返回非零退出码。

### GitHub Actions

`.github/workflows/ci.yml` 是 Day 19 初版 CI：

- `Static verification` 在 Ubuntu runner 上安装 `global.json` 指定的 .NET SDK
  和 Terraform 1.14.0，然后运行仓库统一静态门禁；
- `Database migration` 在独立 Ubuntu runner 上启动本地 Compose PostgreSQL，
  运行不访问 Azure 的 migration/权限回归；
- workflow 只授予 `contents: read`；
- checkout、setup-dotnet 和 setup-terraform 固定到经过审查的 release commit
  SHA，版本写在行尾注释中；
- Azure/Terraform 外部资源 E2E 不会在 Phase 1 CI 自动运行。

Workflow YAML 由固定版本和 SHA-256 的 actionlint 检查。PR 模板、
CODEOWNERS、ADR 模板、required check 名称和责任边界见 `.github/`、
`docs/adr/ADR-template.md` 与 `docs/phase-1/engineering-governance.md`。

### `global.json`

- `sdk.version`：把 .NET SDK `10.0.300` 设为本仓库的基线版本。
- `sdk.rollForward: latestFeature`：找不到精确版本时，可以使用更新的
  .NET 10 feature band，但不会自动升级到 .NET 11。

.NET 在启动 MSBuild 之前就会读取该文件。它是必须提交的工程配置，不是构建
生成物，也不是垃圾文件。

### `dotnet-tools.json`

- `version: 1`：本地工具清单的格式版本。
- `isRoot: true`：工具查找在本仓库清单处停止，不继续向父目录查找。
- `tools.dotnet-ef.version`：固定 EF Core 命令行工具版本为 `10.0.4`。
- `commands`：将工具命令暴露为 `dotnet ef`。
- `rollForward: false`：禁止静默使用其他版本的工具。

执行 `dotnet tool restore` 会按照该清单安装工具。工具二进制保存在仓库之外，
清单本身需要提交到 Git。

2026 年 6 月 18 日复核时，`dotnet list package --outdated` 已报告部分
.NET 10 patch 或测试工具可升级。当前文档记录的是仓库基线，不表示这些依赖
永远保持不变；阶段 1/14 需要把依赖升级、回归测试和供应链扫描纳入固定门禁。

### `compose.yaml`

该文件定义 API、Worker、Migrator 和端到端测试共同使用的本地 PostgreSQL。
文件中的中文注释解释了镜像版本、变量默认值、端口映射、健康检查、重启策略
和命名卷。

`postgres:18-alpine` 的官方 Docker 镜像在 PostgreSQL 18 起使用版本化
`PGDATA` 路径；当前 Compose 把命名卷挂载到 `/var/lib/postgresql`，覆盖的是
官方镜像声明的父级数据卷。不要在没有 dump/restore 或升级演练的情况下把同一
本地卷改挂到其他 PostgreSQL 主版本。

命名卷会在普通的 `docker compose down` 后保留数据。只有执行：

```powershell
docker compose down --volumes
```

才会连同本地数据库数据一起删除。

### `.env.example`

该文件只是 Compose 环境变量的安全示例，不包含真实生产密码。

即使没有 `.env`，Compose 也能运行，因为 `compose.yaml` 使用
`${变量名:-默认值}` 提供了默认配置。如果需要覆盖默认值，可以在本地创建
不会提交到 Git 的 `.env`。

### `FinOpsPlatform.slnx`

SLNX 是当前 .NET 使用的 XML 解决方案格式。它告诉 IDE 和 `dotnet`：

- 哪些项目属于同一个解决方案；
- 执行 restore、build、test 时要包含哪些项目；
- IDE 中如何对项目进行分组。

SLNX 不决定项目之间的运行时依赖。真正的依赖由各个 `.csproj` 中的
`ProjectReference` 声明。

### `Directory.Build.props`

MSBuild 会自动把该文件导入当前目录下的所有项目：

- `TargetFramework`：所有项目统一编译为 .NET 10。
- `Nullable`：启用可空引用类型检查。
- `ImplicitUsings`：自动导入常用命名空间。
- `EnableNETAnalyzers`：启用 SDK 内置 .NET 分析器。
- `AnalysisLevel`：使用当前 SDK 的最新分析规则集。
- `EnforceCodeStyleInBuild`：让 `.editorconfig` 中提升为 warning 的代码样式
  诊断进入构建。
- `TreatWarningsAsErrors`：任何编译警告都会导致构建失败。

集中配置可以避免七个项目分别设置后逐渐产生版本或编译规则偏差。

### `.editorconfig`

该文件是 Day 12 引入的全仓格式和代码样式基线：

- 所有文本文件默认使用空格缩进、去除行尾空白并保留文件末尾换行；
- C# 使用 4 空格缩进，XML、JSON、YAML、Markdown 和 Terraform 使用
  2 空格缩进；
- C# 构建会检查格式诊断 `IDE0055`；
- EF Core migration 文件标记为 generated code，避免设计时生成物被普通
  业务代码规则误伤。

因为 `Directory.Build.props` 同时启用了 `EnforceCodeStyleInBuild` 和
`TreatWarningsAsErrors`，违反这些 warning 级规则会使 `dotnet build` 失败。

## 应用 JSON 配置

### API 的 `appsettings.json`

- `Logging.LogLevel.Default`：应用日志的默认最低级别。
- `Logging.LogLevel.Microsoft.AspNetCore`：ASP.NET Core 框架日志的最低级别，
  设置为 Warning 可以减少普通请求产生的噪声。
- `PostgreSql.Host`：数据库主机地址。
- `PostgreSql.Port`：PostgreSQL 端口。
- `PostgreSql.Database`：默认数据库名称。
- `PostgreSql.Username`：本地数据库用户名。
- `PostgreSql.Password`：本地 Compose 开发密码，不是生产凭据。
- `PostgreSql.TimeoutSeconds`：数据库连接超时时间。API 使用较短时间，使健康
  检查在数据库不可用时能够快速返回。
- `Azure.TenantId`：可选的 Azure Tenant 限制。留空时由
  `DefaultAzureCredential` 使用当前登录身份所属的 Tenant。
- `AzureCost.UseSampleDataWhenUnavailable`：Azure Cost Management 不可用
  或没有数据时，是否允许生成明确标记为 sample 的演示数据。
- `AzureCost.ForceSampleData`：强制使用样例数据的测试开关，正常运行应为
  `false`。
- `Authentication.Oidc.Enabled`：是否允许 API 使用 JWT Bearer handler
  接受外部 OIDC Provider 签发的 token。默认 `false`，关闭时即使收到格式
  正确的 Bearer token 也不会建立认证身份。
- `Authentication.Oidc.Authority`：OIDC issuer/metadata 根地址。启用认证时
  必须是绝对 URI。开发环境使用 tenant-specific v2 Authority：
  `https://login.microsoftonline.com/<tenant-id>/v2.0`。
- `Authentication.Oidc.Audience`：API 接受的 token audience。启用认证时
  必须显式配置。Microsoft Entra v2 access token 使用 API Application
  Client ID 作为 `aud`。
- `Authentication.Oidc.RequireHttpsMetadata`：是否要求 OIDC metadata 使用
  HTTPS。生产和共享环境必须保持 `true`。
- `Authentication.Oidc.ClockSkewSeconds`：token 时间验证允许的时钟偏差，
  必须在 0～300 秒之间，默认 60 秒。
- `AllowedHosts`：ASP.NET Core 接受的 Host 请求头。`*` 适用于本地学习环境，
  生产环境应限制为真实域名。

OIDC 配置不包含 password、client secret、private key 或 token。API 只验证
身份提供方签发的 token，不实现用户名密码登录或 token 签发。JWT handler
保留原始 `iss` 和 `sub` Claim，随后由 `HttpTenantContextMiddleware` 验证
Tenant Membership。health 与根状态端点显式匿名；业务 endpoint 的权限策略和
全面保护属于 Day 27～28。

Day 26 的本地开发客户端是 public client，通过 Device Code Flow 获取 delegated
token。它没有 client secret，也不能作为后台服务身份。初始化脚本把 App
Registration ID 写入 Git 忽略的 `tmp/day26-entra-development.json`；该文件
不含 token 或 credential。Device Code 和 access token 同样不得提交或写入
普通日志。

API 不再自动应用 EF Core migration。首次启动或 schema 更新后必须先运行：

```powershell
./scripts/Invoke-DatabaseMigration.ps1 -Database finops
```

生产发布也必须把 Migrator 作为独立、受控的 release step；API 运行身份不应
拥有 DDL 权限。Migrator 会为目标数据库获取 PostgreSQL advisory lock；如果
同一数据库已有另一个 FinOps Migrator 在运行，本次执行会明确失败。

不访问 Azure 的数据库 migration 回归入口为：

```powershell
./scripts/Test-DatabaseMigration.ps1
```

该脚本使用隔离的临时数据库和角色验证空库升级、重复执行、同库并发拒绝、
连接失败退出码，以及 API/Worker 在无 schema `CREATE` 权限下运行，最后清理
测试数据库、角色、进程和日志。

### Worker 的 `appsettings.json`

日志、PostgreSQL、Azure 和 AzureCost 配置与 API 含义相同。

Worker 的数据库连接超时时间更长，因为一次性 ETL 任务可能会和刚启动的本地
数据库同时运行。

- `Etl.Job`：选择执行 `Resources` 或 `Costs`；其他值会让进程失败退出。
- `Etl.CostDays`：成本任务需要读取的最近天数。

Day 17 起，Worker 通过 `IWorkerJobHandler` 注册表选择 Job：

- `Resources` 由 `ResourceSyncJobHandler` 处理；
- `Costs` 由 `CostSyncJobHandler` 处理；
- Job 名称匹配不区分大小写；
- 未知或重复 Job 名称会失败；
- 宿主取消属于正常停止，普通执行异常会设置非零退出码。

### API 和 Worker 的 `launchSettings.json`

这些文件只影响 IDE 启动和 `dotnet run`，发布应用时不会使用：

- `$schema`：让编辑器获得格式校验和自动补全。
- `commandName: Project`：启动当前项目。
- `dotnetRunMessages`：输出宿主启动和监听地址信息。
- `launchBrowser: false`：API 启动后不自动打开浏览器。
- `applicationUrl`：未通过 `--urls` 指定地址时使用的 API 地址。
- `ASPNETCORE_ENVIRONMENT`：API 使用的环境名称。
- `DOTNET_ENVIRONMENT`：Worker 使用的环境名称。

两者当前都设置为 Development，但并不要求必须存在
`appsettings.Development.json`。不存在环境专属文件时，程序继续使用基础
`appsettings.json`。

## 项目文件

所有 `.csproj` 都会继承 `Directory.Build.props`。文件内的中文 XML 注释
解释了每组引用的用途。

- `FinOps.Domain`：领域核心，不依赖其他项目或第三方包。
- `FinOps.Application`：只依赖 Domain，保存用例和接口契约。
- `FinOps.Infrastructure`：使用 EF Core、PostgreSQL 和 Azure SDK 实现
  Application 中定义的接口。
- `FinOps.Migrator`：独立 migration executable，只引用 Infrastructure，
  显式应用 pending migration 后退出；使用 PostgreSQL advisory lock 避免两个
  Migrator 同时修改同一数据库；失败时返回退出码 1。
- `FinOps.Api`：HTTP 可执行宿主，负责组合 Application 和 Infrastructure。
  Day 15 起，HTTP endpoint registration 位于 `src/FinOps.Api/Endpoints/`，
   按 Health、Cloud、Resources、Costs 和 ETL 拆分；`Program.cs` 只保留宿主
  启动、服务装配和模块挂载。
- Day 16 起，DI 注册按应用用例、PostgreSQL、Azure 和 Health 拆分。API 和
  Worker 共同调用 `AddApplicationUseCases()` 与 `AddInfrastructure(...)`，
  避免两个宿主各自复制生命周期规则。
- `FinOps.Worker`：一次性执行资源或成本 ETL 的宿主。
- `FinOps.Tests`：xUnit 自动化测试和可选覆盖率采集。

EF Core Design 包上的 `PrivateAssets=all` 表示该包只供本项目设计时使用，
不会作为传递依赖暴露给引用 Infrastructure 的其他项目。

## Terraform 配置

- `providers.tf`：声明 Terraform 最低版本和 Provider 版本范围。
- `variables.tf`：声明输入变量、类型、默认值和校验规则。
- `main.tf`：定义 Azure 资源、随机命名后缀和统一治理标签。
- `outputs.tf`：输出人工检查和生命周期脚本需要使用的资源标识。
- `terraform.tfvars.example`：可选变量覆盖的安全示例。
- `.terraform.lock.hcl`：记录精确 Provider 版本和校验值，保证不同机器安装
  相同依赖，因此应该提交到 Git。

Terraform state、plan、真实变量覆盖、下载的 Provider 和验证证据都是本机运行
数据，可能包含敏感信息，因此已经被 `.gitignore` 排除。

`.terraform.lock.hcl` 是 Terraform 自动维护的文件，其顶部英文提示也由工具
生成，不应手工翻译或修改。

## 可删除的本地文件

以下内容都不会提交：

- `tmp/`：端到端报告、命令日志和临时证据；
- `bin/`、`obj/`：.NET 编译缓存；
- `TestResults/`、coverage：测试和覆盖率输出；
- `*.log`：运行日志；
- `.terraform/`、state、plan：Terraform 本地运行数据；
- `.env`：本地环境变量和密码；
- `.vs/`、`.vscode/`、`.idea/`：IDE 本地状态；
- NuGet 包和用户专属配置文件。

Day 1～7 的完整闭环报告放在本地 `tmp/` 下，因此可以保留用于学习，但不会
污染 Git 仓库。
