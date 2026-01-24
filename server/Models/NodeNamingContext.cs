namespace ClashSubManager.Models
{
    /// <summary>
    /// Node naming context
    /// </summary>
    public class NodeNamingContext
    {
        /// <summary>
        /// Original node name
        /// </summary>
        public string OriginalName { get; set; } = string.Empty;

        /// <summary>
        /// Node index (starting from 1)
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Network protocol type (lowercase)
        /// </summary>
        public string Network { get; set; } = string.Empty;

        /// <summary>
        /// Port number
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// New server IP address
        /// </summary>
        public string Server { get; set; } = string.Empty;

        /// <summary>
        /// Original server name (domain)
        /// </summary>
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// Protocol type (uppercase)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// UUID
        /// </summary>
        public string Uuid { get; set; } = string.Empty;

        /// <summary>
        /// Custom properties
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new();

        /// <summary>
        /// Original proxy node (for advanced processing)
        /// </summary>
        public YamlDotNet.RepresentationModel.YamlMappingNode? OriginalProxyNode { get; set; }
    }
}
