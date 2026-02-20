# ClashSubManager 常见问题解答（FAQ）

**🌐 语言**: [English](FAQ.md) | [中文](FAQ-CN.md)

---

## 📋 目录

- [部署相关](#部署相关)
- [使用相关](#使用相关)
- [配置相关](#配置相关)
- [性能相关](#性能相关)
- [故障排查](#故障排查)

---

## 部署相关

### Q1: Docker启动失败，提示端口被占用怎么办？

**错误信息：**
```
Error starting userland proxy: listen tcp4 0.0.0.0:8080: bind: address already in use
```

**解决方案：**

**方法1：更改映射端口**
```yaml
ports:
  - "8081:80"  # 将8080改为8081或其他未占用端口
```

**方法2：停止占用端口的进程**
```bash
# Linux/macOS
sudo lsof -i :8080
sudo kill -9 <PID>

# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F
```

### Q2: 容器启动后无法访问管理界面？

**可能原因：**
1. 防火墙阻止了端口访问
2. Docker网络配置问题
3. 端口映射配置错误

**排查步骤：**

```bash
# 1. 检查容器是否正常运行
docker ps | grep clashsubmanager

# 2. 检查容器日志
docker logs clashsubmanager

# 3. 检查端口映射
docker port clashsubmanager

# 4. 测试本地访问
curl http://localhost:8080/health

# 5. 检查防火墙（Linux）
sudo ufw status
sudo ufw allow 8080
```

### Q3: 环境变量配置错误导致无法登录？

**症状：**
- 输入正确的用户名密码仍然无法登录
- 提示"Invalid credentials"

**解决方案：**

```bash
# 1. 检查环境变量是否正确设置
docker exec clashsubmanager printenv | grep Admin

# 2. 重新设置环境变量
docker-compose down
# 编辑 docker-compose.yml，确保环境变量正确
docker-compose up -d

# 3. 检查Cookie密钥长度（至少32字符）
environment:
  - CookieSecretKey=your_secret_key_at_least_32_chars_long_here
```

### Q4: 数据目录权限问题导致无法写入文件？

**错误信息：**
```
Permission denied: '/app/data/cloudflare-ip.csv'
```

**解决方案：**

```bash
# 1. 检查数据目录权限
ls -la ./data

# 2. 修改目录权限
sudo chown -R 1000:1000 ./data
sudo chmod -R 755 ./data

# 3. 或在docker-compose.yml中指定用户
services:
  clashsubmanager:
    user: "1000:1000"
```

### Q5: 如何升级到最新版本？

**步骤：**

```bash
# 1. 备份数据
cp -r ./data ./data.backup

# 2. 停止容器
docker-compose down

# 3. 拉取最新镜像
docker pull clashsubmanager:latest

# 4. 启动新版本
docker-compose up -d

# 5. 检查日志
docker logs -f clashsubmanager
```

---

## 使用相关

### Q6: 订阅地址无法访问，返回404？

**可能原因：**
1. 用户ID不正确
2. 上游订阅服务不可用
3. 原始订阅地址配置错误

**排查步骤：**

```bash
# 1. 检查订阅地址格式
# 正确格式：http://your-domain:8080/sub/user123

# 2. 测试上游订阅是否可用
curl -A "clash" https://your-airport.com/sub/user123

# 3. 查看容器日志
docker logs clashsubmanager | grep "user123"

# 4. 验证环境变量中的上游订阅地址
docker exec clashsubmanager printenv | grep SUBSCRIPTION_URL
```

### Q7: 配置不生效，节点没有被替换为优选IP？

**可能原因：**
1. 优选IP文件不存在或格式错误
2. 节点的server字段不是域名
3. 配置优先级问题

**检查清单：**

```bash
# 1. 检查优选IP文件是否存在
docker exec clashsubmanager ls -la /app/data/cloudflare-ip.csv

# 2. 检查文件内容格式
docker exec clashsubmanager cat /app/data/cloudflare-ip.csv

# 3. 确认节点server字段是域名而非IP
# 只有域名节点会被扩展，IP节点会保持原样

# 4. 查看最终生成的配置
curl http://localhost:8080/sub/user123 > final-config.yaml
cat final-config.yaml | grep -A 5 "proxies:"
```

### Q8: 优选IP功能不工作，所有节点都是原始节点？

**原因分析：**

优选IP功能只对**域名节点**生效，对**IP节点**不生效。

**示例：**

```yaml
# 会被扩展的节点（server是域名）
- name: "US-Node"
  server: cdn.example.com  # 域名
  
# 不会被扩展的节点（server是IP）
- name: "HK-Node"
  server: 104.28.1.1  # IP地址
```

**解决方案：**

如果你的订阅中所有节点都是IP地址，优选IP功能将不会生效。这是设计行为，因为IP节点通常不需要优选。

### Q9: 节点数量异常，比预期多很多？

**原因：**

这是正常现象。优选IP功能会将每个域名节点扩展为多个节点。

**计算公式：**
```
最终节点数 = 原始节点数 + (域名节点数 × 优选IP数量)
```

**示例：**
- 原始订阅：10个节点（5个域名节点，5个IP节点）
- 优选IP：3个
- 最终节点数：10 + (5 × 3) = 25个节点

**优化建议：**
- 减少优选IP数量（推荐3-5个）
- 使用代理组管理大量节点
- 只为特定用户启用优选IP

### Q10: 如何禁用优选IP功能？

**方法1：删除优选IP文件**

通过管理界面删除优选IP配置，或：

```bash
docker exec clashsubmanager rm /app/data/cloudflare-ip.csv
```

**方法2：使用空的优选IP文件**

在管理界面上传空内容，或创建空文件：

```bash
docker exec clashsubmanager sh -c "echo '' > /app/data/cloudflare-ip.csv"
```

---

## 配置相关

### Q11: 如何获取Cloudflare优选IP？

**使用CloudflareST工具：**

```bash
# 1. 下载CloudflareST
wget https://github.com/XIU2/CloudflareSpeedTest/releases/download/v2.2.5/CloudflareST_linux_amd64.tar.gz
tar -zxvf CloudflareST_linux_amd64.tar.gz

# 2. 执行测速
./CloudflareST -n 200 -t 10 -o result.csv

# 3. 查看结果
cat result.csv

# 4. 上传到ClashSubManager
# 方法1：通过管理界面上传
# 方法2：通过API上传
curl -X POST http://localhost:8080/sub/user123 \
  -H "Content-Type: text/csv" \
  --data-binary @result.csv
```

**推荐参数：**
- `-n 200`：测试200个IP
- `-t 10`：每个IP测试10次
- `-tl 200`：平均延迟上限200ms
- `-sl 5`：下载速度下限5MB/s

### Q12: 如何编写Clash模板？

**基础模板示例：**

```yaml
# /app/data/clash.yaml

# 代理组配置
proxy-groups:
  - name: "Auto"
    type: url-test
    url: 'http://www.gstatic.com/generate_204'
    interval: 300

# 规则配置
rules:
  - DOMAIN-SUFFIX,cn,DIRECT
  - GEOIP,CN,DIRECT
  - MATCH,Auto
```

**高级模板示例：**

```yaml
# DNS配置
dns:
  enable: true
  nameserver:
    - 223.5.5.5
    - 119.29.29.29
  fallback:
    - 8.8.8.8
    - 1.1.1.1

# 代理组
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

# 规则
rules:
  - DOMAIN-SUFFIX,openai.com,Proxy
  - DOMAIN-SUFFIX,github.com,Proxy
  - GEOIP,CN,DIRECT
  - MATCH,Proxy
```

**注意事项：**
- 模板中的配置会与原始订阅合并
- 代理组中引用的节点名称必须存在
- 规则按从上到下的顺序匹配

### Q13: 配置优先级是怎样的？

**优先级顺序：**

```
用户专属配置 > 全局默认配置 > 原始订阅
```

**文件路径：**
- 全局配置：`/app/data/clash.yaml`
- 用户配置：`/app/data/user123/clash.yaml`

**合并规则：**

1. **数组字段**（proxies, rules）：追加合并
2. **对象字段**（dns, tun）：覆盖合并
3. **简单字段**（port, mode）：直接覆盖

详见[高级使用指南 - 配置优先级](advanced-guide-cn.md#14-配置优先级深入理解)

### Q14: 用户专属配置不生效？

**排查步骤：**

```bash
# 1. 检查用户目录是否存在
docker exec clashsubmanager ls -la /app/data/user123/

# 2. 检查配置文件是否存在
docker exec clashsubmanager cat /app/data/user123/clash.yaml

# 3. 验证YAML格式
# 使用在线YAML验证器检查格式

# 4. 查看日志
docker logs clashsubmanager | grep "user123"

# 5. 获取最终配置验证
curl http://localhost:8080/sub/user123 > final.yaml
```

### Q15: 如何为不同用户设置不同的优选IP？

**步骤：**

1. **通过管理界面：**
   - 访问 `/Admin/DefaultIPs`
   - 在用户选择器中选择特定用户
   - 上传或粘贴该用户的优选IP列表

2. **通过API：**
```bash
# 为user123设置专属优选IP
curl -X POST http://localhost:8080/sub/user123 \
  -H "Content-Type: text/csv" \
  --data-binary @user123-ips.csv

# 为user456设置专属优选IP
curl -X POST http://localhost:8080/sub/user456 \
  -H "Content-Type: text/csv" \
  --data-binary @user456-ips.csv
```

3. **文件结构：**
```
/app/data/
├── cloudflare-ip.csv          # 全局优选IP
├── user123/
│   └── cloudflare-ip.csv      # user123专属
└── user456/
    └── cloudflare-ip.csv      # user456专属
```

---

## 性能相关

### Q16: 订阅请求响应速度慢？

**可能原因：**
1. 上游订阅服务响应慢
2. 节点数量过多
3. 配置文件过大
4. 服务器资源不足

**优化方案：**

```bash
# 1. 测试上游订阅响应时间
time curl -A "clash" https://your-airport.com/sub/user123

# 2. 减少优选IP数量
# 从10个减少到3-5个

# 3. 限制Docker资源
services:
  clashsubmanager:
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M

# 4. 启用日志级别为Warning
environment:
  - Logging__LogLevel__Default=Warning
```

### Q17: 内存占用过高？

**正常内存占用：**
- 空闲状态：50-100MB
- 处理请求：100-200MB
- 大量节点：200-500MB

**优化建议：**

```yaml
# 1. 限制Docker内存
deploy:
  resources:
    limits:
      memory: 512M

# 2. 减少日志输出
environment:
  - Logging__LogLevel__Default=Warning

# 3. 定期清理日志
docker exec clashsubmanager sh -c "echo '' > /app/logs/app.log"
```

### Q18: 如何提高并发处理能力？

**方案1：增加资源限制**

```yaml
deploy:
  resources:
    limits:
      cpus: '2.0'
      memory: 1G
```

**方案2：使用负载均衡**

部署多个实例，使用Nginx进行负载均衡：

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

### Q19: 订阅更新失败，提示超时？

**原因：**
- 上游订阅服务响应慢或不可用
- 网络连接问题
- 防火墙阻止

**解决方案：**

```bash
# 1. 测试网络连接
docker exec clashsubmanager curl -v https://your-airport.com

# 2. 检查DNS解析
docker exec clashsubmanager nslookup your-airport.com

# 3. 增加超时时间（如果支持）
# 在环境变量中配置
environment:
  - HTTP_TIMEOUT=30

# 4. 使用代理访问上游订阅（如果需要）
environment:
  - HTTP_PROXY=http://proxy-server:port
```

---

## 故障排查

### Q20: 如何查看详细日志？

**方法1：Docker日志**

```bash
# 实时查看日志
docker logs -f clashsubmanager

# 查看最近100行
docker logs --tail 100 clashsubmanager

# 查看特定时间段
docker logs --since "2024-01-01T00:00:00" clashsubmanager

# 只看错误日志
docker logs clashsubmanager 2>&1 | grep ERROR
```

**方法2：日志文件**

```bash
# 查看应用日志
docker exec clashsubmanager cat /app/logs/app.log

# 实时监控
docker exec clashsubmanager tail -f /app/logs/app.log
```

**方法3：启用详细日志**

```yaml
environment:
  - Logging__LogLevel__Default=Debug
  - Logging__LogLevel__Microsoft=Information
```

### Q21: 如何验证配置是否正确？

**步骤：**

```bash
# 1. 获取最终生成的配置
curl http://localhost:8080/sub/user123 > final-config.yaml

# 2. 验证YAML格式
# 使用在线工具：https://www.yamllint.com/

# 3. 检查节点数量
grep "^  - name:" final-config.yaml | wc -l

# 4. 检查规则
grep -A 20 "^rules:" final-config.yaml

# 5. 检查代理组
grep -A 10 "^proxy-groups:" final-config.yaml

# 6. 在Clash客户端中测试
# 将配置导入Clash，查看是否有错误提示
```

### Q22: 健康检查失败？

**检查命令：**

```bash
# 1. 访问健康检查端点
curl http://localhost:8080/health

# 预期响应
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456"
}

# 2. 如果失败，检查容器状态
docker ps -a | grep clashsubmanager

# 3. 检查端口是否正常监听
docker exec clashsubmanager netstat -tlnp | grep 80

# 4. 重启容器
docker restart clashsubmanager
```

### Q23: 如何重置所有配置？

**警告：此操作会删除所有自定义配置！**

```bash
# 1. 停止容器
docker-compose down

# 2. 备份数据（可选）
cp -r ./data ./data.backup

# 3. 删除数据目录
rm -rf ./data/*

# 4. 重新启动
docker-compose up -d

# 5. 重新配置管理员账号和其他设置
```

### Q24: 如何导出和导入配置？

**导出配置：**

```bash
# 导出所有配置
docker exec clashsubmanager tar -czf /tmp/config-backup.tar.gz /app/data
docker cp clashsubmanager:/tmp/config-backup.tar.gz ./config-backup.tar.gz

# 或直接复制数据目录
cp -r ./data ./data-backup-$(date +%Y%m%d)
```

**导入配置：**

```bash
# 停止容器
docker-compose down

# 解压配置
tar -xzf config-backup.tar.gz -C ./data

# 启动容器
docker-compose up -d
```

### Q25: 遇到其他问题怎么办？

**获取帮助的途径：**

1. **查看文档：**
   - [部署运维指南](deployment/deployment-guide-cn.md)
   - [高级使用指南](advanced-guide-cn.md)
   - [环境变量配置](deployment/env-config-cn.md)

2. **检查日志：**
   ```bash
   docker logs clashsubmanager
   ```

3. **提交Issue：**
   - 访问项目GitHub仓库
   - 提供详细的错误信息和日志
   - 说明你的环境和配置

4. **社区讨论：**
   - 在GitHub Discussions中提问
   - 搜索已有的问题和解答

---

## 💡 提示

- 定期备份配置文件
- 保持Docker镜像更新
- 监控系统资源使用
- 查看日志排查问题
- 测试配置后再应用到生产环境

---

## 📚 相关文档

- [部署运维指南](deployment/deployment-guide-cn.md)
- [高级使用指南](advanced-guide-cn.md)
- [环境变量配置](deployment/env-config-cn.md)
- [项目文档导航](README-CN.md)