# ClashSubManager 部署运维文档

**🌐 语言**: [English](deployment-guide.md) | [中文](deployment-guide-cn.md)

## 1. 部署概述

### 1.1 系统架构
- **架构模式**：单体应用架构
- **技术栈**：.NET 10 + ASP.NET Core Razor Pages
- **部署方式**：Docker容器化部署
- **数据存储**：本地文件系统

### 1.2 系统要求
- **操作系统**：Linux (推荐) / Windows / macOS
- **Docker版本**：20.10+
- **内存要求**：最低512MB，推荐1GB
- **存储要求**：最低1GB，推荐5GB
- **网络要求**：可访问外网（订阅服务调用）

## 2. 快速部署

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
      - ADMIN_USERNAME=admin
      - ADMIN_PASSWORD=your_secure_password_here
      - COOKIE_SECRET_KEY=your_hmac_key_at_least_32_chars_long
      - SESSION_TIMEOUT_MINUTES=30
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

**步骤2：启动服务**
```bash
# 创建数据目录
mkdir -p data logs

# 启动服务
docker-compose up -d

# 查看日志
docker-compose logs -f clashsubmanager
```

### 2.2 使用Docker命令

**步骤1：拉取镜像**
```bash
docker pull clashsubmanager:latest
```

**步骤2：创建数据目录**
```bash
mkdir -p /opt/clashsubmanager/data
mkdir -p /opt/clashsubmanager/logs
```

**步骤3：启动容器**
```bash
docker run -d \
  --name clashsubmanager \
  -p 8080:80 \
  -v /opt/clashsubmanager/data:/app/data \
  -v /opt/clashsubmanager/logs:/app/logs \
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=your_secure_password_here \
  -e COOKIE_SECRET_KEY=your_hmac_key_at_least_32_chars_long \
  -e SESSION_TIMEOUT_MINUTES=30 \
  --restart unless-stopped \
  clashsubmanager:latest
```

## 3. 环境变量配置

### 3.1 必需环境变量

| 变量名 | 说明 | 示例值 | 要求 |
|--------|------|--------|------|
| `ADMIN_USERNAME` | 管理员用户名 | `admin` | 非空 |
| `ADMIN_PASSWORD` | 管理员密码 | `SecurePass123!` | 非空，建议强密码 |
| `COOKIE_SECRET_KEY` | Cookie签名密钥 | `32_character_long_secret_key` | ≥32字符 |
| `SESSION_TIMEOUT_MINUTES` | 会话超时时间（分钟） | `30` | 5-1440 |

### 3.2 可选环境变量

| 变量名 | 说明 | 默认值 | 说明 |
|--------|------|--------|------|
| `ASPNETCORE_ENVIRONMENT` | 运行环境 | `Production` | Development/Production |
| `LOG_LEVEL` | 日志级别 | `Information` | Debug/Information/Warning/Error |
| `MAX_CONCURRENT_REQUESTS` | 最大并发请求数 | `50` | 10-100 |
| `REQUEST_RATE_LIMIT` | 请求频率限制（每秒） | `10` | 1-20 |

### 3.3 安全配置建议

**管理员密码**
```bash
# 生成强密码（至少12位，包含大小写字母、数字、特殊字符）
ADMIN_PASSWORD=MySecureP@ssw0rd2024!
```

**Cookie密钥**
```bash
# 生成32字符随机密钥
COOKIE_SECRET_KEY=$(openssl rand -hex 16)
```

**会话超时**
```bash
# 生产环境建议较短超时
SESSION_TIMEOUT_MINUTES=30

# 管理环境可设置较长超时
SESSION_TIMEOUT_MINUTES=120
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
      - ADMIN_USERNAME=admin
      - ADMIN_PASSWORD=your_secure_password_here
      - COOKIE_SECRET_KEY=your_hmac_key_at_least_32_chars_long
      - SESSION_TIMEOUT_MINUTES=30
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
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=your_secure_password_here \
  -e COOKIE_SECRET_KEY=your_hmac_key_at_least_32_chars_long \
  -e SESSION_TIMEOUT_MINUTES=30 \
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
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=your_secure_password_here \
  -e COOKIE_SECRET_KEY=your_hmac_key_at_least_32_chars_long \
  -e SESSION_TIMEOUT_MINUTES=30 \
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
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=your_secure_password_here \
  -e COOKIE_SECRET_KEY=your_hmac_key_at_least_32_chars_long \
  -e SESSION_TIMEOUT_MINUTES=30 \
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

## 11. 附录

### 11.1 环境变量生成脚本
```bash
#!/bin/bash
echo "=== 生成环境变量 ==="

# 生成管理员密码
ADMIN_PASSWORD=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-12)
echo "ADMIN_PASSWORD=$ADMIN_PASSWORD"

# 生成Cookie密钥
COOKIE_SECRET_KEY=$(openssl rand -hex 16)
echo "COOKIE_SECRET_KEY=$COOKIE_SECRET_KEY"

# 生成会话密钥
SESSION_SECRET=$(openssl rand -hex 16)
echo "SESSION_SECRET=$SESSION_SECRET"

echo "=== 生成完成 ==="
```

### 11.2 Docker Compose模板
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
      - ADMIN_USERNAME=${ADMIN_USERNAME}
      - ADMIN_PASSWORD=${ADMIN_PASSWORD}
      - COOKIE_SECRET_KEY=${COOKIE_SECRET_KEY}
      - SESSION_TIMEOUT_MINUTES=${SESSION_TIMEOUT_MINUTES:-30}
      - LOG_LEVEL=${LOG_LEVEL:-Information}
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

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