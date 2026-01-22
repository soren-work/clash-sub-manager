# ClashSubManager Project Documentation

**🌐 Language**: [English](README.md) | [中文](README-CN.md)

## Documentation Directory Structure

This documentation directory is organized according to the MVP Project Manager workflow specifications, providing complete project implementation guidance for AI Agent software engineers.

### 📁 Directory Overview

```
doc/
├── 📋 README.md                    # This document - Project documentation navigation
├── 🚀 deployment/                  # Deployment related documentation
└── 📋 spec/                        # MVP specification documents
    ├── 🏗️ design/                   # MVP design documents
    │   ├── 🏛️ architecture/         # Architecture design documents
    │   ├── 🧩 modules/              # Module detailed design
    │   └── 📝 requirements/         # Requirements analysis documents
    ├── 📅 plan/                     # MVP development plan
    ├── 🔍 review/                   # MVP review documents
    └── 🧪 test/                     # MVP test documents
```

---

## 🚀 Deployment - Deployment Documentation

Deployment related documents, including Docker containerized deployment and environment configuration instructions.

### Document List:
- [📄 Dockerfile](deployment/Dockerfile) - Docker container build file
- [📄 docker-compose.yml](deployment/docker-compose.yml) - Docker Compose orchestration file
- [📄 Environment Variable Configuration Guide.md](deployment/环境变量配置说明.md) - Detailed environment variable configuration instructions
- [📄 Deployment Operations Guide.md](deployment/部署运维文档.md) - Complete deployment and operations guide

---

## 📋 Spec - MVP Specification Documents

### 🏗️ Design - MVP Design Documents

#### 🏛️ Architecture - Architecture Design
System-level architecture design and MVP boundary definition documents.

- [📄 ClashSubManager-MVP Outline Design.md](spec/design/architecture/ClashSubManager-MVP概要设计.md) - **Core Document** - MVP overall architecture design and technical solution
- [📄 MVP Core Features.md](spec/design/architecture/MVP核心功能.md) - Core feature definitions and implementation logic
- [📄 MVP Boundary Definition.md](spec/design/architecture/MVP边界定义.md) - MVP scope boundaries and excluded features

#### 🧩 Modules - Module Detailed Design
Detailed design documents for each functional module, containing specific implementation details.

- [📄 Admin Authentication-MVP Detailed Design.md](spec/design/modules/管理员认证-MVP详细设计.md) - Admin authentication system detailed design
- [📄 Clash Template-MVP Detailed Design.md](spec/design/modules/Clash模板-MVP详细设计.md) - Clash configuration template management design
- [📄 IP Management-MVP Detailed Design.md](spec/design/modules/IP管理-MVP详细设计.md) - Preferred IP management feature design
- [📄 Subscription API-MVP Detailed Design.md](spec/design/modules/订阅API-MVP详细设计.md) - Subscription interface detailed design

#### 📝 Requirements - Requirements Analysis
Client and server requirements analysis documents.

- [📄 Client Requirements.md](spec/design/requirements/客户端需求.md) - Client extension feature requirements (non-MVP core)
- [📄 Server Requirements.md](spec/design/requirements/服务端需求.md) - Server core feature requirements

### 📅 Plan - MVP Development Plan

- [📄 ClashSubManager-MVP Development Plan.md](spec/plan/ClashSubManager-MVP开发计划.md) - **Core Document** - Complete MVP development plan and task breakdown

### 🔍 Review - MVP Review Documents

*No documents yet - MVP review reports to be added later*

### 🧪 Test - MVP Test Documents

- [📄 ClashSubManager-MVP Unit Test Design.md](spec/test/ClashSubManager-MVP单元测试设计.md) - Unit test design and test cases
- [📄 cloudflare-ip-test.csv](spec/test/cloudflare-ip-test.csv) - Test data file

---

## 🎯 AI Agent Usage Guide

### Recommended Development Sequence:

1. **📖 First Read**:
   - [ClashSubManager-MVP Outline Design.md](spec/design/architecture/ClashSubManager-MVP概要设计.md) - Understand overall architecture
   - [ClashSubManager-MVP Development Plan.md](spec/plan/ClashSubManager-MVP开发计划.md) - Understand development tasks

2. **🔧 Module Development**:
   - Develop according to priority in [Development Plan](spec/plan/ClashSubManager-MVP开发计划.md)
   - Reference corresponding [Module Detailed Design](spec/design/modules/) documents for implementation

3. **🧪 Testing and Validation**:
   - Write tests referencing [Unit Test Design](spec/test/ClashSubManager-MVP单元测试设计.md)
   - Use [test data](spec/test/cloudflare-ip-test.csv) for validation

4. **🚀 Deployment**:
   - Deploy referencing [Deployment Operations Guide](deployment/部署运维文档.md)
   - Configure [environment variables](deployment/环境变量配置说明.md)

### 📚 Document Priority:

- **🔴 Must Read**: MVP outline design, development plan
- **🟡 Important**: Module detailed design, test design
- **🟢 Reference**: Requirements documents, deployment documents

---

## 📝 Documentation Maintenance

### Documentation Standards:
- All documents follow MVP minimum verifiable principle
- Document content is concise, containing only information essential for AI Agent implementation
- Use standard Markdown format for easy reading and maintenance

### Update Instructions:
- MVP version iteration: Update related design and plan documents
- New modules: Add to corresponding module design directory
- Testing improvements: Update test documents and test cases

---

**Document Version**: v1.0  
**Created**: 2026-01-21  
**Maintainer**: AI Agent Software Engineer  
**Scope**: ClashSubManager MVP Development Project