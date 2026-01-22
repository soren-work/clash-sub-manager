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
        public string Sent { get; set; } = "No data";

        /// <summary>
        /// Packets received
        /// </summary>
        public string Received { get; set; } = "No data";

        /// <summary>
        /// Packet loss rate
        /// </summary>
        public string PacketLossRate { get; set; } = "No data";

        /// <summary>
        /// Average latency
        /// </summary>
        public string AverageLatency { get; set; } = "No data";

        /// <summary>
        /// Download speed
        /// </summary>
        public string DownloadSpeed { get; set; } = "No data";

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
    }
}
