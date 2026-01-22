using ClashSubManager.Pages.Admin;
using ClashSubManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Xunit;

namespace ClashSubManager.Tests.Pages.Admin
{
    public class UserConfigTests : IDisposable
    {
        private UserConfigModel _model;
        private string _testDataPath;

        public UserConfigTests()
        {
            _model = new UserConfigModel();
            _testDataPath = Path.Combine(Path.GetTempPath(), "ClashSubManagerTests", "UserConfig");
            Directory.CreateDirectory(_testDataPath);
            
            // Use reflection to set the private _basePath field
            var basePathField = typeof(UserConfigModel).GetField("_basePath", 
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
        public async Task OnGetAsync_WithoutSelectedUser_LoadsUserList()
        {
            // Arrange
            var usersFile = Path.Combine(_testDataPath, "users.txt");
            await File.WriteAllTextAsync(usersFile, "user1\nuser2\nuser3\n");

            // Act
            var result = await _model.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Equal(3, _model.AvailableUsers.Count);
            Assert.Contains("user1", _model.AvailableUsers);
            Assert.Contains("user2", _model.AvailableUsers);
            Assert.Contains("user3", _model.AvailableUsers);
            Assert.Equal(string.Empty, _model.UserConfig.UserId);
        }

        [Fact]
        public async Task OnGetAsync_WithSelectedUser_LoadsUserConfiguration()
        {
            // Arrange
            var usersFile = Path.Combine(_testDataPath, "users.txt");
            await File.WriteAllTextAsync(usersFile, "user1\n");

            var userDir = Path.Combine(_testDataPath, "user1");
            Directory.CreateDirectory(userDir);
            
            var ipFile = Path.Combine(userDir, "cloudflare-ip.csv");
            await File.WriteAllTextAsync(ipFile, "1.1.1.1,100,100,0%,10ms,100MB/s\n2.2.2.2,200,200,0%,20ms,200MB/s");
            
            var templateFile = Path.Combine(userDir, "clash.yaml");
            await File.WriteAllTextAsync(templateFile, "port: 7890\nsocks-port: 7891");

            _model.SelectedUserId = "user1";

            // Act
            var result = await _model.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Equal("user1", _model.UserConfig.UserId);
            Assert.True(_model.UserConfig.DirectoryExists);
            Assert.True(_model.UserConfig.HasIPConfiguration);
            Assert.True(_model.UserConfig.HasTemplate);
            Assert.Equal(2, _model.UserConfig.IPCount);
        }

        [Fact]
        public async Task OnGetAsync_WithSelectedUserWithoutDirectory_ReturnsEmptyConfiguration()
        {
            // Arrange
            var usersFile = Path.Combine(_testDataPath, "users.txt");
            await File.WriteAllTextAsync(usersFile, "user1\n");

            _model.SelectedUserId = "user1";

            // Act
            var result = await _model.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Equal("user1", _model.UserConfig.UserId);
            Assert.False(_model.UserConfig.DirectoryExists);
            Assert.False(_model.UserConfig.HasIPConfiguration);
            Assert.False(_model.UserConfig.HasTemplate);
        }

        [Fact]
        public async Task OnGetAsync_WithSelectedUserWithOnlyIPConfiguration_ReturnsPartialConfiguration()
        {
            // Arrange
            var usersFile = Path.Combine(_testDataPath, "users.txt");
            await File.WriteAllTextAsync(usersFile, "user1\n");

            var userDir = Path.Combine(_testDataPath, "user1");
            Directory.CreateDirectory(userDir);
            
            var ipFile = Path.Combine(userDir, "cloudflare-ip.csv");
            await File.WriteAllTextAsync(ipFile, "1.1.1.1,100,100,0%,10ms,100MB/s");

            _model.SelectedUserId = "user1";

            // Act
            var result = await _model.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Equal("user1", _model.UserConfig.UserId);
            Assert.True(_model.UserConfig.DirectoryExists);
            Assert.True(_model.UserConfig.HasIPConfiguration);
            Assert.False(_model.UserConfig.HasTemplate);
            Assert.Equal(1, _model.UserConfig.IPCount);
        }

        [Fact]
        public async Task OnPostDeleteUserConfigAsync_WithValidUser_DeletesUserConfiguration()
        {
            // Arrange
            var usersFile = Path.Combine(_testDataPath, "users.txt");
            await File.WriteAllTextAsync(usersFile, "user1\n");

            var userDir = Path.Combine(_testDataPath, "user1");
            Directory.CreateDirectory(userDir);
            
            var ipFile = Path.Combine(userDir, "cloudflare-ip.csv");
            await File.WriteAllTextAsync(ipFile, "1.1.1.1,100,100,0%,10ms,100MB/s");
            
            var templateFile = Path.Combine(userDir, "clash.yaml");
            await File.WriteAllTextAsync(templateFile, "port: 7890");

            _model.SelectedUserId = "user1";

            // Act
            var result = await _model.OnPostDeleteUserConfigAsync();

            // Assert
            Assert.IsType<RedirectToPageResult>(result);
            Assert.False(Directory.Exists(userDir));
        }

        [Fact]
        public async Task OnPostDeleteUserConfigAsync_WithoutSelectedUser_ReturnsValidationError()
        {
            // Arrange
            _model.SelectedUserId = "";

            // Act
            var result = await _model.OnPostDeleteUserConfigAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(_model.ModelState.IsValid);
            Assert.Contains("Please select a user", _model.ModelState[string.Empty]?.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task OnPostDeleteUserConfigAsync_WithNonExistentUser_ReturnsSuccess()
        {
            // Arrange
            var usersFile = Path.Combine(_testDataPath, "users.txt");
            await File.WriteAllTextAsync(usersFile, "user1\n");

            _model.SelectedUserId = "nonexistent";

            // Act
            var result = await _model.OnPostDeleteUserConfigAsync();

            // Assert
            Assert.IsType<RedirectToPageResult>(result);
            // Should succeed even if user directory doesn't exist
        }

        [Fact]
        public async Task LoadUserConfigurationAsync_WithEmptyUserId_ReturnsEmptyConfiguration()
        {
            // Arrange
            _model.SelectedUserId = "";

            // Act
            var loadMethod = typeof(UserConfigModel).GetMethod("LoadUserConfigurationAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = loadMethod?.Invoke(_model, new object[] { }) as Task;
            if (task != null)
            {
                await task;
            }

            // Assert
            Assert.NotNull(_model.UserConfig);
            Assert.Equal(string.Empty, _model.UserConfig.UserId);
        }

        [Fact]
        public async Task LoadUserConfigurationAsync_WithValidUser_LoadsFileInformation()
        {
            // Arrange
            var usersFile = Path.Combine(_testDataPath, "users.txt");
            await File.WriteAllTextAsync(usersFile, "user1\n");

            var userDir = Path.Combine(_testDataPath, "user1");
            Directory.CreateDirectory(userDir);
            
            var ipFile = Path.Combine(userDir, "cloudflare-ip.csv");
            var ipContent = "1.1.1.1,100,100,0%,10ms,100MB/s\n2.2.2.2,200,200,0%,20ms,200MB/s";
            await File.WriteAllTextAsync(ipFile, ipContent);
            
            var templateFile = Path.Combine(userDir, "clash.yaml");
            await File.WriteAllTextAsync(templateFile, "port: 7890\nsocks-port: 7891");

            _model.SelectedUserId = "user1";

            // Act
            var loadMethod = typeof(UserConfigModel).GetMethod("LoadUserConfigurationAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = loadMethod?.Invoke(_model, new object[] { }) as Task;
            if (task != null)
            {
                await task;
            }

            // Assert
            Assert.NotNull(_model.UserConfig);
            Assert.Equal("user1", _model.UserConfig.UserId);
            Assert.True(_model.UserConfig.DirectoryExists);
            Assert.True(_model.UserConfig.HasIPConfiguration);
            Assert.True(_model.UserConfig.HasTemplate);
            Assert.Equal(2, _model.UserConfig.IPCount);
            Assert.True(_model.UserConfig.IPFileSize > 0);
            Assert.True(_model.UserConfig.TemplateFileSize > 0);
            Assert.True(_model.UserConfig.IPFileLastModified > DateTime.MinValue);
            Assert.True(_model.UserConfig.TemplateFileLastModified > DateTime.MinValue);
        }

        [Fact]
        public async Task DeleteUserConfigurationAsync_WithExistingUser_ReturnsTrue()
        {
            // Arrange
            var userDir = Path.Combine(_testDataPath, "user1");
            Directory.CreateDirectory(userDir);
            
            var ipFile = Path.Combine(userDir, "cloudflare-ip.csv");
            await File.WriteAllTextAsync(ipFile, "1.1.1.1,100,100,0%,10ms,100MB/s");

            // Act
            var deleteMethod = typeof(UserConfigModel).GetMethod("DeleteUserConfigurationAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = deleteMethod?.Invoke(_model, new object[] { "user1" }) as Task<bool>;
            var result = task != null ? await task : false;

            // Assert
            Assert.True(result);
            Assert.False(Directory.Exists(userDir));
        }

        [Fact]
        public async Task DeleteUserConfigurationAsync_WithNonExistentUser_ReturnsTrue()
        {
            // Arrange - no user directory created

            // Act
            var deleteMethod = typeof(UserConfigModel).GetMethod("DeleteUserConfigurationAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = deleteMethod?.Invoke(_model, new object[] { "nonexistent" }) as Task<bool>;
            var result = task != null ? await task : false;

            // Assert
            Assert.True(result);
        }
    }
}
