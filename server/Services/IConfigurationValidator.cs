using Microsoft.Extensions.Configuration;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Configuration validator interface
    /// </summary>
    public interface IConfigurationValidator
    {
        /// <summary>
        /// Validate configuration
        /// </summary>
        /// <param name="configuration">Configuration object</param>
        /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
        void Validate(IConfiguration configuration);

        /// <summary>
        /// Get validation error list
        /// </summary>
        /// <param name="configuration">Configuration object</param>
        /// <returns>List of validation errors</returns>
        List<string> GetValidationErrors(IConfiguration configuration);

        /// <summary>
        /// Generate default configuration values
        /// </summary>
        /// <returns>Default configuration dictionary</returns>
        Dictionary<string, string> GenerateDefaultConfiguration();

        /// <summary>
        /// Write default configuration to appsettings.json file
        /// </summary>
        /// <param name="configuration">Current configuration</param>
        /// <param name="filePath">Path to appsettings.json file</param>
        Task WriteDefaultConfigurationAsync(IConfiguration configuration, string filePath);
    }
}
