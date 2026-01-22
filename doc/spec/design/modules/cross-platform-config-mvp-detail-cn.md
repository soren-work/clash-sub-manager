# 跨平台配置管理MVP详细设计

**🌐 语言**: [English](./cross-platform-config-mvp-detail.md) | [中文](./cross-platform-config-mvp-detail-cn.md)

## 1. 模块核心功能

### 1.1 必要功能清单
- **统一配置服务**：具有优先级覆盖的集中配置管理
- **环境检测**：自动Docker/独立模式检测
- **跨平台路径解析**：平台无关的数据路径处理
- **配置验证**：启动时验证并提供清晰错误信息
- **向后兼容性**：保持现有Docker部署兼容性

### 1.2 实现优先级
1. **高优先级**：配置服务接口和基础实现
2. **高优先级**：环境检测逻辑
3. **高优先级**：配置验证
4. **中优先级**：与现有FileService集成
5. **低优先级**：高级配置功能（未来增强）

### 1.3 技术约束
- 必须使用.NET 10内置配置系统
- 不能破坏现有Docker部署
- 所有路径必须使用`Path.Combine()`保证跨平台兼容性
- 配置验证必须阻止无效配置的应用启动

## 2. 核心类设计

### 2.1 主要类图
```
IConfigurationService
├── ConfigurationService (实现)
├── IConfigurationValidator
│   └── ConfigurationValidator
├── IEnvironmentDetector
│   └── EnvironmentDetector
└── IPathResolver
    └── PathResolver
```

### 2.2 关键方法定义

#### IConfigurationService 接口
```csharp
public interface IConfigurationService
{
    string GetDataPath();
    T GetValue<T>(string key, T defaultValue = default);
    bool HasValue(string key);
    void ValidateConfiguration();
    string GetEnvironmentType();
}
```

#### ConfigurationService 实现
```csharp
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IEnvironmentDetector _environmentDetector;
    private readonly IPathResolver _pathResolver;
    
    public ConfigurationService(
        IConfiguration configuration,
        ILogger<ConfigurationService> logger,
        IEnvironmentDetector environmentDetector,
        IPathResolver pathResolver)
    {
        _configuration = configuration;
        _logger = logger;
        _environmentDetector = environmentDetector;
        _pathResolver = pathResolver;
    }
    
    public string GetDataPath()
    {
        var configuredPath = GetValue<string>("DataPath");
        if (!string.IsNullOrEmpty(configuredPath))
        {
            return _pathResolver.ResolvePath(configuredPath);
        }
        
        return _pathResolver.GetDefaultDataPath();
    }
}
```

#### IEnvironmentDetector 接口
```csharp
public interface IEnvironmentDetector
{
    string GetEnvironmentType();
    bool IsDockerEnvironment();
    bool IsWindowsEnvironment();
    bool IsLinuxEnvironment();
    bool IsMacOSEnvironment();
}
```

#### EnvironmentDetector 实现
```csharp
public class EnvironmentDetector : IEnvironmentDetector
{
    public string GetEnvironmentType()
    {
        if (IsDockerEnvironment())
            return "Docker";
            
        var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return !string.IsNullOrEmpty(aspnetEnv) ? aspnetEnv : "Standalone";
    }
    
    public bool IsDockerEnvironment()
    {
        return File.Exists("/.dockerenv") || 
               (File.Exists("/proc/1/cgroup") && 
                File.ReadAllText("/proc/1/cgroup").Contains("docker"));
    }
    
    public bool IsWindowsEnvironment() => OperatingSystem.IsWindows();
    public bool IsLinuxEnvironment() => OperatingSystem.IsLinux();
    public bool IsMacOSEnvironment() => OperatingSystem.IsMacOS();
}
```

#### IPathResolver 接口
```csharp
public interface IPathResolver
{
    string ResolvePath(string path);
    string GetDefaultDataPath();
    bool IsValidPath(string path);
}
```

#### PathResolver 实现
```csharp
public class PathResolver : IPathResolver
{
    private readonly IEnvironmentDetector _environmentDetector;
    
    public PathResolver(IEnvironmentDetector environmentDetector)
    {
        _environmentDetector = environmentDetector;
    }
    
    public string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;
            
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        return Path.Combine(assemblyDir, path);
    }
    
    public string GetDefaultDataPath()
    {
        if (_environmentDetector.IsDockerEnvironment())
            return "/app/data";
            
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        return Path.Combine(assemblyDir, "data");
    }
    
    public bool IsValidPath(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }
}
```

#### IConfigurationValidator 接口
```csharp
public interface IConfigurationValidator
{
    void Validate(IConfiguration configuration);
    List<string> GetValidationErrors(IConfiguration configuration);
}
```

#### ConfigurationValidator 实现
```csharp
public class ConfigurationValidator : IConfigurationValidator
{
    public void Validate(IConfiguration configuration)
    {
        var errors = GetValidationErrors(configuration);
        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"配置验证失败: {string.Join(", ", errors)}");
        }
    }
    
    public List<string> GetValidationErrors(IConfiguration configuration)
    {
        var errors = new List<string>();
        
        // 验证必需设置
        if (string.IsNullOrEmpty(configuration["AdminUsername"]))
            errors.Add("AdminUsername是必需的");
            
        if (string.IsNullOrEmpty(configuration["AdminPassword"]))
            errors.Add("AdminPassword是必需的");
            
        var secretKey = configuration["CookieSecretKey"];
        if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
            errors.Add("CookieSecretKey必须至少32字符");
        
        // 验证会话超时
        if (int.TryParse(configuration["SessionTimeoutMinutes"], out var timeout))
        {
            if (timeout < 5 || timeout > 1440)
                errors.Add("SessionTimeoutMinutes必须在5-1440之间");
        }
        
        // 验证数据路径
        var dataPath = configuration["DataPath"];
        if (!string.IsNullOrEmpty(dataPath))
        {
            try
            {
                var fullPath = Path.IsPathRooted(dataPath) ? dataPath : 
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", dataPath);
                Directory.CreateDirectory(fullPath);
            }
            catch (Exception ex)
            {
                errors.Add($"无法创建数据目录 '{dataPath}': {ex.Message}");
            }
        }
        
        return errors;
    }
}
```

### 2.3 数据结构

#### 配置优先级枚举
```csharp
public enum ConfigurationPriority
{
    CommandLine = 1,      // 最高
    EnvironmentVariable = 2,
    UserConfig = 3,
    EnvironmentConfig = 4,
    ModeConfig = 5,
    DefaultConfig = 6,
    CodeDefault = 7       // 最低
}
```

#### 环境类型常量
```csharp
public static class EnvironmentTypes
{
    public const string Docker = "Docker";
    public const string Standalone = "Standalone";
    public const string Development = "Development";
    public const string Production = "Production";
}
```

## 3. 实施要点

### 3.1 关键实现逻辑

#### Program.cs中的配置加载序列
```csharp
var builder = WebApplication.CreateBuilder(args);

// 获取环境类型
var environmentDetector = new EnvironmentDetector();
var environmentType = environmentDetector.GetEnvironmentType();

// 配置具有优先级的配置加载
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environmentType}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// 验证配置
var validator = new ConfigurationValidator();
validator.Validate(builder.Configuration);

// 注册服务
builder.Services.AddSingleton<IEnvironmentDetector, EnvironmentDetector>();
builder.Services.AddSingleton<IPathResolver, PathResolver>();
builder.Services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
```

#### FileService集成
```csharp
public class FileService
{
    private readonly IConfigurationService _configurationService;
    private readonly string _dataPath;
    
    public FileService(IConfigurationService configurationService, ILogger<FileService> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
        _dataPath = _configurationService.GetDataPath();
        
        // 确保数据目录存在
        Directory.CreateDirectory(_dataPath);
    }
    
    // 所有现有方法保持不变，使用_dataPath
}
```

### 3.2 错误处理（必需部分）

#### 配置验证错误
```csharp
public class ConfigurationException : Exception
{
    public List<string> ValidationErrors { get; }
    
    public ConfigurationException(List<string> validationErrors) 
        : base($"配置验证失败: {string.Join(", ", validationErrors)}")
    {
        ValidationErrors = validationErrors;
    }
}
```

#### 路径解析错误
```csharp
public class PathResolutionException : Exception
{
    public string InvalidPath { get; }
    
    public PathResolutionException(string invalidPath, Exception innerException) 
        : base($"路径解析失败: {invalidPath}", innerException)
    {
        InvalidPath = invalidPath;
    }
}
```

### 3.3 性能要求（MVP标准）

#### 配置加载性能
- 配置加载必须在100ms内完成
- 环境检测必须在10ms内完成
- 路径解析必须在5ms内完成

#### 内存使用
- 配置服务内存使用必须低于1MB
- 配置重载场景中无内存泄漏

#### 缓存策略
- 缓存解析的路径避免重复文件系统操作
- 缓存环境类型检测结果
- 支持适用的配置热重载

---

## 语言版本
- [English](./cross-platform-config-mvp-detail.md) | [中文](./cross-platform-config-mvp-detail-cn.md)
