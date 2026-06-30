# Day 胶囊索引

本目录是按 Day 回顾项目历史的入口。每个 `day-x.md` 都必须使用固定 9 段结构，方便长期项目在中途恢复上下文。

固定结构：

1. `# Day X - 标题`
2. `## 1. 目标`：本 Day 要解决什么生产风险 / 工程问题。
3. `## 2. 前置条件`：依赖哪个 Phase / Day / ADR / 风险项。
4. `## 3. 施工范围`：允许改什么，不允许改什么。
5. `## 4. 设计决策`：本 Day 的关键模型、边界、取舍。
6. `## 5. 实现摘要`：改了哪些模块、迁移、脚本、测试。
7. `## 6. 验证证据`：运行了哪些命令，结果如何。
8. `## 7. Review 结论`：Accepted / Validation / Rejected / Blocked，以及发现的问题。
9. `## 8. 遗留风险`：明确留给后续 Day 的内容。
10. `## 9. 相关链接`：PR、commit、ADR、风险项、旧证据文件。

Day 胶囊是历史记录。项目当前整体状态以 [../current-state.md](../current-state.md) 为准。

## 索引

| Day | 胶囊 | 结论 |
| --- | --- | --- |
| 1 | [day-01.md](day-01.md) | 开发基线完成 |
| 2 | [day-02.md](day-02.md) | 开发基线完成 |
| 3 | [day-03.md](day-03.md) | 开发基线完成 |
| 4 | [day-04.md](day-04.md) | 开发基线完成 |
| 5 | [day-05.md](day-05.md) | 开发基线完成 |
| 6 | [day-06.md](day-06.md) | 开发基线完成 |
| 7 | [day-07.md](day-07.md) | 开发基线完成 |
| 8 | [day-08.md](day-08.md) | Phase 0 能力基线 |
| 9 | [day-09.md](day-09.md) | Phase 0 复验证据 |
| 10 | [day-10.md](day-10.md) | Phase 0 架构快照 |
| 11 | [day-11.md](day-11.md) | Phase 0 出关完成 |
| 12 | [day-12.md](day-12.md) | Phase 1 工程基线 |
| 13 | [day-13.md](day-13.md) | 静态门禁建立 |
| 14 | [day-14.md](day-14.md) | 架构测试建立 |
| 15 | [day-15.md](day-15.md) | Endpoint 模块拆分 |
| 16 | [day-16.md](day-16.md) | DI 模块拆分 |
| 17 | [day-17.md](day-17.md) | Worker 注册表建立 |
| 18 | [day-18.md](day-18.md) | Migration Host 分离 |
| 19 | [day-19.md](day-19.md) | Phase 1 Accepted |
| 20 | [day-20.md](day-20.md) | 租户模型 Accepted |
| 21 | [day-21.md](day-21.md) | 租户 schema Validation |
| 22 | [day-22.md](day-22.md) | TenantContext Accepted |
| 23 | [day-23.md](day-23.md) | 租户感知 Repository Accepted |
| 24 | [day-24.md](day-24.md) | 历史回填 Validation |
| 25 | [day-25.md](day-25.md) | OIDC Bearer Validation |
| 26 | [day-26.md](day-26.md) | Entra 开发集成 Accepted |
| 27 | [day-27.md](day-27.md) | RBAC 模型与范围评估 Accepted |
| 28 | [day-28.md](day-28.md) | 端点保护与授权错误契约 Accepted |
| 29 | [day-29.md](day-29.md) | 追加式审计 Accepted |
