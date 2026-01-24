# 节点命名模板系统设计

**🌐 语言**: [English](node-naming-template-design.md) | [中文](node-naming-template-design-cn.md)

## 概述

本文档概述了灵活的节点命名模板系统设计，允许用户通过配置文件、环境变量和命令行参数自定义代理节点名称。

## 当前实现分析

### 现有命名逻辑
```csharp
var useIpFormat = GetValue<bool>("UseIpInNodeName", false);
var newName = useIpFormat 
    ? $"{originalName}-{ip.IPAddress}"
    : $"{originalName}-Node-{index + 1}";
```

### 局限性
- 仅支持两种固定命名模式
- 不支持自定义变量
- 仅限于IP地址或索引包含
- 不支持多级配置

## 设计方案

### 1. 模板系统架构

#### 核心组件
- **INodeNamingTemplateService**: 模板处理接口
- **NodeNamingTemplateService**: 带变量替换引擎的实现
- **TemplateVariableProvider**: 提供可用变量进行替换
- **配置集成**: 支持多种配置源

#### 配置优先级
1. 命令行参数（最高优先级）
2. 环境变量
3. 用户配置文件
4. 默认配置（最低优先级）

### 2. 模板变量系统

#### 支持的变量
| 变量 | 描述 | 示例 | 多级支持 |
|------|------|------|----------|
| `{name}` | 原始代理名称 | `HK-Server` | `proxy.name` |
| `{index}` | 当前节点索引 | `1`, `2`, `3` | `node.index` |
| `{network}` | 网络类型 | `ws`, `grpc`, `h2` | `proxy.network` |
| `{port}` | 端口号 | `443`, `8080` | `proxy.port` |
| `{server}` | 新IP地址 | `104.16.1.1` | `proxy.server` |
| `{servername}` | 原始域名 | `example.com` | `proxy.servername` |
| `{type}` | 服务类型 | `vless`, `vmess` | `proxy.type` |
| `{uuid}` | 原始UUID | `12345678-1234-1234-1234-123456789abc` | `proxy.uuid` |

#### 多级配置示例
```bash
# 访问嵌套属性
{proxy.name}           # 原始代理名称
{proxy.type}           # 服务类型
{proxy.network}        # 网络类型
{node.index}           # 节点索引
{proxy.server}         # 服务器地址
```

### 3. 模板模式

#### 基础模板
```
# 简单命名
"自定义名称-{index}"
"Node-{index}-{server}"
"{name}-CF-{index}"

# 协议特定
"VLESS-{index}-{server}"
"VMess-{name}-{port}"

# 基于位置
"HK-{index}-VLESS"
"US-{name}-Node"
```

#### 高级模板
```
# 多变量组合
"{name}-{type}-{index}-{server}"
"Custom-{proxy.type}-Node-{node.index}"

# 条件命名（未来增强）
"{name}{#if network == 'vless'}-VLESS{#else}-{type}{/if}-{index}"
```

### 4. 配置集成

#### 环境变量
```bash
# 模板配置
NODE_NAMING_TEMPLATE="自定义名称-节点-{index}"
NODE_NAMING_TEMPLATE="{name}-CF-{index}-{server}"

# 向后兼容
USE_IP_IN_NODE_NAME=true  # 映射到 "{name}-{server}"
```

#### 配置文件
```json
{
  "NodeNamingTemplate": "自定义名称-节点-{index}",
  "NodeNamingVariables": {
    "CustomPrefix": "MyProxy",
    "Location": "HK"
  }
}
```

#### 命令行参数
```bash
./ClashSubManager --NodeNamingTemplate "自定义-{index}" --CustomPrefix "Test"
```

### 5. 实现细节

#### 接口设计
```csharp
public interface INodeNamingTemplateService
{
    /// <summary>
    /// 处理模板并替换变量
    /// </summary>
    string ProcessTemplate(string template, NodeNamingContext context);
    
    /// <summary>
    /// 获取可用的模板变量
    /// </summary>
    Dictionary<string, object> GetVariables(NodeNamingContext context);
    
    /// <summary>
    /// 验证模板语法
    /// </summary>
    bool ValidateTemplate(string template, out string errorMessage);
}
```

#### 上下文模型
```csharp
public class NodeNamingContext
{
    public string OriginalName { get; set; }
    public int Index { get; set; }
    public string Network { get; set; }
    public int Port { get; set; }
    public string Server { get; set; }
    public string ServerName { get; set; }
    public string Type { get; set; }
    public string Uuid { get; set; }
    public Dictionary<string, object> CustomProperties { get; set; }
}
```

#### 变量替换引擎
```csharp
public class TemplateVariableProvider
{
    public Dictionary<string, object> ExtractVariables(YamlMappingNode proxyNode, int index, string newServer)
    {
        var variables = new Dictionary<string, object>();
        
        // 提取基础变量
        variables["name"] = ExtractStringValue(proxyNode, "name");
        variables["index"] = index + 1;
        variables["server"] = newServer;
        
        // 提取协议特定变量
        variables["network"] = ExtractStringValue(proxyNode, "type")?.ToLower();
        variables["type"] = ExtractStringValue(proxyNode, "type")?.ToUpper();
        variables["port"] = ExtractIntValue(proxyNode, "port");
        variables["servername"] = ExtractStringValue(proxyNode, "server");
        variables["uuid"] = ExtractStringValue(proxyNode, "uuid");
        
        // 创建多级变量
        CreateNestedVariables(variables);
        
        return variables;
    }
    
    private void CreateNestedVariables(Dictionary<string, object> variables)
    {
        var nested = new Dictionary<string, object>();
        
        // 创建 proxy.* 变量
        var proxyVars = new Dictionary<string, object>();
        foreach (var kvp in variables)
        {
            if (!kvp.Key.StartsWith("node."))
            {
                proxyVars[kvp.Key] = kvp.Value;
            }
        }
        nested["proxy"] = proxyVars;
        
        // 创建 node.* 变量
        var nodeVars = new Dictionary<string, object>();
        nodeVars["index"] = variables["index"];
        nested["node"] = nodeVars;
        
        // 合并嵌套变量
        foreach (var kvp in nested)
        {
            foreach (var innerKvp in (Dictionary<string, object>)kvp.Value)
            {
                variables[$"{kvp.Key}.{innerKvp.Key}"] = innerKvp.Value;
            }
        }
    }
}
```

### 6. 向后兼容性

#### 迁移策略
```csharp
// 从旧配置自动迁移
private string GetNamingTemplate()
{
    // 检查新的模板配置
    var template = GetValue<string>("NodeNamingTemplate");
    if (!string.IsNullOrEmpty(template))
        return template;
    
    // 从旧的 UseIpInNodeName 设置迁移
    var useIpFormat = GetValue<bool>("UseIpInNodeName", false);
    return useIpFormat 
        ? "{name}-{server}" 
        : "{name}-Node-{index}";
}
```

#### 配置映射
| 旧设置 | 新模板 | 描述 |
|--------|--------|------|
| `UseIpInNodeName=true` | `{name}-{server}` | 包含IP地址 |
| `UseIpInNodeName=false` | `{name}-Node-{index}` | 使用节点索引 |

### 7. 验证和错误处理

#### 模板验证规则
- 有效变量名：`[a-zA-Z][a-zA-Z0-9_]*`
- 变量格式：`{variable}` 或 `{category.variable}`
- 无嵌套变量：`{{variable}}`（无效）
- 需要平衡的大括号

#### 错误处理
```csharp
public ValidationResult ValidateTemplate(string template)
{
    var result = new ValidationResult();
    
    // 检查平衡的大括号
    var openBraces = template.Count(c => c == '{');
    var closeBraces = template.Count(c => c == '}');
    
    if (openBraces != closeBraces)
    {
        result.AddError("模板中的大括号不平衡");
    }
    
    // 验证变量名
    var matches = Regex.Matches(template, @"\{([^}]+)\}");
    foreach (Match match in matches)
    {
        var variableName = match.Groups[1].Value;
        if (!IsValidVariableName(variableName))
        {
            result.AddError($"无效的变量名: {variableName}");
        }
    }
    
    return result;
}
```

### 8. 性能考虑

#### 优化策略
- **模板缓存**: 缓存编译后的模板以供重用
- **变量预计算**: 在处理前预提取变量
- **正则表达式优化**: 使用编译的正则表达式模式
- **延迟求值**: 仅在需要时计算变量

#### 内存管理
- 重用模板上下文对象
- 缓存变量提取结果
- 在处理过程中最小化字符串分配

### 9. 测试策略

#### 单元测试覆盖
- 模板处理准确性
- 变量提取正确性
- 多级变量支持
- 错误处理验证
- 向后兼容性

#### 集成测试场景
- 端到端模板处理
- 配置源优先级
- 真实代理配置解析
- 性能基准测试

### 10. 未来增强

#### 条件模板
```
{#if network == 'vless'}VLESS-{index}{#else}{type}-{index}{/if}
```

#### 自定义函数
```
{name}-{upper(server)}-{pad(index, 3)}
```

#### 模板继承
```json
{
  "BaseTemplate": "{name}-{index}",
  "ProtocolTemplates": {
    "vless": "VLESS-{index}-{server}",
    "vmess": "VMess-{name}-{port}"
  }
}
```

## 实施计划

### 第一阶段：核心模板系统
1. 实现 `INodeNamingTemplateService` 接口
2. 创建变量替换引擎
3. 添加基础模板验证
4. 与现有配置系统集成

### 第二阶段：多级变量
1. 实现嵌套变量支持
2. 添加代理配置解析
3. 创建变量提取逻辑
4. 添加全面的变量映射

### 第三阶段：配置集成
1. 添加环境变量支持
2. 实现命令行参数解析
3. 添加配置文件支持
4. 创建配置迁移逻辑

### 第四阶段：测试和文档
1. 全面的单元测试套件
2. 集成测试场景
3. 性能基准测试
4. 用户文档和示例

## 结论

节点命名模板系统为自定义代理节点名称提供了一个灵活且可扩展的解决方案。它支持多种配置源、全面的变量替换，并在保持向后兼容性的同时启用高级命名模式。

模块化设计允许未来增强，并确保系统能够适应不断发展的用户需求。
