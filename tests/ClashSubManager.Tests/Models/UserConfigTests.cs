using ClashSubManager.Models;
using Xunit;

namespace ClashSubManager.Tests.Models
{
    /// <summary>
    /// UserConfig model unit tests
    /// </summary>
    public class UserConfigTests
    {
        [Fact]
        public void IsValidUserId_ValidUserId_ReturnsTrue()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "user123",
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidUserId_EmptyUserId_ReturnsFalse(string userId)
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = userId,
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidUserId_NullUserId_ReturnsFalse()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = null!,
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("a")] // Minimum length 1
        [InlineData("user123")] // Contains letters and numbers
        [InlineData("user_123")] // Contains underscore
        [InlineData("user-123")] // Contains hyphen
        [InlineData("1234567890123456789012345678901234567890123456789012345678901234")] // Maximum length 64
        public void IsValidUserId_ValidFormats_ReturnsTrue(string userId)
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = userId,
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("user@123")] // Contains special characters
        [InlineData("user.123")] // Contains dot
        [InlineData("user 123")] // Contains space
        [InlineData("12345678901234567890123456789012345678901234567890123456789012345")] // Exceeds 64 characters
        public void IsValidUserId_InvalidFormats_ReturnsFalse(string userId)
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = userId,
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidSubscriptionUrl_ValidHttpsUrl_ReturnsTrue()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "user123",
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidSubscriptionUrl();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidSubscriptionUrl_ValidHttpUrl_ReturnsTrue()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "user123",
                SubscriptionUrl = "http://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidSubscriptionUrl();

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-url")]
        [InlineData("ftp://example.com/subscribe")]
        public void IsValidSubscriptionUrl_InvalidUrls_ReturnsFalse(string url)
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "user123",
                SubscriptionUrl = url
            };

            // Act
            var result = userConfig.IsValidSubscriptionUrl();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidSubscriptionUrl_NullUrl_ReturnsFalse()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "user123",
                SubscriptionUrl = null!
            };

            // Act
            var result = userConfig.IsValidSubscriptionUrl();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_ValidUserConfig_ReturnsTrue()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "user123",
                SubscriptionUrl = "https://example.com/subscribe",
                DedicatedIPs = new List<IPRecord>
                {
                    new IPRecord { IPAddress = "1.1.1.1", Port = 443, PacketLoss = 0, Latency = 50 }
                }
            };

            // Act
            var result = userConfig.IsValid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValid_InvalidUserId_ReturnsFalse()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "invalid@user",
                SubscriptionUrl = "https://example.com/subscribe",
                DedicatedIPs = new List<IPRecord>()
            };

            // Act
            var result = userConfig.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_InvalidSubscriptionUrl_ReturnsFalse()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "user123",
                SubscriptionUrl = "invalid-url",
                DedicatedIPs = new List<IPRecord>()
            };

            // Act
            var result = userConfig.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_InvalidDedicatedIPs_ReturnsFalse()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "user123",
                SubscriptionUrl = "https://example.com/subscribe",
                DedicatedIPs = new List<IPRecord>
                {
                    new IPRecord { IPAddress = "invalid.ip", Port = 443, PacketLoss = 0, Latency = 50 }
                }
            };

            // Act
            var result = userConfig.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_EmptyDedicatedIPs_ReturnsTrue()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "user123",
                SubscriptionUrl = "https://example.com/subscribe",
                DedicatedIPs = new List<IPRecord>()
            };

            // Act
            var result = userConfig.IsValid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Constructor_DefaultValues_InitializesCorrectly()
        {
            // Act
            var userConfig = new UserConfig();

            // Assert
            Assert.Equal(string.Empty, userConfig.UserId);
            Assert.Equal(string.Empty, userConfig.SubscriptionUrl);
            Assert.NotNull(userConfig.DedicatedIPs);
            Assert.Empty(userConfig.DedicatedIPs);
            Assert.True(userConfig.CreatedAt > DateTime.MinValue);
            Assert.True(userConfig.UpdatedAt > DateTime.MinValue);
        }

        [Fact]
        public void IsValid_MultipleInvalidDedicatedIPs_ReturnsFalse()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "user123",
                SubscriptionUrl = "https://example.com/subscribe",
                DedicatedIPs = new List<IPRecord>
                {
                    new IPRecord { IPAddress = "1.1.1.1", Port = 443, PacketLoss = 0, Latency = 50 },
                    new IPRecord { IPAddress = "invalid.ip", Port = 443, PacketLoss = 0, Latency = 50 }
                }
            };

            // Act
            var result = userConfig.IsValid();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("https://sub.example.com/config")]
        [InlineData("http://localhost:8080/subscribe")]
        [InlineData("https://api.example.com/v1/subscription?token=abc123")]
        public void IsValidSubscriptionUrl_ComplexValidUrls_ReturnsTrue(string url)
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "user123",
                SubscriptionUrl = url
            };

            // Act
            var result = userConfig.IsValidSubscriptionUrl();

            // Assert
            Assert.True(result);
        }

        // Additional branch coverage tests - boundary conditions
        [Theory]
        [InlineData("1234567890123456789012345678901234567890123456789012345678901234")] // 64 characters
        public void IsValidUserId_WithMaxLength64_ReturnsTrue(string userId)
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = userId,
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("12345678901234567890123456789012345678901234567890123456789012345")] // 65 characters
        public void IsValidUserId_WithLength65_ReturnsFalse(string userId)
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = userId,
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("user@invalid")]
        [InlineData("user#invalid")]
        [InlineData("user space")]
        [InlineData("user.invalid")]
        [InlineData("user+invalid")]
        [InlineData("user*invalid")]
        [InlineData("user%invalid")]
        public void IsValidUserId_WithInvalidCharacters_ReturnsFalse(string userId)
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = userId,
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("user_name")]
        [InlineData("user_name123")]
        [InlineData("test_user_123")]
        [InlineData("_user")]
        [InlineData("user_")]
        [InlineData("__user__")]
        public void IsValidUserId_WithUnderscore_ReturnsTrue(string userId)
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = userId,
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("user-name")]
        [InlineData("user-name123")]
        [InlineData("test-user-123")]
        [InlineData("-user")]
        [InlineData("user-")]
        [InlineData("--user--")]
        public void IsValidUserId_WithHyphen_ReturnsTrue(string userId)
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = userId,
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidUserId_WithLength0_ReturnsFalse()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "",
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("a")] // Minimum length 1
        [InlineData("Z")] // Single uppercase letter
        [InlineData("9")] // Single digit
        public void IsValidUserId_WithMinimumLength1_ReturnsTrue(string userId)
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = userId,
                SubscriptionUrl = "https://example.com/subscribe"
            };

            // Act
            var result = userConfig.IsValidUserId();

            // Assert
            Assert.True(result);
        }
    }
}
