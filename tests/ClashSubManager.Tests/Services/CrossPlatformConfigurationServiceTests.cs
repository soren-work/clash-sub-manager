using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClashSubManager.Models;
using ClashSubManager.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
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

            _configuration = CreateConfiguration();
            var defaultNamingService = CreateNamingService(_configuration);
            _service = CreateService(_configuration, defaultNamingService);
        }

        private IConfiguration CreateConfiguration(Dictionary<string, string?>? overrides = null, bool includeDataPath = true)
        {
            var settings = new Dictionary<string, string?>
            {
                ["AdminUsername"] = "admin",
                ["AdminPassword"] = "password123",
                ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long",
                ["SessionTimeoutMinutes"] = "30"
            };

            if (includeDataPath)
            {
                settings["DataPath"] = "/test/data";
            }

            if (overrides != null)
            {
                foreach (var pair in overrides)
                {
                    settings[pair.Key] = pair.Value;
                }
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        private PlatformConfigurationService CreateService(
            IConfiguration configuration,
            INodeNamingTemplateService namingService,
            string? remoteConfig = null)
        {
            var logger = new Mock<ILogger<PlatformConfigurationService>>().Object;
            var handler = new TestHttpMessageHandler(remoteConfig ?? string.Empty);
            var httpClient = new HttpClient(handler);

            return new PlatformConfigurationService(
                configuration,
                logger,
                _mockDetector,
                _mockPathResolver,
                _mockValidator,
                httpClient,
                namingService);
        }

        private INodeNamingTemplateService CreateNamingService(IConfiguration configuration)
        {
            var namingLogger = new Mock<ILogger<NodeNamingTemplateService>>().Object;
            return new NodeNamingTemplateService(configuration, namingLogger);
        }

        private YamlSequenceNode ParseProxies(string yaml)
        {
            var stream = new YamlStream();
            using var reader = new StringReader(yaml);
            stream.Load(reader);

            var root = (YamlMappingNode)stream.Documents[0].RootNode;
            var proxiesNode = root.Children.FirstOrDefault(c =>
                (c.Key as YamlScalarNode)?.Value == "proxies").Value as YamlSequenceNode;

            Assert.NotNull(proxiesNode);
            return proxiesNode!;
        }

        private static void AssertProxy(YamlMappingNode proxy, string expectedName, string expectedServer, int expectedPort)
        {
            string? GetScalarValue(string key)
            {
                return proxy.Children.FirstOrDefault(c =>
                        (c.Key as YamlScalarNode)?.Value == key)
                    .Value is YamlScalarNode scalar
                    ? scalar.Value
                    : null;
            }

            Assert.Equal(expectedName, GetScalarValue("name"));
            Assert.Equal(expectedServer, GetScalarValue("server"));
            Assert.Equal(expectedPort.ToString(), GetScalarValue("port"));
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
            var configurationWithoutDataPath = CreateConfiguration(includeDataPath: false);
            var namingService = CreateNamingService(configurationWithoutDataPath);
            var service = CreateService(configurationWithoutDataPath, namingService);

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
        public async Task GenerateSubscriptionConfigAsync_DomainProxyExpanded_WithDefaultIPs()
        {
            var template = @"proxies:
  - name: DomainProxy
    server: example.com
    port: 443
    type: ss";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 8443, Latency = 50, PacketLoss = 0.1m },
                new() { IPAddress = "2.2.2.2", Port = 9443, Latency = 30, PacketLoss = 0.2m }
            };

            var result = await _service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, new List<IPRecord>());

            var proxies = ParseProxies(result);
            Assert.Equal(3, proxies.Children.Count);
            AssertProxy((YamlMappingNode)proxies.Children[0], "DomainProxy-Node-1", "example.com", 443);
            AssertProxy((YamlMappingNode)proxies.Children[1], "DomainProxy-Node-2", "1.1.1.1", 8443);
            AssertProxy((YamlMappingNode)proxies.Children[2], "DomainProxy-Node-3", "2.2.2.2", 9443);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_UsesDedicatedIPsWhenAvailable()
        {
            var template = @"proxies:
  - name: DedicatedDomain
    server: dedicated.example.com
    port: 8443
    type: vmess";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "9.9.9.9", Port = 9000, Latency = 40, PacketLoss = 0.3m }
            };
            var dedicatedIPs = new List<IPRecord>
            {
                new() { IPAddress = "10.0.0.1", Port = 8080, Latency = 10, PacketLoss = 0.01m },
                new() { IPAddress = "10.0.0.2", Port = 8081, Latency = 15, PacketLoss = 0.02m }
            };

            var result = await _service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, dedicatedIPs);
            var proxies = ParseProxies(result);

            Assert.Equal(3, proxies.Children.Count);
            AssertProxy((YamlMappingNode)proxies.Children[0], "DedicatedDomain-Node-1", "dedicated.example.com", 8443);
            AssertProxy((YamlMappingNode)proxies.Children[1], "DedicatedDomain-Node-2", "10.0.0.1", 8080);
            AssertProxy((YamlMappingNode)proxies.Children[2], "DedicatedDomain-Node-3", "10.0.0.2", 8081);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_IpProxyRemainsUnchanged()
        {
            var template = @"proxies:
  - name: RawIp
    server: 198.51.100.10
    port: 443
    type: ss";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 443, Latency = 50, PacketLoss = 0.1m }
            };

            var result = await _service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, new List<IPRecord>());
            var proxies = ParseProxies(result);

            Assert.Single(proxies.Children);
            AssertProxy((YamlMappingNode)proxies.Children[0], "RawIp", "198.51.100.10", 443);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_CustomNamingTemplateApplied()
        {
            var template = @"proxies:
  - name: FancyProxy
    server: fancy.example.com
    port: 2053
    type: ss";

            var overrides = new Dictionary<string, string?>
            {
                ["NodeNamingTemplate"] = "{name}-CF-{index}-{server}"
            };

            var configuration = CreateConfiguration(overrides);
            var namingService = CreateNamingService(configuration);
            var service = CreateService(configuration, namingService);

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 8443, Latency = 10, PacketLoss = 0.01m }
            };

            var result = await service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, new List<IPRecord>());
            var proxies = ParseProxies(result);

            Assert.Equal(2, proxies.Children.Count);
            AssertProxy((YamlMappingNode)proxies.Children[0], "FancyProxy-CF-1-fancy.example.com", "fancy.example.com", 2053);
            AssertProxy((YamlMappingNode)proxies.Children[1], "FancyProxy-CF-2-1.1.1.1", "1.1.1.1", 8443);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_DomainAndIpMixedHandledCorrectly()
        {
            var template = @"proxies:
  - name: DomainProxy
    server: example.com
    port: 443
    type: ss
  - name: IpProxy
    server: 203.0.113.10
    port: 8443
    type: vmess";

            var subscriptionUrl = "https://example.com/subscribe";
            var defaultIPs = new List<IPRecord>
            {
                new() { IPAddress = "1.1.1.1", Port = 2053, Latency = 20, PacketLoss = 0.05m }
            };

            var result = await _service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, defaultIPs, new List<IPRecord>());
            var proxies = ParseProxies(result);

            Assert.Equal(3, proxies.Children.Count);
            AssertProxy((YamlMappingNode)proxies.Children[0], "DomainProxy-Node-1", "example.com", 443);
            AssertProxy((YamlMappingNode)proxies.Children[1], "DomainProxy-Node-2", "1.1.1.1", 2053);
            AssertProxy((YamlMappingNode)proxies.Children[2], "IpProxy", "203.0.113.10", 8443);
        }

        [Fact]
        public async Task GenerateSubscriptionConfigAsync_NoAvailableIPs_KeepsOriginalDomainNode()
        {
            var template = @"proxies:
  - name: DomainProxy
    server: example.com
    port: 443
    type: ss";

            var subscriptionUrl = "https://example.com/subscribe";

            var result = await _service.GenerateSubscriptionConfigAsync(template, subscriptionUrl, new List<IPRecord>(), new List<IPRecord>());
            var proxies = ParseProxies(result);

            Assert.Single(proxies.Children);
            AssertProxy((YamlMappingNode)proxies.Children[0], "DomainProxy", "example.com", 443);
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

    internal class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;

        public TestHttpMessageHandler(string responseContent)
        {
            _responseContent = responseContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseContent)
            };

            return Task.FromResult(response);
        }
    }
}
