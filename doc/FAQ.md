# ClashSubManager Frequently Asked Questions (FAQ)

**🌐 Language**: [English](FAQ.md) | [中文](FAQ-CN.md)

---

## 📋 Table of Contents

- [Deployment](#deployment)
- [Usage](#usage)
- [Configuration](#configuration)
- [Performance](#performance)
- [Troubleshooting](#troubleshooting)

---

## Deployment

### Q1: Docker fails to start with "port already in use" error?

**Error message:**
```
Error starting userland proxy: listen tcp4 0.0.0.0:8080: bind: address already in use
```

**Solutions:**

**Method 1: Change mapped port**
```yaml
ports:
  - "8081:80"  # Change 8080 to 8081 or another available port
```

**Method 2: Stop process using the port**
```bash
# Linux/macOS
sudo lsof -i :8080
sudo kill -9 <PID>

# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F
```

### Q2: Cannot access admin interface after container starts?

**Possible causes:**
1. Firewall blocking port access
2. Docker network configuration issue
3. Port mapping configuration error

**Troubleshooting steps:**

```bash
# 1. Check if container is running
docker ps | grep clashsubmanager

# 2. Check container logs
docker logs clashsubmanager

# 3. Check port mapping
docker port clashsubmanager

# 4. Test local access
curl http://localhost:8080/health

# 5. Check firewall (Linux)
sudo ufw status
sudo ufw allow 8080
```

### Q3: Cannot login due to environment variable configuration error?

**Symptoms:**
- Cannot login with correct username/password
- "Invalid credentials" error

**Solutions:**

```bash
# 1. Check if environment variables are set correctly
docker exec clashsubmanager printenv | grep Admin

# 2. Reset environment variables
docker-compose down
# Edit docker-compose.yml to ensure correct environment variables
docker-compose up -d

# 3. Check cookie secret key length (at least 32 characters)
environment:
  - CookieSecretKey=your_secret_key_at_least_32_chars_long_here
```

### Q4: Permission denied error when writing to data directory?

**Error message:**
```
Permission denied: '/app/data/cloudflare-ip.csv'
```

**Solutions:**

```bash
# 1. Check data directory permissions
ls -la ./data

# 2. Change directory permissions
sudo chown -R 1000:1000 ./data
sudo chmod -R 755 ./data

# 3. Or specify user in docker-compose.yml
services:
  clashsubmanager:
    user: "1000:1000"
```

### Q5: How to upgrade to the latest version?

**Steps:**

```bash
# 1. Backup data
cp -r ./data ./data.backup

# 2. Stop container
docker-compose down

# 3. Pull latest image
docker pull clashsubmanager:latest

# 4. Start new version
docker-compose up -d

# 5. Check logs
docker logs -f clashsubmanager
```

---

## Usage

### Q6: Subscription URL returns 404?

**Possible causes:**
1. Incorrect user ID
2. Upstream subscription service unavailable
3. Original subscription URL misconfigured

**Troubleshooting steps:**

```bash
# 1. Check subscription URL format
# Correct format: http://your-domain:8080/sub/user123

# 2. Test if upstream subscription is available
curl -A "clash" https://your-airport.com/sub/user123

# 3. View container logs
docker logs clashsubmanager | grep "user123"

# 4. Verify upstream subscription URL in environment variables
docker exec clashsubmanager printenv | grep SUBSCRIPTION_URL
```

### Q7: Configuration not taking effect, nodes not replaced with optimized IPs?

**Possible causes:**
1. Optimized IP file doesn't exist or has wrong format
2. Node's server field is not a domain
3. Configuration priority issue

**Checklist:**

```bash
# 1. Check if optimized IP file exists
docker exec clashsubmanager ls -la /app/data/cloudflare-ip.csv

# 2. Check file content format
docker exec clashsubmanager cat /app/data/cloudflare-ip.csv

# 3. Confirm node server field is domain not IP
# Only domain nodes will be expanded, IP nodes remain unchanged

# 4. View final generated configuration
curl http://localhost:8080/sub/user123 > final-config.yaml
cat final-config.yaml | grep -A 5 "proxies:"
```

### Q8: Optimized IP feature not working, all nodes are original?

**Analysis:**

Optimized IP feature only works for **domain nodes**, not **IP nodes**.

**Example:**

```yaml
# Will be expanded (server is domain)
- name: "US-Node"
  server: cdn.example.com  # Domain
  
# Will not be expanded (server is IP)
- name: "HK-Node"
  server: 104.28.1.1  # IP address
```

**Solution:**

If all nodes in your subscription are IP addresses, the optimized IP feature will not work. This is by design, as IP nodes typically don't need optimization.

### Q9: Node count is abnormally high, more than expected?

**Reason:**

This is normal behavior. The optimized IP feature expands each domain node into multiple nodes.

**Formula:**
```
Final node count = Original nodes + (Domain nodes × Optimized IP count)
```

**Example:**
- Original subscription: 10 nodes (5 domain nodes, 5 IP nodes)
- Optimized IPs: 3
- Final node count: 10 + (5 × 3) = 25 nodes

**Optimization suggestions:**
- Reduce optimized IP count (recommended 3-5)
- Use proxy groups to manage large numbers of nodes
- Enable optimized IPs only for specific users

### Q10: How to disable optimized IP feature?

**Method 1: Delete optimized IP file**

Delete via admin interface, or:

```bash
docker exec clashsubmanager rm /app/data/cloudflare-ip.csv
```

**Method 2: Use empty optimized IP file**

Upload empty content via admin interface, or create empty file:

```bash
docker exec clashsubmanager sh -c "echo '' > /app/data/cloudflare-ip.csv"
```

---

## Configuration

### Q11: How to get Cloudflare optimized IPs?

**Using CloudflareST tool:**

```bash
# 1. Download CloudflareST
wget https://github.com/XIU2/CloudflareSpeedTest/releases/download/v2.2.5/CloudflareST_linux_amd64.tar.gz
tar -zxvf CloudflareST_linux_amd64.tar.gz

# 2. Run speed test
./CloudflareST -n 200 -t 10 -o result.csv

# 3. View results
cat result.csv

# 4. Upload to ClashSubManager
# Method 1: Upload via admin interface
# Method 2: Upload via API
curl -X POST http://localhost:8080/sub/user123 \
  -H "Content-Type: text/csv" \
  --data-binary @result.csv
```

**Recommended parameters:**
- `-n 200`: Test 200 IPs
- `-t 10`: Test each IP 10 times
- `-tl 200`: Average latency limit 200ms
- `-sl 5`: Download speed minimum 5MB/s

### Q12: How to write Clash templates?

**Basic template example:**

```yaml
# /app/data/clash.yaml

# Proxy group configuration
proxy-groups:
  - name: "Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300

# Rule configuration
rules:
  - DOMAIN-SUFFIX,cn,DIRECT
  - GEOIP,CN,DIRECT
  - MATCH,Auto
```

**Advanced template example:**

```yaml
# DNS configuration
dns:
  enable: true
  nameserver:
    - 223.5.5.5
    - 119.29.29.29
  fallback:
    - 8.8.8.8
    - 1.1.1.1

# Proxy groups
proxy-groups:
  - name: "Proxy"
    type: select
    proxies:
      - Auto
      - DIRECT
  
  - name: "Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300

# Rules
rules:
  - DOMAIN-SUFFIX,openai.com,Proxy
  - DOMAIN-SUFFIX,github.com,Proxy
  - GEOIP,CN,DIRECT
  - MATCH,Proxy
```

**Notes:**
- Template configuration will be merged with original subscription
- Node names referenced in proxy groups must exist
- Rules are matched from top to bottom

### Q13: What is the configuration priority?

**Priority order:**

```
User-specific config > Global default config > Original subscription
```

**File paths:**
- Global config: `/app/data/clash.yaml`
- User config: `/app/data/user123/clash.yaml`

**Merge rules:**

1. **Array fields** (proxies, rules): Append merge
2. **Object fields** (dns, tun): Override merge
3. **Simple fields** (port, mode): Direct override

See [Advanced Guide - Configuration Priority](advanced-guide.md#14-deep-understanding-of-configuration-priority)

### Q14: User-specific configuration not taking effect?

**Troubleshooting steps:**

```bash
# 1. Check if user directory exists
docker exec clashsubmanager ls -la /app/data/user123/

# 2. Check if config file exists
docker exec clashsubmanager cat /app/data/user123/clash.yaml

# 3. Validate YAML format
# Use online YAML validator

# 4. View logs
docker logs clashsubmanager | grep "user123"

# 5. Get final config to verify
curl http://localhost:8080/sub/user123 > final.yaml
```

### Q15: How to set different optimized IPs for different users?

**Steps:**

1. **Via admin interface:**
   - Visit `/Admin/DefaultIPs`
   - Select specific user in user selector
   - Upload or paste optimized IP list for that user

2. **Via API:**
```bash
# Set user-specific optimized IPs for user123
curl -X POST http://localhost:8080/sub/user123 \
  -H "Content-Type: text/csv" \
  --data-binary @user123-ips.csv

# Set user-specific optimized IPs for user456
curl -X POST http://localhost:8080/sub/user456 \
  -H "Content-Type: text/csv" \
  --data-binary @user456-ips.csv
```

3. **File structure:**
```
/app/data/
├── cloudflare-ip.csv          # Global optimized IPs
├── user123/
│   └── cloudflare-ip.csv      # user123 specific
└── user456/
    └── cloudflare-ip.csv      # user456 specific
```

---

## Performance

### Q16: Subscription request response is slow?

**Possible causes:**
1. Upstream subscription service is slow
2. Too many nodes
3. Configuration file too large
4. Insufficient server resources

**Optimization solutions:**

```bash
# 1. Test upstream subscription response time
time curl -A "clash" https://your-airport.com/sub/user123

# 2. Reduce optimized IP count
# From 10 to 3-5

# 3. Limit Docker resources
services:
  clashsubmanager:
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M

# 4. Set log level to Warning
environment:
  - Logging__LogLevel__Default=Warning
```

### Q17: Memory usage too high?

**Normal memory usage:**
- Idle state: 50-100MB
- Processing requests: 100-200MB
- Large number of nodes: 200-500MB

**Optimization suggestions:**

```yaml
# 1. Limit Docker memory
deploy:
  resources:
    limits:
      memory: 512M

# 2. Reduce log output
environment:
  - Logging__LogLevel__Default=Warning

# 3. Clean logs periodically
docker exec clashsubmanager sh -c "echo '' > /app/logs/app.log"
```

### Q18: How to improve concurrent processing capacity?

**Solution 1: Increase resource limits**

```yaml
deploy:
  resources:
    limits:
      cpus: '2.0'
      memory: 1G
```

**Solution 2: Use load balancing**

Deploy multiple instances, use Nginx for load balancing:

```nginx
upstream clashsubmanager {
    server 127.0.0.1:8080;
    server 127.0.0.1:8081;
    server 127.0.0.1:8082;
}

server {
    listen 80;
    location / {
        proxy_pass http://clashsubmanager;
    }
}
```

### Q19: Subscription update fails with timeout?

**Reasons:**
- Upstream subscription service is slow or unavailable
- Network connection issue
- Firewall blocking

**Solutions:**

```bash
# 1. Test network connection
docker exec clashsubmanager curl -v https://your-airport.com

# 2. Check DNS resolution
docker exec clashsubmanager nslookup your-airport.com

# 3. Increase timeout (if supported)
# Configure in environment variables
environment:
  - HTTP_TIMEOUT=30

# 4. Use proxy to access upstream subscription (if needed)
environment:
  - HTTP_PROXY=http://proxy-server:port
```

---

## Troubleshooting

### Q20: How to view detailed logs?

**Method 1: Docker logs**

```bash
# View real-time logs
docker logs -f clashsubmanager

# View last 100 lines
docker logs --tail 100 clashsubmanager

# View specific time period
docker logs --since "2024-01-01T00:00:00" clashsubmanager

# View only error logs
docker logs clashsubmanager 2>&1 | grep ERROR
```

**Method 2: Log files**

```bash
# View application logs
docker exec clashsubmanager cat /app/logs/app.log

# Monitor in real-time
docker exec clashsubmanager tail -f /app/logs/app.log
```

**Method 3: Enable verbose logging**

```yaml
environment:
  - Logging__LogLevel__Default=Debug
  - Logging__LogLevel__Microsoft=Information
```

### Q21: How to verify configuration is correct?

**Steps:**

```bash
# 1. Get final generated configuration
curl http://localhost:8080/sub/user123 > final-config.yaml

# 2. Validate YAML format
# Use online tool: https://www.yamllint.com/

# 3. Check node count
grep "^  - name:" final-config.yaml | wc -l

# 4. Check rules
grep -A 20 "^rules:" final-config.yaml

# 5. Check proxy groups
grep -A 10 "^proxy-groups:" final-config.yaml

# 6. Test in Clash client
# Import config into Clash, check for error messages
```

### Q22: Health check fails?

**Check commands:**

```bash
# 1. Access health check endpoint
curl http://localhost:8080/health

# Expected response
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456"
}

# 2. If failed, check container status
docker ps -a | grep clashsubmanager

# 3. Check if port is listening
docker exec clashsubmanager netstat -tlnp | grep 80

# 4. Restart container
docker restart clashsubmanager
```

### Q23: How to reset all configurations?

**Warning: This will delete all custom configurations!**

```bash
# 1. Stop container
docker-compose down

# 2. Backup data (optional)
cp -r ./data ./data.backup

# 3. Delete data directory
rm -rf ./data/*

# 4. Restart
docker-compose up -d

# 5. Reconfigure admin account and other settings
```

### Q24: How to export and import configurations?

**Export configuration:**

```bash
# Export all configurations
docker exec clashsubmanager tar -czf /tmp/config-backup.tar.gz /app/data
docker cp clashsubmanager:/tmp/config-backup.tar.gz ./config-backup.tar.gz

# Or directly copy data directory
cp -r ./data ./data-backup-$(date +%Y%m%d)
```

**Import configuration:**

```bash
# Stop container
docker-compose down

# Extract configuration
tar -xzf config-backup.tar.gz -C ./data

# Start container
docker-compose up -d
```

### Q25: What to do if encountering other issues?

**Ways to get help:**

1. **Check documentation:**
   - [Deployment and Operations Guide](deployment/deployment-guide.md)
   - [Advanced Usage Guide](advanced-guide.md)
   - [Environment Variable Configuration](deployment/env-config.md)

2. **Check logs:**
   ```bash
   docker logs clashsubmanager
   ```

3. **Submit Issue:**
   - Visit project GitHub repository
   - Provide detailed error information and logs
   - Describe your environment and configuration

4. **Community discussion:**
   - Ask questions in GitHub Discussions
   - Search existing issues and answers

---

## 💡 Tips

- Regularly backup configuration files
- Keep Docker images updated
- Monitor system resource usage
- Check logs to troubleshoot issues
- Test configurations before applying to production

---

## 📚 Related Documentation

- [Deployment and Operations Guide](deployment/deployment-guide.md)
- [Advanced Usage Guide](advanced-guide.md)
- [Environment Variable Configuration](deployment/env-config.md)
- [Project Documentation Navigation](README.md)