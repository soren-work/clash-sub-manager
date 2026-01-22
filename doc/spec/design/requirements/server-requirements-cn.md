# ClashSubManager MVP技术需求规格

**🌐 语言**: [English](server-requirements.md) | [中文](server-requirements-cn.md)

## 1. MVP核心功能

### 1.1 用户订阅接口
- **GET /sub/[id]**: 返回完整Clash YAML配置
- **POST /sub/[id]**: 更新用户专属优选IP（CSV格式）
- **DELETE /sub/[id]**: 删除用户专属优选IP
- **验证机制**: 所有操作前必须通过订阅服务验证用户ID

### 1.2 管理员功能
- 认证系统：基于Docker环境变量的管理员登录
- 默认优选IP管理：可视化界面管理`/app/data/cloudflare-ip.csv`
- Clash模板管理：可视化界面管理`/app/data/clash.yaml`
- 用户配置管理：查看/删除用户专属配置
- **智能数据渲染**：按CloudflareST格式解析数据，解析失败时仅显示IP地址

### 1.3 明确排除的功能
- 用户注册系统
- 复杂权限管理
- 数据库存储
- 分布式架构
- 高级统计分析

## 2. 技术架构

### 2.1 核心架构
- **.NET 10 + ASP.NET Core Razor Pages**
- **单体应用架构**：严禁前后端分离
- **Docker容器化部署**

### 2.2 存储结构
```
/app/data/
├── cloudflare-ip.csv          # 默认优选IP
├── clash.yaml                 # 默认Clash模板
├── users.txt                  # 用户记录
└── [用户id]/
    ├── cloudflare-ip.csv      # 用户专属优选IP
    └── clash.yaml             # 用户专属模板
```

## 3. 实施边界

### 3.1 AI Agent开发范围
- 用户订阅接口（GET/POST/DELETE /sub/[id]）
- 管理员认证系统（Cookie会话管理）
- 管理员界面（Razor Pages）
- 文件操作（CSV/YAML读写）
- 订阅服务集成（HTTP请求）

### 3.2 技术约束条件
- **.NET 10 + ASP.NET Core Razor Pages**
- **Bootstrap前端框架**
- **单体应用架构**：严禁前后端分离
- **文件存储**：仅使用本地文件系统
- **部署方式**：Docker容器化

### 3.3 验收标准
- API响应时间 < 100ms
- 内存占用 < 50MB
- 支持10-50并发请求/秒
- 管理界面功能完整
- 文件操作并发安全

## 4. 核心接口规范

### 4.1 用户订阅接口
```
GET /sub/[id]     # 获取订阅（返回YAML）
POST /sub/[id]    # 更新优选IP（接收CSV）
DELETE /sub/[id]  # 删除优选IP
```

### 4.2 GET /sub/[id] 处理流程
1. **验证用户ID**：携带clash user-agent调用订阅服务验证ID
2. **获取基础订阅**：调用订阅服务API获取用户原始订阅
3. **配置优先级**：用户专属配置 > 默认配置 > 原始数据
4. **优选IP处理**：扩展proxies部分为Cloudflare优选IP
5. **模板注入**：注入proxy-groups、rules、dns策略
6. **兜底机制**：无配置时直接返回订阅服务原始数据
7. **返回YAML**：Content-Type: text/yaml

### 4.3 数据覆写兼容性要求
**核心原则：** 对原始订阅服务数据进行完全灵活的兼容性处理，严禁硬编码字段处理。

#### 4.3.1 动态字段解析
- **clash.yaml模板字段**：完整包含模板文件中的所有字段，动态解析并注入
- **订阅服务返回字段**：完整保留订阅服务返回的所有字段，不得遗漏
- **字段合并策略**：模板字段优先于订阅字段，相同字段时模板覆盖订阅

#### 4.3.2 严禁硬编码处理
- **禁止字段猜测**：不得在代码中预设、猜测任何字段名称或结构
- **禁止字段限制**：不得限制或过滤任何配置字段
- **动态解析机制**：必须使用动态解析机制处理所有未知字段

#### 4.3.3 兼容性实现要求
- **完整字段保留**：订阅服务返回的所有字段必须完整保留到最终输出
- **模板字段注入**：clash.yaml中的所有字段必须按原结构注入
- **字段冲突处理**：相同字段名时，模板字段覆盖订阅字段
- **新增字段支持**：自动支持未来clash版本新增的任何字段

### 4.4 POST /sub/[id] 数据格式
- **输入格式**: 自由文本格式，支持CloudflareST输出或其他格式
- **验证规则**: 
  - 每行最多包含1个IP地址
  - 每次提交至少包含1个IP地址
  - 使用正则表达式提取和验证IP地址
- **Content-Type**: text/csv 或 text/plain
- **存储策略**: 
  - 完整保存原始提交数据到 `/app/data/[用户id]/cloudflare-ip.csv`
  - 管理界面智能解析：按行解析CloudflareST格式数据
  - 解析失败时仅显示IP地址，解析成功时显示完整数据

### 4.5 响应格式
- **成功GET**: `Content-Type: text/yaml`，返回完整YAML配置
- **成功POST/DELETE**: `Content-Type: application/json`
```json
{"success": true, "message": "操作描述"}
```
- **错误**: `Content-Type: application/json`
```json
{"success": false, "message": "操作描述"}
```

#### 标准错误码
- `400`: 请求参数错误
- `401`: 用户ID验证失败
- `403`: 权限不足（管理员功能）
- `404`: 用户或资源不存在
- `429`: 请求频率超限
- `500`: 服务器内部错误

### 4.6 数据验证
- 用户ID: 1-64字符，支持`[a-zA-Z0-9_-]`
- IP地址提交: 每行最多1个IP，每次至少1个IP，正则表达式验证
- 文本文件: 最大10MB，1000行
- YAML模板: 最大1MB
- 并发限制: 每IP每秒10请求

## 5. 关键实现要点

### 5.1 文件操作安全
- 使用`FileStream`+`FileShare.None`独占写入
- 原子写入：临时文件+替换模式
- 并发安全：避免写入冲突

### 5.2 内存优化
- 使用`ArrayPool<T>`和`MemoryPool<T>`
- 配置`GCSettings.LatencyMode`为`LowLatency`
- 禁用不必要的ASP.NET Core服务
- 定期释放大型对象和文件流

### 5.3 安全要求
- 管理员认证：Docker环境变量配置
- Cookie安全：HttpOnly、Secure、SameSite=Strict
- 会话超时：5-1440分钟可配置
- 输入验证：严格的数据格式验证
