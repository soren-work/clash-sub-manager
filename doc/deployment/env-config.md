# Environment Variable Configuration Guide

**🌐 Language**: [English](env-config.md) | [Chinese](env-config-cn.md)

## Overview

ClashSubManager uses environment variables for system configuration, supporting flexible deployment and security management.

## Required Environment Variables

### AdminUsername
- **Description**: Admin username
- **Type**: String
- **Default Value**: None (must be set)
- **Example**: `admin`
- **Requirements**: Non-empty string

```bash
AdminUsername=admin
```

### AdminPassword
- **Description**: Admin password
- **Type**: String
- **Default Value**: None (must be set)
- **Example**: `MySecureP@ssw0rd2024!`
- **Requirements**: Non-empty string, strong password recommended

```bash
AdminPassword=MySecureP@ssw0rd2024!
```

### CookieSecretKey
- **Description**: Cookie signing key for HMACSHA256 signature
- **Type**: String
- **Default Value**: None (must be set)
- **Example**: `32_character_long_secret_key`
- **Requirements**: Random string of at least 32 characters

```bash
CookieSecretKey=32_character_long_secret_key
```

### SessionTimeoutMinutes
- **Description**: Session timeout (minutes)
- **Type**: Integer
- **Default Value**: 60
- **Example**: `60`
- **Requirements**: Integer between 5-1440

```bash
SessionTimeoutMinutes=60
```

## Optional Environment Variables

### ASPNETCORE_ENVIRONMENT
- **Description**: ASP.NET Core runtime environment
- **Type**: String
- **Default Value**: `Production`
- **Available Values**: `Development`, `Staging`, `Production`

```bash
ASPNETCORE_ENVIRONMENT=Production
```

### LOG_LEVEL
- **Description**: Log level
- **Type**: String
- **Default Value**: `Information`
- **Available Values**: `Debug`, `Information`, `Warning`, `Error`, `Critical`

```bash
LOG_LEVEL=Information
```

### DataPath
- **Description**: Data directory (absolute path or relative to executable)
- **Type**: String
- **Default Value**: Standalone: `./data`, Docker: `/app/data`

```bash
DataPath=./data
```

### SUBSCRIPTION_URL_TEMPLATE
- **Description**: Upstream subscription URL template (must contain `{userId}` placeholder)
- **Type**: String
- **Default Value**: None
- **Note**: Can also be set via `SubscriptionUrlTemplate` in configuration file, but environment variable takes precedence

```bash
SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}
```

## Configuration Examples

### Development Environment Configuration
```bash
# .env.development
AdminUsername=admin
AdminPassword=DevPass123!
CookieSecretKey=dev_secret_key_32_characters_long
SessionTimeoutMinutes=120
ASPNETCORE_ENVIRONMENT=Development
LOG_LEVEL=Debug
DataPath=./data
SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}
```

### Production Environment Configuration
```bash
# .env.production
AdminUsername=admin
AdminPassword=ProdSecureP@ssw0rd2024!
CookieSecretKey=$(openssl rand -hex 16)
SessionTimeoutMinutes=30
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
DataPath=/app/data
SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}
```

### Testing Environment Configuration
```bash
# .env.testing
AdminUsername=test_admin
AdminPassword=TestPass123!
CookieSecretKey=test_secret_key_32_characters_long
SessionTimeoutMinutes=60
ASPNETCORE_ENVIRONMENT=Staging
LOG_LEVEL=Warning
DataPath=./data
SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}
```

## Security Configuration Recommendations

### 1. Generate Strong Passwords
```bash
# Generate 12-character strong password
openssl rand -base64 12 | tr -d "=+/" | cut -c1-12

# Generate 16-character strong password
openssl rand -base64 16 | tr -d "=+/" | cut -c1-16
```

### 2. Generate Secure Keys
```bash
# Generate 32-character random key
openssl rand -hex 16

# Generate 64-character random key
openssl rand -hex 32
```

### 3. Environment Variable File Permissions
```bash
# Set environment variable file permissions
chmod 600 .env.production
chmod 600 .env.staging

# Ensure only root user can read
chown root:root .env.production
chown root:root .env.staging
```

## Docker Compose Configuration

### Using Environment Variable Files
```yaml
version: '3.8'

services:
  clashsubmanager:
    image: clashsubmanager:latest
    container_name: clashsubmanager
    ports:
      - "8080:80"
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    env_file:
      - .env.production
    restart: unless-stopped
```

### Direct Setting in Compose
```yaml
version: '3.8'

services:
  clashsubmanager:
    image: clashsubmanager:latest
    container_name: clashsubmanager
    ports:
      - "8080:80"
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    environment:
      - AdminUsername=admin
      - AdminPassword=${AdminPassword}
      - CookieSecretKey=${CookieSecretKey}
      - SessionTimeoutMinutes=30
      - DataPath=/app/data
      - SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}
      - ASPNETCORE_ENVIRONMENT=Production
      - LOG_LEVEL=Information
    restart: unless-stopped
```

## Kubernetes Configuration

### Secret Configuration
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: clashsubmanager-secrets
type: Opaque
data:
  admin-username: YWRtaW4=  # admin (base64)
  admin-password: TXlTZWN1cmVQQHNzd29yZDIwMjQh  # MySecureP@ssw0rd2024! (base64)
  cookie-secret-key: MzJfY2hhcmFjdGVyX2xvbmdfc2VjcmV0X2tleQ==  # 32_character_long_secret_key (base64)
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: clashsubmanager-config
data:
  session-timeout-minutes: "30"
  aspnetcore-environment: "Production"
  log-level: "Information"
  data-path: "/app/data"
  subscription-url-template: "https://api.example.com/sub/{userId}"
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: clashsubmanager
spec:
  replicas: 1
  selector:
    matchLabels:
      app: clashsubmanager
  template:
    metadata:
      labels:
        app: clashsubmanager
    spec:
      containers:
      - name: clashsubmanager
        image: clashsubmanager:latest
        ports:
        - containerPort: 80
        env:
        - name: AdminUsername
          valueFrom:
            secretKeyRef:
              name: clashsubmanager-secrets
              key: admin-username
        - name: AdminPassword
          valueFrom:
            secretKeyRef:
              name: clashsubmanager-secrets
              key: admin-password
        - name: CookieSecretKey
          valueFrom:
            secretKeyRef:
              name: clashsubmanager-secrets
              key: cookie-secret-key
        - name: SessionTimeoutMinutes
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: session-timeout-minutes
        - name: DataPath
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: data-path
        - name: SUBSCRIPTION_URL_TEMPLATE
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: subscription-url-template
        - name: ASPNETCORE_ENVIRONMENT
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: aspnetcore-environment
        - name: LOG_LEVEL
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: log-level
```

## Configuration Validation

### 1. Validate Required Variables
```bash
#!/bin/bash
# Check required environment variables
required_vars=("AdminUsername" "AdminPassword" "CookieSecretKey" "SUBSCRIPTION_URL_TEMPLATE")

for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        echo "Error: Required environment variable $var is not set"
        exit 1
    fi
done

echo "✅ All required environment variables are set"
```

### 2. Validate Password Strength
```bash
#!/bin/bash
# Check password strength
password="$AdminPassword"

if [ ${#password} -lt 12 ]; then
    echo "Warning: Password length is less than 12 characters"
fi

if ! [[ "$password" =~ [A-Z] ]]; then
    echo "Warning: Password does not contain uppercase letters"
fi

if ! [[ "$password" =~ [a-z] ]]; then
    echo "Warning: Password does not contain lowercase letters"
fi

if ! [[ "$password" =~ [0-9] ]]; then
    echo "Warning: Password does not contain numbers"
fi

if ! [[ "$password" =~ [^a-zA-Z0-9] ]]; then
    echo "Warning: Password does not contain special characters"
fi
```

### 3. Validate Key Length
```bash
#!/bin/bash
# Check Cookie key length
key="$CookieSecretKey"

if [ ${#key} -lt 32 ]; then
    echo "Error: CookieSecretKey length must be at least 32 characters"
    exit 1
fi

echo "✅ Cookie key length validation passed"
```

## Troubleshooting

### 1. Environment Variables Not Taking Effect
```bash
# Check container environment variables
docker exec clashsubmanager env | grep ADMIN

# Restart container
docker restart clashsubmanager

# Check logs
docker logs clashsubmanager | grep -i error
```

### 2. Password Errors
```bash
# Regenerate password
NEW_PASSWORD=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-16)

# Update environment variables
docker stop clashsubmanager
docker run -d --name clashsubmanager-new \
  -e AdminUsername=admin \
  -e AdminPassword=$NEW_PASSWORD \
  -e CookieSecretKey=$CookieSecretKey \
  -e SessionTimeoutMinutes=30 \
  -e SUBSCRIPTION_URL_TEMPLATE=$SUBSCRIPTION_URL_TEMPLATE \
  clashsubmanager:latest

# Clean up old container
docker rm clashsubmanager
docker rename clashsubmanager-new clashsubmanager
```

### 3. Session Timeout Issues
```bash
# Check session timeout setting
docker exec clashsubmanager env | grep SESSION_TIMEOUT

# Adjust timeout
docker stop clashsubmanager
docker run -d --name clashsubmanager \
  -e SessionTimeoutMinutes=60 \
  clashsubmanager:latest
```

## Best Practices

1. **Use Environment Variable Files**: Store sensitive information in .env files, do not commit to version control
2. **Regular Key Rotation**: Recommend changing Cookie keys every 3-6 months
3. **Use Strong Passwords**: Admin passwords should include uppercase, lowercase, numbers, and special characters
4. **Principle of Least Privilege**: Adjust configurations according to environment, development environments can use more relaxed settings
5. **Monitor Configuration Changes**: Record all environment variable change history
6. **Backup Configuration**: Regularly backup environment variable configuration files