# ClashSubManager

Clash Subscription Configuration Manager - Lightweight Clash Subscription Proxy Service

## Project Introduction

ClashSubManager is a lightweight Clash subscription configuration management service that acts as an intermediate layer between Clash clients and subscription services, providing dynamic subscription data overwriting and personalized configuration capabilities.

## Core Features

### 🎯 Main Features
- **Unified Subscription Entry**: Provides standardized subscription interface through `/sub/[user_id]`
- **Dynamic Configuration Overwriting**: Completely dynamic parsing and merging of Clash configurations, supporting future version compatibility
- **Preferred IP Extension**: Automatically extends domain proxies to multiple preferred IP address proxies
- **Personalized Configuration**: Supports flexible switching between user-specific configurations and default configurations
- **Admin Management Interface**: Complete web-based management system for IP configurations, Clash templates, and user settings
- **Internationalization Support**: Full English and Chinese interface support
- **Lightweight Architecture**: Single application with minimal resource usage

### 🔧 Technology Stack
- **.NET 10** - Main development framework
- **ASP.NET Core Razor Pages** - Web development mode
- **Bootstrap** - Frontend UI framework
- **Docker** - Containerized deployment

## Quick Start

### 🐳 Docker Deployment
```bash
# Pull image
docker pull clashsubmanager:latest

# Run container
docker run -d \
  -p 8080:80 \
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=your_password \
  -e COOKIE_SECRET_KEY=your_32_char_secret_key \
  -v $(pwd)/data:/app/data \
  clashsubmanager:latest
```

### 📁 Directory Structure
```
ClashSubManager/
├── server/          # Server application
├── client/          # Client scripts
├── doc/            # Project documentation
└── README.md       # Project description
```

## Usage Guide

### 📱 Clash Client Configuration
Configure subscription URL in Clash client:
```
http://your-server:8080/sub/your_user_id
```

### ⚙️ Management Interface
Access `http://your-server:8080/admin` for configuration management:
- Preferred IP management
- Clash template management  
- User-specific configuration management

### 🔄 API Endpoints
- `GET /sub/{id}` - Get user Clash subscription configuration
- `POST /sub/{id}` - Update user preferred IP configuration
- `DELETE /sub/{id}` - Delete user preferred IP configuration

## Configuration Description

### 📊 Data Storage
```
/app/data/
├── cloudflare-ip.csv     # Default preferred IPs
├── clash.yaml           # Default Clash template
└── [userId]/            # User-specific configurations
    ├── cloudflare-ip.csv
    └── clash.yaml
```

### 🎛️ Environment Variables
| Variable | Description | Default |
|----------|-------------|---------|
| `ADMIN_USERNAME` | Admin username | Required |
| `ADMIN_PASSWORD` | Admin password | Required |
| `COOKIE_SECRET_KEY` | Cookie secret key | Required (≥32 characters) |
| `SESSION_TIMEOUT_MINUTES` | Session timeout | 60 |
| `DATA_PATH` | Data directory | `/app/data` |

## Configuration System

ClashSubManager supports flexible cross-platform configuration management with multiple configuration methods:

### Configuration Priority (High to Low)
1. **Command Line Arguments** - Highest priority
2. **Environment Variables** - Second priority
3. **User Configuration File** - `appsettings.User.json`
4. **Environment-Specific Configuration** - `appsettings.{Environment}.json`
5. **Mode-Specific Configuration** - `appsettings.{Mode}.json`
6. **Default Configuration File** - `appsettings.json`
7. **Code Default Values** - Lowest priority

### Automatic Environment Detection
- **Docker Environment**: Automatically detects container environment, uses `/app/data` as default data path
- **Standalone Mode**: Windows/Linux/macOS direct execution, uses `./data` path in program directory
- **Development/Production Environment**: Automatically recognizes based on `ASPNETCORE_ENVIRONMENT` variable

### Configuration Examples

#### Docker Deployment (Recommended)
```bash
docker run -d \
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=your_password \
  -e COOKIE_SECRET_KEY=your_32_character_minimum_key \
  -e SESSION_TIMEOUT_MINUTES=30 \
  -p 8080:8080 \
  clash-sub-manager
```

#### Standalone Execution
```bash
# Windows
./ClashSubManager.exe

# Linux/macOS
./ClashSubManager

# Custom data path
./ClashSubManager --DataPath /custom/data/path
```

#### Configuration File
Create `appsettings.User.json` file:
```json
{
  "AdminUsername": "admin",
  "AdminPassword": "your_password",
  "CookieSecretKey": "your_32_character_minimum_key",
  "SessionTimeoutMinutes": 30,
  "DataPath": "/custom/data/path"
}
```

### Configuration Validation
The system automatically validates the following required configurations on startup:
- `AdminUsername` - Administrator username (required)
- `AdminPassword` - Administrator password (required)
- `CookieSecretKey` - Cookie secret key (required, minimum 32 characters)
- `SessionTimeoutMinutes` - Session timeout (5-1440 minutes)
- `DataPath` - Data path (must be creatable/writable)

## Performance Characteristics

- **Response Time**: < 100ms
- **Concurrent Processing**: 10-50 requests/second
- **Memory Usage**: < 50MB
- **File Limits**: CSV max 10MB, YAML max 1MB

## Security Features

- Cookie security settings (HttpOnly, Secure, SameSite=Strict)
- Session timeout mechanism
- Request rate limiting (10 requests per IP per second)
- Strict data format validation
- Docker containerized deployment

## Documentation

For detailed documentation, see the `doc/` directory:
- [MVP Outline Design](doc/spec/design/architecture/mvp-outline-cn.md)
- [Deployment Operations Guide](doc/deployment/deployment-guide-cn.md)
- [Environment Variable Configuration](doc/deployment/env-config-cn.md)

## Contributing

Issues and Pull Requests are welcome to improve the project.

## License

This project is licensed under the [GPL-3.0 License](LICENSE). 
Copyright (c) 2026 Soren S.

---

**Document Version**: v1.2  
**Created**: 2026-01-22  
**Updated**: 2026-01-23  
**Maintainer**: AI Agent Software Engineer  
**Scope**: ClashSubManager MVP Development Project