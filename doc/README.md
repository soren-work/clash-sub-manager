# ClashSubManager Project Documentation

**🌐 Language**: [English](README.md) | [中文](README-CN.md)

---

## 📖 Project Overview

### What is this project?

ClashSubManager is an intelligent Clash subscription management service that helps you better manage and optimize proxy subscriptions. It can:

- **Aggregate multiple subscription sources**: Merge multiple airport subscriptions into a unified subscription link
- **Preferred IP replacement**: Automatically replace node IPs with Cloudflare preferred IPs to improve connection speed
- **Custom configuration**: Support custom Clash rules, proxy groups, and node naming
- **Multi-user management**: Provide dedicated configurations for different users without interference

### Core Features

- ✅ **Subscription aggregation**: Support multiple upstream subscription sources, automatically merge nodes
- ✅ **IP optimization**: Based on CloudflareST speed test results, replace with optimal IPs
- ✅ **Template system**: Flexible Clash configuration templates, support custom rules and proxy groups
- ✅ **User isolation**: Independent configuration for each user, support batch management
- ✅ **Web management**: Simple management interface, no need to manually edit configuration files

### Target Audience

- 🎯 **Regular users**: Want faster proxy speed without complex configuration
- 🎯 **Advanced users**: Need fine-grained control over rule splitting and proxy group strategies
- 🎯 **Multi-user sharing**: Family/team sharing subscriptions, need dedicated configurations for different members
- 🎯 **Developers**: Want to self-host subscription service or develop based on this project

---

## 🚀 Quick Start Paths

Choose the reading path according to your use case:

### 👤 I'm a Regular User - Just Want to Use

**Goal**: Quickly deploy and use the subscription service

1. 📖 Read [Main README](../README.md) - Understand basic concepts and features
2. 🚀 Refer to [Deployment Guide](deployment/deployment-guide.md) - Quick deployment
3. ⚙️ Check [Environment Variable Configuration](deployment/env-config.md) - Configure your subscription sources
4. 💡 Having issues? Check [FAQ](FAQ.md) (to be added)

**Estimated time**: 30 minutes

---

### 🔧 I'm a Developer - Want to Deploy

**Goal**: Deploy and operate in production environment

1. 📖 Read [Main README](../README.md) - Understand project architecture
2. 🏗️ Read [MVP Outline Design](spec/design/architecture/mvp-outline.md) - Understand system architecture
3. 🚀 Refer to [Deployment Guide](deployment/deployment-guide.md) - Production deployment
4. ⚙️ Check [Environment Variable Configuration](deployment/env-config.md) - Security configuration and optimization
5. 📚 Read [Advanced Guide](advanced-guide.md) (to be added) - Performance optimization and troubleshooting

**Estimated time**: 2 hours

---

### 👨‍💻 I'm a Contributor - Want to Develop

**Goal**: Understand code structure and participate in project development

1. 📖 Read [Main README](../README.md) - Understand project background
2. 🏗️ Read [MVP Outline Design](spec/design/architecture/mvp-outline.md) - Understand overall architecture
3. 📅 Read [MVP Development Plan](spec/plan/mvp-development-plan.md) - Understand development tasks
4. 🧩 Check [Module Detailed Design](spec/design/modules/) - Deep dive into module implementations
5. 🧪 Refer to [Unit Test Design](spec/test/mvp-unit-test.md) - Write test cases

**Estimated time**: 4 hours

---

## 📚 Documentation Reading Guide

### Recommendations by Use Case

| Use Case | Must Read | Optional |
|---------|---------|---------|
| **Quick Usage** | Main README, Deployment Guide | Environment Variable Configuration |
| **Production Deployment** | Main README, Deployment Guide, Environment Configuration | Advanced Guide, Architecture Design |
| **Feature Customization** | Main README, Advanced Guide, Module Design | Architecture Design, Development Plan |
| **Secondary Development** | Architecture Design, Module Design, Development Plan | Test Design, Requirements Documents |

### Document Priority Labels

- 🔴 **Must Read** - All users should read
- 🟡 **Important** - Read selectively based on use case
- 🟢 **Reference** - Reference for in-depth research

---

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
- [📄 Environment Variable Configuration Guide.md](deployment/env-config.md) - Detailed environment variable configuration instructions
- [📄 Deployment Operations Guide.md](deployment/deployment-guide.md) - Complete deployment and operations guide

---

## 📋 Spec - MVP Specification Documents

### 🏗️ Design - MVP Design Documents

#### 🏛️ Architecture - Architecture Design
System-level architecture design and MVP boundary definition documents.

- [📄 MVP Outline Design.md](spec/design/architecture/mvp-outline.md) - **Core Document** - MVP overall architecture design and technical solution
- [📄 MVP Core Features.md](spec/design/architecture/mvp-core-features.md) - Core feature definitions and implementation logic
- [📄 MVP Boundary Definition.md](spec/design/architecture/mvp-boundary.md) - MVP scope boundaries and excluded features

#### 🧩 Modules - Module Detailed Design
Detailed design documents for each functional module, containing specific implementation details.

- [📄 Admin Authentication-MVP Detailed Design.md](spec/design/modules/admin-auth-detail.md) - Admin authentication system detailed design
- [📄 Clash Template-MVP Detailed Design.md](spec/design/modules/clash-template-detail.md) - Clash configuration template management design
- [📄 IP Management-MVP Detailed Design.md](spec/design/modules/ip-management-detail.md) - Preferred IP management feature design
- [📄 Subscription API-MVP Detailed Design.md](spec/design/modules/subscription-api-detail.md) - Subscription interface detailed design

#### 📝 Requirements - Requirements Analysis
Client and server requirements analysis documents.

- [📄 Client Requirements.md](spec/design/requirements/客户端需求.md) - Client extension feature requirements (non-MVP core)
- [📄 Server Requirements.md](spec/design/requirements/服务端需求.md) - Server core feature requirements

### 📅 Plan - MVP Development Plan

- [📄 MVP Development Plan.md](spec/plan/mvp-development-plan.md) - **Core Document** - Complete MVP development plan and task breakdown

### 🔍 Review - MVP Review Documents

*No documents yet - MVP review reports to be added later*

### 🧪 Test - MVP Test Documents

- [📄 MVP Unit Test Design.md](spec/test/mvp-unit-test.md) - Unit test design and test cases
- [📄 cloudflare-ip-test.csv](spec/test/cloudflare-ip-test.csv) - Test data file

---

## 🎯 AI Agent Usage Guide

### Recommended Development Sequence:

1. **📖 First Read**:
   - [MVP Outline Design.md](spec/design/architecture/mvp-outline.md) - Understand overall architecture
   - [MVP Development Plan.md](spec/plan/mvp-development-plan.md) - Understand development tasks

2. **🔧 Module Development**:
   - Develop according to priority in [Development Plan](spec/plan/mvp-development-plan.md)
   - Reference corresponding [Module Detailed Design](spec/design/modules/) documents for implementation

3. **🧪 Testing and Validation**:
   - Write tests referencing [Unit Test Design](spec/test/mvp-unit-test.md)
   - Use [test data](spec/test/cloudflare-ip-test.csv) for validation

4. **🚀 Deployment**:
   - Deploy referencing [Deployment Operations Guide](deployment/deployment-guide.md)
   - Configure [environment variables](deployment/env-config.md)

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