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
            var response = SubscriptionResponse.CreateSuccess(yamlContent);

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
            var response = SubscriptionResponse.CreateSuccess(yamlContent);

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
            var response = SubscriptionResponse.CreateError(errorMessage);

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
            var response = SubscriptionResponse.CreateSuccess("test yaml");

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
            var response = SubscriptionResponse.CreateSuccess(yamlContent);

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
            var response = SubscriptionResponse.CreateSuccess(null!);

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
    }
}
