using ClashSubManager.Models;
using ClashSubManager.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClashSubManager.Tests.Services
{
    /// <summary>
    /// ValidationService unit tests
    /// </summary>
    public class ValidationServiceTests
    {
        private readonly ValidationService _validationService;
        private readonly Mock<ILogger<ValidationService>> _loggerMock;
        private readonly Mock<CloudflareIPParserService> _ipParserMock;

        public ValidationServiceTests()
        {
            _loggerMock = new Mock<ILogger<ValidationService>>();
            _ipParserMock = new Mock<CloudflareIPParserService>();
            _validationService = new ValidationService(_ipParserMock.Object, _loggerMock.Object);
        }

        [Theory]
        [InlineData("user123")]
        [InlineData("a")]
        [InlineData("user_123")]
        [InlineData("user-123")]
        [InlineData("1234567890123456789012345678901234567890123456789012345678901234")]
        public void ValidateUserId_ValidUserIds_ReturnsTrue(string userId)
        {
            // Act
            var result = _validationService.ValidateUserId(userId);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("user@123")]
        [InlineData("user.123")]
        [InlineData("user 123")]
        [InlineData("12345678901234567890123456789012345678901234567890123456789012345")]
        public void ValidateUserId_InvalidUserIds_ReturnsFalse(string userId)
        {
            // Act
            var result = _validationService.ValidateUserId(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateUserId_NullUserId_ReturnsFalse()
        {
            // Act
            var result = _validationService.ValidateUserId(null!);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("192.168.1.1")]
        [InlineData("8.8.8.8")]
        [InlineData("1.1.1.1")]
        [InlineData("255.255.255.255")]
        [InlineData("127.0.0.1")]
        public void ValidateIPv4Address_ValidIPs_ReturnsTrue(string ipAddress)
        {
            // Act
            var result = _validationService.ValidateIPv4Address(ipAddress);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("256.168.1.1")]
        [InlineData("192.168.1")]
        [InlineData("invalid.ip")]
        [InlineData("192.168.1.1.1")]
        public void ValidateIPv4Address_InvalidIPs_ReturnsFalse(string ipAddress)
        {
            // Act
            var result = _validationService.ValidateIPv4Address(ipAddress);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateIPv4Address_NullIP_ReturnsFalse()
        {
            // Act
            var result = _validationService.ValidateIPv4Address(null!);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("https://example.com/subscribe")]
        [InlineData("http://localhost:8080/config")]
        [InlineData("https://api.example.com/v1/subscription")]
        public void ValidateSubscriptionUrl_ValidUrls_ReturnsTrue(string url)
        {
            // Act
            var result = _validationService.ValidateSubscriptionUrl(url);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-url")]
        [InlineData("ftp://example.com/subscribe")]
        public void ValidateSubscriptionUrl_InvalidUrls_ReturnsFalse(string url)
        {
            // Act
            var result = _validationService.ValidateSubscriptionUrl(url);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSubscriptionUrl_NullUrl_ReturnsFalse()
        {
            // Act
            var result = _validationService.ValidateSubscriptionUrl(null!);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(80)]
        [InlineData(443)]
        [InlineData(8080)]
        [InlineData(65535)]
        public void ValidatePort_ValidPorts_ReturnsTrue(int port)
        {
            // Act
            var result = _validationService.ValidatePort(port);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        [InlineData(100000)]
        public void ValidatePort_InvalidPorts_ReturnsFalse(int port)
        {
            // Act
            var result = _validationService.ValidatePort(port);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0.1)]
        [InlineData(50)]
        [InlineData(99.9)]
        [InlineData(100)]
        public void ValidatePacketLoss_ValidValues_ReturnsTrue(decimal packetLoss)
        {
            // Act
            var result = _validationService.ValidatePacketLoss(packetLoss);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(-1)]
        [InlineData(100.1)]
        [InlineData(200)]
        public void ValidatePacketLoss_InvalidValues_ReturnsFalse(decimal packetLoss)
        {
            // Act
            var result = _validationService.ValidatePacketLoss(packetLoss);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0.1)]
        [InlineData(100)]
        [InlineData(5000)]
        [InlineData(9999.99)]
        public void ValidateLatency_ValidValues_ReturnsTrue(decimal latency)
        {
            // Act
            var result = _validationService.ValidateLatency(latency);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(-1)]
        [InlineData(10000)]
        [InlineData(99999.99)]
        public void ValidateLatency_InvalidValues_ReturnsFalse(decimal latency)
        {
            // Act
            var result = _validationService.ValidateLatency(latency);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateIPRecord_ValidRecord_ReturnsTrue()
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = "1.1.1.1",
                Port = 443,
                PacketLoss = 5.5m,
                Latency = 100.25m
            };

            // Act
            var result = _validationService.ValidateIPRecord(ipRecord);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateIPRecord_NullRecord_ReturnsFalse()
        {
            // Act
            var result = _validationService.ValidateIPRecord(null!);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("invalid.ip", 443, 0, 50)]
        [InlineData("1.1.1.1", 0, 0, 50)]
        [InlineData("1.1.1.1", 443, -1, 50)]
        [InlineData("1.1.1.1", 443, 0, -1)]
        public void ValidateIPRecord_InvalidRecords_ReturnsFalse(string ip, int port, decimal packetLoss, decimal latency)
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = ip,
                Port = port,
                PacketLoss = packetLoss,
                Latency = latency
            };

            // Act
            var result = _validationService.ValidateIPRecord(ipRecord);

            // Assert
            Assert.False(result);
        }

        
        [Fact]
        public void ParseCSVContent_ValidCSV_ReturnsIPRecords()
        {
            // Arrange
            var csvContent = @"1.1.1.1,443,0,50
8.8.8.8,8080,5.5,100
# This is a comment
192.168.1.1,443,10,150";

            // Act
            var result = _validationService.ParseCSVContent(csvContent);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("1.1.1.1", result[0].IPAddress);
            Assert.Equal(443, result[0].Port);
            Assert.Equal("8.8.8.8", result[1].IPAddress);
            Assert.Equal(8080, result[1].Port);
            Assert.Equal("192.168.1.1", result[2].IPAddress);
        }

        [Fact]
        public void ParseCSVContent_EmptyContent_ReturnsEmptyList()
        {
            // Arrange
            var csvContent = "";

            // Act
            var result = _validationService.ParseCSVContent(csvContent);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ParseCSVContent_InvalidLines_SkipsInvalidRecords()
        {
            // Arrange
            var csvContent = @"1.1.1.1,443,0,50
invalid.line
8.8.8.8,8080,5.5,100
256.256.256.256,443,0,50";

            // Act
            var result = _validationService.ParseCSVContent(csvContent);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("1.1.1.1", result[0].IPAddress);
            Assert.Equal("8.8.8.8", result[1].IPAddress);
        }

        [Fact]
        public void ConvertToCSV_ValidIPRecords_ReturnsCSVString()
        {
            // Arrange
            var ipRecords = new List<IPRecord>
            {
                new IPRecord { IPAddress = "1.1.1.1", Port = 443, PacketLoss = 0, Latency = 50 },
                new IPRecord { IPAddress = "8.8.8.8", Port = 8080, PacketLoss = 5.5m, Latency = 100 }
            };

            // Act
            var result = _validationService.ConvertToCSV(ipRecords);

            // Assert
            var lines = result.Split('\n');
            Assert.Equal(2, lines.Length);
            Assert.Contains("1.1.1.1,443,0,50", result);
            Assert.Contains("8.8.8.8,8080,5.5,100", result);
        }

        [Fact]
        public void ConvertToCSV_EmptyList_ReturnsEmptyString()
        {
            // Arrange
            var ipRecords = new List<IPRecord>();

            // Act
            var result = _validationService.ConvertToCSV(ipRecords);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData(1024, 1)] // 1KB
        [InlineData(1048576, 1)] // 1MB
        [InlineData(10485760, 10)] // 10MB
        public void ValidateFileSize_ValidSizes_ReturnsTrue(long fileSize, int maxSizeMB)
        {
            // Act
            var result = _validationService.ValidateFileSize(fileSize, maxSizeMB);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(0, 10)] // 0 bytes
        [InlineData(-1, 10)] // Negative size
        [InlineData(10485761, 10)] // Over 10MB
        public void ValidateFileSize_InvalidSizes_ReturnsFalse(long fileSize, int maxSizeMB)
        {
            // Act
            var result = _validationService.ValidateFileSize(fileSize, maxSizeMB);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("proxies:\n  - name: test")]
        [InlineData("key: value")]
        [InlineData("array:\n  - item1\n  - item2")]
        public void ValidateYAMLContent_ValidYAML_ReturnsTrue(string yamlContent)
        {
            // Act
            var result = _validationService.ValidateYAMLContent(yamlContent);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("invalid: yaml: content: [")]
        public void ValidateYAMLContent_InvalidYAML_ReturnsFalse(string? yamlContent)
        {
            // Act
            var result = _validationService.ValidateYAMLContent(yamlContent);

            // Assert
            Assert.False(result);
        }

        // New branch coverage test - boundary conditions
        [Theory]
        [InlineData("valid_user123")]
        [InlineData("user-456")]
        [InlineData("test_user_789")]
        [InlineData("a")]
        [InlineData("Z")]
        [InlineData("9")]
        public void ValidateUserId_WithValidCharacters_ReturnsTrue(string userId)
        {
            // Act
            var result = _validationService.ValidateUserId(userId);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("192.168.1.1", "-1")]
        [InlineData("192.168.1.1", "999999")]
        [InlineData("192.168.1.1", "-100")]
        public void ValidateIPRecord_WithInvalidLatency_ReturnsFalse(string ipAddress, string latency)
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = ipAddress,
                Port = 443,
                PacketLoss = 0,
                Latency = decimal.TryParse(latency, out var lat) ? lat : -1
            };

            // Act
            var result = _validationService.ValidateIPRecord(ipRecord);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-url")]
        [InlineData("ftp://example.com/subscribe")]
        [InlineData("mailto:test@example.com")]
        [InlineData("file:///path/to/file")]
        public void ValidateSubscriptionUrl_WithNullOrInvalidUrl_ReturnsFalse(string url)
        {
            // Act
            var result = _validationService.ValidateSubscriptionUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ValidateYAMLContent_WithEmptyYaml_ReturnsFalse(string yamlContent)
        {
            // Act
            var result = _validationService.ValidateYAMLContent(yamlContent);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("key: value")]
        [InlineData("array:\n  - item1\n  - item2")]
        [InlineData("nested:\n  key: value\n  number: 123")]
        [InlineData("list:\n  - name: test\n    value: 123")]
        public void ValidateYAMLContent_WithValidYaml_ReturnsTrue(string yamlContent)
        {
            // Act
            var result = _validationService.ValidateYAMLContent(yamlContent);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateIPRecord_WithNullIP_ReturnsFalse()
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = null!,
                Port = 443,
                PacketLoss = 0,
                Latency = 50
            };

            // Act
            var result = _validationService.ValidateIPRecord(ipRecord);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateIPRecord_WithEmptyIP_ReturnsFalse()
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = "",
                Port = 443,
                PacketLoss = 0,
                Latency = 50
            };

            // Act
            var result = _validationService.ValidateIPRecord(ipRecord);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateIPRecord_WithWhitespaceIP_ReturnsFalse()
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = "   ",
                Port = 443,
                PacketLoss = 0,
                Latency = 50
            };

            // Act
            var result = _validationService.ValidateIPRecord(ipRecord);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ParseCSVContent_WithNullContent_ReturnsEmptyList()
        {
            // Arrange
            string csvContent = null;

            // Act
            var result = _validationService.ParseCSVContent(csvContent);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ConvertToCSV_WithNullList_ReturnsEmptyString()
        {
            // Arrange
            List<IPRecord> ipRecords = null;

            // Act
            var result = _validationService.ConvertToCSV(ipRecords);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData(0, 10)] // 0 bytes
        [InlineData(-1, 10)] // Negative size
        [InlineData(-100, 10)] // Large negative size
        public void ValidateFileSize_WithNonPositiveSize_ReturnsFalse(long fileSize, int maxSizeMB)
        {
            // Act
            var result = _validationService.ValidateFileSize(fileSize, maxSizeMB);

            // Assert
            Assert.False(result);
        }
    }
}
