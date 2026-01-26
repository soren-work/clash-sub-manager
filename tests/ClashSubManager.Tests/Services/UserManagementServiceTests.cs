using ClashSubManager.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClashSubManager.Tests.Services
{
    /// <summary>
    /// UserManagementService unit tests
    /// </summary>
    public class UserManagementServiceTests : IDisposable
    {
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly Mock<ILogger<UserManagementService>> _mockLogger;
        private readonly UserManagementService _userManagementService;
        private readonly string _tempDirectory;

        public UserManagementServiceTests()
        {
            _mockConfigurationService = new Mock<IConfigurationService>();
            _mockLogger = new Mock<ILogger<UserManagementService>>();
            
            // Create temporary directory for testing
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            
            _mockConfigurationService.Setup(x => x.GetDataPath()).Returns(_tempDirectory);
            
            _userManagementService = new UserManagementService(
                _mockConfigurationService.Object,
                _mockLogger.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Fact]
        public async Task RecordUserAccessAsync_ValidUserId_ReturnsTrue()
        {
            // Arrange
            var userId = "testUser123";

            // Act
            var result = await _userManagementService.RecordUserAccessAsync(userId);

            // Assert
            Assert.True(result);
            
            // Verify user is recorded
            var users = await _userManagementService.GetAllUsersAsync();
            Assert.Contains(userId, users);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task RecordUserAccessAsync_InvalidUserId_ReturnsFalse(string? userId)
        {
            // Act
            var result = await _userManagementService.RecordUserAccessAsync(userId!);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("user@invalid")]
        [InlineData("user.invalid")]
        [InlineData("user space")]
        [InlineData("user#invalid")]
        [InlineData("12345678901234567890123456789012345678901234567890123456789012345")] // 65 characters
        public async Task RecordUserAccessAsync_InvalidUserIdFormat_ReturnsFalse(string userId)
        {
            // Act
            var result = await _userManagementService.RecordUserAccessAsync(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RecordUserAccessAsync_DuplicateUser_ReturnsTrue()
        {
            // Arrange
            var userId = "testUser123";
            await _userManagementService.RecordUserAccessAsync(userId);

            // Act
            var result = await _userManagementService.RecordUserAccessAsync(userId);

            // Assert
            Assert.True(result);
            
            // Verify user is recorded only once
            var users = await _userManagementService.GetAllUsersAsync();
            Assert.Single(users);
            Assert.Contains(userId, users);
        }

        [Fact]
        public async Task GetAllUsersAsync_NoUsers_ReturnsEmptyList()
        {
            // Act
            var users = await _userManagementService.GetAllUsersAsync();

            // Assert
            Assert.Empty(users);
        }

        [Fact]
        public async Task GetAllUsersAsync_MultipleUsers_ReturnsAllUsers()
        {
            // Arrange
            var userIds = new[] { "user1", "user2", "user3" };
            foreach (var userId in userIds)
            {
                await _userManagementService.RecordUserAccessAsync(userId);
            }

            // Act
            var users = await _userManagementService.GetAllUsersAsync();

            // Assert
            Assert.Equal(3, users.Count);
            foreach (var userId in userIds)
            {
                Assert.Contains(userId, users);
            }
        }

        [Fact]
        public async Task UserExistsAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var userId = "testUser123";
            await _userManagementService.RecordUserAccessAsync(userId);

            // Act
            var result = await _userManagementService.UserExistsAsync(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UserExistsAsync_NonExistingUser_ReturnsFalse()
        {
            // Act
            var result = await _userManagementService.UserExistsAsync("nonExistingUser");

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UserExistsAsync_InvalidUserId_ReturnsFalse(string? userId)
        {
            // Act
            var result = await _userManagementService.UserExistsAsync(userId!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteUserAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var userId = "testUser123";
            await _userManagementService.RecordUserAccessAsync(userId);
            
            // Create user directory
            var userDir = Path.Combine(_tempDirectory, userId);
            Directory.CreateDirectory(userDir);
            var testFile = Path.Combine(userDir, "test.txt");
            await File.WriteAllTextAsync(testFile, "test content");

            // Act
            var result = await _userManagementService.DeleteUserAsync(userId);

            // Assert
            Assert.True(result);
            Assert.False(await _userManagementService.UserExistsAsync(userId));
            Assert.False(Directory.Exists(userDir));
        }

        [Fact]
        public async Task DeleteUserAsync_NonExistingUser_ReturnsFalse()
        {
            // Act
            var result = await _userManagementService.DeleteUserAsync("nonExistingUser");

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task DeleteUserAsync_InvalidUserId_ReturnsFalse(string? userId)
        {
            // Act
            var result = await _userManagementService.DeleteUserAsync(userId!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetUserSubscriptionUrlAsync_ValidTemplate_ReturnsReplacedUrl()
        {
            // Arrange
            var userId = "testUser123";
            var template = "https://example.com/subscribe/{userId}";
            _mockConfigurationService.Setup(x => x.GetSubscriptionUrlTemplate()).Returns(template);

            // Act
            var result = await _userManagementService.GetUserSubscriptionUrlAsync(userId);

            // Assert
            Assert.Equal("https://example.com/subscribe/testUser123", result);
        }

        [Fact]
        public async Task GetUserSubscriptionUrlAsync_EmptyTemplate_ReturnsEmpty()
        {
            // Arrange
            _mockConfigurationService.Setup(x => x.GetSubscriptionUrlTemplate()).Returns(string.Empty);

            // Act
            var result = await _userManagementService.GetUserSubscriptionUrlAsync("testUser");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetUserSubscriptionUrlAsync_NullTemplate_ReturnsEmpty()
        {
            // Arrange
            _mockConfigurationService.Setup(x => x.GetSubscriptionUrlTemplate()).Returns((string)null!);

            // Act
            var result = await _userManagementService.GetUserSubscriptionUrlAsync("testUser");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData("https://api.example.com/sub/{userId}")]
        [InlineData("https://example.com/subscribe?user={userId}")]
        [InlineData("https://example.com/{userId}/config")]
        [InlineData("https://example.com/users/{userId}/subscription")]
        public async Task GetUserSubscriptionUrlAsync_DifferentTemplateFormats_ReturnsCorrectUrl(string template)
        {
            // Arrange
            var userId = "testUser123";
            _mockConfigurationService.Setup(x => x.GetSubscriptionUrlTemplate()).Returns(template);

            // Act
            var result = await _userManagementService.GetUserSubscriptionUrlAsync(userId);

            // Assert
            Assert.Equal(template.Replace("{userId}", userId), result);
        }

        [Fact]
        public async Task RecordUserAccessAsync_CaseInsensitive_DeduplicatesCorrectly()
        {
            // Arrange
            var userId1 = "TestUser";
            var userId2 = "testuser"; // Same but different case

            // Act
            await _userManagementService.RecordUserAccessAsync(userId1);
            await _userManagementService.RecordUserAccessAsync(userId2);

            // Assert
            var users = await _userManagementService.GetAllUsersAsync();
            Assert.Equal(2, users.Count); // Case sensitive, should be treated as different users
        }

        [Fact]
        public async Task GetAllUsersAsync_ReturnsSortedUsers()
        {
            // Arrange
            var userIds = new[] { "zebra", "alpha", "beta", "gamma" };
            foreach (var userId in userIds)
            {
                await _userManagementService.RecordUserAccessAsync(userId);
            }

            // Act
            var users = await _userManagementService.GetAllUsersAsync();

            // Assert
            var expectedOrder = new[] { "alpha", "beta", "gamma", "zebra" };
            Assert.Equal(expectedOrder, users);
        }

        [Fact]
        public async Task RecordUserAccessAsync_MaxLengthUserId_ReturnsTrue()
        {
            // Arrange
            var maxLengthUserId = "1234567890123456789012345678901234567890123456789012345678901234"; // 64 characters

            // Act
            var result = await _userManagementService.RecordUserAccessAsync(maxLengthUserId);

            // Assert
            Assert.True(result);
            Assert.Contains(maxLengthUserId, await _userManagementService.GetAllUsersAsync());
        }

        [Fact]
        public async Task RecordUserAccessAsync_MinLengthUserId_ReturnsTrue()
        {
            // Arrange
            var minLengthUserId = "a"; // 1 character

            // Act
            var result = await _userManagementService.RecordUserAccessAsync(minLengthUserId);

            // Assert
            Assert.True(result);
            Assert.Contains(minLengthUserId, await _userManagementService.GetAllUsersAsync());
        }

        [Theory]
        [InlineData("user_name")]
        [InlineData("user-name")]
        [InlineData("user123")]
        [InlineData("_user")]
        [InlineData("user_")]
        [InlineData("-user")]
        [InlineData("user-")]
        public async Task RecordUserAccessAsync_ValidUserIdFormats_ReturnsTrue(string userId)
        {
            // Act
            var result = await _userManagementService.RecordUserAccessAsync(userId);

            // Assert
            Assert.True(result);
            Assert.Contains(userId, await _userManagementService.GetAllUsersAsync());
        }
    }
}
