# 配置示例集

**🌐 语言**: [English](README.md) | [中文](README-CN.md)

---

## 📋 目录

- [优选IP配置示例](#优选ip配置示例)
- [Clash模板示例](#clash模板示例)
- [使用说明](#使用说明)

---

## 优选IP配置示例

### 简化格式（仅IP列表）

适用于只需要IP地址的场景，系统会自动使用这些IP替换域名节点。

**文件**: `cloudflare-ip-simple.csv`

```csv
104.29.125.182
104.26.0.188
104.20.20.191
104.21.21.192
104.18.18.193
```

**使用方法：**
1. 访问管理界面 `/Admin/DefaultIPs`
2. 选择"全局"或特定用户
3. 粘贴上述内容并保存

### 完整格式（CloudflareST输出）

包含延迟和下载速度信息，提供更详细的性能数据。

**文件**: `cloudflare-ip-full.csv`

```csv
IP Address,Sent,Received,Loss Rate,Average Latency,Download Speed (MB/s)
104.29.125.182,4,4,0.00%,152.45 ms,8.52
104.26.0.188,4,4,0.00%,158.10 ms,8.31
104.20.20.191,4,4,0.00%,161.38 ms,8.15
104.21.21.192,4,4,0.00%,165.22 ms,7.98
104.18.18.193,4,4,0.00%,168.55 ms,7.82
```

**使用方法：**
1. 使用CloudflareST工具测速：
   ```bash
   ./CloudflareST -n 200 -t 10 -o result.csv
   ```
2. 将生成的`result.csv`上传到管理界面
3. 或通过API上传：
   ```bash
   curl -X POST http://localhost:8080/sub/user123 \
     -H "Content-Type: text/csv" \
     --data-binary @result.csv
   ```

### 格式说明

**简化格式：**
- 每行一个IP地址
- 不需要表头
- 支持IPv4和IPv6

**完整格式：**
- 第一行为表头（可选）
- 必须包含"IP Address"列
- 其他列为可选的性能数据
- 系统会自动识别并解析

---

## Clash模板示例

### 基础模板（最小配置）

适用于简单场景，只添加基本的规则和代理组。

**文件**: `clash-template-basic.yaml`

```yaml
# 基础Clash模板
# 适用于：个人使用、简单配置需求

# 代理组配置
proxy-groups:
  # 自动选择延迟最低的节点
  - name: "Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300
    tolerance: 50

  # 手动选择代理
  - name: "Proxy"
    type: select
    proxies:
      - Auto
      - DIRECT

# 规则配置
rules:
  # 局域网直连
  - DOMAIN-SUFFIX,local,DIRECT
  - IP-CIDR,192.168.0.0/16,DIRECT
  - IP-CIDR,10.0.0.0/8,DIRECT
  - IP-CIDR,172.16.0.0/12,DIRECT
  
  # 中国大陆直连
  - GEOIP,CN,DIRECT
  
  # 其他走代理
  - MATCH,Proxy
```

**使用方法：**
1. 访问管理界面 `/Admin/ClashTemplate`
2. 选择"全局"或特定用户
3. 粘贴上述内容并保存

### 高级模板（完整配置）

适用于复杂场景，包含DNS、多个代理组、详细规则等。

**文件**: `clash-template-advanced.yaml`

```yaml
# 高级Clash模板
# 适用于：企业使用、复杂配置需求、多场景分流

# 端口配置
port: 7890
socks-port: 7891
allow-lan: true
mode: rule
log-level: info

# DNS配置
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

# 代理组配置
proxy-groups:
  # 主代理选择
  - name: "Proxy"
    type: select
    proxies:
      - Auto
      - US-Auto
      - HK-Auto
      - JP-Auto
      - DIRECT

  # 自动选择（全部节点）
  - name: "Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300
    tolerance: 50

  # 美国节点自动选择
  - name: "US-Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300
    tolerance: 50
    filter: "(?i)美国|US|United States"

  # 香港节点自动选择
  - name: "HK-Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300
    tolerance: 50
    filter: "(?i)香港|HK|Hong Kong"

  # 日本节点自动选择
  - name: "JP-Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300
    tolerance: 50
    filter: "(?i)日本|JP|Japan"

  # AI服务专用
  - name: "AI-Services"
    type: select
    proxies:
      - US-Auto
      - Proxy

  # 流媒体专用
  - name: "Streaming"
    type: select
    proxies:
      - HK-Auto
      - US-Auto
      - Proxy

  # 游戏专用
  - name: "Gaming"
    type: select
    proxies:
      - JP-Auto
      - HK-Auto
      - Proxy

  # 广告拦截
  - name: "AdBlock"
    type: select
    proxies:
      - REJECT
      - DIRECT

# 规则配置
rules:
  # 局域网直连
  - DOMAIN-SUFFIX,local,DIRECT
  - IP-CIDR,192.168.0.0/16,DIRECT
  - IP-CIDR,10.0.0.0/8,DIRECT
  - IP-CIDR,172.16.0.0/12,DIRECT
  - IP-CIDR,127.0.0.0/8,DIRECT

  # 广告拦截
  - DOMAIN-SUFFIX,ad.com,AdBlock
  - DOMAIN-KEYWORD,adservice,AdBlock
  - DOMAIN-KEYWORD,analytics,AdBlock

  # AI服务
  - DOMAIN-SUFFIX,openai.com,AI-Services
  - DOMAIN-SUFFIX,anthropic.com,AI-Services
  - DOMAIN-SUFFIX,claude.ai,AI-Services
  - DOMAIN-KEYWORD,chatgpt,AI-Services

  # 流媒体服务
  - DOMAIN-SUFFIX,netflix.com,Streaming
  - DOMAIN-SUFFIX,youtube.com,Streaming
  - DOMAIN-SUFFIX,spotify.com,Streaming
  - DOMAIN-SUFFIX,twitch.tv,Streaming
  - DOMAIN-KEYWORD,youtube,Streaming

  # 游戏平台
  - DOMAIN-SUFFIX,steam.com,Gaming
  - DOMAIN-SUFFIX,steampowered.com,Gaming
  - DOMAIN-SUFFIX,epicgames.com,Gaming
  - DOMAIN-KEYWORD,game,Gaming

  # 开发工具
  - DOMAIN-SUFFIX,github.com,Proxy
  - DOMAIN-SUFFIX,githubusercontent.com,Proxy
  - DOMAIN-SUFFIX,gitlab.com,Proxy
  - DOMAIN-SUFFIX,stackoverflow.com,Proxy

  # 社交媒体
  - DOMAIN-SUFFIX,twitter.com,Proxy
  - DOMAIN-SUFFIX,facebook.com,Proxy
  - DOMAIN-SUFFIX,instagram.com,Proxy
  - DOMAIN-SUFFIX,telegram.org,Proxy

  # 中国大陆网站直连
  - DOMAIN-SUFFIX,cn,DIRECT
  - DOMAIN-SUFFIX,baidu.com,DIRECT
  - DOMAIN-SUFFIX,taobao.com,DIRECT
  - DOMAIN-SUFFIX,qq.com,DIRECT
  - DOMAIN-SUFFIX,163.com,DIRECT
  - GEOIP,CN,DIRECT

  # 默认规则
  - MATCH,Proxy
```

**使用方法：**
1. 根据实际需求修改配置
2. 调整代理组和规则
3. 上传到管理界面

### 企业模板（内网直连）

适用于企业环境，需要内网域名直连的场景。

**文件**: `clash-template-enterprise.yaml`

```yaml
# 企业Clash模板
# 适用于：企业内网环境、需要内网直连

# 代理组配置
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

# 规则配置
rules:
  # 局域网直连
  - DOMAIN-SUFFIX,local,DIRECT
  - IP-CIDR,192.168.0.0/16,DIRECT
  - IP-CIDR,10.0.0.0/8,DIRECT
  - IP-CIDR,172.16.0.0/12,DIRECT

  # 企业内网域名直连（根据实际情况修改）
  - DOMAIN-SUFFIX,company.com,DIRECT
  - DOMAIN-SUFFIX,internal.local,DIRECT
  - DOMAIN-SUFFIX,corp.local,DIRECT
  - DOMAIN-KEYWORD,intranet,DIRECT

  # 企业内网IP段直连（根据实际情况修改）
  - IP-CIDR,10.10.0.0/16,DIRECT
  - IP-CIDR,172.20.0.0/16,DIRECT

  # 办公软件直连
  - DOMAIN-SUFFIX,office.com,DIRECT
  - DOMAIN-SUFFIX,microsoft.com,DIRECT
  - DOMAIN-SUFFIX,microsoftonline.com,DIRECT

  # 中国大陆直连
  - GEOIP,CN,DIRECT

  # 其他走代理
  - MATCH,Proxy
```

---

## 使用说明

### 如何使用这些示例

#### 方法1：通过管理界面

1. **优选IP配置：**
   - 访问 `http://your-domain:8080/Admin/DefaultIPs`
   - 选择"全局"或特定用户
   - 复制示例内容并粘贴
   - 点击保存

2. **Clash模板配置：**
   - 访问 `http://your-domain:8080/Admin/ClashTemplate`
   - 选择"全局"或特定用户
   - 复制示例内容并粘贴
   - 点击保存

#### 方法2：通过API

```bash
# 上传优选IP
curl -X POST http://localhost:8080/sub/user123 \
  -H "Content-Type: text/csv" \
  --data-binary @cloudflare-ip-full.csv

# 注意：Clash模板需要通过管理界面上传
```

#### 方法3：直接复制文件

```bash
# 复制到Docker容器
docker cp cloudflare-ip-full.csv clashsubmanager:/app/data/cloudflare-ip.csv
docker cp clash-template-advanced.yaml clashsubmanager:/app/data/clash.yaml

# 重启容器使配置生效
docker restart clashsubmanager
```

### 自定义配置

#### 修改优选IP

1. 使用CloudflareST测速获取适合你的IP
2. 选择延迟最低的3-10个IP
3. 按照示例格式整理
4. 上传到系统

#### 修改Clash模板

1. 根据实际需求调整代理组
2. 添加或删除规则
3. 修改域名和IP段
4. 测试配置是否正确

### 配置验证

```bash
# 1. 获取最终生成的配置
curl http://localhost:8080/sub/user123 > final-config.yaml

# 2. 检查配置格式
# 使用在线YAML验证器

# 3. 在Clash客户端中测试
# 导入配置，查看是否有错误
```

### 常见问题

**Q: 配置不生效怎么办？**
- 检查YAML格式是否正确
- 查看容器日志排查错误
- 确认文件路径正确

**Q: 如何为不同用户设置不同配置？**
- 在管理界面选择特定用户
- 上传该用户的专属配置
- 用户配置会覆盖全局配置

**Q: 可以同时使用多个模板吗？**
- 不可以，每个用户只能有一个模板
- 但可以在模板中包含多个代理组和规则

---

## 📚 相关文档

- [高级使用指南](../advanced-guide-cn.md)
- [部署运维指南](../deployment/deployment-guide-cn.md)
- [常见问题解答](../FAQ-CN.md)

---

## 💡 提示

- 定期更新优选IP以获得最佳性能
- 根据实际需求调整配置
- 测试配置后再应用到生产环境
- 备份重要配置

---

**最后更新**: 2026-02-20
