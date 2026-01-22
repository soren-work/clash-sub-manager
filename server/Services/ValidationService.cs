using ClashSubManager.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Data validation service
    /// </summary>
    public class ValidationService
    {
        private readonly ILogger<ValidationService> _logger;
        
        // User ID validation regex
        private static readonly Regex UserIdRegex = new(@"^[a-zA-Z0-9_-]{1,64}$", RegexOptions.Compiled);
        
        // IPv4 address validation regex
        private static readonly Regex IPv4Regex = new(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", RegexOptions.Compiled);

        public ValidationService(ILogger<ValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validate user ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Whether valid</returns>
        public virtual bool ValidateUserId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            return UserIdRegex.IsMatch(userId);
        }

        /// <summary>
        /// Validate IPv4 address
        /// </summary>
        /// <param name="ipAddress">IP address</param>
        /// <returns>Whether valid</returns>
        public bool ValidateIPv4Address(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            return IPv4Regex.IsMatch(ipAddress) && IPAddress.TryParse(ipAddress, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }

        /// <summary>
        /// Validate subscription URL
        /// </summary>
        /// <param name="url">Subscription URL</param>
        /// <returns>Whether valid</returns>
        public bool ValidateSubscriptionUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Validate port number
        /// </summary>
        /// <param name="port">Port number</param>
        /// <returns>Whether valid</returns>
        public bool ValidatePort(int port)
        {
            return port > 0 && port <= 65535;
        }

        /// <summary>
        /// Validate packet loss rate
        /// </summary>
        /// <param name="packetLoss">Packet loss rate</param>
        /// <returns>Whether valid</returns>
        public bool ValidatePacketLoss(decimal packetLoss)
        {
            return packetLoss >= 0 && packetLoss <= 100;
        }

        /// <summary>
        /// Validate latency
        /// </summary>
        /// <param name="latency">Latency</param>
        /// <returns>Whether valid</returns>
        public bool ValidateLatency(decimal latency)
        {
            return latency >= 0 && latency <= 9999.99m;
        }

        /// <summary>
        /// Validate IP record
        /// </summary>
        /// <param name="ipRecord">IP record</param>
        /// <returns>Whether valid</returns>
        public bool ValidateIPRecord(IPRecord ipRecord)
        {
            if (ipRecord == null)
                return false;

            return ValidateIPv4Address(ipRecord.IPAddress) &&
                   ValidatePort(ipRecord.Port) &&
                   ValidatePacketLoss(ipRecord.PacketLoss) &&
                   ValidateLatency(ipRecord.Latency);
        }

        /// <summary>
        /// Validate user configuration
        /// </summary>
        /// <param name="userConfig">User configuration</param>
        /// <returns>Whether valid</returns>
        public bool ValidateUserConfig(UserConfig userConfig)
        {
            if (userConfig == null)
                return false;

            return ValidateUserId(userConfig.UserId) &&
                   ValidateSubscriptionUrl(userConfig.SubscriptionUrl) &&
                   userConfig.DedicatedIPs.All(ValidateIPRecord);
        }

        /// <summary>
        /// Parse CSV content to IP record list
        /// </summary>
        /// <param name="csvContent">CSV content</param>
        /// <returns>IP record list</returns>
        public virtual List<IPRecord> ParseCSVContent(string csvContent)
        {
            var ipRecords = new List<IPRecord>();

            if (string.IsNullOrWhiteSpace(csvContent))
                return ipRecords;

            try
            {
                var lines = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    // Skip comment lines and empty lines
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                        continue;

                    var parts = line.Split(',');
                    if (parts.Length >= 1) // At least IP address is required
                    {
                        var ipRecord = new IPRecord
                        {
                            IPAddress = parts[0].Trim(),
                            Port = parts.Length > 1 && int.TryParse(parts[1].Trim(), out var port) ? port : 443,
                            PacketLoss = parts.Length > 2 && decimal.TryParse(parts[2].Trim(), out var loss) ? loss : 0,
                            Latency = parts.Length > 3 && decimal.TryParse(parts[3].Trim(), out var latency) ? latency : 0
                        };

                        if (ValidateIPRecord(ipRecord))
                        {
                            ipRecords.Add(ipRecord);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid IP record skipped: {line}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CSV content");
                Console.WriteLine($"Error parsing CSV content: {ex.Message}");
            }

            return ipRecords;
        }

        /// <summary>
        /// Convert IP record list to CSV format
        /// </summary>
        /// <param name="ipRecords">IP record list</param>
        /// <returns>CSV content</returns>
        public string ConvertToCSV(List<IPRecord> ipRecords)
        {
            if (ipRecords == null || !ipRecords.Any())
                return string.Empty;

            var lines = ipRecords.Select(ip => 
                $"{ip.IPAddress},{ip.Port},{ip.PacketLoss},{ip.Latency}");

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Validate file size
        /// </summary>
        /// <param name="fileSize">File size (bytes)</param>
        /// <param name="maxSizeMB">Maximum size (MB)</param>
        /// <returns>Whether valid</returns>
        public bool ValidateFileSize(long fileSize, int maxSizeMB = 10)
        {
            var maxSizeBytes = maxSizeMB * 1024L * 1024L;
            return fileSize > 0 && fileSize <= maxSizeBytes;
        }

        /// <summary>
        /// Validate YAML content format
        /// </summary>
        /// <param name="yamlContent">YAML content</param>
        /// <returns>Whether valid</returns>
        public bool ValidateYAMLContent(string yamlContent)
        {
            if (string.IsNullOrWhiteSpace(yamlContent))
                return false;

            try
            {
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                var result = deserializer.Deserialize(yamlContent);
                return result != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
