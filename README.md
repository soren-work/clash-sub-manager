# ClashSubManager

[中文文档](README-CN.md) | **English**

Clash Subscription Configuration Manager - Add Preferred IPs and Custom Configurations to Your Clash Subscription

## Table of Contents

- [What is This?](#what-is-this)
- [Why Do You Need It?](#why-do-you-need-it)
- [How It Works](#how-it-works)
- [Core Features](#core-features)
- [Quick Start](#quick-start)
- [Usage Guide](#usage-guide)
- [Configuration Description](#configuration-description)
- [Configuration System](#configuration-system)
- [Performance Characteristics](#performance-characteristics)
- [Security Features](#security-features)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [Support the Project](#support-the-project)
- [License](#license)

## What is This?

ClashSubManager is a **subscription proxy service** that sits between your Clash client and the original subscription service, automatically processing and optimizing your subscription configuration.

Simply put:
```
Original Subscription → ClashSubManager Processing → Optimized Subscription → Clash Client
```

## Why Do You Need It?

### Problems It Solves

1. **Preferred IP Replacement**: Your subscription has domain-based nodes like `cdn.example.com`, but you want to use speed-tested preferred IPs (like `104.29.125.182`) to improve connection speed
2. **Batch Node Generation**: Automatically expand one domain node into multiple preferred IP nodes without manual configuration editing
3. **Personalized Configuration**: Add your own rules, proxy groups, and other configurations without modifying the original subscription
4. **Unified Management**: Manage preferred IP lists and Clash templates through a web interface without manual YAML editing

### Use Cases

- You have an airport subscription but want to use Cloudflare preferred IPs for acceleration
- You want to use different preferred IP configurations for different devices
- You want to add your own rules and proxy groups on top of the subscription
- You want to centrally manage subscription configurations for multiple users

## How It Works

### Data Flow Process

```
1. Clash client requests subscription
   ↓
2. ClashSubManager fetches original subscription
   ↓
3. Reads preferred IP list (cloudflare-ip.csv)
   ↓
4. Reads Clash template configuration (clash.yaml)
   ↓
5. Processes subscription:
   - Expands domain nodes into multiple preferred IP nodes
   - Merges template configuration (rules, proxy groups, etc.)
   ↓
6. Returns processed configuration to Clash client
```

### Configuration Expansion Example

**Original subscription node:**
```yaml
proxies:
  - name: "US-Node"
    type: vmess
    server: cdn.example.com
    port: 443
```

**Preferred IP list (cloudflare-ip.csv):**
```csv
IP Address,Average Latency
104.29.125.182,152.45ms
104.26.0.188,158.10ms
104.20.20.191,161.38ms
```

**Processed nodes:**
```yaml
proxies:
  - name: "US-Node"
    type: vmess
    server: cdn.example.com
    port: 443
  - name: "US-Node [104.29.125.182]"
    type: vmess
    server: 104.29.125.182
    port: 443
  - name: "US-Node [104.26.0.188]"
    type: vmess
    server: 104.26.0.188
    port: 443
  - name: "US-Node [104.20.20.191]"
    type: vmess
    server: 104.20.20.191
    port: 443
```

One node automatically becomes 4 nodes (original domain + 3 preferred IPs), and you can select the one with the lowest latency in Clash.

## Core Features

### 🎯 Main Features

- **Unified Subscription Entry**: Provides standardized subscription interface through `/sub/[user_id]`
- **Automatic Preferred IP Expansion**: Automatically expands domain proxy nodes into multiple preferred IP address nodes
- **Dynamic Configuration Merging**: Intelligently merges original subscription with custom templates (rules, proxy groups, etc.)
- **Multi-User Support**: Each user can have independent preferred IP lists and Clash templates
- **Web Management Interface**: Visual management of preferred IPs, Clash templates, and user lists
- **Internationalization Support**: Full English and Chinese interface support
- **Lightweight Architecture**: Single application with minimal resource usage (< 50MB memory)

### 🔧 Technology Stack

- **.NET 10** - Main development framework
- **ASP.NET Core Razor Pages** - Web development mode
- **Bootstrap** - Frontend UI framework
- **Docker** - Containerized deployment

## Quick Start

### Prerequisites

- Docker (recommended) or .NET 10 runtime
- A valid Clash subscription URL

### 🐳 Docker Deployment (Recommended)

**Minimal Configuration:**
```bash
docker run -d \
  -p 8080:80 \
  -e AdminUsername=admin \
  -e AdminPassword=your_password \
  -e CookieSecretKey=your_32_character_minimum_secret_key_here \
  -e SUBSCRIPTION_URL_TEMPLATE=https://your-airport.com/sub/{userId} \
  -v $(pwd)/data:/app/data \
  --name clash-sub-manager \
  clashsubmanager:latest
```

**Parameter Explanation:**
- `AdminUsername`: Administrator username (customize)
- `AdminPassword`: Administrator password (customize)
- `CookieSecretKey`: Cookie encryption key (at least 32 characters, randomly generated)
- `SUBSCRIPTION_URL_TEMPLATE`: Your original subscription URL template, `{userId}` will be replaced with actual user ID

**Full Configuration Example:**
```bash
docker run -d \
  -p 8080:80 \
  -e AdminUsername=admin \
  -e AdminPassword=MySecurePassword123 \
  -e CookieSecretKey=abcdef1234567890abcdef1234567890 \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  -e SessionTimeoutMinutes=30 \
  -e DataPath=/app/data \
  -v $(pwd)/data:/app/data \
  --name clash-sub-manager \
  --restart unless-stopped \
  clashsubmanager:latest
```

### 💻 Standalone Execution

```bash
# Windows
./ClashSubManager.exe

# Linux/macOS
./ClashSubManager

# Custom data path
./ClashSubManager --DataPath /custom/data/path
```

### 📁 Project Structure

```
ClashSubManager/
├── server/          # Server application
├── doc/            # Project documentation
├── data/           # Data directory (created at runtime)
│   ├── cloudflare-ip.csv    # Default preferred IP list
│   ├── clash.yaml           # Default Clash template
│   ├── users.txt            # User access records
│   └── [userId]/            # User-specific configurations
└── README.md       # Project description
```

## Usage Guide

### Step 1: Start the Service

After starting the service according to the "Quick Start" section, visit:
```
http://localhost:8080
```

### Step 2: Login to Admin Interface

Access the admin interface:
```
http://localhost:8080/admin
```

Login with your configured admin credentials.

### Step 3: Configure Preferred IP List

In the admin interface:
1. Click "Default Preferred IP Management"
2. Upload or edit your preferred IP list (CSV format)
3. CSV format example:
```csv
IP Address,Sent,Received,Packet Loss Rate,Average Latency,Download Speed
104.29.125.182,4,4,0.00%,152.45ms,0.00
104.26.0.188,4,4,0.00%,158.10ms,0.00
```

### Step 4: Configure Clash Template (Optional)

In the admin interface:
1. Click "Default Clash Template Management"
2. Edit your Clash configuration template
3. You can add custom rules, proxy groups, etc.

### Step 5: Use in Clash Client

Change your Clash client's subscription URL to:
```
http://your-server:8080/sub/your_user_id
```

For example:
```
http://localhost:8080/sub/user123
```

### Advanced Feature: User-Specific Configuration

If you want to set independent preferred IPs or templates for a specific user:
1. Find the user in the "User List" in the admin interface
2. Click "Manage" to enter user-specific configuration
3. Upload the user's preferred IP list or Clash template
4. The user's subscription will prioritize user-specific configuration, falling back to default if not available

### 🔄 API Endpoints

- `GET /sub/{id}` - Get user Clash subscription configuration
- `POST /sub/{id}` - Update user preferred IP configuration
- `DELETE /sub/{id}` - Delete user preferred IP configuration

## Configuration Description

### 📊 Data Storage Structure

```
/app/data/
├── cloudflare-ip.csv     # Default preferred IP list
├── clash.yaml           # Default Clash template
├── users.txt            # User access records (auto-generated)
└── [userId]/            # User-specific configuration directory
    ├── cloudflare-ip.csv  # User-specific preferred IPs
    └── clash.yaml         # User-specific template
```

### 🎛️ Environment Variables

| Variable | Description | Default | Example |
|----------|-------------|---------|---------|
| `AdminUsername` | Administrator username | Required | `admin` |
| `AdminPassword` | Administrator password | Required | `MyPassword123` |
| `CookieSecretKey` | Cookie encryption key | Required (≥32 chars) | `abcdef1234567890abcdef1234567890` |
| `SUBSCRIPTION_URL_TEMPLATE` | Original subscription URL template | Required | `https://api.example.com/sub/{userId}` |
| `SessionTimeoutMinutes` | Session timeout (minutes) | 60 | `30` |
| `DataPath` | Data directory path | Docker: `/app/data`<br>Standalone: `./data` | `/custom/path` |
| `LOG_LEVEL` | Log level | `Information` | `Debug` |

**Notes:**
- `SUBSCRIPTION_URL_TEMPLATE` must contain `{userId}` placeholder
- `CookieSecretKey` requires at least 32 characters, recommend using random string

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

#### Method 1: Environment Variables (Docker Recommended)

```bash
docker run -d \
  -e AdminUsername=admin \
  -e AdminPassword=your_password \
  -e CookieSecretKey=your_32_character_minimum_key \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  -e SessionTimeoutMinutes=30 \
  -p 8080:80 \
  clash-sub-manager
```

#### Method 2: Configuration File

Create `appsettings.User.json` in program directory:
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

#### Method 3: Command Line Arguments

```bash
./ClashSubManager \
  --AdminUsername admin \
  --AdminPassword your_password \
  --DataPath /custom/data/path
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

### 📚 System Architecture
- [System Architecture Overview](doc/spec/design/architecture/mvp-outline.md)
- [Core Features Description](doc/spec/design/architecture/mvp-core-features.md)
- [Cross-Platform Configuration Design](doc/spec/design/architecture/cross-platform-config-mvp-outline.md)

### 🚀 Deployment & Operations
- [Deployment Guide](doc/deployment/deployment-guide.md)
- [Environment Variable Configuration](doc/deployment/env-config.md)
- [Environment Variables Reference](doc/deployment/environment-variables.md)

## Contributing

Issues and Pull Requests are welcome to improve the project.

## Support the Project

If this project has been helpful to you, consider supporting its development:

[![Support via Crypto](https://img.shields.io/badge/Donate-Crypto-yellow?style=for-the-badge)](DONATE.md)

See [DONATE.md](DONATE.md) for cryptocurrency donation addresses.

## License

This project is licensed under the [GPL-3.0 License](LICENSE). 
Copyright (c) 2026 Soren S.