using System.Reflection;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Path resolver implementation
    /// </summary>
    public class PathResolver : IPathResolver
    {
        private readonly IEnvironmentDetector _environmentDetector;
        private readonly string _assemblyDirectory;

        public PathResolver(IEnvironmentDetector environmentDetector)
        {
            _environmentDetector = environmentDetector;
            _assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        }

        /// <summary>
        /// Resolves path
        /// </summary>
        /// <param name="path">Relative or absolute path</param>
        /// <returns>Resolved absolute path</returns>
        public string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return _assemblyDirectory;

            if (Path.IsPathRooted(path))
                return path;

            return Path.Combine(_assemblyDirectory, path);
        }

        /// <summary>
        /// Gets default data path
        /// </summary>
        /// <returns>Default data path</returns>
        public string GetDefaultDataPath()
        {
            if (_environmentDetector.IsDockerEnvironment())
                return "/app/data";

            return Path.Combine(_assemblyDirectory, "data");
        }

        /// <summary>
        /// Validates if path is valid
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>Whether path is valid</returns>
        public bool IsValidPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return false;

                var fullPath = ResolvePath(path);
                Directory.CreateDirectory(fullPath);
                return Directory.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }
    }
}
