# ClashSubManager MVP Technical Requirements Specification

**🌐 Language**: [English](server-requirements.md) | [中文](server-requirements-cn.md)

## 1. MVP Core Functions

### 1.1 User Subscription Interface
- **GET /sub/[id]**: Return complete Clash YAML configuration
- **POST /sub/[id]**: Update user-specific optimized IPs (CSV format)
- **DELETE /sub/[id]**: Delete user-specific optimized IPs
- **Validation Mechanism**: All operations must validate user ID through subscription service first

### 1.2 Admin Functions
- Authentication system: Admin login based on Docker environment variables
- Default optimized IP management: Visual interface to manage `/app/data/cloudflare-ip.csv`
- Clash template management: Visual interface to manage `/app/data/clash.yaml`
- User configuration management: View/delete user-specific configurations
- **Smart data rendering**: Parse data according to CloudflareST format, display only IP addresses when parsing fails

### 1.3 Explicitly Excluded Functions
- User registration system
- Complex permission management
- Database storage
- Distributed architecture
- Advanced statistical analysis

## 2. Technical Architecture

### 2.1 Core Architecture
- **.NET 10 + ASP.NET Core Razor Pages**
- **Monolithic application architecture**: Strictly prohibit frontend-backend separation
- **Docker containerized deployment**

### 2.2 Storage Structure
```
/app/data/
├── cloudflare-ip.csv          # Default optimized IPs
├── clash.yaml                 # Default Clash template
├── users.txt                  # User records
└── [user id]/
    ├── cloudflare-ip.csv      # User-specific optimized IPs
    └── clash.yaml             # User-specific template
```

## 3. Implementation Boundaries

### 3.1 AI Agent Development Scope
- User subscription interface (GET/POST/DELETE /sub/[id])
- Admin authentication system (Cookie session management)
- Admin interface (Razor Pages)
- File operations (CSV/YAML read/write)
- Subscription service integration (HTTP requests)

### 3.2 Technical Constraint Conditions
- **.NET 10 + ASP.NET Core Razor Pages**
- **Bootstrap frontend framework**
- **Monolithic application architecture**: Strictly prohibit frontend-backend separation
- **File storage**: Only use local file system
- **Deployment method**: Docker containerization

### 3.3 Acceptance Criteria
- API response time < 100ms
- Memory usage < 50MB
- Support 10-50 concurrent requests/second
- Complete admin interface functionality
- File operations concurrency-safe

## 4. Core Interface Specifications

### 4.1 User Subscription Interface
```
GET /sub/[id]     # Get subscription (return YAML)
POST /sub/[id]    # Update optimized IPs (receive CSV)
DELETE /sub/[id]  # Delete optimized IPs
```

### 4.2 GET /sub/[id] Processing Flow
1. **Validate user ID**: Call subscription service with clash user-agent to validate ID
2. **Get base subscription**: Call subscription service API to get user's original subscription
3. **Configuration priority**: User-specific configuration > Default configuration > Original data
4. **Optimized IP processing**: Extend proxies section to Cloudflare optimized IPs
5. **Template injection**: Inject proxy-groups, rules, dns strategies
6. **Fallback mechanism**: Return subscription service original data directly when no configuration
7. **Return YAML**: Content-Type: text/yaml

### 4.3 Data Overwrite Compatibility Requirements
**Core Principle**: Provide completely flexible compatibility processing for original subscription service data, strictly prohibit hardcoded field processing.

#### 4.3.1 Dynamic Field Parsing
- **clash.yaml template fields**: Completely include all fields in template file, dynamically parse and inject
- **subscription service returned fields**: Completely preserve all fields returned by subscription service, no omissions
- **Field merging strategy**: Template fields take priority over subscription fields, template overrides subscription for same fields

#### 4.3.2 Strictly Prohibit Hardcoded Processing
- **Prohibit field guessing**: Do not preset or guess any field names or structures in code
- **Prohibit field restrictions**: Do not limit or filter any configuration fields
- **Dynamic parsing mechanism**: Must use dynamic parsing mechanism to handle all unknown fields

#### 4.3.3 Compatibility Implementation Requirements
- **Complete field preservation**: All fields returned by subscription service must be completely preserved to final output
- **Template field injection**: All fields in clash.yaml must be injected according to original structure
- **Field conflict handling**: For same field names, template fields override subscription fields
- **New field support**: Automatically support any fields added in future clash versions

### 4.4 POST /sub/[id] Data Format
- **Input format**: Free text format, support CloudflareST output or other formats
- **Validation rules**: 
  - Maximum 1 IP address per line
  - At least 1 IP address per submission
  - Use regex to extract and validate IP addresses
- **Content-Type**: text/csv or text/plain
- **Storage strategy**: 
  - Completely save original submitted data to `/app/data/[user id]/cloudflare-ip.csv`
  - Management interface smart parsing: Parse CloudflareST format data line by line
  - Display only IP addresses when parsing fails, display complete data when parsing succeeds

### 4.5 Response Format
- **Successful GET**: `Content-Type: text/yaml`, return complete YAML configuration
- **Successful POST/DELETE**: `Content-Type: application/json`
```json
{"success": true, "message": "Operation description"}
```
- **Error**: `Content-Type: application/json`
```json
{"success": false, "message": "Operation description"}
```

#### Standard Error Codes
- `400`: Request parameter error
- `401`: User ID validation failed
- `403`: Insufficient permissions (admin functions)
- `404`: User or resource does not exist
- `429`: Request rate limit exceeded
- `500`: Server internal error

### 4.6 Data Validation
- User ID: 1-64 characters, support `[a-zA-Z0-9_-]`
- IP address submission: Maximum 1 IP per line, at least 1 IP per submission, regex validation
- Text file: Maximum 10MB, 1000 lines
- YAML template: Maximum 1MB
- Concurrency limit: 10 requests per IP per second

## 5. Key Implementation Points

### 5.1 File Operation Security
- Use `FileStream`+`FileShare.None` exclusive write
- Atomic write: Temporary file + replacement mode
- Concurrency safety: Avoid write conflicts

### 5.2 Memory Optimization
- Use `ArrayPool<T>` and `MemoryPool<T>`
- Configure `GCSettings.LatencyMode` to `LowLatency`
- Disable unnecessary ASP.NET Core services
- Regularly release large objects and file streams

### 5.3 Security Requirements
- Admin authentication: Docker environment variable configuration
- Cookie security: HttpOnly, Secure, SameSite=Strict
- Session timeout: Configurable 5-1440 minutes
- Input validation: Strict data format validation
