using ClashSubManager.Pages.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClashSubManager.Tests.Pages.Admin
{
    public class ClashTemplateTests : IDisposable
    {
        private ClashTemplateModel _model;
        private string _testDataPath;

        public ClashTemplateTests()
        {
            _model = new ClashTemplateModel();
            _testDataPath = Path.Combine(Path.GetTempPath(), "ClashSubManagerTests", "ClashTemplate");
            Directory.CreateDirectory(_testDataPath);
            
            // Use reflection to set the private _basePath field
            var basePathField = typeof(ClashTemplateModel).GetField("_basePath", 
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
            Assert.IsType<RedirectToPageResult>(result);
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
            Assert.IsType<RedirectToPageResult>(result);
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
            Assert.IsType<RedirectToPageResult>(result);
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
