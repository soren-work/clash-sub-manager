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
    /// SubscriptionService unit tests
    /// </summary>
    public class SubscriptionServiceTests
    {
        private readonly Mock<IStringLocalizer<SubscriptionService>> _mockLocalizer;
        private readonly Mock<IUserManagementService> _mockUserManagementService;
        private readonly Mock<FileService> _mockFileService;
        private readonly Mock<ValidationService> _mockValidationService;
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly Mock<ILogger<SubscriptionService>> _mockLogger;
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionServiceTests()
        {
            _mockLocalizer = new Mock<IStringLocalizer<SubscriptionService>>();
            _mockUserManagementService = new Mock<IUserManagementService>();
            var mockConfigService = new Mock<IConfigurationService>();
            mockConfigService.Setup(x => x.GetDataPath()).Returns(Path.GetTempPath());
            _mockFileService = new Mock<FileService>(mockConfigService.Object, Mock.Of<ILogger<FileService>>());
            _mockValidationService = new Mock<ValidationService>(Mock.Of<ILogger<ValidationService>>());
            _mockConfigurationService = new Mock<IConfigurationService>();
            _mockLogger = new Mock<ILogger<SubscriptionService>>();

            _mockConfigurationService.Setup(x => x.GetDataPath()).Returns(Path.GetTempPath());
            _mockConfigurationService.Setup(x => x.GenerateSubscriptionConfigAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<IPRecord>>(), It.IsAny<List<IPRecord>>()))
                .ReturnsAsync("port: 7890\nproxies: []");

            _subscriptionService = new SubscriptionService(
                _mockLocalizer.Object,
                _mockUserManagementService.Object,
                _mockFileService.Object,
                _mockValidationService.Object,
                _mockConfigurationService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetSubscriptionAsync_InvalidUserId_ReturnsError()
        {
            // Arrange
            var invalidUserId = "";
            _mockValidationService.Setup(x => x.ValidateUserId(invalidUserId)).Returns(false);
            _mockLocalizer.Setup(x => x["InvalidUserId"]).Returns(new LocalizedString("InvalidUserId", "Invalid user ID", false));

            // Act
            var result = await _subscriptionService.GetSubscriptionAsync(invalidUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid user ID", result.Message);
            Assert.Equal("INVALID_USER_ID", result.ErrorCode);
        }

        [Fact]
        public async Task GetSubscriptionAsync_SubscriptionUrlTemplateNotConfigured_ReturnsError()
        {
            // Arrange
            var userId = "test-user";
            _mockValidationService.Setup(x => x.ValidateUserId(userId)).Returns(true);
            _mockUserManagementService.Setup(x => x.GetUserSubscriptionUrlAsync(userId)).ReturnsAsync(string.Empty);
            _mockLocalizer.Setup(x => x["SubscriptionUrlTemplateNotConfigured"])
                .Returns(new LocalizedString("SubscriptionUrlTemplateNotConfigured", "Subscription URL template not configured", false));

            // Act
            var result = await _subscriptionService.GetSubscriptionAsync(userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Subscription URL template not configured", result.Message);
            Assert.Equal("SUBSCRIPTION_URL_TEMPLATE_NOT_CONFIGURED", result.ErrorCode);
        }

        [Fact]
        public async Task GetSubscriptionAsync_TemplateNotFound_ReturnsError()
        {
            // Arrange
            var userId = "test-user";
            var subscriptionUrl = "https://example.com/sub";
            
            _mockValidationService.Setup(x => x.ValidateUserId(userId)).Returns(true);
            _mockUserManagementService.Setup(x => x.GetUserSubscriptionUrlAsync(userId)).ReturnsAsync(subscriptionUrl);
            _mockUserManagementService.Setup(x => x.RecordUserAccessAsync(userId)).ReturnsAsync(true);
            _mockFileService.Setup(x => x.LoadClashTemplateAsync()).ReturnsAsync((string?)null);
            _mockLocalizer.Setup(x => x["TemplateNotFound"]).Returns(new LocalizedString("TemplateNotFound", "Template not found", false));

            // Act
            var result = await _subscriptionService.GetSubscriptionAsync(userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Template not found", result.Message);
            Assert.Equal("TEMPLATE_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        public async Task GetSubscriptionAsync_ValidInput_ReturnsSuccess()
        {
            // Arrange
            var userId = "test-user";
            var subscriptionUrl = "https://example.com/sub";
            var template = "port: 7890\nproxies: []";
            var defaultIPs = new List<IPRecord>();
            var dedicatedIPs = new List<IPRecord>();
            
            _mockValidationService.Setup(x => x.ValidateUserId(userId)).Returns(true);
            _mockUserManagementService.Setup(x => x.GetUserSubscriptionUrlAsync(userId)).ReturnsAsync(subscriptionUrl);
            _mockUserManagementService.Setup(x => x.RecordUserAccessAsync(userId)).ReturnsAsync(true);
            _mockFileService.Setup(x => x.LoadClashTemplateAsync()).ReturnsAsync(template);
            _mockFileService.Setup(x => x.LoadDefaultIPsAsync()).ReturnsAsync(defaultIPs);
            _mockFileService.Setup(x => x.LoadUserDedicatedIPsAsync(userId)).ReturnsAsync(dedicatedIPs);
            
            var expectedYaml = "port: 7890\nproxies: []";
            _mockConfigurationService.Setup(x => x.GenerateSubscriptionConfigAsync(
                template, subscriptionUrl, defaultIPs, dedicatedIPs)).ReturnsAsync(expectedYaml);

            // Act
            var result = await _subscriptionService.GetSubscriptionAsync(userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(expectedYaml, result.YAMLContent);
        }

        [Fact]
        public async Task GetSubscriptionAsync_ExceptionThrown_ReturnsError()
        {
            // Arrange
            var userId = "test-user";
            _mockValidationService.Setup(x => x.ValidateUserId(userId)).Returns(true);
            _mockUserManagementService.Setup(x => x.GetUserSubscriptionUrlAsync(userId)).ThrowsAsync(new Exception("Test exception"));
            _mockLocalizer.Setup(x => x["InternalServerError"]).Returns(new LocalizedString("InternalServerError", "Internal server error", false));

            // Act
            var result = await _subscriptionService.GetSubscriptionAsync(userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Internal server error", result.Message);
            Assert.Equal("INTERNAL_SERVER_ERROR", result.ErrorCode);
        }
    }
}
