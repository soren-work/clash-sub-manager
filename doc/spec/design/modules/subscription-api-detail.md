# User Subscription Interface MVP Detailed Design

**🌐 Language**: [English](subscription-api-detail.md) | [中文](subscription-api-detail-cn.md)

## 1. MVP Core Functions

### 1.1 Core Value Validation Points
- Unified subscription entry: `/sub/[id]` format
- Configuration assembly: Dynamically merge original subscription, optimized IPs, Clash template
- Completely dynamic processing: Strictly prohibit hardcoding field names or structures

### 1.2 Minimum Feature Set
- **GET /sub/[id]**: Get user Clash subscription configuration
- **POST /sub/[id]**: Update user optimized IP configuration
- **DELETE /sub/[id]**: Delete user optimized IP configuration
- **User ID validation**: Validate user effectiveness through original subscription address

### 1.3 Technical Constraints
- **.NET 10**: Main development framework
- **ASP.NET Core Razor Pages**: Web development pattern
- **Bootstrap**: Frontend framework
- **Monolithic application architecture**: Strictly prohibit frontend-backend separation
- **File storage**: Only use local file system

## 2. Core Interfaces and Processing Flow

### 2.1 User Subscription Interface
- **GET /sub/{id}**: Get user Clash subscription configuration
- **POST /sub/{id}**: Update user optimized IP configuration
- **DELETE /sub/{id}**: Delete user optimized IP configuration

### 2.2 GET /sub/[id] Processing Flow
1. **User validation**: Request real subscription address with Clash User-Agent to validate ID
2. **Get subscription**: Call subscription service API to get user's original subscription data
3. **Configuration loading**: Load user-specific → default configuration files by priority
4. **Dynamic merging**: Completely dynamically parse and merge all fields (template priority)
5. **IP extension**: Perform IP address extension based on proxies
6. **Fallback mechanism**: Return subscription service original data directly when no configuration
7. **Return YAML**: Content-Type: text/yaml

### 2.3 User ID Validation Mechanism
- Request `GET [real subscription address]/[user id]` with clash user-agent
- Validation successful: Return yaml document and HTTP status code is 200
- Validation failed: Return other content means user ID is wrong

### 2.4 Data Overwrite Scope
1. **proxies extension**: Original `proxies` `server` attribute is domain name, copy objects equal to the number of IPs in `cloudflare-ip.csv`, and change server to IP addresses
2. **yaml structure extension**: Read `clash.yaml` template, prioritize template content, add and replace to original content

## 3. Compatibility and Performance Requirements

### 3.1 Complete Dynamic Parsing Principle
- **Strictly prohibit hardcoding**: Do not preset any field names or structures in code
- **Field integrity**: Completely preserve all fields from subscription service and template files
- **Future compatibility**: Automatically support any fields added in future Clash versions
- **Dynamic merging strategy**: Template fields take priority over subscription fields, override for same fields

### 3.2 Compatibility Implementation Requirements
- **Complete field preservation**: All fields returned by subscription service must be completely preserved to final output
- **Template field injection**: All fields in clash.yaml must be injected according to original structure
- **Field conflict handling**: For same field names, template fields override subscription fields
- **No field restrictions**: Do not limit, filter, or preset process any configuration fields

### 3.3 Performance Constraints
- **Response time**: <100ms (low concurrency scenarios)
- **Concurrent processing**: 10-50 requests/second
- **Memory usage**: <50MB (resident memory)
- **Startup time**: <30 seconds
- **File processing**: CSV max 10MB, YAML max 1MB

### 3.4 Security Constraints
- **Request limiting**: Maximum 10 requests per IP per second
- **Input validation**: Strict data format validation
- **File operations**: Exclusive write, atomic replacement

## 4. Response and Data Formats

### 4.1 Standard Response Format
- **Successful POST/DELETE**: `Content-Type: application/json`
  ```json
  {"success": true, "message": "Operation description"}
  ```
- **Error response**: `Content-Type: application/json`
  ```json
  {"success": false, "message": "Operation description"}
  ```

### 4.2 Special Responses
- **GET /sub/[id]**: `Content-Type: text/yaml`, return complete YAML configuration

### 4.3 Standard Error Codes
- `400`: Request parameter error
- `401`: User ID validation failed
- `403`: Insufficient permissions (admin functions)
- `404`: User or resource does not exist
- `429`: Request rate limit exceeded
- `500`: Server internal error

### 4.4 Data Storage Structure
```
/app/data/
├── cloudflare-ip.csv     # Default optimized IPs
├── clash.yaml           # Default Clash template
├── users.txt            # User ID records (auto-deduplication)
└── [userId]/            # User-specific configuration
    ├── cloudflare-ip.csv
    └── clash.yaml
```

**Configuration Simplification Description:**
- Remove complex UserConfig JSON configuration file
- User-specific IP configuration read directly from CSV file
- User management simplified to ID recording and deduplication

### 4.5 Data Validation Rules
- **User ID**: 1-64 characters, support [a-zA-Z0-9_-]
- **IP address submission**: Maximum 1 IP per line, at least 1 IP per submission, regex validation
- **CSV file**: Maximum 10MB, support CloudflareST format or pure IP format
- **YAML template**: Maximum 1MB, no field restrictions
- **Concurrency limit**: 10 requests per IP per second

### 4.6 Configuration Priority
1. User-specific configuration (priority)
2. Default configuration (fallback)
3. Original subscription (base)

## 5. MVP Implementation Points

### 5.1 AI Agent Development Scope
- User subscription interface implementation (GET/POST/DELETE /sub/[id])
- Complete dynamic YAML field parsing and merging
- User ID validation mechanism
- File storage operations (CSV/YAML read/write)
- Subscription service integration (HTTP requests)
- User ID management (users.txt recording and deduplication)
- Environment variable configuration management (subscription URL template)

### 5.2 Key Technical Constraints
- **Completely dynamic processing**: Dynamically parse all fields, strictly prohibit hardcoding field names or structures
- **Field integrity guarantee**: Completely preserve all fields from subscription service and template files
- **Function length**: Not exceeding 50 lines
- **Nesting limit**: Maximum 3 layers
- **Monolithic architecture**: Strictly prohibit frontend-backend separation

### 5.3 Error Handling Strategy
- **Unified format**: All exceptions return standard JSON format
- **Key errors**: User validation failure, subscription retrieval failure, file operation failure
- **Standard error codes**: 400/401/403/404/429/500

### 5.4 MVP Acceptance Criteria
- User subscription interface works normally
- Complete dynamic field parsing and merging
- Meet performance constraint conditions
- File operations are concurrency-safe
