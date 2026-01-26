using ClashSubManager.Pages.Admin;
using ClashSubManager.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace ClashSubManager.Tests.Pages.Admin
{
    public class ClashTemplateTests : IDisposable
    {
        private ClashTemplateModel _model;
        private string _testDataPath;
        private Mock<IConfigurationService> _mockConfigService;
        private Mock<ILogger<ClashTemplateModel>> _mockLogger;
        private Mock<IStringLocalizer<SharedResources>> _mockLocalizer;
        private FileLockProvider _fileLockProvider;

        public ClashTemplateTests()
        {
            _testDataPath = Path.Combine(Path.GetTempPath(), "ClashSubManagerTests", "ClashTemplate");
            Directory.CreateDirectory(_testDataPath);
            
            _mockConfigService = new Mock<IConfigurationService>();
            _mockConfigService.Setup(x => x.GetDataPath()).Returns(_testDataPath);
            
            _mockLogger = new Mock<ILogger<ClashTemplateModel>>();
            _mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();
            _fileLockProvider = new FileLockProvider();
            
            // Setup localizer mock to return non-null values
            _mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns(new LocalizedString("test", "test"));
            _mockLocalizer.Setup(l => l["InvalidYAMLContent"]).Returns(new LocalizedString("InvalidYAMLContent", "Invalid YAML content"));
            _mockLocalizer.Setup(l => l["YAMLContentRequired"]).Returns(new LocalizedString("YAMLContentRequired", "YAML content is required"));
            _mockLocalizer.Setup(l => l["FileSizeExceedsLimit"]).Returns(new LocalizedString("FileSizeExceedsLimit", "File size exceeds the 1MB limit"));
            _mockLocalizer.Setup(l => l["PleaseSelectFileToUpload"]).Returns(new LocalizedString("PleaseSelectFileToUpload", "Please select a file to upload"));
            _mockLocalizer.Setup(l => l["InvalidYAMLFile"]).Returns(new LocalizedString("InvalidYAMLFile", "Invalid YAML file format"));
            
            _model = new ClashTemplateModel(_mockConfigService.Object, _fileLockProvider, _mockLocalizer.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDataPath))
            {
                Directory.Delete(_testDataPath, true);
            }
        }

        [Fact]
        public async Task OnGetAsync_WithoutSelectedUser_LoadsGlobalTemplate()
        {
            // Arrange
            var usersFile = Path.Combine(_testDataPath, "users.txt");
            await File.WriteAllTextAsync(usersFile, "user1\nuser2\n");

            var globalTemplateFile = Path.Combine(_testDataPath, "clash.yaml");
            var yamlContent = @"port: 7890
socks-port: 7891
mixed-port: 7892
allow-lan: true
mode: rule
log-level: info
external-controller: 127.0.0.1:9090";
            await File.WriteAllTextAsync(globalTemplateFile, yamlContent);

            // Act
            var result = await _model.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Equal(2, _model.AvailableUsers.Count);
            Assert.True(_model.FileExists);
            Assert.Contains("port: 7890", _model.YAMLContent);
        }

        [Fact]
        public async Task OnGetAsync_WithSelectedUser_LoadsUserTemplate()
        {
            // Arrange
            var usersFile = Path.Combine(_testDataPath, "users.txt");
            await File.WriteAllTextAsync(usersFile, "user1\n");

            var userDir = Path.Combine(_testDataPath, "user1");
            Directory.CreateDirectory(userDir);
            var userTemplateFile = Path.Combine(userDir, "clash.yaml");
            var yamlContent = @"port: 8080
socks-port: 8081
mixed-port: 8082
allow-lan: false
mode: global
log-level: warning";
            await File.WriteAllTextAsync(userTemplateFile, yamlContent);

            _model.SelectedUserId = "user1";

            // Act
            var result = await _model.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.True(_model.FileExists);
            Assert.Contains("port: 8080", _model.YAMLContent);
        }

        [Fact]
        public async Task OnPostSaveAsync_WithValidYAML_SavesTemplate()
        {
            // Arrange
            var yamlContent = @"port: 7890
socks-port: 7891
mixed-port: 7892
allow-lan: true
mode: rule
log-level: info";
            _model.EditedContent = yamlContent;
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostSaveAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            var globalTemplateFile = Path.Combine(_testDataPath, "clash.yaml");
            Assert.True(File.Exists(globalTemplateFile));
            var savedContent = await File.ReadAllTextAsync(globalTemplateFile);
            Assert.Contains("port: 7890", savedContent);
        }

        [Fact]
        public async Task OnPostSaveAsync_WithInvalidYAML_ReturnsValidationError()
        {
            // Arrange
            var invalidYaml = @"port: 7890
  invalid_indentation: true
unbalanced: [";
            _model.EditedContent = invalidYaml;
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostSaveAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(_model.ModelState.IsValid);
        }

        [Fact]
        public async Task OnPostSaveAsync_WithEmptyContent_ReturnsValidationError()
        {
            // Arrange
            _model.EditedContent = "";
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostSaveAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(_model.ModelState.IsValid);
        }

        [Fact]
        public async Task OnPostUploadAsync_WithValidYAMLFile_SavesTemplate()
        {
            // Arrange
            var yamlContent = @"port: 7890
socks-port: 7891
allow-lan: true
mode: rule";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(yamlContent));
            var file = new FormFile(stream, 0, stream.Length, "file", "template.yaml")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/x-yaml"
            };
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostUploadAsync(file);

            // Assert
            Assert.IsType<PageResult>(result);
            var globalTemplateFile = Path.Combine(_testDataPath, "clash.yaml");
            Assert.True(File.Exists(globalTemplateFile));
            var savedContent = await File.ReadAllTextAsync(globalTemplateFile);
            Assert.Contains("port: 7890", savedContent);
        }

        [Fact]
        public async Task OnPostUploadAsync_WithInvalidYAMLFile_ReturnsValidationError()
        {
            // Arrange
            var invalidYaml = @"port: 7890
  invalid_indentation: true
unbalanced: [";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidYaml));
            var file = new FormFile(stream, 0, stream.Length, "file", "invalid.yaml")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/x-yaml"
            };
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostUploadAsync(file);

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(_model.ModelState.IsValid);
        }

        [Fact]
        public async Task OnPostUploadAsync_WithLargeFile_ReturnsValidationError()
        {
            // Arrange
            var largeContent = new string('x', 2 * 1024 * 1024); // 2MB
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(largeContent));
            var file = new FormFile(stream, 0, stream.Length, "file", "large.yaml")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/x-yaml"
            };
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostUploadAsync(file);

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(_model.ModelState.IsValid);
        }

        [Fact]
        public async Task OnPostDeleteAsync_WithExistingFile_DeletesTemplate()
        {
            // Arrange
            var globalTemplateFile = Path.Combine(_testDataPath, "clash.yaml");
            await File.WriteAllTextAsync(globalTemplateFile, "port: 7890");
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostDeleteAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(File.Exists(globalTemplateFile));
        }

        [Fact]
        public void IsValidYAML_WithValidYAML_ReturnsTrue()
        {
            // Arrange
            var validYaml = @"port: 7890
socks-port: 7891
allow-lan: true
mode: rule";

            // Act
            var isValidMethod = typeof(ClashTemplateModel).GetMethod("IsValidYAML", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = isValidMethod?.Invoke(_model, new object[] { validYaml }) as bool?;

            // Assert
            Assert.True(result.HasValue);
            Assert.True(result.Value);
        }

        [Fact]
        public void IsValidYAML_WithInvalidYAML_ReturnsFalse()
        {
            // Arrange
            var invalidYaml = @"port: 7890
  invalid_indentation: true
unbalanced: [";

            // Act
            var isValidMethod = typeof(ClashTemplateModel).GetMethod("IsValidYAML", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = isValidMethod?.Invoke(_model, new object[] { invalidYaml }) as bool?;

            // Assert
            Assert.True(result.HasValue);
            Assert.False(result.Value);
        }

        [Fact]
        public void IsValidYAML_WithEmptyContent_ReturnsFalse()
        {
            // Arrange
            var emptyYaml = "";

            // Act
            var isValidMethod = typeof(ClashTemplateModel).GetMethod("IsValidYAML", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = isValidMethod?.Invoke(_model, new object[] { emptyYaml }) as bool?;

            // Assert
            Assert.True(result.HasValue);
            Assert.False(result.Value);
        }

        [Fact]
        public void GetFilePath_WithEmptyUserId_ReturnsGlobalPath()
        {
            // Arrange
            var userId = "";

            // Act
            var getFilePathMethod = typeof(ClashTemplateModel).GetMethod("GetFilePath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = getFilePathMethod?.Invoke(_model, new object[] { userId }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Path.Combine(_testDataPath, "clash.yaml"), result);
        }

        [Fact]
        public void GetFilePath_WithUserId_ReturnsUserPath()
        {
            // Arrange
            var userId = "user1";

            // Act
            var getFilePathMethod = typeof(ClashTemplateModel).GetMethod("GetFilePath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = getFilePathMethod?.Invoke(_model, new object[] { userId }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Path.Combine(_testDataPath, "user1", "clash.yaml"), result);
        }
    }
}
