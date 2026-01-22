# Environment Variable Configuration Guide

**🌐 Language**: [English](env-config.md) | [中文](env-config-cn.md)

## Overview

ClashSubManager uses environment variables for system configuration, supporting flexible deployment and security management.

## Required Environment Variables

### ADMIN_USERNAME
- **Description**: Admin username
- **Type**: String
- **Default Value**: None (must be set)
- **Example**: `admin`
- **Requirements**: Non-empty string

```bash
ADMIN_USERNAME=admin
```

### ADMIN_PASSWORD
- **Description**: Admin password
- **Type**: String
- **Default Value**: None (must be set)
- **Example**: `MySecureP@ssw0rd2024!`
- **Requirements**: Non-empty string, strong password recommended

```bash
ADMIN_PASSWORD=MySecureP@ssw0rd2024!
```

### COOKIE_SECRET_KEY
- **Description**: Cookie signing key for HMACSHA256 signature
- **Type**: String
- **Default Value**: None (must be set)
- **Example**: `32_character_long_secret_key`
- **Requirements**: Random string of at least 32 characters

```bash
COOKIE_SECRET_KEY=32_character_long_secret_key
```

### SESSION_TIMEOUT_MINUTES
- **Description**: Session timeout (minutes)
- **Type**: Integer
- **Default Value**: 30
- **Example**: `60`
- **Requirements**: Integer between 5-1440

```bash
SESSION_TIMEOUT_MINUTES=30
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

### MAX_CONCURRENT_REQUESTS
- **Description**: Maximum concurrent requests
- **Type**: Integer
- **Default Value**: `50`
- **Requirements**: Integer between 10-100

```bash
MAX_CONCURRENT_REQUESTS=50
```

### REQUEST_RATE_LIMIT
- **Description**: Request rate limit (requests per IP per second)
- **Type**: Integer
- **Default Value**: `10`
- **Requirements**: Integer between 1-20

```bash
REQUEST_RATE_LIMIT=10
```

## Configuration Examples

### Development Environment Configuration
```bash
# .env.development
ADMIN_USERNAME=admin
ADMIN_PASSWORD=DevPass123!
COOKIE_SECRET_KEY=dev_secret_key_32_characters_long
SESSION_TIMEOUT_MINUTES=120
ASPNETCORE_ENVIRONMENT=Development
LOG_LEVEL=Debug
MAX_CONCURRENT_REQUESTS=20
REQUEST_RATE_LIMIT=5
```

### Production Environment Configuration
```bash
# .env.production
ADMIN_USERNAME=admin
ADMIN_PASSWORD=ProdSecureP@ssw0rd2024!
COOKIE_SECRET_KEY=$(openssl rand -hex 16)
SESSION_TIMEOUT_MINUTES=30
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
MAX_CONCURRENT_REQUESTS=50
REQUEST_RATE_LIMIT=10
```

### Testing Environment Configuration
```bash
# .env.testing
ADMIN_USERNAME=test_admin
ADMIN_PASSWORD=TestPass123!
COOKIE_SECRET_KEY=test_secret_key_32_characters_long
SESSION_TIMEOUT_MINUTES=60
ASPNETCORE_ENVIRONMENT=Staging
LOG_LEVEL=Warning
MAX_CONCURRENT_REQUESTS=30
REQUEST_RATE_LIMIT=8
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
      - ADMIN_USERNAME=admin
      - ADMIN_PASSWORD=${ADMIN_PASSWORD}
      - COOKIE_SECRET_KEY=${COOKIE_SECRET_KEY}
      - SESSION_TIMEOUT_MINUTES=30
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
  max-concurrent-requests: "50"
  request-rate-limit: "10"
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
        - name: ADMIN_USERNAME
          valueFrom:
            secretKeyRef:
              name: clashsubmanager-secrets
              key: admin-username
        - name: ADMIN_PASSWORD
          valueFrom:
            secretKeyRef:
              name: clashsubmanager-secrets
              key: admin-password
        - name: COOKIE_SECRET_KEY
          valueFrom:
            secretKeyRef:
              name: clashsubmanager-secrets
              key: cookie-secret-key
        - name: SESSION_TIMEOUT_MINUTES
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: session-timeout-minutes
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
        - name: MAX_CONCURRENT_REQUESTS
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: max-concurrent-requests
        - name: REQUEST_RATE_LIMIT
          valueFrom:
            configMapKeyRef:
              name: clashsubmanager-config
              key: request-rate-limit
```

## Configuration Validation

### 1. Validate Required Variables
```bash
#!/bin/bash
# Check required environment variables
required_vars=("ADMIN_USERNAME" "ADMIN_PASSWORD" "COOKIE_SECRET_KEY")

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
password="$ADMIN_PASSWORD"

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
key="$COOKIE_SECRET_KEY"

if [ ${#key} -lt 32 ]; then
    echo "Error: COOKIE_SECRET_KEY length must be at least 32 characters"
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
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=$NEW_PASSWORD \
  -e COOKIE_SECRET_KEY=$COOKIE_SECRET_KEY \
  -e SESSION_TIMEOUT_MINUTES=30 \
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
  -e SESSION_TIMEOUT_MINUTES=60 \
  clashsubmanager:latest
```

## Best Practices

1. **Use Environment Variable Files**: Store sensitive information in .env files, do not commit to version control
2. **Regular Key Rotation**: Recommend changing Cookie keys every 3-6 months
3. **Use Strong Passwords**: Admin passwords should include uppercase, lowercase, numbers, and special characters
4. **Principle of Least Privilege**: Adjust configurations according to environment, development environments can use more relaxed settings
5. **Monitor Configuration Changes**: Record all environment variable change history
6. **Backup Configuration**: Regularly backup environment variable configuration files