# 环境变量配置说明

**🌐 语言**: [English](env-config.md) | [中文](env-config-cn.md)

## 概述

ClashSubManager通过环境变量进行系统配置，支持灵活的部署和安全管理。

## 必需环境变量

### ADMIN_USERNAME
- **说明**：管理员用户名
- **类型**：字符串
- **默认值**：无（必须设置）
- **示例**：`admin`
- **要求**：非空字符串

```bash
ADMIN_USERNAME=admin
```

### ADMIN_PASSWORD
- **说明**：管理员密码
- **类型**：字符串
- **默认值**：无（必须设置）
- **示例**：`MySecureP@ssw0rd2024!`
- **要求**：非空字符串，建议使用强密码

```bash
ADMIN_PASSWORD=MySecureP@ssw0rd2024!
```

### COOKIE_SECRET_KEY
- **说明**：Cookie签名密钥，用于HMACSHA256签名
- **类型**：字符串
- **默认值**：无（必须设置）
- **示例**：`32_character_long_secret_key`
- **要求**：至少32字符的随机字符串

```bash
COOKIE_SECRET_KEY=32_character_long_secret_key
```

### SESSION_TIMEOUT_MINUTES
- **说明**：会话超时时间（分钟）
- **类型**：整数
- **默认值**：30
- **示例**：`60`
- **要求**：5-1440之间的整数

```bash
SESSION_TIMEOUT_MINUTES=30
```

## 可选环境变量

### ASPNETCORE_ENVIRONMENT
- **说明**：ASP.NET Core运行环境
- **类型**：字符串
- **默认值**：`Production`
- **可选值**：`Development`, `Staging`, `Production`

```bash
ASPNETCORE_ENVIRONMENT=Production
```

### LOG_LEVEL
- **说明**：日志级别
- **类型**：字符串
- **默认值**：`Information`
- **可选值**：`Debug`, `Information`, `Warning`, `Error`, `Critical`

```bash
LOG_LEVEL=Information
```

### MAX_CONCURRENT_REQUESTS
- **说明**：最大并发请求数
- **类型**：整数
- **默认值**：`50`
- **要求**：10-100之间的整数

```bash
MAX_CONCURRENT_REQUESTS=50
```

### REQUEST_RATE_LIMIT
- **说明**：请求频率限制（每IP每秒请求数）
- **类型**：整数
- **默认值**：`10`
- **要求**：1-20之间的整数

```bash
REQUEST_RATE_LIMIT=10
```

## 配置示例

### 开发环境配置
```bash
# .env.development
ADMIN_USERNAME=admin
ADMIN_PASSWORD=DevPass123!
COOKIE_SECRET_KEY=dev_secret_key_32_characters_long
SESSION_TIMEOUT_MINUTES=120
ASPNETCORE_ENVIRONMENT=Development
LOG_LEVEL=Debug
MAX_CONCURRENT_REQUESTS=20
REQUEST_RATE_LIMIT=5
```

### 生产环境配置
```bash
# .env.production
ADMIN_USERNAME=admin
ADMIN_PASSWORD=ProdSecureP@ssw0rd2024!
COOKIE_SECRET_KEY=$(openssl rand -hex 16)
SESSION_TIMEOUT_MINUTES=30
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
MAX_CONCURRENT_REQUESTS=50
REQUEST_RATE_LIMIT=10
```

### 测试环境配置
```bash
# .env.testing
ADMIN_USERNAME=test_admin
ADMIN_PASSWORD=TestPass123!
COOKIE_SECRET_KEY=test_secret_key_32_characters_long
SESSION_TIMEOUT_MINUTES=60
ASPNETCORE_ENVIRONMENT=Staging
LOG_LEVEL=Warning
MAX_CONCURRENT_REQUESTS=30
REQUEST_RATE_LIMIT=8
```

## 安全配置建议

### 1. 生成强密码
```bash
# 生成12位强密码
openssl rand -base64 12 | tr -d "=+/" | cut -c1-12

# 生成16位强密码
openssl rand -base64 16 | tr -d "=+/" | cut -c1-16
```

### 2. 生成安全密钥
```bash
# 生成32字符随机密钥
openssl rand -hex 16

# 生成64字符随机密钥
openssl rand -hex 32
```

### 3. 环境变量文件权限
```bash
# 设置环境变量文件权限
chmod 600 .env.production
chmod 600 .env.staging

# 确保只有root用户可以读取
chown root:root .env.production
chown root:root .env.staging
```

## Docker Compose配置

### 使用环境变量文件
```yaml
version: '3.8'

services:
  clashsubmanager:
    image: clashsubmanager:latest
    container_name: clashsubmanager
    ports:
      - "8080:80"
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    env_file:
      - .env.production
    restart: unless-stopped
```

### 直接在compose中设置
```yaml
version: '3.8'

services:
  clashsubmanager:
    image: clashsubmanager:latest
    container_name: clashsubmanager
    ports:
      - "8080:80"
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    environment:
      - ADMIN_USERNAME=admin
      - ADMIN_PASSWORD=${ADMIN_PASSWORD}
      - COOKIE_SECRET_KEY=${COOKIE_SECRET_KEY}
      - SESSION_TIMEOUT_MINUTES=30
      - ASPNETCORE_ENVIRONMENT=Production
      - LOG_LEVEL=Information
    restart: unless-stopped
```

## Kubernetes配置

### Secret配置
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: clashsubmanager-secrets
type: Opaque
data:
  admin-username: YWRtaW4=  # admin (base64)
  admin-password: TXlTZWN1cmVQQHNzd29yZDIwMjQh  # MySecureP@ssw0rd2024! (base64)
  cookie-secret-key: MzJfY2hhcmFjdGVyX2xvbmdfc2VjcmV0X2tleQ==  # 32_character_long_secret_key (base64)
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: clashsubmanager-config
data:
  session-timeout-minutes: "30"
  aspnetcore-environment: "Production"
  log-level: "Information"
  max-concurrent-requests: "50"
  request-rate-limit: "10"
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: clashsubmanager
spec:
  replicas: 1
  selector:
    matchLabels:
      app: clashsubmanager
  template:
    metadata:
      labels:
        app: clashsubmanager
    spec:
      containers:
      - name: clashsubmanager
        image: clashsubmanager:latest
        ports:
        - containerPort: 80
        env:
        - name: ADMIN_USERNAME
          valueFrom:
            secretKeyRef:
              name: clashsubmanager-secrets
              key: admin-username
        - name: ADMIN_PASSWORD
          valueFrom:
            secretKeyRef:
              name: clashsubmanager-secrets
              key: admin-password
        - name: COOKIE_SECRET_KEY
          valueFrom:
            secretKeyRef:
              name: clashsubmanager-secrets
              key: cookie-secret-key
        - name: SESSION_TIMEOUT_MINUTES
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: session-timeout-minutes
        - name: ASPNETCORE_ENVIRONMENT
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: aspnetcore-environment
        - name: LOG_LEVEL
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: log-level
        - name: MAX_CONCURRENT_REQUESTS
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: max-concurrent-requests
        - name: REQUEST_RATE_LIMIT
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: request-rate-limit
```

## 配置验证

### 1. 验证必需变量
```bash
#!/bin/bash
# 检查必需环境变量
required_vars=("ADMIN_USERNAME" "ADMIN_PASSWORD" "COOKIE_SECRET_KEY")

for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        echo "错误: 必需环境变量 $var 未设置"
        exit 1
    fi
done

echo "✅ 所有必需环境变量已设置"
```

### 2. 验证密码强度
```bash
#!/bin/bash
# 检查密码强度
password="$ADMIN_PASSWORD"

if [ ${#password} -lt 12 ]; then
    echo "警告: 密码长度少于12个字符"
fi

if ! [[ "$password" =~ [A-Z] ]]; then
    echo "警告: 密码不包含大写字母"
fi

if ! [[ "$password" =~ [a-z] ]]; then
    echo "警告: 密码不包含小写字母"
fi

if ! [[ "$password" =~ [0-9] ]]; then
    echo "警告: 密码不包含数字"
fi

if ! [[ "$password" =~ [^a-zA-Z0-9] ]]; then
    echo "警告: 密码不包含特殊字符"
fi
```

### 3. 验证密钥长度
```bash
#!/bin/bash
# 检查Cookie密钥长度
key="$COOKIE_SECRET_KEY"

if [ ${#key} -lt 32 ]; then
    echo "错误: COOKIE_SECRET_KEY 长度必须至少32个字符"
    exit 1
fi

echo "✅ Cookie密钥长度验证通过"
```

## 故障排除

### 1. 环境变量未生效
```bash
# 检查容器环境变量
docker exec clashsubmanager env | grep ADMIN

# 重启容器
docker restart clashsubmanager

# 检查日志
docker logs clashsubmanager | grep -i error
```

### 2. 密码错误
```bash
# 重新生成密码
NEW_PASSWORD=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-16)

# 更新环境变量
docker stop clashsubmanager
docker run -d --name clashsubmanager-new \
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=$NEW_PASSWORD \
  -e COOKIE_SECRET_KEY=$COOKIE_SECRET_KEY \
  -e SESSION_TIMEOUT_MINUTES=30 \
  clashsubmanager:latest

# 清理旧容器
docker rm clashsubmanager
docker rename clashsubmanager-new clashsubmanager
```

### 3. 会话超时问题
```bash
# 检查会话超时设置
docker exec clashsubmanager env | grep SESSION_TIMEOUT

# 调整超时时间
docker stop clashsubmanager
docker run -d --name clashsubmanager \
  -e SESSION_TIMEOUT_MINUTES=60 \
  clashsubmanager:latest
```

## 最佳实践

1. **使用环境变量文件**：将敏感信息存储在.env文件中，不要提交到版本控制
2. **定期轮换密钥**：建议每3-6个月更换一次Cookie密钥
3. **使用强密码**：管理员密码应包含大小写字母、数字和特殊字符
4. **最小权限原则**：根据环境调整配置，开发环境可以使用较宽松的设置
5. **监控配置变更**：记录所有环境变量的变更历史
6. **备份配置**：定期备份环境变量配置文件