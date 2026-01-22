using ClashSubManager.Models;
using Xunit;

namespace ClashSubManager.Tests.Models
{
    /// <summary>
    /// IPRecord model unit tests
    /// </summary>
    public class IPRecordTests
    {
        [Fact]
        public void IsValidIP_ValidIPv4Address_ReturnsTrue()
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = "192.168.1.1",
                Port = 443,
                PacketLoss = 0,
                Latency = 50.5m
            };

            // Act
            var result = ipRecord.IsValidIP();

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("256.168.1.1")]
        [InlineData("192.168.1.256")]
        [InlineData("invalid.ip")]
        [InlineData("")]
        public void IsValidIP_InvalidIPv4Address_ReturnsFalse(string ipAddress)
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = ipAddress,
                Port = 443,
                PacketLoss = 0,
                Latency = 50.5m
            };

            // Act
            var result = ipRecord.IsValidIP();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_ValidData_ReturnsTrue()
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
            var result = ipRecord.IsValid();

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(65536)]
        public void IsValid_InvalidPort_ReturnsFalse(int port)
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = "1.1.1.1",
                Port = port,
                PacketLoss = 0,
                Latency = 50
            };

            // Act
            var result = ipRecord.IsValid();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void IsValid_InvalidPacketLoss_ReturnsFalse(decimal packetLoss)
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = "1.1.1.1",
                Port = 443,
                PacketLoss = packetLoss,
                Latency = 50
            };

            // Act
            var result = ipRecord.IsValid();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(10000)]
        public void IsValid_InvalidLatency_ReturnsFalse(decimal latency)
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = "1.1.1.1",
                Port = 443,
                PacketLoss = 0,
                Latency = latency
            };

            // Act
            var result = ipRecord.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_InvalidIPAddress_ReturnsFalse()
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = "invalid.ip.address",
                Port = 443,
                PacketLoss = 0,
                Latency = 50
            };

            // Act
            var result = ipRecord.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Constructor_DefaultValues_InitializesCorrectly()
        {
            // Act
            var ipRecord = new IPRecord();

            // Assert
            Assert.Equal(string.Empty, ipRecord.IPAddress);
            Assert.Equal(0, ipRecord.Port);
            Assert.Equal(0m, ipRecord.PacketLoss);
            Assert.Equal(0m, ipRecord.Latency);
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("8.8.8.8")]
        [InlineData("1.1.1.1")]
        [InlineData("255.255.255.255")]
        public void IsValidIP_EdgeCaseValidIPs_ReturnsTrue(string ipAddress)
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = ipAddress,
                Port = 443,
                PacketLoss = 0,
                Latency = 50
            };

            // Act
            var result = ipRecord.IsValidIP();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValid_BoundaryValues_ReturnsTrue()
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = "1.1.1.1",
                Port = 1, // Minimum valid port
                PacketLoss = 0, // Minimum valid packet loss
                Latency = 0 // Minimum valid latency
            };

            // Act
            var result = ipRecord.IsValid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValid_MaxBoundaryValues_ReturnsTrue()
        {
            // Arrange
            var ipRecord = new IPRecord
            {
                IPAddress = "1.1.1.1",
                Port = 65535, // Maximum valid port
                PacketLoss = 100, // Maximum valid packet loss
                Latency = 9999.99m // Maximum valid latency
            };

            // Act
            var result = ipRecord.IsValid();

            // Assert
            Assert.True(result);
        }
    }
}
