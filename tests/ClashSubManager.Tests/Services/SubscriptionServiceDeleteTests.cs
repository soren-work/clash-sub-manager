using ClashSubManager.Models;
using ClashSubManager.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClashSubManager.Tests.Services
{
    /// <summary>
    /// SubscriptionService DELETE endpoint unit tests
    /// </summary>
    public class SubscriptionServiceDeleteTests
    {
        private readonly Mock<IStringLocalizer<SharedResources>> _mockLocalizer;
        private readonly Mock<IUserManagementService> _mockUserManagementService;
        private readonly Mock<FileService> _mockFileService;
        private readonly Mock<ValidationService> _mockValidationService;
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly Mock<ILogger<SubscriptionService>> _mockLogger;
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionServiceDeleteTests()
        {
            _mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();
            _mockUserManagementService = new Mock<IUserManagementService>();
            var mockConfigService = new Mock<IConfigurationService>();
            mockConfigService.Setup(x => x.GetDataPath()).Returns(Path.GetTempPath());
            _mockFileService = new Mock<FileService>(mockConfigService.Object, Mock.Of<ILogger<FileService>>());
            _mockValidationService = new Mock<ValidationService>(Mock.Of<ILogger<ValidationService>>());
            _mockConfigurationService = new Mock<IConfigurationService>();
            _mockLogger = new Mock<ILogger<SubscriptionService>>();

            _subscriptionService = new SubscriptionService(
                _mockLocalizer.Object,
                _mockUserManagementService.Object,
                _mockFileService.Object,
                _mockValidationService.Object,
                _mockConfigurationService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task DeleteUserConfigAsync_InvalidUserId_ReturnsError()
        {
            // Arrange
            var invalidUserId = "";
            _mockValidationService.Setup(x => x.ValidateUserId(invalidUserId)).Returns(false);
            _mockLocalizer.Setup(x => x["InvalidUserId"]).Returns(new LocalizedString("InvalidUserId", "Invalid user ID", false));

            // Act
            var result = await _subscriptionService.DeleteUserConfigAsync(invalidUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid user ID", result.Message);
            Assert.Equal("INVALID_USER_ID", result.ErrorCode);
        }

        [Fact]
        public async Task DeleteUserConfigAsync_UserNotFound_ReturnsError()
        {
            // Arrange
            var userId = "test-user";
            _mockValidationService.Setup(x => x.ValidateUserId(userId)).Returns(true);
            _mockUserManagementService.Setup(x => x.DeleteUserAsync(userId)).ReturnsAsync(false);
            _mockLocalizer.Setup(x => x["UserNotFound"]).Returns(new LocalizedString("UserNotFound", "User not found", false));

            // Act
            var result = await _subscriptionService.DeleteUserConfigAsync(userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found", result.Message);
            Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        public async Task DeleteUserConfigAsync_ExistingUser_ReturnsSuccess()
        {
            // Arrange
            var userId = "test-user";
            _mockValidationService.Setup(x => x.ValidateUserId(userId)).Returns(true);
            _mockUserManagementService.Setup(x => x.DeleteUserAsync(userId)).ReturnsAsync(true);
            _mockLocalizer.Setup(x => x["UserConfigDeleted"]).Returns(new LocalizedString("UserConfigDeleted", "User config deleted successfully", false));

            // Act
            var result = await _subscriptionService.DeleteUserConfigAsync(userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("User config deleted successfully", result.Message);
            _mockUserManagementService.Verify(x => x.DeleteUserAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DeleteUserConfigAsync_ExceptionThrown_ReturnsError()
        {
            // Arrange
            var userId = "test-user";
            _mockValidationService.Setup(x => x.ValidateUserId(userId)).Returns(true);
            _mockUserManagementService.Setup(x => x.DeleteUserAsync(userId)).ThrowsAsync(new Exception("Test exception"));
            _mockLocalizer.Setup(x => x["InternalServerError"]).Returns(new LocalizedString("InternalServerError", "Internal server error", false));

            // Act
            var result = await _subscriptionService.DeleteUserConfigAsync(userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Internal server error", result.Message);
            Assert.Equal("INTERNAL_SERVER_ERROR", result.ErrorCode);
        }
    }
}
