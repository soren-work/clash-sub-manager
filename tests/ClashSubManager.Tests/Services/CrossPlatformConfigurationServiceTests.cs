using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClashSubManager.Services;
using Xunit;
using Moq;

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
            
            _service = new PlatformConfigurationService(
                _configuration,
                logger,
                _mockDetector,
                _mockPathResolver,
                _mockValidator,
                httpClient);
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
            var service = new PlatformConfigurationService(
                configurationWithoutDataPath,
                logger,
                _mockDetector,
                _mockPathResolver,
                _mockValidator,
                httpClient);

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
