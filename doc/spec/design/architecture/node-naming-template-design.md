# Node Naming Template System Design

**🌐 Language**: [English](node-naming-template-design.md) | [中文](node-naming-template-design-cn.md)

## Overview

This document outlines the design for a flexible node naming template system that allows users to customize proxy node names through configuration files, environment variables, and command-line arguments.

## Current Implementation Analysis

### Existing Naming Logic
```csharp
var useIpFormat = GetValue<bool>("UseIpInNodeName", false);
var newName = useIpFormat 
    ? $"{originalName}-{ip.IPAddress}"
    : $"{originalName}-Node-{index + 1}";
```

### Limitations
- Only two fixed naming patterns
- No support for custom variables
- Limited to IP address or index inclusion
- No multi-level configuration support

## Proposed Design

### 1. Template System Architecture

#### Core Components
- **INodeNamingTemplateService**: Interface for template processing
- **NodeNamingTemplateService**: Implementation with variable replacement engine
- **TemplateVariableProvider**: Provides available variables for replacement
- **Configuration Integration**: Support for multiple configuration sources

#### Configuration Hierarchy
1. Command-line arguments (highest priority)
2. Environment variables
3. User configuration files
4. Default configuration (lowest priority)

### 2. Template Variable System

#### Supported Variables
| Variable | Description | Example | Multi-level Support |
|----------|-------------|---------|-------------------|
| `{name}` | Original proxy name | `HK-Server` | `proxy.name` |
| `{index}` | Current node index | `1`, `2`, `3` | `node.index` |
| `{network}` | Network type | `ws`, `grpc`, `h2` | `proxy.network` |
| `{port}` | Port number | `443`, `8080` | `proxy.port` |
| `{server}` | New IP address | `104.16.1.1` | `proxy.server` |
| `{servername}` | Original domain name | `example.com` | `proxy.servername` |
| `{type}` | Service type | `vless`, `vmess` | `proxy.type` |
| `{uuid}` | Original UUID | `12345678-1234-1234-1234-123456789abc` | `proxy.uuid` |

#### Multi-level Configuration Examples
```bash
# Access nested properties
{proxy.name}           # Original proxy name
{proxy.type}           # Service type
{proxy.network}        # Network type
{node.index}           # Node index
{proxy.server}         # Server address
```

### 3. Template Patterns

#### Basic Templates
```
# Simple naming
"自定义名称-{index}"
"Node-{index}-{server}"
"{name}-CF-{index}"

# Protocol-specific
"VLESS-{index}-{server}"
"VMess-{name}-{port}"

# Location-based
"HK-{index}-VLESS"
"US-{name}-Node"
```

#### Advanced Templates
```
# Multi-variable combinations
"{name}-{type}-{index}-{server}"
"Custom-{proxy.type}-Node-{node.index}"

# Conditional naming (future enhancement)
"{name}{#if network == 'vless'}-VLESS{#else}-{type}{/if}-{index}"
```

### 4. Configuration Integration

#### Environment Variables
```bash
# Template configuration
NODE_NAMING_TEMPLATE="自定义名称-节点-{index}"
NODE_NAMING_TEMPLATE="{name}-CF-{index}-{server}"

# Backward compatibility
USE_IP_IN_NODE_NAME=true  # Maps to "{name}-{server}"
```

#### Configuration File
```json
{
  "NodeNamingTemplate": "自定义名称-节点-{index}",
  "NodeNamingVariables": {
    "CustomPrefix": "MyProxy",
    "Location": "HK"
  }
}
```

#### Command-line Arguments
```bash
./ClashSubManager --NodeNamingTemplate "自定义-{index}" --CustomPrefix "Test"
```

### 5. Implementation Details

#### Interface Design
```csharp
public interface INodeNamingTemplateService
{
    /// <summary>
    /// Process template with variable replacement
    /// </summary>
    string ProcessTemplate(string template, NodeNamingContext context);
    
    /// <summary>
    /// Get available template variables
    /// </summary>
    Dictionary<string, object> GetVariables(NodeNamingContext context);
    
    /// <summary>
    /// Validate template syntax
    /// </summary>
    bool ValidateTemplate(string template, out string errorMessage);
}
```

#### Context Model
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

#### Variable Replacement Engine
```csharp
public class TemplateVariableProvider
{
    public Dictionary<string, object> ExtractVariables(YamlMappingNode proxyNode, int index, string newServer)
    {
        var variables = new Dictionary<string, object>();
        
        // Extract basic variables
        variables["name"] = ExtractStringValue(proxyNode, "name");
        variables["index"] = index + 1;
        variables["server"] = newServer;
        
        // Extract protocol-specific variables
        variables["network"] = ExtractStringValue(proxyNode, "type")?.ToLower();
        variables["type"] = ExtractStringValue(proxyNode, "type")?.ToUpper();
        variables["port"] = ExtractIntValue(proxyNode, "port");
        variables["servername"] = ExtractStringValue(proxyNode, "server");
        variables["uuid"] = ExtractStringValue(proxyNode, "uuid");
        
        // Create multi-level variables
        CreateNestedVariables(variables);
        
        return variables;
    }
    
    private void CreateNestedVariables(Dictionary<string, object> variables)
    {
        var nested = new Dictionary<string, object>();
        
        // Create proxy.* variables
        var proxyVars = new Dictionary<string, object>();
        foreach (var kvp in variables)
        {
            if (!kvp.Key.StartsWith("node."))
            {
                proxyVars[kvp.Key] = kvp.Value;
            }
        }
        nested["proxy"] = proxyVars;
        
        // Create node.* variables
        var nodeVars = new Dictionary<string, object>();
        nodeVars["index"] = variables["index"];
        nested["node"] = nodeVars;
        
        // Merge nested variables
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

### 6. Backward Compatibility

#### Migration Strategy
```csharp
// Automatic migration from old configuration
private string GetNamingTemplate()
{
    // Check for new template configuration
    var template = GetValue<string>("NodeNamingTemplate");
    if (!string.IsNullOrEmpty(template))
        return template;
    
    // Migrate from old UseIpInNodeName setting
    var useIpFormat = GetValue<bool>("UseIpInNodeName", false);
    return useIpFormat 
        ? "{name}-{server}" 
        : "{name}-Node-{index}";
}
```

#### Configuration Mapping
| Old Setting | New Template | Description |
|-------------|---------------|-------------|
| `UseIpInNodeName=true` | `{name}-{server}` | Include IP address |
| `UseIpInNodeName=false` | `{name}-Node-{index}` | Use node index |

### 7. Validation and Error Handling

#### Template Validation Rules
- Valid variable names: `[a-zA-Z][a-zA-Z0-9_]*`
- Variable format: `{variable}` or `{category.variable}`
- No nested variables: `{{variable}}` (invalid)
- Balanced braces required

#### Error Handling
```csharp
public ValidationResult ValidateTemplate(string template)
{
    var result = new ValidationResult();
    
    // Check for balanced braces
    var openBraces = template.Count(c => c == '{');
    var closeBraces = template.Count(c => c == '}');
    
    if (openBraces != closeBraces)
    {
        result.AddError("Unbalanced braces in template");
    }
    
    // Validate variable names
    var matches = Regex.Matches(template, @"\{([^}]+)\}");
    foreach (Match match in matches)
    {
        var variableName = match.Groups[1].Value;
        if (!IsValidVariableName(variableName))
        {
            result.AddError($"Invalid variable name: {variableName}");
        }
    }
    
    return result;
}
```

### 8. Performance Considerations

#### Optimization Strategies
- **Template Caching**: Cache compiled templates for reuse
- **Variable Pre-computation**: Pre-extract variables before processing
- **Regex Optimization**: Use compiled regex patterns
- **Lazy Evaluation**: Only compute variables when needed

#### Memory Management
- Reuse template context objects
- Cache variable extraction results
- Minimize string allocations during processing

### 9. Testing Strategy

#### Unit Test Coverage
- Template processing accuracy
- Variable extraction correctness
- Multi-level variable support
- Error handling validation
- Backward compatibility

#### Integration Test Scenarios
- End-to-end template processing
- Configuration source priority
- Real proxy configuration parsing
- Performance benchmarking

### 10. Future Enhancements

#### Conditional Templates
```
{#if network == 'vless'}VLESS-{index}{#else}{type}-{index}{/if}
```

#### Custom Functions
```
{name}-{upper(server)}-{pad(index, 3)}
```

#### Template Inheritance
```json
{
  "BaseTemplate": "{name}-{index}",
  "ProtocolTemplates": {
    "vless": "VLESS-{index}-{server}",
    "vmess": "VMess-{name}-{port}"
  }
}
```

## Implementation Plan

### Phase 1: Core Template System
1. Implement `INodeNamingTemplateService` interface
2. Create variable replacement engine
3. Add basic template validation
4. Integrate with existing configuration system

### Phase 2: Multi-level Variables
1. Implement nested variable support
2. Add proxy configuration parsing
3. Create variable extraction logic
4. Add comprehensive variable mapping

### Phase 3: Configuration Integration
1. Add environment variable support
2. Implement command-line argument parsing
3. Add configuration file support
4. Create configuration migration logic

### Phase 4: Testing and Documentation
1. Comprehensive unit test suite
2. Integration test scenarios
3. Performance benchmarking
4. User documentation and examples

## Conclusion

The node naming template system provides a flexible and extensible solution for customizing proxy node names. It supports multiple configuration sources, comprehensive variable replacement, and maintains backward compatibility while enabling advanced naming patterns.

The modular design allows for future enhancements and ensures the system can adapt to evolving user requirements.
