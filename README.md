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