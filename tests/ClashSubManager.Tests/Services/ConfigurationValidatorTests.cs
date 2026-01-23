using Microsoft.Extensions.Configuration;
using ClashSubManager.Services;
using Xunit;

namespace ClashSubManager.Tests.Services
{
    /// <summary>
    /// ConfigurationValidator unit tests
    /// </summary>
    public class ConfigurationValidatorTests
    {
        private readonly ConfigurationValidator _validator;

        public ConfigurationValidatorTests()
        {
            _validator = new ConfigurationValidator();
        }

        [Fact]
        public void Validate_DoesNotThrow_WhenConfigurationIsValid()
        {
            // Arrange
            var configuration = CreateValidConfiguration();

            // Act & Assert
            _validator.Validate(configuration);
        }

        [Fact]
        public void Validate_ThrowsConfigurationException_WhenAdminUsernameIsMissing()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long"
                })
                .Build();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => _validator.Validate(configuration));
            Assert.Contains("AdminUsername is required", exception.ValidationErrors);
        }

        [Fact]
        public void Validate_ThrowsConfigurationException_WhenAdminPasswordIsMissing()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long"
                })
                .Build();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => _validator.Validate(configuration));
            Assert.Contains("AdminPassword is required", exception.ValidationErrors);
        }

        [Fact]
        public void Validate_ThrowsConfigurationException_WhenCookieSecretKeyIsTooShort()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "short"
                })
                .Build();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => _validator.Validate(configuration));
            Assert.Contains("CookieSecretKey must be at least 32 characters", exception.ValidationErrors);
        }

        [Fact]
        public void Validate_ThrowsConfigurationException_WhenSessionTimeoutIsOutOfRange()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long",
                    ["SessionTimeoutMinutes"] = "2" // Below minimum of 5
                })
                .Build();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => _validator.Validate(configuration));
            Assert.Contains("SessionTimeoutMinutes must be between 5 and 1440", exception.ValidationErrors);
        }

        [Fact]
        public void Validate_ThrowsConfigurationException_WhenDataPathIsInvalid()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long",
                    ["DataPath"] = "Z:\\invalid\\drive\\path" // Invalid path
                })
                .Build();

            // Act & Assert
            var exception = Assert.Throws<ConfigurationException>(() => _validator.Validate(configuration));
            Assert.Contains("Cannot create data directory", exception.ValidationErrors.First());
        }

        [Fact]
        public void GetValidationErrors_ReturnsAllErrors_WhenMultipleValidationFailures()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "short"
                })
                .Build();

            // Act
            var errors = _validator.GetValidationErrors(configuration);

            // Assert
            Assert.True(errors.Count >= 2);
            Assert.Contains(errors, e => e.Contains("AdminUsername is required"));
            Assert.Contains(errors, e => e.Contains("CookieSecretKey must be at least 32 characters"));
        }

        [Fact]
        public void GetValidationErrors_ReturnsEmptyList_WhenConfigurationIsValid()
        {
            // Arrange
            var configuration = CreateValidConfiguration();

            // Act
            var errors = _validator.GetValidationErrors(configuration);

            // Assert
            Assert.Empty(errors);
        }

        private IConfiguration CreateValidConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AdminUsername"] = "admin",
                    ["AdminPassword"] = "password123",
                    ["CookieSecretKey"] = "this-is-a-secret-key-that-is-at-least-32-characters-long",
                    ["SessionTimeoutMinutes"] = "30"
                })
                .Build();
        }
    }
}
