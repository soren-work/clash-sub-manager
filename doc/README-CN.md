# ClashSubManager 项目文档

**🌐 语言**: [English](README.md) | [中文](README-CN.md)

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
- [📄 环境变量配置说明.md](deployment/环境变量配置说明.md) - 环境变量详细配置说明
- [📄 部署运维文档.md](deployment/部署运维文档.md) - 完整部署和运维指南

---

## 📋 Spec - MVP规格文档

### 🏗️ Design - MVP设计文档

#### 🏛️ Architecture - 架构设计
系统级架构设计和MVP边界定义文档。

- [📄 ClashSubManager-MVP概要设计.md](spec/design/architecture/ClashSubManager-MVP概要设计.md) - **核心文档** - MVP整体架构设计和技术方案
- [📄 MVP核心功能.md](spec/design/architecture/MVP核心功能.md) - 核心功能定义和实现逻辑
- [📄 MVP边界定义.md](spec/design/architecture/MVP边界定义.md) - MVP范围边界和排除功能

#### 🧩 Modules - 模块详细设计
各功能模块的详细设计文档，包含具体实现细节。

- [📄 管理员认证-MVP详细设计.md](spec/design/modules/管理员认证-MVP详细设计.md) - 管理员认证系统详细设计
- [📄 Clash模板-MVP详细设计.md](spec/design/modules/Clash模板-MVP详细设计.md) - Clash配置模板管理设计
- [📄 IP管理-MVP详细设计.md](spec/design/modules/IP管理-MVP详细设计.md) - 优选IP管理功能设计
- [📄 订阅API-MVP详细设计.md](spec/design/modules/订阅API-MVP详细设计.md) - 订阅接口详细设计

#### 📝 Requirements - 需求分析
客户端和服务端需求分析文档。

- [📄 客户端需求.md](spec/design/requirements/客户端需求.md) - 客户端扩展功能需求（非MVP核心）
- [📄 服务端需求.md](spec/design/requirements/服务端需求.md) - 服务端核心功能需求

### 📅 Plan - MVP开发计划

- [📄 ClashSubManager-MVP开发计划.md](spec/plan/ClashSubManager-MVP开发计划.md) - **核心文档** - 完整MVP开发计划和任务分解

### 🔍 Review - MVP评审文档

*暂无文档 - 待后续添加MVP评审报告*

### 🧪 Test - MVP测试文档

- [📄 ClashSubManager-MVP单元测试设计.md](spec/test/ClashSubManager-MVP单元测试设计.md) - 单元测试设计和测试用例
- [📄 cloudflare-ip-test.csv](spec/test/cloudflare-ip-test.csv) - 测试数据文件

---

## 🎯 AI Agent使用指南

### 开发顺序建议：

1. **📖 首先阅读**：
   - [ClashSubManager-MVP概要设计.md](spec/design/architecture/ClashSubManager-MVP概要设计.md) - 了解整体架构
   - [ClashSubManager-MVP开发计划.md](spec/plan/ClashSubManager-MVP开发计划.md) - 了解开发任务

2. **🔧 模块开发**：
   - 根据[开发计划](spec/plan/ClashSubManager-MVP开发计划.md)按优先级开发
   - 参考对应[模块详细设计](spec/design/modules/)文档进行实现

3. **🧪 测试验证**：
   - 参考[单元测试设计](spec/test/ClashSubManager-MVP单元测试设计.md)编写测试
   - 使用[测试数据](spec/test/cloudflare-ip-test.csv)进行验证

4. **🚀 部署上线**：
   - 参考[部署运维文档](deployment/部署运维文档.md)进行部署
   - 配置[环境变量](deployment/环境变量配置说明.md)

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

---

**文档版本**：v1.0  
**创建时间**：2026-01-21  
**维护者**：AI Agent软件工程师  
**适用范围**：ClashSubManager MVP开发项目
