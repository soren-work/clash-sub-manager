# ClashSubManager 跨平台配置管理MVP概要设计

**🌐 语言**: [English](./cross-platform-config-mvp-outline.md) | [中文](./cross-platform-config-mvp-outline-cn.md)

## 1. MVP核心功能

### 1.1 核心价值验证点
- **统一配置管理**：所有平台使用单一配置系统
- **环境自动检测**：自动识别Docker/独立运行模式
- **灵活数据路径**：用户可配置数据存储位置
- **跨平台兼容性**：支持Windows/Linux/macOS

### 1.2 最小功能集
- **配置服务**：具有优先级覆盖的统一配置管理
- **环境检测**：自动Docker/独立环境检测
- **路径解析**：跨平台数据路径配置
- **配置验证**：启动时配置验证

### 1.3 明确排除的功能
- GUI配置工具
- 配置迁移工具
- 高级配置模板
- 远程配置管理

## 2. 技术架构

### 2.1 核心架构图
```
应用启动
├── 环境检测
│   ├── Docker环境检查
│   └── 平台检测（Windows/Linux/macOS）
├── 配置加载
│   ├── 基础配置（appsettings.json）
│   ├── 环境特定配置
│   ├── 用户配置（appsettings.User.json）
│   ├── 环境变量
│   └── 命令行参数
├── 配置验证
└── 服务注册
```

### 2.2 技术选型（仅MVP相关）
- **.NET 10**：核心框架
- **Microsoft.Extensions.Configuration**：配置管理
- **System.IO**：跨平台文件操作
- **System.Environment**：环境变量访问

### 2.3 部署方案（简化版）
- **Docker模式**：保持现有 `/app/data` 路径
- **独立模式**：使用exe同目录下的 `./data`
- **配置覆盖**：支持所有设置的环境变量覆盖

## 3. 实施边界

### 3.1 AI Agent开发范围
- 实现 `IConfigurationService` 接口
- 创建 `EnvironmentDetector` 工具类
- 添加配置验证逻辑
- 更新 `FileService` 使用新配置系统
- 修改 `Program.cs` 实现统一配置加载

### 3.2 技术约束条件
- 必须保持与现有Docker部署的向后兼容性
- 配置验证必须阻止无效配置的应用启动
- 所有配置变更必须尽可能支持热重载
- 跨平台路径处理必须使用 `Path.Combine()` 保证兼容性

### 3.3 验收标准
- 应用在Docker和独立模式下均能成功启动
- 数据路径可通过环境变量或配置文件配置
- 配置验证捕获并报告所有关键配置错误
- 现有Docker部署无需修改即可继续工作
- Windows/Linux/macOS独立执行使用默认配置正常工作

## 4. 配置优先级系统

### 4.1 优先级顺序（从高到低）
1. 命令行参数
2. 环境变量
3. 用户配置文件（`appsettings.User.json`）
4. 环境特定配置（`appsettings.{Environment}.json`）
5. 模式特定配置（`appsettings.{Mode}.json`）
6. 默认配置文件（`appsettings.json`）
7. 代码默认值

### 4.2 核心配置项
- `DataPath`：数据存储目录路径
- `AdminUsername`：管理员用户名
- `AdminPassword`：管理员密码
- `CookieSecretKey`：Cookie签名密钥（≥32字符）
- `SessionTimeoutMinutes`：会话超时时间（分钟）
- `RateLimitPerIp`：每IP请求频率限制
- `LogLevel`：日志级别
- `AllowedHosts`：允许绑定的主机

### 4.3 配置验证规则
- `AdminUsername`：不能为空
- `AdminPassword`：不能为空
- `CookieSecretKey`：必须至少32字符
- `DataPath`：必须是可创建/可写的目录
- `SessionTimeoutMinutes`：必须在5-1440之间

---

## 语言版本
- [English](./cross-platform-config-mvp-outline.md) | [中文](./cross-platform-config-mvp-outline-cn.md)
