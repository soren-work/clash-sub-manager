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
    /// SubscriptionService POST interface unit tests
    /// </summary>
    public class SubscriptionServicePostTests
    {
        private readonly Mock<IStringLocalizer<SharedResources>> _mockLocalizer;
        private readonly Mock<IUserManagementService> _mockUserManagementService;
        private readonly Mock<FileService> _mockFileService;
        private readonly Mock<ValidationService> _mockValidationService;
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly Mock<ILogger<SubscriptionService>> _mockLogger;
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionServicePostTests()
        {
            _mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();
            _mockUserManagementService = new Mock<IUserManagementService>();
            var mockConfigService = new Mock<IConfigurationService>();
            mockConfigService.Setup(x => x.GetDataPath()).Returns(Path.GetTempPath());
            _mockFileService = new Mock<FileService>(mockConfigService.Object, Mock.Of<CloudflareIPParserService>(), Mock.Of<ILogger<FileService>>());
            _mockValidationService = new Mock<ValidationService>(Mock.Of<CloudflareIPParserService>(), Mock.Of<ILogger<ValidationService>>());
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
        public async Task UpdateUserIPsAsync_InvalidUserId_ReturnsError()
        {
            // Arrange
            var invalidUserId = "";
            var csvContent = "192.168.1.1,443,0,50";
            _mockValidationService.Setup(x => x.ValidateUserId(invalidUserId)).Returns(false);
            _mockLocalizer.Setup(x => x["InvalidUserId"]).Returns(new LocalizedString("InvalidUserId", "Invalid user ID", false));

            // Act
            var result = await _subscriptionService.UpdateUserIPsAsync(invalidUserId, csvContent);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid user ID", result.Message);
            Assert.Equal("INVALID_USER_ID", result.ErrorCode);
        }

        [Fact]
        public async Task UpdateUserIPsAsync_NoValidIPRecords_ReturnsError()
        {
            // Arrange
            var userId = "test-user";
            var csvContent = "invalid-ip-content";
            var emptyIPList = new List<IPRecord>();
            
            _mockValidationService.Setup(x => x.ValidateUserId(userId)).Returns(true);
            _mockValidationService.Setup(x => x.ParseCSVContent(csvContent)).Returns(emptyIPList);
            _mockLocalizer.Setup(x => x["NoValidIPRecordsFound"]).Returns(new LocalizedString("NoValidIPRecordsFound", "No valid IP records found", false));

            // Act
            var result = await _subscriptionService.UpdateUserIPsAsync(userId, csvContent);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("No valid IP records found", result.Message);
            Assert.Equal("NO_VALID_IP_RECORDS_FOUND", result.ErrorCode);
        }

        [Fact]
        public async Task UpdateUserIPsAsync_NewUser_ReturnsSuccess()
        {
            // Arrange
            var userId = "test-user";
            var csvContent = "192.168.1.1,443,0,50\n192.168.1.2,443,0,60";
            var ipRecords = new List<IPRecord>
            {
                new() { IPAddress = "192.168.1.1", Port = 443, PacketLoss = 0, Latency = 50 },
                new() { IPAddress = "192.168.1.2", Port = 443, PacketLoss = 0, Latency = 60 }
            };
            
            _mockValidationService.Setup(x => x.ValidateUserId(userId)).Returns(true);
            _mockValidationService.Setup(x => x.ParseCSVContent(csvContent)).Returns(ipRecords);
            _mockUserManagementService.Setup(x => x.RecordUserAccessAsync(userId)).ReturnsAsync(true);
            _mockFileService.Setup(x => x.SaveUserDedicatedIPsAsync(userId, ipRecords)).ReturnsAsync(true);
            _mockLocalizer.Setup(x => x["UserIPsUpdatedSuccessfully"]).Returns(new LocalizedString("UserIPsUpdatedSuccessfully", "User IPs updated successfully", false));

            // Act
            var result = await _subscriptionService.UpdateUserIPsAsync(userId, csvContent);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("User IPs updated successfully", result.Message);
            _mockUserManagementService.Verify(x => x.RecordUserAccessAsync(userId), Times.Once);
            _mockFileService.Verify(x => x.SaveUserDedicatedIPsAsync(userId, ipRecords), Times.Once);
        }

        [Fact]
        public async Task UpdateUserIPsAsync_ExistingUser_ReturnsSuccess()
        {
            // Arrange
            var userId = "test-user";
            var csvContent = "192.168.1.3,443,0,40";
            var ipRecords = new List<IPRecord>
            {
                new() { IPAddress = "192.168.1.3", Port = 443, PacketLoss = 0, Latency = 40 }
            };
            
            _mockValidationService.Setup(x => x.ValidateUserId(userId)).Returns(true);
            _mockValidationService.Setup(x => x.ParseCSVContent(csvContent)).Returns(ipRecords);
            _mockUserManagementService.Setup(x => x.RecordUserAccessAsync(userId)).ReturnsAsync(true);
            _mockFileService.Setup(x => x.SaveUserDedicatedIPsAsync(userId, ipRecords)).ReturnsAsync(true);
            _mockLocalizer.Setup(x => x["UserIPsUpdatedSuccessfully"]).Returns(new LocalizedString("UserIPsUpdatedSuccessfully", "User IPs updated successfully", false));

            // Act
            var result = await _subscriptionService.UpdateUserIPsAsync(userId, csvContent);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("User IPs updated successfully", result.Message);
            _mockUserManagementService.Verify(x => x.RecordUserAccessAsync(userId), Times.Once);
            _mockFileService.Verify(x => x.SaveUserDedicatedIPsAsync(userId, ipRecords), Times.Once);
        }

        [Fact]
        public async Task UpdateUserIPsAsync_ExceptionThrown_ReturnsError()
        {
            // Arrange
            var userId = "test-user";
            var csvContent = "192.168.1.1,443,0,50";
            _mockValidationService.Setup(x => x.ValidateUserId(userId)).Returns(true);
            _mockValidationService.Setup(x => x.ParseCSVContent(csvContent)).Throws(new Exception("Test exception"));
            _mockLocalizer.Setup(x => x["InternalServerError"]).Returns(new LocalizedString("InternalServerError", "Internal server error", false));

            // Act
            var result = await _subscriptionService.UpdateUserIPsAsync(userId, csvContent);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Internal server error", result.Message);
            Assert.Equal("INTERNAL_SERVER_ERROR", result.ErrorCode);
        }
    }
}
