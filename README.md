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
- **Admin Management Interface**: Web-based management system for default/user IP lists and Clash templates
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
  -e AdminUsername=admin \
  -e AdminPassword=your_password \
  -e CookieSecretKey=your_32_char_secret_key \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  -e SessionTimeoutMinutes=30 \
  -e DataPath=/app/data \
  -v $(pwd)/data:/app/data \
  clashsubmanager:latest
```

### 📁 Directory Structure
```
ClashSubManager/
├── server/          # Server application
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
- User list management (recorded automatically)

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
├── users.txt            # User access records
└── [userId]/            # User-specific configurations
    ├── cloudflare-ip.csv
    └── clash.yaml
```

### 🎛️ Environment Variables
| Variable | Description | Default |
|----------|-------------|---------|
| `AdminUsername` | Admin username | Required |
| `AdminPassword` | Admin password | Required |
| `CookieSecretKey` | Cookie secret key | Required (≥32 characters) |
| `SessionTimeoutMinutes` | Session timeout | 60 |
| `DataPath` | Data directory (absolute or relative to executable) | `./data` (standalone) / `/app/data` (Docker) |
| `SubscriptionUrlTemplate` | Upstream subscription URL template (must contain `{userId}`) | Optional (fallback) |
| `SUBSCRIPTION_URL_TEMPLATE` | Upstream subscription URL template (overrides `SubscriptionUrlTemplate`) | Required |
| `LOG_LEVEL` | Log level | Optional |

## Configuration System

ClashSubManager supports flexible cross-platform configuration management with multiple configuration methods:

### Configuration Priority (High to Low)
1. **Command Line Arguments** - Highest priority
2. **Environment Variables** - Second priority
3. **User Configuration File** - `appsettings.User.json`
4. **Environment Type Configuration** - `appsettings.{EnvironmentType}.json` (e.g. Docker/Standalone)
5. **Default Configuration File** - `appsettings.json`
6. **Code Default Values** - Lowest priority

### Automatic Environment Detection
- **Docker Environment**: Automatically detects container environment, uses `/app/data` as default data path
- **Standalone Mode**: Windows/Linux/macOS direct execution, uses `./data` path in program directory
- **Development/Production Environment**: Automatically recognizes based on `ASPNETCORE_ENVIRONMENT` variable

### Configuration Examples

#### Docker Deployment (Recommended)
```bash
docker run -d \
  -e AdminUsername=admin \
  -e AdminPassword=your_password \
  -e CookieSecretKey=your_32_character_minimum_key \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  -e SessionTimeoutMinutes=30 \
  -e DataPath=/app/data \
  -p 8080:80 \
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
  "DataPath": "/custom/data/path",
  "SubscriptionUrlTemplate": "https://api.example.com/sub/{userId}"
}
```

### Configuration Validation
The system automatically validates the following required configurations on startup:
- `AdminUsername` - Administrator username (required)
- `AdminPassword` - Administrator password (required)
- `CookieSecretKey` - Cookie secret key (required, minimum 32 characters)
- `SessionTimeoutMinutes` - Session timeout (5-1440 minutes)
- `DataPath` - Data path (must be creatable/writable)

### Language Switching
The UI language is determined by the `.AspNetCore.Culture` cookie (set via the built-in language switcher), with fallback to `en-US`.

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
- [Environment Variable Configuration](doc/deployment/env-config.md)

## Contributing

Issues and Pull Requests are welcome to improve the project.

## Support the Project

If this project has been helpful to you, consider supporting its development:

[![Support via Crypto](https://img.shields.io/badge/Donate-Crypto-yellow?style=for-the-badge)](DONATE.md)

See [DONATE.md](DONATE.md) for cryptocurrency donation addresses.

## License

This project is licensed under the [GPL-3.0 License](LICENSE). 
Copyright (c) 2026 Soren S.