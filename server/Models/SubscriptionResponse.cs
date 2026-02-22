namespace ClashSubManager.Models
{
    /// <summary>
    /// Subscription response data model
    /// </summary>
    public class SubscriptionResponse
    {
        /// <summary>
        /// Response status
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// YAML configuration content
        /// </summary>
        public string? YAMLContent { get; set; }

        /// <summary>
        /// Response timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Error code (if any)
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Upload bytes for subscription user info
        /// </summary>
        public long UploadBytes { get; set; } = 0;

        /// <summary>
        /// Download bytes for subscription user info
        /// </summary>
        public long DownloadBytes { get; set; } = 0;

        /// <summary>
        /// Total bytes limit for subscription user info
        /// </summary>
        public long TotalBytes { get; set; } = 0;

        /// <summary>
        /// Expire time for subscription user info
        /// </summary>
        public DateTime ExpireTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Profile title for subscription
        /// </summary>
        public string ProfileTitle { get; set; } = string.Empty;

        /// <summary>
        /// Update interval in hours for subscription
        /// </summary>
        public int UpdateIntervalHours { get; set; } = 24;

        /// <summary>
        /// Original filename from subscription source
        /// </summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// Profile web page URL
        /// </summary>
        public string ProfileWebPageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Support URL
        /// </summary>
        public string SupportUrl { get; set; } = string.Empty;

        /// <summary>
        /// Create success response
        /// </summary>
        /// <param name="message">Success message</param>
        /// <param name="yamlContent">YAML content (optional)</param>
        /// <returns>Success response</returns>
        public static SubscriptionResponse CreateSuccess(string message, string? yamlContent = null)
        {
            return new SubscriptionResponse
            {
                Success = true,
                Message = message,
                YAMLContent = yamlContent
            };
        }

        /// <summary>
        /// Create success response with YAML content only
        /// </summary>
        /// <param name="yamlContent">YAML content</param>
        /// <param name="message">Success message (optional)</param>
        /// <returns>Success response</returns>
        public static SubscriptionResponse CreateSuccessFromYaml(string yamlContent, string message = "Subscription generated successfully")
        {
            return new SubscriptionResponse
            {
                Success = true,
                Message = message,
                YAMLContent = yamlContent
            };
        }

        /// <summary>
        /// Create success response with subscription information
        /// </summary>
        /// <param name="yamlContent">YAML content</param>
        /// <param name="uploadBytes">Upload bytes</param>
        /// <param name="downloadBytes">Download bytes</param>
        /// <param name="totalBytes">Total bytes limit</param>
        /// <param name="expireTime">Expire time</param>
        /// <param name="profileTitle">Profile title</param>
        /// <param name="updateIntervalHours">Update interval in hours</param>
        /// <param name="originalFileName">Original filename from subscription source</param>
        /// <param name="profileWebPageUrl">Profile web page URL</param>
        /// <param name="supportUrl">Support URL</param>
        /// <returns>Success response with subscription info</returns>
        public static SubscriptionResponse CreateSuccessWithSubscriptionInfo(
            string yamlContent,
            long uploadBytes = 0,
            long downloadBytes = 0,
            long totalBytes = 0,
            DateTime expireTime = default,
            string profileTitle = "",
            int updateIntervalHours = 24,
            string originalFileName = "",
            string profileWebPageUrl = "",
            string supportUrl = "")
        {
            return new SubscriptionResponse
            {
                Success = true,
                Message = "Subscription generated successfully",
                YAMLContent = yamlContent,
                UploadBytes = uploadBytes,
                DownloadBytes = downloadBytes,
                TotalBytes = totalBytes,
                ExpireTime = expireTime == default ? DateTime.MinValue : expireTime,
                ProfileTitle = profileTitle ?? string.Empty,
                UpdateIntervalHours = updateIntervalHours,
                OriginalFileName = originalFileName ?? string.Empty,
                ProfileWebPageUrl = profileWebPageUrl ?? string.Empty,
                SupportUrl = supportUrl ?? string.Empty
            };
        }

        /// <summary>
        /// Create error response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorCode">Error code</param>
        /// <returns>Error response</returns>
        public static SubscriptionResponse CreateError(string message, string? errorCode = null)
        {
            return new SubscriptionResponse
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }

        /// <summary>
        /// Get subscription-userinfo header value
        /// </summary>
        /// <returns>Formatted subscription-userinfo header value</returns>
        public string GetSubscriptionUserInfoHeader()
        {
            long expireTimestamp;
            if (ExpireTime == DateTime.MinValue || ExpireTime.Year < 1970)
            {
                // Use 0 for invalid or very old dates
                expireTimestamp = 0;
            }
            else
            {
                expireTimestamp = new DateTimeOffset(ExpireTime).ToUnixTimeSeconds();
            }
            
            return $"upload={UploadBytes};download={DownloadBytes};total={TotalBytes};expire={expireTimestamp}";
        }
    }
}
