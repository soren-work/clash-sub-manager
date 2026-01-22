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
        /// Create success response
        /// </summary>
        /// <param name="message">Success message</param>
        /// <param name="yamlContent">YAML Content(option)</param>
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
        /// Create success response
        /// </summary>
        /// <param name="yamlContent">YAML content</param>
        /// <returns>Success response</returns>
        public static SubscriptionResponse CreateSuccessFromYaml(string yamlContent)
        {
            return new SubscriptionResponse
            {
                Success = true,
                Message = "Subscription generated successfully",
                YAMLContent = yamlContent
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
    }
}
