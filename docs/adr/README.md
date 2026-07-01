# 架构决策记录

本目录保存当前有效或待签发的 Architecture Decision Record，简称 ADR。

ADR 用来记录长期影响项目结构、数据模型、安全边界、部署方式和工程治理的关键决策。它不是普通说明文档，也不是归档材料；后续 Day 施工如果与已接受 ADR 冲突，必须先更新或新增 ADR，再修改实现。

## 状态

| 状态 | 含义 |
| --- | --- |
| Proposed | 已形成候选决策，等待 Owner 接受 |
| Accepted | 已被 Owner 接受，后续施工必须遵守 |
| Superseded | 已被后续 ADR 替代 |
| Rejected | 已明确拒绝，不作为施工依据 |

## 当前 ADR

| ADR | 状态 | 主题 |
| --- | --- | --- |
| [ADR-0001](ADR-0001-module-boundaries-and-architecture-tests.md) | Accepted | 模块边界与架构测试 |
| [ADR-0002](ADR-0002-migration-host-and-release-flow.md) | Accepted | Migration Host 与发布流程 |
| [ADR-0003](ADR-0003-organization-tenant-cloud-account-model.md) | Accepted | Organization、Tenant、CloudAccount 与范围模型 |
| [ADR-0004](ADR-0004-entra-and-development-identity.md) | Accepted | Microsoft Entra 与开发身份 |
| [ADR-0005](ADR-0005-data-layering-and-lineage.md) | Proposed | 生产数据分层、Lineage 与 Raw Reference |
| [ADR-0018](ADR-0018-dependency-and-toolchain-governance.md) | Accepted | 依赖和工具链版本治理 |

## 模板

- [ADR-template.md](ADR-template.md)
