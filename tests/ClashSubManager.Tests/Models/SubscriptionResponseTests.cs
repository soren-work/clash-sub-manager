using ClashSubManager.Models;
using Xunit;

namespace ClashSubManager.Tests.Models
{
    /// <summary>
    /// SubscriptionResponse model unit tests
    /// </summary>
    public class SubscriptionResponseTests
    {
        [Fact]
        public void CreateSuccess_ValidYamlContent_ReturnsSuccessResponse()
        {
            // Arrange
            var yamlContent = "proxies:\n  - name: test\n    type: http\n    server: example.com";

            // Act
            var response = SubscriptionResponse.CreateSuccessFromYaml(yamlContent);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Subscription generated successfully", response.Message);
            Assert.Equal(yamlContent, response.YAMLContent);
            Assert.Null(response.ErrorCode);
            Assert.True(response.Timestamp > DateTime.MinValue);
        }

        [Fact]
        public void CreateError_ValidMessage_ReturnsErrorResponse()
        {
            // Arrange
            var errorMessage = "Test error message";
            var errorCode = "TEST_ERROR";

            // Act
            var response = SubscriptionResponse.CreateError(errorMessage, errorCode);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Null(response.YAMLContent);
            Assert.Equal(errorCode, response.ErrorCode);
            Assert.True(response.Timestamp > DateTime.MinValue);
        }

        [Fact]
        public void CreateError_WithoutErrorCode_ReturnsErrorResponse()
        {
            // Arrange
            var errorMessage = "Test error message";

            // Act
            var response = SubscriptionResponse.CreateError(errorMessage);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Null(response.YAMLContent);
            Assert.Null(response.ErrorCode);
            Assert.True(response.Timestamp > DateTime.MinValue);
        }

        [Fact]
        public void Constructor_DefaultValues_InitializesCorrectly()
        {
            // Act
            var response = new SubscriptionResponse();

            // Assert
            Assert.False(response.Success); // Default value for bool
            Assert.Equal(string.Empty, response.Message); // Default value for string
            Assert.Null(response.YAMLContent); // Default value for string?
            Assert.Null(response.ErrorCode); // Default value for string?
            Assert.True(response.Timestamp > DateTime.MinValue);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateSuccess_EmptyYamlContent_ReturnsSuccessResponse(string? yamlContent)
        {
            // Act
            var response = SubscriptionResponse.CreateSuccessFromYaml(yamlContent!);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Subscription generated successfully", response.Message);
            Assert.Equal(yamlContent, response.YAMLContent);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateError_EmptyMessage_ReturnsErrorResponse(string? errorMessage)
        {
            // Act
            var response = SubscriptionResponse.CreateError(errorMessage!);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Null(response.YAMLContent);
            Assert.Null(response.ErrorCode);
        }

        [Fact]
        public void Timestamp_AutoSet_IsRecent()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

            // Act
            var response = SubscriptionResponse.CreateSuccess("Test message");

            // Assert
            Assert.True(response.Timestamp >= beforeCreation);
            Assert.True(response.Timestamp <= DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public void CreateSuccess_ComplexYamlContent_HandlesCorrectly()
        {
            // Arrange
            var yamlContent = @"proxies:
  - name: proxy1
    type: http
    server: example.com
    port: 443
  - name: proxy2
    type: ss
    server: example.org
    port: 8080
proxy-groups:
  - name: group1
    type: select
    proxies:
      - proxy1
      - proxy2";

            // Act
            var response = SubscriptionResponse.CreateSuccessFromYaml(yamlContent);

            // Assert
            Assert.True(response.Success);
            Assert.Equal(yamlContent, response.YAMLContent);
            Assert.True(response.Timestamp > DateTime.MinValue);
        }

        [Theory]
        [InlineData("USER_NOT_FOUND")]
        [InlineData("INVALID_INPUT")]
        [InlineData("INTERNAL_ERROR")]
        [InlineData("")]
        public void CreateError_VariousErrorCodes_HandlesCorrectly(string errorCode)
        {
            // Arrange
            var errorMessage = "Error occurred";

            // Act
            var response = SubscriptionResponse.CreateError(errorMessage, errorCode);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal(errorCode, response.ErrorCode);
        }

        [Fact]
        public void CreateSuccess_NullYamlContent_HandlesCorrectly()
        {
            // Act
            var response = SubscriptionResponse.CreateSuccessFromYaml(null!);

            // Assert
            Assert.True(response.Success);
            Assert.Null(response.YAMLContent);
        }

        [Fact]
        public void CreateError_LongErrorMessage_HandlesCorrectly()
        {
            // Arrange
            var longErrorMessage = new string('A', 1000);

            // Act
            var response = SubscriptionResponse.CreateError(longErrorMessage);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(longErrorMessage, response.Message);
            Assert.Null(response.YAMLContent);
        }

        [Fact]
        public void CreateSuccess_WithSubscriptionInfo_ReturnsSuccessResponse()
        {
            // Arrange
            var yamlContent = "proxies:\n  - name: test";
            var uploadBytes = 1024L;
            var downloadBytes = 2048L;
            var totalBytes = 1073741824L;
            var expireTime = DateTime.UtcNow.AddDays(30);
            var profileTitle = "Test Subscription";
            var updateIntervalHours = 24;

            // Act
            var response = SubscriptionResponse.CreateSuccessWithSubscriptionInfo(
                yamlContent, uploadBytes, downloadBytes, totalBytes, expireTime, profileTitle, updateIntervalHours);

            // Assert
            Assert.True(response.Success);
            Assert.Equal(yamlContent, response.YAMLContent);
            Assert.Equal(uploadBytes, response.UploadBytes);
            Assert.Equal(downloadBytes, response.DownloadBytes);
            Assert.Equal(totalBytes, response.TotalBytes);
            Assert.Equal(expireTime, response.ExpireTime);
            Assert.Equal(profileTitle, response.ProfileTitle);
            Assert.Equal(updateIntervalHours, response.UpdateIntervalHours);
        }

        [Fact]
        public void CreateSuccess_WithMinimalSubscriptionInfo_ReturnsSuccessResponse()
        {
            // Arrange
            var yamlContent = "proxies:\n  - name: test";

            // Act
            var response = SubscriptionResponse.CreateSuccessWithSubscriptionInfo(yamlContent);

            // Assert
            Assert.True(response.Success);
            Assert.Equal(yamlContent, response.YAMLContent);
            Assert.Equal(0L, response.UploadBytes);
            Assert.Equal(0L, response.DownloadBytes);
            Assert.Equal(0L, response.TotalBytes);
            Assert.Equal(DateTime.MinValue, response.ExpireTime);
            Assert.Equal(string.Empty, response.ProfileTitle);
            Assert.Equal(24, response.UpdateIntervalHours); // Default value
        }

        [Fact]
        public void SubscriptionUserInfoHeader_FormatsCorrectly()
        {
            // Arrange
            var response = new SubscriptionResponse
            {
                UploadBytes = 1234,
                DownloadBytes = 5678,
                TotalBytes = 10737418240,
                ExpireTime = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc)
            };

            // Act
            var headerValue = response.GetSubscriptionUserInfoHeader();

            // Assert
            Assert.NotNull(headerValue);
            Assert.Contains("upload=1234", headerValue);
            Assert.Contains("download=5678", headerValue);
            Assert.Contains("total=10737418240", headerValue);
            Assert.Contains("expire=1735689599", headerValue); // Unix timestamp
        }

        [Fact]
        public void SubscriptionUserInfoHeader_WithZeroValues_FormatsCorrectly()
        {
            // Arrange
            var response = new SubscriptionResponse
            {
                UploadBytes = 0,
                DownloadBytes = 0,
                TotalBytes = 0,
                ExpireTime = DateTime.MinValue
            };

            // Act
            var headerValue = response.GetSubscriptionUserInfoHeader();

            // Assert
            Assert.NotNull(headerValue);
            Assert.Contains("upload=0", headerValue);
            Assert.Contains("download=0", headerValue);
            Assert.Contains("total=0", headerValue);
            Assert.Contains("expire=0", headerValue); // Updated to expect 0 for DateTime.MinValue
        }

        [Theory]
        [InlineData("My Subscription", "My Subscription")]
        [InlineData("Test 🚀 Subscription", "Test 🚀 Subscription")]
        [InlineData("", "")]
        [InlineData("   ", "   ")]
        public void ProfileTitle_SetAndGet_WorksCorrectly(string title, string expected)
        {
            // Arrange & Act
            var response = new SubscriptionResponse
            {
                ProfileTitle = title
            };

            // Assert
            Assert.Equal(expected, response.ProfileTitle);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(12)]
        [InlineData(24)]
        [InlineData(168)]
        public void UpdateIntervalHours_SetAndGet_WorksCorrectly(int interval)
        {
            // Arrange & Act
            var response = new SubscriptionResponse
            {
                UpdateIntervalHours = interval
            };

            // Assert
            Assert.Equal(interval, response.UpdateIntervalHours);
        }
    }
}
