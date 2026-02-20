# ClashSubManager Deployment Operations Guide

**🌐 Language**: [English](deployment-guide.md) | [中文](deployment-guide-cn.md)

---

## 📋 Pre-Deployment Preparation

### What Do You Need to Prepare?

Before starting deployment, ensure you have the following ready:

#### 🖥️ Hardware and System Requirements
- **Operating System**: Linux (Ubuntu 20.04+ recommended) / Windows Server / macOS
- **CPU**: 1 core (minimum), 2 cores (recommended)
- **Memory**: 512MB (minimum), 1GB (recommended), 2GB (multi-user scenarios)
- **Storage**: 1GB (minimum), 5GB (recommended), adjust based on user count
- **Network**: Stable internet connection for accessing upstream subscription services

#### 🐳 Software Dependencies
- **Docker**: Version 20.10+
- **Docker Compose**: Version 1.29+ (optional but recommended)
- **Reverse Proxy**: Nginx / Apache / Caddy (recommended for production)
- **SSL Certificate**: Let's Encrypt or other CA certificate (for HTTPS access)

#### 📝 Configuration Information
- **Upstream Subscription URL**: Your airport subscription link template
- **Admin Account**: Username and strong password
- **Domain Name**: (Optional) For public internet access
- **Optimized IP List**: (Optional) CloudflareST speed test results

### Prerequisites Knowledge

- ✅ Basic Linux command line operations
- ✅ Docker and Docker Compose fundamentals
- ✅ Basic networking knowledge (ports, firewalls)
- ⚠️ SSL/TLS certificate configuration knowledge (if HTTPS is needed)

### Estimated Deployment Time

| Deployment Scenario | Estimated Time | Difficulty |
|---------------------|----------------|------------|
| **Quick Test Deployment** | 10-15 minutes | ⭐ Easy |
| **Production Standard Deployment** | 30-45 minutes | ⭐⭐ Medium |
| **Production High Availability Deployment** | 1-2 hours | ⭐⭐⭐ Advanced |

---

## 1. Deployment Overview

### 1.1 System Architecture
- **Architecture Pattern**: Monolithic application architecture
- **Technology Stack**: .NET 10 + ASP.NET Core Razor Pages
- **Deployment Method**: Docker containerized deployment
- **Data Storage**: Local file system
- **Health Check**: Built-in `/health` endpoint with system metrics

### 1.2 Deployment Mode Selection

#### Mode 1: Quick Test Deployment (Development/Test Environment)
- **Use Case**: Local testing, feature verification
- **Features**: Quick startup, simple configuration
- **Not Suitable For**: Production environment, multi-user scenarios

#### Mode 2: Production Standard Deployment (Recommended)
- **Use Case**: Personal use, small teams (<50 users)
- **Features**: Complete configuration, secure and reliable
- **Includes**: Reverse proxy, SSL certificate, log management

#### Mode 3: Production High Availability Deployment
- **Use Case**: Large teams (>50 users), high availability requirements
- **Features**: Load balancing, data backup, monitoring and alerting
- **Includes**: Multi-instance deployment, health checks, automatic recovery

---

## 2. Quick Test Deployment (Development/Test Environment)

> ⚠️ **Note**: This deployment method is only suitable for test environments and is not recommended for production.

### 2.1 Using Docker Compose (Recommended)

**Step 1: Create docker-compose.yml**
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

**Step 2: Start Service**
```bash
# Create data directories
mkdir -p data logs

# Start service
docker-compose up -d

# View logs
docker-compose logs -f clashsubmanager

# Access service
# Open in browser: http://localhost:8080
```

### 2.2 Using Docker Commands

```bash
# One-command startup (test environment)
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

# View logs
docker logs -f clashsubmanager
```

---

## 3. Production Standard Deployment (Recommended)

> ✅ **Recommended**: This deployment method is suitable for production environments, including security configuration and reverse proxy.

### 3.1 Preparation

**Step 1: Generate Security Keys**
```bash
# Generate strong password (16 characters)
ADMIN_PASSWORD=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-16)
echo "Admin Password: $ADMIN_PASSWORD"

# Generate Cookie secret key (32 characters)
COOKIE_SECRET=$(openssl rand -hex 16)
echo "Cookie Secret: $COOKIE_SECRET"

# Save to environment file
cat > .env.production << EOF
AdminUsername=admin
AdminPassword=$ADMIN_PASSWORD
CookieSecretKey=$COOKIE_SECRET
SessionTimeoutMinutes=30
SUBSCRIPTION_URL_TEMPLATE=https://your-airport.com/sub/{userId}
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
EOF

# Set file permissions
chmod 600 .env.production
```

**Step 2: Create Directory Structure**
```bash
# Create project directory
mkdir -p /opt/clashsubmanager/{data,logs,config}
cd /opt/clashsubmanager

# Set permissions
chmod 755 data logs
```

### 3.2 Docker Compose Deployment

**Create docker-compose.yml**
```yaml
version: '3.8'

services:
  clashsubmanager:
    image: clashsubmanager:latest
    container_name: clashsubmanager
    ports:
      - "127.0.0.1:8080:80"  # Listen on localhost only, access via reverse proxy
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

**Start Service**
```bash
# Start
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f
```

### 3.3 Configure Reverse Proxy (Nginx)

**Install Nginx**
```bash
# Ubuntu/Debian
sudo apt update
sudo apt install nginx -y

# CentOS/RHEL
sudo yum install nginx -y
```

**Configure HTTP Access**
```bash
# Create configuration file
sudo nano /etc/nginx/sites-available/clashsubmanager

# Add the following content
server {
    listen 80;
    server_name your-domain.com;  # Replace with your domain
    
    # Access logs
    access_log /var/log/nginx/clashsubmanager-access.log;
    error_log /var/log/nginx/clashsubmanager-error.log;
    
    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Timeout settings
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
}

# Enable configuration
sudo ln -s /etc/nginx/sites-available/clashsubmanager /etc/nginx/sites-enabled/

# Test configuration
sudo nginx -t

# Restart Nginx
sudo systemctl restart nginx
```

### 3.4 Configure HTTPS (Let's Encrypt)

**Install Certbot**
```bash
# Ubuntu/Debian
sudo apt install certbot python3-certbot-nginx -y

# CentOS/RHEL
sudo yum install certbot python3-certbot-nginx -y
```

**Obtain SSL Certificate**
```bash
# Automatically configure HTTPS
sudo certbot --nginx -d your-domain.com

# Test automatic renewal
sudo certbot renew --dry-run
```

**Manual HTTPS Configuration (Optional)**
```bash
# Edit Nginx configuration
sudo nano /etc/nginx/sites-available/clashsubmanager

# Add HTTPS configuration
server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;  # Redirect to HTTPS
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;
    
    # SSL certificate
    ssl_certificate /etc/letsencrypt/live/your-domain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/your-domain.com/privkey.pem;
    
    # SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    
    # Access logs
    access_log /var/log/nginx/clashsubmanager-access.log;
    error_log /var/log/nginx/clashsubmanager-error.log;
    
    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Timeout settings
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
}

# Restart Nginx
sudo systemctl restart nginx
```

### 3.5 Configure Firewall

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

### 3.6 Verify Deployment

```bash
# Check service status
docker ps | grep clashsubmanager

# Check health status
curl http://localhost:8080/health

# Check HTTPS access
curl -I https://your-domain.com

# View logs
docker logs clashsubmanager | tail -50
```

---

## 4. Production High Availability Deployment

> 🚀 **Advanced**: Suitable for large teams and high availability requirements.

### 4.1 Load Balancing Configuration

**Nginx Load Balancing**
```nginx
upstream clashsubmanager_backend {
    least_conn;  # Least connections algorithm
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
        
        # Health check
        proxy_next_upstream error timeout http_500 http_502 http_503;
    }
}
```

**Multi-Instance Docker Compose**
```yaml
version: '3.8'

services:
  clashsubmanager-1:
    image: clashsubmanager:latest
    container_name: clashsubmanager-1
    ports:
      - "127.0.0.1:8080:80"
    volumes:
      - ./data:/app/data:ro  # Read-only mount
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

### 4.2 Data Backup Strategy

**Automated Backup Script**
```bash
#!/bin/bash
# /opt/clashsubmanager/scripts/backup.sh

BACKUP_DIR="/backup/clashsubmanager"
DATA_DIR="/opt/clashsubmanager/data"
DATE=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=30

# Create backup directory
mkdir -p $BACKUP_DIR

# Backup data
tar -czf $BACKUP_DIR/data_$DATE.tar.gz -C $DATA_DIR .

# Backup environment variables (encrypted)
gpg --symmetric --cipher-algo AES256 -o $BACKUP_DIR/env_$DATE.gpg /opt/clashsubmanager/.env.production

# Clean old backups
find $BACKUP_DIR -name "data_*.tar.gz" -mtime +$RETENTION_DAYS -delete
find $BACKUP_DIR -name "env_*.gpg" -mtime +$RETENTION_DAYS -delete

# Log
echo "$(date): Backup completed - $BACKUP_DIR/data_$DATE.tar.gz" >> $BACKUP_DIR/backup.log
```

**Configure Cron Job**
```bash
# Edit crontab
crontab -e

# Daily backup at 2 AM
0 2 * * * /opt/clashsubmanager/scripts/backup.sh

# Weekly log cleanup on Sunday at 3 AM
0 3 * * 0 find /opt/clashsubmanager/logs -name "*.log" -mtime +7 -delete
```

### 4.3 Monitoring and Alerting Configuration

**Health Check Script**
```bash
#!/bin/bash
# /opt/clashsubmanager/scripts/health-check.sh

HEALTH_URL="http://localhost:8080/health"
ALERT_EMAIL="admin@example.com"

# Check service health
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)

if [ $HTTP_CODE -ne 200 ]; then
    # Send alert email
    echo "ClashSubManager service abnormal, HTTP status code: $HTTP_CODE" | \
        mail -s "ClashSubManager Alert" $ALERT_EMAIL
    
    # Attempt to restart service
    docker restart clashsubmanager
    
    # Log
    echo "$(date): Service abnormal, attempted restart" >> /var/log/clashsubmanager-health.log
fi
```

**Configure Monitoring**
```bash
# Check every 5 minutes
*/5 * * * * /opt/clashsubmanager/scripts/health-check.sh
```

---

## 5. Environment Variable Configuration Details

> 💡 **Tip**: For complete environment variable configuration instructions, refer to [Environment Variable Configuration Documentation](env-config.md)

### 5.1 Required vs Optional Environment Variables

#### 🔴 Required Variables (Must Configure)

| Variable Name | Description | Production Recommended | Test Example |
|---------------|-------------|------------------------|--------------|
| `AdminUsername` | Admin username | `admin` | `admin` |
| `AdminPassword` | Admin password | Strong password (16+ chars) | `test123` |
| `CookieSecretKey` | Cookie signing key | Random generated (32+ chars) | `test_secret_key_32_chars_long` |
| `SUBSCRIPTION_URL_TEMPLATE` | Upstream subscription URL template | Your airport subscription URL | `https://api.example.com/sub/{userId}` |

#### 🟡 Recommended Variables (Suggested Configuration)

| Variable Name | Description | Production Recommended | Default |
|---------------|-------------|------------------------|---------|
| `SessionTimeoutMinutes` | Session timeout | `30` | `60` |
| `LOG_LEVEL` | Log level | `Information` | `Information` |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` | `Production` |

#### 🟢 Optional Variables (Configure as Needed)

| Variable Name | Description | Default |
|---------------|-------------|---------|
| `DataPath` | Data directory path | `/app/data` |
| `SubscriptionUrlTemplate` | Subscription URL template (fallback) | None |

### 5.2 Production Environment Recommended Configuration

```bash
# .env.production
AdminUsername=admin
AdminPassword=<generated using openssl rand -base64 16>
CookieSecretKey=<generated using openssl rand -hex 16>
SessionTimeoutMinutes=30
SUBSCRIPTION_URL_TEMPLATE=https://your-airport.com/sub/{userId}
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
```

### 5.3 Security Configuration Best Practices

#### ✅ Password Strength Requirements
- **Length**: At least 12 characters, 16+ recommended
- **Complexity**: Include uppercase, lowercase, numbers, special characters
- **Avoid**: Common passwords, birthdays, usernames, etc.

#### ✅ Key Generation Methods
```bash
# Generate strong password
openssl rand -base64 16 | tr -d "=+/" | cut -c1-16

# Generate Cookie key (32 characters)
openssl rand -hex 16

# Generate Cookie key (64 characters, more secure)
openssl rand -hex 32
```

#### ✅ Environment File Permissions
```bash
# Set strict permissions
chmod 600 .env.production
chown root:root .env.production

# Don't commit to version control
echo ".env.production" >> .gitignore
```

## 6. Data Directory Structure

### 6.1 Directory Layout
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

### 6.2 Permission Settings
```bash
# Set data directory permissions
chmod 755 /app/data
chmod 644 /app/data/*.csv
chmod 644 /app/data/*.yaml

# Set log directory permissions
chmod 755 /app/logs
chmod 644 /app/logs/*.log
```

### 6.3 Backup Strategy
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

## 7. Network Configuration

### 7.1 Port Configuration
- **HTTP Port**: 80 (container internal)
- **HTTPS Port**: 443 (if SSL required)
- **Management Port**: 80 (shared with HTTP)

### 7.2 Reverse Proxy Configuration

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

## 8. Monitoring and Logging

### 8.1 Health Check
```bash
# Check service status
curl -f http://localhost:8080/health

# Check container status
docker ps | grep clashsubmanager

# View logs
docker logs clashsubmanager
```

### 8.2 Log Management
```bash
# View application logs
docker logs clashsubmanager

# View real-time logs
docker logs -f clashsubmanager

# View specific time logs
docker logs --since="2026-01-21T10:00:00" clashsubmanager
```

### 8.3 Log Rotation Configuration
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

## 9. Performance Optimization

### 9.1 Container Resource Limits
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

### 9.2 System Optimization
```bash
# Adjust system parameters
echo 'net.core.somaxconn = 1024' >> /etc/sysctl.conf
echo 'net.ipv4.tcp_max_syn_backlog = 1024' >> /etc/sysctl.conf
sysctl -p

# Adjust file descriptor limits
echo '* soft nofile 65536' >> /etc/security/limits.conf
echo '* hard nofile 65536' >> /etc/security/limits.conf
```

## 10. Security Hardening

### 10.1 Container Security
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

### 10.2 Network Security
```bash
# Use firewall to restrict access
ufw allow 8080/tcp
ufw enable

# Restrict management interface to specific IPs only
iptables -A INPUT -p tcp --dport 8080 -s 192.168.1.0/24 -j ACCEPT
iptables -A INPUT -p tcp --dport 8080 -j DROP
```

### 10.3 SSL/TLS Configuration
```bash
# Generate self-signed certificate (test environment)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /etc/ssl/private/clashsubmanager.key \
  -out /etc/ssl/certs/clashsubmanager.crt

# Use Let's Encrypt (production environment)
certbot --nginx -d your-domain.com
```

## 11. Troubleshooting

### 11.1 Common Issues

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

### 11.2 Debug Mode
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

## 12. Maintenance Operations

### 12.1 Daily Maintenance
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

### 12.2 Update Deployment
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

### 12.3 Data Migration
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

## 13. Operations Management

### 13.1 Log Management and Analysis

#### Log Types

ClashSubManager generates the following types of logs:

| Log Type | File Name Format | Description | Retention Period |
|----------|------------------|-------------|------------------|
| **Application Log** | `app-YYYY-MM-DD.log` | Application runtime logs, including startup, configuration loading, etc. | 30 days |
| **Access Log** | `access-YYYY-MM-DD.log` | HTTP request access logs | 7 days |
| **Error Log** | `error-YYYY-MM-DD.log` | Error and exception logs | 90 days |

#### Log Viewing Commands

```bash
# View real-time logs
docker logs -f clashsubmanager

# View last 100 lines
docker logs --tail 100 clashsubmanager

# View logs for specific time period
docker logs --since="2026-02-20T10:00:00" --until="2026-02-20T12:00:00" clashsubmanager

# View error logs
docker logs clashsubmanager 2>&1 | grep -i error

# View application log file
tail -f /opt/clashsubmanager/logs/app-$(date +%Y-%m-%d).log
```

#### Log Analysis Techniques

**1. Analyze Access Frequency**
```bash
# Count hourly access volume
grep "$(date +%Y-%m-%d)" /opt/clashsubmanager/logs/access-*.log | \
  awk '{print $4}' | cut -d: -f2 | sort | uniq -c

# Count most active user IDs
grep "userId=" /opt/clashsubmanager/logs/access-*.log | \
  sed 's/.*userId=\([^&]*\).*/\1/' | sort | uniq -c | sort -rn | head -10
```

**2. Analyze Error Patterns**
```bash
# Count error types
grep ERROR /opt/clashsubmanager/logs/error-*.log | \
  awk '{print $5}' | sort | uniq -c | sort -rn

# Find timeout errors
grep -i "timeout" /opt/clashsubmanager/logs/error-*.log

# Find subscription fetch failures
grep "subscription.*fail" /opt/clashsubmanager/logs/app-*.log
```

**3. Performance Analysis**
```bash
# Calculate average response time
grep "ResponseTime" /opt/clashsubmanager/logs/access-*.log | \
  awk '{sum+=$NF; count++} END {print "Average response time:", sum/count, "ms"}'

# Find slow requests (>1000ms)
grep "ResponseTime" /opt/clashsubmanager/logs/access-*.log | \
  awk '$NF > 1000 {print}'
```

#### Log Rotation Configuration

**Using logrotate (Recommended)**
```bash
# Create configuration file
sudo tee /etc/logrotate.d/clashsubmanager << EOF
/opt/clashsubmanager/logs/*.log {
    daily                    # Rotate daily
    rotate 30                # Keep 30 backups
    compress                 # Compress old logs
    delaycompress            # Delay compression (keep most recent uncompressed)
    missingok                # Don't error if file is missing
    notifempty               # Don't rotate empty files
    create 644 root root     # Permissions for new files
    dateext                  # Use date as suffix
    dateformat -%Y%m%d       # Date format
    maxage 90                # Delete logs older than 90 days
    
    postrotate
        # Notify application to reopen log files
        docker kill -s USR1 clashsubmanager 2>/dev/null || true
    endscript
}
EOF

# Test configuration
sudo logrotate -d /etc/logrotate.d/clashsubmanager

# Manually execute rotation
sudo logrotate -f /etc/logrotate.d/clashsubmanager
```

**Using Cron Jobs for Cleanup**
```bash
# Add to crontab
crontab -e

# Clean access logs older than 7 days at 3 AM daily
0 3 * * * find /opt/clashsubmanager/logs -name "access-*.log" -mtime +7 -delete

# Clean application logs older than 30 days on Sunday at 4 AM
0 4 * * 0 find /opt/clashsubmanager/logs -name "app-*.log" -mtime +30 -delete

# Keep error logs for 90 days
0 5 * * 0 find /opt/clashsubmanager/logs -name "error-*.log" -mtime +90 -delete
```

#### Centralized Log Management (Optional)

**Using ELK Stack**
```yaml
# Add log collection to docker-compose.yml
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
    # ... other configuration

  filebeat:
    image: docker.elastic.co/beats/filebeat:8.11.0
    volumes:
      - ./filebeat.yml:/usr/share/filebeat/filebeat.yml:ro
      - /opt/clashsubmanager/logs:/logs:ro
    depends_on:
      - clashsubmanager
```

### 13.2 Performance Tuning

#### System-Level Optimization

**1. Kernel Parameter Tuning**
```bash
# Edit /etc/sysctl.conf
sudo tee -a /etc/sysctl.conf << EOF
# Network optimization
net.core.somaxconn = 2048
net.core.netdev_max_backlog = 2048
net.ipv4.tcp_max_syn_backlog = 2048
net.ipv4.tcp_fin_timeout = 30
net.ipv4.tcp_keepalive_time = 600
net.ipv4.tcp_tw_reuse = 1

# File descriptors
fs.file-max = 65536
fs.inotify.max_user_watches = 524288
EOF

# Apply configuration
sudo sysctl -p
```

**2. File Descriptor Limits**
```bash
# Edit /etc/security/limits.conf
sudo tee -a /etc/security/limits.conf << EOF
* soft nofile 65536
* hard nofile 65536
* soft nproc 65536
* hard nproc 65536
EOF

# Apply immediately after re-login, or apply now
ulimit -n 65536
```

#### Docker Container Optimization

**1. Resource Limit Tuning**
```yaml
# docker-compose.yml
services:
  clashsubmanager:
    image: clashsubmanager:latest
    deploy:
      resources:
        limits:
          cpus: '2.0'              # Adjust based on actual load
          memory: 1G               # Adjust based on user count
        reservations:
          cpus: '1.0'
          memory: 512M
    # Use host network mode for better performance (optional)
    # network_mode: "host"
```

**2. Storage Optimization**
```bash
# Use overlay2 storage driver (recommended)
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

#### Application-Level Optimization

**1. Environment Variable Tuning**
```bash
# .env.production
# Reduce session timeout (reduce memory usage)
SessionTimeoutMinutes=15

# Adjust log level (use Warning or Error in production)
LOG_LEVEL=Warning

# Enable production mode
ASPNETCORE_ENVIRONMENT=Production
```

**2. Data Directory Optimization**
```bash
# Periodically clean user access records
# Create cleanup script
cat > /opt/clashsubmanager/scripts/cleanup-users.sh << 'EOF'
#!/bin/bash
# Clean user records not accessed in 30 days
find /opt/clashsubmanager/data -name "users.txt" -mtime +30 -delete
echo "$(date): Cleanup completed" >> /var/log/clashsubmanager-cleanup.log
EOF

chmod +x /opt/clashsubmanager/scripts/cleanup-users.sh

# Add to crontab (run weekly)
0 2 * * 0 /opt/clashsubmanager/scripts/cleanup-users.sh
```

#### Performance Monitoring

**1. Real-time Monitoring Script**
```bash
#!/bin/bash
# /opt/clashsubmanager/scripts/monitor.sh

echo "=== ClashSubManager Performance Monitoring ==="
echo "Time: $(date)"
echo ""

# CPU usage
echo "CPU Usage:"
docker stats clashsubmanager --no-stream --format "table {{.CPUPerc}}"
echo ""

# Memory usage
echo "Memory Usage:"
docker stats clashsubmanager --no-stream --format "table {{.MemUsage}}\t{{.MemPerc}}"
echo ""

# Network traffic
echo "Network Traffic:"
docker stats clashsubmanager --no-stream --format "table {{.NetIO}}"
echo ""

# Disk usage
echo "Disk Usage:"
df -h /opt/clashsubmanager/data | tail -1
echo ""

# Active connections
echo "Active Connections:"
netstat -an | grep :8080 | grep ESTABLISHED | wc -l
echo ""

# Recent errors in last hour
echo "Errors in Last Hour:"
grep ERROR /opt/clashsubmanager/logs/error-$(date +%Y-%m-%d).log 2>/dev/null | wc -l
```

**2. Performance Benchmarking**
```bash
# Using ab (Apache Bench)
ab -n 1000 -c 10 http://localhost:8080/health

# Using wrk
wrk -t4 -c100 -d30s http://localhost:8080/health
```

### 13.3 Advanced Troubleshooting

#### Common Issue Diagnosis Workflow

**Issue 1: Slow Service Response**

```bash
# Step 1: Check system resources
top -bn1 | grep clashsubmanager
docker stats clashsubmanager --no-stream

# Step 2: Check network connections
netstat -an | grep :8080 | wc -l
ss -s

# Step 3: Check logs
tail -100 /opt/clashsubmanager/logs/app-$(date +%Y-%m-%d).log | grep -i "slow\|timeout"

# Step 4: Check upstream subscription service
curl -w "@curl-format.txt" -o /dev/null -s "YOUR_SUBSCRIPTION_URL"

# curl-format.txt content:
# time_namelookup:  %{time_namelookup}\n
# time_connect:  %{time_connect}\n
# time_starttransfer:  %{time_starttransfer}\n
# time_total:  %{time_total}\n

# Step 5: Restart service (if necessary)
docker restart clashsubmanager
```

**Issue 2: High Memory Usage**

```bash
# Step 1: Check memory usage details
docker stats clashsubmanager --no-stream

# Step 2: Check user count
find /opt/clashsubmanager/data -type d -name "[0-9]*" | wc -l

# Step 3: Clean cache and temporary files
docker exec clashsubmanager find /tmp -type f -delete

# Step 4: Adjust resource limits
# Edit docker-compose.yml, increase memory limit
docker-compose up -d

# Step 5: Consider hardware upgrade or configuration optimization
```

**Issue 3: Subscription Generation Failure**

```bash
# Step 1: Check error logs
grep "subscription" /opt/clashsubmanager/logs/error-$(date +%Y-%m-%d).log

# Step 2: Verify environment variables
docker exec clashsubmanager env | grep SUBSCRIPTION

# Step 3: Test upstream subscription
curl -v "YOUR_SUBSCRIPTION_URL"

# Step 4: Check configuration files
docker exec clashsubmanager ls -la /app/data/

# Step 5: View detailed logs
docker exec clashsubmanager cat /app/logs/app-$(date +%Y-%m-%d).log | grep -A 10 "subscription"
```

**Issue 4: Users Cannot Access**

```bash
# Step 1: Check service status
docker ps | grep clashsubmanager
curl http://localhost:8080/health

# Step 2: Check network configuration
docker port clashsubmanager
netstat -tlnp | grep 8080

# Step 3: Check firewall
sudo ufw status
sudo iptables -L -n | grep 8080

# Step 4: Check reverse proxy
sudo nginx -t
sudo systemctl status nginx

# Step 5: Check SSL certificate (if using HTTPS)
echo | openssl s_client -connect your-domain.com:443 2>/dev/null | openssl x509 -noout -dates
```

#### Debugging Techniques

**1. Enable Verbose Logging**
```bash
# Temporarily enable debug mode
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

# View detailed logs
docker logs -f clashsubmanager
```

**2. Enter Container for Debugging**
```bash
# Enter container
docker exec -it clashsubmanager /bin/bash

# Check file system
ls -la /app/data
cat /app/data/cloudflare-ip.csv

# Check processes
ps aux

# Check network
netstat -tlnp
curl http://localhost:80/health

# Exit container
exit
```

**3. Packet Capture Analysis**
```bash
# Install tcpdump
sudo apt install tcpdump -y

# Capture traffic on port 8080
sudo tcpdump -i any -w /tmp/clashsubmanager.pcap port 8080

# Analyze with Wireshark
# Or view with tcpdump
sudo tcpdump -r /tmp/clashsubmanager.pcap -A
```

#### Emergency Recovery Procedures

**1. Service Completely Unavailable**
```bash
#!/bin/bash
echo "=== Emergency Recovery Procedure ==="

# 1. Stop service
docker stop clashsubmanager
docker rm clashsubmanager

# 2. Backup current data
tar -czf /backup/emergency_$(date +%Y%m%d_%H%M%S).tar.gz /opt/clashsubmanager/data

# 3. Restore latest backup
LATEST_BACKUP=$(ls -t /backup/clashsubmanager/data_*.tar.gz | head -1)
tar -xzf $LATEST_BACKUP -C /opt/clashsubmanager/

# 4. Restart service
docker-compose up -d

# 5. Verify service
sleep 10
curl http://localhost:8080/health

echo "=== Recovery Complete ==="
```

**2. Data Corruption Recovery**
```bash
#!/bin/bash
echo "=== Data Corruption Recovery ==="

# 1. Stop service
docker stop clashsubmanager

# 2. Check data integrity
find /opt/clashsubmanager/data -type f -name "*.csv" -exec file {} \;
find /opt/clashsubmanager/data -type f -name "*.yaml" -exec file {} \;

# 3. Restore default configuration
docker run --rm \
  -v /opt/clashsubmanager/data:/app/data \
  clashsubmanager:latest \
  /bin/bash -c "cp /app/defaults/* /app/data/"

# 4. Restart service
docker start clashsubmanager

echo "=== Recovery Complete ==="
```

### 13.4 Security Audit

#### Regular Security Checklist

```bash
#!/bin/bash
# /opt/clashsubmanager/scripts/security-audit.sh

echo "=== ClashSubManager Security Audit ==="
echo "Audit Time: $(date)"
echo ""

# 1. Check environment file permissions
echo "1. Environment File Permissions:"
ls -l /opt/clashsubmanager/.env.production
if [ $(stat -c %a /opt/clashsubmanager/.env.production) != "600" ]; then
    echo "⚠️ Warning: Insecure permissions, should be 600"
fi
echo ""

# 2. Check data directory permissions
echo "2. Data Directory Permissions:"
ls -ld /opt/clashsubmanager/data
echo ""

# 3. Check password strength
echo "3. Check Admin Password Length:"
PASS_LENGTH=$(grep AdminPassword /opt/clashsubmanager/.env.production | cut -d= -f2 | wc -c)
if [ $PASS_LENGTH -lt 12 ]; then
    echo "⚠️ Warning: Password length less than 12 characters"
else
    echo "✅ Password length meets requirements"
fi
echo ""

# 4. Check SSL certificate validity
echo "4. SSL Certificate Validity:"
if [ -f /etc/letsencrypt/live/your-domain.com/cert.pem ]; then
    openssl x509 -in /etc/letsencrypt/live/your-domain.com/cert.pem -noout -enddate
else
    echo "⚠️ SSL certificate not found"
fi
echo ""

# 5. Check open ports
echo "5. Open Ports:"
netstat -tlnp | grep -E ":(80|443|8080)"
echo ""

# 6. Check recent failed login attempts
echo "6. Recent Failed Login Attempts:"
grep -i "login.*fail" /opt/clashsubmanager/logs/app-$(date +%Y-%m-%d).log 2>/dev/null | tail -5
echo ""

# 7. Check Docker image updates
echo "7. Docker Image Version:"
docker images clashsubmanager
echo ""

echo "=== Audit Complete ==="
```

## 14. Appendix

### 14.1 Environment Variable Generation Script
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

### 14.2 Docker Compose Template
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