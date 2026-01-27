# ClashSubManager 环境变量配置文档

## 概述

ClashSubManager 通过环境变量进行系统配置，特别是在Docker容器化部署环境中。所有环境变量都有合理的默认值，但建议在生产环境中明确设置。

## 认证配置

### ADMIN_USERNAME
**描述**: 管理员用户名  
**类型**: String  
**默认值**: admin  
**必需**: 否  
**示例**: `ADMIN_USERNAME=myadmin`

### ADMIN_PASSWORD
**描述**: 管理员密码  
**类型**: String  
**默认值**: (无默认值)  
**必需**: 是  
**示例**: `ADMIN_PASSWORD=SecurePassword123!`

### COOKIE_SECRET_KEY
**描述**: Cookie签名密钥，用于HMACSHA256签名  
**类型**: String  
**默认值**: default-key  
**必需**: 否 (生产环境强烈建议设置)  
**约束**: 长度≥32字符  
**示例**: `COOKIE_SECRET_KEY=your-very-long-and-secure-secret-key-for-hmac-signing`

### SESSION_TIMEOUT_MINUTES
**描述**: 会话超时时间（分钟）  
**类型**: Integer  
**默认值**: 30  
**约束**: 5-1440分钟  
**示例**: `SESSION_TIMEOUT_MINUTES=60`

### SUBSCRIPTION_URL_TEMPLATE
**描述**: 上游订阅URL模板（必须包含 `{userId}` 用于订阅接口用户ID验证和订阅生成）  
**类型**: String  
**默认值**: (无默认值)  
**必需**: 是（订阅接口必需）  
**示例**: `SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}`

## 系统配置

### DATA_PATH
**描述**: 数据存储目录路径  
**类型**: String  
**默认值**: /app/data  
**必需**: 否  
**示例**: `DATA_PATH=/custom/data/path`

## Docker Compose 配置示例

```yaml
version: '3.8'
services:
  clash-sub-manager:
    build: .
    ports:
      - "80:80"
    environment:
      - ADMIN_USERNAME=admin
      - ADMIN_PASSWORD=your_secure_password_here
      - COOKIE_SECRET_KEY=your_hmac_key_at_least_32_chars_long
      - SESSION_TIMEOUT_MINUTES=30
      - DATA_PATH=/app/data
      - SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    restart: always
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

## Docker 运行命令示例

```bash
docker run -d \
  --name clash-sub-manager \
  -p 80:80 \
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=SecurePassword123! \
  -e COOKIE_SECRET_KEY=your-very-long-and-secure-secret-key-for-hmac-signing \
  -e SESSION_TIMEOUT_MINUTES=30 \
  -e DATA_PATH=/app/data \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/logs:/app/logs \
  --restart always \
  clash-sub-manager:latest
```

## 安全注意事项

### 1. 密码安全
- `ADMIN_PASSWORD` 不能为空，建议使用强密码
- 密码应包含大小写字母、数字和特殊字符
- 定期更换管理员密码

### 2. Cookie密钥安全
- `COOKIE_SECRET_KEY` 长度必须≥32字符
- 使用随机生成的长字符串作为密钥
- 不要在代码或版本控制中暴露密钥

### 3. 会话管理
- `SESSION_TIMEOUT_MINUTES` 根据安全需求设置
- 敏感环境建议设置较短的超时时间
- 超时范围：5-1440分钟

## 环境变量验证

系统启动时会验证以下环境变量：

1. **ADMIN_PASSWORD**: 不能为空
2. **COOKIE_SECRET_KEY**: 长度≥32字符（如果设置了）
3. **SESSION_TIMEOUT_MINUTES**: 5-1440分钟范围内

如果验证失败，系统会在日志中记录警告并使用默认值。

## 故障排除

### 问题：无法登录管理界面
**可能原因**:
- `ADMIN_USERNAME` 或 `ADMIN_PASSWORD` 设置错误
- 环境变量未正确传递到容器

**解决方案**:
```bash
# 检查环境变量
docker exec clash-sub-manager env | grep ADMIN

# 查看容器日志
docker logs clash-sub-manager
```

### 问题：会话立即过期
**可能原因**:
- `COOKIE_SECRET_KEY` 长度不足32字符
- 密钥在容器重启后发生变化

**解决方案**:
- 确保密钥长度≥32字符
- 在Docker配置中固定设置密钥

### 问题：数据持久化失败
**可能原因**:
- `DATA_PATH` 设置错误
- Docker卷挂载配置问题

**解决方案**:
- 检查数据目录权限
- 验证Docker卷挂载配置

## 生产环境建议

1. **使用强密码**: 管理员密码应包含大小写字母、数字和特殊字符
2. **设置长密钥**: Cookie密钥使用64位随机字符串
3. **合理超时**: 会话超时设置为30-60分钟
4. **定期更新**: 定期更换密码和密钥
5. **监控日志**: 关注登录失败和异常访问日志

## 开发环境配置

开发环境可以使用简化配置：

```bash
ADMIN_USERNAME=admin
ADMIN_PASSWORD=dev123
COOKIE_SECRET_KEY=dev-secret-key-for-development-only
SESSION_TIMEOUT_MINUTES=120
SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}
```

**注意**: 开发环境配置仅用于开发测试，生产环境必须使用安全配置。
