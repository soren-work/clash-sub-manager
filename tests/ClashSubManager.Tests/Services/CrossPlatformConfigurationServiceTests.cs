using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClashSubManager.Services;
using ClashSubManager.Models;
using Xunit;
using Moq;
using YamlDotNet.RepresentationModel;

namespace ClashSubManager.Tests.Services
{
    /// <summary>
    /// PlatformConfigurationService unit tests
    /// </summary>
    public class PlatformConfigurationServiceTests
    {
        private readonly MockEnvironmentDetector _mockDetector;
        private readonly MockPathResolver _mockPathResolver;
        private readonly MockConfigurationValidator _mockValidator;
        private readonly IConfiguration _configuration;
        private readonly PlatformConfigurationService _service;

        public PlatformConfigurationServiceTests()
        {
            _mockDetector = new MockEnvironmentDetector();
            _mockPathResolver = new MockPathResolver();
            _mockValidator = new MockConfigurationValidator();
            
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long",
                    ["SessionTimeoutMinutes"] = "30",
                    ["DataPath"] = "/test/data"
                })
                .Build();

            var logger = new Mock<ILogger<PlatformConfigurationService>>().Object;
            var httpClient = new Mock<System.Net.Http.HttpClient>().Object;
            var mockNamingService = new Mock<INodeNamingTemplateService>().Object;
            
            _service = new PlatformConfigurationService(
                _configuration,
                logger,
                _mockDetector,
                _mockPathResolver,
                _mockValidator,
                httpClient,
                mockNamingService);
        }

        [Fact]
        public void GetDataPath_ReturnsConfiguredPath_WhenDataPathIsSet()
        {
            // Arrange
            var expectedPath = "/test/data";
            _mockPathResolver.SetResolvePathResult(expectedPath);

            // Act
            var result = _service.GetDataPath();

            // Assert
            Assert.Equal(expectedPath, result);
            Assert.True(_mockPathResolver.ResolvePathCalled);
        }

        [Fact]
        public void GetDataPath_ReturnsDefaultPath_WhenDataPathIsNotSet()
        {
            // Arrange
            var configurationWithoutDataPath = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long"
                })
                .Build();

            var logger = new Mock<ILogger<PlatformConfigurationService>>().Object;
            var httpClient = new Mock<System.Net.Http.HttpClient>().Object;
            var mockNamingService = new Mock<INodeNamingTemplateService>().Object;
            var service = new PlatformConfigurationService(
                configurationWithoutDataPath,
                logger,
                _mockDetector,
                _mockPathResolver,
                _mockValidator,
                httpClient,
                mockNamingService);

            var expectedPath = "/default/data";
            _mockPathResolver.SetDefaultDataPathResult(expectedPath);

            // Act
            var result = service.GetDataPath();

            // Assert
            Assert.Equal(expectedPath, result);
            Assert.True(_mockPathResolver.GetDefaultDataPathCalled);
        }

        [Fact]
        public void GetValue_ReturnsCorrectValue_WhenKeyExists()
        {
            // Act
            var result = _service.GetValue<string>("AdminUsername");

            // Assert
            Assert.Equal("admin", result);
        }

        [Fact]
        public void GetValue_ReturnsDefaultValue_WhenKeyDoesNotExist()
        {
            // Act
            var result = _service.GetValue<string>("NonExistentKey", "default_value");

            // Assert
            Assert.Equal("default_value", result);
        }

        [Fact]
        public void GetValue_ReturnsTypedValue_WhenTypeIsSpecified()
        {
            // Act
            var result = _service.GetValue<int>("SessionTimeoutMinutes");

            // Assert
            Assert.Equal(30, result);
        }

        [Fact]
        public void HasValue_ReturnsTrue_WhenKeyExists()
        {
            // Act
            var result = _service.HasValue("AdminUsername");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasValue_ReturnsFalse_WhenKeyDoesNotExist()
        {
            // Act
            var result = _service.HasValue("NonExistentKey");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateConfiguration_DoesNotThrow_WhenConfigurationIsValid()
        {
            // Act & Assert
            _service.ValidateConfiguration();
            Assert.True(_mockValidator.ValidateCalled);
        }

        [Fact]
        public void ValidateConfiguration_Throws_WhenConfigurationIsInvalid()
        {
            // Arrange
            _mockValidator.SetShouldThrow(true);

            // Act & Assert
            Assert.Throws<ConfigurationException>(() => _service.ValidateConfiguration());
        }

        [Fact]
        public void GetEnvironmentType_ReturnsEnvironmentType()
        {
            // Arrange
            var expectedType = "TestEnvironment";
            _mockDetector.SetEnvironmentType(expectedType);

            // Act
            var result = _service.GetEnvironmentType();

            // Assert
            Assert.Equal(expectedType, result);
        }

        [Fact]
        public void GetDataPath_CachesResult_WhenCalledMultipleTimes()
        {
            // Arrange
            var expectedPath = "/test/data";
            _mockPathResolver.SetResolvePathResult(expectedPath);

            // Act
            var result1 = _service.GetDataPath();
            var result2 = _service.GetDataPath();

            // Assert
            Assert.Equal(expectedPath, result1);
            Assert.Equal(expectedPath, result2);
            // Should only call ResolvePath once due to caching
            Assert.Equal(1, _mockPathResolver.ResolvePathCallCount);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_UsesIndexNaming_WhenUseIpInNodeNameIsFalse()
        {
            // Arrange
            var template = @"proxies:
  - name: TestProxy
    server: 192.168.1.1
    port: 443
    type: ss";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 443, Latency = 50, PacketLoss = 0.1m },
                new() { IPAddress = "2.2.2.2", Port = 443, Latency = 30, PacketLoss = 0.2m }
            };
            var dedicatedIPs = new List<IPRecord>();

            // Configure to use index naming (default value)
            var configWithIndexNaming = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long",
                    ["SessionTimeoutMinutes"] = "30",
                    ["DataPath"] = "/test/data",
                    ["UseIpInNodeName"] = "false"
                })
                .Build();

            var logger = new Mock<ILogger<PlatformConfigurationService>>().Object;
            var httpClient = new Mock<System.Net.Http.HttpClient>().Object;
            var mockNamingService = new Mock<INodeNamingTemplateService>().Object;
            var service = new PlatformConfigurationService(
                configWithIndexNaming,
                logger,
                _mockDetector,
                _mockPathResolver,
                _mockValidator,
                httpClient,
                mockNamingService);

            // Act
            var result = await service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, dedicatedIPs);

            // Assert
            Assert.Contains("TestProxy-Node-1", result);
            Assert.Contains("TestProxy-Node-2", result);
            Assert.DoesNotContain("TestProxy-1.1.1.1", result);
            Assert.DoesNotContain("TestProxy-2.2.2.2", result);
            // Original IP should be replaced
            Assert.DoesNotContain("server: 192.168.1.1", result);
            Assert.Contains("server: 1.1.1.1", result);
            Assert.Contains("server: 2.2.2.2", result);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_UsesIpNaming_WhenUseIpInNodeNameIsTrue()
        {
            // Arrange
            var template = @"proxies:
  - name: TestProxy
    server: 192.168.1.1
    port: 443
    type: ss";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 443, Latency = 50, PacketLoss = 0.1m },
                new() { IPAddress = "2.2.2.2", Port = 443, Latency = 30, PacketLoss = 0.2m }
            };
            var dedicatedIPs = new List<IPRecord>();

            // Configure to use IP naming
            var configWithIpNaming = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long",
                    ["SessionTimeoutMinutes"] = "30",
                    ["DataPath"] = "/test/data",
                    ["UseIpInNodeName"] = "true"
                })
                .Build();

            var logger = new Mock<ILogger<PlatformConfigurationService>>().Object;
            var httpClient = new Mock<System.Net.Http.HttpClient>().Object;
            var namingLogger = new Mock<ILogger<NodeNamingTemplateService>>().Object;
            var namingService = new NodeNamingTemplateService(configWithIpNaming, namingLogger);
            var service = new PlatformConfigurationService(
                configWithIpNaming,
                logger,
                _mockDetector,
                _mockPathResolver,
                _mockValidator,
                httpClient,
                namingService);

            // Act
            var result = await service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, dedicatedIPs);

            // Assert
            Assert.Contains("TestProxy-1.1.1.1", result);
            Assert.Contains("TestProxy-2.2.2.2", result);
            Assert.DoesNotContain("TestProxy-Node-1", result);
            Assert.DoesNotContain("TestProxy-Node-2", result);
            // Original IP should be replaced
            Assert.DoesNotContain("server: 192.168.1.1", result);
            Assert.Contains("server: 1.1.1.1", result);
            Assert.Contains("server: 2.2.2.2", result);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_CreatesIndependentNodes_WithDeepClone()
        {
            // Arrange
            var template = @"proxies:
  - name: TestProxy
    server: 192.168.1.1
    port: 443
    type: ss";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 443, Latency = 50, PacketLoss = 0.1m },
                new() { IPAddress = "2.2.2.2", Port = 443, Latency = 30, PacketLoss = 0.2m },
                new() { IPAddress = "3.3.3.3", Port = 443, Latency = 40, PacketLoss = 0.3m }
            };
            var dedicatedIPs = new List<IPRecord>();

            // Create service with UseIpInNodeName=false configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long",
                    ["SessionTimeoutMinutes"] = "30",
                    ["DataPath"] = "/test/data",
                    ["UseIpInNodeName"] = "false"
                })
                .Build();

            var logger = new Mock<ILogger<PlatformConfigurationService>>().Object;
            var httpClient = new Mock<System.Net.Http.HttpClient>().Object;
            var namingLogger = new Mock<ILogger<NodeNamingTemplateService>>().Object;
            var namingService = new NodeNamingTemplateService(configuration, namingLogger);
            
            var service = new PlatformConfigurationService(
                configuration,
                logger,
                _mockDetector,
                _mockPathResolver,
                _mockValidator,
                httpClient,
                namingService);

            // Act
            var result = await service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, dedicatedIPs);

            // Assert - Verify each node has independent IP address and name
            Assert.Contains("server: 1.1.1.1", result);
            Assert.Contains("server: 2.2.2.2", result);
            Assert.Contains("server: 3.3.3.3", result);

            // Verify names are not duplicated
            Assert.Contains("TestProxy-Node-1", result);
            Assert.Contains("TestProxy-Node-2", result);
            Assert.Contains("TestProxy-Node-3", result);

            // Verify no duplicate accumulation issues
            Assert.DoesNotContain("TestProxy-Node-1-Node-2", result);
            Assert.DoesNotContain("TestProxy-Node-1-Node-2-Node-3", result);
            Assert.DoesNotContain("(50ms/0.1%)(30ms/0.2%)(40ms/0.3%)", result);

            // Original IP should be replaced
            Assert.DoesNotContain("server: 192.168.1.1", result);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_PreservesOriginalStructure_WithDedicatedIPs()
        {
            // Arrange
            var template = @"proxies:
  - name: DedicatedProxy
    server: 10.0.0.100
    port: 8080
    type: vmess";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 443, Latency = 50, PacketLoss = 0.1m }
            };
            var dedicatedIPs = new List<IPRecord>
            {
                new() { IPAddress = "10.0.0.1", Port = 8080, Latency = 10, PacketLoss = 0.01m },
                new() { IPAddress = "10.0.0.2", Port = 8080, Latency = 15, PacketLoss = 0.02m }
            };

            // Act
            var result = await _service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, dedicatedIPs);

            // Assert - Should use dedicated IPs instead of default IPs
            Assert.Contains("server: 10.0.0.1", result);
            Assert.Contains("server: 10.0.0.2", result);
            Assert.DoesNotContain("server: 1.1.1.1", result);
            // Original IP should be replaced
            Assert.DoesNotContain("server: 10.0.0.100", result);
            
            // Verify port is correctly set
            Assert.Contains("port: 8080", result);
            Assert.DoesNotContain("port: 443", result);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_PreservesDomainProxies_WhenMixedWithIPs()
        {
            // Arrange
            var template = @"proxies:
  - name: DomainProxy
    server: example.com
    port: 443
    type: ss
  - name: IPProxy
    server: 1.1.1.1
    port: 443
    type: ss";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "2.2.2.2", Port = 443, Latency = 50, PacketLoss = 0.1m },
                new() { IPAddress = "3.3.3.3", Port = 443, Latency = 30, PacketLoss = 0.2m }
            };
            var dedicatedIPs = new List<IPRecord>();

            // Create service with UseIpInNodeName=false configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long",
                    ["SessionTimeoutMinutes"] = "30",
                    ["DataPath"] = "/test/data",
                    ["UseIpInNodeName"] = "false"
                })
                .Build();

            var logger = new Mock<ILogger<PlatformConfigurationService>>().Object;
            var httpClient = new Mock<System.Net.Http.HttpClient>().Object;
            var namingLogger = new Mock<ILogger<NodeNamingTemplateService>>().Object;
            var namingService = new NodeNamingTemplateService(configuration, namingLogger);
            
            var service = new PlatformConfigurationService(
                configuration,
                logger,
                _mockDetector,
                _mockPathResolver,
                _mockValidator,
                httpClient,
                namingService);

            // Act
            var result = await service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, dedicatedIPs);

            // Assert - Domain proxies should be preserved
            Assert.Contains("server: example.com", result);
            Assert.Contains("name: DomainProxy", result);
            
            // Assert - IP proxies should be replaced with cloudflare IPs
            Assert.Contains("server: 2.2.2.2", result);
            Assert.Contains("server: 3.3.3.3", result);
            Assert.DoesNotContain("server: 1.1.1.1", result); // Original IP should be replaced
            
            // Assert - Names should be generated correctly
            Assert.Contains("IPProxy-Node-1", result);
            Assert.Contains("IPProxy-Node-2", result);
            Assert.DoesNotContain("IPProxy-Node-3", result); // Should not have a third one
            
            // Assert - Total node count should be 1 domain proxy + 2 cloudflare IP proxies = 3
            var proxyCount = result.Split('\n').Count(line => line.Trim().StartsWith("- name:"));
            Assert.Equal(3, proxyCount);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_PureIPConfiguration_ReplacesAllWithCloudflareIPs()
        {
            // Arrange - Scenario 1: Pure IP address configuration
            var template = @"proxies:
  - name: IPProxy1
    server: 192.168.1.1
    port: 443
    type: ss
  - name: IPProxy2
    server: 10.0.0.1
    port: 8080
    type: vmess";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 443, Latency = 50, PacketLoss = 0.1m },
                new() { IPAddress = "2.2.2.2", Port = 443, Latency = 30, PacketLoss = 0.2m }
            };
            var dedicatedIPs = new List<IPRecord>();

            // Act
            var result = await _service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, dedicatedIPs);

            // Assert - Original IPs should be replaced
            Assert.DoesNotContain("server: 192.168.1.1", result);
            Assert.DoesNotContain("server: 10.0.0.1", result);
            
            // Assert - Should contain cloudflare IPs
            Assert.Contains("server: 1.1.1.1", result);
            Assert.Contains("server: 2.2.2.2", result);
            
            // Assert - Each original IP proxy generates 2 new proxies, total 4
            Assert.Contains("IPProxy1-Node-1", result);
            Assert.Contains("IPProxy1-Node-2", result);
            Assert.Contains("IPProxy2-Node-1", result);
            Assert.Contains("IPProxy2-Node-2", result);
            
            var proxyCount = result.Split('\n').Count(line => line.Trim().StartsWith("- name:"));
            Assert.Equal(4, proxyCount);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_PureDomainConfiguration_PreservesAll()
        {
            // Arrange - Scenario 2: Pure domain configuration
            var template = @"proxies:
  - name: DomainProxy1
    server: example.com
    port: 443
    type: ss
  - name: DomainProxy2
    server: test.example.org
    port: 8080
    type: vmess";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 443, Latency = 50, PacketLoss = 0.1m },
                new() { IPAddress = "2.2.2.2", Port = 443, Latency = 30, PacketLoss = 0.2m }
            };
            var dedicatedIPs = new List<IPRecord>();

            // Act
            var result = await _service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, dedicatedIPs);

            // Assert - All domain proxies should be preserved
            Assert.Contains("server: example.com", result);
            Assert.Contains("server: test.example.org", result);
            Assert.Contains("name: DomainProxy1", result);
            Assert.Contains("name: DomainProxy2", result);
            
            // Assert - Should not contain cloudflare IPs
            Assert.DoesNotContain("server: 1.1.1.1", result);
            Assert.DoesNotContain("server: 2.2.2.2", result);
            
            // Assert - Total node count should be 2
            var proxyCount = result.Split('\n').Count(line => line.Trim().StartsWith("- name:"));
            Assert.Equal(2, proxyCount);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_ComplexMixedConfiguration_HandlesCorrectly()
        {
            // Arrange - Scenario 4: Complex mixed configuration
            var template = @"proxies:
  - name: DomainProxy1
    server: example.com
    port: 443
    type: ss
  - name: IPProxy1
    server: 192.168.1.1
    port: 443
    type: ss
  - name: DomainProxy2
    server: test.example.org
    port: 8080
    type: vmess
  - name: IPProxy2
    server: 10.0.0.1
    port: 8080
    type: vmess";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 443, Latency = 50, PacketLoss = 0.1m },
                new() { IPAddress = "2.2.2.2", Port = 443, Latency = 30, PacketLoss = 0.2m }
            };
            var dedicatedIPs = new List<IPRecord>();

            // Create service with UseIpInNodeName=false configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long",
                    ["SessionTimeoutMinutes"] = "30",
                    ["DataPath"] = "/test/data",
                    ["UseIpInNodeName"] = "false"
                })
                .Build();

            var logger = new Mock<ILogger<PlatformConfigurationService>>().Object;
            var httpClient = new Mock<System.Net.Http.HttpClient>().Object;
            var namingLogger = new Mock<ILogger<NodeNamingTemplateService>>().Object;
            var namingService = new NodeNamingTemplateService(configuration, namingLogger);
            
            var service = new PlatformConfigurationService(
                configuration,
                logger,
                _mockDetector,
                _mockPathResolver,
                _mockValidator,
                httpClient,
                namingService);

            // Act
            var result = await service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, dedicatedIPs);

            // Assert - Domain proxies should be preserved
            Assert.Contains("server: example.com", result);
            Assert.Contains("server: test.example.org", result);
            Assert.Contains("name: DomainProxy1", result);
            Assert.Contains("name: DomainProxy2", result);
            
            // Assert - Original IPs should be replaced
            Assert.DoesNotContain("server: 192.168.1.1", result);
            Assert.DoesNotContain("server: 10.0.0.1", result);
            
            // Assert - Should contain cloudflare IPs
            Assert.Contains("server: 1.1.1.1", result);
            Assert.Contains("server: 2.2.2.2", result);
            
            // Assert - Each original IP proxy generates 2 new proxies
            Assert.Contains("IPProxy1-Node-1", result);
            Assert.Contains("IPProxy1-Node-2", result);
            Assert.Contains("IPProxy2-Node-1", result);
            Assert.Contains("IPProxy2-Node-2", result);
            
            // Assert - Total node count should be 2 domain proxies + 4 cloudflare IP proxies = 6
            var proxyCount = result.Split('\n').Count(line => line.Trim().StartsWith("- name:"));
            Assert.Equal(6, proxyCount);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_NoServerNode_PreservesAll()
        {
            // Arrange - Scenario 5: No server node
            var template = @"proxies:
  - name: NoServerProxy
    port: 443
    type: ss
  - name: DomainProxy
    server: example.com
    port: 8080
    type: vmess";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 443, Latency = 50, PacketLoss = 0.1m },
                new() { IPAddress = "2.2.2.2", Port = 443, Latency = 30, PacketLoss = 0.2m }
            };
            var dedicatedIPs = new List<IPRecord>();

            // Act
            var result = await _service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, dedicatedIPs);

            // Assert - All proxies should be preserved
            Assert.Contains("name: NoServerProxy", result);
            Assert.Contains("name: DomainProxy", result);
            Assert.Contains("server: example.com", result);
            
            // Assert - Should not contain cloudflare IPs
            Assert.DoesNotContain("server: 1.1.1.1", result);
            Assert.DoesNotContain("server: 2.2.2.2", result);
            
            // Assert - Total node count should be 2
            var proxyCount = result.Split('\n').Count(line => line.Trim().StartsWith("- name:"));
            Assert.Equal(2, proxyCount);
        }
    }

    /// <summary>
    /// Mock PathResolver for testing
    /// </summary>
    public class MockPathResolver : IPathResolver
    {
        private string _resolvePathResult = "";
        private string _defaultDataPathResult = "";

        public bool ResolvePathCalled { get; private set; }
        public bool GetDefaultDataPathCalled { get; private set; }
        public int ResolvePathCallCount { get; private set; }

        public string ResolvePath(string path)
        {
            ResolvePathCalled = true;
            ResolvePathCallCount++;
            return _resolvePathResult;
        }

        public string GetDefaultDataPath()
        {
            GetDefaultDataPathCalled = true;
            return _defaultDataPathResult;
        }

        public bool IsValidPath(string path)
        {
            return true;
        }

        public void SetResolvePathResult(string result)
        {
            _resolvePathResult = result;
        }

        public void SetDefaultDataPathResult(string result)
        {
            _defaultDataPathResult = result;
        }
    }

    /// <summary>
    /// Mock ConfigurationValidator for testing
    /// </summary>
    public class MockConfigurationValidator : IConfigurationValidator
    {
        public bool ValidateCalled { get; private set; }
        public bool ShouldThrow { get; set; } = false;

        public void SetShouldThrow(bool shouldThrow)
        {
            ShouldThrow = shouldThrow;
        }

        public void Validate(IConfiguration configuration)
        {
            ValidateCalled = true;
            if (ShouldThrow)
            {
                throw new ConfigurationException(new List<string> { "Mock validation error" });
            }
        }

        public List<string> GetValidationErrors(IConfiguration configuration)
        {
            return new List<string>();
        }

        public Dictionary<string, string> GenerateDefaultConfiguration()
        {
            return new Dictionary<string, string>
            {
                ["CookieSecretKey"] = "Mock_Secret_Key_32_Characters_Long",
                ["SessionTimeoutMinutes"] = "30",
                ["DataPath"] = "./mock_data"
            };
        }

        public Task WriteDefaultConfigurationAsync(IConfiguration configuration, string filePath)
        {
            // Mock implementation - do nothing
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Mock EnvironmentDetector for testing
    /// </summary>
    public class MockEnvironmentDetector : IEnvironmentDetector
    {
        public bool IsDocker { get; private set; } = false;
        public string EnvironmentType { get; private set; } = EnvironmentTypes.Standalone;

        public string GetEnvironmentType()
        {
            return EnvironmentType;
        }

        public void SetEnvironmentType(string environmentType)
        {
            EnvironmentType = environmentType;
        }

        public bool IsDockerEnvironment()
        {
            return IsDocker;
        }

        public bool IsWindowsEnvironment() => OperatingSystem.IsWindows();
        public bool IsLinuxEnvironment() => OperatingSystem.IsLinux();
        public bool IsMacOSEnvironment() => OperatingSystem.IsMacOS();

        public void SetIsDockerEnvironment(bool isDocker)
        {
            IsDocker = isDocker;
        }
    }
}
