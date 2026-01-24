using ClashSubManager.Models;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ClashSubManager.Services
{
    /// <summary>
    /// File operation service
    /// </summary>
    public class FileService
    {
        private readonly IConfigurationService _configurationService;
        private readonly CloudflareIPParserService _ipParserService;
        private readonly ILogger<FileService> _logger;
        private readonly string _dataPath;

        public FileService(
            IConfigurationService configurationService,
            CloudflareIPParserService ipParserService,
            ILogger<FileService> logger)
        {
            _configurationService = configurationService;
            _ipParserService = ipParserService;
            _logger = logger;
            _dataPath = _configurationService.GetDataPath();
            
            // Ensure data directory exists
            Directory.CreateDirectory(_dataPath);
        }

        /// <summary>
        /// Load user-specific IP list
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>IP record list</returns>
        public virtual async Task<List<IPRecord>> LoadUserDedicatedIPsAsync(string userId)
        {
            try
            {
                var userDir = Path.Combine(_dataPath, userId);
                var csvFile = Path.Combine(userDir, "cloudflare-ip.csv");
                
                if (!File.Exists(csvFile))
                    return new List<IPRecord>();

                var csvContent = await File.ReadAllTextAsync(csvFile);
                return _ipParserService.ParseCSVContent(csvContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user dedicated IPs for user: {UserId}", userId);
                return new List<IPRecord>();
            }
        }

        /// <summary>
        /// Save user-specific IP list
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="ipRecords">IP record list</param>
        /// <returns>Operation result</returns>
        public virtual async Task<bool> SaveUserDedicatedIPsAsync(string userId, List<IPRecord> ipRecords)
        {
            try
            {
                var userDir = Path.Combine(_dataPath, userId);
                var csvFile = Path.Combine(userDir, "cloudflare-ip.csv");

                // Create user directory
                Directory.CreateDirectory(userDir);
                
                var csvLines = ipRecords.Select(ip => 
                    $"{ip.IPAddress},{ip.Port},{ip.PacketLoss},{ip.Latency},{ip.DownloadSpeed}");

                var csvContent = string.Join(Environment.NewLine, csvLines);
                
                // Atomic write
                var tempFile = csvFile + ".tmp";
                await File.WriteAllTextAsync(tempFile, csvContent);
                
                File.Move(tempFile, csvFile, true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user dedicated IPs for user: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Delete user-specific IP list
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Whether deletion was successful</returns>
        public virtual async Task<bool> DeleteUserDedicatedIPsAsync(string userId)
        {
            try
            {
                var userDir = Path.Combine(_dataPath, userId);
                var csvFile = Path.Combine(userDir, "cloudflare-ip.csv");

                if (!File.Exists(csvFile))
                    return false;

                // Delete file
                File.Delete(csvFile);

                // Try to delete user directory (if empty)
                try
                {
                    if (Directory.GetFiles(userDir).Length == 0 && 
                        Directory.GetDirectories(userDir).Length == 0)
                    {
                        Directory.Delete(userDir);
                    }
                }
                catch
                {
                    // Directory deletion failure doesn't affect main functionality
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user dedicated IPs for user: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Load default IP list
        /// </summary>
        /// <returns>IP record list</returns>
        public virtual async Task<List<IPRecord>> LoadDefaultIPsAsync()
        {
            try
            {
                var csvFile = Path.Combine(_dataPath, "cloudflare-ip.csv");
                
                if (!File.Exists(csvFile))
                    return new List<IPRecord>();

                var csvContent = await File.ReadAllTextAsync(csvFile);
                return _ipParserService.ParseCSVContent(csvContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading default IPs");
                return new List<IPRecord>();
            }
        }

        /// <summary>
        /// Save default IP list
        /// </summary>
        /// <param name="ipRecords">IP record list</param>
        /// <returns>Operation result</returns>
        public async Task<bool> SaveDefaultIPsAsync(List<IPRecord> ipRecords)
        {
            try
            {
                var csvFile = Path.Combine(_dataPath, "cloudflare-ip.csv");
                
                var csvLines = ipRecords.Select(ip => 
                    $"{ip.IPAddress},{ip.Port},{ip.PacketLoss},{ip.Latency}");

                var csvContent = string.Join(Environment.NewLine, csvLines);
                
                // Atomic write
                var tempFile = csvFile + ".tmp";
                await File.WriteAllTextAsync(tempFile, csvContent);
                
                File.Move(tempFile, csvFile, true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving default IPs");
                return false;
            }
        }

        /// <summary>
        /// Load Clash template
        /// </summary>
        /// <returns>Template content</returns>
        public virtual async Task<string?> LoadClashTemplateAsync()
        {
            try
            {
                var templateFile = Path.Combine(_dataPath, "clash.yaml");
                
                if (!File.Exists(templateFile))
                    return null;

                return await File.ReadAllTextAsync(templateFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Clash template");
                return null;
            }
        }

        /// <summary>
        /// Save Clash template
        /// </summary>
        /// <param name="templateContent">Template content</param>
        /// <returns>Operation result</returns>
        public async Task<bool> SaveClashTemplateAsync(string templateContent)
        {
            try
            {
                var templateFile = Path.Combine(_dataPath, "clash.yaml");
                
                // Atomic write
                var tempFile = templateFile + ".tmp";
                await File.WriteAllTextAsync(tempFile, templateContent);
                
                File.Move(tempFile, templateFile, true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Clash template");
                return false;
            }
        }
    }
}
