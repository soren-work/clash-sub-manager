# ClashSubManager 项目文档

**🌐 语言**: [English](README.md) | [中文](README-CN.md)

---

## 📖 项目简介

### 这是什么项目？

ClashSubManager 是一个智能的 Clash 订阅管理服务，帮助你更好地管理和优化代理订阅。它可以：

- **整合多个订阅源**：将多个机场订阅合并为一个统一的订阅链接
- **优选 IP 替换**：自动将节点 IP 替换为 CloudflareIP 优选 IP，提升连接速度
- **自定义配置**：支持自定义 Clash 规则、代理组和节点命名
- **多用户管理**：为不同用户提供专属配置，互不干扰

### 核心功能概述

- ✅ **订阅聚合**：支持多个上游订阅源，自动合并节点
- ✅ **IP 优选**：基于 CloudflareST 测速结果，替换为最优 IP
- ✅ **模板系统**：灵活的 Clash 配置模板，支持规则和代理组自定义
- ✅ **用户隔离**：每个用户独立配置，支持批量管理
- ✅ **Web 管理**：简洁的管理界面，无需手动编辑配置文件

### 适用人群

- 🎯 **普通用户**：想要更快的代理速度，不想折腾复杂配置
- 🎯 **高级用户**：需要精细控制规则分流和代理组策略
- 🎯 **多人共享**：家庭/团队共用订阅，需要为不同成员提供专属配置
- 🎯 **开发者**：想要自建订阅服务，或基于此项目二次开发

---

## 🚀 快速上手路径

根据你的使用场景，选择对应的阅读路径：

### 👤 我是普通用户 - 只想使用

**目标**：快速部署并使用订阅服务

1. 📖 阅读 [项目主 README](../README-CN.md) - 了解基本概念和功能
2. 🚀 参考 [部署运维文档](deployment/deployment-guide-cn.md) - 快速部署服务
3. ⚙️ 查看 [环境变量配置](deployment/env-config-cn.md) - 配置你的订阅源
4. 💡 遇到问题？查看 [常见问题 FAQ](FAQ-CN.md)（待添加）

**预计时间**：30 分钟

---

### 🔧 我是开发者 - 想要部署

**目标**：在生产环境部署并运维

1. 📖 阅读 [项目主 README](../README-CN.md) - 了解项目架构
2. 🏗️ 阅读 [MVP 概要设计](spec/design/architecture/mvp-outline-cn.md) - 理解系统架构
3. 🚀 参考 [部署运维文档](deployment/deployment-guide-cn.md) - 生产环境部署
4. ⚙️ 查看 [环境变量配置](deployment/env-config-cn.md) - 安全配置和优化
5. 📚 阅读 [高级使用指南](advanced-guide-cn.md)（待添加） - 性能优化和故障排查

**预计时间**：2 小时

---

### 👨‍💻 我是贡献者 - 想要开发

**目标**：理解代码结构，参与项目开发

1. 📖 阅读 [项目主 README](../README-CN.md) - 了解项目背景
2. 🏗️ 阅读 [MVP 概要设计](spec/design/architecture/mvp-outline-cn.md) - 理解整体架构
3. 📅 阅读 [MVP 开发计划](spec/plan/mvp-development-plan-cn.md) - 了解开发任务
4. 🧩 查看 [模块详细设计](spec/design/modules/) - 深入理解各模块实现
5. 🧪 参考 [单元测试设计](spec/test/mvp-unit-test-cn.md) - 编写测试用例

**预计时间**：4 小时

---

## 📚 文档阅读建议

### 按使用场景推荐

| 使用场景 | 必读文档 | 选读文档 |
|---------|---------|---------|
| **快速使用** | 主 README、部署文档 | 环境变量配置 |
| **生产部署** | 主 README、部署文档、环境变量配置 | 高级指南、架构设计 |
| **功能定制** | 主 README、高级指南、模块设计 | 架构设计、开发计划 |
| **二次开发** | 架构设计、模块设计、开发计划 | 测试设计、需求文档 |

### 文档优先级标注

- 🔴 **必读** - 所有用户都应该阅读
- 🟡 **重要** - 根据使用场景选择性阅读
- 🟢 **参考** - 深入研究时参考

---

## 文档目录结构

本文档目录按照MVP项目经理工作流规范组织，为AI Agent软件工程师提供完整的项目实施指导。

### 📁 目录概览

```
doc/
├── 📋 README.md                    # 本文档 - 项目文档导航
├── 🚀 deployment/                  # 部署相关文档
└── 📋 spec/                        # MVP规格文档
    ├── 🏗️ design/                   # MVP设计文档
    │   ├── 🏛️ architecture/         # 架构设计文档
    │   ├── 🧩 modules/              # 模块详细设计
    │   └── 📝 requirements/         # 需求分析文档
    ├── 📅 plan/                     # MVP开发计划
    ├── 🔍 review/                   # MVP评审文档
    └── 🧪 test/                     # MVP测试文档
```

---

## 🚀 Deployment - 部署文档

部署相关文档，包含Docker容器化部署和环境配置说明。

### 文档列表：
- [📄 Dockerfile](deployment/Dockerfile) - Docker容器构建文件
- [📄 docker-compose.yml](deployment/docker-compose.yml) - Docker Compose编排文件
- [📄 环境变量配置说明.md](deployment/env-config-cn.md) - 环境变量详细配置说明
- [📄 部署运维文档.md](deployment/deployment-guide-cn.md) - 完整部署和运维指南

---

## 📋 Spec - MVP规格文档

### 🏗️ Design - MVP设计文档

#### 🏛️ Architecture - 架构设计
系统级架构设计和MVP边界定义文档。

- [📄 MVP概要设计.md](spec/design/architecture/mvp-outline-cn.md) - **核心文档** - MVP整体架构设计和技术方案
- [📄 MVP核心功能.md](spec/design/architecture/mvp-core-features-cn.md) - 核心功能定义和实现逻辑
- [📄 MVP边界定义.md](spec/design/architecture/mvp-boundary-cn.md) - MVP范围边界和排除功能

#### 🧩 Modules - 模块详细设计
各功能模块的详细设计文档，包含具体实现细节。

- [📄 管理员认证-MVP详细设计.md](spec/design/modules/admin-auth-detail-cn.md) - 管理员认证系统详细设计
- [📄 Clash模板-MVP详细设计.md](spec/design/modules/clash-template-detail-cn.md) - Clash配置模板管理设计
- [📄 IP管理-MVP详细设计.md](spec/design/modules/ip-management-detail-cn.md) - 优选IP管理功能设计
- [📄 订阅API-MVP详细设计.md](spec/design/modules/subscription-api-detail-cn.md) - 订阅接口详细设计

#### 📝 Requirements - 需求分析
客户端和服务端需求分析文档。

- [📄 客户端需求.md](spec/design/requirements/客户端需求.md) - 客户端扩展功能需求（非MVP核心）
- [📄 服务端需求.md](spec/design/requirements/服务端需求.md) - 服务端核心功能需求

### 📅 Plan - MVP开发计划

- [📄 MVP开发计划.md](spec/plan/mvp-development-plan-cn.md) - **核心文档** - 完整MVP开发计划和任务分解

### 🔍 Review - MVP评审文档

*暂无文档 - 待后续添加MVP评审报告*

### 🧪 Test - MVP测试文档

- [📄 MVP单元测试设计.md](spec/test/mvp-unit-test-cn.md) - 单元测试设计和测试用例
- [📄 cloudflare-ip-test.csv](spec/test/cloudflare-ip-test.csv) - 测试数据文件

---

## 🎯 AI Agent使用指南

### 开发顺序建议：

1. **📖 首先阅读**：
   - [MVP概要设计.md](spec/design/architecture/mvp-outline-cn.md) - 了解整体架构
   - [MVP开发计划.md](spec/plan/mvp-development-plan-cn.md) - 了解开发任务

2. **🔧 模块开发**：
   - 根据[开发计划](spec/plan/mvp-development-plan-cn.md)按优先级开发
   - 参考对应[模块详细设计](spec/design/modules/)文档进行实现

3. **🧪 测试验证**：
   - 参考[单元测试设计](spec/test/mvp-unit-test-cn.md)编写测试
   - 使用[测试数据](spec/test/cloudflare-ip-test.csv)进行验证

4. **🚀 部署上线**：
   - 参考[部署运维文档](deployment/deployment-guide-cn.md)进行部署
   - 配置[环境变量](deployment/env-config-cn.md)

### 📚 文档优先级：

- **🔴 必读**：MVP概要设计、开发计划
- **🟡 重要**：模块详细设计、测试设计
- **🟢 参考**：需求文档、部署文档

---

## 📝 文档维护

### 文档规范：
- 所有文档遵循MVP最小可验证原则
- 文档内容精简，仅包含AI Agent实施必需信息
- 使用标准Markdown格式，便于阅读和维护

### 更新说明：
- MVP版本迭代：更新相关设计和计划文档
- 新增模块：添加到对应模块设计目录
- 测试完善：更新测试文档和用例