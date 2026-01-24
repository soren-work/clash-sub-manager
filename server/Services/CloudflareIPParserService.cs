using ClashSubManager.Models;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Unified Cloudflare IP CSV parsing service
    /// </summary>
    public class CloudflareIPParserService
    {
        /// <summary>
        /// Parse Cloudflare IP CSV content
        /// Supported format: IP Address,Sent,Received,Packet Loss Rate,Average Latency,Download Speed
        /// </summary>
        /// <param name="csvContent">CSV content</param>
        /// <returns>List of IP records</returns>
        public List<IPRecord> ParseCSVContent(string csvContent)
        {
            var ipRecords = new List<IPRecord>();

            if (string.IsNullOrWhiteSpace(csvContent))
                return ipRecords;

            try
            {
                var lines = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                var id = 1;
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    // Skip comment lines, empty lines, and header lines
                    if (string.IsNullOrEmpty(trimmedLine) || 
                        trimmedLine.StartsWith("#") || 
                        trimmedLine.StartsWith("IP Address"))
                        continue;

                    var record = ParseLineToIPRecord(trimmedLine);
                    if (record != null)
                    {
                        record.Id = id++;
                        ipRecords.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw exception, return empty list
                Console.WriteLine($"Error parsing CSV content: {ex.Message}");
            }

            return ipRecords;
        }

        /// <summary>
        /// Parse a single CSV line to IP record
        /// </summary>
        /// <param name="line">CSV line data</param>
        /// <returns>IP record or null</returns>
        private IPRecord? ParseLineToIPRecord(string line)
        {
            var columns = line.Split(',');
            
            // At least IP address is required
            if (columns.Length < 1 || !IsValidIP(columns[0].Trim()))
                return null;

            var record = new IPRecord
            {
                IPAddress = columns[0].Trim(),
                Port = 443 // Default HTTPS port
            };

            // Parse 6-field new format: IP Address,Sent,Received,Packet Loss Rate,Average Latency,Download Speed
            if (columns.Length >= 6)
            {
                record.Sent = columns[1].Trim();
                record.Received = columns[2].Trim();
                record.PacketLoss = ParsePacketLoss(columns[3].Trim());
                record.Latency = ParseLatency(columns[4].Trim());
                record.DownloadSpeed = columns[5].Trim();
            }
            // Compatible with 4-field old format: IP,Port,PacketLoss,Latency
            else if (columns.Length >= 4)
            {
                if (int.TryParse(columns[1].Trim(), out var port))
                    record.Port = port;
                record.PacketLoss = decimal.TryParse(columns[2].Trim(), out var loss) ? loss : 0;
                record.Latency = decimal.TryParse(columns[3].Trim(), out var latency) ? latency : 0;
            }
            // Compatible with 3-field format: IP,PacketLoss,Latency
            else if (columns.Length >= 3)
            {
                record.PacketLoss = decimal.TryParse(columns[1].Trim(), out var loss) ? loss : 0;
                record.Latency = decimal.TryParse(columns[2].Trim(), out var latency) ? latency : 0;
            }
            // Compatible with 2-field format: IP,Latency
            else if (columns.Length >= 2)
            {
                record.Latency = decimal.TryParse(columns[1].Trim(), out var latency) ? latency : 0;
            }

            return record.IsValid() ? record : null;
        }

        /// <summary>
        /// Parse packet loss string
        /// </summary>
        /// <param name="packetLossStr">Packet loss string (e.g. "0.00%")</param>
        /// <returns>Packet loss value</returns>
        private decimal ParsePacketLoss(string packetLossStr)
        {
            var cleanStr = packetLossStr.Replace("%", "").Trim();
            return decimal.TryParse(cleanStr, out var loss) ? loss : 0;
        }

        /// <summary>
        /// Parse latency string
        /// </summary>
        /// <param name="latencyStr">Latency string (e.g. "152.45ms")</param>
        /// <returns>Latency value</returns>
        private decimal ParseLatency(string latencyStr)
        {
            var cleanStr = latencyStr.Replace("ms", "").Trim();
            return decimal.TryParse(cleanStr, out var latency) ? latency : 0;
        }

        /// <summary>
        /// Validate IP address format
        /// </summary>
        /// <param name="ip">IP address string</param>
        /// <returns>Whether it is a valid IPv4 address</returns>
        private bool IsValidIP(string ip)
        {
            return System.Net.IPAddress.TryParse(ip, out var address) && 
                   address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }
    }
}
