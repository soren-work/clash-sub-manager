using ClashSubManager.Services;
using Xunit;
using System.Reflection;

namespace ClashSubManager.Tests.Services
{
    /// <summary>
    /// PathResolver unit tests
    /// </summary>
    public class PathResolverTests
    {
        private readonly MockEnvironmentDetector _mockDetector;
        private readonly PathResolver _resolver;

        public PathResolverTests()
        {
            _mockDetector = new MockEnvironmentDetector();
            _resolver = new PathResolver(_mockDetector);
        }

        [Fact]
        public void ResolvePath_ReturnsAbsolutePath_WhenGivenAbsolutePath()
        {
            // Arrange
            var absolutePath = "C:\\test\\path";
            if (!OperatingSystem.IsWindows())
            {
                absolutePath = "/test/path";
            }

            // Act
            var result = _resolver.ResolvePath(absolutePath);

            // Assert
            Assert.Equal(absolutePath, result);
        }

        [Fact]
        public void ResolvePath_ReturnsCombinedPath_WhenGivenRelativePath()
        {
            // Arrange
            var relativePath = "data";
            var expectedPath = Path.Combine(_resolver.GetAssemblyDirectory(), relativePath);

            // Act
            var result = _resolver.ResolvePath(relativePath);

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void GetDefaultDataPath_ReturnsDockerPath_WhenInDockerEnvironment()
        {
            // Arrange
            _mockDetector.SetIsDockerEnvironment(true);

            // Act
            var result = _resolver.GetDefaultDataPath();

            // Assert
            Assert.Equal("/app/data", result);
        }

        [Fact]
        public void GetDefaultDataPath_ReturnsLocalPath_WhenNotInDockerEnvironment()
        {
            // Arrange
            _mockDetector.SetIsDockerEnvironment(false);
            var expectedPath = Path.Combine(_resolver.GetAssemblyDirectory(), "data");

            // Act
            var result = _resolver.GetDefaultDataPath();

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void IsValidPath_ReturnsTrue_WhenPathIsValid()
        {
            // Arrange
            var validPath = Path.GetTempPath();
            var testSubPath = Path.Combine(validPath, "test_valid_path");

            // Act
            var result = _resolver.IsValidPath(testSubPath);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(testSubPath));
            
            // Cleanup
            if (Directory.Exists(testSubPath))
            {
                Directory.Delete(testSubPath);
            }
        }

        [Fact]
        public void IsValidPath_ReturnsFalse_WhenPathIsInvalid()
        {
            // Arrange
            var invalidPath = "";
            if (OperatingSystem.IsWindows())
            {
                invalidPath = "Z:\\invalid\\drive\\path";
            }
            else
            {
                invalidPath = "/invalid/path/that/cannot/be/created";
            }

            // Act
            var result = _resolver.IsValidPath(invalidPath);

            // Assert
            Assert.False(result);
        }
    }

    /// <summary>
    /// Extension method for PathResolver to get assembly directory for testing
    /// </summary>
    public static class PathResolverExtensions
    {
        public static string GetAssemblyDirectory(this PathResolver resolver)
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        }
    }
}
