using Microsoft.Extensions.Configuration;
using ClashSubManager.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClashSubManager.Tests.Services
{
    /// <summary>
    /// ConfigurationValidator default configuration generation tests
    /// </summary>
    public class ConfigurationValidatorDefaultConfigTests
    {
        private readonly ConfigurationValidator _validator;

        public ConfigurationValidatorDefaultConfigTests()
        {
            var loggerMock = new Mock<ILogger<ConfigurationValidator>>();
            _validator = new ConfigurationValidator(loggerMock.Object);
        }

        [Fact]
        public void GenerateDefaultConfiguration_ReturnsValidDefaults()
        {
            // Act
            var defaults = _validator.GenerateDefaultConfiguration();

            // Assert
            Assert.NotNull(defaults);
            Assert.True(defaults.ContainsKey("CookieSecretKey"));
            Assert.True(defaults.ContainsKey("SessionTimeoutMinutes"));
            Assert.True(defaults.ContainsKey("DataPath"));

            // Verify CookieSecretKey length
            var secretKey = defaults["CookieSecretKey"];
            Assert.True(secretKey.Length >= 32, $"CookieSecretKey should be at least 32 characters, got {secretKey.Length}");

            // Verify SessionTimeoutMinutes value
            var sessionTimeout = defaults["SessionTimeoutMinutes"];
            Assert.Equal("30", sessionTimeout);

            // Verify DataPath is not empty
            var dataPath = defaults["DataPath"];
            Assert.False(string.IsNullOrEmpty(dataPath));
        }

        [Fact]
        public async Task WriteDefaultConfigurationAsync_CreatesValidJsonFile()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var testConfigPath = Path.Combine(tempDir, "test-appsettings.json");
            
            // Clean up potentially existing test files
            if (File.Exists(testConfigPath))
            {
                File.Delete(testConfigPath);
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            try
            {
                // Act
                await _validator.WriteDefaultConfigurationAsync(configuration, testConfigPath);

                // Assert
                Assert.True(File.Exists(testConfigPath));
                
                var json = await File.ReadAllTextAsync(testConfigPath);
                Assert.False(string.IsNullOrEmpty(json));
                
                // Verify JSON contains necessary configuration items
                Assert.Contains("CookieSecretKey", json);
                Assert.Contains("SessionTimeoutMinutes", json);
                Assert.Contains("DataPath", json);
                Assert.Contains("Logging", json);
                Assert.Contains("AllowedHosts", json);
            }
            finally
            {
                // Cleanup
                if (File.Exists(testConfigPath))
                {
                    File.Delete(testConfigPath);
                }
            }
        }

        [Fact]
        public async Task WriteDefaultConfigurationAsync_PreservesExistingConfig()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var testConfigPath = Path.Combine(tempDir, "test-existing-appsettings.json");
            
            // Create existing configuration file
            var existingConfig = new
            {
                Logging = new
                {
                    LogLevel = new
                    {
                        Default = "Warning",
                        Microsoft = "Information"
                    }
                },
                AllowedHosts = "localhost",
                ExistingKey = "ExistingValue"
            };

            var existingJson = System.Text.Json.JsonSerializer.Serialize(existingConfig, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(testConfigPath, existingJson);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            try
            {
                // Act
                await _validator.WriteDefaultConfigurationAsync(configuration, testConfigPath);

                // Assert
                var updatedJson = await File.ReadAllTextAsync(testConfigPath);
                
                // Verify existing configuration is preserved
                Assert.Contains("ExistingValue", updatedJson);
                Assert.Contains("Warning", updatedJson);
                
                // Verify new configuration is added
                Assert.Contains("CookieSecretKey", updatedJson);
                Assert.Contains("SessionTimeoutMinutes", updatedJson);
                Assert.Contains("DataPath", updatedJson);
            }
            finally
            {
                // Cleanup
                if (File.Exists(testConfigPath))
                {
                    File.Delete(testConfigPath);
                }
            }
        }
    }
}
