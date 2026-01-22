namespace ClashSubManager.Models
{
    /// <summary>
    /// IP record data model
    /// </summary>
    public class IPRecord
    {
        /// <summary>
        /// IP address
        /// </summary>
        public string IPAddress { get; set; } = string.Empty;

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
