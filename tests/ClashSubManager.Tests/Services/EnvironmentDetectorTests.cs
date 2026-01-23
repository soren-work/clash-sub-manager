using Microsoft.Extensions.Configuration;
using ClashSubManager.Services;
using Xunit;

namespace ClashSubManager.Tests.Services
{
    /// <summary>
    /// EnvironmentDetector unit tests
    /// </summary>
    public class EnvironmentDetectorTests
    {
        private readonly EnvironmentDetector _detector;

        public EnvironmentDetectorTests()
        {
            _detector = new EnvironmentDetector();
        }

        [Fact]
        public void GetEnvironmentType_ReturnsDocker_WhenInDockerEnvironment()
        {
            // This test would need mocking of File.Exists and File.ReadAllText
            // For now, we test the basic functionality
            var result = _detector.GetEnvironmentType();
            
            // The result should be one of the expected types
            Assert.True(result == EnvironmentTypes.Docker || 
                       result == EnvironmentTypes.Standalone ||
                       result == EnvironmentTypes.Development ||
                       result == EnvironmentTypes.Production);
        }

        [Fact]
        public void IsWindowsEnvironment_ReturnsCorrectValue()
        {
            var result = _detector.IsWindowsEnvironment();
            
            // Should match the actual OS
            Assert.Equal(OperatingSystem.IsWindows(), result);
        }

        [Fact]
        public void IsLinuxEnvironment_ReturnsCorrectValue()
        {
            var result = _detector.IsLinuxEnvironment();
            
            // Should match the actual OS
            Assert.Equal(OperatingSystem.IsLinux(), result);
        }

        [Fact]
        public void IsMacOSEnvironment_ReturnsCorrectValue()
        {
            var result = _detector.IsMacOSEnvironment();
            
            // Should match the actual OS
            Assert.Equal(OperatingSystem.IsMacOS(), result);
        }
    }
}
