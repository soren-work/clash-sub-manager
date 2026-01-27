# ClashSubManager Deployment Operations Guide

**🌐 Language**: [English](deployment-guide.md) | [中文](deployment-guide-cn.md)

## 1. Deployment Overview

### 1.1 System Architecture
- **Architecture Pattern**: Monolithic application architecture
- **Technology Stack**: .NET 10 + ASP.NET Core Razor Pages
- **Deployment Method**: Docker containerized deployment
- **Data Storage**: Local file system
- **Health Check**: Built-in `/health` endpoint with system metrics

### 1.2 System Requirements
- **Operating System**: Linux (recommended) / Windows / macOS
- **Docker Version**: 20.10+
- **Memory Requirements**: Minimum 512MB, recommended 1GB
- **Storage Requirements**: Minimum 1GB, recommended 5GB
- **Network Requirements**: Internet access (for subscription service calls)

## 2. Quick Deployment

### 2.1 Using Docker Compose (Recommended)

**Step 1: Create docker-compose.yml**
```yaml
version: '3.8'

services:
  clash-sub-manager:
    build: 
      context: .
      dockerfile: doc/deployment/Dockerfile
    container_name: clash-sub-manager
    ports:
      - "80:80"
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
    restart: always
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    resource_limits:
      memory: 512M
      cpus: '0.5'
```

**Step 2: Start Service**
```bash
# Create data directories
mkdir -p data logs

# Start service
docker-compose up -d

# View logs
docker-compose logs -f clashsubmanager
```

### 2.2 Using Docker Commands

**Step 1: Pull Image**
```bash
docker pull clashsubmanager:latest
```

**Step 2: Create Data Directories**
```bash
mkdir -p /opt/clashsubmanager/data
mkdir -p /opt/clashsubmanager/logs
```

**Step 3: Start Container**
```bash
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
```

## 3. Environment Variable Configuration

### 3.1 Required Environment Variables

| Variable Name | Description | Example Value | Requirements |
|---------------|-------------|---------------|--------------|
| `AdminUsername` | Admin username | `admin` | Non-empty |
| `AdminPassword` | Admin password | `SecurePass123!` | Non-empty, strong password recommended |
| `CookieSecretKey` | Cookie signing key | `32_character_long_secret_key` | ≥32 characters |
| `SessionTimeoutMinutes` | Session timeout (minutes) | `30` | 5-1440 |
| `SUBSCRIPTION_URL_TEMPLATE` | Upstream subscription URL template (must contain `{userId}`) | `https://api.example.com/sub/{userId}` | Required to serve `/sub/{id}` |

### 3.2 Optional Environment Variables

| Variable Name | Description | Default Value | Description |
|---------------|-------------|---------------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` | Development/Production |
| `LOG_LEVEL` | Log level | `Information` | Debug/Information/Warning/Error |
| `DataPath` | Data directory (absolute or relative to executable) | Docker: `/app/data` | Optional |
| `SubscriptionUrlTemplate` | Upstream subscription URL template fallback (must contain `{userId}`) | `https://api.example.com/sub/{userId}` | Optional (fallback) |

### 3.3 Security Configuration Recommendations

**Admin Password**
```bash
# Generate strong password (at least 12 characters, including uppercase, lowercase, numbers, special characters)
AdminPassword=MySecureP@ssw0rd2024!
```

**Cookie Key**
```bash
# Generate 32-character random key
CookieSecretKey=$(openssl rand -hex 16)
```

**Session Timeout**
```bash
# Production environment recommends shorter timeout
SessionTimeoutMinutes=30

# Management environment can set longer timeout
SessionTimeoutMinutes=120
```

## 4. Data Directory Structure

### 4.1 Directory Layout
```
/app/data/
├── cloudflare-ip.csv          # Default optimized IP configuration
├── clash.yaml                 # Default Clash template
├── users.txt                  # User access records
└── [user id]/                 # User-specific configuration directory
    ├── cloudflare-ip.csv      # User-specific optimized IPs
    └── clash.yaml             # User-specific template

/app/logs/
├── app-2026-01-21.log        # Application logs
├── access-2026-01-21.log     # Access logs
└── error-2026-01-21.log      # Error logs
```

### 4.2 Permission Settings
```bash
# Set data directory permissions
chmod 755 /app/data
chmod 644 /app/data/*.csv
chmod 644 /app/data/*.yaml

# Set log directory permissions
chmod 755 /app/logs
chmod 644 /app/logs/*.log
```

### 4.3 Backup Strategy
```bash
# Create backup script
#!/bin/bash
BACKUP_DIR="/backup/clashsubmanager"
DATA_DIR="/app/data"
DATE=$(date +%Y%m%d_%H%M%S)

# Create backup directory
mkdir -p $BACKUP_DIR

# Backup data
tar -czf $BACKUP_DIR/data_$DATE.tar.gz $DATA_DIR

# Clean backups older than 7 days
find $BACKUP_DIR -name "data_*.tar.gz" -mtime +7 -delete
```

## 5. Network Configuration

### 5.1 Port Configuration
- **HTTP Port**: 80 (container internal)
- **HTTPS Port**: 443 (if SSL required)
- **Management Port**: 80 (shared with HTTP)

### 5.2 Reverse Proxy Configuration

**Nginx Configuration Example**
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

**Apache Configuration Example**
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

## 6. Monitoring and Logging

### 6.1 Health Check
```bash
# Check service status
curl -f http://localhost:8080/health

# Check container status
docker ps | grep clashsubmanager

# View logs
docker logs clashsubmanager
```

### 6.2 Log Management
```bash
# View application logs
docker logs clashsubmanager

# View real-time logs
docker logs -f clashsubmanager

# View specific time logs
docker logs --since="2026-01-21T10:00:00" clashsubmanager
```

### 6.3 Log Rotation Configuration
```bash
# Create logrotate configuration
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

## 7. Performance Optimization

### 7.1 Container Resource Limits
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

### 7.2 System Optimization
```bash
# Adjust system parameters
echo 'net.core.somaxconn = 1024' >> /etc/sysctl.conf
echo 'net.ipv4.tcp_max_syn_backlog = 1024' >> /etc/sysctl.conf
sysctl -p

# Adjust file descriptor limits
echo '* soft nofile 65536' >> /etc/security/limits.conf
echo '* hard nofile 65536' >> /etc/security/limits.conf
```

## 8. Security Hardening

### 8.1 Container Security
```bash
# Run as non-root user
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

### 8.2 Network Security
```bash
# Use firewall to restrict access
ufw allow 8080/tcp
ufw enable

# Restrict management interface to specific IPs only
iptables -A INPUT -p tcp --dport 8080 -s 192.168.1.0/24 -j ACCEPT
iptables -A INPUT -p tcp --dport 8080 -j DROP
```

### 8.3 SSL/TLS Configuration
```bash
# Generate self-signed certificate (test environment)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /etc/ssl/private/clashsubmanager.key \
  -out /etc/ssl/certs/clashsubmanager.crt

# Use Let's Encrypt (production environment)
certbot --nginx -d your-domain.com
```

## 9. Troubleshooting

### 9.1 Common Issues

**Issue 1: Container startup failure**
```bash
# Check logs
docker logs clashsubmanager

# Check configuration
docker inspect clashsubmanager

# Restart
docker restart clashsubmanager
```

**Issue 2: Unable to access management interface**
```bash
# Check port mapping
docker port clashsubmanager

# Check firewall
ufw status

# Check environment variables
docker exec clashsubmanager env | grep Admin
```

**Issue 3: Configuration file loss**
```bash
# Check data directory permissions
ls -la /app/data

# Restore backup
tar -xzf /backup/clashsubmanager/data_20260121.tar.gz -C /
```

**Issue 4: Performance issues**
```bash
# Check resource usage
docker stats clashsubmanager

# Check log errors
grep ERROR /app/logs/app-*.log

# Restart service
docker restart clashsubmanager
```

### 9.2 Debug Mode
```bash
# Enable debug logging
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

## 10. Maintenance Operations

### 10.1 Daily Maintenance
```bash
# Daily check script
#!/bin/bash
echo "=== ClashSubManager Daily Check ==="

# Check service status
if docker ps | grep -q clashsubmanager; then
    echo "✅ Service running normally"
else
    echo "❌ Service abnormal, attempting restart"
    docker restart clashsubmanager
fi

# Check disk space
DISK_USAGE=$(df /app/data | awk 'NR==2 {print $5}' | sed 's/%//')
if [ $DISK_USAGE -gt 80 ]; then
    echo "⚠️ Disk usage too high: ${DISK_USAGE}%"
fi

# Check log size
LOG_SIZE=$(du -sh /app/logs | awk '{print $1}' | sed 's/[^0-9.]//g')
if [ ${LOG_SIZE%.*} -gt 100 ]; then
    echo "⚠️ Log files too large: $LOG_SIZE"
fi

echo "=== Check Complete ==="
```

### 10.2 Update Deployment
```bash
# Update script
#!/bin/bash
echo "=== Update ClashSubManager ==="

# Backup data
tar -czf /backup/clashsubmanager/pre-update_$(date +%Y%m%d_%H%M%S).tar.gz /app/data

# Pull new image
docker pull clashsubmanager:latest

# Stop old container
docker stop clashsubmanager

# Start new container
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

# Clean old images
docker image prune -f

echo "=== Update Complete ==="
```

### 10.3 Data Migration
```bash
# Migration script
#!/bin/bash
echo "=== Data Migration ==="

OLD_DATA_DIR="/old/path/to/data"
NEW_DATA_DIR="/new/path/to/data"

# Stop service
docker stop clashsubmanager

# Migrate data
cp -r $OLD_DATA_DIR/* $NEW_DATA_DIR/

# Set permissions
chown -R 1000:1000 $NEW_DATA_DIR
chmod -R 755 $NEW_DATA_DIR

# Start service
docker start clashsubmanager

echo "=== Migration Complete ==="
```

## 11. Appendix

### 11.1 Environment Variable Generation Script
```bash
#!/bin/bash
echo "=== Generate Environment Variables ==="

# Generate admin password
AdminPassword=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-12)
echo "AdminPassword=$AdminPassword"

# Generate cookie key
CookieSecretKey=$(openssl rand -hex 16)
echo "CookieSecretKey=$CookieSecretKey"

# Generate session key
SESSION_SECRET=$(openssl rand -hex 16)
echo "SESSION_SECRET=$SESSION_SECRET"

echo "=== Generation Complete ==="
```

### 11.2 Docker Compose Template
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

  # Optional: Add monitoring
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
    restart: unless-stopped
```

---

**Document Version**: v1.1  
**Creation Time**: 2026-01-22  
**Maintenance Staff**: Operations Engineer  
**Update Frequency**: Update as needed
