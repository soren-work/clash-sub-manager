using Microsoft.Extensions.Configuration;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Cross-platform configuration service interface
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Get data storage path
        /// </summary>
        /// <returns>Data path</returns>
        string GetDataPath();

        /// <summary>
        /// Get configuration value
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>Configuration value</returns>
        T GetValue<T>(string key, T defaultValue = default);

        /// <summary>
        /// Check if configuration key exists
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>True if key exists</returns>
        bool HasValue(string key);

        /// <summary>
        /// Validate configuration
        /// </summary>
        void ValidateConfiguration();

        /// <summary>
        /// Get environment type
        /// </summary>
        /// <returns>Environment type</returns>
        string GetEnvironmentType();
    }
}
