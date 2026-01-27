# ClashSubManager Environment Variables Configuration

## Overview

ClashSubManager is configured through environment variables, especially in Docker containerized deployment environments. All environment variables have reasonable default values, but it is recommended to explicitly set them in production environments.

## Authentication Configuration

### ADMIN_USERNAME
**Description**: Administrator username  
**Type**: String  
**Default**: admin  
**Required**: No  
**Example**: `ADMIN_USERNAME=myadmin`

### ADMIN_PASSWORD
**Description**: Administrator password  
**Type**: String  
**Default**: (no default value)  
**Required**: Yes  
**Example**: `ADMIN_PASSWORD=SecurePassword123!`

### COOKIE_SECRET_KEY
**Description**: Cookie signing key for HMACSHA256 signature  
**Type**: String  
**Default**: default-key  
**Required**: No (strongly recommended in production)  
**Constraint**: Length ≥ 32 characters  
**Example**: `COOKIE_SECRET_KEY=your-very-long-and-secure-secret-key-for-hmac-signing`

### SESSION_TIMEOUT_MINUTES
**Description**: Session timeout duration (minutes)  
**Type**: Integer  
**Default**: 30  
**Constraint**: 5-1440 minutes  
**Example**: `SESSION_TIMEOUT_MINUTES=60`

### SUBSCRIPTION_URL_TEMPLATE
**Description**: Upstream subscription URL template (must contain `{userId}` for subscription interface user ID validation and subscription generation)  
**Type**: String  
**Default**: (no default value)  
**Required**: Yes (required for subscription interface)  
**Example**: `SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}`

## System Configuration

### DATA_PATH
**Description**: Data storage directory path  
**Type**: String  
**Default**: /app/data  
**Required**: No  
**Example**: `DATA_PATH=/custom/data/path`

## Docker Compose Configuration Example

```yaml
version: '3.8'
services:
  clash-sub-manager:
    build: .
    ports:
      - "80:80"
    environment:
      - ADMIN_USERNAME=admin
      - ADMIN_PASSWORD=your_secure_password_here
      - COOKIE_SECRET_KEY=your_hmac_key_at_least_32_chars_long
      - SESSION_TIMEOUT_MINUTES=30
      - DATA_PATH=/app/data
      - SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    restart: always
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

## Docker Run Command Example

```bash
docker run -d \
  --name clash-sub-manager \
  -p 80:80 \
  -e ADMIN_USERNAME=admin \
  -e ADMIN_PASSWORD=SecurePassword123! \
  -e COOKIE_SECRET_KEY=your-very-long-and-secure-secret-key-for-hmac-signing \
  -e SESSION_TIMEOUT_MINUTES=30 \
  -e DATA_PATH=/app/data \
  -e SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId} \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/logs:/app/logs \
  --restart always \
  clash-sub-manager:latest
```

## Security Considerations

### 1. Password Security
- `ADMIN_PASSWORD` cannot be empty, strong password recommended
- Password should contain uppercase and lowercase letters, numbers, and special characters
- Change administrator password regularly

### 2. Cookie Key Security
- `COOKIE_SECRET_KEY` length must be ≥ 32 characters
- Use randomly generated long string as the key
- Do not expose the key in code or version control

### 3. Session Management
- Set `SESSION_TIMEOUT_MINUTES` according to security requirements
- Shorter timeout recommended for sensitive environments
- Timeout range: 5-1440 minutes

## Environment Variable Validation

The system validates the following environment variables at startup:

1. **ADMIN_PASSWORD**: Cannot be empty
2. **COOKIE_SECRET_KEY**: Length ≥ 32 characters (if set)
3. **SESSION_TIMEOUT_MINUTES**: Within 5-1440 minutes range

If validation fails, the system will log a warning and use default values.

## Troubleshooting

### Issue: Cannot login to admin interface
**Possible Causes**:
- `ADMIN_USERNAME` or `ADMIN_PASSWORD` set incorrectly
- Environment variables not properly passed to container

**Solution**:
```bash
# Check environment variables
docker exec clash-sub-manager env | grep ADMIN

# View container logs
docker logs clash-sub-manager
```

### Issue: Session expires immediately
**Possible Causes**:
- `COOKIE_SECRET_KEY` length less than 32 characters
- Key changes after container restart

**Solution**:
- Ensure key length ≥ 32 characters
- Set key permanently in Docker configuration

### Issue: Data persistence failure
**Possible Causes**:
- `DATA_PATH` set incorrectly
- Docker volume mount configuration issue

**Solution**:
- Check data directory permissions
- Verify Docker volume mount configuration

## Production Environment Recommendations

1. **Use Strong Password**: Administrator password should contain uppercase and lowercase letters, numbers, and special characters
2. **Set Long Key**: Use 64-character random string for cookie key
3. **Reasonable Timeout**: Set session timeout to 30-60 minutes
4. **Regular Updates**: Change passwords and keys regularly
5. **Monitor Logs**: Pay attention to login failures and abnormal access logs

## Development Environment Configuration

Development environment can use simplified configuration:

```bash
ADMIN_USERNAME=admin
ADMIN_PASSWORD=dev123
COOKIE_SECRET_KEY=dev-secret-key-for-development-only
SESSION_TIMEOUT_MINUTES=120
SUBSCRIPTION_URL_TEMPLATE=https://api.example.com/sub/{userId}
```

**Note**: Development environment configuration is for development and testing only. Production environment must use secure configuration.
