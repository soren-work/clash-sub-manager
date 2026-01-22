# ClashSubManager Technical Implementation Boundary Specification

**🌐 Language**: [English](mvp-boundary.md) | [中文](mvp-boundary-cn.md)

## 1. Core Function Module Boundaries

### 1.1 User Subscription Interface Module

**Must Implement Functions:**
- `GET /sub/[id]`: Get complete Clash subscription configuration
- `POST /sub/[id]`: Submit CloudflareST results, create/update user-specific optimized IP file
- `DELETE /sub/[id]`: Delete user-specific optimized IP file
- User ID validation: Validate user effectiveness through original subscription URL (using Clash User-Agent request)
- Dynamic configuration merging: Original subscription + optimized IPs + Clash template
- File storage: User-specific configuration and default configuration management
- Configuration priority: User-specific configuration takes priority over default configuration
- **Completely Dynamic Processing**: Dynamically parse all fields, strictly prohibit hardcoding field names or structures
- **Field Integrity Guarantee**: Completely preserve all fields from subscription service and template files

**Prohibited Functions:**
- User registration/creation, password modification, subscription payment
- API permission control, fine-grained permission management

### 1.2 Admin Authentication and Permission Module

**Must Implement Functions:**
- Environment variable authentication: Configure admin through Docker environment variables
- Cookie session management: Secure session state management
- Login/logout: Basic authentication operations
- Permission middleware: Management interface access control
- Session timeout: Automatic session expiration mechanism
- File management interface: Manage cloudflare-ip.csv and clash.yaml files
- User-specific configuration management: Support managing configuration files under /app/data/[user id]/

**Prohibited Functions:**
- Multiple admin accounts, role permission management, OAuth integration
- API key management, audit logs

### 1.3 Default Optimized IP Management Module

**Must Implement Functions:**
- Unified IP management interface: Admin can manage global and user-specific IP configurations
- User selector: Support selecting global or specific users for operations
- CSV content management: Support direct paste of CSV content or file upload, directly overwrite file
- IP list display: Display optimized IPs and speed test data in table format, missing data shows "No Data"
- IP configuration deletion: Delete entire CSV file (global or user-specific)
- Real-time effect: Changes immediately affect user subscription generation
- File path management: Global (/app/data/cloudflare-ip.csv) and user-specific (/app/data/[user id]/cloudflare-ip.csv)
- CSV parsing processing: Parse according to result.csv format, support automatic header skipping
- CloudflareST support: Support user script submission of CloudflareST program running results

**Prohibited Functions:**
- IP speed testing, automatic IP filtering, geographic location analysis, performance monitoring

### 1.4 Clash Template Management Module

**Must Implement Functions:**
- Template content editing: Support completely dynamic YAML template editing
- Template file management: Global and user-specific template management
- Template deletion: Delete global or user-specific templates
- Dynamic field processing: Completely preserve all fields in template, no field restrictions

**Prohibited Functions:**
- Template marketplace, visual editor, template version control, template synchronization

### 1.5 User-Specific Configuration Management Module

**Must Implement Functions:**
- Unified configuration management interface: administrators can manage user-specific IP and template configurations
- User selector: support selecting specific users to view and manage configurations
- User list display: show all accessed user IDs for selection (read from users.txt)
- Dedicated IP configuration viewing: view user's optimized IP configuration and statistics
- Dedicated template configuration viewing: view user's Clash template configuration
- Configuration modification: modify user-specific IP and template configurations
- Configuration deletion: delete user configuration files (IP or template)
- Configuration priority display: clearly show dedicated configurations take priority over default configurations
- Configuration status display: show whether user has configured dedicated IPs and templates

**Prohibited Functions:**
- User information editing, batch user operations, user grouping, user search
- Subscription URL management (moved to environment variable configuration)

## 2. Technology Stack Constraints

### 2.1 Allowed Technologies
**Core Technologies:**
- .NET 10: Main development framework
- ASP.NET Core Razor Pages: Web development pattern
- Docker: Containerized deployment
- YAML: Configuration file format
- CSV: Data storage format

**Auxiliary Technologies:**
- Bootstrap: Frontend UI framework
- JavaScript: Frontend interaction logic
- HttpClient: Network requests
- File I/O: File system operations
- Environment Variables: Configuration management

### 2.2 Strictly Prohibited Technologies
**Prohibited Technologies:**
- Database systems: MySQL, PostgreSQL, etc.
- Frontend frameworks: React, Vue, Angular
- Microservice architecture, message queues: Redis, RabbitMQ, etc.
- Search engines: Elasticsearch, etc.

**Prohibited Architecture Patterns:**
- Frontend-backend separation, microservices, event-driven
- Complex architecture patterns like CQRS, DDD

### 2.3 Extensibility Constraints
**Allowed Extension Points:**
- Configuration format extension: Can support new configuration file formats
- Validation rule extension: Can add new data validation rules
- UI theme extension: Can support different UI themes
- Log format extension: Can support different log output formats

**Strictly Prohibited Extension Points:**
- Data storage extension: Strictly prohibit extension to database storage
- Architecture pattern extension: Strictly prohibit changing monolithic architecture
- Communication protocol extension: Strictly prohibit adding gRPC, WebSocket, etc.
- Deployment mode extension: Strictly prohibit supporting complex deployments like Kubernetes

## 3. Data Processing Constraints

### 3.1 Supported Data Formats
**File Formats:**
- YAML files: Clash configuration templates (.yaml/.yml)
- CSV files: Optimized IP data (.csv)
- Text files: User ID lists (.txt)
- JSON format: API response format

**Data Structure Limitations:**
- User ID length: 1-64 characters
- CSV file size: Maximum 10MB
- YAML file size: Maximum 1MB
- IP address validation: Use regex to extract and validate IP addresses
- CSV submission requirements: Maximum 1 IP address per line, at least 1 IP address per submission

### 3.2 Strictly Prohibited Data Processing
**Prohibited Data Operations:**
- Database queries, file system traversal, web crawling
- Data mining, data trading

**Prohibited Data Formats:**
- Binary files, compressed files, encrypted files, multimedia files

## 4. Security Constraints

### 4.1 Allowed Security Measures
**Basic Security Functions:**
- Environment variable authentication: Admin credential configuration
- Cookie session management: Secure session handling

### 4.2 Strictly Prohibited Security Functions
**Prohibited Security Features:**
- Complex permission systems: RBAC and other complex permissions
- API key management, two-factor authentication, audit logs, intrusion detection

## 5. Performance Requirements

### 5.1 Must Meet Performance Indicators
- Response time: <100ms (low concurrency scenarios)
- Concurrent processing: 10-50 requests/second
- Memory usage: <50MB (resident memory)
- Startup time: <30 seconds
- File processing: Support maximum 10MB files
- **Completely Dynamic Processing**: Support completely dynamic YAML field parsing and merging, strictly prohibit hardcoding any field names or structures

### 5.2 Performance Indicators Not Pursued
- High concurrency processing: Do not pursue 1000+ concurrency
- Low latency optimization: Do not pursue millisecond-level latency optimization
- Large-scale data processing: Do not support GB-level data processing
- Distributed performance, complex caching strategies

## 6. Compatibility Constraints

### 6.1 Supported Compatibility
**Platform Compatibility:**
- Operating systems: Linux, Windows (via Docker)
- Browsers: Modern browsers (Chrome, Firefox, Safari, Edge)
- Clash versions: Support standard Clash configuration format
- Docker versions: Support mainstream Docker versions

**Data Compatibility:**
- Clash configuration: Standard YAML format
- CSV format: Standard comma-separated values
- HTTP protocol: Standard HTTP/HTTPS
- JSON format: Standard JSON data format

### 6.2 Unsupported Compatibility
- Legacy browsers: IE and other old browsers
- Mobile adaptation: MVP does not specifically adapt for mobile
- Other proxy tools: Do not support other proxy configuration formats
- Database compatibility, cloud platform integration

## 7. Deployment Constraints

### 7.1 Supported Deployment Methods
- Docker containers: Standard Docker deployment
- Environment variable configuration: Configure through environment variables
- Single machine deployment: Single machine container deployment
- Data volume mapping: External data directory mapping

### 7.2 Unsupported Deployment Methods
- Kubernetes, cluster deployment, cloud platform specific
- Microservice deployment, stateful services

## 8. API Interface Specifications

### 8.1 Core API Interfaces
- `GET /sub/[user id]` - Get overwritten subscription data
- `POST /sub/[user id]` - Submit CloudflareST results
- `DELETE /sub/[user id]` - Delete user-specific IP file
- Admin login and management interfaces

### 8.2 Response Format Specifications
**Standard Response Format:**
- Success response: `{"success": true, "message": "Operation description"}`
- Error response: `{"success": false, "message": "Operation description"}`

**Special Responses:**
- `GET /sub/[id]`: Return complete YAML configuration (Content-Type: text/yaml)

**Standard Error Codes:**
- `400`: Request parameter error
- `401`: User ID validation failed
- `403`: Insufficient permissions (admin functions)
- `404`: User or resource does not exist
- `429`: Request rate limit exceeded
- `500`: Server internal error

### 8.3 Data Format Specifications
**POST /sub/[id] Request Format:**
- Content-Type: `text/csv` or `text/plain`
- Data Format: CSV format or free text format, support CloudflareST output
- Validation Rules: Maximum 1 IP address per line, at least 1 IP address per submission

### 8.4 System Configuration Specifications
**Subscription URL Configuration:**
- Environment Variable: `SUBSCRIPTION_URL_TEMPLATE`
- Support URL Templates: path parameters, query parameters, fixed URLs
- Automatic Replacement: `{userId}` placeholder replaced with actual user ID

**User Management:**
- User Record File: `/app/data/users.txt`
- Automatic Deduplication: automatically record user ID on first access
- Simplified Management: only record user IDs, no complex configurations

### 8.4 Core Processing Logic
**GET /sub/[user id] Processing Flow:**
1. Request `GET [real subscription address]/[user id]` with clash user-agent
2. Validation successful: Return yaml document and HTTP status code is 200
3. Validation failed: Return other content means user ID is wrong
4. Fallback strategy: When neither user-specific nor default configuration exists, directly return original data from clash subscription interface

**Overwrite Scope:**
1. **Extension of proxies**: Original `proxies` `server` attribute is domain name, copy objects equal to the number of IP addresses in `cloudflare-ip.csv` file, and change server to IP addresses in the file
2. **Extension of yaml document structure**: Read templates and settings defined in `clash.yaml` file, prioritize content in `clash.yaml` file over original content, add and replace content from `clash.yaml` file into original content

**Compatibility Requirements:**
- **Completely Dynamic Processing**: Must dynamically parse and process all fields from clash.yaml template and subscription service return, strictly prohibit hardcoding any field names or structures
- **Field Integrity Guarantee**: Completely preserve all fields returned by subscription service, while completely including all fields from clash.yaml template
- **No Field Restrictions**: Do not limit, filter, or preset process any configuration fields

### 8.5 Priority Application Rules
1. User-specific files take priority over global files
2. clash.yaml template content takes priority over original subscription content
3. IP extension based on existing proxies copying and modification
4. **Dynamic Field Processing**: All fields must be dynamically parsed, strictly prohibit hardcoding field names or structures
5. **Complete Field Preservation**: All fields from subscription service and template files must be completely preserved to final output
