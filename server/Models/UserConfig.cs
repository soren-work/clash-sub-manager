namespace ClashSubManager.Models
{
    /// <summary>
    /// User configuration data model
    /// </summary>
    public class UserConfig
    {
        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Subscription URL
        /// </summary>
        public string SubscriptionUrl { get; set; } = string.Empty;

        /// <summary>
        /// Dedicated IP list
        /// </summary>
        public List<IPRecord> DedicatedIPs { get; set; } = new();

        /// <summary>
        /// Configuration creation time
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Configuration update time
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Validate user ID format
        /// </summary>
        /// <returns>Whether user ID is valid</returns>
        public bool IsValidUserId()
        {
            if (string.IsNullOrWhiteSpace(UserId))
                return false;

            if (UserId.Length < 1 || UserId.Length > 64)
                return false;

            return UserId.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
        }

        /// <summary>
        /// Validate subscription URL format
        /// </summary>
        /// <returns>Whether subscription URL is valid</returns>
        public bool IsValidSubscriptionUrl()
        {
            if (string.IsNullOrWhiteSpace(SubscriptionUrl))
                return false;

            return Uri.TryCreate(SubscriptionUrl, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Validate configuration data integrity
        /// </summary>
        /// <returns>Whether configuration is valid</returns>
        public bool IsValid()
        {
            return IsValidUserId() && 
                   IsValidSubscriptionUrl() &&
                   DedicatedIPs.All(ip => ip.IsValid());
        }
    }
}
