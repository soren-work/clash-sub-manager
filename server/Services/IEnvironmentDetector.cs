namespace ClashSubManager.Services
{
    /// <summary>
    /// Environment detector interface
    /// </summary>
    public interface IEnvironmentDetector
    {
        /// <summary>
        /// Get environment type
        /// </summary>
        /// <returns>Environment type</returns>
        string GetEnvironmentType();

        /// <summary>
        /// Check if running in Docker environment
        /// </summary>
        /// <returns>True if in Docker environment</returns>
        bool IsDockerEnvironment();

        /// <summary>
        /// Check if running in Windows environment
        /// </summary>
        /// <returns>True if in Windows environment</returns>
        bool IsWindowsEnvironment();

        /// <summary>
        /// Check if running in Linux environment
        /// </summary>
        /// <returns>True if in Linux environment</returns>
        bool IsLinuxEnvironment();

        /// <summary>
        /// Check if running in macOS environment
        /// </summary>
        /// <returns>True if in macOS environment</returns>
        bool IsMacOSEnvironment();
    }
}
