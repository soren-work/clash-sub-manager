# ClashSubManager Core Feature Definition

**🌐 Language**: [English](mvp-core-features.md) | [中文](mvp-core-features-cn.md)

## Feature Priority Classification

### Core Features (Highest Priority)
**Feature Description:** By calling the `GET /sub/[user id]` interface, the program will forward the call to `GET [real subscription address]/[user id]` to get data, and perform completely dynamic overwriting on the yaml data returned by the subscription address.

**User ID Validation:**
- Request `GET [real subscription address]/[user id]` with clash user-agent
- Validation successful: Return yaml document and HTTP status code is 200
- Validation failed: Return other content means user ID is wrong

**System Positioning:** As a supplementary layer between clash client and subscription service, adjust the original `clash client -> subscription service` flow to `clash client -> this system -> subscription service`, performing customized adjustments on content returned by subscription service.

**Fallback Strategy:** When neither user-specific nor default configuration exists, directly return original data from clash subscription interface.

**Dependency Relationship:** Support any backend providing clash subscription services.

**Overwrite Scope:**
1. **proxies smart extension:** Detect the `server` attribute type of each node in original `proxies` for differentiated processing:
   - **IP address nodes**: When `server` is an IP address, replace with cloudflare optimized IPs
   - **Domain nodes**: When `server` is a domain name, preserve original node unchanged
   - **No server nodes**: Preserve original node unchanged
   - **Deep copy guarantee**: Use recursive deep copy to ensure each node is completely independent, avoiding shared references

2. **yaml document structure extension:** Read the template and settings defined in `clash.yaml` file, prioritize template content over original content, add and replace template content into original content

**Compatibility Requirements:**
- **Completely Dynamic Processing:** Must dynamically parse and process all fields from clash.yaml template and subscription service return, strictly prohibit hardcoding any field names or structures
- **Field Integrity Guarantee:** Completely preserve all fields returned by subscription service, while completely including all fields from clash.yaml template
- **No Field Restrictions:** Do not limit, filter, or preset process any configuration fields
- **Future Compatibility:** Automatically support any fields and configuration items added in future clash versions

### Extended Feature 1 (Second Priority)
**Feature Description:** The system has administrators who can log in through admin username and password set in environment variables (based on docker), and manage `cloudflare-ip.csv` and `clash.yaml` after login.

**Storage Location:** `/app/data/cloudflare-ip.csv` and `/app/data/clash.yaml`

**Management Methods:**
- Update file (create if not exists)
- Delete file (or clear content)
- Update methods include pasting original content in full or uploading files
- Do not validate file content

### Extended Feature 2 (Third Priority)
**Feature Description:** Based on extended feature 1, support administrators managing `/app/data/[user id]/cloudflare-ip.csv` and `/app/data/[user id]/clash.yaml`.

**Priority Rules:** These two files have higher application priority than extended feature 1, meaning: if these files exist, do not use files from extended feature 1

### Extended Feature 3 (Third Priority)
**Feature Description:** Provide users with scripts supporting windows/linux/mac os, this script will automatically call `CloudflareST` program, and submit the program's result file `result.csv` to `/sub/[user id]` interface in the form of `POST` request.

**Operation Result:** This operation will create/update `/app/data/[user id]/cloudflare-ip.csv` file

### Extended Feature 4 (Fourth Priority)
**Feature Description:** Update extended feature 3, support calling `/sub/[user id]` in the form of `DELETE` request.

**Operation Result:** This operation will delete `/app/data/[user id]/cloudflare-ip.csv`

## System Configuration Management

### Subscription URL Configuration
**Global Configuration Method:** External subscription URL is uniformly configured via Docker environment variables, with each project instance corresponding to one external subscription system.

**Environment Variable:** `SUBSCRIPTION_URL_TEMPLATE`

**URL Template Support:**
- Path parameter replacement: `http://www.domain.com/sub/{userId}`
- Query parameter replacement: `http://www.domain.com/sub?userId={userId}`
- Fixed URL: `http://www.domain.com/sub/abcdefghijkl` (no replacement)

**Replacement Mechanism:** System automatically replaces `{userId}` placeholder with actual user ID received by `/sub/[id]` interface.

### User Management
**User Recording Method:** Only record user IDs in `/app/data/users.txt` with deduplication support.

**First Access Mechanism:** Automatically record to users.txt file when user calls `/sub/[id]` for the first time.

## Technical Implementation Points

### File Storage Structure
```
/app/data/
├── cloudflare-ip.csv          # Global IP file
├── clash.yaml                 # Global configuration file
├── users.txt                  # User ID records (auto-deduplication)
└── [user id]/
    ├── cloudflare-ip.csv      # User-specific IP file
    └── clash.yaml             # User-specific configuration file
```

**Configuration Simplification Description:**
- Remove complex UserConfig JSON configuration file
- User-specific IP configuration read directly from CSV file
- User management simplified to ID recording and deduplication

### API Interface Design
- `GET /sub/[user id]` - Get overwritten subscription data
- `POST /sub/[user id]` - Submit CloudflareST results
- `DELETE /sub/[user id]` - Delete user-specific IP file
- Admin login and management interfaces

### Standard Response Format
- **Success Response**: `Content-Type: application/json`
  ```json
  {"success": true, "message": "Operation description"}
  ```
- **Error Response**: `Content-Type: application/json`
  ```json
  {"success": false, "message": "Operation description"}
  ```

#### Special Responses
- **GET /sub/[id]**: `Content-Type: text/yaml`, return complete YAML configuration

#### Standard Error Codes
- `400`: Request parameter error
- `401`: User ID validation failed
- `403`: Insufficient permissions (admin functions)
- `404`: User or resource does not exist
- `429`: Request rate limit exceeded
- `500`: Server internal error

### Data Format Specifications
#### POST /sub/[id] Request Format
- **Content-Type**: `text/csv` or `text/plain`
- **Data Format**: CSV format or free text format, support CloudflareST output
- **Validation Rules**: 
  - Maximum 1 IP address per line
  - At least 1 IP address per submission
  - Use regex to extract and validate IP addresses
- **Example**:
  ```csv
  IP Address,Sent,Received,Packet Loss,Average Latency,Download Speed (MB/s)
  104.16.1.1,10,10,0%,45.2,12.5
  104.16.2.1,10,9,10%,52.1,8.3
  ```

### Priority Application Rules
1. User-specific files take priority over global files
2. clash.yaml template content takes priority over original subscription content
3. IP extension based on existing proxies copying and modification
4. **Dynamic Field Processing**: All fields must be dynamically parsed, strictly prohibit hardcoding field names or structures
5. **Complete Field Preservation**: All fields from subscription service and template files must be completely preserved to final output
