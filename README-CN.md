# ClashSubManager

Clash订阅配置管理器 - 轻量级Clash订阅代理服务

## 项目简介

ClashSubManager是一个轻量级的Clash订阅配置管理服务，作为Clash客户端与订阅服务之间的中间层，提供订阅数据的动态覆写和个性化配置功能。

## 核心功能

### 🎯 主要特性
- **统一订阅入口**：通过 `/sub/[用户id]` 提供标准化订阅接口
- **动态配置覆写**：完全动态解析和合并Clash配置，支持未来版本兼容
- **优选IP扩展**：自动将域名代理扩展为多个优选IP地址代理
- **个性化配置**：支持用户专属配置与默认配置的灵活切换
- **管理员界面**：Web管理系统，支持默认/用户优选IP与Clash模板管理
- **国际化支持**：完整的中英文界面支持
- **轻量化架构**：单体应用，资源占用极小

### 🔧 技术栈
- **.NET 10** - 主开发框架
- **ASP.NET Core Razor Pages** - Web开发模式
- **Bootstrap** - 前端UI框架
- **Docker** - 容器化部署

## 快速开始

### 🐳 Docker部署
```bash
# 拉取镜像
docker pull clashsubmanager:latest

# 运行容器
docker run -d \
  -p 8080:80 \
  -e AdminUsername=admin \
  -e AdminPassword=your_password \
  -e CookieSecretKey=your_32_char_secret_key \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  -e SessionTimeoutMinutes=30 \
  -e DataPath=/app/data \
  -v $(pwd)/data:/app/data \
  clashsubmanager:latest
```

### 📁 目录结构
```
ClashSubManager/
├── server/          # 服务器端应用
├── doc/            # 项目文档
└── README.md       # 项目说明
```

## 使用指南

### 📱 Clash客户端配置
在Clash客户端中配置订阅URL：
```
http://your-server:8080/sub/your_user_id
```

### ⚙️ 管理界面
访问 `http://your-server:8080/admin` 进行配置管理：
- 优选IP管理
- Clash模板管理  
- 用户列表管理（访问自动记录）

### 🔄 API接口
- `GET /sub/{id}` - 获取用户Clash订阅配置
- `POST /sub/{id}` - 更新用户优选IP配置
- `DELETE /sub/{id}` - 删除用户优选IP配置

## 配置说明

### 📊 数据存储
```
/app/data/
├── cloudflare-ip.csv     # 默认优选IP
├── clash.yaml           # 默认Clash模板
├── users.txt            # 用户访问记录
└── [userId]/            # 用户专属配置
    ├── cloudflare-ip.csv
    └── clash.yaml
```

### 🎛️ 环境变量
| 变量名 | 说明 | 默认值 |
|--------|------|--------|
| `AdminUsername` | 管理员用户名 | 必填 |
| `AdminPassword` | 管理员密码 | 必填 |
| `CookieSecretKey` | Cookie密钥 | 必填(≥32字符) |
| `SessionTimeoutMinutes` | 会话超时时间 | 60 |
| `DataPath` | 数据目录（绝对路径或相对可执行文件路径） | 独立模式`./data` / Docker`/app/data` |
| `SubscriptionUrlTemplate` | 上游订阅URL模板（必须包含`{userId}`） | 可选（兜底） |
| `SUBSCRIPTION_URL_TEMPLATE` | 上游订阅URL模板（覆盖`SubscriptionUrlTemplate`） | 必填 |
| `LOG_LEVEL` | 日志级别 | 可选 |

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

#### Docker 部署（推荐）
```bash
docker run -d \
  -e AdminUsername=admin \
  -e AdminPassword=your_password \
  -e CookieSecretKey=your_32_character_minimum_key \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  -e SessionTimeoutMinutes=30 \
  -e DataPath=/app/data \
  -p 8080:80 \
  clash-sub-manager
```

#### 独立运行
```bash
# Windows
./ClashSubManager.exe

# Linux/macOS
./ClashSubManager

# 自定义数据路径
./ClashSubManager --DataPath /custom/data/path
```

#### 配置文件
创建 `appsettings.User.json` 文件：
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

详细文档请查看 `doc/` 目录：
- [MVP概要设计](doc/spec/design/architecture/mvp-outline-cn.md)
- [部署运维文档](doc/deployment/deployment-guide-cn.md)
- [环境变量配置](doc/deployment/env-config-cn.md)

## 贡献

欢迎提交Issue和Pull Request来改进项目。

## 支持项目

如果这个项目对您有帮助，请考虑支持项目开发：

[![通过加密货币支持](https://img.shields.io/badge/Donate-Crypto-yellow?style=for-the-badge)](DONATE-CN.md)

查看 [DONATE-CN.md](DONATE-CN.md) 了解加密货币捐赠地址。

## 许可证

本项目采用 [GPL-3.0 许可证](LICENSE) 开源。
版权所有 (c) 2026 Soren S.