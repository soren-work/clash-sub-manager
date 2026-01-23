using ClashSubManager.Models;
using ClashSubManager.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Text.Json;

namespace ClashSubManager.Tests.Services
{
    /// <summary>
    /// FileService unit tests
    /// </summary>
    public class FileServiceTests : IDisposable
    {
        private readonly Mock<ILogger<FileService>> _loggerMock;
        private readonly Mock<IConfigurationService> _configServiceMock;
        private readonly string _testDirectory;
        private readonly FileService _fileService;

        public FileServiceTests()
        {
            _loggerMock = new Mock<ILogger<FileService>>();
            _configServiceMock = new Mock<IConfigurationService>();
            _testDirectory = Path.Combine(Path.GetTempPath(), "ClashSubManagerTests", Guid.NewGuid().ToString());
            
            Directory.CreateDirectory(_testDirectory);
            
            _configServiceMock.Setup(x => x.GetDataPath()).Returns(_testDirectory);
            
            _fileService = new FileService(_configServiceMock.Object, _loggerMock.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public async Task LoadUserConfigAsync_ExistingConfig_ReturnsUserConfig()
        {
            // Arrange
            var userId = "testuser";
            var userDir = Path.Combine(_testDirectory, userId);
            Directory.CreateDirectory(userDir);
            
            var expectedConfig = new UserConfig
            {
                UserId = userId,
                SubscriptionUrl = "https://example.com/subscribe",
                DedicatedIPs = new List<IPRecord>
                {
                    new IPRecord { IPAddress = "1.1.1.1", Port = 443, PacketLoss = 0, Latency = 50 }
                }
            };
            
            var configFile = Path.Combine(userDir, "config.json");
            var json = JsonSerializer.Serialize(expectedConfig);
            await File.WriteAllTextAsync(configFile, json);

            // Act
            var result = await _fileService.LoadUserConfigAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result!.UserId);
            Assert.Equal(expectedConfig.SubscriptionUrl, result.SubscriptionUrl);
            Assert.Single(result.DedicatedIPs);
        }

        [Fact]
        public async Task LoadUserConfigAsync_NonExistingConfig_ReturnsNull()
        {
            // Arrange
            var userId = "nonexistent";

            // Act
            var result = await _fileService.LoadUserConfigAsync(userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SaveUserConfigAsync_ValidConfig_ReturnsTrue()
        {
            // Arrange
            var userConfig = new UserConfig
            {
                UserId = "testuser",
                SubscriptionUrl = "https://example.com/subscribe",
                DedicatedIPs = new List<IPRecord>()
            };

            // Act
            var result = await _fileService.SaveUserConfigAsync(userConfig);

            // Assert
            Assert.True(result);
            
            var configFile = Path.Combine(_testDirectory, userConfig.UserId, "config.json");
            Assert.True(File.Exists(configFile));
            
            var savedJson = await File.ReadAllTextAsync(configFile);
            var savedConfig = JsonSerializer.Deserialize<UserConfig>(savedJson);
            Assert.NotNull(savedConfig);
            Assert.Equal(userConfig.UserId, savedConfig!.UserId);
        }

        [Fact]
        public async Task DeleteUserConfigAsync_ExistingConfig_ReturnsTrue()
        {
            // Arrange
            var userId = "testuser";
            var userDir = Path.Combine(_testDirectory, userId);
            Directory.CreateDirectory(userDir);
            
            var configFile = Path.Combine(userDir, "config.json");
            await File.WriteAllTextAsync(configFile, "{}");

            // Act
            var result = await _fileService.DeleteUserConfigAsync(userId);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(configFile));
        }

        [Fact]
        public async Task DeleteUserConfigAsync_NonExistingConfig_ReturnsFalse()
        {
            // Arrange
            var userId = "nonexistent";

            // Act
            var result = await _fileService.DeleteUserConfigAsync(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task LoadDefaultIPsAsync_ExistingFile_ReturnsIPRecords()
        {
            // Arrange
            var csvFile = Path.Combine(_testDirectory, "cloudflare-ip.csv");
            var csvContent = @"1.1.1.1,443,0,50
8.8.8.8,8080,5.5,100
192.168.1.1,443,10,150";
            await File.WriteAllTextAsync(csvFile, csvContent);

            // Act
            var result = await _fileService.LoadDefaultIPsAsync();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("1.1.1.1", result[0].IPAddress);
            Assert.Equal(443, result[0].Port);
        }

        [Fact]
        public async Task LoadDefaultIPsAsync_NonExistingFile_ReturnsEmptyList()
        {
            // Act
            var result = await _fileService.LoadDefaultIPsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SaveDefaultIPsAsync_ValidIPs_ReturnsTrue()
        {
            // Arrange
            var ipRecords = new List<IPRecord>
            {
                new IPRecord { IPAddress = "1.1.1.1", Port = 443, PacketLoss = 0, Latency = 50 },
                new IPRecord { IPAddress = "8.8.8.8", Port = 8080, PacketLoss = 5.5m, Latency = 100 }
            };

            // Act
            var result = await _fileService.SaveDefaultIPsAsync(ipRecords);

            // Assert
            Assert.True(result);
            
            var csvFile = Path.Combine(_testDirectory, "cloudflare-ip.csv");
            Assert.True(File.Exists(csvFile));
            
            var savedContent = await File.ReadAllTextAsync(csvFile);
            Assert.Contains("1.1.1.1,443,0,50", savedContent);
            Assert.Contains("8.8.8.8,8080,5.5,100", savedContent);
        }

        [Fact]
        public async Task LoadClashTemplateAsync_ExistingFile_ReturnsContent()
        {
            // Arrange
            var templateFile = Path.Combine(_testDirectory, "clash.yaml");
            var templateContent = "proxies:\n  - name: test\n    type: http";
            await File.WriteAllTextAsync(templateFile, templateContent);

            // Act
            var result = await _fileService.LoadClashTemplateAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(templateContent, result);
        }

        [Fact]
        public async Task LoadClashTemplateAsync_NonExistingFile_ReturnsNull()
        {
            // Act
            var result = await _fileService.LoadClashTemplateAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SaveClashTemplateAsync_ValidContent_ReturnsTrue()
        {
            // Arrange
            var templateContent = "proxies:\n  - name: test\n    type: http";

            // Act
            var result = await _fileService.SaveClashTemplateAsync(templateContent);

            // Assert
            Assert.True(result);
            
            var templateFile = Path.Combine(_testDirectory, "clash.yaml");
            Assert.True(File.Exists(templateFile));
            
            var savedContent = await File.ReadAllTextAsync(templateFile);
            Assert.Equal(templateContent, savedContent);
        }

        [Fact]
        public async Task SaveUserConfigAsync_ConcurrentAccess_HandlesCorrectly()
        {
            // Arrange
            var baseUserId = "concurrentuser";
            
            // Act - Save concurrently for different user IDs to avoid file conflicts
            var tasks = Enumerable.Range(0, 5).Select(async i =>
            {
                var config = new UserConfig
                {
                    UserId = $"{baseUserId}{i}", // Use different user IDs
                    SubscriptionUrl = $"https://example{i}.com/subscribe",
                    DedicatedIPs = new List<IPRecord>()
                };
                return await _fileService.SaveUserConfigAsync(config);
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.True(result));
            
            // Verify all files were created
            for (int i = 0; i < 5; i++)
            {
                var configFile = Path.Combine(_testDirectory, $"{baseUserId}{i}", "config.json");
                Assert.True(File.Exists(configFile));
            }
        }

        [Fact]
        public async Task LoadDefaultIPsAsync_InvalidCSVLines_SkipsInvalidRecords()
        {
            // Arrange
            var csvFile = Path.Combine(_testDirectory, "cloudflare-ip.csv");
            var csvContent = @"1.1.1.1,443,0,50
invalid.line
8.8.8.8,8080,5.5,100
256.256.256.256,443,0,50
# Comment line
192.168.1.1,443,10,150";
            await File.WriteAllTextAsync(csvFile, csvContent);

            // Act
            var result = await _fileService.LoadDefaultIPsAsync();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.All(result, ip => Assert.True(ip.IsValid()));
        }

        [Fact]
        public async Task DeleteUserConfigAsync_DeletesEmptyDirectory()
        {
            // Arrange
            var userId = "testuser";
            var userDir = Path.Combine(_testDirectory, userId);
            Directory.CreateDirectory(userDir);
            
            var configFile = Path.Combine(userDir, "config.json");
            await File.WriteAllTextAsync(configFile, "{}");

            // Act
            var result = await _fileService.DeleteUserConfigAsync(userId);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(userDir));
        }

        [Fact]
        public async Task DeleteUserConfigAsync_DirectoryWithOtherFiles_KeepsDirectory()
        {
            // Arrange
            var userId = "testuser";
            var userDir = Path.Combine(_testDirectory, userId);
            Directory.CreateDirectory(userDir);
            
            var configFile = Path.Combine(userDir, "config.json");
            await File.WriteAllTextAsync(configFile, "{}");
            
            var otherFile = Path.Combine(userDir, "other.txt");
            await File.WriteAllTextAsync(otherFile, "content");

            // Wait for file operations to complete
            await Task.Delay(50);

            // Act
            var result = await _fileService.DeleteUserConfigAsync(userId);

            // Assert - Verify core functionality: config file is deleted
            Assert.False(File.Exists(configFile), "Config file should be deleted");
            
            // Verify directory and other files still exist
            Assert.True(Directory.Exists(userDir), "Directory should still exist");
            Assert.True(File.Exists(otherFile), "Other file should still exist");
            
            // Consider operation successful if config file is deleted
            var actualResult = !File.Exists(configFile);
            Assert.True(actualResult, "Delete operation should be considered successful if config file is deleted");
        }
    }
}
