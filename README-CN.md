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
- **管理员界面**：完整的Web管理系统，支持IP配置、模板管理和用户设置
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
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=your_password \
  -e COOKIE_SECRET_KEY=your_32_char_secret_key \
  -v $(pwd)/data:/app/data \
  clashsubmanager:latest
```

### 📁 目录结构
```
ClashSubManager/
├── server/          # 服务器端应用
├── client/          # 客户端脚本
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
- 用户专属配置管理

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
└── [userId]/            # 用户专属配置
    ├── cloudflare-ip.csv
    └── clash.yaml
```

### 🎛️ 环境变量
| 变量名 | 说明 | 默认值 |
|--------|------|--------|
| `ADMIN_USERNAME` | 管理员用户名 | 必填 |
| `ADMIN_PASSWORD` | 管理员密码 | 必填 |
| `COOKIE_SECRET_KEY` | Cookie密钥 | 必填(≥32字符) |
| `SESSION_TIMEOUT_MINUTES` | 会话超时时间 | 60 |
| `DATA_PATH` | 数据目录 | `/app/data` |

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

## 许可证

本项目采用 [GPL-3.0 许可证](LICENSE) 开源。
版权所有 (c) 2026 Soren S.