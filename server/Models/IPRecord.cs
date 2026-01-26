namespace ClashSubManager.Models
{
    /// <summary>
    /// IP record data model
    /// </summary>
    public class IPRecord
    {
        /// <summary>
        /// Record ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// IP address
        /// </summary>
        public string IPAddress { get; set; } = string.Empty;

        /// <summary>
        /// Packets sent
        /// </summary>
        public string Sent { get; set; } = string.Empty;

        /// <summary>
        /// Packets received
        /// </summary>
        public string Received { get; set; } = string.Empty;

        /// <summary>
        /// Packet loss rate
        /// </summary>
        public string PacketLossRate => PacketLoss.ToString("F2") + "%";

        /// <summary>
        /// Average latency
        /// </summary>
        public string AverageLatency => Latency.ToString("F2") + "ms";

        /// <summary>
        /// Download speed
        /// </summary>
        public string DownloadSpeed { get; set; } = string.Empty;

        /// <summary>
        /// Port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Packet loss rate (%)
        /// </summary>
        public decimal PacketLoss { get; set; }

        /// <summary>
        /// Latency (ms)
        /// </summary>
        public decimal Latency { get; set; }

        /// <summary>
        /// Validate IP address format
        /// </summary>
        /// <returns>Whether it is a valid IPv4 address</returns>
        public bool IsValidIP()
        {
            return System.Net.IPAddress.TryParse(IPAddress, out var address) && 
                   address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }

        /// <summary>
        /// Validate data integrity
        /// </summary>
        /// <returns>Whether data is valid</returns>
        public bool IsValid()
        {
            return IsValidIP() && 
                   Port > 0 && Port <= 65535 &&
                   PacketLoss >= 0 && PacketLoss <= 100 &&
                   Latency >= 0 && Latency <= 9999.99m;
        }

        /// <summary>
        /// Calculate IP quality level (unified standard)
        /// </summary>
        /// <returns>Quality level and corresponding Bootstrap CSS class</returns>
        public (string Quality, string CssClass) CalculateQuality()
        {
            // 1. Packet loss hard indicator check
            if (PacketLoss > 1)
                return ("Poor", "bg-danger");
            
            if (PacketLoss > 0)
                return ("Fair", "bg-warning");
            
            // 2. Parse download speed
            decimal speed = ParseDownloadSpeed();
            
            // 3. Latency evaluation (unified standard)
            if (Latency <= 150)
            {
                // Low latency range: excellent level
                if (speed > 5) return ("Excellent", "bg-success");
                if (speed > 2) return ("Good", "bg-primary");
                return ("Good", "bg-primary"); // No data, no penalty
            }
            else if (Latency <= 250)
            {
                // Medium latency range: good level
                if (speed > 2) return ("Good", "bg-primary");
                if (speed > 1) return ("Fair", "bg-warning");
                return ("Fair", "bg-warning"); // No data, no penalty
            }
            else if (Latency <= 400)
            {
                // Higher latency range: fair level
                if (speed > 1) return ("Fair", "bg-warning");
                return ("Poor", "bg-danger");
            }
            else
            {
                // High latency range: poor level
                return ("Poor", "bg-danger");
            }
        }

        /// <summary>
        /// Parse download speed string to numeric value (MB/s)
        /// </summary>
        /// <returns>Download speed value, returns 0 if parsing fails</returns>
        private decimal ParseDownloadSpeed()
        {
            if (string.IsNullOrWhiteSpace(DownloadSpeed))
                return 0;

            var speedStr = DownloadSpeed.Trim();

            if (speedStr.EndsWith("MB/s", StringComparison.OrdinalIgnoreCase))
            {
                var numberPart = speedStr.Substring(0, speedStr.Length - 4).Trim();
                return decimal.TryParse(numberPart, out var speed) ? speed : 0;
            }

            if (speedStr.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
            {
                var numberPart = speedStr.Substring(0, speedStr.Length - 2).Trim();
                return decimal.TryParse(numberPart, out var speed) ? speed : 0;
            }

            return decimal.TryParse(speedStr, out var directSpeed) ? directSpeed : 0;
        }
    }
}
