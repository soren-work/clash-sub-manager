namespace ClashSubManager.Services
{
    /// <summary>
    /// Environment detector implementation
    /// </summary>
    public class EnvironmentDetector : IEnvironmentDetector
    {
        /// <summary>
        /// Get environment type
        /// </summary>
        /// <returns>Environment type</returns>
        public string GetEnvironmentType()
        {
            if (IsDockerEnvironment())
                return EnvironmentTypes.Docker;
                
            var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return !string.IsNullOrEmpty(aspnetEnv) ? aspnetEnv : EnvironmentTypes.Standalone;
        }

        /// <summary>
        /// Check if running in Docker environment
        /// </summary>
        /// <returns>True if in Docker environment</returns>
        public bool IsDockerEnvironment()
        {
            return File.Exists("/.dockerenv") || 
                   (File.Exists("/proc/1/cgroup") && 
                    File.ReadAllText("/proc/1/cgroup").Contains("docker"));
        }

        /// <summary>
        /// Check if running in Windows environment
        /// </summary>
        /// <returns>True if in Windows environment</returns>
        public bool IsWindowsEnvironment() => OperatingSystem.IsWindows();

        /// <summary>
        /// Check if running in Linux environment
        /// </summary>
        /// <returns>True if in Linux environment</returns>
        public bool IsLinuxEnvironment() => OperatingSystem.IsLinux();

        /// <summary>
        /// Check if running in macOS environment
        /// </summary>
        /// <returns>True if in macOS environment</returns>
        public bool IsMacOSEnvironment() => OperatingSystem.IsMacOS();
    }

    /// <summary>
    /// Environment type constants
    /// </summary>
    public static class EnvironmentTypes
    {
        public const string Docker = "Docker";
        public const string Standalone = "Standalone";
        public const string Development = "Development";
        public const string Production = "Production";
    }
}
