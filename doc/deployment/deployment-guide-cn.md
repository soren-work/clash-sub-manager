# ClashSubManager 部署运维文档

**🌐 语言**: [English](deployment-guide.md) | [中文](deployment-guide-cn.md)

---

## 📋 部署前准备

### 需要准备什么？

在开始部署之前，请确保你已经准备好以下内容：

#### 🖥️ 硬件和系统要求
- **操作系统**：Linux (推荐 Ubuntu 20.04+) / Windows Server / macOS
- **CPU**：1核心（最低），2核心（推荐）
- **内存**：512MB（最低），1GB（推荐），2GB（多用户场景）
- **存储**：1GB（最低），5GB（推荐），根据用户数量调整
- **网络**：稳定的外网连接，用于访问上游订阅服务

#### 🐳 软件依赖
- **Docker**：20.10+ 版本
- **Docker Compose**：1.29+ 版本（可选，但推荐）
- **反向代理**：Nginx / Apache / Caddy（生产环境推荐）
- **SSL证书**：Let's Encrypt 或其他 CA 证书（HTTPS 访问）

#### 📝 配置信息
- **上游订阅地址**：你的机场订阅链接模板
- **管理员账号**：用户名和强密码
- **域名**：（可选）用于公网访问
- **优选IP列表**：（可选）CloudflareST 测速结果

### 前置知识要求

- ✅ 基本的 Linux 命令行操作
- ✅ Docker 和 Docker Compose 基础知识
- ✅ 基本的网络配置知识（端口、防火墙）
- ⚠️ 如需配置 HTTPS，需要了解 SSL/TLS 证书配置

### 预计部署时间

| 部署场景 | 预计时间 | 难度 |
|---------|---------|------|
| **测试环境快速部署** | 10-15 分钟 | ⭐ 简单 |
| **生产环境标准部署** | 30-45 分钟 | ⭐⭐ 中等 |
| **生产环境高可用部署** | 1-2 小时 | ⭐⭐⭐ 较难 |

---

## 1. 部署概述

### 1.1 系统架构
- **架构模式**：单体应用架构
- **技术栈**：.NET 10 + ASP.NET Core Razor Pages
- **部署方式**：Docker容器化部署
- **数据存储**：本地文件系统
- **健康检查**：内置 `/health` 端点，提供系统指标

### 1.2 部署模式选择

#### 模式一：快速测试部署（开发/测试环境）
- **适用场景**：本地测试、功能验证
- **特点**：快速启动，配置简单
- **不适用**：生产环境、多用户场景

#### 模式二：生产环境标准部署（推荐）
- **适用场景**：个人使用、小团队（<50人）
- **特点**：配置完善，安全可靠
- **包含**：反向代理、SSL证书、日志管理

#### 模式三：生产环境高可用部署
- **适用场景**：大规模团队（>50人）、高可用要求
- **特点**：负载均衡、数据备份、监控告警
- **包含**：多实例部署、健康检查、自动恢复

---

## 2. 快速测试部署（开发/测试环境）

> ⚠️ **注意**：此部署方式仅适用于测试环境，不建议用于生产环境。

### 2.1 使用Docker Compose（推荐）

**步骤1：创建docker-compose.yml**
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
      - AdminUsername=admin
      - AdminPassword=test123
      - CookieSecretKey=test_secret_key_32_chars_long
      - SessionTimeoutMinutes=120
      - DataPath=/app/data
      - SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}
      - LOG_LEVEL=Debug
    restart: unless-stopped
```

**步骤2：启动服务**
```bash
# 创建数据目录
mkdir -p data logs

# 启动服务
docker-compose up -d

# 查看日志
docker-compose logs -f clashsubmanager

# 访问服务
# 浏览器打开: http://localhost:8080
```

### 2.2 使用Docker命令

```bash
# 一键启动（测试环境）
docker run -d \
  --name clashsubmanager \
  -p 8080:80 \
  -v $(pwd)/data:/app/data \
  -e AdminUsername=admin \
  -e AdminPassword=test123 \
  -e CookieSecretKey=test_secret_key_32_chars_long \
  -e SessionTimeoutMinutes=120 \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  clashsubmanager:latest

# 查看日志
docker logs -f clashsubmanager
```

---

## 3. 生产环境标准部署（推荐）

> ✅ **推荐**：此部署方式适用于生产环境，包含安全配置和反向代理。

### 3.1 准备工作

**步骤1：生成安全密钥**
```bash
# 生成强密码（16位）
ADMIN_PASSWORD=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-16)
echo "管理员密码: $ADMIN_PASSWORD"

# 生成Cookie密钥（32字符）
COOKIE_SECRET=$(openssl rand -hex 16)
echo "Cookie密钥: $COOKIE_SECRET"

# 保存到环境变量文件
cat > .env.production << EOF
AdminUsername=admin
AdminPassword=$ADMIN_PASSWORD
CookieSecretKey=$COOKIE_SECRET
SessionTimeoutMinutes=30
SUBSCRIPTION_URL_TEMPLATE=https://your-airport.com/sub/{userId}
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
EOF

# 设置文件权限
chmod 600 .env.production
```

**步骤2：创建目录结构**
```bash
# 创建项目目录
mkdir -p /opt/clashsubmanager/{data,logs,config}
cd /opt/clashsubmanager

# 设置权限
chmod 755 data logs
```

### 3.2 Docker Compose 部署

**创建 docker-compose.yml**
```yaml
version: '3.8'

services:
  clashsubmanager:
    image: clashsubmanager:latest
    container_name: clashsubmanager
    ports:
      - "127.0.0.1:8080:80"  # 仅监听本地，通过反向代理访问
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    env_file:
      - .env.production
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M
```

**启动服务**
```bash
# 启动
docker-compose up -d

# 检查状态
docker-compose ps

# 查看日志
docker-compose logs -f
```

### 3.3 配置反向代理（Nginx）

**安装 Nginx**
```bash
# Ubuntu/Debian
sudo apt update
sudo apt install nginx -y

# CentOS/RHEL
sudo yum install nginx -y
```

**配置 HTTP 访问**
```bash
# 创建配置文件
sudo nano /etc/nginx/sites-available/clashsubmanager

# 添加以下内容
server {
    listen 80;
    server_name your-domain.com;  # 替换为你的域名
    
    # 访问日志
    access_log /var/log/nginx/clashsubmanager-access.log;
    error_log /var/log/nginx/clashsubmanager-error.log;
    
    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # 超时设置
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
}

# 启用配置
sudo ln -s /etc/nginx/sites-available/clashsubmanager /etc/nginx/sites-enabled/

# 测试配置
sudo nginx -t

# 重启 Nginx
sudo systemctl restart nginx
```

### 3.4 配置 HTTPS（Let's Encrypt）

**安装 Certbot**
```bash
# Ubuntu/Debian
sudo apt install certbot python3-certbot-nginx -y

# CentOS/RHEL
sudo yum install certbot python3-certbot-nginx -y
```

**获取 SSL 证书**
```bash
# 自动配置 HTTPS
sudo certbot --nginx -d your-domain.com

# 测试自动续期
sudo certbot renew --dry-run
```

**手动配置 HTTPS（可选）**
```bash
# 编辑 Nginx 配置
sudo nano /etc/nginx/sites-available/clashsubmanager

# 添加 HTTPS 配置
server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;  # 重定向到 HTTPS
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;
    
    # SSL 证书
    ssl_certificate /etc/letsencrypt/live/your-domain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/your-domain.com/privkey.pem;
    
    # SSL 配置
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    
    # 访问日志
    access_log /var/log/nginx/clashsubmanager-access.log;
    error_log /var/log/nginx/clashsubmanager-error.log;
    
    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # 超时设置
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
}

# 重启 Nginx
sudo systemctl restart nginx
```

### 3.5 配置防火墙

```bash
# UFW (Ubuntu/Debian)
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable

# Firewalld (CentOS/RHEL)
sudo firewall-cmd --permanent --add-service=http
sudo firewall-cmd --permanent --add-service=https
sudo firewall-cmd --reload
```

### 3.6 验证部署

```bash
# 检查服务状态
docker ps | grep clashsubmanager

# 检查健康状态
curl http://localhost:8080/health

# 检查 HTTPS 访问
curl -I https://your-domain.com

# 查看日志
docker logs clashsubmanager | tail -50
```

---

## 4. 生产环境高可用部署

> 🚀 **高级**：适用于大规模团队和高可用要求场景。

### 4.1 负载均衡配置

**Nginx 负载均衡**
```nginx
upstream clashsubmanager_backend {
    least_conn;  # 最少连接算法
    server 127.0.0.1:8080 weight=1 max_fails=3 fail_timeout=30s;
    server 127.0.0.1:8081 weight=1 max_fails=3 fail_timeout=30s;
    server 127.0.0.1:8082 weight=1 max_fails=3 fail_timeout=30s;
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;
    
    ssl_certificate /etc/letsencrypt/live/your-domain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/your-domain.com/privkey.pem;
    
    location / {
        proxy_pass http://clashsubmanager_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # 健康检查
        proxy_next_upstream error timeout http_500 http_502 http_503;
    }
}
```

**多实例 Docker Compose**
```yaml
version: '3.8'

services:
  clashsubmanager-1:
    image: clashsubmanager:latest
    container_name: clashsubmanager-1
    ports:
      - "127.0.0.1:8080:80"
    volumes:
      - ./data:/app/data:ro  # 只读挂载
      - ./logs/instance-1:/app/logs
    env_file:
      - .env.production
    restart: unless-stopped

  clashsubmanager-2:
    image: clashsubmanager:latest
    container_name: clashsubmanager-2
    ports:
      - "127.0.0.1:8081:80"
    volumes:
      - ./data:/app/data:ro
      - ./logs/instance-2:/app/logs
    env_file:
      - .env.production
    restart: unless-stopped

  clashsubmanager-3:
    image: clashsubmanager:latest
    container_name: clashsubmanager-3
    ports:
      - "127.0.0.1:8082:80"
    volumes:
      - ./data:/app/data:ro
      - ./logs/instance-3:/app/logs
    env_file:
      - .env.production
    restart: unless-stopped
```

### 4.2 数据备份策略

**自动备份脚本**
```bash
#!/bin/bash
# /opt/clashsubmanager/scripts/backup.sh

BACKUP_DIR="/backup/clashsubmanager"
DATA_DIR="/opt/clashsubmanager/data"
DATE=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=30

# 创建备份目录
mkdir -p $BACKUP_DIR

# 备份数据
tar -czf $BACKUP_DIR/data_$DATE.tar.gz -C $DATA_DIR .

# 备份环境变量（加密）
gpg --symmetric --cipher-algo AES256 -o $BACKUP_DIR/env_$DATE.gpg /opt/clashsubmanager/.env.production

# 清理旧备份
find $BACKUP_DIR -name "data_*.tar.gz" -mtime +$RETENTION_DAYS -delete
find $BACKUP_DIR -name "env_*.gpg" -mtime +$RETENTION_DAYS -delete

# 记录日志
echo "$(date): 备份完成 - $BACKUP_DIR/data_$DATE.tar.gz" >> $BACKUP_DIR/backup.log
```

**配置定时任务**
```bash
# 编辑 crontab
crontab -e

# 每天凌晨2点备份
0 2 * * * /opt/clashsubmanager/scripts/backup.sh

# 每周日凌晨3点清理日志
0 3 * * 0 find /opt/clashsubmanager/logs -name "*.log" -mtime +7 -delete
```

### 4.3 监控告警配置

**健康检查脚本**
```bash
#!/bin/bash
# /opt/clashsubmanager/scripts/health-check.sh

HEALTH_URL="http://localhost:8080/health"
ALERT_EMAIL="admin@example.com"

# 检查服务健康状态
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)

if [ $HTTP_CODE -ne 200 ]; then
    # 发送告警邮件
    echo "ClashSubManager 服务异常，HTTP状态码: $HTTP_CODE" | \
        mail -s "ClashSubManager 告警" $ALERT_EMAIL
    
    # 尝试重启服务
    docker restart clashsubmanager
    
    # 记录日志
    echo "$(date): 服务异常，已尝试重启" >> /var/log/clashsubmanager-health.log
fi
```

**配置监控**
```bash
# 每5分钟检查一次
*/5 * * * * /opt/clashsubmanager/scripts/health-check.sh
```

---

## 5. 环境变量配置详解

> 💡 **提示**：完整的环境变量配置说明请参考 [环境变量配置文档](env-config-cn.md)

### 5.1 必需环境变量 vs 可选环境变量

#### 🔴 必需变量（必须配置）

| 变量名 | 说明 | 生产环境推荐值 | 测试环境示例 |
|--------|------|---------------|-------------|
| `AdminUsername` | 管理员用户名 | `admin` | `admin` |
| `AdminPassword` | 管理员密码 | 强密码（16位+） | `test123` |
| `CookieSecretKey` | Cookie签名密钥 | 随机生成（32字符+） | `test_secret_key_32_chars_long` |
| `SUBSCRIPTION_URL_TEMPLATE` | 上游订阅URL模板 | 你的机场订阅地址 | `https://api.example.com/sub/{userId}` |

#### 🟡 推荐变量（建议配置）

| 变量名 | 说明 | 生产环境推荐值 | 默认值 |
|--------|------|---------------|--------|
| `SessionTimeoutMinutes` | 会话超时时间 | `30` | `60` |
| `LOG_LEVEL` | 日志级别 | `Information` | `Information` |
| `ASPNETCORE_ENVIRONMENT` | 运行环境 | `Production` | `Production` |

#### 🟢 可选变量（按需配置）

| 变量名 | 说明 | 默认值 |
|--------|------|--------|
| `DataPath` | 数据目录路径 | `/app/data` |
| `SubscriptionUrlTemplate` | 订阅URL模板（兜底） | 无 |

### 5.2 生产环境推荐配置

```bash
# .env.production
AdminUsername=admin
AdminPassword=<使用 openssl rand -base64 16 生成>
CookieSecretKey=<使用 openssl rand -hex 16 生成>
SessionTimeoutMinutes=30
SUBSCRIPTION_URL_TEMPLATE=https://your-airport.com/sub/{userId}
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
```

### 5.3 安全配置最佳实践

#### ✅ 密码强度要求
- **长度**：至少 12 位，推荐 16 位以上
- **复杂度**：包含大小写字母、数字、特殊字符
- **避免**：不要使用常见密码、生日、用户名等

#### ✅ 密钥生成方法
```bash
# 生成强密码
openssl rand -base64 16 | tr -d "=+/" | cut -c1-16

# 生成Cookie密钥（32字符）
openssl rand -hex 16

# 生成Cookie密钥（64字符，更安全）
openssl rand -hex 32
```

#### ✅ 环境变量文件权限
```bash
# 设置严格权限
chmod 600 .env.production
chown root:root .env.production

# 不要提交到版本控制
echo ".env.production" >> .gitignore
```

## 4. 数据目录结构

### 4.1 目录布局
```
/app/data/
├── cloudflare-ip.csv          # 默认优选IP配置
├── clash.yaml                 # 默认Clash模板
├── users.txt                  # 用户访问记录
└── [用户id]/                  # 用户专属配置目录
    ├── cloudflare-ip.csv      # 用户专属优选IP
    └── clash.yaml             # 用户专属模板

/app/logs/
├── app-2026-01-21.log        # 应用日志
├── access-2026-01-21.log     # 访问日志
└── error-2026-01-21.log      # 错误日志
```

### 4.2 权限设置
```bash
# 设置数据目录权限
chmod 755 /app/data
chmod 644 /app/data/*.csv
chmod 644 /app/data/*.yaml

# 设置日志目录权限
chmod 755 /app/logs
chmod 644 /app/logs/*.log
```

### 4.3 备份策略
```bash
# 创建备份脚本
#!/bin/bash
BACKUP_DIR="/backup/clashsubmanager"
DATA_DIR="/app/data"
DATE=$(date +%Y%m%d_%H%M%S)

# 创建备份目录
mkdir -p $BACKUP_DIR

# 备份数据
tar -czf $BACKUP_DIR/data_$DATE.tar.gz $DATA_DIR

# 清理7天前的备份
find $BACKUP_DIR -name "data_*.tar.gz" -mtime +7 -delete
```

## 5. 网络配置

### 5.1 端口配置
- **HTTP端口**：80（容器内部）
- **HTTPS端口**：443（如需SSL）
- **管理端口**：80（与HTTP共用）

### 5.2 反向代理配置

**Nginx配置示例**
```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}

server {
    listen 443 ssl;
    server_name your-domain.com;
    
    ssl_certificate /path/to/your/cert.pem;
    ssl_certificate_key /path/to/your/key.pem;
    
    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

**Apache配置示例**
```apache
<VirtualHost *:80>
    ServerName your-domain.com
    ProxyPreserveHost On
    ProxyPass / http://localhost:8080/
    ProxyPassReverse / http://localhost:8080/
</VirtualHost>

<VirtualHost *:443>
    ServerName your-domain.com
    SSLEngine on
    SSLCertificateFile /path/to/your/cert.pem
    SSLCertificateKeyFile /path/to/your/key.pem
    ProxyPreserveHost On
    ProxyPass / http://localhost:8080/
    ProxyPassReverse / http://localhost:8080/
</VirtualHost>
```

## 6. 监控与日志

### 6.1 健康检查
```bash
# 检查服务状态
curl -f http://localhost:8080/health

# 检查容器状态
docker ps | grep clashsubmanager

# 查看容器资源使用
docker stats clashsubmanager
```

### 6.2 日志管理
```bash
# 查看应用日志
docker logs clashsubmanager

# 查看实时日志
docker logs -f clashsubmanager

# 查看特定时间日志
docker logs --since="2026-01-21T10:00:00" clashsubmanager
```

### 6.3 日志轮转配置
```bash
# 创建logrotate配置
cat > /etc/logrotate.d/clashsubmanager << EOF
/app/logs/*.log {
    daily
    rotate 30
    compress
    delaycompress
    missingok
    notifempty
    create 644 root root
    postrotate
        docker kill -s USR1 clashsubmanager
    endscript
}
EOF
```

## 7. 性能优化

### 7.1 容器资源限制
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
      - AdminUsername=admin
      - AdminPassword=your_secure_password_here
      - CookieSecretKey=your_hmac_key_at_least_32_chars_long
      - SessionTimeoutMinutes=30
      - DataPath=/app/data
      - SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M
    restart: unless-stopped
```

### 7.2 系统优化
```bash
# 调整系统参数
echo 'net.core.somaxconn = 1024' >> /etc/sysctl.conf
echo 'net.ipv4.tcp_max_syn_backlog = 1024' >> /etc/sysctl.conf
sysctl -p

# 调整文件描述符限制
echo '* soft nofile 65536' >> /etc/security/limits.conf
echo '* hard nofile 65536' >> /etc/security/limits.conf
```

## 8. 安全加固

### 8.1 容器安全
```bash
# 使用非root用户运行
docker run -d \
  --name clashsubmanager \
  -p 8080:80 \
  -v /opt/clashsubmanager/data:/app/data \
  -v /opt/clashsubmanager/logs:/app/logs \
  -e AdminUsername=admin \
  -e AdminPassword=your_secure_password_here \
  -e CookieSecretKey=your_hmac_key_at_least_32_chars_long \
  -e SessionTimeoutMinutes=30 \
  -e DataPath=/app/data \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  --user 1000:1000 \
  --read-only \
  --tmpfs /tmp \
  --restart unless-stopped \
  clashsubmanager:latest
```

### 8.2 网络安全
```bash
# 使用防火墙限制访问
ufw allow 8080/tcp
ufw enable

# 限制仅特定IP访问管理界面
iptables -A INPUT -p tcp --dport 8080 -s 192.168.1.0/24 -j ACCEPT
iptables -A INPUT -p tcp --dport 8080 -j DROP
```

### 8.3 SSL/TLS配置
```bash
# 生成自签名证书（测试环境）
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /etc/ssl/private/clashsubmanager.key \
  -out /etc/ssl/certs/clashsubmanager.crt

# 使用Let's Encrypt（生产环境）
certbot --nginx -d your-domain.com
```

## 9. 故障排除

### 9.1 常见问题

**问题1：容器启动失败**
```bash
# 检查日志
docker logs clashsubmanager

# 检查配置
docker inspect clashsubmanager

# 重新启动
docker restart clashsubmanager
```

**问题2：无法访问管理界面**
```bash
# 检查端口映射
docker port clashsubmanager

# 检查防火墙
ufw status

# 检查环境变量
docker exec clashsubmanager env | grep ADMIN
```

**问题3：配置文件丢失**
```bash
# 检查数据目录权限
ls -la /app/data

# 恢复备份
tar -xzf /backup/clashsubmanager/data_20260121.tar.gz -C /
```

**问题4：性能问题**
```bash
# 检查资源使用
docker stats clashsubmanager

# 检查日志错误
grep ERROR /app/logs/app-*.log

# 重启服务
docker restart clashsubmanager
```

### 9.2 调试模式
```bash
# 启用调试日志
docker run -d \
  --name clashsubmanager-debug \
  -p 8080:80 \
  -v /opt/clashsubmanager/data:/app/data \
  -v /opt/clashsubmanager/logs:/app/logs \
  -e AdminUsername=admin \
  -e AdminPassword=your_secure_password_here \
  -e CookieSecretKey=your_hmac_key_at_least_32_chars_long \
  -e SessionTimeoutMinutes=30 \
  -e DataPath=/app/data \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  -e LOG_LEVEL=Debug \
  -e ASPNETCORE_ENVIRONMENT=Development \
  clashsubmanager:latest
```

## 10. 维护操作

### 10.1 日常维护
```bash
# 每日检查脚本
#!/bin/bash
echo "=== ClashSubManager 日常检查 ==="

# 检查服务状态
if docker ps | grep -q clashsubmanager; then
    echo "✅ 服务运行正常"
else
    echo "❌ 服务异常，尝试重启"
    docker restart clashsubmanager
fi

# 检查磁盘空间
DISK_USAGE=$(df /app/data | awk 'NR==2 {print $5}' | sed 's/%//')
if [ $DISK_USAGE -gt 80 ]; then
    echo "⚠️ 磁盘使用率过高: ${DISK_USAGE}%"
fi

# 检查日志大小
LOG_SIZE=$(du -sh /app/logs | awk '{print $1}' | sed 's/[^0-9.]//g')
if [ ${LOG_SIZE%.*} -gt 100 ]; then
    echo "⚠️ 日志文件过大: $LOG_SIZE"
fi

echo "=== 检查完成 ==="
```

### 10.2 更新部署
```bash
# 更新脚本
#!/bin/bash
echo "=== 更新 ClashSubManager ==="

# 备份数据
tar -czf /backup/clashsubmanager/pre-update_$(date +%Y%m%d_%H%M%S).tar.gz /app/data

# 拉取新镜像
docker pull clashsubmanager:latest

# 停止旧容器
docker stop clashsubmanager

# 启动新容器
docker run -d \
  --name clashsubmanager \
  -p 8080:80 \
  -v /opt/clashsubmanager/data:/app/data \
  -v /opt/clashsubmanager/logs:/app/logs \
  -e AdminUsername=admin \
  -e AdminPassword=your_secure_password_here \
  -e CookieSecretKey=your_hmac_key_at_least_32_chars_long \
  -e SessionTimeoutMinutes=30 \
  -e DataPath=/app/data \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  --restart unless-stopped \
  clashsubmanager:latest

# 清理旧镜像
docker image prune -f

echo "=== 更新完成 ==="
```

### 10.3 数据迁移
```bash
# 迁移脚本
#!/bin/bash
echo "=== 数据迁移 ==="

OLD_DATA_DIR="/old/path/to/data"
NEW_DATA_DIR="/new/path/to/data"

# 停止服务
docker stop clashsubmanager

# 迁移数据
cp -r $OLD_DATA_DIR/* $NEW_DATA_DIR/

# 设置权限
chown -R 1000:1000 $NEW_DATA_DIR
chmod -R 755 $NEW_DATA_DIR

# 启动服务
docker start clashsubmanager

echo "=== 迁移完成 ==="
```

## 11. 运维管理

### 11.1 日志管理和分析

#### 日志类型说明

ClashSubManager 生成以下类型的日志：

| 日志类型 | 文件名格式 | 说明 | 保留时间 |
|---------|-----------|------|---------|
| **应用日志** | `app-YYYY-MM-DD.log` | 应用运行日志，包含启动、配置加载等信息 | 30天 |
| **访问日志** | `access-YYYY-MM-DD.log` | HTTP请求访问日志 | 7天 |
| **错误日志** | `error-YYYY-MM-DD.log` | 错误和异常日志 | 90天 |

#### 日志查看命令

```bash
# 查看实时日志
docker logs -f clashsubmanager

# 查看最近100行日志
docker logs --tail 100 clashsubmanager

# 查看特定时间段日志
docker logs --since="2026-02-20T10:00:00" --until="2026-02-20T12:00:00" clashsubmanager

# 查看错误日志
docker logs clashsubmanager 2>&1 | grep -i error

# 查看应用日志文件
tail -f /opt/clashsubmanager/logs/app-$(date +%Y-%m-%d).log
```

#### 日志分析技巧

**1. 分析访问频率**
```bash
# 统计每小时访问量
grep "$(date +%Y-%m-%d)" /opt/clashsubmanager/logs/access-*.log | \
  awk '{print $4}' | cut -d: -f2 | sort | uniq -c

# 统计最活跃的用户ID
grep "userId=" /opt/clashsubmanager/logs/access-*.log | \
  sed 's/.*userId=\([^&]*\).*/\1/' | sort | uniq -c | sort -rn | head -10
```

**2. 分析错误模式**
```bash
# 统计错误类型
grep ERROR /opt/clashsubmanager/logs/error-*.log | \
  awk '{print $5}' | sort | uniq -c | sort -rn

# 查找超时错误
grep -i "timeout" /opt/clashsubmanager/logs/error-*.log

# 查找订阅获取失败
grep "subscription.*fail" /opt/clashsubmanager/logs/app-*.log
```

**3. 性能分析**
```bash
# 统计响应时间
grep "ResponseTime" /opt/clashsubmanager/logs/access-*.log | \
  awk '{sum+=$NF; count++} END {print "平均响应时间:", sum/count, "ms"}'

# 查找慢请求（>1000ms）
grep "ResponseTime" /opt/clashsubmanager/logs/access-*.log | \
  awk '$NF > 1000 {print}'
```

#### 日志轮转配置

**使用 logrotate（推荐）**
```bash
# 创建配置文件
sudo tee /etc/logrotate.d/clashsubmanager << EOF
/opt/clashsubmanager/logs/*.log {
    daily                    # 每天轮转
    rotate 30                # 保留30个备份
    compress                 # 压缩旧日志
    delaycompress            # 延迟压缩（保留最近一天未压缩）
    missingok                # 文件不存在不报错
    notifempty               # 空文件不轮转
    create 644 root root     # 创建新文件的权限
    dateext                  # 使用日期作为后缀
    dateformat -%Y%m%d       # 日期格式
    maxage 90                # 删除90天前的日志
    
    postrotate
        # 通知应用重新打开日志文件
        docker kill -s USR1 clashsubmanager 2>/dev/null || true
    endscript
}
EOF

# 测试配置
sudo logrotate -d /etc/logrotate.d/clashsubmanager

# 手动执行轮转
sudo logrotate -f /etc/logrotate.d/clashsubmanager
```

**使用定时任务清理**
```bash
# 添加到 crontab
crontab -e

# 每天凌晨3点清理7天前的访问日志
0 3 * * * find /opt/clashsubmanager/logs -name "access-*.log" -mtime +7 -delete

# 每周日凌晨4点清理30天前的应用日志
0 4 * * 0 find /opt/clashsubmanager/logs -name "app-*.log" -mtime +30 -delete

# 保留错误日志90天
0 5 * * 0 find /opt/clashsubmanager/logs -name "error-*.log" -mtime +90 -delete
```

#### 集中式日志管理（可选）

**使用 ELK Stack**
```yaml
# docker-compose.yml 添加日志收集
version: '3.8'

services:
  clashsubmanager:
    image: clashsubmanager:latest
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
        labels: "service=clashsubmanager"
    # ... 其他配置

  filebeat:
    image: docker.elastic.co/beats/filebeat:8.11.0
    volumes:
      - ./filebeat.yml:/usr/share/filebeat/filebeat.yml:ro
      - /opt/clashsubmanager/logs:/logs:ro
    depends_on:
      - clashsubmanager
```

### 11.2 性能调优

#### 系统级优化

**1. 内核参数调优**
```bash
# 编辑 /etc/sysctl.conf
sudo tee -a /etc/sysctl.conf << EOF
# 网络优化
net.core.somaxconn = 2048
net.core.netdev_max_backlog = 2048
net.ipv4.tcp_max_syn_backlog = 2048
net.ipv4.tcp_fin_timeout = 30
net.ipv4.tcp_keepalive_time = 600
net.ipv4.tcp_tw_reuse = 1

# 文件描述符
fs.file-max = 65536
fs.inotify.max_user_watches = 524288
EOF

# 应用配置
sudo sysctl -p
```

**2. 文件描述符限制**
```bash
# 编辑 /etc/security/limits.conf
sudo tee -a /etc/security/limits.conf << EOF
* soft nofile 65536
* hard nofile 65536
* soft nproc 65536
* hard nproc 65536
EOF

# 重新登录后生效，或立即应用
ulimit -n 65536
```

#### Docker 容器优化

**1. 资源限制调优**
```yaml
# docker-compose.yml
services:
  clashsubmanager:
    image: clashsubmanager:latest
    deploy:
      resources:
        limits:
          cpus: '2.0'              # 根据实际负载调整
          memory: 1G               # 根据用户数量调整
        reservations:
          cpus: '1.0'
          memory: 512M
    # 使用 host 网络模式提升性能（可选）
    # network_mode: "host"
```

**2. 存储优化**
```bash
# 使用 overlay2 存储驱动（推荐）
sudo tee /etc/docker/daemon.json << EOF
{
  "storage-driver": "overlay2",
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}
EOF

sudo systemctl restart docker
```

#### 应用级优化

**1. 环境变量调优**
```bash
# .env.production
# 减少会话超时时间（减少内存占用）
SessionTimeoutMinutes=15

# 调整日志级别（生产环境使用 Warning 或 Error）
LOG_LEVEL=Warning

# 启用生产模式
ASPNETCORE_ENVIRONMENT=Production
```

**2. 数据目录优化**
```bash
# 定期清理用户访问记录
# 创建清理脚本
cat > /opt/clashsubmanager/scripts/cleanup-users.sh << 'EOF'
#!/bin/bash
# 清理30天未访问的用户记录
find /opt/clashsubmanager/data -name "users.txt" -mtime +30 -delete
echo "$(date): 清理完成" >> /var/log/clashsubmanager-cleanup.log
EOF

chmod +x /opt/clashsubmanager/scripts/cleanup-users.sh

# 添加到 crontab（每周执行）
0 2 * * 0 /opt/clashsubmanager/scripts/cleanup-users.sh
```

#### 性能监控

**1. 实时监控脚本**
```bash
#!/bin/bash
# /opt/clashsubmanager/scripts/monitor.sh

echo "=== ClashSubManager 性能监控 ==="
echo "时间: $(date)"
echo ""

# CPU 使用率
echo "CPU 使用率:"
docker stats clashsubmanager --no-stream --format "table {{.CPUPerc}}"
echo ""

# 内存使用
echo "内存使用:"
docker stats clashsubmanager --no-stream --format "table {{.MemUsage}}\t{{.MemPerc}}"
echo ""

# 网络流量
echo "网络流量:"
docker stats clashsubmanager --no-stream --format "table {{.NetIO}}"
echo ""

# 磁盘使用
echo "磁盘使用:"
df -h /opt/clashsubmanager/data | tail -1
echo ""

# 活跃连接数
echo "活跃连接数:"
netstat -an | grep :8080 | grep ESTABLISHED | wc -l
echo ""

# 最近错误数
echo "最近1小时错误数:"
grep ERROR /opt/clashsubmanager/logs/error-$(date +%Y-%m-%d).log 2>/dev/null | wc -l
```

**2. 性能基准测试**
```bash
# 使用 ab (Apache Bench) 测试
ab -n 1000 -c 10 http://localhost:8080/health

# 使用 wrk 测试
wrk -t4 -c100 -d30s http://localhost:8080/health
```

### 11.3 故障排查进阶

#### 常见问题诊断流程

**问题1：服务响应缓慢**

```bash
# 步骤1：检查系统资源
top -bn1 | grep clashsubmanager
docker stats clashsubmanager --no-stream

# 步骤2：检查网络连接
netstat -an | grep :8080 | wc -l
ss -s

# 步骤3：检查日志
tail -100 /opt/clashsubmanager/logs/app-$(date +%Y-%m-%d).log | grep -i "slow\|timeout"

# 步骤4：检查上游订阅服务
curl -w "@curl-format.txt" -o /dev/null -s "YOUR_SUBSCRIPTION_URL"

# curl-format.txt 内容：
# time_namelookup:  %{time_namelookup}\n
# time_connect:  %{time_connect}\n
# time_starttransfer:  %{time_starttransfer}\n
# time_total:  %{time_total}\n

# 步骤5：重启服务（如果必要）
docker restart clashsubmanager
```

**问题2：内存占用过高**

```bash
# 步骤1：检查内存使用详情
docker stats clashsubmanager --no-stream

# 步骤2：检查用户数量
find /opt/clashsubmanager/data -type d -name "[0-9]*" | wc -l

# 步骤3：清理缓存和临时文件
docker exec clashsubmanager find /tmp -type f -delete

# 步骤4：调整资源限制
# 编辑 docker-compose.yml，增加内存限制
docker-compose up -d

# 步骤5：考虑升级硬件或优化配置
```

**问题3：订阅生成失败**

```bash
# 步骤1：检查错误日志
grep "subscription" /opt/clashsubmanager/logs/error-$(date +%Y-%m-%d).log

# 步骤2：验证环境变量
docker exec clashsubmanager env | grep SUBSCRIPTION

# 步骤3：测试上游订阅
curl -v "YOUR_SUBSCRIPTION_URL"

# 步骤4：检查配置文件
docker exec clashsubmanager ls -la /app/data/

# 步骤5：查看详细日志
docker exec clashsubmanager cat /app/logs/app-$(date +%Y-%m-%d).log | grep -A 10 "subscription"
```

**问题4：用户无法访问**

```bash
# 步骤1：检查服务状态
docker ps | grep clashsubmanager
curl http://localhost:8080/health

# 步骤2：检查网络配置
docker port clashsubmanager
netstat -tlnp | grep 8080

# 步骤3：检查防火墙
sudo ufw status
sudo iptables -L -n | grep 8080

# 步骤4：检查反向代理
sudo nginx -t
sudo systemctl status nginx

# 步骤5：检查 SSL 证书（如果使用 HTTPS）
echo | openssl s_client -connect your-domain.com:443 2>/dev/null | openssl x509 -noout -dates
```

#### 调试技巧

**1. 启用详细日志**
```bash
# 临时启用调试模式
docker stop clashsubmanager
docker run -d \
  --name clashsubmanager \
  -p 8080:80 \
  -v /opt/clashsubmanager/data:/app/data \
  -v /opt/clashsubmanager/logs:/app/logs \
  --env-file .env.production \
  -e LOG_LEVEL=Debug \
  -e ASPNETCORE_ENVIRONMENT=Development \
  clashsubmanager:latest

# 查看详细日志
docker logs -f clashsubmanager
```

**2. 进入容器调试**
```bash
# 进入容器
docker exec -it clashsubmanager /bin/bash

# 检查文件系统
ls -la /app/data
cat /app/data/cloudflare-ip.csv

# 检查进程
ps aux

# 检查网络
netstat -tlnp
curl http://localhost:80/health

# 退出容器
exit
```

**3. 抓包分析**
```bash
# 安装 tcpdump
sudo apt install tcpdump -y

# 抓取 8080 端口流量
sudo tcpdump -i any -w /tmp/clashsubmanager.pcap port 8080

# 使用 Wireshark 分析
# 或使用 tcpdump 查看
sudo tcpdump -r /tmp/clashsubmanager.pcap -A
```

#### 应急恢复流程

**1. 服务完全不可用**
```bash
#!/bin/bash
echo "=== 应急恢复流程 ==="

# 1. 停止服务
docker stop clashsubmanager
docker rm clashsubmanager

# 2. 备份当前数据
tar -czf /backup/emergency_$(date +%Y%m%d_%H%M%S).tar.gz /opt/clashsubmanager/data

# 3. 恢复最近的备份
LATEST_BACKUP=$(ls -t /backup/clashsubmanager/data_*.tar.gz | head -1)
tar -xzf $LATEST_BACKUP -C /opt/clashsubmanager/

# 4. 重新启动服务
docker-compose up -d

# 5. 验证服务
sleep 10
curl http://localhost:8080/health

echo "=== 恢复完成 ==="
```

**2. 数据损坏恢复**
```bash
#!/bin/bash
echo "=== 数据损坏恢复 ==="

# 1. 停止服务
docker stop clashsubmanager

# 2. 检查数据完整性
find /opt/clashsubmanager/data -type f -name "*.csv" -exec file {} \;
find /opt/clashsubmanager/data -type f -name "*.yaml" -exec file {} \;

# 3. 恢复默认配置
docker run --rm \
  -v /opt/clashsubmanager/data:/app/data \
  clashsubmanager:latest \
  /bin/bash -c "cp /app/defaults/* /app/data/"

# 4. 重启服务
docker start clashsubmanager

echo "=== 恢复完成 ==="
```

### 11.4 安全审计

#### 定期安全检查清单

```bash
#!/bin/bash
# /opt/clashsubmanager/scripts/security-audit.sh

echo "=== ClashSubManager 安全审计 ==="
echo "审计时间: $(date)"
echo ""

# 1. 检查环境变量文件权限
echo "1. 环境变量文件权限:"
ls -l /opt/clashsubmanager/.env.production
if [ $(stat -c %a /opt/clashsubmanager/.env.production) != "600" ]; then
    echo "⚠️ 警告: 权限不安全，应该是 600"
fi
echo ""

# 2. 检查数据目录权限
echo "2. 数据目录权限:"
ls -ld /opt/clashsubmanager/data
echo ""

# 3. 检查密码强度
echo "3. 检查管理员密码长度:"
PASS_LENGTH=$(grep AdminPassword /opt/clashsubmanager/.env.production | cut -d= -f2 | wc -c)
if [ $PASS_LENGTH -lt 12 ]; then
    echo "⚠️ 警告: 密码长度不足12位"
else
    echo "✅ 密码长度符合要求"
fi
echo ""

# 4. 检查 SSL 证书有效期
echo "4. SSL 证书有效期:"
if [ -f /etc/letsencrypt/live/your-domain.com/cert.pem ]; then
    openssl x509 -in /etc/letsencrypt/live/your-domain.com/cert.pem -noout -enddate
else
    echo "⚠️ 未找到 SSL 证书"
fi
echo ""

# 5. 检查开放端口
echo "5. 开放端口:"
netstat -tlnp | grep -E ":(80|443|8080)"
echo ""

# 6. 检查最近的失败登录
echo "6. 最近的失败登录尝试:"
grep -i "login.*fail" /opt/clashsubmanager/logs/app-$(date +%Y-%m-%d).log 2>/dev/null | tail -5
echo ""

# 7. 检查 Docker 镜像更新
echo "7. Docker 镜像版本:"
docker images clashsubmanager
echo ""

echo "=== 审计完成 ==="
```

## 12. 附录

### 12.1 环境变量生成脚本
```bash
#!/bin/bash
echo "=== 生成环境变量 ==="

# 生成管理员密码
AdminPassword=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-12)
echo "AdminPassword=$AdminPassword"

# 生成Cookie密钥
CookieSecretKey=$(openssl rand -hex 16)
echo "CookieSecretKey=$CookieSecretKey"

# 生成会话密钥
SESSION_SECRET=$(openssl rand -hex 16)
echo "SESSION_SECRET=$SESSION_SECRET"

echo "=== 生成完成 ==="
```

### 12.2 Docker Compose模板
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
      - AdminUsername=${AdminUsername}
      - AdminPassword=${AdminPassword}
      - CookieSecretKey=${CookieSecretKey}
      - SessionTimeoutMinutes=${SessionTimeoutMinutes:-30}
      - DataPath=/app/data
      - SUBSCRIPTION_URL_TEMPLATE=${SUBSCRIPTION_URL_TEMPLATE}
      - LOG_LEVEL=${LOG_LEVEL:-Information}
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M

  # 可选：添加监控
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
    restart: unless-stopped
```