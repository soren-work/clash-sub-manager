using Microsoft.Extensions.Configuration;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Cross-platform configuration service implementation
    /// </summary>
    public class PlatformConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PlatformConfigurationService> _logger;
        private readonly IEnvironmentDetector _environmentDetector;
        private readonly IPathResolver _pathResolver;
        private readonly IConfigurationValidator _configurationValidator;
        private string? _cachedDataPath;

        public PlatformConfigurationService(
            IConfiguration configuration,
            ILogger<PlatformConfigurationService> logger,
            IEnvironmentDetector environmentDetector,
            IPathResolver pathResolver,
            IConfigurationValidator configurationValidator)
        {
            _configuration = configuration;
            _logger = logger;
            _environmentDetector = environmentDetector;
            _pathResolver = pathResolver;
            _configurationValidator = configurationValidator;
        }

        /// <summary>
        /// Get data storage path
        /// </summary>
        /// <returns>Data path</returns>
        public string GetDataPath()
        {
            if (_cachedDataPath != null)
                return _cachedDataPath;

            var configuredPath = GetValue<string>("DataPath");
            if (!string.IsNullOrEmpty(configuredPath))
            {
                _cachedDataPath = _pathResolver.ResolvePath(configuredPath);
            }
            else
            {
                _cachedDataPath = _pathResolver.GetDefaultDataPath();
            }

            // Ensure data directory exists
            try
            {
                Directory.CreateDirectory(_cachedDataPath);
                _logger.LogInformation("Data path configured: {DataPath}", _cachedDataPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create data directory: {DataPath}", _cachedDataPath);
                throw new InvalidOperationException($"Cannot create data directory: {_cachedDataPath}", ex);
            }

            return _cachedDataPath;
        }

        /// <summary>
        /// Get configuration value
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>Configuration value</returns>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            try
            {
                var value = _configuration.GetValue(key, defaultValue);
                _logger.LogDebug("Configuration value retrieved: {Key} = {Value}", key, value);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get configuration value for key: {Key}", key);
                return defaultValue;
            }
        }

        /// <summary>
        /// Check if configuration key exists
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>True if key exists</returns>
        public bool HasValue(string key)
        {
            return _configuration[key] != null;
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public void ValidateConfiguration()
        {
            try
            {
                _configurationValidator.Validate(_configuration);
                _logger.LogInformation("Configuration validation passed");
            }
            catch (ConfigurationException ex)
            {
                _logger.LogError("Configuration validation failed: {Errors}", string.Join(", ", ex.ValidationErrors));
                throw;
            }
        }

        /// <summary>
        /// Get environment type
        /// </summary>
        /// <returns>Environment type</returns>
        public string GetEnvironmentType()
        {
            return _environmentDetector.GetEnvironmentType();
        }
    }
}
