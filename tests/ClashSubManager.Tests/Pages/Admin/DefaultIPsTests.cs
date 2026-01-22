using ClashSubManager.Pages.Admin;
using ClashSubManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClashSubManager.Tests.Pages.Admin
{
    public class DefaultIPsTests : IDisposable
    {
        private DefaultIPsModel _model;
        private string _testDataPath;

        public DefaultIPsTests()
        {
            _model = new DefaultIPsModel();
            _testDataPath = Path.Combine(Path.GetTempPath(), "ClashSubManagerTests", "DefaultIPs");
            Directory.CreateDirectory(_testDataPath);
            
            // Use reflection to set the private _basePath field
            var basePathField = typeof(DefaultIPsModel).GetField("_basePath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            basePathField?.SetValue(_model, _testDataPath);
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
            Assert.Equal(1, _model.IPRecords.Count);
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
            Assert.Equal(1, _model.IPRecords.Count);
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
            Assert.IsType<RedirectToPageResult>(result);
            var globalIpFile = Path.Combine(_testDataPath, "cloudflare-ip.csv");
            Assert.True(File.Exists(globalIpFile));
            var savedContent = await File.ReadAllTextAsync(globalIpFile);
            Assert.True(savedContent.Contains("1.1.1.1"));
            Assert.True(savedContent.Contains("2.2.2.2"));
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
            Assert.IsType<RedirectToPageResult>(result);
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
            Assert.IsType<RedirectToPageResult>(result);
            Assert.False(File.Exists(globalIpFile));
        }

        [Fact]
        public void ParseCSVContent_WithValidContent_ReturnsIPRecords()
        {
            // Arrange
            var csvContent = "IP Address,Sent,Received,Packet Loss,Average Latency,Download Speed\n1.1.1.1,100,100,0%,10ms,100MB/s\n2.2.2.2,200,200,0%,20ms,200MB/s";

            // Act - Use reflection to call private method
            var parseMethod = typeof(DefaultIPsModel).GetMethod("ParseCSVContent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = parseMethod?.Invoke(_model, new object[] { csvContent }) as List<IPRecord>;

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

            // Act
            var parseMethod = typeof(DefaultIPsModel).GetMethod("ParseCSVContent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = parseMethod?.Invoke(_model, new object[] { csvContent }) as List<IPRecord>;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Count);
        }

        [Fact]
        public void ParseLineToIPRecord_WithValidLine_ReturnsIPRecord()
        {
            // Arrange
            var line = "1.1.1.1,100,100,0%,10ms,100MB/s";

            // Act
            var parseMethod = typeof(DefaultIPsModel).GetMethod("ParseLineToIPRecord", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = parseMethod?.Invoke(_model, new object[] { line }) as IPRecord;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1.1.1.1", result.IPAddress);
            Assert.Equal("100", result.Sent);
            Assert.Equal("100", result.Received);
            Assert.Equal("0%", result.PacketLossRate);
            Assert.Equal("10ms", result.AverageLatency);
            Assert.Equal("100MB/s", result.DownloadSpeed);
        }

        [Fact]
        public void ParseLineToIPRecord_WithInvalidIP_ReturnsNull()
        {
            // Arrange
            var line = "invalid.ip,100,100,0%,10ms,100MB/s";

            // Act
            var parseMethod = typeof(DefaultIPsModel).GetMethod("ParseLineToIPRecord", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = parseMethod?.Invoke(_model, new object[] { line }) as IPRecord;

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void IsValidIP_WithValidIP_ReturnsTrue()
        {
            // Arrange
            var validIP = "1.1.1.1";

            // Act
            var isValidMethod = typeof(DefaultIPsModel).GetMethod("IsValidIP", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = isValidMethod?.Invoke(_model, new object[] { validIP }) as bool?;

            // Assert
            Assert.True(result.HasValue);
            Assert.True(result.Value);
        }

        [Fact]
        public void IsValidIP_WithInvalidIP_ReturnsFalse()
        {
            // Arrange
            var invalidIP = "invalid.ip";

            // Act
            var isValidMethod = typeof(DefaultIPsModel).GetMethod("IsValidIP", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = isValidMethod?.Invoke(_model, new object[] { invalidIP }) as bool?;

            // Assert
            Assert.True(result.HasValue);
            Assert.False(result.Value);
        }
    }
}
