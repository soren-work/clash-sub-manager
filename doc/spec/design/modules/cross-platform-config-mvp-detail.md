# Cross-Platform Configuration Management MVP Detail Design

> **📌 Document Status**: MVP completed, this document is retained as technical reference  
> **🎯 Target Audience**: Developers, contributors  
> **📅 Last Updated**: 2026-02-20  
> **💡 Tip**: For feature usage, please refer to [Advanced Guide](../../../advanced-guide.md)

**🌐 Language**: [English](./cross-platform-config-mvp-detail.md) | [中文](./cross-platform-config-mvp-detail-cn.md)

## 1. Module Core Features

### 1.1 Required Feature List
- **Unified Configuration Service**: Centralized configuration management with priority override
- **Environment Detection**: Automatic Docker/Standalone mode detection
- **Cross-Platform Path Resolution**: Platform-independent data path handling
- **Configuration Validation**: Startup-time validation with clear error messages
- **Backward Compatibility**: Maintain existing Docker deployment compatibility

### 1.2 Implementation Priority
1. **High Priority**: Configuration service interface and basic implementation
2. **High Priority**: Environment detection logic
3. **High Priority**: Configuration validation
4. **Medium Priority**: Integration with existing FileService
5. **Low Priority**: Advanced configuration features (future enhancement)

### 1.3 Technical Constraints
- Must use .NET 10 built-in configuration system
- Cannot break existing Docker deployment
- All paths must use `Path.Combine()` for cross-platform compatibility
- Configuration validation must prevent application startup with invalid settings

## 2. Core Class Design

### 2.1 Main Class Diagram
```
IConfigurationService
├── ConfigurationService (Implementation)
├── IConfigurationValidator
│   └── ConfigurationValidator
├── IEnvironmentDetector
│   └── EnvironmentDetector
└── IPathResolver
    └── PathResolver
```

### 2.2 Key Method Definitions

#### IConfigurationService Interface
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

#### ConfigurationService Implementation
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

#### IEnvironmentDetector Interface
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

#### EnvironmentDetector Implementation
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

#### IPathResolver Interface
```csharp
public interface IPathResolver
{
    string ResolvePath(string path);
    string GetDefaultDataPath();
    bool IsValidPath(string path);
}
```

#### PathResolver Implementation
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

#### IConfigurationValidator Interface
```csharp
public interface IConfigurationValidator
{
    void Validate(IConfiguration configuration);
    List<string> GetValidationErrors(IConfiguration configuration);
}
```

#### ConfigurationValidator Implementation
```csharp
public class ConfigurationValidator : IConfigurationValidator
{
    public void Validate(IConfiguration configuration)
    {
        var errors = GetValidationErrors(configuration);
        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Configuration validation failed: {string.Join(", ", errors)}");
        }
    }
    
    public List<string> GetValidationErrors(IConfiguration configuration)
    {
        var errors = new List<string>();
        
        // Validate required settings
        if (string.IsNullOrEmpty(configuration["AdminUsername"]))
            errors.Add("AdminUsername is required");
            
        if (string.IsNullOrEmpty(configuration["AdminPassword"]))
            errors.Add("AdminPassword is required");
            
        var secretKey = configuration["CookieSecretKey"];
        if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
            errors.Add("CookieSecretKey must be at least 32 characters");
        
        // Validate session timeout
        if (int.TryParse(configuration["SessionTimeoutMinutes"], out var timeout))
        {
            if (timeout < 5 || timeout > 1440)
                errors.Add("SessionTimeoutMinutes must be between 5 and 1440");
        }
        
        // Validate data path
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
                errors.Add($"Cannot create data directory '{dataPath}': {ex.Message}");
            }
        }
        
        return errors;
    }
}
```

### 2.3 Data Structures

#### Configuration Priority Enum
```csharp
public enum ConfigurationPriority
{
    CommandLine = 1,      // Highest
    EnvironmentVariable = 2,
    UserConfig = 3,
    EnvironmentConfig = 4,
    ModeConfig = 5,
    DefaultConfig = 6,
    CodeDefault = 7       // Lowest
}
```

#### Environment Type Constants
```csharp
public static class EnvironmentTypes
{
    public const string Docker = "Docker";
    public const string Standalone = "Standalone";
    public const string Development = "Development";
    public const string Production = "Production";
}
```

## 3. Implementation Points

### 3.1 Key Implementation Logic

#### Configuration Loading Sequence in Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);

// Get environment type
var environmentDetector = new EnvironmentDetector();
var environmentType = environmentDetector.GetEnvironmentType();

// Configure configuration loading with priority
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environmentType}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// Validate configuration
var validator = new ConfigurationValidator();
validator.Validate(builder.Configuration);

// Register services
builder.Services.AddSingleton<IEnvironmentDetector, EnvironmentDetector>();
builder.Services.AddSingleton<IPathResolver, PathResolver>();
builder.Services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
```

#### FileService Integration
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
        
        // Ensure data directory exists
        Directory.CreateDirectory(_dataPath);
    }
    
    // All existing methods remain the same, using _dataPath
}
```

### 3.2 Error Handling (Required Parts)

#### Configuration Validation Errors
```csharp
public class ConfigurationException : Exception
{
    public List<string> ValidationErrors { get; }
    
    public ConfigurationException(List<string> validationErrors) 
        : base($"Configuration validation failed: {string.Join(", ", validationErrors)}")
    {
        ValidationErrors = validationErrors;
    }
}
```

#### Path Resolution Errors
```csharp
public class PathResolutionException : Exception
{
    public string InvalidPath { get; }
    
    public PathResolutionException(string invalidPath, Exception innerException) 
        : base($"Failed to resolve path: {invalidPath}", innerException)
    {
        InvalidPath = invalidPath;
    }
}
```

### 3.3 Performance Requirements (MVP Standard)

#### Configuration Loading Performance
- Configuration loading must complete within 100ms
- Environment detection must complete within 10ms
- Path resolution must complete within 5ms

#### Memory Usage
- Configuration service memory usage must be under 1MB
- No memory leaks in configuration reload scenarios

#### Caching Strategy
- Cache resolved paths to avoid repeated file system operations
- Cache environment type detection result
- Support configuration hot-reload where applicable

---

## Language Versions
- [English](./cross-platform-config-mvp-detail.md) | [中文](./cross-platform-config-mvp-detail-cn.md)
