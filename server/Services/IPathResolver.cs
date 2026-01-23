namespace ClashSubManager.Services
{
    /// <summary>
    /// Path resolver interface
    /// </summary>
    public interface IPathResolver
    {
        /// <summary>
        /// Resolve path
        /// </summary>
        /// <param name="path">Relative or absolute path</param>
        /// <returns>Resolved absolute path</returns>
        string ResolvePath(string path);

        /// <summary>
        /// Get default data path
        /// </summary>
        /// <returns>Default data path</returns>
        string GetDefaultDataPath();

        /// <summary>
        /// Validate if path is valid
        /// </summary>
        /// <param name="path">Path to validate</param>
        /// <returns>True if path is valid</returns>
        bool IsValidPath(string path);
    }
}
