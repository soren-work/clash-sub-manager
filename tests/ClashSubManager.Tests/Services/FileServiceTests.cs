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
        private readonly FileService _fileService;
        private readonly Mock<IConfigurationService> _configServiceMock;
        private readonly Mock<CloudflareIPParserService> _ipParserMock;
        private readonly Mock<ILogger<FileService>> _loggerMock;
        private readonly string _testDirectory;

        public FileServiceTests()
        {
            _configServiceMock = new Mock<IConfigurationService>();
            _ipParserMock = new Mock<CloudflareIPParserService>(Mock.Of<ILogger<CloudflareIPParserService>>());
            _loggerMock = new Mock<ILogger<FileService>>();
            
            _testDirectory = Path.Combine(Path.GetTempPath(), "ClashSubManagerTests", Guid.NewGuid().ToString());
            
            Directory.CreateDirectory(_testDirectory);
            
            _configServiceMock.Setup(x => x.GetDataPath()).Returns(_testDirectory);
            
            _fileService = new FileService(_configServiceMock.Object, _ipParserMock.Object, _loggerMock.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public async Task LoadUserDedicatedIPsAsync_NonExistingUser_ReturnsEmptyList()
        {
            // Arrange
            var userId = "nonexistent";

            // Act
            var result = await _fileService.LoadUserDedicatedIPsAsync(userId);

            // Assert
            Assert.Empty(result);
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
        public async Task LoadUserDedicatedIPsAsync_ExistingFile_ReturnsIPRecords()
        {
            // Arrange
            var userId = "testuser";
            var userDir = Path.Combine(_testDirectory, userId);
            Directory.CreateDirectory(userDir);
            
            var csvFile = Path.Combine(userDir, "cloudflare-ip.csv");
            var csvContent = @"1.1.1.1,443,0,50
8.8.8.8,8080,5.5,100";
            await File.WriteAllTextAsync(csvFile, csvContent);

            // Act
            var result = await _fileService.LoadUserDedicatedIPsAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("1.1.1.1", result[0].IPAddress);
        }

        [Fact]
        public async Task SaveUserDedicatedIPsAsync_ValidIPs_ReturnsTrue()
        {
            // Arrange
            var userId = "testuser";
            var ipRecords = new List<IPRecord>
            {
                new IPRecord { IPAddress = "1.1.1.1", Port = 443, PacketLoss = 0, Latency = 50 }
            };

            // Act
            var result = await _fileService.SaveUserDedicatedIPsAsync(userId, ipRecords);

            // Assert
            Assert.True(result);
            
            var userDir = Path.Combine(_testDirectory, userId);
            var csvFile = Path.Combine(userDir, "cloudflare-ip.csv");
            Assert.True(File.Exists(csvFile));
        }

        [Fact]
        public async Task DeleteUserDedicatedIPsAsync_ExistingFile_ReturnsTrue()
        {
            // Arrange
            var userId = "testuser";
            var userDir = Path.Combine(_testDirectory, userId);
            Directory.CreateDirectory(userDir);
            
            var csvFile = Path.Combine(userDir, "cloudflare-ip.csv");
            await File.WriteAllTextAsync(csvFile, "1.1.1.1,443,0,50");

            // Act
            var result = await _fileService.DeleteUserDedicatedIPsAsync(userId);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(csvFile));
        }
    }
}
