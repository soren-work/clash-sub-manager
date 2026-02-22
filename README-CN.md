# ClashSubManager

**中文** | [English](README.md)

Clash订阅配置管理器 - 为你的Clash订阅添加优选IP和自定义配置

## 目录

- [这是什么？](#这是什么)
- [为什么需要它？](#为什么需要它)
- [工作原理](#工作原理)
- [核心功能](#核心功能)
- [快速开始](#快速开始)
- [使用指南](#使用指南)
- [配置说明](#配置说明)
- [配置系统](#配置系统)
- [性能特性](#性能特性)
- [安全特性](#安全特性)
- [文档](#文档)
- [贡献](#贡献)
- [支持项目](#支持项目)
- [许可证](#许可证)

## 这是什么？

ClashSubManager 是一个**订阅代理服务**，它位于你的 Clash 客户端和原始订阅服务之间，帮你自动处理和优化订阅配置。

简单来说：
```
原始订阅 → ClashSubManager 处理 → 优化后的订阅 → Clash 客户端
```

## 为什么需要它？

### 解决的问题

1. **优选IP替换**：你的订阅中有 `cdn.example.com` 这样的域名节点，但你想用测速后的优选IP（如 `104.29.125.182`）来替代，提升连接速度
2. **批量节点生成**：一个域名节点自动扩展为多个优选IP节点，无需手动编辑配置
3. **个性化配置**：在不修改原始订阅的情况下，添加自己的规则、代理组等配置
4. **统一管理**：通过Web界面管理优选IP列表和Clash模板，无需手动编辑YAML文件

### 使用场景

- 你有一个机场订阅，但想用 Cloudflare 优选IP来加速
- 你想为不同设备使用不同的优选IP配置
- 你想在订阅基础上添加自己的规则和代理组
- 你想集中管理多个用户的订阅配置

## 工作原理

### 数据流转过程

```
1. Clash客户端请求订阅
   ↓
2. ClashSubManager 获取原始订阅
   ↓
3. 读取优选IP列表（cloudflare-ip.csv）
   ↓
4. 读取Clash模板配置（clash.yaml）
   ↓
5. 处理订阅：
   - 将域名节点扩展为多个优选IP节点
   - 合并模板配置（规则、代理组等）
   ↓
6. 返回处理后的配置给Clash客户端
```

### 配置扩展示例

**原始订阅节点：**
```yaml
proxies:
  - name: "US-Node"
    type: vmess
    server: cdn.example.com
    port: 443
  - name: "HK-Node"
    type: vmess
    server: 1.2.3.4
    port: 443
```

**优选IP列表（cloudflare-ip.csv）：**
```csv
IP Address,Average Latency
104.29.125.182,152.45ms
104.26.0.188,158.10ms
104.20.20.191,161.38ms
```

**处理后的节点：**
```yaml
proxies:
  # 域名节点：保留原节点 + 扩展优选IP
  - name: "US-Node"                      # 原域名节点（保留作为备用）
    type: vmess
    server: cdn.example.com
    port: 443
  - name: "US-Node [104.29.125.182]"     # 扩展的优选IP节点
    type: vmess
    server: 104.29.125.182
    port: 443
  - name: "US-Node [104.26.0.188]"
    type: vmess
    server: 104.26.0.188
    port: 443
  - name: "US-Node [104.20.20.191]"
    type: vmess
    server: 104.20.20.191
    port: 443
  
  # IP节点：保持不变
  - name: "HK-Node"                      # IP节点不进行扩展
    type: vmess
    server: 1.2.3.4
    port: 443
```

**扩展规则：**
- **域名节点**：自动扩展为 1个原域名节点（排第一位作为备用） + N个优选IP节点
- **IP节点**：保持不变，不进行扩展

## 核心功能

### 🎯 主要特性

- **统一订阅入口**：通过 `/sub/[用户id]` 提供标准化订阅接口
- **优选IP自动扩展**：自动将域名代理节点扩展为多个优选IP地址节点
- **配置动态合并**：将原始订阅与自定义模板（规则、代理组等）智能合并
- **多用户支持**：每个用户可以有独立的优选IP列表和Clash模板
- **Web管理界面**：可视化管理优选IP、Clash模板和用户列表
- **国际化支持**：完整的中英文界面支持
- **轻量化架构**：单体应用，资源占用极小（< 50MB内存）

### 🔧 技术栈

- **.NET 10** - 主开发框架
- **ASP.NET Core Razor Pages** - Web开发模式
- **Bootstrap** - 前端UI框架
- **Docker** - 容器化部署

## 快速开始

### 前置要求

- Docker（推荐）或 .NET 10 运行时
- 一个有效的Clash订阅地址

### 🐳 Docker部署（推荐）

**最小化配置：**
```bash
docker run -d \
  -p 8080:80 \
  -e AdminUsername=admin \
  -e AdminPassword=your_password \
  -e CookieSecretKey=your_32_character_minimum_secret_key_here \
  -e SUBSCRIPTION_URL_TEMPLATE=https://your-airport.com/sub/{userId} \
  -v $(pwd)/data:/app/data \
  --name clash-sub-manager \
  clashsubmanager:latest
```

**参数说明：**
- `AdminUsername`：管理员用户名（自定义）
- `AdminPassword`：管理员密码（自定义）
- `CookieSecretKey`：Cookie加密密钥（至少32个字符，随机生成）
- `SUBSCRIPTION_URL_TEMPLATE`：你的原始订阅地址模板，`{userId}` 会被替换为实际用户ID

**完整配置示例：**
```bash
docker run -d \
  -p 8080:80 \
  -e AdminUsername=admin \
  -e AdminPassword=MySecurePassword123 \
  -e CookieSecretKey=abcdef1234567890abcdef1234567890 \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  -e SessionTimeoutMinutes=30 \
  -e DataPath=/app/data \
  -v $(pwd)/data:/app/data \
  --name clash-sub-manager \
  --restart unless-stopped \
  clashsubmanager:latest
```

### 💻 独立运行

```bash
# Windows
./ClashSubManager.exe

# Linux/macOS
./ClashSubManager

# 自定义数据路径
./ClashSubManager --DataPath /custom/data/path
```

### 📁 项目结构

```
ClashSubManager/
├── server/          # 服务器端应用
├── doc/            # 项目文档
├── data/           # 数据目录（运行时创建）
│   ├── cloudflare-ip.csv    # 默认优选IP列表
│   ├── clash.yaml           # 默认Clash模板
│   ├── users.txt            # 用户访问记录
│   └── [userId]/            # 用户专属配置
└── README.md       # 项目说明
```

## 使用指南

### 第一步：启动服务

按照"快速开始"部分启动服务后，访问：
```
http://localhost:8080
```

### 第二步：登录管理界面

访问管理界面：
```
http://localhost:8080/admin
```

使用你设置的管理员账号密码登录。

### 第三步：配置优选IP列表

在管理界面中：
1. 点击"默认优选IP管理"
2. 上传或编辑你的优选IP列表（CSV格式）
3. CSV格式示例：
```csv
IP Address,Sent,Received,Packet Loss Rate,Average Latency,Download Speed
104.29.125.182,4,4,0.00%,152.45ms,0.00
104.26.0.188,4,4,0.00%,158.10ms,0.00
```

### 第四步：配置Clash模板（可选）

在管理界面中：
1. 点击"默认Clash模板管理"
2. 编辑你的Clash配置模板
3. 可以添加自定义规则、代理组等

### 第五步：在Clash客户端中使用

将Clash客户端的订阅地址改为：
```
http://your-server:8080/sub/your_user_id
```

例如：
```
http://localhost:8080/sub/user123
```

### 高级功能：用户专属配置

如果你想为某个用户设置独立的优选IP或模板：
1. 在管理界面的"用户列表"中找到该用户
2. 点击"管理"进入用户专属配置
3. 上传该用户的优选IP列表或Clash模板
4. 用户的订阅会优先使用专属配置，没有则使用默认配置

### 🔄 API接口

- `GET /sub/{id}` - 获取用户Clash订阅配置
- `POST /sub/{id}` - 更新用户优选IP配置
- `DELETE /sub/{id}` - 删除用户优选IP配置

## 配置说明

### 📊 数据存储结构

```
/app/data/
├── cloudflare-ip.csv     # 默认优选IP列表
├── clash.yaml           # 默认Clash模板
├── users.txt            # 用户访问记录（自动生成）
└── [userId]/            # 用户专属配置目录
    ├── cloudflare-ip.csv  # 用户专属优选IP
    └── clash.yaml         # 用户专属模板
```

### 🎛️ 环境变量

| 变量名 | 说明 | 默认值 | 示例 |
|--------|------|--------|------|
| `AdminUsername` | 管理员用户名 | 必填 | `admin` |
| `AdminPassword` | 管理员密码 | 必填 | `MyPassword123` |
| `CookieSecretKey` | Cookie加密密钥 | 必填(≥32字符) | `abcdef1234567890abcdef1234567890` |
| `SUBSCRIPTION_URL_TEMPLATE` | 原始订阅URL模板 | 必填 | `https://api.example.com/sub/{userId}` |
| `SessionTimeoutMinutes` | 会话超时时间（分钟） | 60 | `30` |
| `DataPath` | 数据目录路径 | Docker: `/app/data`<br>独立: `./data` | `/custom/path` |
| `LOG_LEVEL` | 日志级别 | `Information` | `Debug` |

**注意：**
- `SUBSCRIPTION_URL_TEMPLATE` 必须包含 `{userId}` 占位符
- `CookieSecretKey` 至少需要32个字符，建议使用随机字符串

## 配置系统

ClashSubManager 支持灵活的跨平台配置管理，支持多种配置方式：

### 配置优先级（从高到低）

1. **命令行参数** - 最高优先级
2. **环境变量** - 次优先级
3. **用户配置文件** - `appsettings.User.json`
4. **环境类型配置** - `appsettings.{EnvironmentType}.json`（例如 Docker/Standalone）
5. **默认配置文件** - `appsettings.json`
6. **代码默认值** - 最低优先级

### 环境自动检测

- **Docker环境**：自动检测容器环境，使用 `/app/data` 作为默认数据路径
- **独立模式**：Windows/Linux/macOS 直接运行，使用程序同目录下的 `./data` 路径
- **开发/生产环境**：基于 `ASPNETCORE_ENVIRONMENT` 变量自动识别

### 配置方式示例

#### 方式1：环境变量（Docker推荐）

```bash
docker run -d \
  -e AdminUsername=admin \
  -e AdminPassword=your_password \
  -e CookieSecretKey=your_32_character_minimum_key \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  -e SessionTimeoutMinutes=30 \
  -p 8080:80 \
  clash-sub-manager
```

#### 方式2：配置文件

在程序目录创建 `appsettings.User.json`：
```json
{
  "AdminUsername": "admin",
  "AdminPassword": "your_password",
  "CookieSecretKey": "your_32_character_minimum_key",
  "SessionTimeoutMinutes": 30,
  "DataPath": "/custom/data/path",
  "SubscriptionUrlTemplate": "https://api.example.com/sub/{userId}"
}
```

#### 方式3：命令行参数

```bash
./ClashSubManager \
  --AdminUsername admin \
  --AdminPassword your_password \
  --DataPath /custom/data/path
```

### 配置验证

系统启动时会自动验证以下必需配置：
- `AdminUsername` - 管理员用户名（必需）
- `AdminPassword` - 管理员密码（必需）
- `CookieSecretKey` - Cookie密钥（必需，最少32字符）
- `SessionTimeoutMinutes` - 会话超时时间（5-1440分钟）
- `DataPath` - 数据路径（必须可创建/可写）

### 语言切换

界面语言通过 `.AspNetCore.Culture` Cookie 控制（内置语言切换器写入），默认回退 `en-US`。

## 性能特性

- **响应时间**：< 100ms
- **并发处理**：10-50 请求/秒
- **内存占用**：< 50MB
- **文件限制**：CSV最大10MB，YAML最大1MB

## 安全特性

- Cookie安全设置（HttpOnly、Secure、SameSite=Strict）
- 会话超时机制
- 请求频率限制（每IP每秒10请求）
- 严格的数据格式验证
- Docker容器化部署

## 文档

### 📚 系统架构

- [系统架构概述](doc/spec/design/architecture/mvp-outline-cn.md)
- [核心功能说明](doc/spec/design/architecture/mvp-core-features-cn.md)
- [跨平台配置设计](doc/spec/design/architecture/cross-platform-config-mvp-outline-cn.md)

### 🚀 部署与运维

- [部署运维指南](doc/deployment/deployment-guide-cn.md)
- [环境变量配置](doc/deployment/env-config-cn.md)
- [环境变量参考](doc/deployment/environment-variables-CN.md)

## 贡献

欢迎提交Issue和Pull Request来改进项目。

## 支持项目

如果这个项目对您有帮助，请考虑支持项目开发：

[![通过加密货币支持](https://img.shields.io/badge/Donate-Crypto-yellow?style=for-the-badge)](DONATE-CN.md)

查看 [DONATE-CN.md](DONATE-CN.md) 了解加密货币捐赠地址。

## 许可证

本项目采用 [GPL-3.0 许可证](LICENSE) 开源。
版权所有 (c) 2026 Soren S.