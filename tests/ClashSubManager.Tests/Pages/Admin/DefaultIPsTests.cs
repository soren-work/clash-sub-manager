using ClashSubManager.Pages.Admin;
using ClashSubManager.Services;
using ClashSubManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace ClashSubManager.Tests.Pages.Admin
{
    public class DefaultIPsTests : IDisposable
    {
        private CloudflareIpModel _model;
        private string _testDataPath;
        private Mock<IConfigurationService> _mockConfigService;
        private Mock<CloudflareIPParserService> _mockIpParserService;
        private Mock<ILogger<CloudflareIpModel>> _mockLogger;
        private Mock<IStringLocalizer<SharedResources>> _mockLocalizer;
        private FileLockProvider _fileLockProvider;

        public DefaultIPsTests()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _mockIpParserService = new Mock<CloudflareIPParserService>(Mock.Of<ILogger<CloudflareIPParserService>>());
            _mockLogger = new Mock<ILogger<CloudflareIpModel>>();
            _mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();
            _fileLockProvider = new FileLockProvider();
            
            // Setup localizer mock to return non-null values
            _mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns(new LocalizedString("test", "test"));
            _mockLocalizer.Setup(l => l["PleaseSelectFileToUpload"]).Returns(new LocalizedString("PleaseSelectFileToUpload", "Please select a file to upload"));
            _mockLocalizer.Setup(l => l["FileSizeExceedsLimit"]).Returns(new LocalizedString("FileSizeExceedsLimit", "File size exceeds the 10MB limit"));
            
            _model = new CloudflareIpModel(_mockConfigService.Object, _fileLockProvider, _mockIpParserService.Object, _mockLocalizer.Object, _mockLogger.Object);
            _testDataPath = Path.Combine(Path.GetTempPath(), "ClashSubManagerTests", "DefaultIPs");
            Directory.CreateDirectory(_testDataPath);
            
            // Update mock to return actual test path
            _mockConfigService.Setup(x => x.GetDataPath()).Returns(_testDataPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDataPath))
            {
                Directory.Delete(_testDataPath, true);
            }
        }

        [Fact]
        public async Task OnGetAsync_WithoutSelectedUser_LoadsGlobalConfiguration()
        {
            // Arrange
            var usersFile = Path.Combine(_testDataPath, "users.txt");
            await File.WriteAllTextAsync(usersFile, "user1\nuser2\n");

            var globalIpFile = Path.Combine(_testDataPath, "cloudflare-ip.csv");
            var csvContent = "IP Address,Sent,Received,Packet Loss,Average Latency,Download Speed\n1.1.1.1,100,100,0%,10ms,100MB/s";
            await File.WriteAllTextAsync(globalIpFile, csvContent);

            // Act
            var result = await _model.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Equal(2, _model.AvailableUsers.Count);
            Assert.True(_model.FileExists);
            Assert.Single(_model.IPRecords);
            Assert.Equal("1.1.1.1", _model.IPRecords.First().IPAddress);
        }

        [Fact]
        public async Task OnGetAsync_WithSelectedUser_LoadsUserConfiguration()
        {
            // Arrange
            var usersFile = Path.Combine(_testDataPath, "users.txt");
            await File.WriteAllTextAsync(usersFile, "user1\n");

            var userDir = Path.Combine(_testDataPath, "user1");
            Directory.CreateDirectory(userDir);
            var userIpFile = Path.Combine(userDir, "cloudflare-ip.csv");
            var csvContent = "IP Address,Sent,Received,Packet Loss,Average Latency,Download Speed\n2.2.2.2,200,200,0%,20ms,200MB/s";
            await File.WriteAllTextAsync(userIpFile, csvContent);

            _model.SelectedUserId = "user1";

            // Act
            var result = await _model.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.True(_model.FileExists);
            Assert.Single(_model.IPRecords);
            Assert.Equal("2.2.2.2", _model.IPRecords.First().IPAddress);
        }

        [Fact]
        public async Task OnPostSetIPsAsync_WithValidContent_SavesConfiguration()
        {
            // Arrange
            _model.CSVContent = "1.1.1.1,100,100,0%,10ms,100MB/s\n2.2.2.2,200,200,0%,20ms,200MB/s";
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostSetIPsAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            var globalIpFile = Path.Combine(_testDataPath, "cloudflare-ip.csv");
            Assert.True(File.Exists(globalIpFile));
            var savedContent = await File.ReadAllTextAsync(globalIpFile);
            Assert.Contains("1.1.1.1", savedContent);
            Assert.Contains("2.2.2.2", savedContent);
        }

        [Fact]
        public async Task OnPostSetIPsAsync_WithEmptyContent_ReturnsValidationError()
        {
            // Arrange
            _model.CSVContent = "";
            _model.SelectedUserId = "";
            
            // Manually trigger validation
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(_model);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(_model, validationContext, validationResults, true);
            
            foreach (var result in validationResults)
            {
                _model.ModelState.AddModelError(result.MemberNames.FirstOrDefault() ?? "", result.ErrorMessage ?? "");
            }

            // Act
            var actualResult = await _model.OnPostSetIPsAsync();

            // Assert
            Assert.IsType<PageResult>(actualResult);
            Assert.False(_model.ModelState.IsValid);
        }

        [Fact]
        public async Task OnPostUploadAsync_WithValidFile_SavesConfiguration()
        {
            // Arrange
            var csvContent = "1.1.1.1,100,100,0%,10ms,100MB/s";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));
            var file = new FormFile(stream, 0, stream.Length, "file", "test.csv")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostUploadAsync(file);

            // Assert
            Assert.IsType<PageResult>(result);
            var globalIpFile = Path.Combine(_testDataPath, "cloudflare-ip.csv");
            Assert.True(File.Exists(globalIpFile));
            var savedContent = await File.ReadAllTextAsync(globalIpFile);
            Assert.Contains("1.1.1.1", savedContent);
        }

        [Fact]
        public async Task OnPostUploadAsync_WithLargeFile_ReturnsValidationError()
        {
            // Arrange
            var largeContent = new string('x', 11 * 1024 * 1024); // 11MB
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(largeContent));
            var file = new FormFile(stream, 0, stream.Length, "file", "large.csv")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostUploadAsync(file);

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(_model.ModelState.IsValid);
        }

        [Fact]
        public async Task OnPostDeleteIPsAsync_WithExistingFile_DeletesConfiguration()
        {
            // Arrange
            var globalIpFile = Path.Combine(_testDataPath, "cloudflare-ip.csv");
            await File.WriteAllTextAsync(globalIpFile, "1.1.1.1,100,100,0%,10ms,100MB/s");
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostDeleteIPsAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(File.Exists(globalIpFile));
        }

        [Fact]
        public void ParseCSVContent_WithValidContent_ReturnsIPRecords()
        {
            // Arrange
            var csvContent = "IP Address,Sent,Received,Packet Loss,Average Latency,Download Speed\n1.1.1.1,100,100,0%,10ms,100MB/s\n2.2.2.2,200,200,0%,20ms,200MB/s";

            // Act - Use actual parser service with mocked logger
            var mockLogger = new Mock<ILogger<CloudflareIPParserService>>();
            var parser = new CloudflareIPParserService(mockLogger.Object);
            var result = parser.ParseCSVContent(csvContent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("1.1.1.1", result[0].IPAddress);
            Assert.Equal("100", result[0].Sent);
            Assert.Equal("2.2.2.2", result[1].IPAddress);
            Assert.Equal("200", result[1].Sent);
        }

        [Fact]
        public void ParseCSVContent_WithEmptyContent_ReturnsEmptyList()
        {
            // Arrange
            var csvContent = "";

            // Act - Use actual parser service with mocked logger
            var mockLogger = new Mock<ILogger<CloudflareIPParserService>>();
            var parser = new CloudflareIPParserService(mockLogger.Object);
            var result = parser.ParseCSVContent(csvContent);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ParseLineToIPRecord_WithValidLine_ReturnsIPRecord()
        {
            // Arrange
            var line = "1.1.1.1,100,100,0,10,100MB/s";
            var expectedRecord = new IPRecord { IPAddress = "1.1.1.1", Sent = "100", Received = "100", PacketLoss = 0, Latency = 10, DownloadSpeed = "100MB/s" };

            // Act - Mock the parser service to test individual line parsing
            var mockLogger = new Mock<ILogger<CloudflareIPParserService>>();
            var parser = new CloudflareIPParserService(mockLogger.Object);
            var result = parser.ParseCSVContent(line);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("1.1.1.1", result[0].IPAddress);
        }

        [Fact]
        public void ParseLineToIPRecord_WithInvalidIP_ReturnsNull()
        {
            // Arrange
            var line = "invalid.ip,100,100,0,10,100MB/s";

            // Act
            var mockLogger = new Mock<ILogger<CloudflareIPParserService>>();
            var parser = new CloudflareIPParserService(mockLogger.Object);
            var result = parser.ParseCSVContent(line);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
