# Configuration Examples

**🌐 Language**: [English](README.md) | [中文](README-CN.md)

---

## 📋 Table of Contents

- [Optimized IP Configuration Examples](#optimized-ip-configuration-examples)
- [Clash Template Examples](#clash-template-examples)
- [Usage Instructions](#usage-instructions)

---

## Optimized IP Configuration Examples

### Simple Format (IP List Only)

Suitable for scenarios where only IP addresses are needed. The system will automatically use these IPs to replace domain nodes.

**File**: `cloudflare-ip-simple.csv`

```csv
104.29.125.182
104.26.0.188
104.20.20.191
104.21.21.192
104.18.18.193
```

**Usage:**
1. Visit admin interface `/Admin/DefaultIPs`
2. Select "Global" or specific user
3. Paste the above content and save

### Full Format (CloudflareST Output)

Includes latency and download speed information, providing more detailed performance data.

**File**: `cloudflare-ip-full.csv`

```csv
IP Address,Sent,Received,Loss Rate,Average Latency,Download Speed (MB/s)
104.29.125.182,4,4,0.00%,152.45 ms,8.52
104.26.0.188,4,4,0.00%,158.10 ms,8.31
104.20.20.191,4,4,0.00%,161.38 ms,8.15
104.21.21.192,4,4,0.00%,165.22 ms,7.98
104.18.18.193,4,4,0.00%,168.55 ms,7.82
```

**Usage:**
1. Use CloudflareST tool for speed testing:
   ```bash
   ./CloudflareST -n 200 -t 10 -o result.csv
   ```
2. Upload the generated `result.csv` to admin interface
3. Or upload via API:
   ```bash
   curl -X POST http://localhost:8080/sub/user123 \
     -H "Content-Type: text/csv" \
     --data-binary @result.csv
   ```

### Format Description

**Simple Format:**
- One IP address per line
- No header required
- Supports IPv4 and IPv6

**Full Format:**
- First line is header (optional)
- Must contain "IP Address" column
- Other columns are optional performance data
- System will automatically recognize and parse

---

## Clash Template Examples

### Basic Template (Minimal Configuration)

Suitable for simple scenarios, only adds basic rules and proxy groups.

**File**: `clash-template-basic.yaml`

```yaml
# Basic Clash Template
# Suitable for: Personal use, simple configuration needs

# Proxy group configuration
proxy-groups:
  # Auto-select node with lowest latency
  - name: "Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300
    tolerance: 50

  # Manual proxy selection
  - name: "Proxy"
    type: select
    proxies:
      - Auto
      - DIRECT

# Rule configuration
rules:
  # LAN direct connection
  - DOMAIN-SUFFIX,local,DIRECT
  - IP-CIDR,192.168.0.0/16,DIRECT
  - IP-CIDR,10.0.0.0/8,DIRECT
  - IP-CIDR,172.16.0.0/12,DIRECT
  
  # China mainland direct connection
  - GEOIP,CN,DIRECT
  
  # Others use proxy
  - MATCH,Proxy
```

**Usage:**
1. Visit admin interface `/Admin/ClashTemplate`
2. Select "Global" or specific user
3. Paste the above content and save

### Advanced Template (Complete Configuration)

Suitable for complex scenarios, includes DNS, multiple proxy groups, detailed rules, etc.

**File**: `clash-template-advanced.yaml`

```yaml
# Advanced Clash Template
# Suitable for: Enterprise use, complex configuration needs, multi-scenario routing

# Port configuration
port: 7890
socks-port: 7891
allow-lan: true
mode: rule
log-level: info

# DNS configuration
dns:
  enable: true
  listen: 0.0.0.0:53
  enhanced-mode: fake-ip
  fake-ip-range: 198.18.0.1/16
  nameserver:
    - 223.5.5.5
    - 119.29.29.29
  fallback:
    - 8.8.8.8
    - 1.1.1.1
    - tls://dns.google
  fallback-filter:
    geoip: true
    geoip-code: CN
    ipcidr:
      - 240.0.0.0/4

# Proxy group configuration
proxy-groups:
  # Main proxy selection
  - name: "Proxy"
    type: select
    proxies:
      - Auto
      - US-Auto
      - HK-Auto
      - JP-Auto
      - DIRECT

  # Auto-select (all nodes)
  - name: "Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300
    tolerance: 50

  # US nodes auto-select
  - name: "US-Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300
    tolerance: 50
    filter: "(?i)美国|US|United States"

  # HK nodes auto-select
  - name: "HK-Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300
    tolerance: 50
    filter: "(?i)香港|HK|Hong Kong"

  # JP nodes auto-select
  - name: "JP-Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300
    tolerance: 50
    filter: "(?i)日本|JP|Japan"

  # AI services dedicated
  - name: "AI-Services"
    type: select
    proxies:
      - US-Auto
      - Proxy

  # Streaming dedicated
  - name: "Streaming"
    type: select
    proxies:
      - HK-Auto
      - US-Auto
      - Proxy

  # Gaming dedicated
  - name: "Gaming"
    type: select
    proxies:
      - JP-Auto
      - HK-Auto
      - Proxy

  # Ad blocking
  - name: "AdBlock"
    type: select
    proxies:
      - REJECT
      - DIRECT

# Rule configuration
rules:
  # LAN direct connection
  - DOMAIN-SUFFIX,local,DIRECT
  - IP-CIDR,192.168.0.0/16,DIRECT
  - IP-CIDR,10.0.0.0/8,DIRECT
  - IP-CIDR,172.16.0.0/12,DIRECT
  - IP-CIDR,127.0.0.0/8,DIRECT

  # Ad blocking
  - DOMAIN-SUFFIX,ad.com,AdBlock
  - DOMAIN-KEYWORD,adservice,AdBlock
  - DOMAIN-KEYWORD,analytics,AdBlock

  # AI services
  - DOMAIN-SUFFIX,openai.com,AI-Services
  - DOMAIN-SUFFIX,anthropic.com,AI-Services
  - DOMAIN-SUFFIX,claude.ai,AI-Services
  - DOMAIN-KEYWORD,chatgpt,AI-Services

  # Streaming services
  - DOMAIN-SUFFIX,netflix.com,Streaming
  - DOMAIN-SUFFIX,youtube.com,Streaming
  - DOMAIN-SUFFIX,spotify.com,Streaming
  - DOMAIN-SUFFIX,twitch.tv,Streaming
  - DOMAIN-KEYWORD,youtube,Streaming

  # Gaming platforms
  - DOMAIN-SUFFIX,steam.com,Gaming
  - DOMAIN-SUFFIX,steampowered.com,Gaming
  - DOMAIN-SUFFIX,epicgames.com,Gaming
  - DOMAIN-KEYWORD,game,Gaming

  # Development tools
  - DOMAIN-SUFFIX,github.com,Proxy
  - DOMAIN-SUFFIX,githubusercontent.com,Proxy
  - DOMAIN-SUFFIX,gitlab.com,Proxy
  - DOMAIN-SUFFIX,stackoverflow.com,Proxy

  # Social media
  - DOMAIN-SUFFIX,twitter.com,Proxy
  - DOMAIN-SUFFIX,facebook.com,Proxy
  - DOMAIN-SUFFIX,instagram.com,Proxy
  - DOMAIN-SUFFIX,telegram.org,Proxy

  # China mainland websites direct
  - DOMAIN-SUFFIX,cn,DIRECT
  - DOMAIN-SUFFIX,baidu.com,DIRECT
  - DOMAIN-SUFFIX,taobao.com,DIRECT
  - DOMAIN-SUFFIX,qq.com,DIRECT
  - DOMAIN-SUFFIX,163.com,DIRECT
  - GEOIP,CN,DIRECT

  # Default rule
  - MATCH,Proxy
```

**Usage:**
1. Modify configuration according to actual needs
2. Adjust proxy groups and rules
3. Upload to admin interface

### Enterprise Template (Intranet Direct)

Suitable for enterprise environments that require intranet domain direct connection.

**File**: `clash-template-enterprise.yaml`

```yaml
# Enterprise Clash Template
# Suitable for: Enterprise intranet environment, requires intranet direct connection

# Proxy group configuration
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

# Rule configuration
rules:
  # LAN direct connection
  - DOMAIN-SUFFIX,local,DIRECT
  - IP-CIDR,192.168.0.0/16,DIRECT
  - IP-CIDR,10.0.0.0/8,DIRECT
  - IP-CIDR,172.16.0.0/12,DIRECT

  # Enterprise intranet domains direct (modify according to actual situation)
  - DOMAIN-SUFFIX,company.com,DIRECT
  - DOMAIN-SUFFIX,internal.local,DIRECT
  - DOMAIN-SUFFIX,corp.local,DIRECT
  - DOMAIN-KEYWORD,intranet,DIRECT

  # Enterprise intranet IP ranges direct (modify according to actual situation)
  - IP-CIDR,10.10.0.0/16,DIRECT
  - IP-CIDR,172.20.0.0/16,DIRECT

  # Office software direct
  - DOMAIN-SUFFIX,office.com,DIRECT
  - DOMAIN-SUFFIX,microsoft.com,DIRECT
  - DOMAIN-SUFFIX,microsoftonline.com,DIRECT

  # China mainland direct
  - GEOIP,CN,DIRECT

  # Others use proxy
  - MATCH,Proxy
```

---

## Usage Instructions

### How to Use These Examples

#### Method 1: Via Admin Interface

1. **Optimized IP Configuration:**
   - Visit `http://your-domain:8080/Admin/DefaultIPs`
   - Select "Global" or specific user
   - Copy and paste example content
   - Click save

2. **Clash Template Configuration:**
   - Visit `http://your-domain:8080/Admin/ClashTemplate`
   - Select "Global" or specific user
   - Copy and paste example content
   - Click save

#### Method 2: Via API

```bash
# Upload optimized IPs
curl -X POST http://localhost:8080/sub/user123 \
  -H "Content-Type: text/csv" \
  --data-binary @cloudflare-ip-full.csv

# Note: Clash templates need to be uploaded via admin interface
```

#### Method 3: Direct File Copy

```bash
# Copy to Docker container
docker cp cloudflare-ip-full.csv clashsubmanager:/app/data/cloudflare-ip.csv
docker cp clash-template-advanced.yaml clashsubmanager:/app/data/clash.yaml

# Restart container to apply configuration
docker restart clashsubmanager
```

### Customize Configuration

#### Modify Optimized IPs

1. Use CloudflareST speed test to get IPs suitable for you
2. Select 3-10 IPs with lowest latency
3. Organize according to example format
4. Upload to system

#### Modify Clash Template

1. Adjust proxy groups according to actual needs
2. Add or remove rules
3. Modify domains and IP ranges
4. Test if configuration is correct

### Configuration Validation

```bash
# 1. Get final generated configuration
curl http://localhost:8080/sub/user123 > final-config.yaml

# 2. Check configuration format
# Use online YAML validator

# 3. Test in Clash client
# Import configuration, check for errors
```

### Common Questions

**Q: Configuration not taking effect?**
- Check if YAML format is correct
- View container logs to troubleshoot errors
- Confirm file path is correct

**Q: How to set different configurations for different users?**
- Select specific user in admin interface
- Upload user-specific configuration
- User configuration will override global configuration

**Q: Can I use multiple templates simultaneously?**
- No, each user can only have one template
- But you can include multiple proxy groups and rules in the template

---

## 📚 Related Documentation

- [Advanced Usage Guide](../advanced-guide.md)
- [Deployment and Operations Guide](../deployment/deployment-guide.md)
- [Frequently Asked Questions](../FAQ.md)

---

## 💡 Tips

- Regularly update optimized IPs for best performance
- Adjust configuration according to actual needs
- Test configuration before applying to production
- Backup important configurations

---

**Last Updated**: 2026-02-20
