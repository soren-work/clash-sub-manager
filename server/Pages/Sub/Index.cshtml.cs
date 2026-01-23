using ClashSubManager.Models;
using ClashSubManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace ClashSubManager.Pages.Sub
{
    /// <summary>
    /// User subscription interface page model
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly ValidationService _validationService;
        private readonly IUserManagementService _userManagementService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILogger<IndexModel> _logger;
        private readonly HttpClient _httpClient;

        public IndexModel(
            SubscriptionService subscriptionService,
            ValidationService validationService,
            IUserManagementService userManagementService,
            IStringLocalizer<SharedResources> localizer,
            ILogger<IndexModel> logger,
            HttpClient httpClient)
        {
            _subscriptionService = subscriptionService;
            _validationService = validationService;
            _userManagementService = userManagementService;
            _localizer = localizer;
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("clash");
        }

        /// <summary>
        /// User ID (obtained from route parameters)
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// GET /sub/{id} - Get user Clash subscription configuration
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>YAML configuration content or error response</returns>
        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                _logger.LogInformation("Processing subscription request for user: {UserId}", id);
                
                UserId = id ?? string.Empty;

                // Input validation
                if (!_validationService.ValidateUserId(UserId))
                {
                    _logger.LogWarning("Invalid user ID: {UserId}", UserId);
                    return CreateErrorResponse("Invalid user ID", "INVALID_USER_ID", 400);
                }

                // Validate user ID - verify through actual subscription address
                var subscriptionUrl = await _userManagementService.GetUserSubscriptionUrlAsync(UserId);
                if (string.IsNullOrEmpty(subscriptionUrl))
                {
                    _logger.LogWarning("Subscription URL template not configured for user: {UserId}", UserId);
                    return CreateErrorResponse(_localizer["SubscriptionUrlTemplateNotConfigured"], "SUBSCRIPTION_URL_TEMPLATE_NOT_CONFIGURED", 404);
                }

                // Validate user ID through actual subscription service
                var isValidUser = await ValidateUserIdWithSubscriptionService(subscriptionUrl, UserId);
                if (!isValidUser)
                {
                    _logger.LogWarning("User ID validation failed: {UserId}", UserId);
                    return CreateErrorResponse(_localizer["UserIdValidationFailed"], "USER_ID_VALIDATION_FAILED", 401);
                }

                // Get subscription configuration
                var subscriptionResponse = await _subscriptionService.GetSubscriptionAsync(UserId);
                if (!subscriptionResponse.Success)
                {
                    _logger.LogError("Subscription generation failed: {Message}", subscriptionResponse.Message);
                    var statusCode = subscriptionResponse.ErrorCode switch
                    {
                        "USER_CONFIG_NOT_FOUND" => 404,
                        "TEMPLATE_NOT_FOUND" => 404,
                        "INVALID_USER_ID" => 400,
                        _ => 500
                    };
                    return CreateErrorResponse(_localizer["SubscriptionGenerationFailed"], subscriptionResponse.ErrorCode, statusCode);
                }

                _logger.LogInformation("Subscription generated successfully for user: {UserId}", UserId);
                
                // Return YAML content
                return Content(subscriptionResponse.YAMLContent ?? string.Empty, "text/yaml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription request for user: {UserId}", UserId);
                _logger.LogError("Error processing subscription request: {Message}", ex.Message);
                return CreateErrorResponse(_localizer["InternalServerError"], "INTERNAL_SERVER_ERROR", 500);
            }
        }

        /// <summary>
        /// POST /sub/{id} - Update user preferred IP configuration
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Operation result</returns>
        public async Task<IActionResult> OnPostAsync(string id)
        {
            try
            {
                _logger.LogInformation("Processing IP update request for user: {UserId}", id);
                
                UserId = id ?? string.Empty;

                // Input validation
                if (!_validationService.ValidateUserId(UserId))
                {
                    _logger.LogWarning("Invalid user ID: {UserId}", UserId);
                    return CreateErrorResponse("Invalid user ID", "INVALID_USER_ID", 400);
                }

                // Read request body content
                string csvContent;
                using (var reader = new StreamReader(Request.Body))
                {
                    csvContent = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(csvContent))
                {
                    _logger.LogWarning("Empty CSV content received");
                    return CreateErrorResponse(_localizer["EmptyCSVContent"], "EMPTY_CSV_CONTENT", 400);
                }

                // Update user IP configuration
                var updateResponse = await _subscriptionService.UpdateUserIPsAsync(UserId, csvContent);
                if (!updateResponse.Success)
                {
                    _logger.LogError("IP update failed: {Message}", updateResponse.Message);
                    var statusCode = updateResponse.ErrorCode switch
                    {
                        "INVALID_USER_ID" => 400,
                        "NO_VALID_IP_RECORDS" => 400,
                        _ => 500
                    };
                    return CreateErrorResponse(_localizer["IPUpdateFailed"], updateResponse.ErrorCode, statusCode);
                }

                _logger.LogInformation("User IPs updated successfully for user: {UserId}", UserId);
                
                // Return success response
                return CreateJsonResponse(true, updateResponse.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user IPs for user: {UserId}", UserId);
                _logger.LogError("Error updating user IPs: {Message}", ex.Message);
                return CreateErrorResponse(_localizer["InternalServerError"], "INTERNAL_SERVER_ERROR", 500);
            }
        }

        /// <summary>
        /// DELETE /sub/{id} - Delete user configuration
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Operation result</returns>
        public async Task<IActionResult> OnDeleteAsync(string id)
        {
            try
            {
                _logger.LogInformation("Processing user config deletion request for user: {UserId}", id);
                
                UserId = id ?? string.Empty;

                // Input validation
                if (!_validationService.ValidateUserId(UserId))
                {
                    _logger.LogWarning("Invalid user ID: {UserId}", UserId);
                    return CreateErrorResponse("Invalid user ID", "INVALID_USER_ID", 400);
                }

                // Delete user configuration
                var deleteResponse = await _subscriptionService.DeleteUserConfigAsync(UserId);
                if (!deleteResponse.Success)
                {
                    _logger.LogError("User config deletion failed: {Message}", deleteResponse.Message);
                    var statusCode = deleteResponse.ErrorCode switch
                    {
                        "INVALID_USER_ID" => 400,
                        "USER_CONFIG_NOT_FOUND" => 404,
                        _ => 500
                    };
                    return CreateErrorResponse(_localizer["UserConfigDeletionFailed"], deleteResponse.ErrorCode, statusCode);
                }

                _logger.LogInformation("User config deleted successfully for user: {UserId}", UserId);
                
                // Return success response
                return CreateJsonResponse(true, deleteResponse.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user config for user: {UserId}", UserId);
                _logger.LogError("Error deleting user config: {Message}", ex.Message);
                return CreateErrorResponse(_localizer["InternalServerError"], "INTERNAL_SERVER_ERROR", 500);
            }
        }

        /// <summary>
        /// Validate user ID through subscription service
        /// </summary>
        /// <param name="subscriptionUrl">Subscription URL</param>
        /// <param name="userId">User ID</param>
        /// <returns>Whether the user ID is valid</returns>
        private async Task<bool> ValidateUserIdWithSubscriptionService(string subscriptionUrl, string userId)
        {
            try
            {
                // Build validation URL
                var validationUrl = $"{subscriptionUrl.TrimEnd('/')}/{userId}";
                _logger.LogInformation("Validating user ID with subscription service: {ValidationUrl}", validationUrl);
                
                // Send request to subscription service
                var response = await _httpClient.GetAsync(validationUrl);
                
                // Check response status code and content type
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    if (contentType == "text/yaml" || contentType == "application/x-yaml" || contentType == "text/plain")
                    {
                        _logger.LogInformation("User ID validation successful: {UserId}", userId);
                        return true;
                    }
                }

                _logger.LogWarning("User ID validation failed: {UserId}, Status: {StatusCode}", userId, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user ID with subscription service: {UserId}", userId);
                _logger.LogError("Error validating user ID: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Create error response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorCode">Error code</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>Error response</returns>
        private IActionResult CreateErrorResponse(string message, string? errorCode, int statusCode)
        {
            Response.StatusCode = statusCode;
            var errorResponse = new { success = false, message, errorCode };
            return new JsonResult(errorResponse);
        }

        /// <summary>
        /// Create JSON response
        /// </summary>
        /// <param name="success">Whether successful</param>
        /// <param name="message">Message</param>
        /// <returns>JSON response</returns>
        private IActionResult CreateJsonResponse(bool success, string message)
        {
            var response = new { success, message };
            return new JsonResult(response);
        }
    }
}
