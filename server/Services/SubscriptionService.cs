using ClashSubManager.Models;
using Microsoft.Extensions.Localization;
using System.Diagnostics;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Subscription service core business logic
    /// </summary>
    public class SubscriptionService
    {
        private readonly IStringLocalizer<SubscriptionService> _localizer;
        private readonly IUserManagementService _userManagementService;
        private readonly FileService _fileService;
        private readonly ValidationService _validationService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            IStringLocalizer<SubscriptionService> localizer,
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
        /// Get user subscription configuration
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
                    Console.WriteLine($"Invalid user ID: {userId}");
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
                    Console.WriteLine($"Subscription URL template not configured for user: {userId}");
                    return SubscriptionResponse.CreateError(
                        _localizer["SubscriptionUrlTemplateNotConfigured"], 
                        "SUBSCRIPTION_URL_TEMPLATE_NOT_CONFIGURED");
                }

                // Get base template
                var template = await _fileService.LoadClashTemplateAsync();
                if (string.IsNullOrEmpty(template))
                {
                    Console.WriteLine("Clash template not found");
                    return SubscriptionResponse.CreateError(
                        _localizer["TemplateNotFound"], 
                        "TEMPLATE_NOT_FOUND");
                }

                // Get default IP list
                var defaultIPs = await _fileService.LoadDefaultIPsAsync();
                
                // Get user-specific IP list
                var dedicatedIPs = await _fileService.LoadUserDedicatedIPsAsync(userId);
                
                // Merge configuration and generate YAML
                var yamlContent = await _configurationService.GenerateSubscriptionConfigAsync(
                    template, 
                    subscriptionUrl, 
                    defaultIPs, 
                    dedicatedIPs);

                Console.WriteLine($"Subscription generated successfully for user: {userId}");
                return SubscriptionResponse.CreateSuccessFromYaml(yamlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription for user: {UserId}", userId);
                Console.WriteLine($"Error getting subscription: {ex.Message}");
                return SubscriptionResponse.CreateError(
                    _localizer["InternalServerError"], 
                    "INTERNAL_SERVER_ERROR");
            }
        }

        /// <summary>
        /// Update user IP configuration
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
                    Console.WriteLine($"Invalid user ID: {userId}");
                    return SubscriptionResponse.CreateError(
                        _localizer["InvalidUserId"], 
                        "INVALID_USER_ID");
                }

                // Parse CSV content
                var ipRecords = _validationService.ParseCSVContent(csvContent);
                if (!ipRecords.Any())
                {
                    Console.WriteLine($"No valid IP records found in CSV content");
                    return SubscriptionResponse.CreateError(
                        _localizer["NoValidIPRecords"], 
                        "NO_VALID_IP_RECORDS");
                }

                // Record user access
                await _userManagementService.RecordUserAccessAsync(userId);

                // Save user-specific IP list
                await _fileService.SaveUserDedicatedIPsAsync(userId, ipRecords);

                Console.WriteLine($"User IPs updated successfully for user: {userId}, count: {ipRecords.Count}");
                return SubscriptionResponse.CreateSuccess(
                    _localizer["UserIPsUpdated"].ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user IPs for user: {UserId}", userId);
                Console.WriteLine($"Error updating user IPs: {ex.Message}");
                return SubscriptionResponse.CreateError(
                    _localizer["InternalServerError"], 
                    "INTERNAL_SERVER_ERROR");
            }
        }

        /// <summary>
        /// Delete user configuration
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
                    Console.WriteLine($"Invalid user ID: {userId}");
                    return SubscriptionResponse.CreateError(
                        _localizer["InvalidUserId"], 
                        "INVALID_USER_ID");
                }

                // Delete user configuration
                var deleted = await _userManagementService.DeleteUserAsync(userId);
                if (!deleted)
                {
                    Console.WriteLine($"User not found for deletion: {userId}");
                    return SubscriptionResponse.CreateError(
                        _localizer["UserNotFound"], 
                        "USER_NOT_FOUND");
                }

                Console.WriteLine($"User config deleted successfully for user: {userId}");
                return SubscriptionResponse.CreateSuccess(
                    _localizer["UserConfigDeleted"].ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user config for user: {UserId}", userId);
                Console.WriteLine($"Error deleting user config: {ex.Message}");
                return SubscriptionResponse.CreateError(
                    _localizer["InternalServerError"], 
                    "INTERNAL_SERVER_ERROR");
            }
        }
    }
}
