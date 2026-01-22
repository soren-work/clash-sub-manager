# ClashSubManager MVP Outline Design

**🌐 Language**: [English](mvp-outline.md) | [中文](mvp-outline-cn.md)

## 1. MVP Core Functions

### 1.1 Core Value Validation Points
- Unified subscription entry: `/sub/[id]` format
- Configuration assembly: Dynamically merge original subscription, optimized IPs, Clash template
- Personalization support: User-specific configuration and default configuration switching
- Lightweight architecture: Monolithic application, minimal resource usage

### 1.2 Minimum Feature Set
- User subscription interface (GET/POST/DELETE /sub/[id])
- Admin authentication (environment variables + Cookie)
- Optimized IP management (CSV format)
- Clash template management (YAML format)
- User-specific configuration management
- **Internationalization support**: English/Chinese language switching

### 1.3 Explicitly Excluded Functions
- User management system
- Billing functions
- Complex permission system
- Database storage
- Microservice architecture

## 2. Technical Architecture

### 2.1 Core Architecture Diagram
```
ClashSubManager/
├── server/                  # Server-side application
│   ├── ClashSubManager.csproj
│   ├── Program.cs
│   ├── Pages/               # Razor Pages
│   ├── Services/            # Business services
│   ├── Models/              # Data models
│   └── wwwroot/             # Static resources
└── doc/                     # Documentation directory
```

### 2.2 Technology Selection (MVP related only)
- **.NET 10**: Main development framework
- **ASP.NET Core Razor Pages**: Web development pattern
- **Bootstrap**: Frontend framework
- **Docker**: Containerized deployment
- **ASP.NET Core Localization**: Internationalization support

### 2.3 Deployment Solution (Simplified)
- Single Docker container deployment
- Environment variable configuration for admin account
- Data persistence to `/app/data` directory

## 3. Implementation Boundaries

### 3.1 AI Agent Development Scope
- User subscription interface implementation
- Admin authentication middleware
- Optimized IP management interface
- Clash template management interface
- User-specific configuration management

### 3.2 Technical Constraint Conditions
- Strictly prohibit frontend-backend separation architecture
- Only use local file storage
- Function length not exceeding 50 lines
- Nesting limit maximum 3 layers
- Resident memory <50MB
- Response time <100ms
- Concurrent processing: 10-50 requests/second

### 3.3 Data Processing Compatibility Requirements
#### 3.3.1 Complete Dynamic Parsing Principle
- **Strictly prohibit hardcoding**: Do not preset any field names or structures in code
- **Field integrity**: Completely preserve all fields from subscription service and template files
- **Future compatibility**: Automatically support any fields added in future Clash versions
- **Dynamic merging strategy**: Template fields take priority over subscription fields, override for same fields

#### 3.3.2 Compatibility Implementation Requirements
- **Complete field preservation**: All fields returned by subscription service must be completely preserved to final output
- **Template field injection**: All fields in clash.yaml must be injected according to original structure
- **Field conflict handling**: For same field names, template fields override subscription fields
- **No field restrictions**: Do not limit, filter, or preset process any configuration fields

### 3.4 Performance and Security Constraints
#### 3.4.1 Performance Requirements
- Response time: <100ms (low concurrency scenarios)
- Concurrent processing: 10-50 requests/second
- Resident memory: <50MB
- File processing: CSV max 10MB, YAML max 1MB
- Startup time: <30 seconds

#### 3.4.2 Security Requirements
- Cookie security: HttpOnly, Secure, SameSite=Strict
- Session timeout: Configurable 5-1440 minutes
- Request limiting: Maximum 10 requests per IP per second
- Input validation: Strict data format validation
- Admin authentication: Docker environment variable configuration

#### 3.4.3 Internationalization Requirements
- **Default language**: English
- **Supported languages**: English, Chinese (Simplified)
- **Language detection**: Automatic browser language detection
- **Language switching**: Manual language toggle in admin interface
- **Content scope**: All admin interfaces support i18n
- **Exclusions**: Console logs, code comments, system messages use English only

### 3.3 Acceptance Criteria
- User subscription interface works normally
- Admin authentication function works normally
- Configuration file CRUD operations work normally
- Meet performance constraint conditions
- **Language switching works correctly**: Default English, auto-detect Chinese, manual toggle functional
- **All admin interfaces support both languages**: Complete UI localization coverage

## 4. Core Interface Definitions

### 4.1 User Subscription Interface
- **GET /sub/{id}**: Get user Clash subscription configuration
- **POST /sub/{id}**: Update user optimized IP configuration
- **DELETE /sub/{id}**: Delete user optimized IP configuration

### 4.2 GET /sub/[id] Processing Flow
1. **User validation**: Request real subscription address with Clash User-Agent to validate ID
2. **Get subscription**: Call subscription service API to get user's original subscription data
3. **Configuration loading**: Load user-specific → default configuration files by priority
4. **Dynamic merging**: Completely dynamically parse and merge all fields (template priority)
5. **IP extension**: Perform IP address extension based on proxies
6. **Fallback mechanism**: Return subscription service original data directly when no configuration
7. **Return YAML**: Content-Type: text/yaml

#### 4.2.1 User ID Validation Mechanism
- Request `GET [real subscription address]/[user id]` with clash user-agent
- Validation successful: Return yaml document and HTTP status code is 200
- Validation failed: Return other content means user ID is wrong

#### 4.2.2 Data Overwrite Scope
1. **proxies extension**: Original `proxies` `server` attribute is domain name, copy objects equal to the number of IPs in `cloudflare-ip.csv`, and change server to IP addresses
2. **yaml structure extension**: Read `clash.yaml` template, prioritize template content, add and replace to original content

### 4.3 Management Interface Routes
- `/admin/login` - Login page
- `/admin` - Management panel homepage
- `/admin/default-ips` - Optimized IP management
- `/admin/clash-templates` - Template management
- `/admin/users/config` - User configuration management

### 4.3.1 Internationalization Implementation
#### Language Detection Mechanism
```csharp
// Automatic browser language detection
var language = Request.GetBrowserLanguage();
if (language.StartsWith("zh")) {
    CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
} else {
    CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");
}
```

#### Language Switching Interface
- Language toggle button in admin header
- Cookie-based language preference storage
- Immediate UI refresh without page reload

#### Resource File Structure
```
Resources/
├── Pages/
│   ├── Admin/
│   │   ├── Index.en.resx
│   │   ├── Index.zh-CN.resx
│   │   ├── Login.en.resx
│   │   └── Login.zh-CN.resx
│   └── Shared/
│       ├── _Layout.en.resx
│       └── _Layout.zh-CN.resx
```

#### Supported Languages Scope
- **Full support**: All admin interface pages (login, dashboard, IP management, template management, user config)
- **Partial support**: Error messages, validation messages, tooltips
- **No support**: Console logs, code comments, system internal messages (English only)

### 4.4 Response Format Specifications
#### 4.4.1 Standard Response Format
- **Successful POST/DELETE**: `Content-Type: application/json`
  ```json
  {"success": true, "message": "Operation description"}
  ```
- **Error response**: `Content-Type: application/json`
  ```json
  {"success": false, "message": "Operation description"}
  ```

#### 4.4.2 Special Responses
- **GET /sub/[id]**: `Content-Type: text/yaml`, return complete YAML configuration

#### 4.4.3 Standard Error Codes
- `400`: Request parameter error
- `401`: User ID validation failed
- `403`: Insufficient permissions (admin functions)
- `404`: User or resource does not exist
- `429`: Request rate limit exceeded
- `500`: Server internal error

### 4.3 Data Flow Design
```
User request → User validation → Get original subscription → Load optimized IPs → Load template → Dynamic merge → Return YAML
```

## 5. Data Storage Structure

### 5.1 File Storage Structure
```
/app/data/
├── cloudflare-ip.csv     # Default optimized IPs
├── clash.yaml           # Default Clash template
├── users.txt            # User records
└── [userId]/            # User-specific configuration
    ├── cloudflare-ip.csv
    └── clash.yaml
```

### 5.2 Data Format Specifications
- **CSV format**: Optimized IP configuration, support CloudflareST output format
- **YAML format**: Clash configuration template, completely dynamic parsing
- **TXT format**: User access records

### 5.3 Data Validation Rules
- **User ID**: 1-64 characters, support [a-zA-Z0-9_-]
- **IP address submission**: Maximum 1 IP per line, at least 1 IP per submission, regex validation
- **CSV file**: Maximum 10MB, support CloudflareST format or pure IP format
- **YAML template**: Maximum 1MB, no field restrictions
- **Concurrency limit**: 10 requests per IP per second

### 5.4 Configuration Priority
1. User-specific configuration (priority)
2. Default configuration (fallback)
3. Original subscription (base)

### 5.5 File Operation Security
- Use `FileStream`+`FileShare.None` exclusive write
- Atomic write: Temporary file + replacement mode
- Concurrency safety: Avoid write conflicts
