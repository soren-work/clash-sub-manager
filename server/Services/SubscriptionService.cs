using ClashSubManager.Models;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Text;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Subscription service core business logic
    /// </summary>
    public class SubscriptionService
    {
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IUserManagementService _userManagementService;
        private readonly FileService _fileService;
        private readonly ValidationService _validationService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            IStringLocalizer<SharedResources> localizer,
            IUserManagementService userManagementService,
            FileService fileService,
            ValidationService validationService,
            IConfigurationService configurationService,
            ILogger<SubscriptionService> logger)
        {
            _localizer = localizer;
            _userManagementService = userManagementService;
            _fileService = fileService;
            _validationService = validationService;
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// Gets user subscription configuration
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Subscription response</returns>
        public async Task<SubscriptionResponse> GetSubscriptionAsync(string userId)
        {
            try
            {
                // Input validation
                if (!_validationService.ValidateUserId(userId))
                {
                    _logger.LogWarning("Invalid user ID: {UserId}", userId);
                    return SubscriptionResponse.CreateError(
                        _localizer["InvalidUserId"], 
                        "INVALID_USER_ID");
                }

                // Record user access
                await _userManagementService.RecordUserAccessAsync(userId);

                // Get user subscription URL
                var subscriptionUrl = await _userManagementService.GetUserSubscriptionUrlAsync(userId);
                if (string.IsNullOrEmpty(subscriptionUrl))
                {
                    _logger.LogWarning("Subscription URL template not configured for user: {UserId}", userId);
                    return SubscriptionResponse.CreateError(
                        _localizer["SubscriptionUrlTemplateNotConfigured"], 
                        "SUBSCRIPTION_URL_TEMPLATE_NOT_CONFIGURED");
                }

                // Get base template
                var template = await _fileService.LoadClashTemplateAsync();
                if (string.IsNullOrEmpty(template))
                {
                    _logger.LogWarning("Clash template not found");
                    return SubscriptionResponse.CreateError(
                        _localizer["TemplateNotFound"], 
                        "TEMPLATE_NOT_FOUND");
                }

                // Get default IP list
                var defaultIPs = await _fileService.LoadDefaultIPsAsync();
                
                // Get user dedicated IP list
                var dedicatedIPs = await _fileService.LoadUserDedicatedIPsAsync(userId);
                
                // Merge configuration to generate YAML
                var yamlContent = await _configurationService.GenerateSubscriptionConfigAsync(
                    template, 
                    subscriptionUrl, 
                    defaultIPs, 
                    dedicatedIPs);

                // Get subscription information (mock data for now, can be enhanced with real data)
                var subscriptionInfo = await GetSubscriptionInfoAsync(userId);

                _logger.LogInformation("Subscription generated successfully for user: {UserId}", userId);
                return SubscriptionResponse.CreateSuccessWithSubscriptionInfo(
                    yamlContent,
                    subscriptionInfo.UploadBytes,
                    subscriptionInfo.DownloadBytes,
                    subscriptionInfo.TotalBytes,
                    subscriptionInfo.ExpireTime,
                    subscriptionInfo.ProfileTitle,
                    subscriptionInfo.UpdateIntervalHours);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription for user: {UserId}", userId);
                return SubscriptionResponse.CreateError(
                    _localizer["InternalServerError"], 
                    "INTERNAL_SERVER_ERROR");
            }
        }

        /// <summary>
        /// Updates user IP configuration
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="csvContent">CSV content</param>
        /// <returns>Operation result</returns>
        public async Task<SubscriptionResponse> UpdateUserIPsAsync(string userId, string csvContent)
        {
            try
            {
                // Input validation
                if (!_validationService.ValidateUserId(userId))
                {
                    _logger.LogWarning("Invalid user ID: {UserId}", userId);
                    return SubscriptionResponse.CreateError(
                        _localizer["InvalidUserId"], 
                        "INVALID_USER_ID");
                }

                // Parse CSV content
                var ipRecords = _validationService.ParseCSVContent(csvContent);
                if (!ipRecords.Any())
                {
                    _logger.LogWarning("No valid IP records found in CSV content");
                    return SubscriptionResponse.CreateError(
                        _localizer["NoValidIPRecordsFound"], 
                        "NO_VALID_IP_RECORDS_FOUND");
                }

                // Record user access
                await _userManagementService.RecordUserAccessAsync(userId);

                // Save user dedicated IP list
                await _fileService.SaveUserDedicatedIPsAsync(userId, ipRecords);

                _logger.LogInformation("User IPs updated successfully for user: {UserId}, count: {Count}", userId, ipRecords.Count);
                return SubscriptionResponse.CreateSuccess(
                    _localizer["UserIPsUpdatedSuccessfully"].ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user IPs for user: {UserId}", userId);
                return SubscriptionResponse.CreateError(
                    _localizer["InternalServerError"], 
                    "INTERNAL_SERVER_ERROR");
            }
        }

        /// <summary>
        /// Deletes user configuration
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Operation result</returns>
        public async Task<SubscriptionResponse> DeleteUserConfigAsync(string userId)
        {
            try
            {
                // Input validation
                if (!_validationService.ValidateUserId(userId))
                {
                    _logger.LogWarning("Invalid user ID: {UserId}", userId);
                    return SubscriptionResponse.CreateError(
                        _localizer["InvalidUserId"], 
                        "INVALID_USER_ID");
                }

                // Delete user configuration
                var deleted = await _userManagementService.DeleteUserAsync(userId);
                if (!deleted)
                {
                    _logger.LogWarning("User not found for deletion: {UserId}", userId);
                    return SubscriptionResponse.CreateError(
                        _localizer["UserNotFoundForDeletion"], 
                        "USER_NOT_FOUND_FOR_DELETION");
                }

                _logger.LogInformation("User config deleted successfully for user: {UserId}", userId);
                return SubscriptionResponse.CreateSuccess(
                    _localizer["UserConfigDeleted"].ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user config for user: {UserId}", userId);
                return SubscriptionResponse.CreateError(
                    _localizer["InternalServerError"], 
                    "INTERNAL_SERVER_ERROR");
            }
        }

        /// <summary>
        /// Get subscription information for user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Subscription information</returns>
        private async Task<(long UploadBytes, long DownloadBytes, long TotalBytes, DateTime ExpireTime, string ProfileTitle, int UpdateIntervalHours)> GetSubscriptionInfoAsync(string userId)
        {
            try
            {
                // Get user subscription URL
                var subscriptionUrl = await _userManagementService.GetUserSubscriptionUrlAsync(userId);
                if (string.IsNullOrEmpty(subscriptionUrl))
                {
                    _logger.LogWarning("Subscription URL not configured for user: {UserId}", userId);
                    return (0L, 0L, 0L, DateTime.MinValue, string.Empty, 24);
                }

                // Try to fetch subscription info from original source
                var originalSubscriptionInfo = await FetchOriginalSubscriptionInfoAsync(subscriptionUrl);
                
                if (originalSubscriptionInfo != null)
                {
                    _logger.LogDebug("Successfully fetched subscription info from original source for user: {UserId}", userId);
                    return originalSubscriptionInfo.Value;
                }
                else
                {
                    // Fallback to mock data if original source is not available
                    _logger.LogWarning("Failed to fetch subscription info from original source, using mock data for user: {UserId}", userId);
                    return GenerateMockSubscriptionInfo(userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription info for user: {UserId}", userId);
                
                // Return default values on error
                return (0L, 0L, 0L, DateTime.MinValue, string.Empty, 24);
            }
        }

        /// <summary>
        /// Fetch subscription information from original subscription source
        /// </summary>
        /// <param name="subscriptionUrl">Original subscription URL</param>
        /// <returns>Subscription information or null if failed</returns>
        private async Task<(long UploadBytes, long DownloadBytes, long TotalBytes, DateTime ExpireTime, string ProfileTitle, int UpdateIntervalHours)?> FetchOriginalSubscriptionInfoAsync(string subscriptionUrl)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("clash-verge/v1.0.0");
                
                // Send HEAD request to get headers only
                using var response = await httpClient.GetAsync(subscriptionUrl, HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Original subscription source returned status: {StatusCode}", response.StatusCode);
                    return null;
                }

                long uploadBytes = 0, downloadBytes = 0, totalBytes = 0;
                DateTime expireTime = DateTime.MinValue;
                string profileTitle = string.Empty;
                int updateIntervalHours = 24;

                // Parse subscription-userinfo header
                if (response.Headers.TryGetValues("subscription-userinfo", out var userinfoValues))
                {
                    var userinfo = userinfoValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(userinfo))
                    {
                        ParseSubscriptionUserInfo(userinfo, out uploadBytes, out downloadBytes, out totalBytes, out expireTime);
                    }
                }

                // Parse Profile-Title header
                if (response.Headers.TryGetValues("Profile-Title", out var titleValues))
                {
                    var title = titleValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(title))
                    {
                        // Handle base64 encoded title
                        if (title.StartsWith("base64:"))
                        {
                            try
                            {
                                var base64String = title.Substring(7);
                                var titleBytes = Convert.FromBase64String(base64String);
                                profileTitle = Encoding.UTF8.GetString(titleBytes);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to decode base64 Profile-Title header");
                                profileTitle = title; // Fallback to original if decoding fails
                            }
                        }
                        else
                        {
                            profileTitle = title;
                        }
                        
                        // Remove surrounding quotes if present
                        if (profileTitle.StartsWith("\"") && profileTitle.EndsWith("\""))
                        {
                            profileTitle = profileTitle.Substring(1, profileTitle.Length - 2);
                        }
                    }
                }

                // Parse profile-update-interval header
                if (response.Headers.TryGetValues("profile-update-interval", out var intervalValues))
                {
                    var interval = intervalValues.FirstOrDefault();
                    if (int.TryParse(interval, out var parsedInterval) && parsedInterval > 0)
                    {
                        updateIntervalHours = parsedInterval;
                    }
                }

                return (uploadBytes, downloadBytes, totalBytes, expireTime, profileTitle, updateIntervalHours);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching original subscription info from: {Url}", MaskUrlLikeValue(subscriptionUrl));
                return null;
            }
        }

        private static string MaskUrlLikeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return "[REDACTED]";

            return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
        }

        /// <summary>
        /// Parse subscription-userinfo header value
        /// </summary>
        /// <param name="userinfo">User info header value</param>
        /// <param name="uploadBytes">Output upload bytes</param>
        /// <param name="downloadBytes">Output download bytes</param>
        /// <param name="totalBytes">Output total bytes</param>
        /// <param name="expireTime">Output expire time</param>
        private void ParseSubscriptionUserInfo(string userinfo, out long uploadBytes, out long downloadBytes, out long totalBytes, out DateTime expireTime)
        {
            uploadBytes = 0;
            downloadBytes = 0;
            totalBytes = 0;
            expireTime = DateTime.MinValue;

            if (string.IsNullOrEmpty(userinfo))
                return;

            var parts = userinfo.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length != 2)
                    continue;

                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();

                switch (key)
                {
                    case "upload":
                        long.TryParse(value, out uploadBytes);
                        break;
                    case "download":
                        long.TryParse(value, out downloadBytes);
                        break;
                    case "total":
                        long.TryParse(value, out totalBytes);
                        break;
                    case "expire":
                        if (long.TryParse(value, out var timestamp) && timestamp > 0)
                        {
                            try
                            {
                                expireTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to parse expire timestamp from subscription-userinfo");
                                expireTime = DateTime.MinValue;
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Generate mock subscription information as fallback
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Mock subscription information</returns>
        private (long UploadBytes, long DownloadBytes, long TotalBytes, DateTime ExpireTime, string ProfileTitle, int UpdateIntervalHours) GenerateMockSubscriptionInfo(string userId)
        {
            var random = new Random(userId.GetHashCode());
            
            // Mock subscription data
            var uploadBytes = (long)(random.NextDouble() * 1024 * 1024 * 1024); // Random up to 1GB
            var downloadBytes = (long)(random.NextDouble() * 5 * 1024 * 1024 * 1024); // Random up to 5GB
            var totalBytes = 10L * 1024 * 1024 * 1024; // 10GB total limit
            var expireTime = DateTime.UtcNow.AddDays(30); // 30 days from now
            var profileTitle = $"Clash Subscription - {userId}";
            var updateIntervalHours = 24; // Update every 24 hours

            _logger.LogDebug("Generated mock subscription info for user: {UserId}, Upload: {UploadBytes}, Download: {DownloadBytes}", 
                userId, uploadBytes, downloadBytes);

            return (uploadBytes, downloadBytes, totalBytes, expireTime, profileTitle, updateIntervalHours);
        }
    }
}
