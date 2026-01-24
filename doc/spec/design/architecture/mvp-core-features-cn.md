# ClashSubManager 核心功能定义

**🌐 语言**: [English](mvp-core-features.md) | [中文](mvp-core-features-cn.md)

## 功能优先级分级

### 核心功能（优先级最高）
**功能描述：** 通过调用`GET /sub/[用户id]`接口，程序将调用转接到`GET [真实订阅地址]/[用户id]`获取数据，并对订阅地址返回的yaml数据进行全动态的覆写。

**用户ID验证：**
- 携带clash的user-agent请求`GET [真实订阅地址]/[用户id]`
- 验证成功：返回yaml文档且HTTP状态码为200
- 验证失败：返回其他内容则用户ID错误

**系统定位：** 作为clash客户端与订阅服务之间的补充层，将原本`clash客户端 -> 订阅服务`流程调整为`clash客户端 -> 本系统 -> 订阅服务`，对订阅服务返回内容进行定制化调整。

**兜底策略：** 当既无用户专属配置也无默认配置时，直接返回clash订阅接口的原始数据。

**依赖关系：** 支持任何提供clash订阅服务的后端。

**覆写范围：**
1. **proxies的智能扩展：** 检测原始`proxies`中每个节点的`server`属性类型，进行差异化处理：
   - **IP地址节点**：当`server`为IP地址时，用cloudflare优选IP替换
   - **域名节点**：当`server`为域名时，保留原始节点不变  
   - **无server节点**：保留原始节点不变
   - **深复制保证**：使用递归深复制确保每个节点完全独立，避免引用共享

2. **yaml文档结构的扩展：** 读取`clash.yaml`文件中定义好的模板与设定，以`clash.yaml`文件中的内容为优先项，以原内容为次优先项，将`clash.yaml`文件的内容添加和替换到原内容中

**兼容性要求：**
- **完全动态处理：** 必须动态解析和处理clash.yaml模板及订阅服务返回的所有字段，严禁硬编码任何字段名称或结构
- **字段完整性保证：** 完整保留订阅服务返回的所有字段，同时完整包含clash.yaml模板中的所有字段
- **无字段限制：** 不得对任何配置字段进行限制、过滤或预设处理
- **未来兼容性：** 自动支持未来clash版本新增的任何字段和配置项

### 扩展功能1（第二优先级）
**功能描述：** 系统具有管理员，可以通过环境变量（基于docker）中设置的管理员用户名和密码登录，登录后可以对`cloudflare-ip.csv`以及`clash.yaml`进行管理。

**存储位置：** `/app/data/cloudflare-ip.csv`和`/app/data/clash.yaml`

**管理方式：**
- 更新文件（不存在则创建）
- 删除文件（或者清空内容）
- 更新方式包含粘贴原始内容全文或者上传文件
- 不对文件内容进行校验

### 扩展功能2（第三优先级）
**功能描述：** 基于扩展功能1，支持管理员管理`/app/data/[用户id]/cloudflare-ip.csv`和`/app/data/[用户id]/clash.yaml`。

**优先级规则：** 这两份文件的应用优先级高于扩展功能1，即：若存在这两份文件，则不使用扩展功能1中的文件

### 扩展功能3（第三优先级）
**功能描述：** 为用户提供支持windows/linux/mac os的脚本，此脚本会自动调用`CloudflareST`程序，并将程序运行的结果文件`result.csv`以`POST`请求的形式提交到`/sub/[用户id]`接口。

**操作结果：** 此操作将创建/更新`/app/data/[用户id]/cloudflare-ip.csv`文件

### 扩展功能4（第四优先级）
**功能描述：** 对扩展功能3进行更新，支持以`DELETE`请求的形式调用`/sub/[用户id]`。

**操作结果：** 此操作将删除`/app/data/[用户id]/cloudflare-ip.csv`

## 系统配置管理

### 订阅URL配置
**全局配置方式：** 外部订阅URL通过Docker环境变量统一配置，每个项目实例对应一个外部订阅系统。

**环境变量：** `SUBSCRIPTION_URL_TEMPLATE`

**URL模板支持：**
- 路径参数替换：`http://www.domain.com/sub/{userId}`
- 查询参数替换：`http://www.domain.com/sub?userId={userId}`
- 固定URL：`http://www.domain.com/sub/abcdefghijkl`（不进行替换）

**替换机制：** 
- 系统自动将 `{userId}` 占位符替换为 `/sub/[id]` 接口接收的实际用户ID
- **重要**：替换后的URL已包含完整用户ID，验证时无需再次拼接用户ID
- **禁止重复拼接**：避免出现 `http://domain.com/sub/user123/user123` 的错误格式

**URL模板示例：**
```
# 路径参数模式
SUBSCRIPTION_URL_TEMPLATE=http://api.example.com/subscription/{userId}
# 替换后：http://api.example.com/subscription/user123

# 查询参数模式  
SUBSCRIPTION_URL_TEMPLATE=http://example.com/subscribe?user={userId}
# 替换后：http://example.com/subscribe?user=user123

# 固定URL模式
SUBSCRIPTION_URL_TEMPLATE=http://example.com/subscription/fixed
# 替换后：http://example.com/subscription/fixed
```

### 用户管理
**用户记录方式：** 仅在 `/app/data/users.txt` 中记录用户ID，支持去重处理。

**首次访问机制：** 用户第一次调用 `/sub/[id]` 时自动记录到users.txt文件中。

## 技术实现要点

### 文件存储结构
```
/app/data/
├── cloudflare-ip.csv          # 全局IP文件
├── clash.yaml                 # 全局配置文件
├── users.txt                  # 用户ID记录（自动去重）
└── [用户id]/
    ├── cloudflare-ip.csv      # 用户专属IP文件
    └── clash.yaml             # 用户专属配置文件
```

**配置简化说明：**
- 移除复杂的UserConfig JSON配置文件
- 专属IP配置直接从CSV文件读取
- 用户管理简化为ID记录和去重

### API接口设计
- `GET /sub/[用户id]` - 获取覆写后的订阅数据
- `POST /sub/[用户id]` - 提交CloudflareST结果
- `DELETE /sub/[用户id]` - 删除用户专属IP文件
- 管理员登录和管理接口

### 标准响应格式
- **成功响应**: `Content-Type: application/json`
  ```json
  {"success": true, "message": "操作描述"}
  ```
- **错误响应**: `Content-Type: application/json`
  ```json
  {"success": false, "message": "操作描述"}
  ```

#### 特殊响应
- **GET /sub/[id]**: `Content-Type: text/yaml`，返回完整YAML配置

#### 标准错误码
- `400`: 请求参数错误
- `401`: 用户ID验证失败
- `403`: 权限不足（管理员功能）
- `404`: 用户或资源不存在
- `429`: 请求频率超限
- `500`: 服务器内部错误

### 数据格式规范
#### POST /sub/[id] 请求格式
- **Content-Type**: `text/csv` 或 `text/plain`
- **数据格式**: CSV格式或自由文本格式，支持CloudflareST输出
- **验证规则**: 
  - 每行最多包含1个IP地址
  - 每次提交至少包含1个IP地址
  - 使用正则表达式提取和验证IP地址
- **示例**:
  ```csv
  IP地址,已发送,已接收,丢包率,平均延迟,下载速度(MB/s)
  104.16.1.1,10,10,0%,45.2,12.5
  104.16.2.1,10,9,10%,52.1,8.3
  ```

### 优先级应用规则
1. 用户专属文件优先于全局文件
2. clash.yaml模板内容优先于原订阅内容
3. IP扩展基于现有proxies进行复制和修改
4. **动态字段处理**：所有字段必须动态解析，严禁硬编码字段名称或结构
5. **完整字段保留**：订阅服务和模板文件的所有字段必须完整保留到最终输出
