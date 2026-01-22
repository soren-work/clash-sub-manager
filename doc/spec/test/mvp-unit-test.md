# ClashSubManager MVP Unit Test Case Design

**🌐 Language**: [English](mvp-unit-test.md) | [中文](mvp-unit-test-cn.md)

## 1. MVP Test Scope Definition

### 1.1 Core Value Validation Points
- **Subscription Interface Functionality**: Verify `GET /sub/[id]` dynamic configuration merging capability
- **Admin Authentication**: Verify environment variable authentication and session management
- **File Management**: Verify CRUD operations for CSV and YAML files
- **Complete Dynamic Processing**: Verify YAML field dynamic parsing and merging

### 1.2 Minimum Test Function Set
- **User Subscription Interface Module**: GET/POST/DELETE /sub/[id] interface tests
- **Admin Authentication Module**: Login/logout and permission middleware tests
- **Default Optimized IP Management Module**: CSV file management tests
- **Clash Template Management Module**: YAML file management tests
- **User-Specific Configuration Management Module**: User-level configuration management tests

### 1.3 Explicitly Excluded Test Functions
- Frontend UI interaction tests (Bootstrap interface)
- Database integration tests (no database)
- Microservice communication tests (monolithic architecture)
- Performance stress tests (beyond MVP scope)
- Security penetration tests (beyond MVP scope)

## 2. Test Technical Architecture

### 2.1 Test Technology Selection
- **xUnit**: .NET unit testing framework
- **Moq**: Mock object framework
- **FluentAssertions**: Assertion library
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing
- **TestContainers**: Containerized test environment

### 2.2 Test Project Structure
```
ClashSubManager.Tests/
├── Unit/
│   ├── Pages/
│   │   ├── Admin/
│   │   │   ├── LoginTests.cs
│   │   │   ├── LogoutTests.cs
│   │   │   ├── DefaultIPsTests.cs
│   │   │   ├── ClashTemplateTests.cs
│   │   │   └── UserConfigTests.cs
│   │   └── Sub/
│   │       └── SubscriptionTests.cs
│   ├── Services/
│   │   ├── AuthServiceTests.cs
│   │   ├── FileServiceTests.cs
│   │   ├── SubscriptionServiceTests.cs
│   │   └── YAMLServiceTests.cs
│   └── Middleware/
│       └── AdminAuthMiddlewareTests.cs
├── Integration/
│   ├── SubscriptionApiTests.cs
│   └── AdminWorkflowTests.cs
├── TestData/
│   ├── csv-samples/
│   ├── yaml-samples/
│   └── mock-responses/
└── Helpers/
    ├── TestFixtures.cs
    ├── MockDataBuilder.cs
    └── FileTestHelper.cs
```

### 2.3 Test Data Management
- **Test Isolation**: Each test uses independent temporary directories
- **Data Cleanup**: Automatic cleanup of temporary files after tests
- **Mock Data**: Standardized CSV and YAML test data
- **Environment Variables**: Test-specific environment variable configuration

## 3. Core Function Module Test Cases

### 3.1 User Subscription Interface Module Tests

#### 3.1.1 GET /sub/[id] Normal Flow Test
```csharp
[Theory]
[InlineData("user123", "valid_user")]
[InlineData("test_user", "another_user")]
public async Task GetSubscription_ValidUser_ReturnsYamlConfig(string userId, string expectedUser)
{
    // Arrange
    var mockSubscriptionService = new Mock<ISubscriptionService>();
    mockSubscriptionService.Setup(x => x.ValidateUserAsync(userId))
                          .ReturnsAsync(true);
    mockSubscriptionService.Setup(x => x.GetOriginalSubscriptionAsync(userId))
                          .ReturnsAsync(GetMockOriginalSubscription());
    
    // Set up test files
    await SetupTestFiles(userId);
    
    // Act
    var response = await _client.GetAsync($"/sub/{userId}");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Content.Headers.ContentType.MediaType.Should().Be("text/yaml");
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("proxies:");
    content.Should().Contain("proxy-groups:");
    content.Should().Contain("rules:");
}
```

#### 3.1.2 GET /sub/[id] User Validation Failure Test
```csharp
[Theory]
[InlineData("invalid_user")]
[InlineData("nonexistent")]
[InlineData("")]
public async Task GetSubscription_InvalidUser_ReturnsUnauthorized(string userId)
{
    // Arrange
    var mockSubscriptionService = new Mock<ISubscriptionService>();
    mockSubscriptionService.Setup(x => x.ValidateUserAsync(userId))
                          .ReturnsAsync(false);
    
    // Act
    var response = await _client.GetAsync($"/sub/{userId}");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    errorResponse.Success.Should().BeFalse();
    errorResponse.Message.Should().Contain("User ID validation failed");
}
```

#### 3.1.3 POST /sub/[id] Update Optimized IP Test
```csharp
[Fact]
public async Task PostSubscription_ValidCSV_UpdatesUserIPs()
{
    // Arrange
    var userId = "test_user";
    var csvContent = @"IP Address,Sent,Received,Packet Loss Rate,Average Latency,Download Speed (MB/s)
104.16.1.1,10,10,0%,45.2,12.5
104.16.2.1,10,9,10%,52.1,8.3";
    
    var content = new StringContent(csvContent, Encoding.UTF8, "text/csv");
    
    // Act
    var response = await _client.PostAsync($"/sub/{userId}", content);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var successResponse = await response.Content.ReadFromJsonAsync<SuccessResponse>();
    successResponse.Success.Should().BeTrue();
    
    // Verify file created
    var userIPPath = Path.Combine(_testDataPath, userId, "cloudflare-ip.csv");
    File.Exists(userIPPath).Should().BeTrue();
    var savedContent = await File.ReadAllTextAsync(userIPPath);
    savedContent.Should().Be(csvContent);
}
```

#### 3.1.4 DELETE /sub/[id] Delete User IP Test
```csharp
[Fact]
public async Task DeleteSubscription_UserExists_DeletesUserIPs()
{
    // Arrange
    var userId = "test_user";
    var userIPPath = Path.Combine(_testDataPath, userId, "cloudflare-ip.csv");
    Directory.CreateDirectory(Path.GetDirectoryName(userIPPath));
    await File.WriteAllTextAsync(userIPPath, "104.16.1.1,10,10,0%,45.2,12.5");
    
    // Act
    var response = await _client.DeleteAsync($"/sub/{userId}");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var successResponse = await response.Content.ReadFromJsonAsync<SuccessResponse>();
    successResponse.Success.Should().BeTrue();
    
    // Verify file deleted
    File.Exists(userIPPath).Should().BeFalse();
}
```

### 3.2 Dynamic YAML Processing Tests

#### 3.2.1 Complete Dynamic Field Parsing Test
```csharp
[Fact]
public async Task ProcessYaml_DynamicFields_MergesCorrectly()
{
    // Arrange
    var originalYaml = @"proxies:
  - name: proxy1
    server: example.com
    port: 443
    type: trojan
    password: password123
    skip-cert-verify: true";

    var templateYaml = @"proxies:
  - name: proxy1
    server: example.com
    port: 443
    type: trojan
    password: password123
    skip-cert-verify: true
    udp: true
    smux-opts:
      stream:
        xudp-version: 1
        xudp-proxy-udp443: 'auto'
rules:
  - DOMAIN-SUFFIX,google.com,proxy1
  - DOMAIN-SUFFIX,youtube.com,proxy1";

    var ipList = new List<string> { "104.16.1.1", "104.16.2.1" };
    
    // Act
    var result = await _yamlService.ProcessSubscriptionAsync(originalYaml, templateYaml, ipList);
    
    // Assert
    result.Should().Contain("udp: true");
    result.Should().Contain("xudp-version: 1");
    result.Should().Contain("xudp-proxy-udp443: 'auto'");
    result.Should().Contain("DOMAIN-SUFFIX,google.com,proxy1");
    
    // Verify IP extension
    var proxyCount = CountOccurrences(result, "server: 104.");
    proxyCount.Should().Be(ipList.Count);
}
```

#### 3.2.2 Future Field Compatibility Test
```csharp
[Fact]
public async Task ProcessYaml_FutureFields_PreservesAllFields()
{
    // Arrange - Simulate new fields from future Clash versions
    var originalYaml = @"proxies:
  - name: proxy1
    server: example.com
    port: 443
    type: trojan
    password: password123
    # Possible new fields in future versions
    new-feature-flag: true
    experimental-opts:
      quantum-tunnel: enabled
      ai-routing: aggressive";

    var templateYaml = @"proxies:
  - name: proxy1
    server: example.com
    # More future fields
    future-metric: high-performance
    ai-optimization: v2.0";

    // Act
    var result = await _yamlService.ProcessSubscriptionAsync(originalYaml, templateYaml, new List<string>());
    
    // Assert - Verify all fields are preserved
    result.Should().Contain("new-feature-flag: true");
    result.Should().Contain("quantum-tunnel: enabled");
    result.Should().Contain("ai-routing: aggressive");
    result.Should().Contain("future-metric: high-performance");
    result.Should().Contain("ai-optimization: v2.0");
}
```

## 4. Admin Authentication Module Test Cases

### 4.1 Login Function Tests

#### 4.1.1 Normal Login Test
```csharp
[Fact]
public async Task Login_ValidCredentials_RedirectsToAdmin()
{
    // Arrange
    Environment.SetEnvironmentVariable("ADMIN_USERNAME", "admin");
    Environment.SetEnvironmentVariable("ADMIN_PASSWORD", "password123");
    Environment.SetEnvironmentVariable("COOKIE_SECRET_KEY", "test-key-32-chars-long");
    
    var loginData = new Dictionary<string, string>
    {
        {"Username", "admin"},
        {"Password", "password123"}
    };
    
    var content = new FormUrlEncodedContent(loginData);
    
    // Act
    var response = await _client.PostAsync("/Admin/Login", content);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    response.Headers.Location.ToString().Should().Contain("/Admin/Index");
    
    // Verify Cookie setting
    response.Headers.Should().ContainKey("Set-Cookie");
    var setCookieHeader = response.Headers.Single(h => h.Key == "Set-Cookie").Value;
    setCookieHeader.Should().Contain("AdminSession");
    setCookieHeader.Should().Contain("HttpOnly");
    setCookieHeader.Should().Contain("Secure");
}
```

#### 4.1.2 Invalid Credentials Test
```csharp
[Theory]
[InlineData("admin", "wrong_password")]
[InlineData("wrong_user", "password123")]
[InlineData("", "")]
[InlineData("admin", "")]
[InlineData("", "password123")]
public async Task Login_InvalidCredentials_ReturnsErrorPage(string username, string password)
{
    // Arrange
    Environment.SetEnvironmentVariable("ADMIN_USERNAME", "admin");
    Environment.SetEnvironmentVariable("ADMIN_PASSWORD", "password123");
    
    var loginData = new Dictionary<string, string>
    {
        {"Username", username},
        {"Password", password}
    };
    
    var content = new FormUrlEncodedContent(loginData);
    
    // Act
    var response = await _client.PostAsync("/Admin/Login", content);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var responseContent = await response.Content.ReadAsStringAsync();
    responseContent.Should().Contain("Invalid username or password");
}
```

### 4.2 Authentication Middleware Tests

#### 4.2.1 Unauthenticated Access Test
```csharp
[Theory]
[InlineData("/Admin/DefaultIPs")]
[InlineData("/Admin/ClashTemplate")]
[InlineData("/Admin/UserConfig")]
[InlineData("/Admin/Index")]
public async Task AdminPage_Unauthenticated_RedirectsToLogin(string path)
{
    // Act
    var response = await _client.GetAsync(path);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    response.Headers.Location.ToString().Should().Contain("/Admin/Login");
}
```

#### 4.2.2 Authenticated Access Test
```csharp
[Theory]
[InlineData("/Admin/DefaultIPs")]
[InlineData("/Admin/ClashTemplate")]
[InlineData("/Admin/UserConfig")]
[InlineData("/Admin/Index")]
public async Task AdminPage_Authenticated_ReturnsPage(string path)
{
    // Arrange - Login first to get valid Cookie
    await LoginAsAdmin();
    
    // Act
    var response = await _client.GetAsync(path);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## 5. IP Management Module Test Cases

### 5.1 CSV File Management Tests

#### 5.1.1 Save IP Configuration Test
```csharp
[Fact]
public async Task SetIPs_ValidCSV_SavesSuccessfully()
{
    // Arrange
    var userId = "test_user";
    var csvContent = @"IP Address,Sent,Received,Packet Loss Rate,Average Latency,Download Speed (MB/s)
104.16.1.1,10,10,0%,45.2,12.5
104.16.2.1,10,9,10%,52.1,8.3
104.16.3.1,10,8,20%,61.3,5.7";

    // Act
    var result = await _ipService.SetIPsAsync(csvContent, userId);
    
    // Assert
    result.Should().BeTrue();
    
    var filePath = Path.Combine(_testDataPath, userId, "cloudflare-ip.csv");
    File.Exists(filePath).Should().BeTrue();
    
    var savedContent = await File.ReadAllTextAsync(filePath);
    savedContent.Should().Be(csvContent);
}
```

#### 5.1.2 CSV Parsing Test
```csharp
[Fact]
public void ParseCSVContent_ValidContent_ReturnsCorrectRecords()
{
    // Arrange
    var csvContent = @"IP Address,Sent,Received,Packet Loss Rate,Average Latency,Download Speed (MB/s)
104.16.1.1,10,10,0%,45.2,12.5
104.16.2.1,10,9,10%,52.1,8.3";

    // Act
    var records = _ipService.ParseCSVContent(csvContent);
    
    // Assert
    records.Should().HaveCount(2);
    records[0].IPAddress.Should().Be("104.16.1.1");
    records[0].Sent.Should().Be("10");
    records[0].Received.Should().Be("10");
    records[0].PacketLossRate.Should().Be("0%");
    records[0].AverageLatency.Should().Be("45.2");
    records[0].DownloadSpeed.Should().Be("12.5");
}
```

#### 5.1.3 Invalid IP Address Test
```csharp
[Theory]
[InlineData("invalid.ip.address")]
[InlineData("999.999.999.999")]
[InlineData("not_an_ip")]
[InlineData("")]
public void ParseCSVContent_InvalidIP_SkipsRecord(string invalidIP)
{
    // Arrange
    var csvContent = $@"IP Address,Sent,Received,Packet Loss Rate,Average Latency,Download Speed (MB/s)
{invalidIP},10,10,0%,45.2,12.5
104.16.1.1,10,9,10%,52.1,8.3";

    // Act
    var records = _ipService.ParseCSVContent(csvContent);
    
    // Assert
    records.Should().HaveCount(1);
    records[0].IPAddress.Should().Be("104.16.1.1");
}
```

### 5.2 File Size Limit Tests

#### 5.2.1 Oversized File Rejection Test
```csharp
[Fact]
public async Task SetIPs_FileTooLarge_ReturnsFalse()
{
    // Arrange - Create CSV content exceeding 10MB
    var largeContent = string.Join("\n", Enumerable.Repeat("104.16.1.1,10,10,0%,45.2,12.5", 100000));
    
    // Act
    var result = await _ipService.SetIPsAsync(largeContent, "test_user");
    
    // Assert
    result.Should().BeFalse();
}
```

## 6. Clash Template Management Module Test Cases

### 6.1 YAML File Management Tests

#### 6.1.1 Save YAML Template Test
```csharp
[Fact]
public async Task SaveYAMLTemplate_ValidYAML_SavesSuccessfully()
{
    // Arrange
    var userId = "test_user";
    var yamlContent = @"proxies:
  - name: proxy1
    server: example.com
    port: 443
    type: trojan
    password: password123
    skip-cert-verify: true

proxy-groups:
  - name: proxy
    type: select
    proxies:
      - proxy1

rules:
  - DOMAIN-SUFFIX,google.com,proxy
  - DOMAIN-SUFFIX,youtube.com,proxy";

    // Act
    var result = await _yamlService.SaveYAMLTemplateAsync(yamlContent, userId);
    
    // Assert
    result.Should().BeTrue();
    
    var filePath = Path.Combine(_testDataPath, userId, "clash.yaml");
    File.Exists(filePath).Should().BeTrue();
    
    var savedContent = await File.ReadAllTextAsync(filePath);
    savedContent.Should().Be(yamlContent);
}
```

#### 6.1.2 YAML Format Validation Test
```csharp
[Theory]
[InlineData("valid: yaml\ncontent: here")]
[InlineData("proxies:\n  - name: test\n    server: example.com")]
public void IsValidYAML_ValidYAML_ReturnsTrue(string yamlContent)
{
    // Act
    var isValid = _yamlService.IsValidYAML(yamlContent);
    
    // Assert
    isValid.Should().BeTrue();
}

[Theory]
[InlineData("invalid: yaml\n  content: wrong_indent")]
[InlineData("proxies:\n  - name: test\n    server: example.com\n  wrong_indent: value")]
[InlineData("unclosed: [")]
public void IsValidYAML_InvalidYAML_ReturnsFalse(string yamlContent)
{
    // Act
    var isValid = _yamlService.IsValidYAML(yamlContent);
    
    // Assert
    isValid.Should().BeFalse();
}
```

#### 6.1.3 YAML File Size Limit Test
```csharp
[Fact]
public async Task SaveYAMLTemplate_FileTooLarge_ReturnsFalse()
{
    // Arrange - Create YAML content exceeding 1MB
    var largeContent = string.Join("\n", Enumerable.Repeat("key: value", 50000));
    
    // Act
    var result = await _yamlService.SaveYAMLTemplateAsync(largeContent, "test_user");
    
    // Assert
    result.Should().BeFalse();
}
```

## 7. User-Specific Configuration Management Test Cases

### 7.1 Configuration Priority Tests

#### 7.1.1 User Configuration Priority Test
```csharp
[Fact]
public async Task GetSubscription_UserAndGlobalConfig_UsesUserConfig()
{
    // Arrange
    var userId = "test_user";
    
    // Set global configuration
    var globalIPs = "104.16.1.1,10,10,0%,45.2,12.5";
    await _ipService.SetIPsAsync(globalIPs, null);
    
    // Set user configuration
    var userIPs = "104.16.2.1,10,9,10%,52.1,8.3";
    await _ipService.SetIPsAsync(userIPs, userId);
    
    var originalYaml = @"proxies:
  - name: proxy1
    server: example.com
    port: 443
    type: trojan";

    // Act
    var result = await _subscriptionService.ProcessSubscriptionAsync(userId, originalYaml);
    
    // Assert - Should use user configuration IPs
    result.Should().Contain("server: 104.16.2.1");
    result.Should().NotContain("server: 104.16.1.1");
}
```

#### 7.1.2 Fallback Mechanism Test
```csharp
[Fact]
public async Task GetSubscription_NoConfig_ReturnsOriginal()
{
    // Arrange
    var userId = "test_user";
    var originalYaml = @"proxies:
  - name: proxy1
    server: example.com
    port: 443
    type: trojan
    password: password123";

    // Act
    var result = await _subscriptionService.ProcessSubscriptionAsync(userId, originalYaml);
    
    // Assert - Should return original subscription content
    result.Should().Be(originalYaml);
}
```

## 8. Test Data Preparation and Cleanup Strategy

### 8.1 Test Fixture Design
```csharp
public class TestFixture : IDisposable
{
    public string TestDataPath { get; private set; }
    
    public TestFixture()
    {
        // Create unique temporary test directory
        TestDataPath = Path.Combine(Path.GetTempPath(), "ClashSubManagerTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(TestDataPath);
        
        // Set test environment variables
        Environment.SetEnvironmentVariable("DATA_PATH", TestDataPath);
        Environment.SetEnvironmentVariable("ADMIN_USERNAME", "test_admin");
        Environment.SetEnvironmentVariable("ADMIN_PASSWORD", "test_password");
        Environment.SetEnvironmentVariable("COOKIE_SECRET_KEY", "test-key-32-chars-long-for-hmac");
        Environment.SetEnvironmentVariable("SESSION_TIMEOUT_MINUTES", "30");
    }
    
    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(TestDataPath))
        {
            Directory.Delete(TestDataPath, true);
        }
    }
}
```

### 8.2 Mock Data Builder
```csharp
public static class MockDataBuilder
{
    public static string GetValidCSVContent()
    {
        return @"IP Address,Sent,Received,Packet Loss Rate,Average Latency,Download Speed (MB/s)
104.16.1.1,10,10,0%,45.2,12.5
104.16.2.1,10,9,10%,52.1,8.3
104.16.3.1,10,8,20%,61.3,5.7";
    }
    
    public static string GetValidYAMLContent()
    {
        return @"proxies:
  - name: proxy1
    server: example.com
    port: 443
    type: trojan
    password: password123
    skip-cert-verify: true

proxy-groups:
  - name: proxy
    type: select
    proxies:
      - proxy1

rules:
  - DOMAIN-SUFFIX,google.com,proxy
  - DOMAIN-SUFFIX,youtube.com,proxy";
    }
    
    public static string GetOriginalSubscriptionContent()
    {
        return @"proxies:
  - name: original-proxy
    server: original.example.com
    port: 443
    type: trojan
    password: original-password

proxy-groups:
  - name: original-group
    type: select
    proxies:
      - original-proxy

rules:
  - DOMAIN-SUFFIX,original.com,original-group";
    }
}
```

### 8.3 File Test Helper Class
```csharp
public static class FileTestHelper
{
    public static async Task SetupTestFiles(string userId, string testDataPath)
    {
        var userDir = Path.Combine(testDataPath, userId);
        Directory.CreateDirectory(userDir);
        
        // Create user-specific IP file
        var userIPPath = Path.Combine(userDir, "cloudflare-ip.csv");
        await File.WriteAllTextAsync(userIPPath, MockDataBuilder.GetValidCSVContent());
        
        // Create user-specific template file
        var userTemplatePath = Path.Combine(userDir, "clash.yaml");
        await File.WriteAllTextAsync(userTemplatePath, MockDataBuilder.GetValidYAMLContent());
    }
    
    public static async Task CleanupTestFiles(string userId, string testDataPath)
    {
        var userDir = Path.Combine(testDataPath, userId);
        if (Directory.Exists(userDir))
        {
            Directory.Delete(userDir, true);
        }
    }
    
    public static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
```

## 9. MVP Test Constraint Check

### 9.1 Test Scope Compliance
- ✅ **Test MVP functions only**: No non-MVP function tests included
- ✅ **Core value validation**: Focus on testing dynamic YAML processing capability
- ✅ **Technical constraint compliance**: Use .NET 10 + xUnit test stack
- ✅ **Monolithic architecture testing**: No microservice integration tests

### 9.2 Test Technical Constraint Compliance
- ✅ **xUnit framework**: Conforms to .NET testing standards
- ✅ **Moq mocking**: Avoid external dependencies
- ✅ **FluentAssertions**: Improve test readability
- ✅ **Test isolation**: Each test runs independently

### 9.3 MVP Test Optimization Results
- ✅ **Simplified test design**: Direct testing of core business logic
- ✅ **Remove complex tests**: No UI tests and performance tests included
- ✅ **Standardized test data**: Unified Mock data builder
- ✅ **Automated cleanup**: Test fixtures automatically clean up

## 10. Test Implementation Plan

### 10.1 Test Development Priority
1. **High priority**: Core subscription interface and dynamic YAML processing tests
2. **High priority**: Admin authentication and permission tests
3. **Medium priority**: File management function tests
4. **Medium priority**: Configuration priority and fallback mechanism tests

### 10.2 Test Acceptance Criteria
- **Code coverage**: ≥80% (MVP functions only)
- **Test pass rate**: 100%
- **Test execution time**: ≤5 minutes
- **Test isolation**: No inter-test dependencies

### 10.3 Continuous Integration Integration
- **Automated execution**: Automatically run on every code commit
- **Test reports**: Generate detailed test coverage reports
- **Quick feedback**: Immediate notification on test failures
- **Environment consistency**: Use Docker containers to ensure consistent test environment
