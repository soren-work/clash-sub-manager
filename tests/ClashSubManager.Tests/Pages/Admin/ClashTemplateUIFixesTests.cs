using ClashSubManager.Pages.Admin;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Xunit;
using Moq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ClashSubManager.Services;
using Microsoft.Extensions.Logging;

namespace ClashSubManager.Tests.Pages.Admin
{
    public class ClashTemplateUIFixesTests : IDisposable
    {
        private ClashTemplateModel _model;
        private string _testDataPath;
        private Mock<IConfigurationService> _mockConfigService;

        public ClashTemplateUIFixesTests()
        {
            _testDataPath = Path.Combine(Path.GetTempPath(), "ClashSubManagerTests", "ClashTemplateUIFixes");
            Directory.CreateDirectory(_testDataPath);
            
            _mockConfigService = new Mock<IConfigurationService>();
            _mockConfigService.Setup(x => x.GetDataPath()).Returns(_testDataPath);
            
            var mockLogger = new Mock<ILogger<ClashTemplateModel>>();
            _model = new ClashTemplateModel(_mockConfigService.Object, mockLogger.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDataPath))
            {
                Directory.Delete(_testDataPath, true);
            }
        }

        [Fact]
        public async Task OnPostSaveAsync_WithValidYAML_ReturnsPageResult()
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
        public async Task OnGetAsync_LoadsPageDataCorrectly()
        {
            // Arrange
            var yamlContent = @"port: 7890
socks-port: 7891";
            var yamlFile = Path.Combine(_testDataPath, "clash.yaml");
            await File.WriteAllTextAsync(yamlFile, yamlContent);

            // Act
            var result = await _model.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Equal(yamlContent, _model.YAMLContent);
            Assert.True(_model.FileExists);
        }
    }
}
