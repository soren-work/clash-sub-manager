using Microsoft.Extensions.Configuration;
using ClashSubManager.Models;

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

        /// <summary>
        /// Get subscription URL template
        /// </summary>
        /// <returns>Subscription URL template</returns>
        string GetSubscriptionUrlTemplate();

        /// <summary>
        /// Generate subscription configuration
        /// </summary>
        /// <param name="template">Base template</param>
        /// <param name="subscriptionUrl">Subscription URL</param>
        /// <param name="defaultIPs">Default IP list</param>
        /// <param name="dedicatedIPs">Dedicated IP list</param>
        /// <returns>Generated YAML configuration</returns>
        Task<string> GenerateSubscriptionConfigAsync(
            string template, 
            string subscriptionUrl, 
            List<IPRecord> defaultIPs, 
            List<IPRecord> dedicatedIPs);

        /// <summary>
        /// Get node naming template
        /// </summary>
        /// <returns>Node naming template</returns>
        string GetNodeNamingTemplate();
    }
}
