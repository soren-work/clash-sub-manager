using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Configuration validator implementation
    /// </summary>
    public class ConfigurationValidator : IConfigurationValidator
    {
        /// <summary>
        /// Validate configuration
        /// </summary>
        /// <param name="configuration">Configuration object</param>
        /// <exception cref="ConfigurationException">Thrown when validation fails</exception>
        public void Validate(IConfiguration configuration)
        {
            var errors = GetValidationErrors(configuration);
            if (errors.Any())
            {
                throw new ConfigurationException(errors);
            }
        }

        /// <summary>
        /// Get validation error list
        /// </summary>
        /// <param name="configuration">Configuration object</param>
        /// <returns>List of validation errors</returns>
        public List<string> GetValidationErrors(IConfiguration configuration)
        {
            var errors = new List<string>();
            
            // Validate required settings
            if (string.IsNullOrEmpty(configuration["AdminUsername"]))
                errors.Add("AdminUsername is required");
                
            if (string.IsNullOrEmpty(configuration["AdminPassword"]))
                errors.Add("AdminPassword is required");
                
            var secretKey = configuration["CookieSecretKey"];
            if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
                errors.Add("CookieSecretKey must be at least 32 characters");
        
            // Validate session timeout
            if (int.TryParse(configuration["SessionTimeoutMinutes"], out var timeout))
            {
                if (timeout < 5 || timeout > 1440)
                    errors.Add("SessionTimeoutMinutes must be between 5 and 1440");
            }
        
            // Validate data path
            var dataPath = configuration["DataPath"];
            if (!string.IsNullOrEmpty(dataPath))
            {
                try
                {
                    var fullPath = Path.IsPathRooted(dataPath) ? dataPath : 
                        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", dataPath);
                    Directory.CreateDirectory(fullPath);
                }
                catch (Exception ex)
                {
                    errors.Add($"Cannot create data directory '{dataPath}': {ex.Message}");
                }
            }
        
            return errors;
        }

        /// <summary>
        /// Generate default configuration values
        /// </summary>
        /// <returns>Default configuration dictionary</returns>
        public Dictionary<string, string> GenerateDefaultConfiguration()
        {
            var defaults = new Dictionary<string, string>
            {
                ["CookieSecretKey"] = GenerateRandomSecretKey(),
                ["SessionTimeoutMinutes"] = "30",
                ["DataPath"] = GetDefaultDataPath()
            };

            return defaults;
        }

        /// <summary>
        /// Generate random secret key
        /// </summary>
        /// <returns>Random secret key with at least 32 characters</returns>
        private string GenerateRandomSecretKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new StringBuilder();
            
            // Generate 48-character random secret key
            for (int i = 0; i < 48; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Get default data path
        /// </summary>
        /// <returns>Default data path</returns>
        private string GetDefaultDataPath()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            
            // Check if running in Docker environment
            if (File.Exists("/.dockerenv") || 
                (File.Exists("/proc/1/cgroup") && 
                 File.ReadAllText("/proc/1/cgroup").Contains("docker")))
            {
                return "/app/data";
            }
            
            // Use data folder in the same directory as the program in standalone mode
            return Path.Combine(assemblyDirectory ?? ".", "data");
        }

        /// <summary>
        /// Write default configuration to appsettings.json file
        /// </summary>
        /// <param name="configuration">Current configuration</param>
        /// <param name="filePath">Path to appsettings.json file</param>
        public async Task WriteDefaultConfigurationAsync(IConfiguration configuration, string filePath)
        {
            try
            {
                // Read existing configuration
                var existingConfig = new Dictionary<string, object>();
                
                // Read existing appsettings.json (if exists)
                if (File.Exists(filePath))
                {
                    var existingJson = await File.ReadAllTextAsync(filePath);
                    existingConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson) ?? new Dictionary<string, object>();
                }
                else
                {
                    // Create default base configuration
                    existingConfig = new Dictionary<string, object>
                    {
                        ["Logging"] = new Dictionary<string, object>
                        {
                            ["LogLevel"] = new Dictionary<string, object>
                            {
                                ["Default"] = "Information",
                                ["Microsoft.AspNetCore"] = "Warning"
                            }
                        },
                        ["AllowedHosts"] = "*"
                    };
                }

                // Get default configuration to add
                var defaults = GenerateDefaultConfiguration();
                
                // Update configuration (only add non-existing items)
                foreach (var (key, value) in defaults)
                {
                    if (!existingConfig.ContainsKey(key) || existingConfig[key] == null)
                    {
                        existingConfig[key] = value;
                    }
                }

                // Write to file
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                var updatedJson = System.Text.Json.JsonSerializer.Serialize(existingConfig, jsonOptions);
                await File.WriteAllTextAsync(filePath, updatedJson);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to write default configuration to {filePath}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Configuration exception
    /// </summary>
    public class ConfigurationException : Exception
    {
        public List<string> ValidationErrors { get; }
        
        public ConfigurationException(List<string> validationErrors) 
            : base($"Configuration validation failed: {string.Join(", ", validationErrors)}")
        {
            ValidationErrors = validationErrors;
        }
    }
}
