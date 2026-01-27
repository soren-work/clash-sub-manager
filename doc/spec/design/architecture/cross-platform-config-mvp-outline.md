# ClashSubManager Cross-Platform Configuration Management MVP Outline

**🌐 Language**: [English](./cross-platform-config-mvp-outline.md) | [中文](./cross-platform-config-mvp-outline-cn.md)

## 1. MVP Core Features

### 1.1 Core Value Validation Points
- **Unified Configuration Management**: Single configuration system for all platforms
- **Environment Auto-Detection**: Automatic Docker/Standalone mode detection
- **Flexible Data Path**: User-configurable data storage location
- **Cross-Platform Compatibility**: Windows/Linux/macOS support

### 1.2 Minimum Feature Set
- **Configuration Service**: Unified configuration management with priority override
- **Environment Detection**: Automatic Docker/Standalone environment detection
- **Path Resolution**: Cross-platform data path configuration
- **Configuration Validation**: Startup-time configuration validation

### 1.3 Explicitly Excluded Features
- GUI configuration tools
- Configuration migration utilities
- Advanced configuration templates
- Remote configuration management

## 2. Technical Architecture

### 2.1 Core Architecture Diagram
```
Application Startup
├── Environment Detection
│   ├── Docker Environment Check
│   └── Platform Detection (Windows/Linux/macOS)
├── Configuration Loading
│   ├── Base Configuration (appsettings.json)
│   ├── Environment-Specific Config
│   ├── User Configuration (appsettings.User.json)
│   ├── Environment Variables
│   └── Command Line Arguments
├── Configuration Validation
└── Service Registration
```

### 2.2 Technology Selection (MVP Related)
- **.NET 10**: Core framework
- **Microsoft.Extensions.Configuration**: Configuration management
- **System.IO**: Cross-platform file operations
- **System.Environment**: Environment variable access

### 2.3 Deployment Scheme (Simplified)
- **Docker Mode**: Keep existing `/app/data` path
- **Standalone Mode**: Use `./data` relative to executable
- **Configuration Override**: Support environment variables for all settings

## 3. Implementation Boundaries

### 3.1 AI Agent Development Scope
- Implement `IConfigurationService` interface
- Create `EnvironmentDetector` utility class
- Add configuration validation logic
- Update `FileService` to use new configuration system
- Modify `Program.cs` for unified configuration loading

### 3.2 Technical Constraints
- Must maintain backward compatibility with existing Docker deployment
- Configuration validation must prevent application startup with invalid settings
- All configuration changes must support hot-reload where possible
- Cross-platform path handling must use `Path.Combine()` for compatibility

### 3.3 Acceptance Criteria
- Application starts successfully in both Docker and standalone modes
- Data path can be configured via environment variables or configuration files
- Configuration validation catches and reports all critical misconfigurations
- Existing Docker deployment continues to work without modification
- Windows/Linux/macOS standalone execution works with default configuration

## 4. Configuration Priority System

### 4.1 Priority Order (High to Low)
1. Command Line Arguments
2. Environment Variables
3. User Configuration File (`appsettings.User.json`)
4. Environment-Specific Configuration (`appsettings.{Environment}.json`)
5. Mode-Specific Configuration (`appsettings.{Mode}.json`)
6. Default Configuration File (`appsettings.json`)
7. Code Default Values

### 4.2 Core Configuration Items
- `DataPath`: Data storage directory path
- `AdminUsername`: Administrator username
- `AdminPassword`: Administrator password
- `CookieSecretKey`: Cookie signing secret key (≥32 characters)
- `SessionTimeoutMinutes`: Session timeout in minutes
- `LogLevel`: Logging level
- `AllowedHosts`: Allowed hosts for binding

### 4.3 Configuration Validation Rules
- `AdminUsername`: Must not be empty
- `AdminPassword`: Must not be empty
- `CookieSecretKey`: Must be at least 32 characters
- `DataPath`: Must be creatable/writable directory
- `SessionTimeoutMinutes`: Must be between 5-1440

---

## Language Versions
- [English](./cross-platform-config-mvp-outline.md) | [中文](./cross-platform-config-mvp-outline-cn.md)
