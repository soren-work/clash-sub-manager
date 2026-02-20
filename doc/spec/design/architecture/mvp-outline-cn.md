# ClashSubManager MVP概要设计

> **📌 文档状态**: MVP已完成，本文档作为架构参考保留  
> **🎯 目标读者**: 开发者、贡献者、架构研究者  
> **📅 最后更新**: 2026-02-20  
> **💡 提示**: 如需了解功能使用，请参阅[高级指南](../../../advanced-guide-cn.md)

**🌐 语言**: [English](mvp-outline.md) | [中文](mvp-outline-cn.md)

## 1. MVP核心功能

### 1.1 核心价值验证点
- 统一订阅入口：`/sub/[id]` 格式
- 配置拼装：动态合并原始订阅、优选IP、Clash模板
- 个性化支持：用户专属配置与默认配置切换
- 轻量化架构：单体应用，最小化资源占用

### 1.2 最小功能集
- 用户订阅接口（GET/POST/DELETE /sub/[id]）
- 管理员认证（环境变量 + Cookie）
- 优选IP管理（CSV格式）
- Clash模板管理（YAML格式）
- 用户专属配置管理
- **国际化支持**：中英文语言切换

### 1.3 明确排除的功能
- 用户管理系统
- 计费功能
- 复杂权限系统
- 数据库存储
- 微服务架构

## 2. 技术架构

### 2.1 核心架构图
```
ClashSubManager/
├── server/                  # 服务器端应用
│   ├── ClashSubManager.csproj
│   ├── Program.cs
│   ├── Pages/               # Razor Pages
│   ├── Services/            # 业务服务
│   ├── Models/              # 数据模型
│   └── wwwroot/             # 静态资源
└── doc/                     # 文档目录
```

### 2.2 技术选型（仅MVP相关）
- **.NET 10**：主开发框架
- **ASP.NET Core Razor Pages**：Web开发模式
- **Bootstrap**：前端框架
- **Docker**：容器化部署
- **ASP.NET Core Localization**：国际化支持

### 2.3 部署方案（简化版）
- 单体Docker容器部署
- 环境变量配置管理员账户
- 数据持久化到 `/app/data` 目录



## 3. 实施边界

### 3.1 AI Agent开发范围
- 用户订阅接口实现
- 管理员认证中间件
- 优选IP管理界面
- Clash模板管理界面
- 用户专属配置管理

### 3.2 技术约束条件
- 严禁前后端分离架构
- 仅使用本地文件存储
- 函数长度不超过50行
- 嵌套限制最多3层
- 常驻内存<50MB
- 响应时间<100ms
- 并发处理：10-50请求/秒

### 3.3 数据处理兼容性要求
#### 3.3.1 完全动态解析原则
- **严禁硬编码**：不得在代码中预设任何字段名称或结构
- **字段完整性**：完整保留订阅服务和模板文件的所有字段
- **未来兼容性**：自动支持未来Clash版本新增的任何字段
- **动态合并策略**：模板字段优先于订阅字段，相同字段时覆盖

#### 3.3.2 兼容性实现要求
- **完整字段保留**：订阅服务返回的所有字段必须完整保留到最终输出
- **模板字段注入**：clash.yaml中的所有字段必须按原结构注入
- **字段冲突处理**：相同字段名时，模板字段覆盖订阅字段
- **无字段限制**：不得对任何配置字段进行限制、过滤或预设处理

### 3.4 性能与安全约束
#### 3.4.1 性能要求
- 响应时间：<100ms（低并发场景）
- 并发处理：10-50请求/秒
- 常驻内存：<50MB
- 文件处理：CSV最大10MB，YAML最大1MB
- 启动时间：<30秒

#### 3.4.2 安全要求
- Cookie安全：HttpOnly、Secure、SameSite=Strict
- 会话超时：5-1440分钟可配置
- 请求限制：每IP每秒最多10个请求
- 输入验证：严格的数据格式验证
- 管理员认证：Docker环境变量配置

#### 3.4.3 国际化要求
- **默认语言**：英文
- **支持语言**：英文、中文（简体）
- **语言检测**：自动检测浏览器语言
- **语言切换**：管理界面手动语言切换
- **内容范围**：所有管理界面支持国际化
- **排除项**：控制台日志、代码注释、系统信息仅使用英文

### 3.3 验收标准
- 用户订阅接口正常工作
- 管理员认证功能正常
- 配置文件CRUD操作正常
- 符合性能约束条件
- **语言切换正常工作**：默认英文，自动检测中文，手动切换功能正常
- **所有管理界面支持双语**：完整的UI本地化覆盖

## 4. 核心接口定义

### 4.1 用户订阅接口
- **GET /sub/{id}**：获取用户Clash订阅配置
- **POST /sub/{id}**：更新用户优选IP配置
- **DELETE /sub/{id}**：删除用户优选IP配置

### 4.2 GET /sub/[id] 处理流程
1. **用户验证**：携带Clash User-Agent请求真实订阅地址验证ID
2. **获取订阅**：调用订阅服务API获取用户原始订阅数据
3. **配置加载**：按优先级加载用户专属→默认配置文件
4. **动态合并**：完全动态解析并合并所有字段（模板优先）
5. **IP扩展**：基于proxies进行IP地址扩展
6. **兜底机制**：无配置时直接返回订阅服务原始数据
7. **返回YAML**：Content-Type: text/yaml

#### 4.2.1 用户ID验证机制
- 携带clash的user-agent请求`GET [真实订阅地址]/[用户id]`
- 验证成功：返回yaml文档且HTTP状态码为200
- 验证失败：返回其他内容则用户ID错误

#### 4.2.2 数据覆写范围
1. **proxies智能扩展**：
   - **智能识别**：检测原始`proxies`中每个节点的`server`属性类型
   - **IP地址节点**：当`server`为IP地址时，用cloudflare优选IP替换
   - **域名节点**：当`server`为域名时，保留原始节点不变
   - **无server节点**：保留原始节点不变
   - **深复制保证**：使用递归深复制确保每个节点完全独立

2. **yaml结构扩展**：读取`clash.yaml`模板，以模板内容为优先项，添加和替换到原内容中

### 4.3 管理界面路由
- `/admin/login` - 登录页面
- `/admin` - 管理面板首页
- `/admin/default-ips` - 优选IP管理
- `/admin/clash-templates` - 模板管理
- `/admin/users/config` - 用户配置管理

### 4.3.1 国际化实现
#### 语言检测机制
```csharp
// 自动浏览器语言检测
var language = Request.GetBrowserLanguage();
if (language.StartsWith("zh")) {
    CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
} else {
    CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");
}
```

#### 语言切换界面
- 管理界面头部语言切换按钮
- 基于Cookie的语言偏好存储
- 无需页面刷新的即时UI更新

#### 资源文件结构
```
Resources/
├── Pages/
│   ├── Admin/
│   │   ├── Index.en.resx
│   │   ├── Index.zh-CN.resx
│   │   ├── Login.en.resx
│   │   └── Login.zh-CN.resx
│   └── Shared/
│       ├── _Layout.en.resx
│       └── _Layout.zh-CN.resx
```

#### 支持语言范围
- **完全支持**：所有管理界面页面（登录、仪表板、IP管理、模板管理、用户配置）
- **部分支持**：错误消息、验证消息、工具提示
- **不支持**：控制台日志、代码注释、系统内部消息（仅英文）

### 4.4 响应格式规范
#### 4.4.1 标准响应格式
- **成功POST/DELETE**：`Content-Type: application/json`
  ```json
  {"success": true, "message": "操作描述"}
  ```
- **错误响应**：`Content-Type: application/json`
  ```json
  {"success": false, "message": "操作描述"}
  ```

#### 4.4.2 特殊响应
- **GET /sub/[id]**：`Content-Type: text/yaml`，返回完整YAML配置

#### 4.4.3 标准错误码
- `400`：请求参数错误
- `401`：用户ID验证失败
- `403`：权限不足（管理员功能）
- `404`：用户或资源不存在
- `429`：请求频率超限
- `500`：服务器内部错误

### 4.3 数据流设计
```
用户请求 → 用户验证 → 获取原始订阅 → 加载优选IP → 加载模板 → 动态合并 → 返回YAML
```

## 5. 数据存储结构

### 5.1 文件存储结构
```
/app/data/
├── cloudflare-ip.csv     # 默认优选IP
├── clash.yaml           # 默认Clash模板
├── users.txt            # 用户记录
└── [userId]/            # 用户专属配置
    ├── cloudflare-ip.csv
    └── clash.yaml
```

### 5.2 数据格式规范
- **CSV格式**：优选IP配置，支持CloudflareST输出格式
- **YAML格式**：Clash配置模板，完全动态解析
- **TXT格式**：用户访问记录

### 5.3 数据验证规则
- **用户ID**：1-64字符，支持[a-zA-Z0-9_-]
- **IP地址提交**：每行最多1个IP，每次至少1个IP，正则表达式验证
- **CSV文件**：最大10MB，支持CloudflareST格式或纯IP格式
- **YAML模板**：最大1MB，无字段限制
- **并发限制**：每IP每秒10请求

### 5.4 配置优先级
1. 用户专属配置（优先）
2. 默认配置（兜底）
3. 原始订阅（基础）

### 5.5 文件操作安全
- 使用`FileStream`+`FileShare.None`独占写入
- 原子写入：临时文件+替换模式
- 并发安全：避免写入冲突





