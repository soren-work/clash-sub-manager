# ClashSubManager Client Extension Feature Requirements

**🌐 Language**: [English](client-requirements.md) | [中文](client-requirements-cn.md)

> **Important Note**: This document describes the **client extension features** of the ClashSubManager system, belonging to subsequent optimization modules, not MVP core functions. The core of the system is the server-side web application, providing subscription interfaces and management interface.

## I. Function Positioning and Boundaries

### System Architecture Positioning
- **Server-side (Core)**: .NET 10 Web application, providing subscription interfaces and management interface
- **Client Scripts (Extension)**: Cross-platform script programs for CloudflareST program management and speed test result submission
- **Interaction Relationship**: Client scripts implement data submission by calling server-side API interfaces

### Extension Feature Priority
- **MVP Phase**: Only implement server-side core functions
- **Extension Phase**: Add client script support as needed

### Explicitly Excluded Functions
- User registration and authentication system
- Database storage functionality
- Complex configuration management interface
- Distributed architecture support
- Advanced statistical analysis functions
- Multi-user management functions

### Extension Feature List (Non-MVP)
- **CloudflareST Program Management**: Automatic detection, download, cross-platform support
- **User Subscription Address Management**: Configuration storage, user ID extraction
- **Optimized IP Speed Testing and Submission**: CloudflareST integration, API interaction

## II. Extension Feature Detailed Design

* **Supported Operating Systems**: Windows, Linux, Unix, MacOS
* **Deployment Method**: Script files, placed in the same directory as CloudflareST program
* **Dependency Management**: Automatic detection and download of CloudflareST program
* **Configuration Storage**: Local file storage of user subscription addresses (sub.txt)
* **Network Communication**: Interact with server-side API
* **Architecture Pattern**: Cross-platform script program

### 2.1 CloudflareST Program Management **[Extension Feature]**

**Function Description**:

* Automatically detect if CloudflareST program exists in current directory;
* If not exists, provide GitHub download page address for manual download;
* Support user manual input of CloudflareST local storage location;
* Download corresponding version of CloudflareST based on current operating system:
  * Windows: Download `.exe` version
  * Linux/Unix: Download corresponding architecture binary file
  * MacOS: Download macOS version

**Technical Requirements**:

* Support multiple architectures: x86, x64, ARM64
* Provide download progress display
* Error handling and retry mechanism
* Cross-platform compatibility

---

### 2.2 User Subscription Address Management **[Extension Feature]**

**Function Description**:

* Support user input and saving of subscription addresses;
* Subscription address format: `https://sub.domain.com/sub/[id]`
* Save subscription address in `sub.txt` file in script directory;

### 2.3 Server-side API Integration **[Extension Feature]**

**Function Description**:

* Call server-side `POST /sub/[id]` interface to submit CloudflareST speed test results
* Call server-side `DELETE /sub/[id]` interface to delete user optimized IP configuration
* Support error retry and status feedback

**API Interaction Flow**:
1. Read local `sub.txt` to get user ID and subscription address
2. Execute CloudflareST speed test program
3. Parse speed test result file `result.csv`
4. Call server-side API to submit data
5. Handle response results and error status

**Configuration File Format**:
```
https://sub.domain.com/sub/your-id
```

## III. Integration with Server-side

### 3.1 API Interface Calls
Client scripts implement functionality by calling the following server-side interfaces:

| Interface | Method | Purpose | Data Format |
|-----------|--------|---------|-------------|
| Submit speed test results | POST /sub/[id] | Create/update user-specific optimized IPs | CSV format |
| Delete configuration | DELETE /sub/[id] | Delete user-specific optimized IPs | - |

### 3.2 Data Format Compatibility
- **Input Format**: result.csv file output by CloudflareST program
- **Submission Format**: CSV or text format compliant with server-side API requirements
- **Response Handling**: Parse server-side JSON response, handle success/failure status

### 3.3 Error Handling Strategy
- **Network Errors**: Support retry mechanism, maximum 3 retries
- **Authentication Errors**: Check user ID validity, prompt user to check subscription address
- **Server Errors**: Log error messages, provide user-friendly error prompts

## IV. Implementation Priority

### 4.1 MVP Phase (Server-side Core)
- **Focus**: Implement server-side web application core functions
- **Client**: Temporarily not implemented, submit CloudflareST results manually

### 4.2 Extension Phase (Client Support)
- **Priority 1**: Basic API calling functionality
- **Priority 2**: CloudflareST program management
- **Priority 3**: User interface and error handling optimization

### 4.3 Technical Implementation Suggestions
- **Script Language**: Choose based on target platform (PowerShell for Windows, Bash for Linux/Mac)
- **Cross-platform Solution**: Consider implementing with cross-platform languages like Python
- **Packaging Method**: Single script file, convenient for distribution and deployment

## V. Relationship with Existing Documents

### 5.1 Function Complementarity
- **This Document**: Describes client extension features, belonging to optional modules
- **mvp-outline.md**: Describes server-side core functions, belonging to MVP must implement
- **mvp-boundary.md**: Defines server-side technical boundaries and constraints

### 5.2 Interface Consistency
API interfaces called by client scripts are detailed in the following documents:
- **server-requirements.md**: Section 4 "Core Interface Specifications"
- **mvp-boundary.md**: Section 8 "API Interface Specifications"
- **mvp-core-features.md**: Section 2 "Core Function Definitions"

### 5.3 Implementation Suggestions
1. **MVP Phase**: Focus on server-side function implementation, client functions temporarily postponed
2. **Extension Phase**: Decide whether to implement client scripts based on actual needs
3. **Integration Testing**: After client implementation, conduct complete integration testing with server-side
